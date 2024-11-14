// file:	Squidex.Extensions.AzureServiceBus\AzureServiceBusPlugin.cs
//
// summary:	Implements the azure service bus plugin class
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Infrastructure.Plugins;

namespace Squidex.Extensions.AzureServiceBus;

/// <summary>
/// An azure service bus plugin. This class cannot be inherited.
/// </summary>
public sealed class AzureServiceBusPlugin : IPlugin
{
    /// <summary>
    /// Configure services.
    /// </summary>
    /// <param name="services"> The services.</param>
    /// <param name="config"> The configuration.</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddRuleAction<AzureServiceBusAction, AzureServiceBusActionHandler>();
    }
}
