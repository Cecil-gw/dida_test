using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncFilterBean
{
	[JsonProperty(PropertyName = "update")]
	public List<FilterModel> Update { get; set; } = new List<FilterModel>();

	[JsonProperty(PropertyName = "add")]
	public List<FilterModel> Add { get; set; } = new List<FilterModel>();

	[JsonProperty(PropertyName = "delete")]
	public Collection<string> Deleted { get; set; } = new Collection<string>();

	public bool Empty
	{
		get
		{
			if (Add.Count == 0 && Update.Count == 0)
			{
				return Deleted.Count == 0;
			}
			return false;
		}
	}
}
