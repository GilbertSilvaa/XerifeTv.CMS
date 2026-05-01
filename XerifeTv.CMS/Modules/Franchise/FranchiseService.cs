using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Franchise.Dtos.Request;
using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Franchise;

public class FranchiseService(
    IFranchiseRepository _repository,
    IMovieRepository _movieRepository,
    ISeriesRepository _seriesRepository) : IFranchiseService
{
    public async Task<Result<IEnumerable<GetFranchiseResponseDto>>> GetAllAsync()
    {
        try
        {
            var response = await _repository.GetAllAsync();

            return Result<IEnumerable<GetFranchiseResponseDto>>.Success(
                response.Select(GetFranchiseResponseDto.FromEntity));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<IEnumerable<GetFranchiseResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<IEnumerable<GetFranchiseResponseDto>>> SearchByNameAsync(string search)
    {
        try
        {
            var response = await _repository.SearchByNameAsync(search);

            return Result<IEnumerable<GetFranchiseResponseDto>>.Success(
                response.Select(GetFranchiseResponseDto.FromEntity));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<IEnumerable<GetFranchiseResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<GetFranchiseResponseDto?>> GetAsync(string id)
    {
        try
        {
            var response = await _repository.GetAsync(id);

            if (response == null)
                return Result<GetFranchiseResponseDto?>.Failure(
                    new Error("404", "Franquia nao encontrada"));

            return Result<GetFranchiseResponseDto?>.Success(
                GetFranchiseResponseDto.FromEntity(response));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetFranchiseResponseDto?>.Failure(error);
        }
    }

    public async Task<Result<GetFranchiseResponseDto>> CreateAsync(CreateFranchiseRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<GetFranchiseResponseDto>.Failure(
                    new Error("400", "Nome da franquia obrigatorio"));

            var existingFranchise = await _repository.GetByNameAsync(dto.Name);
            if (existingFranchise != null)
                return Result<GetFranchiseResponseDto>.Failure(
                    new Error("409", "Ja existe uma franquia com este nome"));

            var entity = dto.ToEntity();
            await _repository.CreateAsync(entity);

            return Result<GetFranchiseResponseDto>.Success(
                GetFranchiseResponseDto.FromEntity(entity));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetFranchiseResponseDto>.Failure(error);
        }
    }

    public async Task<Result<GetFranchiseResponseDto>> UpdateAsync(UpdateFranchiseRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
                return Result<GetFranchiseResponseDto>.Failure(
                    new Error("400", "Franquia invalida"));

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<GetFranchiseResponseDto>.Failure(
                    new Error("400", "Nome da franquia obrigatorio"));

            var existingFranchise = await _repository.GetAsync(dto.Id);
            if (existingFranchise == null)
                return Result<GetFranchiseResponseDto>.Failure(
                    new Error("404", "Franquia nao encontrada"));

            var duplicatedFranchise = await _repository.GetByNameAsync(dto.Name, dto.Id);
            if (duplicatedFranchise != null)
                return Result<GetFranchiseResponseDto>.Failure(
                    new Error("409", "Ja existe uma franquia com este nome"));

            existingFranchise.Name = dto.Name.Trim();
            await _repository.UpdateAsync(existingFranchise);

            return Result<GetFranchiseResponseDto>.Success(
                GetFranchiseResponseDto.FromEntity(existingFranchise));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetFranchiseResponseDto>.Failure(error);
        }
    }

    public async Task<Result<bool>> DeleteAsync(string id)
    {
        try
        {
            var existingFranchise = await _repository.GetAsync(id);
            if (existingFranchise == null)
                return Result<bool>.Failure(new Error("404", "Franquia nao encontrada"));

            var moviesCount = await _movieRepository.CountByFranchiseIdAsync(id);
            var seriesCount = await _seriesRepository.CountByFranchiseIdAsync(id);

            if (moviesCount > 0 || seriesCount > 0)
                return Result<bool>.Failure(
                    new Error("409", "Nao foi possivel excluir! A franquia esta vinculada a outros filmes ou series"));

            await _repository.DeleteAsync(id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }
}
