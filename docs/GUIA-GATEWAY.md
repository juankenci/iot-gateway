# Guía paso a paso del Gateway (.NET 8)

Pensada para quien viene de .NET Framework 4.6.1 y para preparar la defensa. Todo el código, las rutas y el JSON están en español.

## 1. Qué hace el Gateway
Es el servidor central. Recibe a los ESP32 (que reportan cada 5 s) y a los clientes (Postman, Telegram, Claude). Guarda todo en memoria y **nunca llama a un ESP32**: los comandos se encolan y el ESP32 los recoge en su siguiente `reporte`.

```
Cliente --POST comandos--> [cola del dispositivo] --(respuesta del próximo reporte)--> ESP32
ESP32  --POST reporte (lecturas)--> [lecturas + ultimaVez]  <--GET dispositivos-- Cliente
```

## 2. Archivos y para qué sirve cada uno
| Archivo | Rol | Equivalente en .NET Framework |
|---|---|---|
| `Program.cs` | Arranque, servicios, middleware y rutas | `Global.asax` + `Startup` + controllers |
| `Modelos.cs` | DTOs (formatos JSON de entrada y salida) | Clases modelo |
| `RegistroDispositivos.cs` | Lógica de negocio: registro, cola, validaciones | Clase de servicio / repositorio |
| `MiddlewareApiKey.cs` | Verifica el header `X-Api-Key` | `DelegatingHandler` / `ActionFilter` |
| `appsettings.json` | Configuración (URL y puerto) | `Web.config` |
| `Properties/launchSettings.json` | Perfiles de ejecución en desarrollo | Propiedades del proyecto web |
| `IoTGateway.csproj` | Proyecto: .NET 8 y paquetes | `.csproj` + `packages.config` |

## 3. `Program.cs`, de arriba hacia abajo
1. **`WebApplication.CreateBuilder(args)`**: crea el "builder" (configuración, logging, DI). Reemplaza al `Startup.ConfigureServices`.
2. **Registro de servicios (DI integrada):**
   - `AddSingleton(TimeProvider.System)`: reloj inyectable (facilita probar el tiempo).
   - `AddSingleton<RegistroDispositivos>()`: **una única instancia** para toda la app. Es clave: el estado en memoria debe compartirse entre peticiones.
   - `AddSwaggerGen`: genera la documentación y declara el esquema de API key.
3. **`builder.Build()`**: construye la app.
4. **Pipeline (el orden importa):** `UseSwagger` → `UseSwaggerUI` → `UseMiddleware<MiddlewareApiKey>`. Cada petición pasa por ese orden.
5. **Rutas con `MapGroup("/api/dispositivos")`:** agrupa las 4 rutas bajo el mismo prefijo.
6. **Endpoints**, cada uno es una lambda cuyos parámetros se resuelven solos:
   - `string id` viene de la ruta.
   - `SolicitudReporte? solicitud` / `Comando? comando` se deserializan del body JSON.
   - `RegistroDispositivos registro` y `ILogger<Program> log` los inyecta la DI.
7. **`Results.Ok / BadRequest / NotFound / Accepted`**: devuelven el código HTTP (equivalentes a `Ok()`, `BadRequest()` de Web API).
8. **`public partial class Program { }`**: permite que un proyecto de tests de integración acceda al `Program`.

## 4. Los endpoints en detalle
### `POST /api/dispositivos/{id}/reporte` (ESP32)
1. Valida el `id` (`a-z`, `0-9`, `-`, máx. 32).
2. Llama a `registro.Reportar(...)`, que valida las capacidades y **actualiza lecturas, estado y `ultimaVez`**.
3. Devuelve `200` con `{"comandos":[...]}` y **vacía la cola**.
4. Si el dispositivo no existía, se crea: así funciona el *Device Discovery*.

### `GET /api/dispositivos` y `GET /api/dispositivos/{id}`
Devuelven `DispositivoDto`. El campo `activo` se **calcula en el momento** (`ahora - ultimaVez < 15 s`); no hay hilos ni timers.

### `POST /api/dispositivos/{id}/comandos`
`registro.EncolarComando` valida en este orden: dispositivo existe (404) → `destino` válido → el dispositivo tiene esa capacidad → formato del valor. Si todo está bien, encola y responde `202`. Si no, `400` con el motivo.

