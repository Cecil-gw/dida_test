using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class SyncSortOrder
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("order")]
	public long? Order { get; set; }

	[JsonProperty("type")]
	public int Type { get; set; }
}
