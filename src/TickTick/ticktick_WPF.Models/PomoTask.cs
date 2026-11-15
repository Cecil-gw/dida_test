using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class PomoTask : BaseModel
{
	[JsonProperty("taskId")]
	public string TaskId { get; set; }

	[JsonProperty("habitId")]
	public string HabitId { get; set; }

	[JsonIgnore]
	public string PomoId { get; set; }

	[JsonProperty("title")]
	public string Title { get; set; }

	[JsonProperty("projectName")]
	public string ProjectName { get; set; }

	[Ignore]
	[JsonProperty("tags")]
	public string[] Tags { get; set; }

	[JsonIgnore]
	public string TagString { get; set; }

	[JsonProperty("startTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime StartTime { get; set; }

	[JsonProperty("endTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime EndTime { get; set; }

	[JsonProperty("timerId")]
	public string TimerSid { get; set; }

	[JsonProperty("timerName")]
	public string TimerName { get; set; }

	public string GetId()
	{
		if (!string.IsNullOrEmpty(TimerSid))
		{
			return TimerSid;
		}
		if (!string.IsNullOrEmpty(TaskId))
		{
			return TaskId;
		}
		if (!string.IsNullOrEmpty(HabitId))
		{
			return HabitId;
		}
		return null;
	}

	public string GetEntityId()
	{
		if (!string.IsNullOrEmpty(TaskId))
		{
			return TaskId;
		}
		if (!string.IsNullOrEmpty(HabitId))
		{
			return HabitId;
		}
		return null;
	}

	public long GetTodayDuration()
	{
		return (long)(EndTime - ((StartTime > DateTime.Today) ? StartTime : DateTime.Today)).TotalSeconds;
	}

	public PomoTask Copy()
	{
		return (PomoTask)MemberwiseClone();
	}

	public string GetTitle()
	{
		if (string.IsNullOrEmpty(Title))
		{
			return TimerName;
		}
		return Title;
	}
}
