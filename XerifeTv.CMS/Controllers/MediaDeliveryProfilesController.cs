using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize(Roles = "admin")]
public class MediaDeliveryProfilesController(IMediaDeliveryProfileService _service, ILogger<MediaDeliveryProfilesController> _logger) : Controller
{
    public async Task<IActionResult> Create(CreateMediaDeliveryProfileRequestDto dto)
    {
        var response = await _service.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Perfil Entrega de Midia cadastrado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} registered the midia delivery profile {dto.Name}");

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }

    public async Task<IActionResult> Update(UpdateMediaDeliveryProfileRequestDto dto)
    {
        var response = await _service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Perfil Entrega de Midia atualizado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated the midia delivery profile {dto.Name}");

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

            _logger.LogInformation($"{User.Identity?.Name} removed the midia delivery profile with id = {id}");
        }

        return Redirect(Url.Action("Index", "Settings") + "#media-delivery");
    }
}
