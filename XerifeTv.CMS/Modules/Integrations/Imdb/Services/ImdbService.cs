using Newtonsoft.Json;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Integrations.Imdb.Dtos;

namespace XerifeTv.CMS.Modules.Integrations.Imdb.Services;

public class ImdbService(IConfiguration _configuration) : IImdbService
{
	public async Task<Result<GetAllResultsByImdbIdResponseDto?>> GetAllResultsByImdbIdAsync(string imdbId)
	{
		try
		{
			var client = new HttpClient();
			var url = $"https://api.themoviedb.org/3/find/{imdbId}";
			var tmdbKey = _configuration["Tmdb:Key"];

			var response = await client.GetAsync($"{url}?api_key={tmdbKey}&external_source=imdb_id");
			
			if (!response.IsSuccessStatusCode) 
				return Result<GetAllResultsByImdbIdResponseDto?>.Failure(
					new Error("400", $"[{imdbId}] IMDB API retornou: {response.ReasonPhrase}"));
			
			var responseJsonString = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<GetAllResultsByImdbIdResponseDto>(responseJsonString);

			if (result is null)
				return Result<GetAllResultsByImdbIdResponseDto?>.Failure(
					new Error("404", $"Imdb ID: {imdbId} invalido"));

			return Result<GetAllResultsByImdbIdResponseDto?>.Success(result);
		}
		catch (Exception ex)
		{
			var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
			return Result<GetAllResultsByImdbIdResponseDto?>.Failure(error);
		}
	}

	public async Task<Result<GetMovieByImdbResponseDto?>> GetMovieByImdbIdAsync(string imdbId)
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