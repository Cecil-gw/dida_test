using System;
using Newtonsoft.Json;
using ticktick_WPF.Notifier;

namespace ticktick_WPF.Models;

public class CalendarConfig : PreferenceBaseModel
{
	public enum CalendarItemStyle
	{
		Modern,
		Classic
	}

	[JsonProperty("itemStyle")]
	public string ItemStyle { get; set; }

	[JsonIgnore]
	public CalendarItemStyle? ItemStyleType
	{
		get
		{
			if (string.IsNullOrEmpty(ItemStyle))
			{
				return null;
			}
			if (Enum.TryParse<CalendarItemStyle>(ItemStyle, ignoreCase: true, out var result))
			{
				return result;
			}
			return null;
		}
		set
		{
			ItemStyle = value.ToString().ToLower();
		}
	}

	[JsonProperty("checkbox")]
	public bool? CheckBox { get; set; }

	[JsonProperty("heatmap")]
	public bool? Heatmap { get; set; }

	[JsonProperty("event")]
	public bool? Event { get; set; }

	[JsonProperty("note")]
	public bool? Note { get; set; }

	[JsonProperty("habit")]
	public bool? Habit { get; set; }

	[JsonProperty("focus")]
	public bool? Focus { get; set; }

	[JsonProperty("countdown")]
	public bool? Countdown { get; set; }

	[JsonProperty("course")]
	public bool? Course { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel remote)
	{
		if (remote is CalendarConfig calendarConfig)
		{
			if (calendarConfig.mtime > base.mtime)
			{
				bool num = CheckBox != calendarConfig.CheckBox || Event != calendarConfig.Event || Note != calendarConfig.Note || Habit != calendarConfig.Habit || Focus != calendarConfig.Focus || Countdown != calendarConfig.Countdown || Course != calendarConfig.Course;
				bool flag = ItemStyle != calendarConfig.ItemStyle;
				ItemStyle = calendarConfig.ItemStyle;
				CheckBox = calendarConfig.CheckBox;
				Heatmap = calendarConfig.Heatmap;
				Event = calendarConfig.Event;
				Note = calendarConfig.Note;
				Habit = calendarConfig.Habit;
				Focus = calendarConfig.Focus;
				Countdown = calendarConfig.Countdown;
				Course = calendarConfig.Course;
				base.mtime = calendarConfig.mtime;
				if (num)
				{
					DataChangedNotifier.NotifyCalendarShowIconChanged();
				}
				if (flag)
				{
					DataChangedNotifier.NotifyCalendarTaskbarStyleChanged();
				}
			}
			if (remote.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
