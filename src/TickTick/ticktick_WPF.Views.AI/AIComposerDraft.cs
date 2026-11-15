namespace ticktick_WPF.Views.AI;

internal sealed class AIComposerDraft
{
	public static readonly AIComposerDraft Empty = new AIComposerDraft(string.Empty, null);

	public string Text { get; }

	public string EditingMessageId { get; }

	public bool IsEmpty
	{
		get
		{
			if (Text.Length == 0)
			{
				return string.IsNullOrEmpty(EditingMessageId);
			}
			return false;
		}
	}

	public AIComposerDraft(string text, string editingMessageId)
	{
		Text = text ?? string.Empty;
		EditingMessageId = editingMessageId;
	}
}
