using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.User;

public sealed class UserService(
  IHashPassword _hashPassword,
  IUserRepository _repository, 
  ITokenService _tokenService) : IUserService
{
  public async Task<Result<PagedList<GetUserRequestDto>>> Get(int currentPage, int limit)
  {
    try
    {
      var response = await _repository.GetAsync(currentPage, limit);

      var result = new PagedList<GetUserRequestDto>(
        response.CurrentPage,
        response.TotalPageCount,
        response.Items
          .Where(r => r.Role != Enums.EUserRole.ADMIN)
          .Select(GetUserRequestDto.FromEntity));

      return Result<PagedList<GetUserRequestDto>>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetUserRequestDto>>.Failure(error);
    }
  }

  public async Task<Result<string>> Register(RegisterUserRequestDto dto)
  {
    try
    {
      var entity = dto.ToEntity();

      if (!RegexHelper.IsValidEmail(entity.Email))
				return Result<string>.Failure(new Error("400", "Email invalido"));

			var userByName = await _repository.GetByUserNameAsync(entity.UserName);

      if (userByName != null)
        return Result<string>.Failure(new Error("409", "Username ja registrado"));

			var userByEmail = await _repository.GetByEmailAsync(entity.Email);

      if (userByEmail != null)
				return Result<string>.Failure(new Error("409", "Email ja registrado"));

			entity.Password = _hashPassword.Encrypt(dto.Password);

      var response = await _repository.CreateAsync(entity);
      return Result<string>.Success(response);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<string>.Failure(error);
    }
  }

  public async Task<Result<LoginUserResponseDto>> Login(LoginUserRequestDto dto)
  {
    try
    {
      var response = await _repository.GetByUserNameAsync(dto.UserName);

      if (response is null)
        return Result<LoginUserResponseDto>.Failure(
          new Error("404", "Usuario nao encontrado"));

      var isPasswordCorrect = _hashPassword.Verify(dto.Password, response.Password);

      if (!isPasswordCorrect)
        return Result<LoginUserResponseDto>.Failure(
          new Error("401", "Credenciais invalidas"));

      return Result<LoginUserResponseDto>.Success(
        new LoginUserResponseDto(
          _tokenService.GenerateToken(response), 
          _tokenService.GenerateRefreshToken(response)));
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<LoginUserResponseDto>.Failure(error);
    }
  }

  public async Task<Result<bool>> Delete(string id)
  {
    try
    {
      var response = await _repository.GetAsync(id);

      if (response is null)
        return Result<bool>.Failure(new Error("404", "Usuario nao encontrado"));

      if (response.Role == Enums.EUserRole.ADMIN)
        return Result<bool>.Failure(new Error("403", "Usuario nao pode ser deletado"));

      await _repository.DeleteAsync(id);
      return Result<bool>.Success(true);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<bool>.Failure(error);
    }
  }

  public async Task<Result<(string? newToken, string? newRefreshToken)>> TryRefreshSession(string refreshToken)
  {
    try
    {
      var (isValid, userName) = await _tokenService.ValidateTokenAsync(refreshToken);
      
      if (!isValid) 
        return Result<(string?, string?)>.Failure(new Error("401", "Token invalido"));
      
      var user = await _repository.GetByUserNameAsync(userName!);
      
      return Result<(string?, string?)>.Success((
        _tokenService.GenerateToken(user!), 
        _tokenService.GenerateRefreshToken(user!)));
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<(string?, string?)>.Failure(error);
    }
  }
}