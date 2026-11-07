using IoTGateway.Models;

namespace IoTGateway.Repositories
{
    public interface IDeviceRepository
    {
        void SaveDevice(DeviceRegistrationDto device);
        IEnumerable<DeviceRegistrationDto> GetAllDevices();
        DeviceRegistrationDto? GetDeviceById(string deviceId);

        void SetTelemetry(string deviceId, string componentId, object value);
        object? GetTelemetry(string deviceId, string componentId);
    }
}