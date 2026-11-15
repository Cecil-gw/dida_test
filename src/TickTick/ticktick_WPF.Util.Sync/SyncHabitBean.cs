using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncHabitBean
{
	[JsonProperty(PropertyName = "add")]
	public List<HabitModel> Add { get; set; } = new List<HabitModel>();

	[JsonProperty(PropertyName = "update")]
	public List<HabitModel> Update { get; set; } = new List<HabitModel>();

	[JsonProperty(PropertyName = "delete")]
	public Collection<string> Deleted { get; set; } = new Collection<string>();

	public bool Empty()
	{
		if (Add.Count == 0 && Update.Count == 0)
		{
			return Deleted.Count == 0;
		}
		return false;
	}
}
