using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IoTGateway.Mcp;

public sealed class GatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public GatewayClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<DeviceDto>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        return await GetAsync<List<DeviceDto>>("api/client/devices", cancellationToken) ?? [];
    }

    public Task<SensorReadingDto?> GetSensorReadingAsync(
        string deviceId,
        string componentId,
        CancellationToken cancellationToken)
    {
        return GetAsync<SensorReadingDto>(
            $"api/client/devices/{Uri.EscapeDataString(deviceId)}/sensors/{Uri.EscapeDataString(componentId)}",
            cancellationToken);
    }

    public async Task<CommandResultDto> SendCommandAsync(
        string deviceId,
        string componentId,
        string action,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"api/client/devices/{Uri.EscapeDataString(deviceId)}/commands",
            new { componentId, action },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateGatewayExceptionAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<CommandResultDto>(JsonOptions, cancellationToken)
            ?? new CommandResultDto("Comando aceptado por el Gateway.", null);
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateGatewayExceptionAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static async Task<Exception> CreateGatewayExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var error = JsonSerializer.Deserialize<ErrorDto>(body, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return new InvalidOperationException(
                    $"El Gateway respondió HTTP {(int)response.StatusCode}: {error.Message}");
            }
        }
        catch (JsonException)
        {
            // Use the status code below when the Gateway response is not JSON.
        }

        return new InvalidOperationException(
            $"El Gateway respondió HTTP {(int)response.StatusCode}.");
    }
}

public sealed class DeviceDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ComponentDto> Components { get; set; } = [];
    public DateTime LastSeen { get; set; }
    public bool Online { get; set; }
}

public sealed class ComponentDto
{
    public string ComponentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = [];
}

public sealed class SensorReadingDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string SensorId { get; set; } = string.Empty;
    public JsonElement Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed record CommandResultDto(string? Message, string? CommandId);

public sealed class ErrorDto
{
    public string? Message { get; set; }
}
