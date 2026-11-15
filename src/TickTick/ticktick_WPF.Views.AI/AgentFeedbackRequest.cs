using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.AI;

internal sealed class AgentFeedbackRequest
{
	public string ConversationId { get; }

	public string MessageId { get; }

	public int Rating { get; }

	private AgentFeedbackRequest(string conversationId, string messageId, int rating)
	{
		ConversationId = conversationId;
		MessageId = messageId;
		Rating = rating;
	}

	public static AgentFeedbackRequest Parse(JToken payload)
	{
		if (!(payload is JObject jObject))
		{
			return null;
		}
		string text = NormalizeString(jObject["conversationId"]);
		string text2 = NormalizeString(jObject["messageId"]);
		if (text == null || text2 == null)
		{
			return null;
		}
		JToken jToken = jObject["rating"];
		if (jToken == null || jToken.Type != JTokenType.Integer || jToken.Value<int>() != -1)
		{
			return null;
		}
		return new AgentFeedbackRequest(text, text2, -1);
	}

	private static string NormalizeString(JToken value)
	{
		if (value == null || value.Type != JTokenType.String || string.IsNullOrWhiteSpace(value.Value<string>()))
		{
			return null;
		}
		return value.Value<string>().Trim();
	}
}
