using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Authentication.Dtos.Request;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

public class UsersController(
	IUserService userService, 
	IAuthService authService,
	IConfiguration configuration,
	ILogger<UsersController> logger) : Controller
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
		var response = await userService.GetAsync(1, 20);

		logger.LogInformation($"{User.Identity?.Name} accessed the users page");

		if (response.IsSuccess)
			return View(response.Data?.Items);

		return View(Enumerable.Empty<GetUserResponseDto>());
	}

	[AllowAnonymous]
	public IActionResult SignIn()
	{
		if (User.Identity != null && User.Identity.IsAuthenticated)
			return RedirectToAction("Index", "Home");

		ViewBag.GoogleClientId = configuration["OAuth2Google:ClientId"];

		return View();
	}

	[HttpPost]
	[AllowAnonymous]
	public async Task<IActionResult> SignIn(LoginRequestDto dto)
	{
		var response = await authService.LoginAsync(dto);

		if (response.IsFailure)
		{
			TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty);
			ViewBag.GoogleClientId = configuration["OAuth2Google:ClientId"];
			logger.LogInformation("There was an unsuccessful login attempt");
			return View();
		}

		Response.Cookies.Append("token", response.Data?.Token ?? string.Empty, _cookieOptions);
		Response.Cookies.Append("refreshToken", response.Data?.RefreshToken ?? string.Empty, _cookieOptions);

		logger.LogInformation($"{User.Identity?.Name} logged into the system");

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

		var response = await userService.SendEmailResetPasswordAsync(email);

		if (response.IsFailure)
		{
			TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty);
			logger.LogInformation($"{email} tried to send password reset email and failed");
			return View();
		}

		TempData["Notification"] = MessageViewHelper.SuccessJson("Email enviado com sucesso");
		logger.LogInformation($"{email} tried to send password reset email");

		return View(model: email);
	}

	[AllowAnonymous]
	public async Task<IActionResult> ResetPassword(string code)
	{
		if (User.Identity != null && User.Identity.IsAuthenticated)
			return RedirectToAction("Index", "Home");

		var response = await userService.ValidateResetPasswordGuidAsync(new Guid(code));

		if (response.IsFailure)
		{
			TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty);
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

		var response = await userService.ResetPasswordAsync(dto);

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
		logger.LogInformation($"{User.Identity?.Name} logged out of the system");

		Response.Cookies.Delete("token");
		Response.Cookies.Delete("refreshToken");
		return RedirectToAction("Index", "Home");
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<IActionResult> Register(RegisterUserRequestDto dto)
	{
		var response = await userService.RegisterAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Usuário {dto.UserName} cadastrado com sucesso");

		logger.LogInformation($"{User.Identity?.Name} registered a new user");

		return RedirectToAction("Index");
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<IActionResult> Update(UpdateUserRequestDto dto)
	{
		var response = await userService.UpdateAsync(dto);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Usuário {dto.UserName} atualizado com sucesso");

		logger.LogInformation($"{User.Identity?.Name} updated user {dto.Id}");
		return RedirectToAction("Index");
	}

	[Authorize(Roles = "admin")]
	public async Task<IActionResult> Delete(string id)
	{
		var response = await userService.DeleteAsync(id);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson("Usuário deletado com sucesso");

		logger.LogInformation($"{User.Identity?.Name} removed user with id = {id}");

		return RedirectToAction("Index");
	}

	[AllowAnonymous]
	public IActionResult UserUnauthorized()
	{
		logger.LogInformation($"{User.Identity?.Name} tried to access a page for which he is not authorized");

		return View();
	}

	[AllowAnonymous]
	public async Task<IActionResult> RefreshSession(string? successRedirectUrl = null)
	{
		var refreshToken = Request.Cookies["refreshToken"];

		if (string.IsNullOrEmpty(refreshToken))
			return RedirectToAction("SignIn");

		var response = await authService.TryRefreshSessionAsync(refreshToken);

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
}

