// file:	Squidex.Extensions.AssetSAS\Extensions\ResourcesExtensions.cs
//
// summary:	Implements the resources extensions class
using Microsoft.AspNetCore.Mvc;
using Squidex.Web;

namespace Squidex.Extensions.AssetSAS;

/// <summary>
/// The resources extensions.
/// </summary>
internal static class ResourcesExtensions
{
    /// <summary>
    /// The Resources extension method that urls.
    /// </summary>
    /// <param name="resource"> The resource to act on.</param>
    /// <param name="action"> The action.</param>
    /// <param name="contoller"> (Optional) Name of the contoller.</param>
    /// <param name="values"> (Optional) The values.</param>
    /// <returns>
    /// A string?
    /// </returns>
    public static string Url(this Resources resource, string action, string contoller = "Assets", object? values = null)
    {
        return resource.Controller.Url.Action(action, contoller, values)!;
    }
}
