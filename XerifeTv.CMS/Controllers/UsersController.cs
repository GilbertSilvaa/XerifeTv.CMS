using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;
using XerifeTv.CMS.Modules.User.Interfaces;

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
  public async Task<IActionResult> Index(MessageView? messageView)
  {
    ViewData["Message"] = messageView;

    var response = await _service.Get(1, 20);

    _logger.LogInformation($"{User.Identity?.Name} accessed the users page");

    if (response.IsSuccess)
      return View(response.Data?.Items);

    return View(Enumerable.Empty<GetUserResponseDto>());
  }

  [AllowAnonymous]
  public IActionResult SignIn()
  {
    if (User.Identity.IsAuthenticated) 
      return RedirectToAction("Index", "Home");

    return View();
  }

  [AllowAnonymous]
  public IActionResult EmailResetPasswordForm()
  {
    if (User.Identity.IsAuthenticated) 
      return RedirectToAction("Index", "Home");
    
    return View();
  }

  [HttpPost]
  [AllowAnonymous]
  public async Task<IActionResult> SignIn(LoginUserRequestDto dto)
  {
    var response = await _service.Login(dto);

    if (response.IsFailure)
    {
      ViewData["Message"] = new MessageView(
        EMessageViewType.ERROR, response.Error.Description ?? string.Empty);

      _logger.LogInformation("There was an unsuccessful login attempt");

      return View();
    }
    
    Response.Cookies.Append("token", response.Data?.Token ?? string.Empty, _cookieOptions);
    Response.Cookies.Append("refreshToken", response.Data?.RefreshToken ?? string.Empty, _cookieOptions);

    _logger.LogInformation($"{User.Identity?.Name} logged into the system");

    return RedirectToAction("Index", "Home");
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
    var response = await _service.Register(dto);

    if (response.IsFailure)
      return RedirectToAction("Index", new MessageView(
        EMessageViewType.ERROR, response.Error.Description ?? string.Empty));

    _logger.LogInformation($"{User.Identity?.Name} registered a new user");

    return RedirectToAction("Index");
  }
  
  [HttpPost]
  [Authorize]
  public async Task<IActionResult> UpdateProfile(UpdateUserRequestDto dto)
  {
    var response = await _service.Update(dto);

    if (response.IsFailure)
      return RedirectToAction("Settings", new MessageView(
        EMessageViewType.ERROR, response.Error.Description ?? string.Empty));
    
    _logger.LogInformation($"{User.Identity?.Name} updated your own profile");
    return RedirectToAction("Settings"); 
  }

  [HttpPost]
  [Authorize]
  public async Task<IActionResult> UpdatePassword(UpdatePasswordUserRequestDto dto)
  {
    if (dto.NewPassword != dto.NewPasswordConfirm) 
      return RedirectToAction("Settings", new MessageView(
        EMessageViewType.ERROR, "Confirmacao de senha incorreta"));
    
    var response = await _service.UpdatePassword(dto);
    
    if (response.IsFailure)
      return RedirectToAction("Settings", new MessageView(
        EMessageViewType.ERROR, response.Error.Description ?? string.Empty));
    
    _logger.LogInformation($"{User.Identity?.Name} updated your password");
    
    return RedirectToAction("Settings", new MessageView(
      EMessageViewType.SUCCESS, "Senha atualizada com sucesso")); 
  }
  
  [HttpPost]
  [Authorize(Roles = "admin")]
  public async Task<IActionResult> Update(UpdateUserRequestDto dto)
  {
    var response = await _service.Update(dto);

    if (response.IsFailure)
      return RedirectToAction("Index", new MessageView(
        EMessageViewType.ERROR, response.Error.Description ?? string.Empty));
    
    _logger.LogInformation($"{User.Identity?.Name} updated user {dto.Id}");
    return RedirectToAction("Index");
  }
  
  [Authorize(Roles = "admin")]
	public async Task<IActionResult> Delete(string id)
  {
    await _service.Delete(id);

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
    
    var response = await _service.TryRefreshSession(refreshToken);
    
    if (response.IsFailure) 
      return RedirectToAction("SignIn");
    
    var (newToken,newRefreshToken) = response.Data;

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
  public async Task<IActionResult> Settings(MessageView? messageView)
  {
    ViewData["Message"] = messageView;
    
    var response = await _service.GetByUsername(User.Identity.Name);

    if (response.IsFailure) return Logout();
    
    return View(response.Data);
  }
}