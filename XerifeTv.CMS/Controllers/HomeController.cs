using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Models.Dashboard.Dtos.Response;
using XerifeTv.CMS.Models.Dashboard.Interfaces;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class HomeController(IDashboardService _service, ILogger<HomeController> _logger) : Controller
{
  public async Task<IActionResult> Index()
  {
    var response = await _service.Get();

    _logger.LogInformation($"{User.Identity?.Name} accessed the dashboard page");

    if (response.IsSuccess) return View(response.Data);

    return View(new GetDashboardDataRequestDto(0, 0, 0));
  }
}