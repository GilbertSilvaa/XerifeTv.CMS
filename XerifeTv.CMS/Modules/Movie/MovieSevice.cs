using Microsoft.VisualBasic;
using OfficeOpenXml;
using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Movie.Specifications;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace XerifeTv.CMS.Modules.Movie;

public sealed class MovieSevice(
  IMovieRepository _repository,
  ISpreadsheetReaderService _spreadsheetReaderService,
  IConfiguration _configuration) : IMovieService
{
  private static int _registerBySpreadsheetProgress = 0;
  
  public async Task<Result<PagedList<GetMovieResponseDto>>> Get(int currentPage, int limit)
  {
    try
    {
      var response = await _repository.GetAsync(currentPage, limit);

      var result = new PagedList<GetMovieResponseDto>(
        response.CurrentPage, 
        response.TotalPageCount, 
        response.Items.Select(GetMovieResponseDto.FromEntity));

      return Result<PagedList<GetMovieResponseDto>>.Success(result);
    }
    catch (Exception ex) 
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetMovieResponseDto>>.Failure(error);
    }
  }

  public async Task<Result<GetMovieResponseDto?>> Get(string id)
  {
    try
    {
      var response = await _repository.GetAsync(id);

      if (response is null) 
        return Result<GetMovieResponseDto?>
          .Failure(new Error("404", "Conteudo nao encontrado"));

      return Result<GetMovieResponseDto?>
        .Success(GetMovieResponseDto.FromEntity(response));
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<GetMovieResponseDto?>.Failure(error);
    }
  }

  public async Task<Result<string>> Create(CreateMovieRequestDto dto)
  {
    try
    {
      var entity = dto.ToEntity();
      var imdbIdSpec = new UniqueImdbIdSpecification(_repository);
      
      if (!await imdbIdSpec.IsSatisfiedByAsync(entity))
        return Result<string>.Failure(
          new Error("409", $"Filme nao cadastrado. Imdb ID {entity.ImdbId} duplicado"));

      var response = await _repository.CreateAsync(entity);
      return Result<string>.Success(response);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<string>.Failure(error);
    }
  }

  public async Task<Result<string>> Update(UpdateMovieRequestDto dto)
  {
    try
    {
      var entity = dto.ToEntity();
      var response = await _repository.GetAsync(entity.Id);

      if (response is null)
        return Result<string>.Failure(new Error("404", "Conteudo nao encontrado"));
      
      var imdbIdSpec = new UniqueImdbIdSpecification(_repository);

      if (!await imdbIdSpec.IsSatisfiedByAsync(entity))
          return Result<string>.Failure(
            new Error("409", $"Filme nao atualizado. Imdb ID {entity.ImdbId} duplicado"));

      entity.CreateAt = response.CreateAt;
      await _repository.UpdateAsync(entity);
      return Result<string>.Success(entity.Id);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<string>.Failure(error);
    }
  }

  public async Task<Result<bool>> Delete(string id)
  {
    try
    {
      var response = await _repository.GetAsync(id);

      if (response is null)
        return Result<bool>.Failure(new Error("404", "Conteudo nao encontrado"));

      await _repository.DeleteAsync(id);
      return Result<bool>.Success(true);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<bool>.Failure(error);
    }
  }

  public async Task<Result<PagedList<GetMovieResponseDto>>> GetByFilter(GetMoviesByFilterRequestDto dto)
  {
    try
    {
      var response = await _repository.GetByFilterAsync(dto);

      var result = new PagedList<GetMovieResponseDto>(
        response.CurrentPage,
        response.TotalPageCount,
        response.Items.Select(GetMovieResponseDto.FromEntity));

      return Result<PagedList<GetMovieResponseDto>>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetMovieResponseDto>>.Failure(error);
    }
  }

  public async Task<Result<GetMovieByImdbResponseDto?>> GetByImdbId(string imdbId)
  {
    try
    {
      var client = new HttpClient();
      var url = $"https://api.themoviedb.org/3/movie/{imdbId}";
      var tmdbKey = _configuration["Tmdb:Key"];

			var response = await client.GetAsync($"{url}?api_key={tmdbKey}&language=pt-BR&page=1");
      
      if (!response.IsSuccessStatusCode) 
        return Result<GetMovieByImdbResponseDto?>.Failure(
          new Error("400", $"[{imdbId}] IMDB API retornou: {response.ReasonPhrase}"));
      
      var responseJsonString = await response.Content.ReadAsStringAsync();
      var result = JsonConvert.DeserializeObject<GetMovieByImdbResponseDto>(responseJsonString);

      if (result is null)
        return Result<GetMovieByImdbResponseDto?>.Failure(
          new Error("404", $"Imdb ID: {imdbId} invalido"));

      return Result<GetMovieByImdbResponseDto?>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<GetMovieByImdbResponseDto?>.Failure(error);
    }
  }

  public async Task<Result<(int? SuccessCount, int? FailCount, string[] ErrorList)>> RegisterBySpreadsheet(IFormFile file)
  {
    try
    {
      string[] expectedColluns =
      [
        "IMDB ID (REQUIRED)",
        "PARENTAL RATING (REQUIRED)",
        "URL VIDEO (REQUIRED)",
        "STREAM FORMAT (REQUIRED)",
        "DURATION (REQUIRED)",
        "URL SUBTITLES"
      ];

      using var stream = new MemoryStream();
      file.CopyTo(stream);

      int successCount = 0;
      int failCount = 0;
      ICollection<string> errorList = [];

      var spreadsheetResponse = _spreadsheetReaderService.Read(expectedColluns, stream);
      ICollection<SpreadsheetMovieResponseDto> movieList = [];

      foreach (var item in spreadsheetResponse)
        try
        {
          var spreadsheetMovieDto = SpreadsheetMovieResponseDto.FromCollunsStr(item);
          movieList.Add(spreadsheetMovieDto);
        }
        catch (SpreadsheetInvalidException ex)
        {
          failCount++;
          errorList.Add(ex.Message);
          _registerBySpreadsheetProgress =
            (int)(((float)(failCount + successCount) / spreadsheetResponse.Length) * 100);
        }

      foreach (var movieItem in movieList)
      {
        var movieByImdbresponse = await GetByImdbId(movieItem.ImdbId);

        if (movieByImdbresponse.IsFailure)
        {
          failCount++;
          errorList.Add(movieByImdbresponse.Error.Description ?? string.Empty);
          _registerBySpreadsheetProgress =
            (int)(((float)(failCount + successCount) / spreadsheetResponse.Length) * 100);
          continue;
        }

        var createMovieDto = new CreateMovieRequestDto
        {
          ImdbId = movieItem.ImdbId,
          Title = movieByImdbresponse.Data?.Title ?? string.Empty,
          Synopsis = movieByImdbresponse.Data?.Overview ?? string.Empty,
          Categories = String.Join(", ", movieByImdbresponse.Data?.Genres.Select(g => g.Name.ToLower())),
          PosterUrl = movieByImdbresponse.Data?.PosterUrl ?? string.Empty,
          BannerUrl = movieByImdbresponse.Data?.BannerUrl ?? string.Empty,
          ReleaseYear = int.Parse(movieByImdbresponse.Data?.ReleaseYear ?? "0"),
          Review = movieByImdbresponse.Data?.VoteAverage ?? 0,
          ParentalRating = movieItem.ParentalRating,
          VideoUrl = movieItem.Video?.Url ?? string.Empty,
          VideoDuration = movieItem.Video?.Duration ?? 0,
          VideoStreamFormat = movieItem.Video?.StreamFormat ?? string.Empty,
          VideoSubtitle = movieItem.Video?.Subtitle
        };

        var response = await Create(createMovieDto);

        if (response.IsSuccess)
          successCount++;
        else
        {
          failCount++;
          errorList.Add(response.Error?.Description ?? string.Empty);
        }

        _registerBySpreadsheetProgress = (int)(((float)(failCount + successCount) / spreadsheetResponse.Length) * 100);
      }

      return Result<(int?, int?, string[])>.Success((successCount, failCount, errorList.ToArray()));
    }
    catch (SpreadsheetInvalidException ex)
    {
      var error = new Error("400", ex.InnerException?.Message ?? ex.Message);
      return Result<(int?, int?, string[])>.Failure(error);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<(int?, int?, string[])>.Failure(error);
    }
    finally
    {
      _registerBySpreadsheetProgress = 0;
    }
  }

  public async Task<Result<int>> MonitorSpreadsheetRegistration()
  {
    await Task.CompletedTask;
    
    return Result<int>.Success(_registerBySpreadsheetProgress);
  }
}
