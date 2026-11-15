using System;
using SQLite;

namespace ticktick_WPF.Dal;

[Serializable]
public class TaskCompletionRate
{
	public bool IsFromDb;

	[PrimaryKey]
	public string TaskId { get; set; } = string.Empty;

	public int CompletedCount { get; set; }

	public int TotalCount { get; set; }
}
