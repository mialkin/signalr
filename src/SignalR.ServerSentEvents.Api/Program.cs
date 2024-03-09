using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

var application = builder.Build();

var channel = Channel.CreateUnbounded<string>();

application.UseStaticFiles();
application.UseRouting();

application.Map("/send", async ctx =>
{
    if (ctx.Request.Query.TryGetValue("m", out var stringValues))
    {
        var message = stringValues.First() ?? string.Empty;

        Console.WriteLine("Message to send: " + message);
        await channel.Writer.WriteAsync(message);
    }

    ctx.Response.StatusCode = 200;
});

application.Use(async (httpContext, next) =>
{
    if (httpContext.Request.Path.ToString().Equals("/sse"))
    {
        var response = httpContext.Response;
        response.Headers.Append("Content-Type", "text/event-stream");

        await response.WriteAsync("event: custom\r");
        await response.WriteAsync("data: custom event data\r\r");
        await response.Body.FlushAsync();

        while (await channel.Reader.WaitToReadAsync())
        {
            var message = await channel.Reader.ReadAsync();
            Console.WriteLine("Sending message: " + message);
            await response.WriteAsync($"data: {message}\r\r");
            await response.Body.FlushAsync();
        }
    }

    await next();
});

application.MapGet("/", () => "Hello World!");

application.Run();
