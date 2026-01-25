using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Channel.Enums;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Views.Channels.Models;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class ChannelsController(
  IChannelService _service,
  ILogger<ChannelsController> _logger,
  ISpreadsheetBatchImporter<IChannelService> _spreadsheetBatchImporter,
  IMediaDeliveryProfileService _mediaDeliveryProfileService) : Controller
{
    private const int limitResultsPage = 20;

    public async Task<IActionResult> Index(int? currentPage, EChannelSearchFilter? filter, string? search)
    {
        Result<PagedList<GetChannelResponseDto>>? result;

        _logger.LogInformation($"{User.Identity?.Name} accessed the channels page");

        if (filter is EChannelSearchFilter && !string.IsNullOrEmpty(search))
        {
            result = await _service.GetByFilterAsync(
              new GetChannelsByFilterRequestDto(
                filter,
                search,
                limitResultsPage,
                currentPage,
                isIncludeDisabled: true));

            ViewBag.Search = search;
            ViewBag.Filter = filter.ToString()?.ToLower();
        }
        else
        {
            result = await _service.GetAsync(currentPage ?? 1, limitResultsPage);
        }

        if (result.IsSuccess)
        {
            ViewBag.CurrentPage = result.Data?.CurrentPage;
            ViewBag.TotalPages = result.Data?.TotalPageCount ?? 1;
            ViewBag.HasNextPage = result.Data?.HasNext;
            ViewBag.HasPrevPage = result.Data?.HasPrevious;

            return View(result.Data?.Items);
        }

        return View(Enumerable.Empty<GetChannelResponseDto>());
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Form(string? id)
    {
        IEnumerable<GetMediaDeliveryProfileResponseDto> mediaDeliveryProfiles = [];
        var mediaProfilesResponse = await _mediaDeliveryProfileService.GetAllAsync(isIncludeDisabled: false);
        if (mediaProfilesResponse.IsSuccess) mediaDeliveryProfiles = mediaProfilesResponse.Data ?? [];
        
        if (id is not null)
        {
            var response = await _service.GetAsync(id);
            if (response.IsSuccess) return View(new ChannelFormModelView(response.Data, mediaDeliveryProfiles));
        }

        return View(new ChannelFormModelView(null, mediaDeliveryProfiles));
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Create(CreateChannelRequestDto dto)
    {
        var response = await _service.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Canal cadastrado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} registered the channel {dto.Title}");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Update(UpdateChannelRequestDto dto)
    {
        var response = await _service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Canal atualizado com sucesso");

        _logger.LogInformation($"{User.Identity?.Name} updated the channel {dto.Title}");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Delete(string? id)
    {
        if (id is not null)
        {
            var response = await _service.DeleteAsync(id);

            TempData["Notification"] = response.IsFailure
              ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
              : MessageViewHelper.SuccessJson($"Canal deletado com sucesso");

            _logger.LogInformation($"{User.Identity?.Name} removed the channel with id = {id}");
        }

        return RedirectToAction("Index");
    }

	[Authorize(Roles = "admin, common")]
	[HttpPost]
    public async Task<IActionResult> RegisterBySpreadsheet(IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest();

        var response = await _spreadsheetBatchImporter.ImportAsync(file);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        return Ok(response.Data);
    }

	[Authorize(Roles = "admin, common")]
	[HttpGet]
    public async Task<IActionResult> MonitorSpreadsheetRegistration(string importId)
    {
        var response = await _spreadsheetBatchImporter.MonitorImportAsync(importId);

        if (response.IsSuccess && response.Data?.ProgressCount == 100 && response.Data.SuccessCount > 1)
            TempData["Notification"] = MessageViewHelper
              .SuccessJson($"{response.Data.SuccessCount} canais cadastrados com sucesso");

        if (response.IsSuccess)
            return Ok(response.Data);

        return BadRequest(response.Error.Description ?? string.Empty);
    }
}

