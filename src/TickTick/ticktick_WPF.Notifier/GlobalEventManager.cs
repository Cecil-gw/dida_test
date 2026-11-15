using System;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;

namespace ticktick_WPF.Notifier;

public static class GlobalEventManager
{
	public static event EventHandler<double> PomoWindowOpacityChanged;

	public static event EventHandler<string> PomoWindowThemeChanged;

	public static event EventHandler PomoDailyGoalChanged;

	public static event EventHandler TimeFormatChanged;

	public static event EventHandler ReloadCalendar;

	public static event EventHandler ModifyRecurrenceCompleted;

	public static event EventHandler<RemindMessage> RemindHandled;

	public static event EventHandler ArrangeFilterTypeChanged;

	public static event EventHandler ArrangeTaskSortTypeChanged;

	public static event EventHandler QuadrantSortChanged;

	public static event EventHandler StickyOpacityChanged;

	public static event EventHandler WidgetOpenChanged;

	public static event EventHandler<string> TimelineSetChanged;

	public static event EventHandler<bool> TimeLineTimeZonesChanged;

	public static event EventHandler<int> QuickAddStyleChanged;

	public static void NotifyWidgetOpenChanged()
	{
		GlobalEventManager.WidgetOpenChanged?.Invoke(null, null);
	}

	public static void NotifyPomoOpacityChanged(double opacity)
	{
		GlobalEventManager.PomoWindowOpacityChanged?.Invoke(null, opacity);
	}

	public static void NotifyPomoThemeChanged(string theme)
	{
		GlobalEventManager.PomoWindowThemeChanged?.Invoke(null, theme);
	}

	public static void NotifyPomoDailyGoalChanged()
	{
		GlobalEventManager.PomoDailyGoalChanged?.Invoke(null, null);
	}

	public static void NotifyTimeFormatChanged()
	{
		GlobalEventManager.TimeFormatChanged?.Invoke(null, null);
	}

	public static void NotifyRemindHandled(RemindMessage remindMessage)
	{
		GlobalEventManager.RemindHandled?.Invoke(null, remindMessage);
	}

	public static void NotifyReloadCalendar()
	{
		Application.Current.Dispatcher?.Invoke(delegate
		{
			GlobalEventManager.ReloadCalendar?.Invoke(null, null);
		});
	}

	public static void NotifyModifyRecurrenceCompleted()
	{
		GlobalEventManager.ModifyRecurrenceCompleted?.Invoke(null, null);
	}

	public static void NotifyArrangeTaskDateTypeChanged()
	{
		GlobalEventManager.ArrangeFilterTypeChanged?.Invoke(null, null);
	}

	public static void NotifyArrangeDisplayTypeChanged()
	{
		GlobalEventManager.ArrangeTaskSortTypeChanged?.Invoke(null, null);
	}

	public static void NotifyQuadrantSortChanged(object sender)
	{
		GlobalEventManager.QuadrantSortChanged?.Invoke(sender, null);
	}

	internal static void OnStickyOpacityChanged()
	{
		GlobalEventManager.StickyOpacityChanged?.Invoke(null, null);
	}

	public static void NotifyTimelineSetChanged(string name)
	{
		GlobalEventManager.TimelineSetChanged?.Invoke(null, name);
	}

	public static async void NotifyTimeLineTimeZonesChangedChanged(bool countChanged, bool delay = false)
	{
		if (delay)
		{
			await Task.Delay(4000);
		}
		GlobalEventManager.TimeLineTimeZonesChanged?.Invoke(null, countChanged);
	}

	public static void NotifyQuickAddStyleChanged(int style)
	{
		GlobalEventManager.QuickAddStyleChanged?.Invoke(null, style);
	}
}
