using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Dashboard.Dtos.Response;
using XerifeTv.CMS.Modules.Dashboard.Interfaces;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class HomeController(IDashboardService service, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var response = await service.GetAsync(!User.IsInRole("admin") ? User.Identity?.Name : null);

        logger.LogInformation($"{User.Identity?.Name} accessed the dashboard page");

        if (response.IsSuccess) return View(response.Data);

        return View(new GetDashboardDataRequestDto([], [], []));
    }
}
