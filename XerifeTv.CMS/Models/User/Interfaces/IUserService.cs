﻿using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.User.Dtos.Request;
using XerifeTv.CMS.Models.User.Dtos.Response;

namespace XerifeTv.CMS.Models.User.Interfaces;

public interface IUserService
{
  Task<Result<string>> Register(RegisterUserRequestDto dto);
  Task<Result<LoginUserResponseDto>> Login(LoginUserRequestDto dto);
}