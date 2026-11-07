using IoTGateway.Models;
using IoTGateway.Repositories;

namespace IoTGateway.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly IDeviceRepository _repository;

        public TelemetryService(IDeviceRepository repository)
        {
            _repository = repository;
        }

        public void RegisterDevice(DeviceRegistrationDto deviceDto) => _repository.SaveOrUpdateDevice(deviceDto);
        public IEnumerable<DeviceRegistrationDto> GetAllDevices() => _repository.GetAllDevices();
        public void RecordTelemetry(string deviceId, string componentId, double value) => _repository.UpdateTelemetry(deviceId, componentId, value);
        public object? GetSensorValue(string deviceId, string componentId) => _repository.GetSensorValue(deviceId, componentId);
    }
}
