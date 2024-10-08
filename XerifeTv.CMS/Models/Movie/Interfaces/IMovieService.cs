﻿using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Movie.Dtos.Request;
using XerifeTv.CMS.Models.Movie.Dtos.Response;

namespace XerifeTv.CMS.Models.Movie.Interfaces;

public interface IMovieService
{
  Task<Result<PagedList<GetMovieResponseDto>>> Get(int currentPage, int limit);
  Task<Result<GetMovieResponseDto?>> Get(string id);
  Task<Result<string>> Create(CreateMovieRequestDto dto);
  Task<Result<string>> Update(UpdateMovieRequestDto dto);
  Task<Result<bool>> Delete(string id);
  Task<Result<PagedList<GetMovieResponseDto>>> GetByFilter(GetMoviesByFilterRequestDto dto);
}
