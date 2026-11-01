using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IoTGateway;

public sealed class DeviceRegistry
{
    public static readonly TimeSpan ActiveWindow = TimeSpan.FromSeconds(15);

    private static readonly HashSet<string> KnownCapabilities = new() { "bmp280", "rgb_led", "hcsr04", "motor" };
    private static readonly HashSet<string> ActuatorTargets = new() { "rgb_led", "motor" };
    private static readonly Regex IdPattern = new("^[a-z0-9-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex ColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly ConcurrentDictionary<string, Device> _devices = new();
    private readonly TimeProvider _time;

    public DeviceRegistry(TimeProvider time) => _time = time;

    public static bool IsValidId(string id) => IdPattern.IsMatch(id);

    public string? Report(string id, ReportRequest request, out List<Command> pending)
    {
        pending = new List<Command>();

        if (request.Capabilities is null || request.Capabilities.Count == 0)
            return "'capabilities' es obligatorio y no puede estar vacio";
        var unknown = request.Capabilities.FirstOrDefault(c => !KnownCapabilities.Contains(c));
        if (unknown is not null)
            return $"Capability desconocida: '{unknown}'";

        var device = _devices.GetOrAdd(id, key => new Device(key));
        lock (device.Sync)
        {
            device.Capabilities = request.Capabilities.Distinct().ToList();
            device.Readings = request.Readings ?? new();
            device.State = request.State ?? new();
            device.LastSeen = _time.GetUtcNow().UtcDateTime;
            pending = device.Pending.ToList();
            device.Pending.Clear();
        }
        return null;
    }

    public DeviceDto? Get(string id) =>
        _devices.TryGetValue(id, out var device) ? ToDto(device) : null;

    public List<DeviceDto> List() =>
        _devices.Values.OrderBy(d => d.Id).Select(ToDto).ToList();

    public enum EnqueueResult { Ok, NotFound, Invalid }

    public EnqueueResult Enqueue(string id, Command command, out string? error)
    {
        error = null;
        if (!_devices.TryGetValue(id, out var device))
            return EnqueueResult.NotFound;

        if (!ActuatorTargets.Contains(command.Target))
        {
            error = $"Target invalido: '{command.Target}'. Validos: rgb_led, motor";
            return EnqueueResult.Invalid;
        }

        lock (device.Sync)
        {
            if (!device.Capabilities.Contains(command.Target))
            {
                error = $"El dispositivo '{id}' no tiene la capability '{command.Target}'";
                return EnqueueResult.Invalid;
            }

            error = ValidateValue(command);
            if (error is not null)
                return EnqueueResult.Invalid;

            device.Pending.Enqueue(command);
        }
        return EnqueueResult.Ok;
    }

    private static string? ValidateValue(Command command)
    {
        var v = command.Value;
        switch (command.Target)
        {
            case "rgb_led":
                return v.ValueKind == JsonValueKind.String && ColorPattern.IsMatch(v.GetString()!)
                    ? null
                    : "El valor de rgb_led debe ser un string '#RRGGBB'";
            case "motor":
                return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) && n is >= -100 and <= 100
                    ? null
                    : "El valor de motor debe ser un entero entre -100 y 100";
            default:
                return "Target invalido";
        }
    }

    private DeviceDto ToDto(Device d)
    {
        lock (d.Sync)
        {
            var active = _time.GetUtcNow().UtcDateTime - d.LastSeen < ActiveWindow;
            return new DeviceDto(d.Id, active, d.LastSeen,
                new List<string>(d.Capabilities),
                new Dictionary<string, double>(d.Readings),
                new Dictionary<string, JsonElement>(d.State));
        }
    }

    private sealed class Device
    {
        public Device(string id) => Id = id;
        public string Id { get; }
        public object Sync { get; } = new();
        public List<string> Capabilities { get; set; } = new();
        public Dictionary<string, double> Readings { get; set; } = new();
        public Dictionary<string, JsonElement> State { get; set; } = new();
        public DateTime LastSeen { get; set; }
        public Queue<Command> Pending { get; } = new();
    }
}
