using System;
using System.Collections.Generic;

namespace ticktick_WPF.Views;

public class ReminderSelectedEventArgs : EventArgs
{
	public List<string> Reminders { get; }

	public int? AnnoyingAlert { get; }

	public ReminderSelectedEventArgs(List<string> reminders, int? annoyingAlert)
	{
		Reminders = reminders;
		AnnoyingAlert = annoyingAlert;
	}
}
