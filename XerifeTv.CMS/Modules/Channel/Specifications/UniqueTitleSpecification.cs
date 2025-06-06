using MongoDB.Driver.Linq;
using XerifeTv.CMS.Modules.Channel.Dtos.Request;
using XerifeTv.CMS.Modules.Channel.Enums;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.Channel.Specifications;

public class UniqueTitleSpecification(IChannelRepository _repository) : ISpecification<ChannelEntity>
{
    public async Task<bool> IsSatisfiedByAsync(ChannelEntity channel)
    {
        try
        {
            var filterDto = new GetChannelsByFilterRequestDto(
                EChannelSearchFilter.TITLE, channel.Title, 50, 1, true);

            var channelsByTitle = await _repository.GetByFilterAsync(filterDto);

            var matchingChannels = channelsByTitle.Items
                .Where(c => c.Title.Equals(channel.Title, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingChannels.Count == 0)
                return true;

            if (matchingChannels.Count == 1 && matchingChannels[0].Id == channel.Id)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}