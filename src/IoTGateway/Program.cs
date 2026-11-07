using IoTGateway.Middleware;
using IoTGateway.Repositories;
using IoTGateway.Services;

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
app.UseMiddleware<MiddlewareApiKey>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
