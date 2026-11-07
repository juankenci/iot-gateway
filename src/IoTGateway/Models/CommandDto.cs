using System;

namespace IoTGateway.Models
{
    public class CommandDto
    {
        public string CommandId { get; set; } = Guid.NewGuid().ToString();
        public string ComponentId { get; set; } = string.Empty; // Ej: "main_relay"
        public string Action { get; set; } = string.Empty;       // Ej: "SET", "TOGGLE", "READ"
        public object? Payload { get; set; }                     // Ej: true, 50, o null
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}