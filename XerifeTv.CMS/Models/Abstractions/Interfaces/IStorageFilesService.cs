namespace XerifeTv.CMS.Models.Abstractions.Interfaces;

public interface IStorageFilesService
{
  Task<Result<string>> UploadFileAsync(Stream fileStream, string fileName);
}