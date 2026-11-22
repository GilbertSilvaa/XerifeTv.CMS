using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Dtos.Response;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Channel.Interfaces;

public interface IChannelService
{
    Task<Result<PagedList<GetChannelResponseDto>>> GetAsync(int currentPage, int limit);
    Task<Result<GetChannelResponseDto?>> GetAsync(string id);
    Task<Result<string>> CreateAsync(CreateChannelRequestDto dto);
    Task<Result<string>> UpdateAsync(UpdateChannelRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
    Task<Result<PagedList<GetChannelResponseDto>>> GetByFilterAsync(GetChannelsByFilterRequestDto dto);
}