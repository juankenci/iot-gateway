namespace IoTGateway.Models
{
    public class DeviceRegistrationDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<DeviceComponentDto> Components { get; set; } = new();
        public DateTime LastSeen { get; set; }
        public bool Online => DateTime.UtcNow - LastSeen < TimeSpan.FromSeconds(15);
    }
}
