using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationPinUpsert
{
	[JsonProperty("conversation_id")]
	public string ConversationId { get; set; }

	[JsonProperty("sort_order")]
	public long SortOrder { get; set; }
}
