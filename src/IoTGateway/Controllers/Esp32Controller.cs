using Microsoft.AspNetCore.Mvc;
using IoTGateway.Models;
using IoTGateway.Services;

namespace IoTGateway.Controllers
{
    [ApiController]
    [Route("api/esp32")]
    public class Esp32Controller : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public Esp32Controller(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] DeviceRegistrationDto dto)
        {
            var success = _deviceService.RegisterDevice(dto);
            if (!success)
                return BadRequest(new { message = "El DeviceId es obligatorio." });

            return Ok(new { message = $"Dispositivo {dto.DeviceId} registrado exitosamente." });
        }

        [HttpPost("{deviceId}/telemetry/{componentId}")]
        public IActionResult PostTelemetry(string deviceId, string componentId, [FromBody] TelemetryDto dto)
        {
            var updated = _deviceService.UpdateTelemetry(deviceId, componentId, dto.Value);
            if (!updated)
                return NotFound(new { message = "Dispositivo no registrado en el Gateway." });

            return Ok(new { message = "Telemetría actualizada correctamente." });
        }
    }

}