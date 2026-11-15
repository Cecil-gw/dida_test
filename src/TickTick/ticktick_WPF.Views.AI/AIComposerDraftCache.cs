using System;
using System.Collections.Generic;

namespace ticktick_WPF.Views.AI;

internal sealed class AIComposerDraftCache
{
	private readonly Dictionary<string, AIComposerDraft> _conversationDrafts = new Dictionary<string, AIComposerDraft>(StringComparer.Ordinal);

	private AIComposerDraft _newConversationDraft = AIComposerDraft.Empty;

	public AIComposerDraft Get(string conversationId)
	{
		string text = NormalizeConversationId(conversationId);
		if (text == null)
		{
			return _newConversationDraft;
		}
		if (!_conversationDrafts.TryGetValue(text, out var value))
		{
			return AIComposerDraft.Empty;
		}
		return value;
	}

	public void Set(string conversationId, string text, string editingMessageId)
	{
		string text2 = NormalizeConversationId(conversationId);
		AIComposerDraft aIComposerDraft = new AIComposerDraft(text, editingMessageId);
		if (text2 == null)
		{
			_newConversationDraft = (aIComposerDraft.IsEmpty ? AIComposerDraft.Empty : aIComposerDraft);
		}
		else if (aIComposerDraft.IsEmpty)
		{
			_conversationDrafts.Remove(text2);
		}
		else
		{
			_conversationDrafts[text2] = aIComposerDraft;
		}
	}

	public bool ClearIfMatches(string conversationId, string submittedContent, string editingMessageId)
	{
		AIComposerDraft aIComposerDraft = Get(conversationId);
		if (!string.Equals(aIComposerDraft.Text.Trim(), submittedContent, StringComparison.Ordinal) || !string.Equals(aIComposerDraft.EditingMessageId, editingMessageId, StringComparison.Ordinal))
		{
			return false;
		}
		Set(conversationId, string.Empty, null);
		return true;
	}

	public void Clear()
	{
		_newConversationDraft = AIComposerDraft.Empty;
		_conversationDrafts.Clear();
	}

	private static string NormalizeConversationId(string conversationId)
	{
		if (!string.IsNullOrWhiteSpace(conversationId))
		{
			return conversationId.Trim();
		}
		return null;
	}
}
