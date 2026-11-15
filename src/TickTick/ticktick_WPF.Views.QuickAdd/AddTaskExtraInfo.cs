using System.Collections.Generic;

namespace ticktick_WPF.Views.QuickAdd;

public class AddTaskExtraInfo
{
	public string Title { get; set; }

	public string Content { get; set; }

	public string AssigneeId { get; set; }

	public List<string> Tags { get; set; }
}
