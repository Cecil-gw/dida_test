using System;

namespace ticktick_WPF.Views.Misc;

public class CustomSnoozeConfirmedEventArgs : EventArgs
{
	public int Minutes { get; }

	public CustomSnoozeConfirmedEventArgs(int minutes)
	{
		Minutes = minutes;
	}
}
