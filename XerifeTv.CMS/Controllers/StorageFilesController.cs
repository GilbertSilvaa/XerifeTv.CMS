using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class StorageFilesController(
  IStorageFilesService service,
  ILogger<StorageFilesController> logger) : Controller
{
    [HttpPost]
	[Authorize(Roles = "admin, common")]
	public async Task<JsonResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Json(Result<string>.Failure(new Error("400", "Arquivo ausente")));

        using var stream = file.OpenReadStream();
        var response = await service.UploadFileAsync(stream, file.FileName, "subtitles");

        logger.LogInformation(
          response.IsSuccess ? $"Upload file {response.Data} success" : "Error uploading file");

        return Json(response);
    }
}
