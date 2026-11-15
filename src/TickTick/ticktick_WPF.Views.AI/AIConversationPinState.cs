using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationPinState
{
	public bool Initialized { get; set; }

	public List<AIConversationListItem> Items { get; set; } = new List<AIConversationListItem>();

	public Dictionary<string, AIPendingPinChange> Pending { get; set; } = new Dictionary<string, AIPendingPinChange>(StringComparer.Ordinal);

	[JsonIgnore]
	public List<string> UnavailableIds { get; set; } = new List<string>();

	[JsonIgnore]
	public bool IsTransient { get; set; }
}
