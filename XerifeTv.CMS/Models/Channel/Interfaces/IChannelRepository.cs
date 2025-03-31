using XerifeTv.CMS.Models.Abstractions;
using XerifeTv.CMS.Models.Abstractions.Interfaces;
using XerifeTv.CMS.Models.Channel.Dtos.Request;

namespace XerifeTv.CMS.Models.Channel.Interfaces;

public interface IChannelRepository : IBaseRepository<ChannelEntity>
{
  Task<PagedList<ChannelEntity>> GetByFilterAsync(GetChannelsByFilterRequestDto dto);
  Task<IEnumerable<ItemsByCategory<ChannelEntity>>> GetGroupByCategoryAsync(int limit);
}
