using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

public class UsersController(IUserService _service, ILogger<UsersController> _logger) : Controller
{
    private readonly CookieOptions _cookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTime.UtcNow.AddHours(6)
    };

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Index()
    {
        var response = await _service.GetAsync(1, 20);

        _logger.LogInformation($"{User.Identity?.Name} accessed the users page");

        if (response.IsSuccess)
            return View(response.Data?.Items);

        return View(Enumerable.Empty<GetUserResponseDto>());
    }

    [AllowAnonymous]
    public IActionResult SignIn()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn(LoginUserRequestDto dto)
    {
        var response = await _service.LoginAsync(dto);

        if (response.IsFailure)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty);
            _logger.LogInformation("There was an unsuccessful login attempt");
            return View();
        }

        Response.Cookies.Append("token", response.Data?.Token ?? string.Empty, _cookieOptions);
        Response.Cookies.Append("refreshToken", response.Data?.RefreshToken ?? string.Empty, _cookieOptions);

        _logger.LogInformation($"{User.Identity?.Name} logged into the system");

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult EmailResetPasswordForm()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> EmailResetPasswordForm(string email)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        var response = await _service.SendEmailResetPasswordAsync(email);

        if (response.IsFailure)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description);
            _logger.LogInformation($"{email} tried to send password reset email and failed");
            return View();
        }

        TempData["Notification"] = MessageViewHelper.SuccessJson("Email enviado com sucesso");
        _logger.LogInformation($"{email} tried to send password reset email");

        return View(model: email);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string code)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        var response = await _service.ValidateResetPasswordGuidAsync(new Guid(code));

        if (response.IsFailure)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description);
            return View();
        }

        return View(model: response.Data);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        if (dto.Password != dto.ConfirmPassword)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson("Confirmacao de senha incorreta");
            return RedirectToAction("ResetPassword", new { code = dto.CodeGuid });
        }

        var response = await _service.ResetPasswordAsync(dto);

        if (response.IsFailure)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty);
            return RedirectToAction("ResetPassword", new { code = dto.CodeGuid });
        }

        TempData["Notification"] = MessageViewHelper.SuccessJson("Senha redefinida com sucesso");

        return RedirectToAction("SignIn");
    }

    [AllowAnonymous]
    public IActionResult Logout()
    {
        _logger.LogInformation($"{User.Identity?.Name} logged out of the system");

        Response.Cookies.Delete("token");
        Response.Cookies.Delete("refreshToken");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Register(RegisterUserRequestDto dto)
    {
        var response = await _service.RegisterAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Usuario cadastrado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} registered a new user");

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateUserRequestDto dto)
    {
        var response = await _service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Perfil atualizado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated your own profile");

        return RedirectToAction("Settings");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpdatePassword(UpdatePasswordUserRequestDto dto)
    {
        if (dto.NewPassword != dto.NewPasswordConfirm)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson("Confirmacao de senha incorreta");
            return RedirectToAction("Settings");
        }

        var response = await _service.UpdatePasswordAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Senha atualizada com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated your password");

        return RedirectToAction("Settings");
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(UpdateUserRequestDto dto)
    {
        var response = await _service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Usuario atualizado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated user {dto.Id}");
        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var response = await _service.DeleteAsync(id);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson("Usuario deletado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} removed user with id = {id}");

        return RedirectToAction("Index");
    }

    [AllowAnonymous]
    public IActionResult UserUnauthorized()
    {
        _logger.LogInformation($"{User.Identity?.Name} tried to access a page for which he is not authorized");

        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> RefreshSession(string? successRedirectUrl = null)
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return RedirectToAction("SignIn");

        var response = await _service.TryRefreshSessionAsync(refreshToken);

        if (response.IsFailure)
            return RedirectToAction("SignIn");

        var (newToken, newRefreshToken) = response.Data;

        if (!string.IsNullOrEmpty(newToken) && !string.IsNullOrEmpty(newRefreshToken))
        {
            Response.Cookies.Append("token", newToken, _cookieOptions);
            Response.Cookies.Append("refreshToken", newRefreshToken, _cookieOptions);

            if (string.IsNullOrEmpty(successRedirectUrl))
                return RedirectToAction("Index", "Home");

            return Redirect(successRedirectUrl);
        }

        return RedirectToAction("SignIn");
    }

    [Authorize]
    public async Task<IActionResult> Settings()
    {
        var response = await _service.GetByUsernameAsync(User.Identity?.Name ?? string.Empty);

        if (response.IsFailure) return Logout();

        return View(response.Data);
    }
}