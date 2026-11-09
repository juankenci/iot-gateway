using IoTGateway.Middleware;
using IoTGateway.Repositories;
using IoTGateway.Services;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Inyección de Dependencias (Arquitectura por Capas)
builder.Services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<ICommandService, CommandService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandler(a => a.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode = ex switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,
        ArgumentException => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
    var mensaje = ex is KeyNotFoundException or ArgumentException ? ex.Message : "Error interno";
    await ctx.Response.WriteAsJsonAsync(new { message = mensaje });
}));

app.UseMiddleware<MiddlewareApiKey>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
