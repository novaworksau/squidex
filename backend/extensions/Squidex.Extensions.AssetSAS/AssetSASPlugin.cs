// file:	Squidex.Extensions.AssetSAS\AssetSASPlugin.cs
//
// summary:	Implements the asset sas plugin class
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Extensions.AssetSAS.Middleware;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.Plugins;

namespace Squidex.Extensions.AssetSAS;

/// <summary>
/// An asset sas plugin.
/// </summary>
internal class AssetSASPlugin : IPlugin
{
    /// <summary>
    /// Configure services.
    /// </summary>
    /// <param name="services"> The services.</param>
    /// <param name="config"> The configuration.</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        //services.AddSingletonAs<AssetSASMiddleware>()
        //    .As<ICommandMiddleware>();

        // throw new NotImplementedException();
        services.AddSingletonAs<AssetSASCustomMiddleware>()
            .As<ICustomCommandMiddleware>();
    }
}
