// file:	Squidex.Extensions.AssetSAS\Middleware\CreateAssetSASCommand.cs
//
// summary:	Implements the create asset sas command class
using Squidex.Domain.Apps.Core.Assets;
using Squidex.Domain.Apps.Entities;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;

namespace Squidex.Extensions.AssetSAS.Middleware;

/// <summary>
/// An asset sas command.
/// </summary>
public class AssetSASCommand : SquidexCommand, IAppCommand, IAggregateCommand
{
    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    /// <value>
    /// The tags.
    /// </value>
    public HashSet<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    /// <value>
    /// The size of the file.
    /// </value>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the type of the mime.
    /// </summary>
    /// <value>
    /// The type of the mime.
    /// </value>
    public string? MimeType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Gets the metadata.
    /// </summary>
    /// <value>
    /// The metadata.
    /// </value>
    public AssetMetadata Metadata { get; } = [];

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public AssetType Type { get; set; }

    /// <summary>
    /// Gets or sets the file hash.
    /// </summary>
    /// <value>
    /// The file hash.
    /// </value>
    public string FileHash { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the file.
    /// </summary>
    /// <value>
    /// The identifier of the file.
    /// </value>
    public string FileId { get; set; }

    /// <summary>
    /// Gets or sets the filename of the file.
    /// </summary>
    /// <value>
    /// The name of the file.
    /// </value>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the asset.
    /// </summary>
    /// <value>
    /// The identifier of the asset.
    /// </value>
    public DomainId AssetId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the do not script.
    /// </summary>
    /// <value>
    /// True if do not script, false if not.
    /// </value>
    public bool DoNotScript { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the application.
    /// </summary>
    /// <value>
    /// The identifier of the application.
    /// </value>
    public NamedId<DomainId> AppId { get; set; }

    /// <summary>
    /// Gets the identifier of the aggregate.
    /// </summary>
    /// <value>
    /// The identifier of the aggregate.
    /// </value>
    public DomainId AggregateId
    {
        get => DomainId.Combine(AppId, AssetId);
    }
}
