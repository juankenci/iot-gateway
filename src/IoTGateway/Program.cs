using IoTGateway;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "IoT Gateway API", Version = "1.0.0" });
    o.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = ApiKeyMiddleware.HeaderName
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

var api = app.MapGroup("/api/devices");

IResult BadId() => Results.BadRequest(new ErrorDto("Id invalido (a-z, 0-9 y '-', maximo 32 caracteres)"));
IResult NotFound(string id) => Results.NotFound(new ErrorDto($"El dispositivo '{id}' no existe"));

api.MapPost("/{id}/report", (string id, ReportRequest? request, DeviceRegistry registry, ILogger<Program> log) =>
{
    if (!DeviceRegistry.IsValidId(id)) return BadId();
    if (request is null) return Results.BadRequest(new ErrorDto("Body invalido"));

    var error = registry.Report(id, request, out var pending);
    if (error is not null) return Results.BadRequest(new ErrorDto(error));

    log.LogInformation("Report de {Id}: {Count} comando(s) entregado(s)", id, pending.Count);
    return Results.Ok(new ReportResponse(pending));
})
.WithName("reportDevice")
.Produces<ReportResponse>()
.Produces<ErrorDto>(StatusCodes.Status400BadRequest);

api.MapGet("/", (DeviceRegistry registry) => Results.Ok(registry.List()))
    .WithName("listDevices")
    .Produces<List<DeviceDto>>();

api.MapGet("/{id}", (string id, DeviceRegistry registry) =>
    registry.Get(id) is { } device ? Results.Ok(device) : NotFound(id))
    .WithName("getDevice")
    .Produces<DeviceDto>()
    .Produces<ErrorDto>(StatusCodes.Status404NotFound);

api.MapPost("/{id}/commands", (string id, Command? command, DeviceRegistry registry, ILogger<Program> log) =>
{
    if (command is null || string.IsNullOrEmpty(command.Target))
        return Results.BadRequest(new ErrorDto("Body invalido: se requiere 'target' y 'value'"));

    var result = registry.Enqueue(id, command, out var error);
    switch (result)
    {
        case DeviceRegistry.EnqueueResult.NotFound: return NotFound(id);
        case DeviceRegistry.EnqueueResult.Invalid: return Results.BadRequest(new ErrorDto(error!));
        default:
            log.LogInformation("Comando encolado para {Id}: {Target}={Value}", id, command.Target, command.Value);
            return Results.Accepted();
    }
})
.WithName("sendCommand")
.Produces(StatusCodes.Status202Accepted)
.Produces<ErrorDto>(StatusCodes.Status400BadRequest)
.Produces<ErrorDto>(StatusCodes.Status404NotFound);

app.Run();

public partial class Program { }
