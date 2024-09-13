// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Squidex.Areas.Api.Config.OpenApi;
using Squidex.Areas.Api.Controllers.Assets.Models;
using Squidex.Assets;
using Squidex.Domain.Apps.Core.Scripting;
using Squidex.Domain.Apps.Core.Tags;
using Squidex.Domain.Apps.Entities;
using Squidex.Domain.Apps.Entities.Assets;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Shared;
using Squidex.Web;

namespace Squidex.Areas.Api.Controllers.Assets;

/// <summary>
/// Uploads and retrieves assets.
/// </summary>
[ApiExplorerSettings(GroupName = nameof(Assets))]
public sealed class AssetsController : ApiController
{
    /// <summary>
    /// (Immutable) the asset query.
    /// </summary>
    private readonly IAssetQueryService assetQuery;

    /// <summary>
    /// (Immutable) the asset usage tracker.
    /// </summary>
    private readonly IAssetUsageTracker assetUsageTracker;

    /// <summary>
    /// (Immutable) the tag service.
    /// </summary>
    private readonly ITagService tagService;

    /// <summary>
    /// (Immutable) the asset tus runner.
    /// </summary>
    private readonly AssetTusRunner assetTusRunner;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="commandBus"> The command bus.</param>
    /// <param name="assetQuery"> The asset query.</param>
    /// <param name="assetUsageTracker"> The asset usage tracker.</param>
    /// <param name="tagService"> The tag service.</param>
    /// <param name="assetTusRunner"> The asset tus runner.</param>
    public AssetsController(
        ICommandBus commandBus,
        IAssetQueryService assetQuery,
        IAssetUsageTracker assetUsageTracker,
        ITagService tagService,
        AssetTusRunner assetTusRunner)
        : base(commandBus)
    {
        this.assetQuery = assetQuery;
        this.assetUsageTracker = assetUsageTracker;
        this.assetTusRunner = assetTusRunner;
        this.tagService = tagService;
    }

