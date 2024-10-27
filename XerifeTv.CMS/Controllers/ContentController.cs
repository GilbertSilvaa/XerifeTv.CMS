﻿using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Models.Content.Interfaces;

namespace XerifeTv.CMS.Controllers;

[Route("Api/Content")]
[ApiController]
public class ContentController(IContentService _service, ILogger<ContentController> _logger) : ControllerBase
{
  [HttpGet]
  [Route("Movies")]
  public async Task<IActionResult> Movies(int? limit)
  {
    var response = await _service.GetMoviesGroupByCategory(limit);
    _logger.LogInformation("Request Content API /Movies");

    return Ok(response.IsSuccess ? response.Data : []);
  }

  [HttpGet]
  [Route("Movies/{category}")]
  public async Task<IActionResult> MoviesCategory(string category, int? limit)
  {
    var response = await _service.GetMoviesByCategory(category, limit);
    _logger.LogInformation($"Request Content API /Movies/{category}");

    return Ok(response.IsSuccess ? response.Data : []);
  }

  [HttpGet]
  [Route("Series")]
  public async Task<IActionResult> Series(int? limit)
  {
    var response = await _service.GetSeriesGroupByCategory(limit);
    _logger.LogInformation("Request Content API /Series");

    return Ok(response.IsSuccess ? response.Data : []);
  }

  [HttpGet]
  [Route("Series/{category}")]
  public async Task<IActionResult> SeriesCategory(string category, int? limit)
  {
    var response = await _service.GetSeriesByCategory(category, limit);
    _logger.LogInformation($"Request Content API /Series/{category}");

    return Ok(response.IsSuccess ? response.Data : []);
  }

  [HttpGet]
  [Route("Series/Episodes/{serieId}/{season}")]
  public async Task<IActionResult> SeriesEpisodes(string serieId, int season)
  {
    var response = await _service.GetEpisodesSeriesBySeason(serieId, season);
    _logger.LogInformation($"Request Content API /Series/Episodes/{serieId}/{season}");

    return Ok(response.IsSuccess ? response.Data : []);
  }

  [HttpGet]
  [Route("Channels")]
  public async Task<IActionResult> Channels(int? limit)
  {
    var response = await _service.GetChannelsGroupByCategory(limit);
    _logger.LogInformation("Request Content API /Channels");

    return Ok(response.IsSuccess ? response.Data : []);
  }
}
