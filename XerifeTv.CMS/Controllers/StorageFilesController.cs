using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Abstractions.Services;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class StorageFilesController(
  IStorageFilesService _service, 
  ILogger<StorageFilesController> _logger) : Controller
{
  [HttpPost]
  public async Task<JsonResult> UploadFile(IFormFile file)
  {
    if (file == null || file.Length == 0)
      return Json(Result<string>.Failure(new Error("400", "missing file")));
    
    using var stream = file.OpenReadStream();
    var response = await _service.UploadFileAsync(stream, file.FileName);
    
    return Json(response);
  }
}