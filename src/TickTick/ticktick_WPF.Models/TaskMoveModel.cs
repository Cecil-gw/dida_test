using System;

namespace ticktick_WPF.Models;

[Serializable]
public class TaskMoveModel
{
	public string fromProjectId { get; set; }

	public string toProjectId { get; set; }

	public string taskId { get; set; }

	public long sortOrder { get; set; }
}
