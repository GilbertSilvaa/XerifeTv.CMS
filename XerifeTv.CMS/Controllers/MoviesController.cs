using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XerifeTv.CMS.Modules.Movie.Enums;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class MoviesController(IMovieService _service, ILogger<MoviesController> _logger) : Controller
{
  private const int limitResultsPage = 20;

  public async Task<IActionResult> Index(int? currentPage, EMovieSearchFilter? filter, string? search)
  {
    Result<PagedList<GetMovieResponseDto>>? result;

    _logger.LogInformation($"{User.Identity?.Name} accessed the movies page");

    if (TempData["ErrorMessage"] is string errorMessage)
    {
      ViewData["Message"] = new MessageView(
        EMessageViewType.ERROR,
        errorMessage);
    }

    if (filter is EMovieSearchFilter && !string.IsNullOrEmpty(search))
    {
      result = await _service.GetByFilter(
        new GetMoviesByFilterRequestDto(
          filter, 
          EMovieOrderFilter.TITLE, 
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

    return View(Enumerable.Empty<GetMovieResponseDto>());
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
  public async Task<IActionResult> Create(CreateMovieRequestDto dto)
  {
    var response = await _service.Create(dto);

    if (response.IsFailure)
      TempData["ErrorMessage"] = response.Error.Description ?? string.Empty;

    _logger.LogInformation($"{User.Identity?.Name} registered the movie {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Update(UpdateMovieRequestDto dto)
  {
    var response = await _service.Update(dto);
    
    if (response.IsFailure)
      TempData["ErrorMessage"] = response.Error.Description ?? string.Empty;

    _logger.LogInformation($"{User.Identity?.Name} updated the movie {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Delete(string? id)
  {
    if (id is not null)
    {
      await _service.Delete(id);
      _logger.LogInformation($"{User.Identity?.Name} removed the movie with id = {id}");
    }
   
    return RedirectToAction("Index");
  }

  [HttpGet]
  public async Task<IActionResult> GetByImdbId(string imdbId)
  {
    if (string.IsNullOrEmpty(imdbId)) return BadRequest();
    
    var response = await _service.GetByImdbId(imdbId);
    
    return response.IsFailure ? BadRequest() : Ok(response.Data);
  }

  [HttpPost]
  public async Task<IActionResult> RegisterBySpreadsheet(IFormFile file)
  {
    if (file is null || file.Length == 0) return BadRequest();

    var response = await _service.RegisterBySpreadsheet(file);

    if (response.IsFailure) 
      return BadRequest(response.Error.Description ?? string.Empty);

    object responseData = new
    {
			response.Data.SuccessCount,
      response.Data.FailCount,
      response.Data.ErrorList
    };
    
    return Ok(responseData);
  }
}
