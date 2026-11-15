using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class TaskSyncedJsonModel : BaseModel
{
	[Indexed]
	public string taskSid { get; set; }

	public string userId { get; set; }

	public string jsonString { get; set; }
}
