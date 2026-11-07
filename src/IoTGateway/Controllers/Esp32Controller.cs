using Microsoft.AspNetCore.Mvc;
using IoTGateway.Models;
using IoTGateway.Services;

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
            _telemetryService.RecordTelemetry(deviceId, componentId, payload.Value);
            return Ok(new { message = "Telemetría recibida correctamente." });
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
        public double Value { get; set; }
    }
}