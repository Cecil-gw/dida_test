using System;

namespace ticktick_WPF.Models;

[Serializable]
public class ColumnSyncStatusModel
{
	public string ColumnId { get; set; }

	public string SyncStatus { get; set; }
}
