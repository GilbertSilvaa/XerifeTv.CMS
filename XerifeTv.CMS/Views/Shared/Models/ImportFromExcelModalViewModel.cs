namespace XerifeTv.CMS.Views.Shared.Models;

public class ImportFromExcelModalViewModel
{
    public required string Title { get; set; }
    public required string Controller { get; set; }
    public required string TemplateFileName { get; set; }
    public required string TemplateDownloadName { get; set; }
}