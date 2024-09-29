﻿using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.User.Common;
using XerifeTv.CMS.Models.User.Dtos.Request;
using XerifeTv.CMS.Models.User.Dtos.Response;
using XerifeTv.CMS.Models.User.Interfaces;

namespace XerifeTv.CMS.Models.User;

public class UserService(
  IUserRepository _repository, 
  ITokenService _tokenService,
  IConfiguration _configuration) : IUserService
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

      var userByName = await _repository.GetByUserNameAsync(entity.UserName);

      if (userByName is not null)
        return Result<string>.Failure(new Error("409", "username already registered"));

      entity.Password = new HashPassword(_configuration).Encrypt(dto.Password);

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
          new Error("404", "user not found"));

      var isPasswordCorrect =
        new HashPassword(_configuration).Verify(dto.Password, response.Password);

      if (!isPasswordCorrect)
        return Result<LoginUserResponseDto>.Failure(
          new Error("401", "unauthorized"));

      return Result<LoginUserResponseDto>.Success(
        new LoginUserResponseDto(_tokenService.GenerateToken(response)));
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
        return Result<bool>.Failure(new Error("404", "user not found"));

      if (response.Role == Enums.EUserRole.ADMIN)
        return Result<bool>.Failure(new Error("403", "user cannot be deleted"));

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