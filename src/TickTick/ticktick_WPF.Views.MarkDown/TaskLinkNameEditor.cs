namespace ticktick_WPF.Views.MarkDown;

public class TaskLinkNameEditor : EmojiEditor
{
	public TaskLinkNameEditor()
		: base(parseTaskLink: true)
	{
	}
}
