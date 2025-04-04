using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Channel;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Channel.Interfaces;

public interface IChannelRepository : IBaseRepository<ChannelEntity>
{
  Task<PagedList<ChannelEntity>> GetByFilterAsync(GetChannelsByFilterRequestDto dto);
  Task<IEnumerable<ItemsByCategory<ChannelEntity>>> GetGroupByCategoryAsync(int limit);
}
