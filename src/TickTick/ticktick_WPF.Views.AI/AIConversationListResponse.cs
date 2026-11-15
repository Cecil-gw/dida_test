using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationListResponse
{
	[JsonProperty("items")]
	public List<AIConversationListItem> Items { get; set; }

	[JsonProperty("total")]
	public int Total { get; set; }
}
