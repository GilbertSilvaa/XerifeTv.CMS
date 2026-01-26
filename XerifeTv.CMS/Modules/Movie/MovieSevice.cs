using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Webhook.Enums;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Modules.Movie.Dtos.Request;
using XerifeTv.CMS.Modules.Movie.Dtos.Response;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Movie.Specifications;

namespace XerifeTv.CMS.Modules.Movie;

public sealed class MovieSevice(
    IMovieRepository _repository,
    IWebhookService _webhookService) : IMovieService
{
    public async Task<Result<PagedList<GetMovieResponseDto>>> GetAsync(int currentPage, int limit)
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

    public async Task<Result<GetMovieResponseDto?>> GetAsync(string id)
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

    public async Task<Result<GetMovieResponseDto?>> GetByImdbIdAsync(string imdbId)
    {
        try
        {
            var response = await _repository.GetByImdbIdAsync(imdbId);

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

    public async Task<Result<string>> CreateAsync(CreateMovieRequestDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            var imdbIdSpec = new UniqueImdbIdSpecification(_repository);

            if (!await imdbIdSpec.IsSatisfiedByAsync(entity))
                return Result<string>.Failure(
                  new Error("409", $"Filme nao cadastrado. Imdb ID {entity.ImdbId} duplicado"));

            var response = await _repository.CreateAsync(entity);

            _ = Task.Run(() => _webhookService.DispacthWebhooksByTriggerEventAsync(EWebhookTriggerEvent.MOVIE_PUBLISHED, response));

            return Result<string>.Success(response);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }

    public async Task<Result<string>> UpdateAsync(UpdateMovieRequestDto dto)
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

    public async Task<Result<bool>> DeleteAsync(string id)
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

    public async Task<Result<PagedList<GetMovieResponseDto>>> GetByFilterAsync(GetMoviesByFilterRequestDto dto)
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
}
