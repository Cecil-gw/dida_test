using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.AI;

internal sealed class AgentApproval
{
	public const string ApprovedDecision = "approved";

	public const string RejectedDecision = "rejected";

	public string ConversationId { get; }

	public string MessageId { get; }

	public string Status { get; }

	public string Title { get; }

	public string Description { get; }

	public string ConfirmText { get; }

	public string CancelText { get; }

	public string Decision { get; }

	public string RunId { get; }

	public string Error { get; }

	public string[] TaskTitles { get; }

	public string ConfirmAllText { get; }

	public bool ShouldDisplay
	{
		get
		{
			if (!string.Equals(Status, "pending", StringComparison.Ordinal))
			{
				return string.Equals(Status, "submitting", StringComparison.Ordinal);
			}
			return true;
		}
	}

	private bool IsRetry
	{
		get
		{
			if (Status == "submitting" && !string.IsNullOrWhiteSpace(Error) && !string.IsNullOrWhiteSpace(RunId))
			{
				return IsSupportedDecision(Decision);
			}
			return false;
		}
	}

	private AgentApproval(string conversationId, string messageId, string status, string title, string description, string confirmText, string cancelText, string decision, string runId, string error, string[] taskTitles, string confirmAllText)
	{
		ConversationId = conversationId;
		MessageId = messageId;
		Status = status;
		Title = title;
		Description = description;
		ConfirmText = confirmText;
		CancelText = cancelText;
		Decision = decision;
		RunId = runId;
		Error = error;
		TaskTitles = taskTitles;
		ConfirmAllText = confirmAllText;
	}

	public bool CanSubmitDecision(string decision)
	{
		if (!IsSupportedDecision(decision))
		{
			return false;
		}
		if (!(Status == "pending") || !string.IsNullOrEmpty(Decision))
		{
			if (IsRetry)
			{
				return string.Equals(Decision, decision, StringComparison.Ordinal);
			}
			return false;
		}
		return true;
	}

	public static bool IsSupportedDecision(string decision)
	{
		if (!string.Equals(decision, "approved", StringComparison.Ordinal))
		{
			return string.Equals(decision, "rejected", StringComparison.Ordinal);
		}
		return true;
	}

	public static AgentApproval Parse(string conversationId, JToken payload)
	{
		if (!(payload is JObject jObject) || (jObject["type"] != null && !string.Equals((string?)jObject["type"], "approval", StringComparison.OrdinalIgnoreCase)))
		{
			return null;
		}
		JToken? jToken = jObject["messageId"];
		string text = ((jToken != null && jToken.Type == JTokenType.String) ? jObject["messageId"].Value<string>() : null);
		string text2 = NormalizeString(jObject["status"])?.ToLowerInvariant();
		if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2))
		{
			return null;
		}
		return new AgentApproval(conversationId, text, text2, NormalizeString(jObject["title"]) ?? string.Empty, NormalizeString(jObject["description"]) ?? string.Empty, NormalizeString(jObject["confirmText"]) ?? string.Empty, NormalizeString(jObject["cancelText"]) ?? string.Empty, NormalizeString(jObject["decision"])?.ToLowerInvariant(), NormalizeString(jObject["runId"]), NormalizeString(jObject["error"]), (from item in (jObject["taskTitles"] as JArray)?.Where((JToken item) => item.Type == JTokenType.String)
			select item.Value<string>()).ToArray() ?? Array.Empty<string>(), NormalizeString(jObject["confirmAllText"]) ?? string.Empty);
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
