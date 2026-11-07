using Microsoft.AspNetCore.Mvc;
using IoTGateway.Models;
using IoTGateway.Services;

namespace IoTGateway.Controllers
{
    [ApiController]
    [Route("api/client/devices")]
    public class ClientController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public ClientController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpGet]
        public IActionResult GetDevices()
        {
            return Ok(_deviceService.GetDevices());
        }

        [HttpGet("{deviceId}/sensors/{componentId}")]
        public IActionResult GetSensorValue(string deviceId, string componentId)
        {
            var value = _deviceService.GetSensorValue(deviceId, componentId);
            if (value == null)
                return NotFound(new { message = "No se encontró valor para este sensor o el dispositivo está inactivo." });

            return Ok(new { deviceId, componentId, value });
        }
    }
}