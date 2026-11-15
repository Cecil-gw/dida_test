using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncHabitSectionsBean
{
	[JsonProperty(PropertyName = "add")]
	public List<HabitSectionModel> Add { get; set; } = new List<HabitSectionModel>();

	[JsonProperty(PropertyName = "update")]
	public List<HabitSectionModel> Update { get; set; } = new List<HabitSectionModel>();

	[JsonProperty(PropertyName = "delete")]
	public Collection<string> Delete { get; set; } = new Collection<string>();

	public bool Empty()
	{
		if (Add.Count == 0 && Update.Count == 0)
		{
			return Delete.Count == 0;
		}
		return false;
	}
}
