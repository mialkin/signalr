using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var application = builder.Build();

application.UseStaticFiles();
application.UseRouting();
application.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

var logger = LoggerFactory.Create(config => { config.AddConsole(); }).CreateLogger("Program");

List<WebSocket> webSockets = [];

application.Map("/ws", async httpContext =>
{
    logger.LogInformation("/ws endpoint called");

    var buffer = new byte[1024 * 4];
    var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
    webSockets.Add(webSocket);

    logger.LogInformation("Receiving first message");
    var receiveResult = await webSocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), CancellationToken.None);
    logger.LogInformation("Received first message");
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

        logger.LogInformation("Receiving {i}-th message", i);
        receiveResult = await webSocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), CancellationToken.None);
        logger.LogInformation("Received {i}-th message {message}", i,
            Encoding.UTF8.GetString(buffer[..receiveResult.Count]));
    }

    await webSocket.CloseAsync(
        receiveResult.CloseStatus.Value,
        receiveResult.CloseStatusDescription,
        CancellationToken.None);

    webSockets.Remove(webSocket);

    logger.LogInformation("WebSocket closed");
});

application.MapGet("/", () => "Hello World!");

application.Run();
