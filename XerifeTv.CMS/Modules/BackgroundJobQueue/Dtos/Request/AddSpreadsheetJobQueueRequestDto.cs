using XerifeTv.CMS.Modules.BackgroundJobQueue.Enums;

namespace XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;

public class AddSpreadsheetJobQueueRequestDto
{
    private EBackgroundJobType _backgroundJobType;
    public EBackgroundJobType Type
    {
        get => _backgroundJobType;
        init
        {
            if (value == EBackgroundJobType.IMPORT_EPISODES_FROM_SERIES_IMDB)
                throw new ArgumentException("This job type is not allowed for spreadsheet jobs.", nameof(Type));

            _backgroundJobType = value;
        }
    }

    public string RequestedByUsername { get; set; } = string.Empty;
    public IFormFile? SpreadsheetFile { get; init; }
}