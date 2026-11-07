using System.Security.Cryptography;
using System.Text;

namespace IoTGateway.Middleware
{
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
                // Obtenemos el header de forma segura
                var headerValue = contexto.Request.Headers[NombreHeader].ToString() ?? string.Empty;
                var recibida = Encoding.UTF8.GetBytes(headerValue);

                // CryptographicOperations.FixedTimeEquals requiere que ambos arreglos tengan la misma longitud,
                // de lo contrario fallará si la longitud difiere. Validamos esto primero de forma segura.
                bool esValida = false;
                if (recibida.Length == _esperada.Length)
                {
                    esValida = CryptographicOperations.FixedTimeEquals(recibida, _esperada);
                }

                if (!esValida)
                {
                    contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    // Usamos un objeto anónimo para evitar depender de ErrorDto y simplificar
                    await contexto.Response.WriteAsJsonAsync(new { message = "Falta o es invalido el header X-Api-Key" });
                    return;
                }
            }

            await _siguiente(contexto);
        }
    }
}