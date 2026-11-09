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
}