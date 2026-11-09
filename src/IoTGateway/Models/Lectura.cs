namespace IoTGateway.Models
{
    public record Lectura(object Valor, DateTime Fecha);
    public record Archivo(byte[] Contenido, string ContentType, DateTime Fecha);
}