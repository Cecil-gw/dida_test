using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.AI;

internal sealed class AgentRatingSubmission
{
	public const string NotUsefulReason = "Not useful";

	public const string DidNotFollowRequestReason = "Didn't follow my request";

	public const string ResponseIncompleteReason = "Response incomplete";

	public const string OtherReason = "Other";

	private static readonly HashSet<string> SupportedReasons = new HashSet<string>(StringComparer.Ordinal) { "Not useful", "Didn't follow my request", "Response incomplete", "Other" };

	public string ConversationId { get; }

	public string MessageId { get; }

	public int? Rating { get; }

	public string Reason { get; }

	public string Comment { get; }

	public bool IsValid
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(ConversationId) && !string.IsNullOrWhiteSpace(MessageId) && (!Rating.HasValue || Rating == 1 || Rating == -1) && (Reason == null || IsSupportedReason(Reason)) && (Rating != -1 || IsSupportedReason(Reason)))
			{
				return (Comment?.Length ?? 0) <= 500;
			}
			return false;
		}
	}

	public AgentRatingSubmission(string conversationId, string messageId, int? rating, string reason, string comment)
	{
		ConversationId = conversationId?.Trim();
		MessageId = messageId?.Trim();
		Rating = rating;
		Reason = (string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());
		Comment = (string.IsNullOrWhiteSpace(comment) ? null : comment.Trim());
	}

	public static bool IsSupportedReason(string reason)
	{
		if (reason != null)
		{
			return SupportedReasons.Contains(reason);
		}
		return false;
	}

	public JObject ToPayload()
	{
		JObject jObject = new JObject
		{
			["conversationId"] = ConversationId,
			["messageId"] = MessageId,
			["rating"] = (Rating.HasValue ? new JValue(Rating.Value) : JValue.CreateNull())
		};
		if (Reason != null)
		{
			jObject["reason"] = Reason;
		}
		if (Comment != null)
		{
			jObject["comment"] = Comment;
		}
		return jObject;
	}
}
