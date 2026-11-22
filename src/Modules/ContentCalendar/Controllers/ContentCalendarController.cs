using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.ContentCalendar.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCalendar.Enums;
using XerifeTv.CMS.Modules.ContentCalendar.Interfaces;
using XerifeTv.CMS.Modules.ContentCalendar.Services;
using XerifeTv.CMS.Modules.Integrations.Tmdb.Services;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.ContentCalendar.Controllers;

[Authorize(Roles = "admin, common")]
public class ContentCalendarController(
    IContentScheduleService service,
    ITmdbService tmdbService,
    IBusinessClock clock,
    ILogger<ContentCalendarController> logger) : Controller
{
    public async Task<IActionResult> Index(int? year, int? month, string? view)
    {
        var today = clock.Today;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;

        if (selectedMonth is < 1 or > 12)
        {
            selectedYear = today.Year;
            selectedMonth = today.Month;
        }

        var start = new DateOnly(selectedYear, selectedMonth, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var response = await service.GetByPeriodAsync(start, end);

        if (response.IsFailure)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson(
                response.Error.Description ?? "Não foi possível carregar o calendário.");
            ViewBag.Schedules = Array.Empty<Dtos.Response.GetContentScheduleResponseDto>();
        }
        else
        {
            ViewBag.Schedules = response.Data ?? [];
        }

        ViewBag.CurrentYear = selectedYear;
        ViewBag.CurrentMonth = selectedMonth;
        ViewBag.ViewMode = view == "list" ? "list" : "calendar";
        ViewBag.Today = today;
        ViewBag.PreviousYear = selectedMonth == 1 ? selectedYear - 1 : selectedYear;
        ViewBag.PreviousMonth = selectedMonth == 1 ? 12 : selectedMonth - 1;
        ViewBag.NextYear = selectedMonth == 12 ? selectedYear + 1 : selectedYear;
        ViewBag.NextMonth = selectedMonth == 12 ? 1 : selectedMonth + 1;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateContentScheduleRequestDto dto)
    {
        var response = await service.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? "Não foi possível criar o agendamento.")
            : MessageViewHelper.SuccessJson($"Agendamento de {dto.Title} criado com sucesso.");

        if (response.IsSuccess)
            logger.LogInformation(
                "{User} created content schedule for {ContentType} TMDB {TmdbId}",
                User.Identity?.Name, dto.ContentType, dto.TmdbId);

        return RedirectToAction(nameof(Index), new
        {
            year = dto.ScheduledDate.Year,
            month = dto.ScheduledDate.Month
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDate(UpdateContentScheduleDateRequestDto dto)
    {
        var response = await service.UpdateDateAsync(dto);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? "Não foi possível alterar a data.")
            : MessageViewHelper.SuccessJson("Data prevista atualizada com sucesso.");

        return RedirectToAction(nameof(Index), new
        {
            year = dto.ScheduledDate.Year,
            month = dto.ScheduledDate.Month
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var response = await service.DeleteAsync(id);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? "Não foi possível excluir o agendamento.")
            : MessageViewHelper.SuccessJson("Agendamento excluído com sucesso.");

        if (response.IsSuccess)
            logger.LogInformation("{User} deleted content schedule {ScheduleId}", User.Identity?.Name, id);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(string id)
    {
        var response = await service.GetDetailsAsync(id);

        if (response.IsFailure)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson(
                response.Error.Description ?? "Agendamento não encontrado.");
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Link(LinkContentScheduleRequestDto dto)
    {
        var response = await service.LinkAsync(dto);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? "Não foi possível vincular o conteúdo.")
            : MessageViewHelper.SuccessJson("Conteúdo vinculado e agendamento concluído.");

        return RedirectToAction(nameof(Details), new { id = dto.Id });
    }

    public async Task<IActionResult> Shortcut(string id)
    {
        var response = await service.GetShortcutAsync(id);

        return Json(new
        {
            isSuccess = response.IsSuccess,
            url = response.Data?.Url,
            warningMessage = response.Data?.WarningMessage,
            error = response.Error.Description
        });
    }

    public async Task<IActionResult> SearchMovies(string? query, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new { isSuccess = true, data = Array.Empty<object>() });

        var response = await tmdbService.SearchMoviesAsync(query, page);

        return Json(new
        {
            isSuccess = response.IsSuccess,
            data = response.Data,
            error = response.Error.Description
        });
    }

    public async Task<IActionResult> SearchSeries(string? query, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new { isSuccess = true, data = Array.Empty<object>() });

        var response = await tmdbService.SearchSeriesAsync(query, page);

        return Json(new
        {
            isSuccess = response.IsSuccess,
            data = response.Data,
            error = response.Error.Description
        });
    }

    public async Task<IActionResult> GetSeriesDetails(int tmdbId)
    {
        if (tmdbId <= 0)
            return Json(new { isSuccess = false, error = "Parâmetros inválidos." });

        var response = await tmdbService.GetSeriesByTmdbIdAsync(tmdbId);

        if (response.IsFailure || response.Data is null)
            return Json(new
            {
                isSuccess = false,
                error = response.Error.Description
            });

        return Json(new
        {
            isSuccess = true,
            numberOfSeasons = response.Data.NumberSeasons,
            title = response.Data.Title
        });
    }

    public async Task<IActionResult> GetEpisodes(int tmdbId, int season)
    {
        if (tmdbId <= 0 || season < 0)
            return Json(new { isSuccess = false, data = Array.Empty<object>(), error = "Parâmetros inválidos." });

        var response = await tmdbService.GetSeriesEpisodesByTmdbIdAsync(tmdbId, season);

        if (response.IsFailure)
            return Json(new
            {
                isSuccess = false,
                data = Array.Empty<object>(),
                error = response.Error.Description
            });

        if (response.Data is null)
            return Json(new { isSuccess = true, data = Array.Empty<object>() });

        var episodes = response.Data.Episodes.Select(episode => new
        {
            episode.EpisodeNumber,
            episode.SeasonNumber,
            episode.Name,
            episode.BannerUrl,
            episode.AirDate
        });

        return Json(new { isSuccess = true, data = episodes });
    }
}
