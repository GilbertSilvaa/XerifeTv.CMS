using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Abstractions.Interfaces;

public interface IStorageFilesService
{
    Task<Result<string>> UploadFileAsync(Stream fileStream, string fileName, string bucketName);
}