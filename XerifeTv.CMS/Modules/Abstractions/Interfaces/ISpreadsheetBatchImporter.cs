using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;

namespace XerifeTv.CMS.Modules.Abstractions.Interfaces;

public interface ISpreadsheetBatchImporter
{
    Task<Result<string>> ImportAsync(IFormFile file);
    Task<Result<ImportSpreadsheetResponseDto>> MonitorImportAsync(string importId);
}

public interface ISpreadsheetBatchImporter<TService> : ISpreadsheetBatchImporter;