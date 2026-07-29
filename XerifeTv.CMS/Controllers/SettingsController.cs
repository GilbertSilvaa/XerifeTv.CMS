using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Common.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Views.Settings.Models;

namespace XerifeTv.CMS.Controllers;

public class SettingsController(
    IUserService userService,
    IWebhookService webhookService,
    IMediaDeliveryProfileService mediaDeliveryProfileService,
    ILogger<SettingsController> logger) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userResponse = await userService.GetByUsernameAsync(User.Identity?.Name ?? string.Empty);
        if (userResponse.IsFailure) return RedirectToAction("Logout", "Users");

        var webhooksResponse = await webhookService.GetAsync(currentPage: 1, limit: 50);
        if (webhooksResponse.IsFailure) return RedirectToAction("Index", "Home");

        var mediaDeliveryProfilesResponse = await mediaDeliveryProfileService.GetAllAsync(isIncludeDisabled: true);
        if (mediaDeliveryProfilesResponse.IsFailure) return RedirectToAction("Index", "Home");

        SettingsModelView model = new(userResponse.Data!, webhooksResponse.Data?.Items ?? [], mediaDeliveryProfilesResponse.Data ?? []);

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UserUpdateProfile(UpdateUserProfileRequestDto dto)
    {
        var updateUserRequestDto = new UpdateUserRequestDto
        {
            Id = dto.Id,
            Email = dto.Email,
            UserName = dto.UserName,
            Role = null,
            Blocked = null
        };

        var response = await userService.UpdateAsync(updateUserRequestDto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Perfil atualizado com sucesso");

        logger.LogInformation($"{User.Identity?.Name} updated your own profile");

        return Redirect(Url.Action("Index") + "#profile");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UserUpdatePassword(UpdatePasswordUserRequestDto dto)
    {
        if (dto.NewPassword != dto.NewPasswordConfirm)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson("Confirmacao de senha incorreta");
            return Redirect(Url.Action("Index") + "#password");
        }

        var response = await userService.UpdatePasswordAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Senha atualizada com sucesso");

        logger.LogInformation($"{User.Identity?.Name} updated your password");

        return Redirect(Url.Action("Index") + "#password");
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RegisterWebhook(CreateWebhookRequestDto dto)
    {
        var response = await webhookService.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Webhook cadastrado com sucesso");

        return Redirect(Url.Action("Index") + "#webhook");
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateWebhook(UpdateWebhookRequestDto dto)
    {
        var response = await webhookService.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Webhook atualizado com sucesso");

        return Redirect(Url.Action("Index") + "#webhook");
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteWebhook(string id)
    {
        var response = await webhookService.DeleteAsync(id);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Webhook deletado com sucesso");

        return Redirect(Url.Action("Index") + "#webhook");
    }
}
