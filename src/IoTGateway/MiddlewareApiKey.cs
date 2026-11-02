using System.Security.Cryptography;
using System.Text;

namespace IoTGateway;

public class MiddlewareApiKey
{
    public const string NombreHeader = "X-Api-Key";
    private readonly RequestDelegate _siguiente;
    private readonly byte[] _esperada;

    public MiddlewareApiKey(RequestDelegate siguiente, IConfiguration configuracion)
    {
        _siguiente = siguiente;
        var clave = configuracion["ApiKey"];
        if (string.IsNullOrWhiteSpace(clave))
            throw new InvalidOperationException(
                "Falta la configuracion 'ApiKey'. Usa: dotnet user-secrets set ApiKey <valor> (o la variable de entorno ApiKey).");
        _esperada = Encoding.UTF8.GetBytes(clave);
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        var ruta = contexto.Request.Path;
        var esPublica = ruta.StartsWithSegments("/swagger") || ruta == "/";

        if (!esPublica)
        {
            var recibida = Encoding.UTF8.GetBytes(contexto.Request.Headers[NombreHeader].ToString());
            if (!CryptographicOperations.FixedTimeEquals(recibida, _esperada))
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await contexto.Response.WriteAsJsonAsync(new ErrorDto("Falta o es invalido el header X-Api-Key"));
                return;
            }
        }

        await _siguiente(contexto);
    }
}
