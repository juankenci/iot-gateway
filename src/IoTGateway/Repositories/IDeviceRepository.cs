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
        void UpdateTelemetry(string deviceId, string componentId, double value);
        object? GetSensorValue(string deviceId, string componentId);

        // Gestión de Cola de Comandos
        void EnqueueCommand(string deviceId, CommandDto command);
        IEnumerable<CommandDto> GetAndClearPendingCommands(string deviceId);
    }
}