# iot-gateway

Gateway central del sistema IoT (ASP.NET Core, .NET 8, Minimal API). Los ESP32 reportan por polling y los clientes (Postman, Telegram, Claude) consultan y envian comandos. Contrato: `Contratos/openapi.yaml` (v1.0.0).

## Ejecutar
```powershell
cd src/IoTGateway
dotnet user-secrets set ApiKey <valor-de-desarrollo>   # una sola vez; no se sube al repo
dotnet run --launch-profile http                       # escucha en http://0.0.0.0:5000
```
Swagger UI: http://localhost:5000/swagger. Todas las peticiones a `/api/*` requieren el header `X-Api-Key`.
En Windows, permitir el puerto 5000 en el firewall para que los ESP32 puedan conectarse.

## Endpoints
| Metodo | Ruta | Descripcion |
|---|---|---|
| POST | `/api/devices/{id}/report` | El ESP32 reporta y recibe comandos pendientes |
| GET | `/api/devices` | Lista dispositivos (`active` si lastSeen < 15 s) |
| GET | `/api/devices/{id}` | Detalle y ultimas lecturas |
| POST | `/api/devices/{id}/commands` | Encola un comando (`rgb_led`: `#RRGGBB`, `motor`: -100..100) |

El estado vive en memoria: al reiniciar, los ESP32 se registran solos en su siguiente `report`.
