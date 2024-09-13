// file:	Squidex.Extensions.AssetSAS\Models\SASFile.cs
//
// summary:	Implements the sas file class
using Squidex.Assets;

namespace Squidex.Extensions.AssetSAS.Models;

/// <summary>
/// The sas file.
/// </summary>
internal class SASFile : IAssetFile
{
    /// <summary>
    /// Gets or sets the filename of the file.
    /// </summary>
    /// <value>
    /// The name of the file.
    /// </value>
    public string FileName { get; set; }

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
    public string MimeType { get; set; }

    public string Hash { get; set; }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources asynchronously.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dispose operation.
    /// </returns>
    public ValueTask DisposeAsync()
    {
        return new();
    }

    /// <summary>
    /// Opens the read.
    /// </summary>
    /// <returns>
    /// A Stream.
    /// </returns>
    public Stream OpenRead()
    {
        throw new NotImplementedException();
    }
}
