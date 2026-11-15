using System;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.AI;

internal static class AgentRatingResult
{
	public static bool Matches(JToken payload, string conversationId, string messageId)
	{
		if (payload is JObject jObject && string.Equals((string?)jObject["conversationId"], conversationId, StringComparison.Ordinal))
		{
			return string.Equals((string?)jObject["messageId"], messageId, StringComparison.Ordinal);
		}
		return false;
	}
}
