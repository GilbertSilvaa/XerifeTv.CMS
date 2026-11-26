namespace XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;

public sealed class GetDemoStreamingSettingsResponseDto
{
    public string VideoUrl { get; private set; } = string.Empty;

    public static GetDemoStreamingSettingsResponseDto FromEntity(DemoStreamingSettingsEntity entity)
    {
        return new GetDemoStreamingSettingsResponseDto
        {
            VideoUrl = entity.VideoUrl
        };
    }
}
