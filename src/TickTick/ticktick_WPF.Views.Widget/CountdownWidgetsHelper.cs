using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

internal class CountdownWidgetsHelper
{
	private static List<WidgetWindow> _widgets { get; set; } = new List<WidgetWindow>();

	public static WidgetWindow ListWidget { get; private set; }

	public static async Task AddWidget(WidgetType type)
	{
		switch (type)
		{
		case WidgetType.CountdownItem:
			await CountdownWidgetDao.CreateWidget();
			await LoadSingleWidgets();
			break;
		case WidgetType.CountdownList:
			if (ListWidget == null)
			{
				await SingleWidgetDao.CreateWidget(UniqueWidgetMode.CountdownList);
				await LoadListWidget();
			}
			GlobalEventManager.NotifyWidgetOpenChanged();
			break;
		}
	}

	public static async Task LoadAllWidgets()
	{
		await LoadSingleWidgets();
		await LoadListWidget();
	}

	public static async Task LoadSingleWidgets()
	{
		List<CountdownWidgetModel> list = await WidgetBaseDao<CountdownWidgetModel>.GetWidgets();
		UtilLog.Info("LoadCountdownWidgetsCount: " + list?.Count);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (CountdownWidgetModel widget in list)
		{
			if (!_widgets.Any((WidgetWindow x) => x.Model.Id == widget.Id))
			{
				WidgetViewModel model = new WidgetViewModel(widget);
				WidgetWindow window = new WidgetWindow(model);
				_widgets.Add(window);
				await Task.Delay(200);
				window.ShowWidget();
			}
		}
	}

	public static async Task<WidgetWindow> LoadListWidget()
	{
		if (ListWidget != null)
		{
			ListWidget.CloseWidget();
			ListWidget = null;
		}
		CalendarWidgetModel calendarWidgetModel = await SingleWidgetDao.TryGetWidget(UniqueWidgetMode.CountdownList);
		if (calendarWidgetModel != null)
		{
			ListWidget = new WidgetWindow(new WidgetViewModel(calendarWidgetModel));
			await Task.Delay(200);
			ListWidget.ShowWidget();
			App.RefreshIconMenu();
			JumpHelper.InitJumpList();
		}
		return ListWidget;
	}

	public static async Task<WidgetWindow> ReopenWidget(string widgetId)
	{
		if (_widgets.Any((WidgetWindow x) => x.Model.Id == widgetId))
		{
			WidgetWindow widgetWindow = _widgets.First((WidgetWindow widget) => widget.Model.Id == widgetId);
			if (widgetWindow != null)
			{
				widgetWindow.Close();
				_widgets.Remove(widgetWindow);
			}
			CountdownWidgetModel countdownWidgetModel = await WidgetBaseDao<CountdownWidgetModel>.GetWidgetById(widgetId);
			if (countdownWidgetModel != null)
			{
				WidgetWindow widgetWindow2 = new WidgetWindow(new WidgetViewModel(countdownWidgetModel));
				_widgets.Add(widgetWindow2);
				widgetWindow2.ShowWidget();
				return widgetWindow2;
			}
		}
		return null;
	}

	public static async Task CloseSingleCountdown(string widgetId)
	{
		_widgets = _widgets.Where((WidgetWindow widget) => widget.Model.Id != widgetId).ToList();
		await WidgetBaseDao<CountdownWidgetModel>.DeleteWidgetById(widgetId);
	}

	public static void CloseAll()
	{
		if (_widgets.Count > 0)
		{
			for (int i = 0; i < _widgets.Count; i++)
			{
				_widgets[i].CloseWidget();
			}
		}
		_widgets = new List<WidgetWindow>();
		HideListWidget();
	}

	public static async Task HideListWidget()
	{
		ListWidget?.CloseWidget();
		ListWidget = null;
	}

	public static async Task CloseListWidget()
	{
		ListWidget?.CloseWidget();
		ListWidget = null;
		await SingleWidgetDao.DeleteWidget(UniqueWidgetMode.CountdownList);
		App.RefreshIconMenu();
		JumpHelper.InitJumpList();
		GlobalEventManager.NotifyWidgetOpenChanged();
	}

	public static bool CanAddListWidget()
	{
		return ListWidget == null;
	}

	public static void ResetPosition()
	{
		List<WidgetWindow> widgets = _widgets;
		if (widgets != null && widgets.Count > 0)
		{
			foreach (WidgetWindow widget in _widgets)
			{
				widget?.ResetPosition();
			}
		}
		ListWidget?.ResetPosition();
	}

	public static void OnShowListUnitChange()
	{
		ListWidget?.CountDownListWidget?.OnShowUnitChange();
	}

	public static void OnStyleChange(string widgetId)
	{
		_widgets.FirstOrDefault((WidgetWindow x) => x.Model.Id == widgetId)?.CountDownItemWidget?.LoadData();
	}

	public static void SaveCountdownDateFormat(string widgetId, DateDisplayFormatType formatType)
	{
		WidgetWindow widgetWindow = _widgets.FirstOrDefault((WidgetWindow x) => x.Model.Id == widgetId);
		widgetWindow.Model.CountdownDateFormatType = formatType;
		widgetWindow.Model.Save();
	}
}
