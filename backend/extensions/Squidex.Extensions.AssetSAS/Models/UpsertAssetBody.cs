// file:	Squidex.Extensions.AssetSAS\Models\UpsertAssetBody.cs
//
// summary:	Implements the upsert asset body class
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Squidex.Extensions.AssetSAS.Models;

/// <summary>
/// An upsert asset body.
/// </summary>
public class UpsertAssetBody
{
    /// <summary>
    /// The file name if the URL is specified.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// The alternative URL to download from.
    /// </summary>
    /// <value>
    /// The URL.
    /// </value>
    [FromForm(Name = "url")]
    public string Url { get; set; } = default!;
}
