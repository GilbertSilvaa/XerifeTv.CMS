using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.User.Dtos.Request;
using XerifeTv.CMS.Models.User.Dtos.Response;
using XerifeTv.CMS.Models.User.Interfaces;

namespace XerifeTv.CMS.Controllers;

[Authorize(Roles = "admin")]
public class UsersController(IUserService _service) : Controller
{
  public async Task<IActionResult> Index(MessageView? messageView)
  {
    ViewData["Message"] = messageView;

    var response = await _service.Get(1, 20);

    if (response.IsSuccess)
      return View(response.Data?.Items);

    return View(Enumerable.Empty<GetUserRequestDto>());
  }

  [AllowAnonymous]
  public IActionResult SignIn()
  {
    if (User.Identity is null) return View();

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
        EMessageViewType.ERROR,
        response.Error.Description ?? string.Empty);

      return View();
    }

    var cookieOptions = new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      SameSite = SameSiteMode.Strict,
      Expires = DateTime.UtcNow.AddDays(30)
    };

    var token = response.Data?.Token ?? string.Empty;
    Response.Cookies.Append("token", token, cookieOptions);

    return RedirectToAction("Index", "Home");
  }

  [AllowAnonymous]
  public IActionResult Logout()
  {
    Response.Cookies.Delete("token");
    return RedirectToAction("Index", "Home");
  }

  public async Task<IActionResult> Register(RegisterUserRequestDto dto)
  {
    var response = await _service.Register(dto);

    if (response.IsFailure)
      return RedirectToAction("Index", new MessageView(
        EMessageViewType.ERROR,
        response.Error.Description ?? string.Empty));

    return RedirectToAction("Index");
  }

  public async Task<IActionResult> Delete(string id)
  {
    await _service.Delete(id);
    return RedirectToAction("Index");
  }

  [AllowAnonymous]
  public IActionResult UserUnauthorized() => View();
}