using System.Collections.Generic;
using IoTGateway.Models;
using IoTGateway.Repositories;

namespace IoTGateway.Services
{
    public interface ICommandService
    {
        void SendCommand(string deviceId, CommandDto command);
        IEnumerable<CommandDto> FetchPendingCommands(string deviceId);
    }

    public class CommandService : ICommandService
    {
        private readonly IDeviceRepository _repository;

        public CommandService(IDeviceRepository repository)
        {
            _repository = repository;
        }

        public void SendCommand(string deviceId, CommandDto command)
        {
            // Podríamos agregar validación previa: verificar si el dispositivo existe
            _repository.EnqueueCommand(deviceId, command);
        }

        public IEnumerable<CommandDto> FetchPendingCommands(string deviceId)
        {
            return _repository.GetAndClearPendingCommands(deviceId);
        }
    }
}