using IoTGateway;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<RegistroDispositivos>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "API del Gateway IoT", Version = "1.0.0" });
    o.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = MiddlewareApiKey.NombreHeader
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<MiddlewareApiKey>();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

var api = app.MapGroup("/api/dispositivos");

IResult IdInvalido() => Results.BadRequest(new ErrorDto("Id invalido (a-z, 0-9 y '-', maximo 32 caracteres)"));
IResult NoEncontrado(string id) => Results.NotFound(new ErrorDto($"El dispositivo '{id}' no existe"));

api.MapPost("/{id}/reporte", (string id, SolicitudReporte? solicitud, RegistroDispositivos registro, ILogger<Program> log) =>
{
    if (!RegistroDispositivos.EsIdValido(id)) return IdInvalido();
    if (solicitud is null) return Results.BadRequest(new ErrorDto("Body invalido"));

    var error = registro.Reportar(id, solicitud, out var pendientes);
    if (error is not null) return Results.BadRequest(new ErrorDto(error));

    log.LogInformation("Reporte de {Id}: {Cantidad} comando(s) entregado(s)", id, pendientes.Count);
    return Results.Ok(new RespuestaReporte(pendientes));
})
.WithName("reportarDispositivo")
.Produces<RespuestaReporte>()
.Produces<ErrorDto>(StatusCodes.Status400BadRequest);

api.MapGet("/", (RegistroDispositivos registro) => Results.Ok(registro.Listar()))
    .WithName("listarDispositivos")
    .Produces<List<DispositivoDto>>();

api.MapGet("/{id}", (string id, RegistroDispositivos registro) =>
    registro.Obtener(id) is { } dispositivo ? Results.Ok(dispositivo) : NoEncontrado(id))
    .WithName("obtenerDispositivo")
    .Produces<DispositivoDto>()
    .Produces<ErrorDto>(StatusCodes.Status404NotFound);

api.MapPost("/{id}/comandos", (string id, Comando? comando, RegistroDispositivos registro, ILogger<Program> log) =>
{
    if (comando is null || string.IsNullOrEmpty(comando.Destino))
        return Results.BadRequest(new ErrorDto("Body invalido: se requiere 'destino' y 'valor'"));

    var resultado = registro.EncolarComando(id, comando, out var error);
    switch (resultado)
    {
        case RegistroDispositivos.ResultadoEncolado.NoEncontrado: return NoEncontrado(id);
        case RegistroDispositivos.ResultadoEncolado.Invalido: return Results.BadRequest(new ErrorDto(error!));
        default:
            log.LogInformation("Comando encolado para {Id}: {Destino}={Valor}", id, comando.Destino, comando.Valor);
            return Results.Accepted();
    }
})
.WithName("enviarComando")
.Produces(StatusCodes.Status202Accepted)
.Produces<ErrorDto>(StatusCodes.Status400BadRequest)
.Produces<ErrorDto>(StatusCodes.Status404NotFound);

app.Run();

public partial class Program { }
