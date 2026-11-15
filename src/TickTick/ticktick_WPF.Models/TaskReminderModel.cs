using System;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class TaskReminderModel : BaseModel
{
	public string id { get; set; }

	public int Taskid { get; set; }

	[Indexed]
	public string taskserverid { get; set; }

	public string trigger { get; set; }

	public bool IsEnd()
	{
		if (trigger != null)
		{
			return trigger.Contains("END");
		}
		return false;
	}
}
