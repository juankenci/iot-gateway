using Microsoft.AspNetCore.Mvc;
using IoTGateway.Models;
using IoTGateway.Services;

namespace IoTGateway.Controllers
{
    [ApiController]
    [Route("api/client")]
    public class ClientController : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;
        private readonly ICommandService _commandService;

        public ClientController(ITelemetryService telemetryService, ICommandService commandService)
        {
            _telemetryService = telemetryService;
            _commandService = commandService;
        }

        [HttpGet("devices")]
        public IActionResult GetDevices() => Ok(_telemetryService.GetAllDevices());

        [HttpGet("devices/{deviceId}/sensors/{sensorId}")]
        public IActionResult GetSensorValue(string deviceId, string sensorId)
        {
            var l = _telemetryService.GetSensorValue(deviceId, sensorId);
            if (l == null) return NotFound(new { message = "Sensor o lectura no encontrada." });
            return Ok(new { deviceId, sensorId, value = l.Valor, timestamp = l.Fecha });
        }
        [HttpGet("devices/{deviceId}/components/{componentId}/file")]
        public IActionResult GetFile(string deviceId, string componentId)
        {
            var a = _telemetryService.GetFile(deviceId, componentId);
            if (a == null) return NotFound(new { message = "Todavía no hay archivo." });
            return File(a.Contenido, a.ContentType);
        }


        // Enviar comando a un actuador del ESP32
        [HttpPost("devices/{deviceId}/commands")]
        public IActionResult SendCommand(string deviceId, [FromBody] CommandDto command)
        {
            _commandService.SendCommand(deviceId, command);
            return Ok(new { message = "Comando encolado exitosamente.", commandId = command.CommandId });
        }
    }
}