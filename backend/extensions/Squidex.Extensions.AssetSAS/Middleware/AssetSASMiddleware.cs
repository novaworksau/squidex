// file:	Squidex.Extensions.AssetSAS\Middleware\AssetSASMiddleware.cs
//
// summary:	Implements the asset sas middleware class
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidex.Assets;
using Squidex.Domain.Apps.Core.Assets;
using Squidex.Domain.Apps.Entities;
using Squidex.Domain.Apps.Entities.Assets;
using Squidex.Domain.Apps.Entities.Assets.DomainObject;
using Squidex.Domain.Apps.Entities.Assets.Queries;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.Validation;
using System.Reflection;

namespace Squidex.Extensions.AssetSAS.Middleware;

/// <summary>
/// An asset sas custom middleware.
/// </summary>
public class AssetSASCustomMiddleware : ICustomCommandMiddleware
{
    /// <summary>
    /// (Immutable) the BLOB container field.
    /// </summary>
    private static readonly FieldInfo? _blobContainerField = typeof(AzureBlobAssetStore).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.FieldType == typeof(BlobContainerClient));

    /// <summary>
    /// (Immutable) the no overwrite copy.
    /// </summary>
    private static readonly BlobCopyFromUriOptions NoOverwriteCopy = new BlobCopyFromUriOptions
    {
        DestinationConditions = new BlobRequestConditions
        {
            IfNoneMatch = new ETag("*")
        }
    };

    /// <summary>
    /// (Immutable) the asset enricher.
    /// </summary>
    private readonly IAssetEnricher _assetEnricher;

    /// <summary>
    /// (Immutable) the asset file store.
    /// </summary>
    private readonly IAssetFileStore _assetFileStore;

    /// <summary>
    /// (Immutable) the asset metadata sources.
    /// </summary>
    private readonly IEnumerable<IAssetMetadataSource> _assetMetadataSources;

    /// <summary>
    /// (Immutable) the asset query.
    /// </summary>
    private readonly IAssetQueryService _assetQuery;

    /// <summary>
    /// (Immutable) the asset store.
    /// </summary>
    private readonly IAssetStore _assetStore;

    /// <summary>
    /// (Immutable) the context provider.
    /// </summary>
    private readonly IContextProvider _contextProvider;

    /// <summary>
    /// (Immutable) the domain object cache.
    /// </summary>
    private readonly IDomainObjectCache _domainObjectCache;

    /// <summary>
    /// (Immutable) the domain object factory.
    /// </summary>
    private readonly IDomainObjectFactory _domainObjectFactory;

    /// <summary>
    /// (Immutable) the service provider.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="domainObjectFactory"> The domain object factory.</param>
    /// <param name="domainObjectCache"> The domain object cache.</param>
    /// <param name="assetFileStore"> The asset file store.</param>
    /// <param name="assetQuery"> The asset query.</param>
    /// <param name="contextProvider"> The context provider.</param>
    /// <param name="assetMetadataSources"> The asset metadata sources.</param>
    /// <param name="serviceProvider"> The service provider.</param>
    /// <param name="assetStore"> The asset store.</param>
    /// <param name="assetEnricher"> The asset enricher.</param>
    public AssetSASCustomMiddleware(IDomainObjectFactory domainObjectFactory,
        IDomainObjectCache domainObjectCache,
        IAssetFileStore assetFileStore,
        IAssetQueryService assetQuery,
        IContextProvider contextProvider,
        IEnumerable<IAssetMetadataSource> assetMetadataSources,
        IServiceProvider serviceProvider,
        IAssetStore assetStore,
        IAssetEnricher assetEnricher)
    {
        _assetFileStore = assetFileStore;
        _assetQuery = assetQuery;
        _contextProvider = contextProvider;
        _assetMetadataSources = assetMetadataSources;
        _serviceProvider = serviceProvider;
        _assetStore = assetStore;
        _domainObjectFactory = domainObjectFactory;
        _domainObjectCache = domainObjectCache;
        _assetEnricher = assetEnricher;
    }

    /// <summary>
    /// Handles the asynchronous.
    /// </summary>
    /// <param name="context"> The context.</param>
    /// <param name="next"> The next.</param>
    /// <param name="ct"> A token that allows processing to be cancelled.</param>
    /// <returns>
    /// A Task.
    /// </returns>
    public async Task HandleAsync(CommandContext context, NextDelegate next, CancellationToken ct)
    {
        switch (context.Command)
        {
            case CreateAssetSASCommand create:
                await UploadWithDuplicateCheckAsync(context, create, create.Duplicate, next, ct);
                break;

            case UpsertAssetSASCommand upsert:
                await UploadWithDuplicateCheckAsync(context, upsert, upsert.Duplicate, next, ct);
                break;

            default:
                await next(context, ct);
                break;
        }
    }

    /// <summary>
    /// Calculates the hash.
    /// </summary>
    /// <param name="file"> The file.</param>
    /// <param name="blobProperties"> The BLOB properties.</param>
    /// <returns>
    /// The calculated hash.
    /// </returns>
    private static string ComputeHash(UploadAssetSASCommand file, BlobProperties blobProperties)
    {
        var initHash = blobProperties.ChecksumHash();
        return $"{initHash}{file.FileName}{blobProperties.ContentLength}".ToSha256Base64();
    }

    /// <summary>
    /// Enrich result asynchronous.
    /// </summary>
    /// <param name="context"> The context.</param>
    /// <param name="result"> The result.</param>
    /// <param name="ct"> A token that allows processing to be cancelled.</param>
    /// <returns>
    /// The enrich result.
    /// </returns>
    private async Task<object> EnrichResultAsync(CommandContext context, CommandResult result, CancellationToken ct)
    {
        var payload = result.Payload;
        if (payload is not Asset asset)
        {
            return payload;
        }

        if (result.IsChanged && context.Command is UploadAssetSASCommand)
        {
            var tempFile = context.ContextId.ToString();
            try
            {
                await _assetFileStore.CopyAsync(tempFile, asset.AppId.Id, asset.Id, asset.FileVersion, null, ct);
            }
            catch (AssetAlreadyExistsException)
            {
            }
        }

        if (payload is not EnrichedAsset)
        {
            payload = await _assetEnricher.EnrichAsync(asset, _contextProvider.Context, ct);
        }

        return payload;
    }

    /// <summary>
    /// Enrich with hash and upload asynchronous.
    /// </summary>
    /// <exception cref="ValidationException"> Thrown when a Validation error condition occurs.</exception>
    /// <exception cref="AssetStoreException"> Thrown when an Asset Store error condition occurs.</exception>
    /// <exception cref="AssetAlreadyExistsException"> Thrown when an Asset Already Exists error
    /// condition occurs.</exception>
    /// <exception cref="AssetNotFoundException"> Thrown when an Asset Not Found error condition occurs.</exception>
    /// <param name="command"> The command.</param>
    /// <param name="tempFile"> The temporary file.</param>
    /// <param name="ct"> A token that allows processing to be cancelled.</param>
    /// <returns>
    /// A Task.
    /// </returns>
    private async Task EnrichWithHashAndUploadAsync(UploadAssetSASCommand command, string tempFile, CancellationToken ct)
    {
        if (_assetStore is not AzureBlobAssetStore bassetStore)
        {
            throw new ValidationException("asset blob store is not AzureBlobStore");
        }

        var blobContainer = await GetContainerClient(bassetStore, ct);
        BlobProperties blobProperties;
        try
        {
            var blobTarget = blobContainer.GetBlobClient(tempFile);
            await blobTarget.StartCopyFromUriAsync(command.Uri, NoOverwriteCopy, ct);

            do
            {
                blobProperties = await blobTarget.GetPropertiesAsync(cancellationToken: ct);

                await Task.Delay(50, ct);
            }
            while (blobProperties.CopyStatus == CopyStatus.Pending);

            if (blobProperties.CopyStatus != CopyStatus.Success)
            {
                throw new AssetStoreException($"Copy of temporary file failed: {blobProperties.CopyStatus}");
            }

            if (blobProperties.ContentHash == null)
            {
                blobProperties = await blobTarget.GetPropertiesAsync(cancellationToken: ct);
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            throw new AssetAlreadyExistsException(tempFile, ex);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new AssetNotFoundException(command.Uri.ToString(), ex);
        }

        command.MimeType = GetMimeTypeMap(command, blobProperties);
        command.FileSize = blobProperties.ContentLength;
        command.FileHash = ComputeHash(command, blobProperties);
    }

    /// <summary>
    /// Enrich with metadata asynchronous.
    /// </summary>
    /// <param name="command"> The command.</param>
    /// <param name="ct"> A token that allows processing to be cancelled.</param>
    /// <returns>
    /// A Task.
    /// </returns>
    private Task EnrichWithMetadataAsync(UploadAssetSASCommand command, CancellationToken ct)
    {
        if (command.Tags == null)
        {
            command.Tags = [];
        }
        var extension = command.FileName.FileType();
        if (!string.IsNullOrWhiteSpace(extension))
        {
            command.Tags.Add($"type/{extension.ToLowerInvariant()}");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets container client.
    /// </summary>
    /// <param name="abs"> The abs.</param>
    /// <param name="ct"> A token that allows processing to be cancelled.</param>
    /// <returns>
    /// The container client.
    /// </returns>
    private async Task<BlobContainerClient> GetContainerClient(AzureBlobAssetStore abs, CancellationToken ct)
    {
        var blobContainer = TryGetFromPrivateField(abs);
        if (blobContainer != null)
        {
            return blobContainer;
        }
        var options = _serviceProvider.GetRequiredService<IOptions<AzureBlobAssetOptions>>().Value;

        var blobServiceClient = new BlobServiceClient(options.ConnectionString);

        blobContainer = blobServiceClient.GetBlobContainerClient(options.ContainerName);

        await blobContainer.CreateIfNotExistsAsync(cancellationToken: ct);

        return blobContainer;
    }

    /// <summary>
    /// Gets mime type map.
    /// </summary>
    /// <param name="command"> The command.</param>
    /// <param name="blobProperties"> The BLOB properties.</param>
    /// <returns>
    /// The mime type map.
    /// </returns>
    private string GetMimeTypeMap(UploadAssetSASCommand command, BlobProperties blobProperties)
    {
        var mimeType = (blobProperties.ContentType ?? "application/octet-stream").ToLowerInvariant();

        if (mimeType.Equals("application/octet-stream"))
        {
            var extension = command.FileName.FileType();
            if (!string.IsNullOrWhiteSpace(extension) && MimeTypes.TryGetMimeType(command.FileName, out var mt))
            {
                mimeType = mt;
            }
        }
        return mimeType;
    }

    /// <summary>
    /// Attempts to get from private field, returning a default value rather than throwing an
    /// exception if it fails.
    /// </summary>
    /// <param name="abs"> The abs.</param>
    /// <returns>
    /// A BlobContainerClient?
    /// </returns>
    private BlobContainerClient? TryGetFromPrivateField(AzureBlobAssetStore abs)
    {
        if (_blobContainerField is not null)
        {
            return _blobContainerField.GetValue(abs) as BlobContainerClient;
        }
        return null;
    }

    /// <summary>
    /// Uploads a with duplicate check asynchronous.
    /// </summary>
    /// <param name="context"> The context.</param>
    /// <param name="command"> The command.</param>
    /// <param name="duplicate"> True to duplicate.</param>
    /// <param name="next"> The next.</param>
    /// <param name="ct"> A token that allows processing to be cancelled.</param>
    /// <returns>
    /// A Task.
    /// </returns>
    private async Task UploadWithDuplicateCheckAsync(CommandContext context, UploadAssetSASCommand command, bool duplicate, NextDelegate next, CancellationToken ct)
    {
        command.FileId = context.ContextId.ToString();
        try
        {
            await EnrichWithMetadataAsync(command, ct);
            await EnrichWithHashAndUploadAsync(command, command.FileId, ct);

            if (!duplicate)
            {
                var existing = await _assetQuery.FindByHashAsync(_contextProvider.Context,
                        command.FileHash,
                        command.FileName,
                        command.FileSize,
                        ct);

                if (existing != null)
                {
                    context.Complete(new AssetDuplicate(existing));

                    await next(context, ct);
                    return;
                }
            }

            await UpsertAsset(context, command, command.FileId, ct);

            await next(context, ct);
        }
        finally
        {
            await _assetFileStore.DeleteAsync(command.FileId, ct);
        }
    }

    /// <summary>
    /// Upsert asset.
    /// </summary>
    /// <param name="context"> The context.</param>
    /// <param name="command"> The command.</param>
    /// <param name="tempFile"> The temporary file.</param>
    /// <param name="ct"> A token that allows processing to be cancelled.</param>
    /// <returns>
    /// A Task.
    /// </returns>
    private async Task UpsertAsset(CommandContext context, UploadAssetSASCommand command, string tempFile, CancellationToken ct)
    {
        var cmd = command.ToCommand();
        var f = _domainObjectFactory.Create<AssetDomainObject>(cmd.AggregateId);
        var oldSnapshot = f.Snapshot;
        var r = await f.ExecuteAsync(cmd, ct);

        await _domainObjectCache.SetAsync(f.UniqueId, oldSnapshot.Version, oldSnapshot, default);
        await _domainObjectCache.SetAsync(f.UniqueId, f.Snapshot.Version, f.Snapshot, default);

        var payload = await EnrichResultAsync(context, r, ct);
        context.Complete(payload);
    }
}
