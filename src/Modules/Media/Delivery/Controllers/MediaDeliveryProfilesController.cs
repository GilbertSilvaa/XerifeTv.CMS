using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Extensions;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.Media.Delivery.Controllers;

public class MediaDeliveryProfilesController(
    IMediaDeliveryProfileService service,
    IMediaDeliveryUrlResolver urlResolver,
    IDemoStreamingSettingsService demoStreamingSettingsService,
    ILogger<MediaDeliveryProfilesController> logger,
    ICacheService cacheService,
    IAuditLogService auditLogService,
    IConfiguration configuration) : Controller
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create(CreateMediaDeliveryProfileRequestDto dto)
    {
        var response = await service.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Perfil Entrega de Mídia cadastrado com sucesso");

        if (response.IsSuccess)
        {
            await this.AddAuditLogAsync(
                auditLogService,
                "MediaDeliveryProfile",
                response.Data ?? string.Empty,
                $"adicionou o perfil de entrega de mídia {dto.Name}");
        }

        logger.LogInformation($"{User.Identity?.Name} registered the media delivery profile {dto.Name}");

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(UpdateMediaDeliveryProfileRequestDto dto)
    {
        var response = await service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Perfil Entrega de Mídia atualizado com sucesso");

        if (response.IsSuccess)
        {
            await InvalidateMediaDeliveryProfileCacheAsync(dto.Id);

            await this.AddAuditLogAsync(
                auditLogService,
                "MediaDeliveryProfile",
                response.Data ?? string.Empty,
                $"atualizou o perfil de entrega de mídia {dto.Name}");
        }

        logger.LogInformation($"{User.Identity?.Name} updated the media delivery profile {dto.Name}");

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string? id)
    {
        if (id is not null)
        {
            var mdpResponse = await service.GetAsync(id);
            var name = mdpResponse.IsSuccess && mdpResponse.Data is not null
                ? mdpResponse.Data.Name
                : id;

            var response = await service.DeleteAsync(id);

            TempData["Notification"] = response.IsFailure
              ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
              : MessageViewHelper.SuccessJson($"Perfil Entrega de Mídia deletado com sucesso");

            if (response.IsSuccess)
            {
                await InvalidateMediaDeliveryProfileCacheAsync(id);

                await this.AddAuditLogAsync(
                    auditLogService,
                    "MediaDeliveryProfile",
                    id,
                    $"removeu o perfil de entrega de mídia {name}");
            }

            logger.LogInformation($"{User.Identity?.Name} removed the media delivery profile with id = {id}");
        }

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ResolveUrl(string mediaPath, string mediaDeliveryProfileId, bool isCached = false)
    {
        var demoResult = await TryResolveDemoAsync();

        if (demoResult is not null)
            return demoResult;

        var normalizedPath = mediaPath.Trim().ToLowerInvariant();
        var cacheKey = $"resolve-url:{normalizedPath}:{mediaDeliveryProfileId}";

        int? errorStatusCode = null;
        string? errorDescription = null;

        async Task<GetResolveUrlResponseDto?> ResolveAsync()
        {
            var response = await urlResolver.ResolveUrlAsync(mediaPath, mediaDeliveryProfileId);

            if (response.IsFailure)
            {
                errorStatusCode = int.Parse(response.Error.Code);
                errorDescription = response.Error.Description;
                return null;
            }

            return response.Data;
        }

        GetResolveUrlResponseDto? data;

        if (isCached)
        {
            data = await cacheService.GetOrCreateAsync(cacheKey, CacheTtl, async () =>
            {
                var result = await ResolveAsync();

                if (result is not null)
                    await RegisterInvalidationKeyAsync(MediaDeliveryProfileIndexKey(mediaDeliveryProfileId), cacheKey);

                return result;
            });
        }
        else
        {
            data = await ResolveAsync();
        }

        if (errorStatusCode is not null)
            return StatusCode(errorStatusCode.Value, errorDescription);

        string urlResult = data?.Url ?? "";

        if (configuration["UseMediaProxy"]?.Equals("true") == false)
            return Ok(new { Url = urlResult, data?.StreamFormat });

        string routeMidiaProxy = normalizedPath.EndsWith(".mp4") ? "mp4" : "hls";
        string urlEncrypted = CryptographyHelper.Encrypt(urlResult, configuration["SecuritySettings:ContentEncryptionKey"]!);
        string urlMidiaProxy = $"/MediaProxy/{routeMidiaProxy}?url={Uri.EscapeDataString(urlEncrypted)}";

        return Ok(new { Url = urlMidiaProxy, data?.StreamFormat });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ResolveUrlFixed(string urlFixed, string streamFormat)
    {
        var demoResult = await TryResolveDemoAsync();

        if (demoResult is not null)
            return demoResult;

        var response = await urlResolver.ResolveUrlFixedAsync(urlFixed, streamFormat);

        if (response.IsFailure)
            return StatusCode(int.Parse(response.Error.Code), response.Error.Description);

        string urlResult = response.Data?.Url ?? "";

        if (configuration["UseMediaProxy"]?.Equals("true") == false)
            return Ok(new { Url = urlResult, response.Data?.StreamFormat });

        string routeMidiaProxy = "hls";

        if (Uri.TryCreate(urlResult, UriKind.Absolute, out var uri) &&
            uri.AbsolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            routeMidiaProxy = "mp4";
        }

        string urlEncrypted = CryptographyHelper.Encrypt(urlResult, configuration["SecuritySettings:ContentEncryptionKey"]!);
        string urlMidiaProxy = $"/MediaProxy/{routeMidiaProxy}?url={Uri.EscapeDataString(urlEncrypted)}";

        return Ok(new { Url = urlMidiaProxy, response.Data?.StreamFormat });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ResolveUrlMdp(string mp, string mdp)
    {
        var demoResult = await TryResolveDemoAsync();

        if (demoResult is not null)
            return demoResult;

        string mediaDeliveryProfileId = CryptographyHelper.Decrypt(mdp, configuration["SecuritySettings:ContentEncryptionKey"]!);
        string mediaPath = CryptographyHelper.Decrypt(mp, configuration["SecuritySettings:ContentEncryptionKey"]!);

        var normalizedPath = mediaPath.Trim().ToLowerInvariant();
        var cacheKey = $"resolve-url:{normalizedPath}:{mediaDeliveryProfileId}";

        int? errorStatusCode = null;
        string? errorDescription = null;

        var data = await cacheService.GetOrCreateAsync(cacheKey, CacheTtl, async () =>
        {
            var response = await urlResolver.ResolveUrlAsync(mediaPath, mediaDeliveryProfileId);

            if (response.IsFailure)
            {
                errorStatusCode = int.Parse(response.Error.Code);
                errorDescription = response.Error.Description;
                return null;
            }

            await RegisterInvalidationKeyAsync(MediaDeliveryProfileIndexKey(mediaDeliveryProfileId), cacheKey);

            return response.Data;
        });

        if (errorStatusCode is not null)
            return StatusCode(errorStatusCode.Value, errorDescription);

        string urlResult = data?.Url ?? "";

        if (configuration["UseMediaProxy"]?.Equals("true") == false)
            return Ok(new { Url = urlResult, data?.StreamFormat });

        string routeMidiaProxy = normalizedPath.EndsWith(".mp4") ? "mp4" : "hls";
        string urlEncrypted = CryptographyHelper.Encrypt(urlResult, configuration["SecuritySettings:ContentEncryptionKey"]!);
        string urlMidiaProxy = $"/MediaProxy/{routeMidiaProxy}?url={Uri.EscapeDataString(urlEncrypted)}";

        return Ok(new { Url = urlMidiaProxy, data?.StreamFormat });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ResolveUrlFx(string uf, string sf)
    {
        var demoResult = await TryResolveDemoAsync();

        if (demoResult is not null)
            return demoResult;

        string urlFixed = CryptographyHelper.Decrypt(uf, configuration["SecuritySettings:ContentEncryptionKey"]!);
        string streamFormat = CryptographyHelper.Decrypt(sf, configuration["SecuritySettings:ContentEncryptionKey"]!);

        var response = await urlResolver.ResolveUrlFixedAsync(urlFixed, streamFormat);

        if (response.IsFailure)
            return StatusCode(int.Parse(response.Error.Code), response.Error.Description);

        string urlResult = response.Data?.Url ?? "";

        if (configuration["UseMediaProxy"]?.Equals("true") == false)
            return Ok(new { Url = urlResult, response.Data?.StreamFormat });

        string routeMidiaProxy = "hls";

        if (Uri.TryCreate(urlResult, UriKind.Absolute, out var uri) &&
            uri.AbsolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            routeMidiaProxy = "mp4";
        }

        string urlEncrypted = CryptographyHelper.Encrypt(urlResult, configuration["SecuritySettings:ContentEncryptionKey"]!);
        string urlMidiaProxy = $"/MediaProxy/{routeMidiaProxy}?url={Uri.EscapeDataString(urlEncrypted)}";

        return Ok(new { Url = urlMidiaProxy, response.Data?.StreamFormat });
    }

    private async Task<IActionResult?> TryResolveDemoAsync()
    {
        if (!User.IsInRole("demo"))
            return null;

        var response = await demoStreamingSettingsService.GetAsync();

        if (response.IsFailure)
            return StatusCode(int.Parse(response.Error.Code), response.Error.Description);

        var videoUrl = response.Data?.VideoUrl ?? string.Empty;

        if (string.IsNullOrWhiteSpace(videoUrl))
            return NotFound(new { message = "Vídeo de demonstração não configurado." });

        return Ok(new
        {
            Url = videoUrl,
            StreamFormat = InferStreamFormat(videoUrl)
        });
    }

    private static string InferStreamFormat(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "hls";

        var path = uri.AbsolutePath;

        if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) return "mp4";
        if (path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)) return "webm";
        if (path.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)) return "mkv";
        if (path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)) return "mov";
        if (path.EndsWith(".mpd", StringComparison.OrdinalIgnoreCase)) return "mpeg-dash";

        return "hls";
    }

    private static string MediaDeliveryProfileIndexKey(string mediaDeliveryProfileId)
        => $"invalidation-index-media-delivery-profile-{mediaDeliveryProfileId}";

    private async Task RegisterInvalidationKeyAsync(string indexKey, string cacheKey)
    {
        var keys = await cacheService.GetValueAsync<List<string>>(indexKey) ?? [];

        if (keys.Contains(cacheKey)) return;

        keys.Add(cacheKey);

        await cacheService.SetValueAsync(indexKey, CacheTtl, keys);
    }

    private async Task InvalidateMediaDeliveryProfileCacheAsync(string mediaDeliveryProfileId)
    {
        var indexKey = MediaDeliveryProfileIndexKey(mediaDeliveryProfileId);
        var keys = await cacheService.GetValueAsync<List<string>>(indexKey);

        if (keys is null) return;

        foreach (var key in keys)
            await cacheService.RemoveAsync(key);

        await cacheService.RemoveAsync(indexKey);
    }
}