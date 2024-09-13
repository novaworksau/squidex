// file:	Squidex.Extensions.AssetSAS\Models\UpsertAssetSASDto.cs
//
// summary:	Implements the upsert asset sas data transfer object class
using Microsoft.AspNetCore.Mvc;
using Squidex.Assets;
using Squidex.Extensions.AssetSAS.Middleware;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Reflection;
using Squidex.Infrastructure.Validation;
using Squidex.Web;

namespace Squidex.Extensions.AssetSAS.Models;

/// <summary>
/// An upsert asset sas data transfer object.
/// </summary>
[OpenApiRequest]
public class UpsertAssetSASDto
{
    /// <summary>
    /// True to duplicate the asset, event if the file has been uploaded.
    /// </summary>
    /// <value>
    /// True if duplicate, false if not.
    /// </value>
    [FromQuery(Name = "duplicate")]
    public bool Duplicate { get; set; }

    /// <summary>
    /// The optional custom asset id.
    /// </summary>
    /// <value>
    /// The identifier.
    /// </value>
    [FromQuery(Name = "id")]
    public DomainId Id { get; set; }

    /// <summary>
    /// The file name if the URL is specified.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    //[FromForm(Name = "name")]
    //public string Name { get; set; } = default!;

    /// <summary>
    /// The optional parent folder id.
    /// </summary>
    /// <value>
    /// The identifier of the parent.
    /// </value>
    [FromQuery(Name = "parentId")]
    public DomainId ParentId { get; set; }

    /// <summary>
    /// The alternative URL to download from.
    /// </summary>
    /// <value>
    /// The URL.
    /// </value>
    //[FromForm(Name = "url")]
    //public string Url { get; set; } = default!;

    [FromBody]
    public UpsertAssetBody Body { get; set; } = default!;


    /// <summary>
    /// Converts this object to the sas command.
    /// </summary>
    /// <exception cref="ValidationException"> Thrown when a Validation error condition occurs.</exception>
    /// <param name="id"> The optional custom asset id.</param>
    /// <param name="assetStore"> The asset store.</param>
    /// <returns>
    /// The given data converted to an UpsertAssetSASCommand.
    /// </returns>
    public UpsertAssetSASCommand ToSASCommand(DomainId id, IAssetStore assetStore)
    {
        Uri? blobUri;
        if (string.IsNullOrEmpty(Body.Url))
        {
            throw new ValidationException("asset sas URL missing");
        }
        else if (!Uri.TryCreate(Body.Url, UriKind.Absolute, out blobUri) || blobUri == null)
        {
            throw new ValidationException("asset sas URL is not valid");
        }

        if (assetStore is not AzureBlobAssetStore bassetStore)
        {
            throw new ValidationException("asset blob store is not AzureBlobStore");
        }

        var command = SimpleMapper.Map(this, new UpsertAssetSASCommand { AssetId = id, Uri = blobUri, FileName = Body.Name });
        return command;
    }
}
