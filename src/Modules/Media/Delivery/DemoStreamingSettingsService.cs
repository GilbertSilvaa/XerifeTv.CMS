using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Request;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

namespace XerifeTv.CMS.Modules.Media.Delivery;

public sealed class DemoStreamingSettingsService(
    IDemoStreamingSettingsRepository repository) : IDemoStreamingSettingsService
{
    public async Task<Result<GetDemoStreamingSettingsResponseDto>> GetAsync()
    {
        try
        {
            var settings = await repository.GetSettingsAsync();

            return Result<GetDemoStreamingSettingsResponseDto>.Success(
                GetDemoStreamingSettingsResponseDto.FromEntity(settings));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetDemoStreamingSettingsResponseDto>.Failure(error);
        }
    }

    public async Task<Result<string>> UpdateAsync(UpdateDemoStreamingSettingsRequestDto dto)
    {
        try
        {
            var videoUrl = dto.VideoUrl?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(videoUrl))
                return Result<string>.Failure(
                    new Error("400", "Informe a URL do vídeo de demonstração."));

            if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return Result<string>.Failure(
                    new Error("400", "Informe uma URL válida (http ou https)."));
            }

            var settings = await repository.GetSettingsAsync();
            settings.VideoUrl = videoUrl;

            await repository.CreateOrUpdateAsync(settings);

            return Result<string>.Success(settings.Id);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<string>.Failure(error);
        }
    }
}
