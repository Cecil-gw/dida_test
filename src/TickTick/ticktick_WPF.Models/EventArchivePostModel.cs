using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class EventArchivePostModel
{
	public List<EventArchiveSyncModel> add { get; set; } = new List<EventArchiveSyncModel>();

	public List<string> delete { get; set; } = new List<string>();

	[JsonIgnore]
	public bool Empty
	{
		get
		{
			if (add.Count == 0)
			{
				return delete.Count == 0;
			}
			return false;
		}
	}
}
