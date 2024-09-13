// file:	Squidex.Extensions.AssetSAS\Middleware\CreateAssetSASCommand.cs
//
// summary:	Implements the create asset sas command class
using Squidex.Assets;
using Squidex.Domain.Apps.Core.Apps;
using Squidex.Domain.Apps.Entities.Assets.Commands;
using Squidex.Extensions.AssetSAS.Models;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Reflection;

namespace Squidex.Extensions.AssetSAS.Middleware;

/// <summary>
/// A create asset sas command.
/// </summary>
public class CreateAssetSASCommand : UploadAssetSASCommand
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public CreateAssetSASCommand()
    {
        AssetId = DomainId.NewGuid();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the duplicate.
    /// </summary>
    /// <value>
    /// True if duplicate, false if not.
    /// </value>
    public bool Duplicate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the optimize validation.
    /// </summary>
    /// <value>
    /// True if optimize validation, false if not.
    /// </value>
    public bool OptimizeValidation { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent.
    /// </summary>
    /// <value>
    /// The identifier of the parent.
    /// </value>
    public DomainId ParentId { get; set; }

    /// <summary>
    /// Converts this object to a command.
    /// </summary>
    /// <returns>
    /// This object as an UploadAssetCommand.
    /// </returns>
    public override UploadAssetCommand ToCommand()
    {
        var file = new SASFile
        {
            FileName = FileName,
            Hash = FileHash,
            FileSize = FileSize,
            MimeType = MimeType ?? "application/octet-stream"
        };

        var command = SimpleMapper.Map(this, new CreateAsset { File = file, FileHash = FileHash, AppId = AppId });
        return command;
    }
}
