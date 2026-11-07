using IoTGateway.Models;

namespace IoTGateway.Services
{
    public interface IDeviceService
    {
        bool RegisterDevice(DeviceRegistrationDto dto);
        IEnumerable<DeviceRegistrationDto> GetDevices();
        bool UpdateTelemetry(string deviceId, string componentId, object value);
        object? GetSensorValue(string deviceId, string componentId);
        bool DeviceExists(string deviceId);
    }
}