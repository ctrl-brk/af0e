using AF0E.Services.DxCluster.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AF0E.Services.DxCluster;

public static class DxClusterServiceCollectionExtensions
{
    public static IServiceCollection AddDxCluster(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services.Configure<DxClusterOptions>(configurationSection);
        services.TryAddSingleton<IDxClusterEventsPublisher>(_ => new NullDxClusterEventsPublisher());
        services.AddSingleton<IDxClusterService, DxClusterService>();

        return services;
    }
}
