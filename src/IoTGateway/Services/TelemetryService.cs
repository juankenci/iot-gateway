using IoTGateway.Models;
using IoTGateway.Repositories;
using System.Text.RegularExpressions;

namespace IoTGateway.Services
{
    public class TelemetryService : ITelemetryService
    {
        public const int MaxBytesArchivo = 2 * 1024 * 1024;
        private static readonly Regex PatronId = new("^[a-z0-9_-]{1,32}$");
        private readonly IDeviceRepository _repository;

        public TelemetryService(IDeviceRepository repository)
        {
            _repository = repository;
        }



        public void RegisterDevice(DeviceRegistrationDto dto)
        {
            if (!PatronId.IsMatch(dto.DeviceId))
                throw new ArgumentException("deviceId invalido (a-z, 0-9, _ y -, max 32).");
            if (dto.Components.Any(c => !PatronId.IsMatch(c.ComponentId)))
                throw new ArgumentException("componentId invalido (a-z, 0-9, _ y -, max 32).");
            if (dto.Components.Select(c => c.ComponentId).Distinct().Count() != dto.Components.Count)
                throw new ArgumentException("componentId repetido.");
            _repository.SaveOrUpdateDevice(dto);
        }
        public IEnumerable<DeviceRegistrationDto> GetAllDevices() => _repository.GetAllDevices();
        private DeviceComponentDto ObtenerComponente(string deviceId, string componentId)
        {
            var d = _repository.GetDevice(deviceId)
                ?? throw new KeyNotFoundException($"Dispositivo '{deviceId}' no registrado.");
            return d.Components.FirstOrDefault(c => c.ComponentId == componentId)
                ?? throw new KeyNotFoundException($"Componente '{componentId}' no existe en '{deviceId}'.");
        }

        public void RecordTelemetry(string deviceId, string componentId, object value)
        {
            var comp = ObtenerComponente(deviceId, componentId);
            if (comp.Type != ComponentType.Sensor)
                throw new ArgumentException($"'{componentId}' no es un sensor.");
            _repository.UpdateTelemetry(deviceId, componentId, value);
            _repository.Touch(deviceId);
        }

        public void SaveFile(string deviceId, string componentId, byte[] contenido, string contentType)
        {
            ObtenerComponente(deviceId, componentId);
            if (contenido.Length < 2 || contenido.Length > MaxBytesArchivo)
                throw new ArgumentException("Tamano invalido (1 byte a 2 MB).");
            if (contentType.StartsWith("image/jpeg") && !(contenido[0] == 0xFF && contenido[1] == 0xD8))
                throw new ArgumentException("El archivo no es un JPEG valido.");
            _repository.SaveFile(deviceId, componentId, new Archivo(contenido, contentType, DateTime.UtcNow));
            _repository.Touch(deviceId);
        }
        public Archivo? GetFile(string deviceId, string componentId)
        {
            ObtenerComponente(deviceId, componentId);
            return _repository.GetFile(deviceId, componentId);
        }

        public Lectura? GetSensorValue(string deviceId, string componentId) => _repository.GetSensorValue(deviceId, componentId);

    }
}
