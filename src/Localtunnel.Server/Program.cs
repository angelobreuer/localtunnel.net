using Localtunnel.Server;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.WebHost
    .ConfigureLogging(x => x
    .AddConsole(x => x.FormatterName = ConsoleFormatterNames.Simple));

builder.Services.AddLocaltunnel(builder.Configuration);

var application = builder.Build();

application.UseLocaltunnel();
application.Run();