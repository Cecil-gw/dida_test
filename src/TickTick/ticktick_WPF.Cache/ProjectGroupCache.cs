using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.Cache;

public class ProjectGroupCache : CacheBase<ProjectGroupModel>
{
	private bool _loaded;

	public override async Task Load()
	{
		if (!_loaded)
		{
			_loaded = true;
			List<ProjectGroupModel> groups = (await ProjectGroupDao.GetProjectGroups()).ToList();
			await CheckTimelineModel(groups);
			AssembleData(groups, (ProjectGroupModel group) => group.id);
		}
	}

	private async Task CheckTimelineModel(List<ProjectGroupModel> groups)
	{
		if (groups == null || groups.Count <= 0)
		{
			return;
		}
		List<ProjectGroupModel> list = new List<ProjectGroupModel>();
		foreach (ProjectGroupModel group in groups)
		{
			if (group.SyncTimeline.SortType != group.Timeline.SortType)
			{
				group.SyncTimeline.SortType = group.Timeline.SortType;
				list.Add(group);
			}
		}
		if (list.Any())
		{
			await App.Connection.UpdateAllAsync(list);
		}
	}
}
