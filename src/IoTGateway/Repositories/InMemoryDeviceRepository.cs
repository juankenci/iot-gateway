using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using IoTGateway.Models;

namespace IoTGateway.Repositories
{
    public class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly ConcurrentDictionary<string, DeviceRegistrationDto> _devices = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> _telemetry = new();

        // Nueva cola de comandos concurrente por dispositivo
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CommandDto>> _commandQueues = new();

        public void SaveOrUpdateDevice(DeviceRegistrationDto deviceDto)
        {
            _devices[deviceDto.DeviceId] = deviceDto;
        }

        public DeviceRegistrationDto? GetDevice(string deviceId)
        {
            _devices.TryGetValue(deviceId, out var device);
            return device;
        }

        public IEnumerable<DeviceRegistrationDto> GetAllDevices() => _devices.Values;

        public void UpdateTelemetry(string deviceId, string componentId, double value)
        {
            var deviceTelemetry = _telemetry.GetOrAdd(deviceId, _ => new ConcurrentDictionary<string, double>());
            deviceTelemetry[componentId] = value;
        }

        public object? GetSensorValue(string deviceId, string componentId)
        {
            if (_telemetry.TryGetValue(deviceId, out var deviceTelemetry))
            {
                if (deviceTelemetry.TryGetValue(componentId, out var val))
                    return val;
            }
            return null;
        }

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