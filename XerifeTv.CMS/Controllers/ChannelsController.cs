using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Channel.Enums;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Shared.Extensions;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Views.Channels.Models;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class ChannelsController(
  IChannelService service,
  ILogger<ChannelsController> logger,
  ISpreadsheetBatchImporter<IChannelService> spreadsheetBatchImporter,
  IMediaDeliveryProfileService mediaDeliveryProfileService,
  IAuditLogService auditLogService) : Controller
{
    private const int limitResultsPage = 20;

    public async Task<IActionResult> Index(int? currentPage, EChannelSearchFilter? filter, string? search)
    {
        Result<PagedList<GetChannelResponseDto>>? result;

        logger.LogInformation($"{User.Identity?.Name} accessed the channels page");

        if (filter is EChannelSearchFilter && !string.IsNullOrEmpty(search))
        {
            result = await service.GetByFilterAsync(
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
            result = await service.GetAsync(currentPage ?? 1, limitResultsPage);
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
        var mediaProfilesResponse = await mediaDeliveryProfileService.GetAllAsync(isIncludeDisabled: false);
        if (mediaProfilesResponse.IsSuccess) mediaDeliveryProfiles = mediaProfilesResponse.Data ?? [];
        
        if (id is not null)
        {
            var response = await service.GetAsync(id);
            if (response.IsSuccess) return View(new ChannelFormModelView(response.Data, mediaDeliveryProfiles));
        }

        return View(new ChannelFormModelView(null, mediaDeliveryProfiles));
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Create(CreateChannelRequestDto dto)
    {
        var response = await service.CreateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Canal cadastrado com sucesso");

        logger.LogInformation($"{User.Identity?.Name} registered the channel {dto.Title}");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "Channel", response.Data ?? string.Empty, $"adicionou o canal {dto.Title}");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Update(UpdateChannelRequestDto dto)
    {
        var response = await service.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Canal atualizado com sucesso");

        logger.LogInformation($"{User.Identity?.Name} updated the channel {dto.Title}");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "Channel", dto.Id, $"atualizou o canal {dto.Title}");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> Delete(string? id)
    {
        if (id is not null)
        {
            var channelResponse = await service.GetAsync(id);
            var title = channelResponse.IsSuccess && channelResponse.Data is not null
                ? channelResponse.Data.Title
                : id;

            var response = await service.DeleteAsync(id);

            TempData["Notification"] = response.IsFailure
              ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
              : MessageViewHelper.SuccessJson($"Canal deletado com sucesso");

            logger.LogInformation($"{User.Identity?.Name} removed the channel with id = {id}");

            if (response.IsSuccess)
                await this.AddAuditLogAsync(auditLogService, "Channel", id, $"removeu o canal {title}");
        }

        return RedirectToAction("Index");
    }

	[Authorize(Roles = "admin, common")]
	[HttpPost]
    public async Task<IActionResult> RegisterBySpreadsheet(IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest();

        var response = await spreadsheetBatchImporter.ImportAsync(file);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        await this.AddAuditLogAsync(auditLogService, "Channel", file.FileName, $"iniciou a importação de canais da planilha {file.FileName}");

        return Ok(response.Data);
    }

	[Authorize(Roles = "admin, common")]
	[HttpGet]
    public async Task<IActionResult> MonitorSpreadsheetRegistration(string importId)
    {
        var response = await spreadsheetBatchImporter.MonitorImportAsync(importId);

        if (response.IsSuccess && response.Data?.ProgressCount == 100 && response.Data.SuccessCount > 1)
            TempData["Notification"] = MessageViewHelper
              .SuccessJson($"{response.Data.SuccessCount} canais cadastrados com sucesso");

        if (response.IsSuccess)
            return Ok(response.Data);

        return BadRequest(response.Error.Description ?? string.Empty);
    }
}


