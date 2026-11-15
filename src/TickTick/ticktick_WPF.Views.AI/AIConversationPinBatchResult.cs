using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationPinBatchResult
{
	[JsonProperty("items")]
	public List<AIConversationListItem> Items { get; set; }

	[JsonProperty("not_found")]
	public List<string> NotFound { get; set; }
}
