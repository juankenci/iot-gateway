namespace IoTGateway.Models
{
    public class DeviceRegistrationDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<DeviceComponentDto> Components { get; set; } = new();
    }
}
