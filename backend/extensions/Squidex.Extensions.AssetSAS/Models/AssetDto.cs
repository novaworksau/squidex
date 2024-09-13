// file:	Squidex.Extensions.AssetSAS\Models\AssetDto.cs
//
// summary:	Implements the asset data transfer object class
using System.Text.Json.Serialization;
using NodaTime;
using Squidex.Domain.Apps.Core.Assets;
using Squidex.Domain.Apps.Entities.Assets;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Reflection;
using Squidex.Web;

namespace Squidex.Extensions.AssetSAS.Models;

/// <summary>
/// An asset meta. This class cannot be inherited.
/// </summary>
public sealed class AssetMeta
{
    /// <summary>
    /// Indicates whether the asset is a duplicate.
    /// </summary>
    /// <value>
    /// The is duplicate.
    /// </value>
    public string IsDuplicate { get; set; }
}

/// <summary>
/// An asset data transfer object. This class cannot be inherited.
/// </summary>
public sealed class AssetDto : Resource
{
    /// <summary>
    /// The ID of the asset.
    /// </summary>
    /// <value>
    /// The identifier.
    /// </value>
    public DomainId Id { get; set; }

    /// <summary>
    /// The ID of the parent folder. Empty for files without parent.
    /// </summary>
    /// <value>
    /// The identifier of the parent.
    /// </value>
    public DomainId ParentId { get; set; }

    /// <summary>
    /// The file name.
    /// </summary>
    /// <value>
    /// The name of the file.
    /// </value>
    public string FileName { get; set; }

    /// <summary>
    /// The file hash.
    /// </summary>
    /// <value>
    /// The file hash.
    /// </value>
    public string? FileHash { get; set; }

    /// <summary>
    /// True, when the asset is not public.
    /// </summary>
    /// <value>
    /// True if this object is protected, false if not.
    /// </value>
    public bool IsProtected { get; set; }

    /// <summary>
    /// The slug.
    /// </summary>
    /// <value>
    /// The slug.
    /// </value>
    public string Slug { get; set; }

    /// <summary>
    /// The mime type.
    /// </summary>
    /// <value>
    /// The type of the mime.
    /// </value>
    public string MimeType { get; set; }

    /// <summary>
    /// The file type.
    /// </summary>
    /// <value>
    /// The type of the file.
    /// </value>
    public string FileType { get; set; }

    /// <summary>
    /// The formatted text representation of the metadata.
    /// </summary>
    /// <value>
    /// The metadata text.
    /// </value>
    public string MetadataText { get; set; }

    /// <summary>
    /// The UI token.
    /// </summary>
    /// <value>
    /// The edit token.
    /// </value>
    public string? EditToken { get; set; }

    /// <summary>
    /// The asset metadata.
    /// </summary>
    /// <value>
    /// The metadata.
    /// </value>
    public AssetMetadata Metadata { get; set; }

    /// <summary>
    /// The asset tags.
    /// </summary>
    /// <value>
    /// The tags.
    /// </value>
    public HashSet<string>? Tags { get; set; }

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    /// <value>
    /// The size of the file.
    /// </value>
    public long FileSize { get; set; }

    /// <summary>
    /// The version of the file.
    /// </summary>
    /// <value>
    /// The file version.
    /// </value>
    public long FileVersion { get; set; }

    /// <summary>
    /// The type of the asset.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public AssetType Type { get; set; }

    /// <summary>
    /// The user that has created the schema.
    /// </summary>
    /// <value>
    /// Amount to created by.
    /// </value>
    public RefToken CreatedBy { get; set; }

    /// <summary>
    /// The user that has updated the asset.
    /// </summary>
    /// <value>
    /// Amount to last modified by.
    /// </value>
    public RefToken LastModifiedBy { get; set; }

    /// <summary>
    /// The date and time when the asset has been created.
    /// </summary>
    /// <value>
    /// The created.
    /// </value>
    public Instant Created { get; set; }

