using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncCountdownBean
{
	[JsonProperty(PropertyName = "add")]
	public List<CountdownModel> Add { get; set; } = new List<CountdownModel>();

	[JsonProperty(PropertyName = "update")]
	public List<CountdownModel> Update { get; set; } = new List<CountdownModel>();

	[JsonProperty(PropertyName = "delete")]
	public List<string> Delete { get; set; } = new List<string>();

	public bool Empty()
	{
		if (!Add.Any() && !Update.Any())
		{
			return !Delete.Any();
		}
		return false;
	}
}
