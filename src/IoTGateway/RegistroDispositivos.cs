using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IoTGateway;

public sealed class RegistroDispositivos
{
    public static readonly TimeSpan VentanaActivo = TimeSpan.FromSeconds(15);

    private static readonly HashSet<string> CapacidadesConocidas = new() { "bmp280", "led_rgb", "hcsr04", "motor" };
    private static readonly HashSet<string> DestinosActuadores = new() { "led_rgb", "motor" };
    private static readonly Regex PatronId = new("^[a-z0-9-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex PatronColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly ConcurrentDictionary<string, Dispositivo> _dispositivos = new();
    private readonly TimeProvider _reloj;

    public RegistroDispositivos(TimeProvider reloj) => _reloj = reloj;

    public static bool EsIdValido(string id) => PatronId.IsMatch(id);

    public string? Reportar(string id, SolicitudReporte solicitud, out List<Comando> pendientes)
    {
        pendientes = new List<Comando>();

        if (solicitud.Capacidades is null || solicitud.Capacidades.Count == 0)
            return "'capacidades' es obligatorio y no puede estar vacio";
        var desconocida = solicitud.Capacidades.FirstOrDefault(c => !CapacidadesConocidas.Contains(c));
        if (desconocida is not null)
            return $"Capacidad desconocida: '{desconocida}'";

        var dispositivo = _dispositivos.GetOrAdd(id, clave => new Dispositivo(clave));
        lock (dispositivo.Bloqueo)
        {
            dispositivo.Capacidades = solicitud.Capacidades.Distinct().ToList();
            dispositivo.Lecturas = solicitud.Lecturas ?? new();
            dispositivo.Estado = solicitud.Estado ?? new();
            dispositivo.UltimaVez = _reloj.GetUtcNow().UtcDateTime;
            pendientes = dispositivo.Pendientes.ToList();
            dispositivo.Pendientes.Clear();
        }
        return null;
    }

    public DispositivoDto? Obtener(string id) =>
        _dispositivos.TryGetValue(id, out var dispositivo) ? ADto(dispositivo) : null;

    public List<DispositivoDto> Listar() =>
        _dispositivos.Values.OrderBy(d => d.Id).Select(ADto).ToList();

    public enum ResultadoEncolado { Ok, NoEncontrado, Invalido }

    public ResultadoEncolado EncolarComando(string id, Comando comando, out string? error)
    {
        error = null;
        if (!_dispositivos.TryGetValue(id, out var dispositivo))
            return ResultadoEncolado.NoEncontrado;

        if (!DestinosActuadores.Contains(comando.Destino))
        {
            error = $"Destino invalido: '{comando.Destino}'. Validos: led_rgb, motor";
            return ResultadoEncolado.Invalido;
        }

        lock (dispositivo.Bloqueo)
        {
            if (!dispositivo.Capacidades.Contains(comando.Destino))
            {
                error = $"El dispositivo '{id}' no tiene la capacidad '{comando.Destino}'";
                return ResultadoEncolado.Invalido;
            }

            error = ValidarValor(comando);
            if (error is not null)
                return ResultadoEncolado.Invalido;

            dispositivo.Pendientes.Enqueue(comando);
        }
        return ResultadoEncolado.Ok;
    }

    private static string? ValidarValor(Comando comando)
    {
        var v = comando.Valor;
        switch (comando.Destino)
        {
            case "led_rgb":
                return v.ValueKind == JsonValueKind.String && PatronColor.IsMatch(v.GetString()!)
                    ? null
                    : "El valor de led_rgb debe ser un string '#RRGGBB'";
            case "motor":
                return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) && n is >= -100 and <= 100
                    ? null
                    : "El valor de motor debe ser un entero entre -100 y 100";
            default:
                return "Destino invalido";
        }
    }

    private DispositivoDto ADto(Dispositivo d)
    {
        lock (d.Bloqueo)
        {
            var activo = _reloj.GetUtcNow().UtcDateTime - d.UltimaVez < VentanaActivo;
            return new DispositivoDto(d.Id, activo, d.UltimaVez,
                new List<string>(d.Capacidades),
                new Dictionary<string, double>(d.Lecturas),
                new Dictionary<string, JsonElement>(d.Estado));
        }
    }

    private sealed class Dispositivo
    {
        public Dispositivo(string id) => Id = id;
        public string Id { get; }
        public object Bloqueo { get; } = new();
        public List<string> Capacidades { get; set; } = new();
        public Dictionary<string, double> Lecturas { get; set; } = new();
        public Dictionary<string, JsonElement> Estado { get; set; } = new();
        public DateTime UltimaVez { get; set; }
        public Queue<Comando> Pendientes { get; } = new();
    }
}
