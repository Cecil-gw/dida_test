using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class RemindMessage
{
	public string id { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime remindTime { get; set; }

	public string type { get; set; }

	public RemindMessage(ReminderModel reminderModel)
	{
		id = reminderModel.GetObjId();
		type = reminderModel.GetObjType();
		if (reminderModel.ReminderTime.HasValue)
		{
			remindTime = reminderModel.ReminderTime.Value;
		}
	}

	public RemindMessage()
	{
	}
}
