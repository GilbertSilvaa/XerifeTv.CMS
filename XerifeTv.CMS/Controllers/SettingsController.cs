using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Request;
using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.Integrations.Webhook.Entities;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Extensions;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Views.Settings.Models;

namespace XerifeTv.CMS.Controllers;

public class SettingsController(
    IUserService userService,
    IWebhookService webhookService,
    IWebhookDispatchHistoryService webhookDispatchHistoryService,
    IMediaDeliveryProfileService mediaDeliveryProfileService,
    IAuditLogService auditLogService,
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

        if (response.IsSuccess)
        {
            await this.AddAuditLogAsync(
                auditLogService,
                "User",
                response.Data ?? string.Empty,
                $"atualizou o perfil de usuário");
        }

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

        if (response.IsSuccess)
        {
            await this.AddAuditLogAsync(
                auditLogService,
                "User",
                response.Data ?? string.Empty,
                $"atualizou a senha de usuário");
        }

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

        if (response.IsSuccess)
        {
            await this.AddAuditLogAsync(
                auditLogService,
                "Webhook",
                response.Data ?? string.Empty,
                $"adicionou o webhook {dto.Name}");
        }

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

        if (response.IsSuccess)
        {
            await this.AddAuditLogAsync(
                auditLogService,
                "Webhook",
                response.Data ?? string.Empty,
                $"atualizou o webhook {dto.Name}");
        }

        return Redirect(Url.Action("Index") + "#webhook");
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteWebhook(string id)
    {
        if (id is not null)
        {
            var webhookResponse = await webhookService.GetAsync(id);
            var name = webhookResponse.IsSuccess && webhookResponse.Data is not null
                ? webhookResponse.Data.Name
                : id;

            var response = await webhookService.DeleteAsync(id);

            TempData["Notification"] = response.IsFailure
              ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
              : MessageViewHelper.SuccessJson("Webhook deletado com sucesso");

            if (response.IsSuccess)
            {
                await this.AddAuditLogAsync(
                    auditLogService,
                    "Webhook",
                    id,
                    $"removeu o webhook {name}");
            }
        }


        return Redirect(Url.Action("Index") + "#webhook");
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetWebhookDispatchHistory(
        string? webhookId,
        EWebhookTriggerEvent? triggerEvent,
        EWebhookDispatchStatus? status,
        int page = 1,
        int limit = 10)
    {
        var result = await webhookDispatchHistoryService.GetHistoryAsync(webhookId, triggerEvent, status, page, limit);

        if (result.IsFailure)
        {
            return PartialView("_WebhookHistoryTable", new PagedList<GetWebhookDispatchHistoryResponseDto>(1, 0, []));
        }

        return PartialView("_WebhookHistoryTable", result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RedispatchWebhook([FromBody] RedispatchWebhookRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.HistoryId))
        {
            return Json(new { success = false, message = "ID de histórico inválido." });
        }

        var response = await webhookDispatchHistoryService.RedispatchAsync(request.HistoryId);

        if (response.IsFailure)
        {
            return Json(new { success = false, message = response.Error.Description ?? "Falha ao re-disparar o webhook." });
        }

        var item = response.Data;

        await this.AddAuditLogAsync(
            auditLogService,
            "WebhookHistory",
            item?.Id ?? request.HistoryId,
            $"disparou manualmente o webhook {item?.WebhookName}");

        return Json(new
        {
            success = true,
            message = item?.Status == EWebhookDispatchStatus.SUCCESS
                ? "Webhook disparado com sucesso!"
                : $"Disparo manual finalizado com status: {item?.Status}",
            data = item
        });
    }
}
