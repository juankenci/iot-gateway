using System.Security.Cryptography;
using System.Text;

namespace IoTGateway;

public class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly byte[] _expected;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        var key = config["ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Falta la configuracion 'ApiKey'. Usa: dotnet user-secrets set ApiKey <valor> (o la variable de entorno ApiKey).");
        _expected = Encoding.UTF8.GetBytes(key);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var isPublic = path.StartsWithSegments("/swagger") || path == "/";

        if (!isPublic)
        {
            var provided = Encoding.UTF8.GetBytes(context.Request.Headers[HeaderName].ToString());
            if (!CryptographicOperations.FixedTimeEquals(provided, _expected))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ErrorDto("Falta o es invalido el header X-Api-Key"));
                return;
            }
        }

        await _next(context);
    }
}
