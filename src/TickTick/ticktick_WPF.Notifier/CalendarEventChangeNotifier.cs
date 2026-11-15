using System;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;

namespace ticktick_WPF.Notifier;

public static class CalendarEventChangeNotifier
{
	public static event EventHandler<TextExtra> TitleChanged;

	public static event EventHandler<TextExtra> SummaryChanged;

	public static event EventHandler<string> Deleted;

	public static event EventHandler<string> Restored;

	public static event EventHandler<CalendarEventModel> Changed;

	public static event EventHandler RemoteChanged;

	public static event EventHandler FontSizeChanged;

	public static void NotifyTitleChanged(string eventId, string text)
	{
		CalendarEventChangeNotifier.TitleChanged?.Invoke(null, new TextExtra
		{
			Id = eventId,
			Text = text
		});
	}

	public static void NotifySummaryChanged(string eventId, string text)
	{
		CalendarEventChangeNotifier.SummaryChanged?.Invoke(null, new TextExtra
		{
			Id = eventId,
			Text = text
		});
	}

	public static void NotifyEventDeleted(string eventId)
	{
		CalendarEventChangeNotifier.Deleted?.Invoke(null, eventId);
	}

	public static void NotifyEventChanged(CalendarEventModel model)
	{
		CalendarEventChangeNotifier.Changed?.Invoke(null, model);
	}

	public static void NotifyEventRestored(string eventId)
	{
		CalendarEventChangeNotifier.Restored?.Invoke(null, eventId);
	}

	public static void NotifyFontSizeChanged()
	{
		CalendarEventChangeNotifier.FontSizeChanged?.Invoke(null, null);
	}

	public static void NotifyRemoteChanged()
	{
		CalendarEventChangeNotifier.RemoteChanged?.Invoke(null, null);
		if (ABTestManager.IsNewRemindCalculate())
		{
			EventReminderCalculator.InitAllEventsReminderTimes();
		}
		else
		{
			ReminderCalculator.AssembleReminders();
		}
	}
}
