using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Models;

public class HabitModel : BaseModel
{
	private const string DefaultIconRes = "habit_daily_check_in";

	private string _iconRes;

	[JsonProperty(PropertyName = "id")]
	[Indexed]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "name")]
	public string Name { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonProperty(PropertyName = "iconRes")]
	public string IconRes
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_iconRes))
			{
				return _iconRes;
			}
			return "habit_daily_check_in";
		}
		set
		{
			_iconRes = value;
		}
	}

	[JsonProperty(PropertyName = "color")]
	public string Color { get; set; }

	[JsonProperty(PropertyName = "status")]
	[Indexed]
	public int Status { get; set; }

	[JsonProperty(PropertyName = "totalCheckIns")]
	public int TotalCheckIns { get; set; }

	[JsonProperty(PropertyName = "createdTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime CreatedTime { get; set; }

	[JsonProperty(PropertyName = "modifiedTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime ModifiedTime { get; set; }

	[JsonProperty(PropertyName = "archivedTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? ArchivedTime { get; set; }

	[JsonProperty(PropertyName = "type")]
	public string Type { get; set; }

	[JsonProperty(PropertyName = "goal")]
	public double Goal { get; set; }

	[JsonProperty(PropertyName = "step")]
	public double Step { get; set; }

	[JsonProperty(PropertyName = "unit")]
	public string Unit { get; set; }

	[JsonProperty(PropertyName = "etag")]
	public string Etag { get; set; }

	[JsonProperty(PropertyName = "sortOrder")]
	public long SortOrder { get; set; }

	[JsonProperty(PropertyName = "repeatRule")]
	public string RepeatRule { get; set; }

	[JsonProperty(PropertyName = "encouragement")]
	public string Encouragement { get; set; }

	[Ignore]
	[JsonProperty(PropertyName = "reminders")]
	public string[] Reminders
	{
		get
		{
			if (!string.IsNullOrEmpty(Reminder))
			{
				return Reminder.Split(',');
			}
			return new string[0];
		}
		set
		{
			if (value != null && value.Any())
			{
				Reminder = value.Join(",");
			}
			else
			{
				Reminder = string.Empty;
			}
		}
	}

	[JsonProperty("annoyingAlert")]
	public int? AnnoyingAlert { get; set; }

	[Ignore]
	[JsonProperty(PropertyName = "exDates")]
	public string[] ExDates
	{
		get
		{
			if (!string.IsNullOrEmpty(ExDateString))
			{
				return JsonConvert.DeserializeObject<string[]>(ExDateString);
			}
			return new string[0];
		}
		set
		{
			if (value != null)
			{
				ExDateString = JsonConvert.SerializeObject(value);
			}
		}
	}

	[JsonIgnore]
	public string ExDateString { get; set; }

	[JsonProperty(PropertyName = "recordEnable")]
	public bool? RecordEnable { get; set; } = false;

	[JsonProperty(PropertyName = "sectionId")]
	public string SectionId { get; set; }

	[JsonProperty(PropertyName = "targetDays")]
	public int? TargetDays { get; set; }

	[JsonProperty(PropertyName = "targetStartDate")]
	public int? TargetStartDate { get; set; }

	[JsonProperty(PropertyName = "completedCycles")]
	public int? CompletedCycles { get; set; }

	[JsonProperty(PropertyName = "style")]
	public int? Style { get; set; }

	[JsonIgnore]
	[Ignore]
	public List<CompletedCycle> CompletedCyclesList { get; set; }

	[JsonIgnore]
	public string CompletedCyclesListStr
	{
		get
		{
			return JsonConvert.SerializeObject(CompletedCyclesList);
		}
		set
		{
			if (value != null)
			{
				CompletedCyclesList = JsonConvert.DeserializeObject<List<CompletedCycle>>(value);
			}
		}
	}

	[JsonIgnore]
	public string Reminder { get; set; }

	[JsonIgnore]
	public int CheckStamp { get; set; }

	[JsonIgnore]
	[NotNull]
	[DefaultValue("2")]
	public int SyncStatus { get; set; }

	public DateTime GetStartDate()
	{
		if (TargetStartDate.HasValue && DateTime.TryParseExact(TargetStartDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) && IsValidTargetStartDate(result))
		{
			return result;
		}
		if (!IsValidTargetStartDate(CreatedTime))
		{
			return DateTime.Today;
		}
		return CreatedTime;
	}

	public static bool IsValidTargetStartDate(DateTime date)
	{
		if (!Utils.IsEmptyDate(date))
		{
			return date.Year >= 1900;
		}
		return false;
	}

	public bool IsBoolHabit()
	{
		return IsBooleanType();
	}

	public string GetSafeIconRes()
	{
		return IconRes;
	}

	public string GetSafeType()
	{
		return NormalizeType(Type, Goal, Step);
	}

	public static string NormalizeType(string type, double goal, double step)
	{
		if (string.Equals(type, HabitType.Real.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Number", StringComparison.OrdinalIgnoreCase))
		{
			return HabitType.Real.ToString();
		}
		if (string.Equals(type, HabitType.Boolean.ToString(), StringComparison.OrdinalIgnoreCase))
		{
			return HabitType.Boolean.ToString();
		}
		if (string.IsNullOrWhiteSpace(type) || string.Equals(type, "1", StringComparison.Ordinal))
		{
			if (!(Math.Abs(goal - 1.0) > 0.01) && !(Math.Abs(step - 1.0) > 0.01))
			{
				return HabitType.Boolean.ToString();
			}
			return HabitType.Real.ToString();
		}
		return HabitType.Boolean.ToString();
	}

	public bool IsBooleanType()
	{
		return string.Equals(GetSafeType(), HabitType.Boolean.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	public static HabitModel GetDefaultHabit()
	{
		return new HabitModel
		{
			Id = Utils.GetGuid(),
			Name = "",
			IconRes = "habit_daily_check_in",
			Color = HabitUtils.IconInfos["habit_daily_check_in"].IconOutlineColor,
			Type = HabitType.Boolean.ToString(),
			Goal = 1.0,
			Step = 1.0,
			Unit = Utils.GetString("Count"),
			RepeatRule = "RRULE:FREQ=DAILY;INTERVAL=1",
			RecordEnable = false,
			CreatedTime = DateTime.Now
		};
	}

	public bool IsSkipToday()
	{
		if (ExDates != null)
		{
			return ExDates.Contains(DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		}
		return false;
	}

	public bool IsSkip(DateTime dateTime)
	{
		if (ExDates != null)
		{
			return ExDates.Contains(dateTime.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		}
		return false;
	}

	public void MergeSkip(List<string> localExDates)
	{
	}
}
