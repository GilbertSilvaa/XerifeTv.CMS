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
}
