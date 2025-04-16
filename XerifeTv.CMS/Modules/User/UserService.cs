using SharpCompress.Common;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.User.Dtos.Request;
using XerifeTv.CMS.Modules.User.Dtos.Response;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Modules.User.Specifications;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Modules.User;

public sealed class UserService(
  IHashPassword _hashPassword,
  IUserRepository _repository, 
  ITokenService _tokenService) : IUserService
{
  public async Task<Result<PagedList<GetUserResponseDto>>> Get(int currentPage, int limit)
  {
    try
    {
      var response = await _repository.GetAsync(currentPage, limit);

      var result = new PagedList<GetUserResponseDto>(
        response.CurrentPage,
        response.TotalPageCount,
        response.Items
          .Where(r => r.Role != Enums.EUserRole.ADMIN)
          .Select(GetUserResponseDto.FromEntity));

      return Result<PagedList<GetUserResponseDto>>.Success(result);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<PagedList<GetUserResponseDto>>.Failure(error);
    }
  }

  public async Task<Result<GetUserResponseDto?>> GetByUsername(string username)
  {
    try
    {
      var response = await _repository.GetByUsernameAsync(username);
      
      if (response == null)
        return Result<GetUserResponseDto?>.Failure(
          new Error("404", "Usuario nao encontrado"));

      return Result<GetUserResponseDto>.Success(GetUserResponseDto.FromEntity(response));
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<GetUserResponseDto?>.Failure(error);
    }
  } 

  public async Task<Result<string>> Register(RegisterUserRequestDto dto)
  {
    try
    {
      var entity = dto.ToEntity();
      var emailSpec = new UniqueEmailSpecification(_repository);
      var usernameSpec = new UniqueUsernameSpecification(_repository);

      if (!await emailSpec.IsSatisfiedByAsync(entity))
        return Result<string>.Failure(new Error("409", "Email ja esta em uso"));
      
      if (!await usernameSpec.IsSatisfiedByAsync(entity))
        return Result<string>.Failure(new Error("409", "Username ja esta em uso"));

			entity.Password = _hashPassword.Encrypt(dto.Password);
      var response = await _repository.CreateAsync(entity);
      return Result<string>.Success(response);
    }
		catch (ArgumentException ex)
    {
			return Result<string>.Failure(new Error("400", ex.Message));
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
      var response = RegexHelper.IsValidEmail(dto.UserNameOrEmail) 
        ? await _repository.GetByEmailAsync(dto.UserNameOrEmail)
        : await _repository.GetByUsernameAsync(dto.UserNameOrEmail);

			if (response is null)
        return Result<LoginUserResponseDto>.Failure(
          new Error("404", "Usuario ou Email nao encontrado"));

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

  public async Task<Result<string>> Update(UpdateUserRequestDto dto)
	{
    try
    {
      var user = await _repository.GetAsync(dto.Id);

      if (user is null)
        return Result<string>.Failure(new Error("404", "Usuario nao encontrado"));
      
      var emailSpec = new UniqueEmailSpecification(_repository);
      var usernameSpec = new UniqueUsernameSpecification(_repository);
      
      if (!await emailSpec.IsSatisfiedByAsync(dto.ToEntity()))
        return Result<string>.Failure(new Error("409", "Email ja esta em uso"));
      
      if (!await usernameSpec.IsSatisfiedByAsync(dto.ToEntity()))
        return Result<string>.Failure(new Error("409", "Username ja esta em uso"));

      user.Email = dto.Email ?? user.Email;
      user.UserName = dto.UserName ?? user.UserName;
      user.Role = dto.Role ?? user.Role;
      
      await _repository.UpdateAsync(user);

      return Result<string>.Success(user.Id);
    }
    catch (ArgumentException ex)
		{
			return Result<string>.Failure(new Error("400", ex.Message));
		}
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<string>.Failure(error);
    }
  }

  public async Task<Result<string>> UpdatePassword(UpdatePasswordUserRequestDto dto)
  {
    try
    {
      var user = await _repository.GetAsync(dto.Id);
      
      if  (user is null) 
        return Result<string>.Failure(new Error("404", "Usuario nao encontrado"));
      
      var isPasswordCorrect = _hashPassword.Verify(dto.OldPassword, user.Password);
      
      if (!isPasswordCorrect)
        return Result<string>.Failure(new Error("401", "Senha atual incorreta"));
      
      user.Password = _hashPassword.Encrypt(dto.NewPassword);
      await _repository.UpdateAsync(user);
      
      return Result<string>.Success(user.Id);
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
      
      var user = await _repository.GetByUsernameAsync(userName!);
      
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

  public async Task<Result<string>> SendEmailResetPassword(string email)
  {
    try
    {
      var userByEmail = await _repository.GetByEmailAsync(email);
      
      if (userByEmail is null) 
        return Result<string>.Failure(new Error("404", "Email nao registrado"));

      return Result<string>.Success(email);
    }
    catch (Exception ex)
    {
      var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
      return Result<string>.Failure(error);
    }
  }
}