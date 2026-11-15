using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class SortOrderData
{
	[JsonProperty("changed")]
	public List<SyncSortOrder> Changed { get; set; }

	[JsonProperty("deleted")]
	public List<SyncSortOrder> Deleted { get; set; }
}
