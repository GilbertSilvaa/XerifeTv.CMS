using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Channel.Interfaces;

public interface IChannelService
{
    Task<Result<PagedList<GetChannelResponseDto>>> Get(int currentPage, int limit);
    Task<Result<GetChannelResponseDto?>> Get(string id);
    Task<Result<string>> Create(CreateChannelRequestDto dto);
    Task<Result<string>> Update(UpdateChannelRequestDto dto);
    Task<Result<bool>> Delete(string id);
    Task<Result<PagedList<GetChannelResponseDto>>> GetByFilter(GetChannelsByFilterRequestDto dto);
}