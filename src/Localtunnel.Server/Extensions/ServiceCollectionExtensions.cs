namespace Localtunnel.Server;

public static class ServiceCollectionExtensions
{
    public static void AddLocaltunnel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BasicTunnelDnsProviderOptions>(configuration.GetSection("Dns"));
        services.Configure<PortAllocationServiceOptions>(configuration.GetSection("PortAllocation"));

        services.AddSingleton<TunnelRequestHandler>();

        services.AddSingleton<ITunnelService, TunnelService>();
        services.AddSingleton<IPortAllocationService, PortAllocationService>();
        services.AddSingleton<ITunnelDnsProvider, BasicTunnelDnsProvider>();
    }
}