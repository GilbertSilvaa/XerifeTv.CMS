using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

public class SettingsController(IUserService _userService, ILogger<SettingsController> _logger) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var response = await _userService.GetByUsernameAsync(User.Identity?.Name ?? string.Empty);

        if (response.IsFailure) return RedirectToAction("Logout", "Users");

        return View(response.Data);
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

        var response = await _userService.UpdateAsync(updateUserRequestDto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Perfil atualizado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated your own profile");

        return Redirect(Url.Action("Index") + "#profile");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UserUpdatePassword(UpdatePasswordUserRequestDto dto)
    {
        if (dto.NewPassword != dto.NewPasswordConfirm)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson("Confirmacao de senha incorreta");
            return Redirect(Url.Action("Index") + "password");
        }

        var response = await _userService.UpdatePasswordAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Senha atualizada com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated your password");

        return Redirect(Url.Action("Index") + "#password");
    }
}