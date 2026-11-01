using System.Text.Json;

namespace IoTGateway;

public record Command(string Target, JsonElement Value);

public record ReportRequest(
    List<string>? Capabilities,
    Dictionary<string, double>? Readings,
    Dictionary<string, JsonElement>? State);

public record ReportResponse(List<Command> Commands);

public record DeviceDto(
    string Id,
    bool Active,
    DateTime LastSeen,
    List<string> Capabilities,
    Dictionary<string, double> Readings,
    Dictionary<string, JsonElement> State);

public record ErrorDto(string Error);
