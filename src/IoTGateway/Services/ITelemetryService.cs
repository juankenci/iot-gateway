using System.Collections.Generic;
using IoTGateway.Models;
using IoTGateway.Repositories;

namespace IoTGateway.Services
{
    public interface ITelemetryService
    {
        void RegisterDevice(DeviceRegistrationDto deviceDto);
        IEnumerable<DeviceRegistrationDto> GetAllDevices();
        void RecordTelemetry(string deviceId, string componentId, object value);
        Lectura? GetSensorValue(string deviceId, string componentId);
        void SaveFile(string deviceId, string componentId, byte[] contenido, string contentType);
        Archivo? GetFile(string deviceId, string componentId);
    }
}