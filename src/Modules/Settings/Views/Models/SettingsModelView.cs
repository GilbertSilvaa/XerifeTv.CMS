using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.Media.Delivery.Dtos.Response;
using XerifeTv.CMS.Modules.User.Dtos.Response;

namespace XerifeTv.CMS.Modules.Settings.Views.Models;

public sealed record SettingsModelView(
    GetUserResponseDto UserSettingModel,
    IEnumerable<GetWebhookResponseDto> WebHooks,
    IEnumerable<GetMediaDeliveryProfileResponseDto> MediaDeliveryProfiles,
    GetDemoStreamingSettingsResponseDto DemoStreamingSettings);
