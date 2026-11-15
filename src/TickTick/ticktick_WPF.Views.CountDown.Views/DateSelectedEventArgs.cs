using System;

namespace ticktick_WPF.Views.CountDown.Views;

public class DateSelectedEventArgs : EventArgs
{
	public DateTime Date { get; set; }

	public bool IsGregorianCalendar { get; set; }

	public bool IgnoreYear { get; set; }
}
