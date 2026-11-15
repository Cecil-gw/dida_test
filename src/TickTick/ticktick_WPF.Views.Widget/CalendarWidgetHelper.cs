using System;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public static class CalendarWidgetHelper
{
	public static WidgetWindow Widget { get; private set; }

	public static async Task AddWidget()
	{
		if (Widget == null)
		{
			await SingleWidgetDao.CreateWidget(UniqueWidgetMode.Calendar);
			await TryLoadWidget();
		}
		App.RefreshIconMenu();
		JumpHelper.InitJumpList();
		GlobalEventManager.NotifyWidgetOpenChanged();
	}

	public static async Task<WidgetWindow> TryLoadWidget()
	{
		Widget?.CloseWidget();
		CalendarWidgetModel calendarWidgetModel = await SingleWidgetDao.TryGetWidget(UniqueWidgetMode.Calendar);
		if (calendarWidgetModel != null)
		{
			WidgetViewModel model = new WidgetViewModel(calendarWidgetModel);
			WidgetWindow widget = new WidgetWindow(model);
			await Task.Delay(200);
			try
			{
				widget.ShowWidget();
			}
			catch (Exception)
			{
				widget = null;
			}
			Widget = widget;
			App.RefreshIconMenu();
			JumpHelper.InitJumpList();
		}
		else
		{
			Widget = null;
		}
		return Widget;
	}

	public static async Task HideWidget()
	{
		Widget?.CloseWidget();
		Widget = null;
	}

	public static async Task CloseWidget()
	{
		Widget?.CloseWidget();
		Widget = null;
		await SingleWidgetDao.DeleteWidget(UniqueWidgetMode.Calendar);
		App.RefreshIconMenu();
		JumpHelper.InitJumpList();
		GlobalEventManager.NotifyWidgetOpenChanged();
	}

	public static bool CanAddProject()
	{
		return Widget == null;
	}

	public static void CheckProEnable()
	{
		Widget?.CalendarWidget?.CheckCalendarProEnable();
	}

	public static void ReloadCal()
	{
		Widget?.CalendarWidget?.Reload();
	}

	public static void OnCurrentDateChanged()
	{
		Widget?.CalendarWidget?.CalendarControl?.OnCurrentDateChanged();
	}

	public static void ResetPosition()
	{
		Widget?.ResetPosition();
	}
}
