using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var application = builder.Build();

application.UseStaticFiles();
application.UseRouting();
application.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

List<WebSocket> webSockets = [];

application.Map("/ws", async httpContext =>
{
    var buffer = new byte[1024 * 4];
    var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
    webSockets.Add(webSocket);

    var receiveResult = await webSocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), CancellationToken.None);
    var i = 0;

    while (!receiveResult.CloseStatus.HasValue)
    {
        var message = Encoding.UTF8.GetBytes($"message index {i++}");
        foreach (var socket in webSockets)
        {
            await socket.SendAsync(
                buffer: new ArraySegment<byte>(message, offset: 0, count: message.Length),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);
        }

        receiveResult = await webSocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), CancellationToken.None);

        Console.WriteLine($"Received: {Encoding.UTF8.GetString(buffer[..receiveResult.Count])}");
    }

    await webSocket.CloseAsync(
        receiveResult.CloseStatus.Value,
        receiveResult.CloseStatusDescription,
        CancellationToken.None);

    webSockets.Remove(webSocket);
});

application.MapGet("/", () => "Hello World!");

application.Run();
