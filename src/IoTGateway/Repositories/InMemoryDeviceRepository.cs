using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IoTGateway.Models;

namespace IoTGateway.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly ConcurrentDictionary<string, DeviceRegistrationDto> _devices = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Lectura>> _telemetry = new();
        private readonly ConcurrentDictionary<string, Archivo> _archivos = new();

        // Nueva cola de comandos concurrente por dispositivo
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CommandDto>> _commandQueues = new();

        public void SaveOrUpdateDevice(DeviceRegistrationDto d)
        {
            d.LastSeen = DateTime.UtcNow;
            _devices[d.DeviceId] = d;
        }
        public void Touch(string deviceId)
        {
            if (_devices.TryGetValue(deviceId, out var d)) d.LastSeen = DateTime.UtcNow;
        }
        public DeviceRegistrationDto? GetDevice(string deviceId)
        {
            _devices.TryGetValue(deviceId, out var device);
            return device;
        }

        public IEnumerable<DeviceRegistrationDto> GetAllDevices() => _devices.Values;

        public void UpdateTelemetry(string deviceId, string componentId, object value)
        {
            var t = _telemetry.GetOrAdd(deviceId, _ => new ConcurrentDictionary<string, Lectura>());
            t[componentId] = new Lectura(value, DateTime.UtcNow);
        }

        public Lectura? GetSensorValue(string deviceId, string componentId) => _telemetry.TryGetValue(deviceId, out var t) && t.TryGetValue(componentId, out var l) ? l : null;

        public void SaveFile(string deviceId, string componentId, Archivo a) => _archivos[$"{deviceId}/{componentId}"] = a;
        
        public Archivo? GetFile(string deviceId, string componentId) => _archivos.TryGetValue($"{deviceId}/{componentId}", out var a) ? a : null;

        public void EnqueueCommand(string deviceId, CommandDto command)
        {
            var queue = _commandQueues.GetOrAdd(deviceId, _ => new ConcurrentQueue<CommandDto>());
            queue.Enqueue(command);
        }

        public IEnumerable<CommandDto> GetAndClearPendingCommands(string deviceId)
        {
            var commands = new List<CommandDto>();
            if (_commandQueues.TryGetValue(deviceId, out var queue))
            {
                while (queue.TryDequeue(out var cmd))
                {
                    commands.Add(cmd);
                }
            }
            return commands;
        }
    }
}