# Guía del Gateway (para la defensa)

## Dónde está cada cosa (`src/IoTGateway`)

| Carpeta / archivo | Responsabilidad |
|---|---|
| `Controllers/Esp32Controller.cs` | Endpoints que usan los dispositivos |
| `Controllers/ClientController.cs` | Endpoints que usan Telegram / Claude / Postman |
| `Services/` | Reglas de negocio (registro, telemetría, comandos, archivos) y validaciones |
| `Repositories/InMemoryDeviceRepository.cs` | Almacén en memoria (dispositivos, lecturas, comandos, archivos) |
| `Models/` | Entidades y DTOs |
| `MiddlewareApiKey.cs` | Autenticación por `X-Api-Key` |
| `Program.cs` | DI, manejo global de errores, Swagger |
| `Contratos/` | Colección Postman (con tests) y ejemplos JSON |

## Decisiones de diseño

- **Genérico**: el ESP32 declara sus componentes al registrarse; agregar hardware nuevo no requiere tocar el Gateway.
- **Polling** en vez de push: el ESP32 está detrás de NAT/WiFi y no expone servidor; el polling además sirve de latido (`online`).
- **En memoria + capa Repository**: KISS para el TP; se puede cambiar por una BD sin tocar controllers.
- **Errores centralizados**: los servicios lanzan `KeyNotFoundException` (404) y `ArgumentException` (400); un único handler los traduce.
- **Archivos aparte de la telemetría**: las imágenes se suben como binario, no dentro de JSON.

## Limitaciones (y cómo se mejorarían)

- Sin persistencia → base de datos.
- Sin ack de comandos → estado del comando y confirmación del ESP32.
- API Key única, sin TLS → HTTPS y una clave por dispositivo.
- Un solo archivo por componente → historial de capturas.

## Preguntas típicas

- *¿Qué pasa si el Gateway se reinicia?* Pierde el estado; el ESP32 recibe 404 y se re-registra.
- *¿Cómo sé si un dispositivo está vivo?* `online` = contacto en los últimos 15 s (registro, telemetría o polling).
- *¿Cómo agrego un sensor nuevo?* Lo declara el ESP32 en `register`; no hay cambios en el Gateway.
- *¿Por qué no WebSockets/MQTT?* Complejidad innecesaria para el alcance; HTTP es suficiente y fácil de probar con Postman.
