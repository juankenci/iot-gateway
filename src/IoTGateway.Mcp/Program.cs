using IoTGateway.Mcp;
using IoTGateway.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// MCP uses stdout for protocol messages. Logs must go to stderr.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var gatewayUrl = builder.Configuration["Gateway:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("GATEWAY_URL")
    ?? "http://localhost:5000";
var gatewayApiKey = builder.Configuration["Gateway:ApiKey"]
    ?? builder.Configuration["ApiKey"]
    ?? Environment.GetEnvironmentVariable("GATEWAY_API_KEY");

if (string.IsNullOrWhiteSpace(gatewayApiKey))
{
    throw new InvalidOperationException(
        "Falta Gateway:ApiKey. Configúrala con User Secrets o la variable GATEWAY_API_KEY.");
}

builder.Services.AddHttpClient<GatewayClient>(client =>
{
    client.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("X-Api-Key", gatewayApiKey);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<IoTTools>();

await builder.Build().RunAsync();
