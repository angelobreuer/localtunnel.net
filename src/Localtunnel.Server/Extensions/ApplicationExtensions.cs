namespace Localtunnel.Server;

public static class ApplicationExtensions
{
    public static IApplicationBuilder UseLocaltunnel(this WebApplication application)
    {
        var requestHandler = application.Services.GetRequiredService<TunnelRequestHandler>();
        application.Use(requestHandler.HandleRequestAsync);
        return application;
    }
}