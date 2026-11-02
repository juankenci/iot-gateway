using System.Text.Json;

namespace IoTGateway;

public record Comando(string Destino, JsonElement Valor);

public record SolicitudReporte(
    List<string>? Capacidades,
    Dictionary<string, double>? Lecturas,
    Dictionary<string, JsonElement>? Estado);

public record RespuestaReporte(List<Comando> Comandos);

public record DispositivoDto(
    string Id,
    bool Activo,
    DateTime UltimaVez,
    List<string> Capacidades,
    Dictionary<string, double> Lecturas,
    Dictionary<string, JsonElement> Estado);

public record ErrorDto(string Error);
