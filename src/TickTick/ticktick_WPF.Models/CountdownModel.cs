using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;

namespace ticktick_WPF.Models;

public class CountdownModel : BaseModel
{
	[JsonProperty(PropertyName = "id")]
	[Indexed]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "sid")]
	public string Sid { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonProperty(PropertyName = "name")]
	public string Name { get; set; }

	[JsonProperty(PropertyName = "iconRes")]
	public string IconRes { get; set; }

	[JsonProperty(PropertyName = "color")]
	public string Color { get; set; }

	[JsonProperty(PropertyName = "date")]
	public int Date { get; set; }

	[JsonIgnore]
	public DateTime TargetDate { get; set; }

	[JsonProperty(PropertyName = "typeOfSmartList")]
	public int TypeOfSmartList { get; set; }

	[JsonProperty(PropertyName = "ignoreYear")]
	public bool IgnoreYear { get; set; }

	[JsonProperty(PropertyName = "type")]
	public CountDownType Type { get; set; }

	[JsonProperty(PropertyName = "showCalendarType")]
	public ShowCalendarType ShowCalendarType { get; set; } = ShowCalendarType.Gregorian;

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
			if (value != null && value.Length != 0)
			{
				Reminder = string.Join(",", value);
			}
			else
			{
				Reminder = string.Empty;
			}
		}
	}

	[JsonProperty("annoyingAlert")]
	public int? AnnoyingAlert { get; set; }

	[JsonIgnore]
	public string Reminder { get; set; }

	[JsonProperty(PropertyName = "repeatFlag")]
	public string RepeatFlag { get; set; }

	[JsonProperty(PropertyName = "remark")]
	public string Remark { get; set; }

	[JsonProperty("timerMode")]
	public DisplayTimerMode TimerMode { get; set; }

	[JsonProperty("showAge")]
	public bool ShowAge { get; set; }

	[JsonProperty(PropertyName = "daysOption")]
	public DayStartOption DaysOption { get; set; }

	[JsonProperty(PropertyName = "status")]
	[Indexed]
	public int Status { get; set; }

	[JsonProperty(PropertyName = "style")]
	public string Style { get; set; } = "normal";

	[JsonProperty(PropertyName = "background")]
	[JsonConverter(typeof(BackgroundConverter))]
	public string Background { get; set; }

	[JsonIgnore]
	[Ignore]
	public BackgroundModel BackgroundObject
	{
		get
		{
			if (string.IsNullOrEmpty(Background))
			{
				return null;
			}
			try
			{
				return JsonConvert.DeserializeObject<BackgroundModel>(Background);
			}
			catch (Exception ex)
			{
				UtilLog.Warn("Error deserializing Background: " + ex.Message);
				return null;
			}
		}
		set
		{
			if (value == null)
			{
				Background = null;
			}
			else
			{
				Background = JsonConvert.SerializeObject(value);
			}
		}
	}

	[Ignore]
	[JsonProperty(PropertyName = "styleColor")]
	public string[] StyleColors
	{
		get
		{
			if (!string.IsNullOrEmpty(StyleColor))
			{
				return StyleColor.Split(',');
			}
			return new string[0];
		}
		set
		{
			if (value != null && value.Length != 0)
			{
				StyleColor = string.Join(",", value);
			}
			else
			{
				StyleColor = string.Empty;
			}
		}
	}

	[JsonIgnore]
	public string StyleColor { get; set; }

	[JsonProperty(PropertyName = "dateDisplayFormat")]
	public string DateDisplayFormat { get; set; } = "day";

	[Ignore]
	[JsonIgnore]
	public DateDisplayFormatType DateDisplayFormatType
	{
		get
		{
			if (Enum.TryParse<DateDisplayFormatType>(DateDisplayFormat, ignoreCase: true, out var result))
			{
				return result;
			}
			return DateDisplayFormatType.Day;
		}
		set
		{
			DateDisplayFormat = value.ToString().ToLower();
		}
	}

	[JsonProperty(PropertyName = "sortOrder")]
	public long SortOrder { get; set; }

	[JsonProperty(PropertyName = "deleted")]
	public int Deleted { get; set; }

	[JsonProperty(PropertyName = "etag")]
	public string Etag { get; set; }

	[JsonProperty(PropertyName = "archivedTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? ArchivedTime { get; set; }

	[JsonProperty(PropertyName = "pinnedTime")]
	public string pinnedTimeValue { get; set; }

	[JsonIgnore]
	[Ignore]
	public long pinnedTimeStamp
	{
		get
		{
			if (string.IsNullOrEmpty(pinnedTimeValue))
			{
				return 0L;
			}
			if (pinnedTimeValue == "-1")
			{
				return -1L;
			}
			if (DateTime.TryParse(pinnedTimeValue, out var result))
			{
				if (result.Year <= 2000)
				{
					return 0L;
				}
				return Utils.GetTimeStampInSecond(result);
			}
			return 0L;
		}
		set
		{
			if (value < 0)
			{
				pinnedTimeValue = "-1";
				return;
			}
			if (value == 0L)
			{
				pinnedTimeValue = null;
				return;
			}
			DateTime date = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).AddSeconds(value);
			pinnedTimeValue = date.ToString(UtcDateTimeConverter.GetConverterValue(date));
		}
	}

	[JsonProperty(PropertyName = "createdTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime CreatedTime { get; set; }

	[JsonProperty(PropertyName = "modifiedTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime ModifiedTime { get; set; }

	[JsonIgnore]
	[NotNull]
	[DefaultValue("2")]
	public int SyncStatus { get; set; }

	[JsonIgnore]
	public string LocalImagePath { get; set; }

	[Ignore]
	public CountdownWidgetModel.DisplayDescType WidgetDescType { get; set; }

	[JsonProperty(PropertyName = "showRemark")]
	public bool ShowRemark { get; set; } = true;

	public DateTime GetDateAsDateTime()
	{
		if (DateTime.TryParseExact(Date.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			return result;
		}
		return DateTime.Today;
	}

	public static CountdownModel GetDefaultCountdown(CountDownType type = CountDownType.Anniversary)
	{
		return new CountdownModel
		{
			Id = Utils.GetGuid(),
			Name = "",
			IconRes = "countdown_countdown",
			Color = null,
			Type = type,
			ShowCalendarType = ShowCalendarType.Gregorian,
			DateDisplayFormat = "day",
			Style = "normal",
			TypeOfSmartList = 0,
			IgnoreYear = (type == CountDownType.Holiday),
			Date = int.Parse(DateTime.Today.ToString("yyyyMMdd")),
			Status = 0,
			Deleted = 0,
			TimerMode = DisplayTimerMode.CountDown,
			ShowAge = false,
			CreatedTime = DateTime.Now,
			ModifiedTime = DateTime.Now
		};
	}

	public DateTime GetTargetDate(DisplayTimerMode? timerMode = null)
	{
		DateTime dateTime = GetDateAsDateTime();
		DateTime today = DateTime.Today;
		if (!string.IsNullOrEmpty(RepeatFlag) && !RepeatFlag.Contains("NONE") && dateTime.Date < today.Date && (!timerMode.HasValue || timerMode == DisplayTimerMode.CountDown))
		{
			try
			{
				dateTime = RepeatCalculator.GetCountdownRepeatNextDate(RepeatFlag, dateTime);
			}
			catch (Exception ex)
			{
				UtilLog.Error("Error deserializing Target Date: " + ex.Message);
				return GetDateAsDateTime();
			}
		}
		return dateTime;
	}

	public int GetDaysRemaining(DateTime? target = null, DateTime? current = null, DisplayTimerMode? timerMode = null)
	{
		DateTime dateTime = current ?? DateTime.Today;
		DateTime targetDate = target ?? GetTargetDate(timerMode);
		DisplayTimerMode? displayTimerMode = timerMode;
		if (!displayTimerMode.HasValue)
		{
			_ = TimerMode;
		}
		else
		{
			displayTimerMode.GetValueOrDefault();
		}
		_ = DaysOption;
		targetDate = GetDayOptionTargetDate(targetDate, timerMode);
		return (targetDate - dateTime).Days;
	}

	public DateTime GetDayOptionTargetDate(DateTime targetDate, DisplayTimerMode? timerMode = null)
	{
		DateTime today = DateTime.Today;
		DayStartOption daysOption = DaysOption;
		DisplayTimerMode displayTimerMode = timerMode ?? TimerMode;
		int days = (targetDate - today).Days;
		if (daysOption == DayStartOption.AddOneDay && !IgnoreYear && days <= 0 && (string.IsNullOrEmpty(RepeatFlag) || RepeatFlag.Contains("NONE") || displayTimerMode == DisplayTimerMode.CountUp))
		{
			return targetDate.AddDays(-1.0);
		}
		return targetDate;
	}

	public static string GetTypeText(CountDownType type)
	{
		return Utils.GetString(GetTypeResourceKey(type));
	}

	public static string GetTypeResourceKey(CountDownType type)
	{
		string result = string.Empty;
		switch (type)
		{
		case CountDownType.All:
			result = "All";
			break;
		case CountDownType.Holiday:
			result = "type_holiday";
			break;
		case CountDownType.Birthday:
			result = "type_birthday";
			break;
		case CountDownType.Anniversary:
			result = "type_anniversary";
			break;
		case CountDownType.Countdown:
			result = "type_countdown";
			break;
		}
		return result;
	}

	public static string GetTimerModeText(DisplayTimerMode type)
	{
		string key = string.Empty;
		switch (type)
		{
		case DisplayTimerMode.CountDown:
			key = "CountDownMode";
			break;
		case DisplayTimerMode.CountUp:
			key = "CountUpMode";
			break;
		}
		return Utils.GetString(key);
	}

	public bool CheckActualCanShowAge(DisplayTimerMode? timerMode = null)
	{
		timerMode = timerMode ?? new DisplayTimerMode?(TimerMode);
		if (ShowAge)
		{
			DateTime dateAsDateTime = GetDateAsDateTime();
			bool flag = RepeatUtils.CheckIfCountdownCustom(RepeatFlag, dateAsDateTime);
			if (Type == CountDownType.Birthday && !IgnoreYear && timerMode == DisplayTimerMode.CountDown && !string.IsNullOrEmpty(RepeatFlag) && RepeatFlag.Contains("FREQ=YEARLY") && (!RepeatFlag.Contains("INTERVAL") || RepeatFlag.Contains("INTERVAL=1")))
			{
				if (ShowCalendarType == ShowCalendarType.Gregorian)
				{
					return !flag;
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public string GetAge(DateTime targetDate)
	{
		if (Type != CountDownType.Birthday)
		{
			return string.Empty;
		}
		DateTime dateAsDateTime = GetDateAsDateTime();
		if (targetDate.Year < dateAsDateTime.Year)
		{
			return string.Empty;
		}
		int num = 0;
		if (ShowCalendarType == ShowCalendarType.Gregorian)
		{
			num = targetDate.Year - dateAsDateTime.Year;
			if (dateAsDateTime.AddYears(num) > targetDate)
			{
				num--;
			}
		}
		else
		{
			LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(dateAsDateTime);
			LunarUtils.ChineseCalendar chineseCalendar2 = new LunarUtils.ChineseCalendar(targetDate);
			num = chineseCalendar2.ChineseYear - chineseCalendar.ChineseYear;
			if (chineseCalendar2.ChineseMonth < chineseCalendar.ChineseMonth)
			{
				num--;
			}
			else if (chineseCalendar2.ChineseMonth == chineseCalendar.ChineseMonth && chineseCalendar2.ChineseDay < chineseCalendar.ChineseDay)
			{
				chineseCalendar2.GetMonthTotalDays();
				if (chineseCalendar2.ChineseDay >= chineseCalendar.ChineseDay)
				{
					num--;
				}
			}
		}
		if (num <= 0)
		{
			return string.Empty;
		}
		string text = null;
		text = ((num != 1) ? string.Format(Utils.GetString("reminder_xx_years_old"), num) : Utils.GetString("reminder_1_year_old"));
		return " (" + text + ")";
	}
}
