using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Squidex.Assets;
using Squidex.Domain.Apps.Core.Apps;
using Squidex.Domain.Apps.Entities.Assets.Commands;
using Squidex.Extensions.AssetSAS.Middleware;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Reflection;
using Squidex.Infrastructure.Validation;
using Squidex.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Squidex.Extensions.AssetSAS.Models;

[OpenApiRequest]
public class CreateAssetSASDto
{
    /// <summary>
    /// True to duplicate the asset, event if the file has been uploaded.
    /// </summary>
    [FromQuery(Name = "duplicate")]
    public bool Duplicate { get; set; }

    /// <summary>
    /// The optional custom asset id.
    /// </summary>
    [FromQuery(Name = "id")]
    public DomainId? Id { get; set; }

    /// <summary>
    /// The file name if the URL is specified.
    /// </summary>
    //[FromForm(Name = "name")]
    //public string Name { get; set; } = default!;

    /// <summary>
    /// The optional parent folder id.
    /// </summary>
     [FromQuery(Name = "parentId")]
    public DomainId ParentId { get; set; }

    /// <summary>
    /// The alternative URL to download from.
    /// </summary>
    //[FromForm(Name = "url")]
    //public string Url { get; set; } = default!;

    [FromBody]
    public UpsertAssetBody Body { get; set; } = default!;

    public CreateAssetSASCommand ToSASCommand(IAssetStore assetStore)
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

        var command = SimpleMapper.Map(this, new CreateAssetSASCommand { Uri = blobUri, FileName = Body.Name });
        return command;
    }
}
