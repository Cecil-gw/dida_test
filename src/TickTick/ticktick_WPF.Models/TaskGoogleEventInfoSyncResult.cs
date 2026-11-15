using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class TaskGoogleEventInfoSyncResult
{
	public long checkpoint { get; set; }

	public string nextCursor { get; set; }

	public bool hasMore { get; set; }

	public List<TaskGoogleEventInfo> items { get; set; }

	public List<string> deletedIds { get; set; }
}
