using IoTGateway.Models;
using IoTGateway.Repositories;

namespace IoTGateway.Services
{
    public class CommandService : ICommandService
    {
        private readonly IDeviceRepository _repository;

        public CommandService(IDeviceRepository repository)
        {
            _repository = repository;
        }

        public void SendCommand(string deviceId, CommandDto command)
        {
            var device = _repository.GetDevice(deviceId)
                ?? throw new KeyNotFoundException($"Dispositivo '{deviceId}' no registrado.");
            var comp = device.Components.FirstOrDefault(c => c.ComponentId == command.ComponentId)
                ?? throw new KeyNotFoundException($"Componente '{command.ComponentId}' no existe.");
            if (comp.Type != ComponentType.Actuator)
                throw new ArgumentException($"'{comp.ComponentId}' no es un actuador.");
            if (comp.Actions.Count > 0 &&
                !comp.Actions.Contains(command.Action, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Accion '{command.Action}' no soportada. Validas: {string.Join(", ", comp.Actions)}");

            command.CommandId = Guid.NewGuid().ToString();   // el cliente no elige el id
            command.CreatedAt = DateTime.UtcNow;
            _repository.EnqueueCommand(deviceId, command);
        }

        public IEnumerable<CommandDto> FetchPendingCommands(string deviceId)
        {
            if (_repository.GetDevice(deviceId) is null)
                throw new KeyNotFoundException($"Dispositivo '{deviceId}' no registrado.");
            _repository.Touch(deviceId);   // el polling también cuenta como "latido"
            return _repository.GetAndClearPendingCommands(deviceId);
        }
    }
}
