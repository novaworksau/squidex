// file:	Squidex.Extensions.AssetSAS\Controllers\AssetSASUploadController.cs
//
// summary:	Implements the asset sas upload controller class
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Squidex.Assets;
using Squidex.Domain.Apps.Entities.Assets;
using Squidex.Extensions.AssetSAS.Models;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Shared;
using Squidex.Web;
using System.Reflection;

namespace Squidex.Extensions.AssetSAS.Controllers
{
    /// <summary>
    /// A controller for handling asset sas uploads.
    /// </summary>
    [ApiExplorerSettings(GroupName = nameof(Assets))]
    
    public sealed class AssetSASUploadController : ApiController
    {
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
        /// The BLOB container filed.
        /// </summary>
        private static FieldInfo? blobContainerFiled = typeof(AzureBlobAssetStore).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => x.FieldType == typeof(BlobContainerClient));

        /// <summary>
        /// (Immutable) the asset store.
        /// </summary>
        private readonly IAssetStore _assetStore;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="commandBus"> The command bus.</param>
        /// <param name="assetStore"> The asset store.</param>
        public AssetSASUploadController(ICommandBus commandBus, IAssetStore assetStore)
            : base(commandBus)
        {
            _assetStore = assetStore;
        }

        /// <summary>
        /// (An Action that handles HTTP POST requests) posts an asset.
        /// </summary>
        /// <param name="app"> The application.</param>
        /// <param name="request"> The request.</param>
        /// <param name="ct"> A token that allows processing to be cancelled.</param>
        /// <returns>
        /// An IActionResult.
        /// </returns>
        [HttpPost]
        [Route("apps/{app}/assets_sas/")]
        [ProducesResponseType(typeof(AssetDto), StatusCodes.Status201Created)]
        
        [ApiPermissionOrAnonymous(PermissionIds.AppAssetsCreate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PostAsset(string app, CreateAssetSASDto request, CancellationToken ct)
        {
            var cmd = request.ToSASCommand(_assetStore);
            var response = await InvokeCommandAsync(cmd);
            return CreatedAtAction("GetAsset", "Assets", new { app, id = response.Id }, response);
        }

        /// <summary>
        /// (An Action that handles HTTP POST requests) posts an asset.
        /// </summary>
        /// <param name="app"> The application.</param>
        /// <param name="id"> The identifier.</param>
        /// <param name="request"> The request.</param>
        /// <param name="ct"> A token that allows processing to be cancelled.</param>
        /// <returns>
        /// An IActionResult.
        /// </returns>
        [HttpPost]
        [Route("apps/{app}/assets_sas/{id}")]
        [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
        [AssetRequestSizeLimit]
        [ApiPermissionOrAnonymous(PermissionIds.AppAssetsCreate)]
        [ApiCosts(1)]
        public async Task<IActionResult> PutAsset(string app, DomainId id, UpsertAssetSASDto request, CancellationToken ct)
        {
            var cmd = request.ToSASCommand(id, _assetStore);
            var response = await InvokeCommandAsync(cmd);
            return Ok(response);
        }

        /// <summary>
        /// Executes the command asynchronous on a different thread, and waits for the result.
        /// </summary>
        /// <param name="command"> The command.</param>
        /// <returns>
        /// The invoke command.
        /// </returns>
        private async Task<AssetDto> InvokeCommandAsync(ICommand command)
        {
            var context = await CommandBus.PublishAsync(command, HttpContext.RequestAborted);

            if (context.PlainResult is AssetDuplicate created)
            {
                return AssetDto.FromDomain(created.Asset, Resources, true);
            }
            else
            {
                return AssetDto.FromDomain(context.Result<EnrichedAsset>(), Resources);
            }
        }
    }
}
