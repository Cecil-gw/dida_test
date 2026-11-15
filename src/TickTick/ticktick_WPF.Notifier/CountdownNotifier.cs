using System;
using ticktick_WPF.Models;

namespace ticktick_WPF.Notifier;

public class CountdownNotifier : EventArgs
{
	public CountdownModel Model { get; set; }

	public CountdownNotifyType? Type { get; set; }
}
