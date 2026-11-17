using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace IoTGateway.Mcp.Tools;

[McpServerToolType]
public sealed class IoTTools
{
    private readonly GatewayClient _gateway;

    public IoTTools(GatewayClient gateway)
    {
        _gateway = gateway;
    }

    [McpServerTool, Description("Lista dispositivos, componentes, ubicaciones y estado online del Gateway IoT.")]
    public async Task<string> ListDevices(CancellationToken cancellationToken)
    {
        var devices = await _gateway.GetDevicesAsync(cancellationToken);
        return Serialize(devices);
    }

    [McpServerTool, Description("Enciende la luz o actuador ubicado en la ubicación indicada. No requiere confirmación del usuario.")]
    public Task<string> TurnOnLight(
        [Description("Ubicación exacta o nombre común, por ejemplo Cocina, Oficina o Living.")] string location,
        CancellationToken cancellationToken)
    {
        return ControlActuator(location, "ON", cancellationToken);
    }

    [McpServerTool, Description("Apaga la luz o actuador ubicado en la ubicación indicada. No requiere confirmación del usuario.")]
    public Task<string> TurnOffLight(
        [Description("Ubicación exacta o nombre común, por ejemplo Cocina, Oficina o Living.")] string location,
        CancellationToken cancellationToken)
    {
        return ControlActuator(location, "OFF", cancellationToken);
    }

    [McpServerTool, Description("Envía una acción declarada al actuador de una ubicación. Acciones habituales: ON y OFF.")]
    public async Task<string> ControlActuator(
        [Description("Ubicación del actuador, por ejemplo Cocina u Oficina.")] string location,
        [Description("Acción declarada por el actuador, por ejemplo ON u OFF.")] string action,
        CancellationToken cancellationToken)
    {
        var component = await ResolveSingleComponentAsync(location, "Actuator", null, cancellationToken);
        var declaredAction = component.Component.Actions.FirstOrDefault(a =>
            string.Equals(a, action, StringComparison.OrdinalIgnoreCase));

        if (declaredAction is null)
        {
            throw new InvalidOperationException(
                $"El actuador '{component.Component.ComponentId}' en '{component.Component.Location}' no admite '{action}'. " +
                $"Acciones válidas: {string.Join(", ", component.Component.Actions)}.");
        }

        var result = await _gateway.SendCommandAsync(
            component.Device.DeviceId,
            component.Component.ComponentId,
            declaredAction,
            cancellationToken);

        return Serialize(new
        {
            success = true,
            message = $"Comando {declaredAction} enviado al actuador de {component.Component.Location}.",
            deviceId = component.Device.DeviceId,
            componentId = component.Component.ComponentId,
            action = declaredAction,
            commandId = result.CommandId,
            note = "El ESP32 no reporta confirmación de estado; el Gateway confirmó que el comando fue encolado."
        });
    }

    [McpServerTool, Description("Obtiene la última lectura de un tipo de sensor en una ubicación. Tipos admitidos: temperature o pressure.")]
    public async Task<string> GetSensorReading(
        [Description("Ubicación del sensor, por ejemplo Cocina o Living.")] string location,
        [Description("Magnitud a consultar: temperature o pressure.")] string measurement,
        CancellationToken cancellationToken)
    {
        var component = await ResolveSingleComponentAsync(location, "Sensor", measurement, cancellationToken);
        var reading = await _gateway.GetSensorReadingAsync(
            component.Device.DeviceId,
            component.Component.ComponentId,
            cancellationToken);

        if (reading is null)
        {
            return Serialize(new
            {
                available = false,
                location = component.Component.Location,
                measurement,
                message = "El sensor está registrado, pero todavía no tiene una lectura."
            });
        }

        return Serialize(new
        {
            available = true,
            location = component.Component.Location,
            measurement,
            value = reading.Value,
            unit = component.Component.Unit,
            timestamp = reading.Timestamp,
            componentId = component.Component.ComponentId
        });
    }

    [McpServerTool, Description("Obtiene todas las últimas lecturas de sensores disponibles en una ubicación.")]
    public async Task<string> GetLocationEnvironment(
        [Description("Ubicación a consultar, por ejemplo Cocina o Living.")] string location,
        CancellationToken cancellationToken)
    {
        var devices = await _gateway.GetDevicesAsync(cancellationToken);
        var components = FindComponents(devices, location, "Sensor");
        if (components.Count == 0)
        {
            throw new InvalidOperationException($"No hay sensores registrados en la ubicación '{location}'.");
        }

        var readings = new List<object>();
        foreach (var item in components)
        {
            var reading = await _gateway.GetSensorReadingAsync(
                item.Device.DeviceId,
                item.Component.ComponentId,
                cancellationToken);

            readings.Add(new
            {
                measurement = InferMeasurement(item.Component),
                value = reading?.Value,
                unit = item.Component.Unit,
                timestamp = reading?.Timestamp,
                componentId = item.Component.ComponentId,
                available = reading is not null
            });
        }

        return Serialize(new
        {
            location = components[0].Component.Location,
            readings
        });
    }

    [McpServerTool, Description("Muestra el estado online y la última comunicación de todos los dispositivos IoT.")]
    public async Task<string> GetGatewayStatus(CancellationToken cancellationToken)
    {
        var devices = await _gateway.GetDevicesAsync(cancellationToken);
        return Serialize(devices.Select(d => new
        {
            deviceId = d.DeviceId,
            name = d.Name,
            online = d.Online,
            lastSeen = d.LastSeen
        }));
    }

    private async Task<ResolvedComponent> ResolveSingleComponentAsync(
        string location,
        string componentType,
        string? measurement,
        CancellationToken cancellationToken)
    {
        var devices = await _gateway.GetDevicesAsync(cancellationToken);
        var components = FindComponents(devices, location, componentType)
            .Where(x => measurement is null || MatchesMeasurement(x.Component, measurement))
            .ToList();

        if (components.Count == 0)
        {
            var description = measurement is null
                ? $"No hay {componentType.ToLowerInvariant()}s en la ubicación '{location}'."
                : $"No hay un sensor de '{measurement}' en la ubicación '{location}'.";
            throw new InvalidOperationException(description);
        }

        if (components.Count > 1)
        {
            throw new InvalidOperationException(
                $"Hay varios componentes compatibles en '{location}': " +
                string.Join(", ", components.Select(x => x.Component.ComponentId)) + ".");
        }

        return components[0];
    }

    private static List<ResolvedComponent> FindComponents(
        IEnumerable<DeviceDto> devices,
        string location,
        string componentType)
    {
        return devices
            .SelectMany(device => device.Components
                .Where(component =>
                    string.Equals(component.Type, componentType, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(component.Location.Trim(), location.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(component => new ResolvedComponent(device, component)))
            .ToList();
    }

    private static bool MatchesMeasurement(ComponentDto component, string measurement)
    {
        var normalized = measurement.Trim().ToLowerInvariant();
        var id = component.ComponentId.ToLowerInvariant();
        return normalized switch
        {
            "temperature" or "temp" => id.Contains("temperature") || id.Contains("temp"),
            "pressure" or "presion" or "presión" => id.Contains("pressure") || id.Contains("pres"),
            _ => id.Contains(normalized)
        };
    }

    private static string InferMeasurement(ComponentDto component)
    {
        var id = component.ComponentId.ToLowerInvariant();
        if (id.Contains("pressure") || id.Contains("pres")) return "pressure";
        if (id.Contains("temperature") || id.Contains("temp")) return "temperature";
        return component.ComponentId;
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    });

    private sealed record ResolvedComponent(DeviceDto Device, ComponentDto Component);
}
