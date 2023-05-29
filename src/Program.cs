
using Microsoft.AspNetCore.Mvc;
using Steeltoe.Extensions.Configuration.Placeholder;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);



builder.Configuration.AddPlaceholderResolver();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/ping", async (
    [FromBody] PingMessage message,
    [FromServices] HttpClient http,
    [FromServices] IConfiguration config,
    HttpContext context) =>
{
    if (message.Ttl <= 0) return Results.BadRequest();
    var appUrl = config["AppUrl"];
    var insId = config["InstanceId"] ?? "<Id-Not-Set>";
    var localIp = context.Connection.LocalIpAddress?.ToString() ?? "<UNKNOWN>";
    var thisNode = new NodeInfo(insId, localIp);
    
    if (message.Ttl == 1)
    {
        return Results.Ok(
            new PongMessage(message.Count + 1, message.Initial, message.Sender, thisNode));
    }
    else
    {
        var response = await http.PostAsJsonAsync(appUrl,
            new PingMessage(message.Ttl - 1, message.Count + 1, message.Initial, thisNode))
            ;
        return Results.Ok(await response.Content.ReadFromJsonAsync<PongMessage>());
    }
}).WithName("Ping").WithOpenApi();


app.MapPost("/batch", async (
    [FromBody] BatchRequest request,
    [FromServices] IConfiguration config,
    HttpContext context) =>
{
    var appUrl = config["AppUrl"];
    var insId = config["InstanceId"] ?? "<Id-Not-Set>";
    var localIp = context.Connection.LocalIpAddress?.ToString() ?? "<UNKNOWN>";
    var thisNode = new NodeInfo(insId, localIp);
    var bag = new List<BatchResponseDetail>();
    var ping = new PingMessage(request.Ttl, 0, thisNode, thisNode);
    for (int i = 0; i < request.BatchSize; i++)
    {
        var start = DateTime.Now;
        var response = await new HttpClient().PostAsJsonAsync(appUrl, ping);
        var end = DateTime.Now;
        var pong = await response.Content.ReadFromJsonAsync<PongMessage>();
        bag.Add(new(pong!.ThisNode, (long)((end - start).TotalMilliseconds)));
    }

    var total = bag.Count;
    return new BatchResponse(
    bag.GroupBy(d => d.FinalNode)
        .ToDictionary(k => $"[{k.Key.Id},{k.Key.Ip}]",
            k => new BatchResponseStats(k.Count(), (double)k.Count() / total)), bag);

}).WithName("Batch").WithOpenApi();

app.Run();


public record NodeInfo(string Id, string Ip);
public record PingMessage(int Ttl, int Count, NodeInfo Initial, NodeInfo Sender);
public record PongMessage(int Count, NodeInfo Initial, NodeInfo LastNode, NodeInfo ThisNode);
public record BatchRequest(int Ttl, int BatchSize);
public record BatchResponseStats(int Count, double Ratio);
public record BatchResponseDetail(NodeInfo FinalNode, long Time);
public record BatchResponse(Dictionary<string, BatchResponseStats> Stats, List<BatchResponseDetail> Details);