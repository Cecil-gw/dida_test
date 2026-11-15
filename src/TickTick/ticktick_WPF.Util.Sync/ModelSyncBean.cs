using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync;

public class ModelSyncBean<T>
{
	[JsonProperty(PropertyName = "add")]
	public List<T> Add { get; set; } = new List<T>();

	[JsonProperty(PropertyName = "update")]
	public List<T> Update { get; set; } = new List<T>();

	[JsonProperty(PropertyName = "delete")]
	public List<string> Deleted { get; set; } = new List<string>();

	public bool Empty()
	{
		if (Add.Count == 0 && Update.Count == 0)
		{
			return Deleted.Count == 0;
		}
		return false;
	}
}
