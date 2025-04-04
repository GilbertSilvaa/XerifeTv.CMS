using Supabase;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Abstractions.Services;

public class StorageFilesService : IStorageFilesService
{
	private readonly string[] _acceptedExtensions = [".vtt"];
  private readonly Client? _client = default;

  public StorageFilesService(IConfiguration _configuration)
  {
    _client = new Client(
      _configuration["Supabase:Url"]!, 
      _configuration["Supabase:Key"]);
  }
  
  public async Task<Result<string>> UploadFileAsync(Stream fileStream, string fileName)
  {
    var fileExtension = Path.GetExtension(fileName);

    if (!_acceptedExtensions.Contains(fileExtension))
      return Result<string>.Failure(new Error("400", $"Extensao de arquivo {fileExtension} invalida"));

    await _client.InitializeAsync();
    
    using var ms = new MemoryStream(); 
    await fileStream.CopyToAsync(ms);
    
    var response = await _client.Storage
      .From("subtitles")
      .Upload(ms.ToArray(), fileName, new() {Upsert = true});

    if (response == null) 
      return Result<string>.Failure(new Error("400", "Erro no uploading do arqivo"));

    var urlFile = $"{_client.Auth.Options.Url.Split("/auth")[0]}/storage/v1/object/public/{response}";
    return Result<string>.Success(urlFile);
  }
}