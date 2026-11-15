using System;

namespace ticktick_WPF.Notifier;

internal class WindowNotifier
{
	public static event EventHandler<SubWindowType> OnSubWindowOpen;

	public static void NotifySubWindowOpen(SubWindowType type)
	{
		WindowNotifier.OnSubWindowOpen?.Invoke(null, type);
	}
}
