using IoTGateway.Models;
using IoTGateway.Repositories;

namespace IoTGateway.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;

        public DeviceService(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        public bool RegisterDevice(DeviceRegistrationDto dto)
        {
            if (string.IsNullOrEmpty(dto.DeviceId)) return false;
            _deviceRepository.SaveDevice(dto);
            return true;
        }

        public IEnumerable<DeviceRegistrationDto> GetDevices()
        {
            return _deviceRepository.GetAllDevices();
        }

        public bool UpdateTelemetry(string deviceId, string componentId, object value)
        {
            var device = _deviceRepository.GetDeviceById(deviceId);
            if (device == null) return false;

            _deviceRepository.SetTelemetry(deviceId, componentId, value);
            return true;
        }

        public object? GetSensorValue(string deviceId, string componentId)
        {
            return _deviceRepository.GetTelemetry(deviceId, componentId);
        }

        public bool DeviceExists(string deviceId)
        {
            return _deviceRepository.GetDeviceById(deviceId) != null;
        }
    }
}