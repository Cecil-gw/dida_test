using System;
using SQLite;
using TickTickUtils;

namespace TickTickModels;

public class ReminderTimeModel : BaseModel
{
	[Indexed]
	public string EntityId { get; set; }

	public int Type { get; set; }

	public long ReminderTime { get; set; }

	public string UserId { get; set; }

	public DateTime Time { get; set; }

	public bool IsEnd { get; set; }

	public ReminderTimeModel()
	{
	}

	public ReminderTimeModel(string id, int type, long ticks)
	{
		ticks = ticks / 600000000 * 600000000;
		EntityId = id;
		Type = type;
		ReminderTime = ticks;
		UserId = GlobalSettings.UserId;
		Time = new DateTime(ticks);
	}
}
