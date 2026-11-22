using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Views.Models;

public sealed record ContentPickerItem(
    string ContentId,
    EContentType ContentType,
    string Title,
    string PosterUrl);

public sealed class ContentPickerViewModel
{
    public string IdPrefix { get; init; } = "contentPicker";
    public string InputName { get; init; } = "Items";
    public IReadOnlyList<EContentType> AllowedTypes { get; init; } =
        [EContentType.MOVIE, EContentType.SERIES];
    public IReadOnlyList<ContentPickerItem> Items { get; init; } = [];
}
