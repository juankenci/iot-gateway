using System.Collections.Concurrent;
using IoTGateway.Models;

namespace IoTGateway.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly ConcurrentDictionary<string, DeviceRegistrationDto> _devices = new();
        private readonly ConcurrentDictionary<string, object> _sensorValues = new();

        public void SaveDevice(DeviceRegistrationDto device)
        {
            _devices[device.DeviceId] = device;
        }

        public IEnumerable<DeviceRegistrationDto> GetAllDevices()
        {
            return _devices.Values;
        }

        public DeviceRegistrationDto? GetDeviceById(string deviceId)
        {
            _devices.TryGetValue(deviceId, out var device);
            return device;
        }

        public void SetTelemetry(string deviceId, string componentId, object value)
        {
            string key = $"{deviceId}:{componentId}";
            _sensorValues[key] = value;
        }

        public object? GetTelemetry(string deviceId, string componentId)
        {
            string key = $"{deviceId}:{componentId}";
            _sensorValues.TryGetValue(key, out var value);
            return value;
        }
    }
}