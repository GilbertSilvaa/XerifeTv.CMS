using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Models.Movie.Enums;
using XerifeTv.CMS.Models.Movie.Dtos.Request;
using XerifeTv.CMS.Models.Movie.Interfaces;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Movie.Dtos.Response;
using Microsoft.AspNetCore.Authorization;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class MoviesController(
  IMovieService _service, 
  ILogger<MoviesController> _logger,
  IConfiguration _configuration) : Controller
{
  private const int limitResultsPage = 15;

  public async Task<IActionResult> Index(int? currentPage, EMovieSearchFilter? filter, string? search)
  {
    Result<PagedList<GetMovieResponseDto>>? result;

    _logger.LogInformation($"{User.Identity?.Name} accessed the movies page");

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
    await _service.Create(dto);

    _logger.LogInformation($"{User.Identity?.Name} registered the movie {dto.Title}");

    return RedirectToAction("Index");
  }

  [Authorize(Roles = "admin, common")]
  public async Task<IActionResult> Update(UpdateMovieRequestDto dto)
  {
    await _service.Update(dto);

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
  public async Task<JsonResult> GetByImdbId(string id)
  {
    var client = new HttpClient();
    var url = $"https://api.themoviedb.org/3/movie/{id}";

    HttpResponseMessage response = await client.GetAsync(
      $"{url}?api_key={_configuration["Tmdb:Key"]}&language=pt-BR&page=1");

    return Json(response.IsSuccessStatusCode 
      ? response.Content.ReadAsStringAsync()
      : Enumerable.Empty<string>());
  }
}