    /// <summary>
    /// The date and time when the asset has been modified last.
    /// </summary>
    /// <value>
    /// The last modified.
    /// </value>
    public Instant LastModified { get; set; }

    /// <summary>
    /// The version of the asset.
    /// </summary>
    /// <value>
    /// The version.
    /// </value>
    public long Version { get; set; }

    /// <summary>
    /// The metadata.
    /// </summary>
    /// <value>
    /// The meta.
    /// </value>
    [JsonPropertyName("_meta")]
    public AssetMeta? Meta { get; set; }

    /// <summary>
    /// Determines of the created file is an image.
    /// </summary>
    /// <value>
    /// True if this object is image, false if not.
    /// </value>
    [Obsolete("Use 'type' field now.")]
    public bool IsImage
    {
        get => Type == AssetType.Image;
    }

    /// <summary>
    /// The width of the image in pixels if the asset is an image.
    /// </summary>
    /// <value>
    /// The width of the pixel.
    /// </value>
    [Obsolete("Use 'metadata' field now.")]
    public int? PixelWidth
    {
        get => Metadata.GetInt32(KnownMetadataKeys.PixelWidth);
    }

    /// <summary>
    /// The height of the image in pixels if the asset is an image.
    /// </summary>
    /// <value>
    /// The height of the pixel.
    /// </value>
    [Obsolete("Use 'metadata' field now.")]
    public int? PixelHeight
    {
        get => Metadata.GetInt32(KnownMetadataKeys.PixelHeight);
    }

    /// <summary>
    /// From domain.
    /// </summary>
    /// <param name="asset"> The asset.</param>
    /// <param name="resources"> The resources.</param>
    /// <param name="isDuplicate"> (Optional) True if is duplicate, false if not.</param>
    /// <returns>
    /// An AssetDto.
    /// </returns>
    public static AssetDto FromDomain(EnrichedAsset asset, Resources resources, bool isDuplicate = false)
    {
        var result = SimpleMapper.Map(asset, new AssetDto());

        result.Tags = asset.TagNames;

        if (isDuplicate)
        {
            result.Meta = new AssetMeta
            {
                IsDuplicate = "true"
            };
        }

        result.FileType = asset.FileName.FileType();

        return result.CreateLinks(resources);
    }

    /// <summary>
    /// Creates the links.
    /// </summary>
    /// <param name="resources"> The resources.</param>
    /// <returns>
    /// The new links.
    /// </returns>
    private AssetDto CreateLinks(Resources resources)
    {
        var app = resources.App;

        var values = new { app, id = Id };

        AddSelfLink(resources.Url("GetAsset", values: values));

        if (resources.CanUpdateAsset)
        {
            AddPutLink("update", resources.Url("PutAsset", values: values));
        }

        if (resources.CanUpdateAsset)
        {
            AddPutLink("move", resources.Url("PutAssetParent", values: values));
        }

        if (resources.CanUploadAsset)
        {
            AddPutLink("upload", resources.Url("PutAssetContent", values: values));
        }

        if (resources.CanDeleteAsset)
        {
            AddDeleteLink("delete", resources.Url("DeleteAsset", values: values));
        }

        if (!string.IsNullOrWhiteSpace(Slug))
        {
            var idValues = new { app, idOrSlug = Id, more = Slug };

            AddGetLink("content", resources.Url("GetAssetContentBySlug", contoller: "AssetContent", values: idValues));

            var slugValues = new { app, idOrSlug = Slug };

            AddGetLink("content/slug", resources.Url("GetAssetContentBySlug", contoller: "AssetContent", values: slugValues));
        }
        else
        {
            var idValues = new { app, idOrSlug = Id };

            AddGetLink("content", resources.Url("GetAssetContentBySlug", contoller: "AssetContent", values: idValues));
        }

        return this;
    }
}
