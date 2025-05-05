using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Channel.Enums;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class ChannelsController(IChannelService _service, ILogger<ChannelsController> _logger) : Controller
{
  private const int limitResultsPage = 20;

  public async Task<IActionResult> Index(int? currentPage, EChannelSearchFilter? filter, string? search)
  {
    Result<PagedList<GetChannelResponseDto>>? result;

    _logger.LogInformation($"{User.Identity?.Name} accessed the channels page");

    if (filter is EChannelSearchFilter && !string.IsNullOrEmpty(search))
    {
      result = await _service.GetByFilter(
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
      result = await _service.Get(currentPage ?? 1, limitResultsPage);
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
    if (id is not null)
    {
      var response = await _service.Get(id);
      if (response.IsSuccess) return View(response.Data);
    }

    return View();
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Create(CreateChannelRequestDto dto)
  {
    var response = await _service.Create(dto);
    
    TempData["Notification"] = response.IsFailure
      ? MessageViewHelper.ErrorJson(response.Error.Description)
      : MessageViewHelper.SuccessJson($"Canal cadastrado com sucesso");

    _logger.LogInformation($"{User.Identity?.Name} registered the channel {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Update(UpdateChannelRequestDto dto)
  {
    var response = await _service.Update(dto);
    
    TempData["Notification"] = response.IsFailure
      ? MessageViewHelper.ErrorJson(response.Error.Description)
      : MessageViewHelper.SuccessJson($"Canal atualizado com sucesso");

    _logger.LogInformation($"{User.Identity?.Name} updated the channel {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Delete(string? id)
  {
    if (id is not null)
    {
      var response = await _service.Delete(id);
      
      TempData["Notification"] = response.IsFailure
        ? MessageViewHelper.ErrorJson(response.Error.Description)
        : MessageViewHelper.SuccessJson($"Canal deletado com sucesso");
      
      _logger.LogInformation($"{User.Identity?.Name} removed the channel with id = {id}");
    }
    
    return RedirectToAction("Index");
  }
  
  [HttpPost]
  public async Task<IActionResult> RegisterBySpreadsheet(IFormFile file)
  {
    if (file is null || file.Length == 0) return BadRequest();
    
    return Ok();
  }

  [HttpGet]
  public async Task<IActionResult> MonitorSpreadsheetRegistration(string importId)
  {
    return Ok();
  }
}

