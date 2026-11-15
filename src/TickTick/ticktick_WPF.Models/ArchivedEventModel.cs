using System;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class ArchivedEventModel : BaseModel
{
	public string UserId { get; set; }

	public string Key { get; set; }

	public DateTime? StartTime { get; set; }

	public string Title { get; set; }

	public int Kind { get; set; }

	public int SyncStatus { get; set; }
}
