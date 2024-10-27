using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Channel.Dtos.Request;
using XerifeTv.CMS.Models.Channel.Dtos.Response;
using XerifeTv.CMS.Models.Channel.Enums;
using XerifeTv.CMS.Models.Channel.Interfaces;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class ChannelsController(IChannelService _service, ILogger<ChannelsController> _logger) : Controller
{
  private const int limitResultsPage = 15;

  public async Task<IActionResult> Index(int? currentPage, EChannelSearchFilter? filter, string? search)
  {
    Result<PagedList<GetChannelResponseDto>>? result;

    _logger.LogInformation($"{User.Identity?.Name} accessed the channels page");

    if (filter is EChannelSearchFilter && !string.IsNullOrEmpty(search))
    {
      result = await _service.GetByFilter(
        new GetChannelsByFilterRequestDto(filter, search, limitResultsPage, currentPage));

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
    await _service.Create(dto);

    _logger.LogInformation($"{User.Identity?.Name} registered the channel {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Update(UpdateChannelRequestDto dto)
  {
    await _service.Update(dto);

    _logger.LogInformation($"{User.Identity?.Name} updated the channel {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Delete(string? id)
  {
    if (id is not null)
    {
      await _service.Delete(id);
      _logger.LogInformation($"{User.Identity?.Name} removed the channel with id = {id}");
    }
    
    return RedirectToAction("Index");
  }
}

