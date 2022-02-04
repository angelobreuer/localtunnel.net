using Localtunnel.Server;

var builder = WebApplication.CreateBuilder();

builder.Services.AddLocaltunnel(builder.Configuration);

var application = builder.Build();

application.UseLocaltunnel();
application.Run();