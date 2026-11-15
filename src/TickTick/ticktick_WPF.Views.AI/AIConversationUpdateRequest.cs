using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationUpdateRequest
{
	[JsonProperty("title")]
	public string Title { get; set; }
}
