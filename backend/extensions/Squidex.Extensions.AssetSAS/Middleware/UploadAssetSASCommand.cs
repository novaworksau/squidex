// file:	Squidex.Extensions.AssetSAS\Middleware\CreateAssetSASCommand.cs
//
// summary:	Implements the create asset sas command class
using Squidex.Domain.Apps.Entities.Assets.Commands;

namespace Squidex.Extensions.AssetSAS.Middleware;

/// <summary>
/// An upload asset sas command.
/// </summary>
public class UploadAssetSASCommand : AssetSASCommand
{
    /// <summary>
    /// Gets or sets URI of the document.
    /// </summary>
    /// <value>
    /// The URI.
    /// </value>
    public Uri Uri { get; set; }

    /// <summary>
    /// Converts this object to a command.
    /// </summary>
    /// <returns>
    /// This object as an UploadAssetCommand.
    /// </returns>
    public virtual UploadAssetCommand ToCommand()
    {
        return default!;

    }
}
