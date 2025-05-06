using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Series.Interfaces;

namespace XerifeTv.CMS.Modules.Series.Specifications;

public class UniqueImdbIdSpecification(ISeriesRepository _repository) : ISpecification<SeriesEntity>
{
	public async Task<bool> IsSatisfiedByAsync(SeriesEntity series)
	{
		try
		{
			var seriesByImdb = await _repository.GetByImdbIdAsync(series.ImdbId);
			return seriesByImdb == null || seriesByImdb.Id == series.Id;
		}
		catch
		{
			return false;
		}
	}
}