using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Common.Dtos;

namespace XerifeTv.CMS.Modules.Abstractions.Interfaces;

public interface ISpreadsheetBatchImporter<T>
{
  Task<Result<string>> ImportAsync(IFormFile file);
  Task<Result<ImportSpreadsheetResponseDto>> MonitorImportAsync(string importId);
}