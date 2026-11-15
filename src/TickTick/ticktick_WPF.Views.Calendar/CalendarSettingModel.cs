using System;

namespace ticktick_WPF.Views.Calendar;

public class CalendarSettingModel
{
	public bool ShowMiniCal;

	public bool ShowArrange;

	public double MiniCalWidth = 244.0;

	public double ArrangeWidth = 244.0;

	public int MultiNum;

	public CalendarViewType HeadSwitch { get; set; }

	public string GetSwitchMode()
	{
		switch (HeadSwitch)
		{
		case CalendarViewType.Month:
		case CalendarViewType.Week:
		case CalendarViewType.Day:
		case CalendarViewType.Agenda:
		case CalendarViewType.Year:
			return ((int)HeadSwitch).ToString();
		case CalendarViewType.MultiDay:
			return "D" + Math.Min(14, Math.Max(1, MultiNum));
		case CalendarViewType.MultiWeek:
			return "W" + Math.Min(6, Math.Max(2, MultiNum));
		default:
			return "0";
		}
	}

	public string GetSwitchText()
	{
		return HeadSwitch switch
		{
			CalendarViewType.Day => "day", 
			CalendarViewType.Week => "week", 
			CalendarViewType.Month => "month", 
			CalendarViewType.MultiDay => "multi_day", 
			CalendarViewType.MultiWeek => "multi_week", 
			CalendarViewType.Agenda => "agenda", 
			CalendarViewType.Year => "year", 
			_ => "month", 
		};
	}
}
