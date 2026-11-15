using System;
using System.Windows;

namespace ticktick_WPF.Views.CountDown.Views;

public class OpenImageFinishedEventArgs : EventArgs
{
	public string Path { get; set; }

	public Rect ClipRect { get; set; }

	public OpenImageFinishedEventArgs(string message, Rect rect)
	{
		Path = message;
		ClipRect = rect;
	}
}
