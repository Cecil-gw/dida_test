using System.Collections.Generic;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncTaskOrderBean
{
	public Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> taskOrderByDate { get; set; }

	public Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> taskOrderByPriority { get; set; }

	public Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> taskOrderByProject { get; set; }
}
