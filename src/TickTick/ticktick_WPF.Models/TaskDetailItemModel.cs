using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

[Serializable]
public class TaskDetailItemModel : BaseModel
{
	[Indexed]
	public string id { get; set; }

	public int TaskId { get; set; }

	[Indexed]
	public string TaskServerId { get; set; }

	public string title { get; set; }

	public int status { get; set; }

	public long sortOrder { get; set; }

	public bool? isAllDay { get; set; }

	[Ignore]
	private DateTime? CompletedTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? completedTime
	{
		get
		{
			if (!Utils.IsEmptyDate(CompletedTime))
			{
				return CompletedTime;
			}
			return null;
		}
		set
		{
			CompletedTime = value;
		}
	}

	[JsonIgnore]
	public DateTime? startDate { get; set; }

	[Ignore]
	[JsonProperty(PropertyName = "startDate")]
	public string serverStartDate { get; set; }

	[Ignore]
	private DateTime? SnoozeReminderTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? snoozeReminderTime
	{
		get
		{
			if (!Utils.IsEmptyDate(SnoozeReminderTime))
			{
				return SnoozeReminderTime;
			}
			return null;
		}
		set
		{
			SnoozeReminderTime = value;
		}
	}

	public TaskDetailItemModel Copy()
	{
		return (TaskDetailItemModel)MemberwiseClone();
	}
}
