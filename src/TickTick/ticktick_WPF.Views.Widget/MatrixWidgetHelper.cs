using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public class MatrixWidgetHelper
{
	public static WidgetWindow Widget { get; private set; }

	public static async Task AddWidget()
	{
		if (Widget == null)
		{
			await SingleWidgetDao.CreateWidget(UniqueWidgetMode.Matrix);
			await TryLoadWidget();
		}
		App.RefreshIconMenu();
		JumpHelper.InitJumpList();
		GlobalEventManager.NotifyWidgetOpenChanged();
	}

	public static async Task<WidgetWindow> TryLoadWidget()
	{
		if (Widget != null)
		{
			Widget.CloseWidget();
			Widget = null;
		}
		CalendarWidgetModel calendarWidgetModel = await SingleWidgetDao.TryGetWidget(UniqueWidgetMode.Matrix);
		if (calendarWidgetModel != null)
		{
			Widget = new WidgetWindow(new WidgetViewModel(calendarWidgetModel));
			await Task.Delay(200);
			Widget.ShowWidget();
			App.RefreshIconMenu();
			JumpHelper.InitJumpList();
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
		await SingleWidgetDao.DeleteWidget(UniqueWidgetMode.Matrix);
		App.RefreshIconMenu();
		JumpHelper.InitJumpList();
		GlobalEventManager.NotifyWidgetOpenChanged();
	}

	public static bool CanAddWidget()
	{
		return Widget == null;
	}

	public static void CheckProEnable()
	{
		Widget?.MatrixWidget?.CheckProEnable();
	}

	public static void ResetPosition()
	{
		Widget?.ResetPosition();
	}
}
