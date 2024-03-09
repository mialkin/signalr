var builder = WebApplication.CreateBuilder(args);
var application = builder.Build();

application.MapGet("/", () => "Hello World!");

application.Run();
