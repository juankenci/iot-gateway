# IoT Gateway

API Gateway en ASP.NET Core (.NET 8) para un sistema IoT distribuido. Es **genérico**: cada ESP32 se registra indicando quién es y qué componentes tiene (Device Discovery); el Gateway no conoce el hardware de antemano.

```
Telegram / Claude / Postman ──HTTP──▶  IoT Gateway  ◀──HTTP── ESP32 (CAM, sensores, actuadores)
      (/api/client/*)                  (memoria)                 (/api/esp32/*)
```

## Cómo correrlo

```powershell
cd src\IoTGateway
dotnet run --launch-profile http     # escucha en http://0.0.0.0:5000
```

- Swagger (solo Development): <http://localhost:5000/swagger>
- Todas las rutas `/api/*` exigen el header `X-Api-Key`. Valor de desarrollo: `dev-key-local` (ver `appsettings.json`, clave `ApiKey`; se puede sobreescribir con user-secrets o variable de entorno `ApiKey`).
- Para que un ESP32 de la red llegue al Gateway, abrir el puerto 5000 en el firewall de Windows (PowerShell como administrador):

```powershell
New-NetFirewallRule -DisplayName "IoTGateway 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Private
```

## Endpoints

### ESP32

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/esp32/register` | Registra (o actualiza) el dispositivo y sus componentes |
| POST | `/api/esp32/{deviceId}/telemetry/{componentId}` | Publica una lectura `{"value": ...}` (solo sensores) |
| GET | `/api/esp32/{deviceId}/commands/pending` | Devuelve y vacía la cola de comandos (también actúa como latido) |
| POST | `/api/esp32/{deviceId}/components/{componentId}/file` | Sube un archivo binario (ej. JPEG, máx. 2 MB, se guarda solo el último) |

### Clientes

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/client/devices` | Lista dispositivos con `lastSeen` y `online` |
| GET | `/api/client/devices/{id}/sensors/{sensorId}` | Última lectura del sensor |
| POST | `/api/client/devices/{id}/commands` | Encola un comando `{componentId, action, payload?}` |
| GET | `/api/client/devices/{id}/components/{componentId}/file` | Descarga el último archivo |

Ejemplos de cuerpos en [`src/IoTGateway/Contratos/ejemplos`](src/IoTGateway/Contratos/ejemplos).

### Registro

```json
{
  "deviceId": "esp32-cam",
  "name": "ESP32-CAM Seguridad",
  "components": [
    { "componentId": "distance_sensor", "type": "Sensor",   "dataType": "Float", "unit": "cm" },
    { "componentId": "camera",          "type": "Actuator", "dataType": "File",  "unit": "", "actions": ["CAPTURE"] }
  ]
}
```

- `type`: `Sensor` | `Actuator`. `dataType`: `Float` | `Integer` | `Boolean` | `String` | `File`.
- `deviceId` y `componentId` cumplen `^[a-z0-9_-]{1,32}$`; los `componentId` no se repiten.
- Si un componente declara `actions`, los comandos solo pueden usar esas acciones.

## Errores

| Código | Cuándo |
|---|---|
| 400 | Datos inválidos, acción no declarada, comando a un sensor, telemetría a un actuador, archivo inválido |
| 401 | Falta o es incorrecto `X-Api-Key` |
| 404 | Dispositivo, componente, lectura o archivo inexistente |
| 500 | Error inesperado |

Formato: `{"message": "..."}`.

## Flujo de un comando (tomar una foto)

1. El ESP32 se registra (`register`) y hace polling de `commands/pending` cada pocos segundos.
2. El cliente envía `POST /api/client/devices/esp32-cam/commands` con `{"componentId":"camera","action":"CAPTURE"}` → `200 {message, commandId}`.
3. En su próximo polling el ESP32 recibe el comando, saca la foto y la sube a `.../components/camera/file`.
4. El cliente descarga la imagen con `GET /api/client/devices/esp32-cam/components/camera/file`.

## Pruebas

Colección Postman con tests automáticos en `src/IoTGateway/Contratos/postman`. Con el Gateway recién iniciado:

```powershell
npx newman run src\IoTGateway\Contratos\postman\IoTGateway_Postman_Collection.json
```

(Variables: `baseUrl`, `apiKey`, `deviceId`.) La colección está ordenada y asume estado limpio (memoria vacía).

## Limitaciones conocidas

- Estado **en memoria**: al reiniciar se pierde; los ESP32 deben volver a registrarse (ante un 404 deben re-registrarse).
- `Online` = último contacto hace menos de 15 s.
- Los comandos no tienen confirmación (ack): entrega al menos una vez.
- Solo se guarda la última lectura y el último archivo por componente.
- API Key única compartida; HTTP sin TLS (red local / desarrollo).
