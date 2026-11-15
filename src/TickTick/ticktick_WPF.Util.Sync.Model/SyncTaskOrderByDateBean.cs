using System.Collections.Generic;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncTaskOrderByDateBean
{
	public List<TaskSortOrder> changed { get; set; }

	public List<TaskSortOrder> deleted { get; set; }
}
