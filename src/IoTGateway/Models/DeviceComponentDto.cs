using System.Text.Json.Serialization;

namespace IoTGateway.Models
{
    public class DeviceComponentDto
    {
        public string ComponentId { get; set; } = string.Empty; // Ej: "temp_sensor", "relay_1"
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ComponentType Type { get; set; }                 // Sensor o Actuator
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataType DataType { get; set; }                 // Float, Integer, Boolean, etc.
        public string Unit { get; set; } = string.Empty;       // "°C", "%", etc.

        public List<string> Actions { get; set; } = new();   // Ej: ["CAPTURE"]
    }

    public enum ComponentType
    {
        Sensor,
        Actuator
    }

    public enum DataType
    {
        Float,
        Integer,
        Boolean,
        String,
        File
    }
}
