using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
var application = builder.Build();

application.UseStaticFiles();
application.UseRouting();

var channel = Channel.CreateUnbounded<string>();

application.Map("/listen", async httpContext =>
{
    if (await channel.Reader.WaitToReadAsync())
    {
        if (channel.Reader.TryRead(out var data))
        {
            httpContext.Response.StatusCode = 200;
            await httpContext.Response.WriteAsync(data);
            return;
        }
    }

    httpContext.Response.StatusCode = 200;
});

application.Map("/send", async httpContext =>
{
    if (httpContext.Request.Query.TryGetValue("m", out var m))
    {
        Console.WriteLine("message to send: " + m);
        var message = m.First() ?? string.Empty;
        await channel.Writer.WriteAsync(message);
    }

    httpContext.Response.StatusCode = 200;
});

application.MapGet("/", () => "Hello World!");

application.Run();
