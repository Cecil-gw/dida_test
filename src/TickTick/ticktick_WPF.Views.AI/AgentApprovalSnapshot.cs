using System.Linq;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.AI;

internal sealed class AgentApprovalSnapshot
{
	public string ConversationId { get; }

	public AgentApproval[] Approvals { get; }

	public AgentApproval CurrentApproval { get; }

	public int CurrentIndex { get; }

	public int Total { get; }

	private AgentApprovalSnapshot(string conversationId, AgentApproval[] approvals, JObject queue)
	{
		ConversationId = conversationId;
		Approvals = approvals;
		string messageId = ((queue != null && queue["messageId"]?.Type == JTokenType.String) ? queue["messageId"].Value<string>() : null);
		CurrentIndex = ((queue != null && queue["currentIndex"]?.Type == JTokenType.Integer) ? queue["currentIndex"].Value<int>() : 0);
		Total = ((queue != null && queue["total"]?.Type == JTokenType.Integer) ? queue["total"].Value<int>() : 0);
		CurrentApproval = ((CurrentIndex > 0 && Total >= CurrentIndex) ? approvals.FirstOrDefault((AgentApproval item) => item.MessageId == messageId && item.ShouldDisplay) : null);
	}

	public static AgentApprovalSnapshot Parse(JToken payload, string fallbackConversationId = null)
	{
		JObject jObject = payload as JObject;
		if (!(jObject?["approvals"] is JArray source))
		{
			return null;
		}
		string conversationId = NormalizeString(jObject["conversationId"]);
		if (conversationId == null && !string.IsNullOrWhiteSpace(fallbackConversationId))
		{
			conversationId = fallbackConversationId.Trim();
		}
		return new AgentApprovalSnapshot(conversationId, (from item in source
			select AgentApproval.Parse(conversationId, item) into item
			where item != null
			select item).ToArray(), jObject["queue"] as JObject);
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
