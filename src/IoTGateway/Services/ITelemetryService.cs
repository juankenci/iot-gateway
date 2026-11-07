using System.Collections.Generic;
using IoTGateway.Models;
using IoTGateway.Repositories;

namespace IoTGateway.Services
{
    public interface ITelemetryService
    {
        void RegisterDevice(DeviceRegistrationDto deviceDto);
        IEnumerable<DeviceRegistrationDto> GetAllDevices();
        void RecordTelemetry(string deviceId, string componentId, double value);
        object? GetSensorValue(string deviceId, string componentId);
    }   
}