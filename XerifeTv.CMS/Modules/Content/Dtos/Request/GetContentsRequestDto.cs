namespace XerifeTv.CMS.Modules.Content.Dtos.Request;

public record GetContentsRequestDto(string Search, int? CurrentPage, int? Limit);