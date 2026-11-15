using System;

namespace ticktick_WPF.Views.MarkDown;

public sealed class MarkDownEditorTextChangedEventArgs : EventArgs
{
	public string TaskId { get; }

	public MarkDownEditorTextChangedEventArgs(string taskId)
	{
		TaskId = taskId;
	}
}
