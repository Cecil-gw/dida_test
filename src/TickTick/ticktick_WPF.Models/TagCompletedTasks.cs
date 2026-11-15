using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class TagCompletedTasks
{
	public string nextToken { get; set; }

	public List<TaskModel> tasks { get; set; }
}
