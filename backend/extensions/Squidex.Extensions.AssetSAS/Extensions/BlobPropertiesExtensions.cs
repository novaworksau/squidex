// file:	Squidex.Extensions.AssetSAS\Extensions\BlobPropertiesExtensions.cs
//
// summary:	Implements the BLOB properties extensions class
using Azure.Storage.Blobs.Models;
using System.Text;

namespace Squidex.Extensions.AssetSAS;

/// <summary>
/// A BLOB properties extensions.
/// </summary>
internal static class BlobPropertiesExtensions
{
    /// <summary>
    /// The BlobProperties extension method that checksum hash.
    /// </summary>
    /// <param name="properties"> The properties to act on.</param>
    /// <param name="upperCase"> (Optional) True to upper case.</param>
    /// <returns>
    /// A string.
    /// </returns>
    public static string ChecksumHash(this BlobProperties properties, bool upperCase = false)
    {
        if(properties?.ContentHash == null)
        {
            return string.Empty;
        }

        var bytes = properties.ContentHash;
        StringBuilder result = new StringBuilder(bytes.Length * 2);

        for (int i = 0; i < bytes.Length; i++)
        {
            result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
        }

        return result.ToString();
    }
}
