using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;

namespace XerifeTv.CMS.Modules.Media.Delivery.Services;

public class MediaDeliveryUrlResolver(
    IEnumerable<IMediaDeliveryTokenStrategy> _mediaTokenStrategies,
    IMediaDeliveryProfileService _service) : IMediaDeliveryUrlResolver
{
    public async Task<Result<GetResolveUrlResponseDto>> ResolveUrlAsync(string mediaPath, string mediaDeliveryProfileId)
    {
        try
        {
            var response = await _service.GetAsync(mediaDeliveryProfileId);

            if (response.IsFailure)
                return Result<GetResolveUrlResponseDto>.Failure(response.Error);

            var mediaProfile = response.Data!;

            var tokenStrategy = _mediaTokenStrategies.FirstOrDefault(s => s.CanHandle(mediaProfile.TokenStrategy));

            if (tokenStrategy == null)
                return Result<GetResolveUrlResponseDto>.Failure(new Error("400", "No token strategy found for the specified type"));

            var tokenResult = tokenStrategy.Resolve(mediaProfile.QueryParameters);

            if (tokenResult.IsFailure)
                return Result<GetResolveUrlResponseDto>.Failure(tokenResult.Error);

            var baseUri = new Uri(mediaProfile.BaseUrl);
            var combinedPath = $"{baseUri.AbsolutePath.TrimEnd('/')}/{mediaPath.TrimStart('/')}";

            var urlBuilder = new UriBuilder(baseUri)
            {
                Path = combinedPath,
                Query = tokenResult.Data
            };

            return Result<GetResolveUrlResponseDto>.Success(new(urlBuilder.ToString(), mediaProfile.StreamFormat));
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<GetResolveUrlResponseDto>.Failure(error);
        }
    }

    public async Task<Result<GetResolveUrlResponseDto>> ResolveUrlFixedAsync(string urlFixed, string streamFormat)
    {
       return await Task.FromResult(Result<GetResolveUrlResponseDto>.Success(new(urlFixed, streamFormat)));
    }
}