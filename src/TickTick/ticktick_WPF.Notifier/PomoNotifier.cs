using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Notifier;

public class PomoNotifier
{
	public static event EventHandler<PomoChangeArgs> Changed;

	public static event EventHandler<PomoLinkArgs> LinkChanged;

	public static event EventHandler<PomoPageChangeArgs> PageChanged;

	public static event EventHandler ServiceChanged;

	public static event EventHandler TimerChanged;

	public static event EventHandler PomoCommit;

	public static event EventHandler FocusingNoteChanged;

	public static void NotifyChanged(string pomoId, PomoChangeType type)
	{
		PomoChangeArgs e = new PomoChangeArgs
		{
			Id = pomoId,
			ChangeType = type
		};
		PomoNotifier.Changed?.Invoke(null, e);
		TickFocusManager.LoadStatistics();
	}

	public static void NotifyChanged(List<string> ids, PomoChangeType type)
	{
		PomoChangeArgs e = new PomoChangeArgs
		{
			Ids = ids.ToList(),
			ChangeType = type
		};
		PomoNotifier.Changed?.Invoke(null, e);
		TickFocusManager.LoadStatistics();
	}

	public static void NotifyServiceChanged()
	{
		PomoNotifier.ServiceChanged?.Invoke(null, null);
		TickFocusManager.LoadStatistics();
	}

	public static void NotifyTimerChanged()
	{
		PomoNotifier.TimerChanged?.Invoke(null, null);
	}

	public static void NotifyLinkChanged(string previewTimerId, string timerId)
	{
		PomoNotifier.LinkChanged?.Invoke(null, new PomoLinkArgs
		{
			PreviewTimerId = previewTimerId,
			NewTimerId = timerId
		});
	}

	public static void NotifyFocusNoteChanged(object sender)
	{
		PomoNotifier.FocusingNoteChanged?.Invoke(sender, null);
	}

	public static void NotifyPomoCommit()
	{
		PomoNotifier.PomoCommit?.Invoke(null, null);
	}

	public static void NotifyPageChange(PomoPageEventType eventType, object data = null, PomoDurationChangeSource source = PomoDurationChangeSource.EditDuration)
	{
		PomoNotifier.PageChanged?.Invoke(null, new PomoPageChangeArgs
		{
			EventType = eventType,
			Data = data,
			Source = source
		});
	}
}
