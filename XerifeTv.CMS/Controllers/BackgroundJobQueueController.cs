using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Response;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class BackgroundJobQueueController(IBackgroundJobQueueService _service, IUserService _userService) : Controller
{
	private const int limitResultsPage = 15;

	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> Index(int? currentPage, string? username, EBackgroundJobStatus? status)
	{
		var modelView = new BackgroundJobQueueModelView();

		if (User.IsInRole("admin"))
		{
			var usersResult = await _userService.GetAsync(currentPage: 1, limit: 1000);
			if (usersResult.IsSuccess) modelView.Users = usersResult.Data?.Items ?? [];
		}
		else
			username = User.Identity?.Name;

		var jobsResult = await _service.GetByFilterAsync(new GetBackgroundJobsByFilterRequestDto(
			order: EBackgroundJobOrderFilter.REGISTRATION_DATE_DESC,
			limitResults: limitResultsPage,
			currentPage: currentPage ?? 1,
			responsibleUsername: username ?? User.Identity?.Name,
			status));

		if (jobsResult.IsSuccess)
		{
			modelView.Jobs = jobsResult.Data?.Items ?? [];
			ViewBag.CurrentPage = jobsResult.Data?.CurrentPage;
			ViewBag.TotalPages = jobsResult.Data?.TotalPageCount ?? 1;
			ViewBag.HasNextPage = jobsResult.Data?.HasNext;
			ViewBag.HasPrevPage = jobsResult.Data?.HasPrevious;
			ViewBag.Username = username ?? User.Identity?.Name;

			return View(modelView);
		}

		TempData["Notification"] = MessageViewHelper.ErrorJson(jobsResult.Error.Description ?? string.Empty);

		return View(modelView);
	}

	[HttpPost]
	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> AddJobInQueueSpreadsheetRegisters(AddSpreadsheetJobQueueRequestDto dto)
	{
		dto.RequestedByUsername = User?.Identity?.Name ?? string.Empty;
		var response = await _service.AddJobInQueueAsync(dto);

		if (response.IsFailure) return BadRequest(response.Error.Description);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Processo adicionado a fila com sucesso");

		return Ok(response.Data);
	}

	[HttpPost]
	[Authorize(Roles = "admin, common")]
	public async Task<IActionResult> AddJobInQueueImportEpisodesSeries(AddImportEpisodesJobQueueRequestDto dto)
	{
		dto.RequestedByUsername = User?.Identity?.Name ?? string.Empty;
		var response = await _service.AddJobInQueueAsync(dto);

		if (response.IsFailure) return BadRequest(response.Error.Description);

		TempData["Notification"] = response.IsFailure
		  ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
		  : MessageViewHelper.SuccessJson($"Processo adicionado a fila com sucesso");

		return Ok(response.Data);
	}
}
