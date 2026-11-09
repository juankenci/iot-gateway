using Microsoft.AspNetCore.Mvc;
using IoTGateway.Models;
using IoTGateway.Services;
using System.Text.Json;

namespace IoTGateway.Controllers
{
    [ApiController]
    [Route("api/esp32")]
    public class Esp32Controller : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;
        private readonly ICommandService _commandService;

        public Esp32Controller(ITelemetryService telemetryService, ICommandService commandService)
        {
            _telemetryService = telemetryService;
            _commandService = commandService;
        }

        [HttpPost("register")]
        public IActionResult RegisterDevice([FromBody] DeviceRegistrationDto dto)
        {
            _telemetryService.RegisterDevice(dto);
            return Ok(new { message = $"Dispositivo {dto.DeviceId} registrado exitosamente." });
        }

        [HttpPost("{deviceId}/telemetry/{componentId}")]
        public IActionResult PostTelemetry(string deviceId, string componentId, [FromBody] TelemetryPayload payload)
        {
            if (payload.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                throw new ArgumentException("Falta 'value'.");
            _telemetryService.RecordTelemetry(deviceId, componentId, payload.Value);
            return Ok(new { message = "Telemetría recibida correctamente." });
        }

        [HttpPost("{deviceId}/components/{componentId}/file")]
        [RequestSizeLimit(TelemetryService.MaxBytesArchivo)]
        public async Task<IActionResult> PostFile(string deviceId, string componentId)
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            _telemetryService.SaveFile(deviceId, componentId, ms.ToArray(),
                Request.ContentType ?? "application/octet-stream");
            return Ok(new { message = "Archivo recibido.", bytes = ms.Length });
        }


        // Polling de Comandos desde el ESP32
        [HttpGet("{deviceId}/commands/pending")]
        public IActionResult GetPendingCommands(string deviceId)
        {
            var commands = _commandService.FetchPendingCommands(deviceId);
            return Ok(commands);
        }
    }

    public class TelemetryPayload
    {
        public JsonElement Value { get; set; }
    }
}