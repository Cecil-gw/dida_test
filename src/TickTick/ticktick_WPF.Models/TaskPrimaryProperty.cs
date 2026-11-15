using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class TaskPrimaryProperty
{
	public int? Priority { get; set; }

	public TimeData TimeData { get; set; }

	public List<string> Tags { get; set; }

	public int TaskStatus { get; set; }

	public long SortOrder { get; set; }

	public string ProjectId { get; set; } = string.Empty;

	public string AssigneeId { get; set; } = string.Empty;

	public string ParentId { get; set; }
}