    /// <summary>
    /// Get assets tags.
    /// </summary>
    /// <remarks>
    /// Get all tags for assets.
    /// </remarks>
    /// <param name="app"> The name of the app.</param>
    /// <returns>
    /// The tags.
    /// </returns>
    [HttpGet]
    [Route("apps/{app}/assets/tags")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsRead)]
    [ApiCosts(1)]
    public async Task<IActionResult> GetTags(string app)
    {
        var tags = await tagService.GetTagsAsync(AppId, TagGroups.Assets, HttpContext.RequestAborted);

        Response.Headers[HeaderNames.ETag] = tags.Version.ToString(CultureInfo.InvariantCulture);

        return Ok(tags);
    }

    /// <summary>
    /// Rename an asset tag.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="name"> The tag to return.</param>
    /// <param name="request"> The required request object.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpPut]
    [Route("apps/{app}/assets/tags/{name}")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsUpdate)]
    [ApiCosts(1)]
    public async Task<IActionResult> PutTag(string app, string name, [FromBody] RenameTagDto request)
    {
        await tagService.RenameTagAsync(AppId, TagGroups.Assets, Uri.UnescapeDataString(name), request.TagName, HttpContext.RequestAborted);

        return await GetTags(app);
    }

    /// <summary>
    /// Get assets.
    /// </summary>
    /// <remarks>
    /// Get all assets for the app.
    /// </remarks>
    /// <param name="app"> The name of the app.</param>
    /// <param name="parentId"> The optional parent folder id.</param>
    /// <param name="ids"> (Optional) The optional asset ids.</param>
    /// <param name="q"> (Optional) The optional json query.</param>
    /// <returns>
    /// The assets.
    /// </returns>
    [HttpGet]
    [Route("apps/{app}/assets/")]
    [ProducesResponseType(typeof(AssetsDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsRead)]
    [ApiCosts(1)]
    [AcceptQuery(false)]
    [AcceptHeader.NoTotal]
    [AcceptHeader.NoSlowTotal]
    public async Task<IActionResult> GetAssets(string app, [FromQuery] DomainId? parentId, [FromQuery] string? ids = null, [FromQuery] string? q = null)
    {
        var assets = await assetQuery.QueryAsync(Context, parentId, CreateQuery(ids, q), HttpContext.RequestAborted);

        var response = Deferred.Response(() =>
        {
            return AssetsDto.FromDomain(assets, Resources);
        });

        return Ok(response);
    }

    /// <summary>
    /// Get assets.
    /// </summary>
    /// <remarks>
    /// Get all assets for the app.
    /// </remarks>
    /// <param name="app"> The name of the app.</param>
    /// <param name="query"> The required query object.</param>
    /// <returns>
    /// The assets post.
    /// </returns>
    [HttpPost]
    [Route("apps/{app}/assets/query")]
    [ProducesResponseType(typeof(AssetsDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsRead)]
    [ApiCosts(1)]
    [AcceptHeader.NoTotal]
    [AcceptHeader.NoSlowTotal]
    public async Task<IActionResult> GetAssetsPost(string app, [FromBody] QueryDto query)
    {
        var assets = await assetQuery.QueryAsync(Context, query?.ParentId, query?.ToQuery() ?? Q.Empty, HttpContext.RequestAborted);

        var response = Deferred.Response(() =>
        {
            return AssetsDto.FromDomain(assets, Resources);
        });

        return Ok(response);
    }

    /// <summary>
    /// Get an asset by id.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="id"> The ID of the asset to retrieve.</param>
    /// <returns>
    /// The asset.
    /// </returns>
    [HttpGet]
    [Route("apps/{app}/assets/{id}/")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsRead)]
    [ApiCosts(1)]
    public async Task<IActionResult> GetAsset(string app, DomainId id)
    {
        var asset = await assetQuery.FindAsync(Context, id, ct: HttpContext.RequestAborted);

        if (asset == null)
        {
            return NotFound();
        }

        var response = Deferred.Response(() =>
        {
            return AssetDto.FromDomain(asset, Resources);
        });

        return Ok(response);
    }

    /// <summary>
    /// Upload a new asset.
    /// </summary>
    /// <remarks>
    /// You can only upload one file at a time. The mime type of the file is not calculated by
    /// Squidex and is required correctly.
    /// </remarks>
    /// <param name="app"> The name of the app.</param>
    /// <param name="request"> The request parameters.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpPost]
    [Route("apps/{app}/assets/")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status201Created)]
    [AssetRequestSizeLimit]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsCreate)]
    [ApiCosts(1)]
    public async Task<IActionResult> PostAsset(string app, CreateAssetDto request)
    {
        var command = await request.ToCommandAsync(HttpContext, App);

        var response = await InvokeCommandAsync(command);

        return CreatedAtAction(nameof(GetAsset), new { app, id = response.Id }, response);
    }

    /// <summary>
    /// Upload a new asset using tus.io.
    /// </summary>
    /// <remarks>
    /// Use the tus protocol to upload an asset.
    /// </remarks>
    /// <param name="app"> The name of the app.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("apps/{app}/assets/tus/{**fileId}")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status201Created)]
    [AssetRequestSizeLimit]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsCreate)]
    [ApiCosts(1)]
    public async Task<IActionResult> PostAssetTus(string app)
    {
        var url = Url.Action(null, new { app, fileId = (object?)null })!;

        var (result, file) = await assetTusRunner.InvokeAsync(HttpContext, url);

        if (file != null)
        {
            var command = UpsertAssetDto.ToCommand(file);

            var response = await InvokeCommandAsync(command);

            return CreatedAtAction(nameof(GetAsset), new { app, id = response.Id }, response);
        }

        return result;
    }

    /// <summary>
    /// Bulk update assets.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="request"> The bulk update request.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpPost]
    [Route("apps/{app}/assets/bulk")]
    [ProducesResponseType(typeof(BulkResultDto[]), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsRead)]
    [ApiCosts(5)]
    public async Task<IActionResult> BulkUpdateAssets(string app, [FromBody] BulkUpdateAssetsDto request)
    {
        var command = request.ToCommand();

        var context = await CommandBus.PublishAsync(command, HttpContext.RequestAborted);

        var result = context.Result<BulkUpdateResult>();
        var response = result.Select(x => BulkResultDto.FromDomain(x, HttpContext)).ToArray();

        return Ok(response);
    }

    /// <summary>
    /// Upsert an asset.
    /// </summary>
    /// <remarks>
    /// You can only upload one file at a time. The mime type of the file is not calculated by
    /// Squidex and is required correctly.
    /// </remarks>
    /// <param name="app"> The name of the app.</param>
    /// <param name="id"> The optional custom asset id.</param>
    /// <param name="request"> The request parameters.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpPost]
    [Route("apps/{app}/assets/{id}")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [AssetRequestSizeLimit]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsCreate)]
    [ApiCosts(1)]
    public async Task<IActionResult> PostUpsertAsset(string app, DomainId id, UpsertAssetDto request)
    {
        var command = await request.ToCommandAsync(id, HttpContext, App);

        var response = await InvokeCommandAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Replace asset content.
    /// </summary>
    /// <remarks>
    /// Use multipart request to upload an asset.
    /// </remarks>
    /// <param name="app"> The name of the app.</param>
    /// <param name="id"> The ID of the asset.</param>
    /// <param name="request"> The request parameters.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpPut]
    [Route("apps/{app}/assets/{id}/content/")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [AssetRequestSizeLimit]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsUpload)]
    [ApiCosts(1)]
    public async Task<IActionResult> PutAssetContent(string app, DomainId id, UpdateAssetDto request)
    {
        var command = await request.ToCommandAsync(id, HttpContext, App);

        var response = await InvokeCommandAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Update an asset.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="id"> The ID of the asset.</param>
    /// <param name="request"> The asset object that needs to updated.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpPut]
    [Route("apps/{app}/assets/{id}/")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [AssetRequestSizeLimit]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsUpdate)]
    [ApiCosts(1)]
    public async Task<IActionResult> PutAsset(string app, DomainId id, [FromBody] AnnotateAssetDto request)
    {
        var command = request.ToCommand(id);

        var response = await InvokeCommandAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Moves the asset.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="id"> The ID of the asset.</param>
    /// <param name="request"> The asset object that needs to updated.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpPut]
    [Route("apps/{app}/assets/{id}/parent")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [AssetRequestSizeLimit]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsUpdate)]
    [ApiCosts(1)]
    public async Task<IActionResult> PutAssetParent(string app, DomainId id, [FromBody] MoveAssetDto request)
    {
        var command = request.ToCommand(id);

        var response = await InvokeCommandAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Delete an asset.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="id"> The ID of the asset to delete.</param>
    /// <param name="request"> The request parameters.</param>
    /// <returns>
    /// An IActionResult.
    /// </returns>
    [HttpDelete]
    [Route("apps/{app}/assets/{id}/")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ApiPermissionOrAnonymous(PermissionIds.AppAssetsDelete)]
    [ApiCosts(1)]
    public async Task<IActionResult> DeleteAsset(string app, DomainId id, DeleteAssetDto request)
    {
        var command = request.ToCommand(id);

        await CommandBus.PublishAsync(command, HttpContext.RequestAborted);

        return NoContent();
    }

    /// <summary>
    /// (An Action that handles HTTP GET requests) gets script completion.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="schema"> The schema.</param>
    /// <param name="completer"> The completer.</param>
    /// <returns>
    /// A response to return to the caller.
    /// </returns>
    [HttpGet]
    [Route("apps/{app}/assets/completion")]
    [ApiPermissionOrAnonymous]
    [ApiCosts(1)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetScriptCompletion(string app, string schema,
        [FromServices] ScriptingCompleter completer)
    {
        var completion = completer.AssetScript();

        return Ok(completion);
    }

    /// <summary>
    /// (An Action that handles HTTP GET requests) gets script trigger completion.
    /// </summary>
    /// <param name="app"> The name of the app.</param>
    /// <param name="schema"> The schema.</param>
    /// <param name="completer"> The completer.</param>
    /// <returns>
    /// A response to return to the caller.
    /// </returns>
    [HttpGet]
    [Route("apps/{app}/assets/completion/trigger")]
    [ApiPermissionOrAnonymous]
    [ApiCosts(1)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetScriptTriggerCompletion(string app, string schema,
        [FromServices] ScriptingCompleter completer)
    {
        var completion = completer.AssetTrigger();

        return Ok(completion);
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

    /// <summary>
    /// Creates a query.
    /// </summary>
    /// <param name="ids"> The optional asset ids.</param>
    /// <param name="q"> The optional json query.</param>
    /// <returns>
    /// The new query.
    /// </returns>
    private Q CreateQuery(string? ids, string? q)
    {
        return Q.Empty
            .WithIds(ids)
            .WithJsonQuery(q)
            .WithODataQuery(Request.QueryString.ToString());
    }
}
