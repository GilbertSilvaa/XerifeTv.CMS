namespace XerifeTv.CMS.Shared.Views.Models;

public class ImportFromExcelModalViewModel
{
    public required string Title { get; set; }
    public required string Controller { get; set; }
    public required string TemplateFileName { get; set; }
    public required string TemplateDownloadName { get; set; }
}