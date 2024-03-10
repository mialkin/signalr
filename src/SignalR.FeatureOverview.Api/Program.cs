using SignalR.FeatureOverview.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var application = builder.Build();

application.UseStaticFiles();
application.UseRouting();

application.MapGet("/", () => "Hello World!");

application.MapHub<ChatHub>("/chatHub");

application.Run();
