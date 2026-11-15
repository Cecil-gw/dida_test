using System;
using System.Windows;

namespace ticktick_WPF.Views.Calendar.Month;

public class WeekEventModel
{
	public int Row { get; set; }

	public int DragDiffDays { get; set; }

	public int DisplayColumn => Column + DragDiffDays;

	public int Column { get; set; }

	public int ColumnSpan { get; set; }

	public DateTime TargetDate { get; set; }

	public CalendarDisplayViewModel Data { get; set; }

	public Rect Position { get; set; }

	public void SetPosition()
	{
	}
}
