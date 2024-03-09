using System.Net.WebSockets;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.WriteTo.Console();
});

var application = builder.Build();

application.UseStaticFiles();
application.UseRouting();
application.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

List<WebSocket> webSockets = [];

application.Map("/ws", async httpContext =>
{
    Log.Information("/ws endpoint called");

    var buffer = new byte[1024 * 4];
    var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
    webSockets.Add(webSocket);

    Log.Information("Receiving first message");
    var receiveResult = await webSocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), CancellationToken.None);
    Log.Information("Received first message: {message}", Encoding.UTF8.GetString(buffer[..receiveResult.Count]));
    var i = 1;

    while (!receiveResult.CloseStatus.HasValue)
    {
        var message = Encoding.UTF8.GetBytes($"Message index {i++}");
        foreach (var socket in webSockets)
        {
            await socket.SendAsync(
                buffer: new ArraySegment<byte>(message, offset: 0, count: message.Length),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);
        }

        Log.Information("Receiving {i}-th message", i);
        receiveResult = await webSocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), CancellationToken.None);
        Log.Information("Received {i}-th message {message}", i,
            Encoding.UTF8.GetString(buffer[..receiveResult.Count]));
    }

    await webSocket.CloseAsync(
        receiveResult.CloseStatus.Value,
        receiveResult.CloseStatusDescription,
        CancellationToken.None);

    webSockets.Remove(webSocket);

    Log.Information("WebSocket closed");
});

application.MapGet("/", () => "Hello World!");

application.Run();
