using XerifeTv.CMS.Modules.Integrations.Webhook.Dtos.Response;
using XerifeTv.CMS.Modules.User.Dtos.Response;

namespace XerifeTv.CMS.Views.Settings.Models;

public sealed record SettingsModelView(
    GetUserResponseDto UserSettingModel,
    IEnumerable<GetWebhookResponseDto> WebHooks);
