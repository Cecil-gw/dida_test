using System;

namespace ticktick_WPF.Views.Calendar;

public class CalendarDetailConvertEventArgs : EventArgs
{
	public CalendarDetailViewModel Model { get; }

	public bool IsNote { get; }

	public bool ManualTimeSelected { get; }

	public CalendarDetailConvertEventArgs(CalendarDetailViewModel model, bool isNote, bool manualTimeSelected = false)
	{
		Model = model;
		IsNote = isNote;
		ManualTimeSelected = manualTimeSelected;
	}
}
