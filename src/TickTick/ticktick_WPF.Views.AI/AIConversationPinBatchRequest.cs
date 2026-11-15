using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationPinBatchRequest
{
	[JsonProperty("update")]
	public List<AIConversationPinUpsert> Update { get; set; } = new List<AIConversationPinUpsert>();

	[JsonProperty("delete")]
	public List<string> Delete { get; set; } = new List<string>();
}
