using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.User.Enums;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Extensions;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Views.BackgroundJobQueue.Models;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class BackgroundJobQueueController(
    IBackgroundJobQueueService service,
    IUserService userService,
    IAuditLogService auditLogService) : Controller
{
    private const int limitResultsPage = 15;

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Index(int? currentPage, string? username, EBackgroundJobStatus? status)
    {
        var modelView = new BackgroundJobQueueModelView();
        var usernameSearch = User.Identity?.Name;

        if (User.IsInRole("admin"))
        {
            usernameSearch = username ?? User.Identity?.Name;
            var usersResult = await userService.GetAsync(currentPage: 1, limit: 1000, includeAdmin: true);
            if (usersResult.IsSuccess) modelView.Users = usersResult.Data?.Items.Where(u => u.Role != EUserRole.VISITOR) ?? [];
        }

        var jobsResult = await service.GetByFilterAsync(new GetBackgroundJobsByFilterRequestDto(
            order: EBackgroundJobOrderFilter.REGISTRATION_DATE_DESC,
            limitResults: limitResultsPage,
            currentPage: currentPage ?? 1,
            responsibleUsername: usernameSearch,
            status));

        if (jobsResult.IsSuccess)
        {
            modelView.Jobs = jobsResult.Data?.Items ?? [];
            ViewBag.CurrentPage = jobsResult.Data?.CurrentPage;
            ViewBag.TotalPages = jobsResult.Data?.TotalPageCount ?? 1;
            ViewBag.HasNextPage = jobsResult.Data?.HasNext;
            ViewBag.HasPrevPage = jobsResult.Data?.HasPrevious;
            ViewBag.Username = usernameSearch;
            ViewBag.Status = status != null ? $"{(int)status}" : string.Empty;

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
        var response = await service.AddJobInQueueAsync(dto);

        if (response.IsFailure) return BadRequest(response.Error.Description);

        string spreadsheetTypename = dto.Type switch
        {
            EBackgroundJobType.REGISTER_SPREADSHEET_MOVIES => "filmes",
            EBackgroundJobType.REGISTER_SPREADSHEET_SERIES => "séries",
            EBackgroundJobType.REGISTER_SPREADSHEET_CHANNELS => "canais",
            _ => "desconhecido"
        };

        await this.AddAuditLogAsync(
            auditLogService,
            "BackgroundJob",
            response.Data?.JobId ?? string.Empty,
            $"adicionou a planilha de {spreadsheetTypename} ({dto.SpreadsheetFile?.FileName}) na fila de processamento");

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
        var response = await service.AddJobInQueueAsync(dto);

        if (response.IsFailure) return BadRequest(response.Error.Description);

        await this.AddAuditLogAsync(
            auditLogService,
            "BackgroundJob",
            response.Data?.JobId ?? string.Empty,
            $"adicionou a importação de episódios da série {dto.SeriesTitle} na fila de processamento");

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Processo adicionado a fila com sucesso");

        return Ok(response.Data);
    }

    [HttpGet]
    [Authorize(Roles = "admin, common")]
    public async Task GetJobsNotification(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, HttpContext.RequestAborted);

        try
        {
            while (!linked.IsCancellationRequested)
            {
                var response = await service.GetJobsToNotifyAsync(username: User?.Identity?.Name ?? string.Empty);

                var payload = $"data: {JsonSerializer.Serialize(response.Data)}\n\n";
                await Response.WriteAsync(payload, linked.Token);
                await Response.Body.FlushAsync(linked.Token);

                await Task.Delay(TimeSpan.FromSeconds(5), linked.Token);
            }
        }
        catch (OperationCanceledException) { }
    }
}

