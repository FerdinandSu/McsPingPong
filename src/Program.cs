using Microsoft.AspNetCore.Mvc;
using Steeltoe.Extensions.Configuration.Placeholder;
using System.Net;

ServicePointManager.DnsRefreshTimeout = 0; // Force DNS Refresh
var builder = WebApplication.CreateBuilder(args);


builder.Configuration.AddPlaceholderResolver();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet
    (
        "/",
        async
        (
            [FromQuery] int ttl,
            [FromServices] HttpClient http,
            [FromServices] IConfiguration config,
            [FromServices] ILogger<Program> logger,
            HttpContext context
        ) =>
        {
            if (ttl <= 0) return Results.BadRequest();
            var appUrl = config["AppUrl"];
            var insId = config["InstanceId"] ?? "<Id-Not-Set>";
            var localIp = context.Connection.LocalIpAddress?.ToString() ?? "<UNKNOWN>";
            var thisNode = new NodeInfo(insId, localIp, Dns.GetHostName());
            NodeInfo[]? result;
            if (ttl == 1)
            {
                result = [thisNode];
            }
            else
            {
                var target=$"{appUrl}?ttl={ttl - 1}";
                logger.LogInformation($"Requsting: {target}");
                var response = await http.GetFromJsonAsync<NodeInfo[]?>(target);
                result = response is null ? null : [thisNode, .. response];
            }

            return Results.Ok(result);
        }
    )
   .WithName("Ping")
   .WithOpenApi();


app.Run();

public record NodeInfo(string Id, string Ip, string Hostname);