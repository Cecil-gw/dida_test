using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class FilterCache : CacheBase<FilterModel>
{
	public override async Task Load()
	{
		List<FilterModel> filters = await FilterDao.GetAllFilters();
		await CheckTimelineModel(filters);
		AssembleData(filters, (FilterModel filter) => filter.id);
	}

	private async Task CheckTimelineModel(List<FilterModel> filters)
	{
		if (filters == null || filters.Count <= 0)
		{
			return;
		}
		List<FilterModel> list = new List<FilterModel>();
		foreach (FilterModel filter in filters)
		{
			if (filter.SyncTimeline.SortType != filter.Timeline.SortType)
			{
				filter.SyncTimeline.SortType = filter.Timeline.SortType;
				list.Add(filter);
			}
		}
		if (list.Any())
		{
			await App.Connection.UpdateAllAsync(list);
		}
	}
}
