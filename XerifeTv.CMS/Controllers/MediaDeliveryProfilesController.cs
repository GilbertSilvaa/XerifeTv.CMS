using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize(Roles = "admin")]
public class MediaDeliveryProfilesController(
    IMediaDeliveryProfileService _service,
    IMediaDeliveryUrlResolver _urlResolver,
    ILogger<MediaDeliveryProfilesController> _logger,
    ICacheService _cacheService) : Controller
{
    public async Task<IActionResult> Create(CreateMediaDeliveryProfileRequestDto dto)
    {
        var response = await _service.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Perfil Entrega de Midia cadastrado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} registered the media delivery profile {dto.Name}");

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }

    public async Task<IActionResult> Update(UpdateMediaDeliveryProfileRequestDto dto)
    {
        var response = await _service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Perfil Entrega de Midia atualizado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated the media delivery profile {dto.Name}");

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }

    public async Task<IActionResult> Delete(string? id)
    {
        if (id is not null)
        {
            var response = await _service.DeleteAsync(id);

            TempData["Notification"] = response.IsFailure
              ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
              : MessageViewHelper.SuccessJson($"Perfil Entrega de Midia deletado com sucesso");

            _logger.LogInformation($"{User.Identity?.Name} removed the media delivery profile with id = {id}");
        }

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ResolveUrl(string mediaPath, string mediaDeliveryProfileId, bool isCached = false)
    {
        var normalizedPath = mediaPath.Trim().ToLowerInvariant();
        var cacheKey = $"resolve-url:{normalizedPath}:{mediaDeliveryProfileId}";
        var responseCache = _cacheService.GetValue<GetResolveUrlResponseDto?>(cacheKey);

        if (responseCache != null && isCached)
            return Ok(new { responseCache?.Url, responseCache?.StreamFormat });

        var response = await _urlResolver.ResolveUrlAsync(mediaPath, mediaDeliveryProfileId);

        if (response.IsFailure)
            return StatusCode(int.Parse(response.Error.Code), response.Error.Description);
        
        _cacheService.SetValue<GetResolveUrlResponseDto?>(cacheKey, response.Data);

        return Ok(new { response.Data?.Url, response.Data?.StreamFormat });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ResolveUrlFixed(string urlFixed, string streamFormat)
    {
        var response = await _urlResolver.ResolveUrlFixedAsync(urlFixed, streamFormat);

        if (response.IsFailure)
            return StatusCode(int.Parse(response.Error.Code), response.Error.Description);

        return Ok(new { response.Data?.Url, response.Data?.StreamFormat });
    }
}