## 5. `RegistroDispositivos.cs`
- **`ConcurrentDictionary<string, Dispositivo>`**: diccionario seguro entre hilos. Kestrel atiende peticiones en paralelo.
- **`GetOrAdd`**: obtiene el dispositivo o lo crea de forma atómica.
- **`lock (dispositivo.Bloqueo)`**: protege los datos de *un* dispositivo (listas, cola) mientras se leen o modifican. El diccionario es seguro por sí solo, pero los objetos que contiene no.
- **`Queue<Comando>`**: cola FIFO; los comandos se entregan en el orden en que llegaron.
- **Regex precompiladas**: `PatronId` (`^[a-z0-9-]{1,32}$`) y `PatronColor` (`^#[0-9A-Fa-f]{6}$`).
- **`ValidarValor`**: `led_rgb` exige un string `#RRGGBB`; `motor` exige un entero entre -100 y 100.
- **`ADto`**: copia los datos bajo el lock para no exponer los objetos internos.
- **`ResultadoEncolado`**: enum que evita usar excepciones para el flujo normal.

## 6. `MiddlewareApiKey.cs`
- En el constructor lee `ApiKey` de la configuración (user-secrets o variable de entorno). **Si falta, la app no arranca** (falla rápido).
- En `InvokeAsync` deja pasar `/` y `/swagger`, y para todo lo demás compara el header con `CryptographicOperations.FixedTimeEquals` (comparación en tiempo constante, evita ataques de tiempo).
- Si no coincide, responde `401` y **no llama a `_siguiente`**, o sea, corta el pipeline.

## 7. Configuración y secretos
- La clave **nunca** va en el repo: `dotnet user-secrets set ApiKey <valor>` (se guarda fuera del proyecto, vinculado por `UserSecretsId`).
- En otro equipo o servidor se usa la variable de entorno `ApiKey`.
- El puerto está en `appsettings.json` (`Urls`: `http://0.0.0.0:5000`). `0.0.0.0` permite conexiones desde la red local (los ESP32).

## 8. Cómo ejecutarlo y probarlo
```powershell
cd src/IoTGateway
dotnet user-secrets set ApiKey dev-key-local
dotnet run --launch-profile http
```
- Swagger UI: http://localhost:5000/swagger (botón **Authorize** para poner la API key).
- Si cambiás el `.csproj` o agregás secretos, **recompilá** (`dotnet build`) antes de usar `--no-build`.
- Abrir el puerto 5000 en el firewall de Windows para los ESP32.

Pruebas rápidas en PowerShell:
```powershell
$h = @{ "X-Api-Key" = "dev-key-local" }
Invoke-RestMethod http://localhost:5000/api/dispositivos -Headers $h
Invoke-RestMethod http://localhost:5000/api/dispositivos/esp32-1/reporte -Method Post -Headers $h `
  -ContentType "application/json" -Body '{"capacidades":["bmp280","led_rgb"],"lecturas":{"temperatura":23.4}}'
```

## 9. Decisiones de diseño (para la defensa)
| Decisión | Motivo |
|---|---|
| Polling, el ESP32 siempre inicia | Funciona detrás de cualquier red; el ESP32 no necesita servidor HTTP |
| `reporte` único (registro + heartbeat + comandos) | Un solo mecanismo, menos código y menos fallos |
| Estado en memoria | Alcanza para el TP; los ESP32 se re-registran solos tras un reinicio |
| `activo` calculado al consultar | Sin hilos ni timers; cumple el requisito de Device Discovery |
| Validación en el Gateway | Un comando inválido nunca llega al hardware |
| API key + user-secrets | Seguridad mínima sin subir secretos al repo |
| Minimal API | Menos ceremonia que controllers para solo 4 endpoints |
| Nombres en español | Más legible para el grupo y para la defensa |

## 10. Limitaciones conocidas y mejoras posibles
- Se pierde todo al reiniciar el Gateway (mitigable con SQLite).
- Sin HTTPS: tráfico en claro dentro de la red local.
- Sin límite de tamaño ni de longitud de la cola de comandos.
- Sin histórico de lecturas.
- Latencia de comandos de hasta 5 s por diseño.
- Posibles extras: endpoint de histórico, HTTPS, reglas automáticas (por ejemplo, parar el motor si `distancia < 15`).

## 11. Preguntas típicas de la defensa
- **¿Cómo sabe el Gateway que un ESP32 está vivo?** Por `ultimaVez`; si pasaron más de 15 s, `activo = false`.
- **¿Qué pasa si dos clientes envían comandos a la vez?** Se encolan bajo `lock` y se entregan en orden.
- **¿Y si se cae el Gateway?** Los ESP32 reintentan en cada ciclo y se vuelven a registrar solos.
- **¿Por qué no WebSockets o MQTT?** Por simplicidad (KISS); HTTP alcanza para el requisito y se prueba con Postman.
- **¿Dónde se valida un comando?** En `RegistroDispositivos.EncolarComando`, antes de encolarlo.
