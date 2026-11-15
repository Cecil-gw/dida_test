using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncTagBean
{
	[JsonProperty(PropertyName = "update")]
	public List<TagModel> Update { get; set; } = new List<TagModel>();

	[JsonProperty(PropertyName = "add")]
	public List<TagModel> Add { get; set; } = new List<TagModel>();

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
