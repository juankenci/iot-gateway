using System.Collections.Generic;
using IoTGateway.Models;

namespace IoTGateway.Repositories
{
    public interface IDeviceRepository
    {
        // Gestión de Dispositivos y Telemetría
        void SaveOrUpdateDevice(DeviceRegistrationDto deviceDto);
        DeviceRegistrationDto? GetDevice(string deviceId);
        IEnumerable<DeviceRegistrationDto> GetAllDevices();
        void UpdateTelemetry(string deviceId, string componentId, object value);
        Lectura? GetSensorValue(string deviceId, string componentId);
        void SaveFile(string deviceId, string componentId, Archivo archivo);
        Archivo? GetFile(string deviceId, string componentId);
        void Touch(string deviceId);


        // Gestión de Cola de Comandos
        void EnqueueCommand(string deviceId, CommandDto command);
        IEnumerable<CommandDto> GetAndClearPendingCommands(string deviceId);
    }
}