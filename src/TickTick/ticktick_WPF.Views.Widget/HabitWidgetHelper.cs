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

internal class HabitWidgetHelper
{
	private static List<WidgetWindow> _habitWidgets { get; set; } = new List<WidgetWindow>();

	private static List<WidgetWindow> _habitWeekWidgets { get; set; } = new List<WidgetWindow>();

	public static WidgetWindow ListWidget { get; private set; }

	public static async Task AddWidget(WidgetType type)
	{
		switch (type)
		{
		case WidgetType.HabitItem:
			await HabitWidgetDao.CreateHabitItemWidget();
			await LoadHabitItemWidgets();
			break;
		case WidgetType.HabitList:
			if (ListWidget == null)
			{
				await SingleWidgetDao.CreateWidget(UniqueWidgetMode.HabitList);
				await LoadListWidget();
			}
			GlobalEventManager.NotifyWidgetOpenChanged();
			break;
		case WidgetType.HabitWeekItem:
			await HabitWeekWidgetDao.CreateHabitWeekItemWidget();
			await LoadHabitWeekItemWidgets();
			break;
		}
	}

	public static async Task LoadAllWidgets()
	{
		await LoadHabitItemWidgets();
		await LoadHabitWeekItemWidgets();
		await LoadListWidget();
	}

	public static async Task LoadHabitItemWidgets()
	{
		List<HabitWidgetModel> list = await WidgetBaseDao<HabitWidgetModel>.GetWidgets();
		UtilLog.Info("LoadHabitItemWidgetsCount: " + list?.Count);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (HabitWidgetModel widget in list)
		{
			if (!_habitWidgets.Any((WidgetWindow x) => x.Model.Id == widget.Id))
			{
				WidgetViewModel model = new WidgetViewModel(widget);
				WidgetWindow window = new WidgetWindow(model);
				_habitWidgets.Add(window);
				await Task.Delay(200);
				window.ShowWidget();
			}
		}
	}

	public static async Task LoadHabitWeekItemWidgets()
	{
		List<HabitWeekWidgetModel> list = await WidgetBaseDao<HabitWeekWidgetModel>.GetWidgets();
		UtilLog.Info("LoadHabitWeekItemWidgetsCount: " + list?.Count);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (HabitWeekWidgetModel widget in list)
		{
			if (!_habitWeekWidgets.Any((WidgetWindow x) => x.Model.Id == widget.Id))
			{
				WidgetViewModel model = new WidgetViewModel(widget);
				WidgetWindow window = new WidgetWindow(model);
				_habitWeekWidgets.Add(window);
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
		CalendarWidgetModel calendarWidgetModel = await SingleWidgetDao.TryGetWidget(UniqueWidgetMode.HabitList);
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

	public static async Task<WidgetWindow> ReopenHabitItemWidget(string widgetId)
	{
		if (_habitWidgets.Any((WidgetWindow x) => x.Model.Id == widgetId))
		{
			WidgetWindow widgetWindow = _habitWidgets.First((WidgetWindow widget) => widget.Model.Id == widgetId);
			if (widgetWindow != null)
			{
				widgetWindow.Close();
				_habitWidgets.Remove(widgetWindow);
			}
			HabitWidgetModel habitWidgetModel = await WidgetBaseDao<HabitWidgetModel>.GetWidgetById(widgetId);
			if (habitWidgetModel != null)
			{
				WidgetWindow widgetWindow2 = new WidgetWindow(new WidgetViewModel(habitWidgetModel));
				_habitWidgets.Add(widgetWindow2);
				widgetWindow2.ShowWidget();
				return widgetWindow2;
			}
		}
		return null;
	}

	public static async Task<WidgetWindow> ReopenHabitWeekItemWidget(string widgetId)
	{
		if (_habitWeekWidgets.Any((WidgetWindow x) => x.Model.Id == widgetId))
		{
			WidgetWindow widgetWindow = _habitWeekWidgets.First((WidgetWindow widget) => widget.Model.Id == widgetId);
			if (widgetWindow != null)
			{
				widgetWindow.Close();
				_habitWeekWidgets.Remove(widgetWindow);
			}
			HabitWeekWidgetModel habitWeekWidgetModel = await WidgetBaseDao<HabitWeekWidgetModel>.GetWidgetById(widgetId);
			if (habitWeekWidgetModel != null)
			{
				WidgetWindow widgetWindow2 = new WidgetWindow(new WidgetViewModel(habitWeekWidgetModel));
				_habitWeekWidgets.Add(widgetWindow2);
				widgetWindow2.ShowWidget();
				return widgetWindow2;
			}
		}
		return null;
	}

	public static async Task CloseHabitItemWidget(string widgetId)
	{
		_habitWidgets = _habitWidgets.Where((WidgetWindow widget) => widget.Model.Id != widgetId).ToList();
		await WidgetBaseDao<HabitWidgetModel>.DeleteWidgetById(widgetId);
	}

	public static async Task CloseHabitWeekItemWidget(string widgetId)
	{
		_habitWeekWidgets = _habitWeekWidgets.Where((WidgetWindow widget) => widget.Model.Id != widgetId).ToList();
		await WidgetBaseDao<HabitWeekWidgetModel>.DeleteWidgetById(widgetId);
	}

	public static void CloseAll()
	{
		if (_habitWidgets.Count > 0)
		{
			for (int i = 0; i < _habitWidgets.Count; i++)
			{
				_habitWidgets[i].CloseWidget();
			}
		}
		if (_habitWeekWidgets.Count > 0)
		{
			for (int j = 0; j < _habitWeekWidgets.Count; j++)
			{
				_habitWeekWidgets[j].CloseWidget();
			}
		}
		_habitWidgets = new List<WidgetWindow>();
		_habitWeekWidgets = new List<WidgetWindow>();
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
		await SingleWidgetDao.DeleteWidget(UniqueWidgetMode.HabitList);
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
		List<WidgetWindow> habitWidgets = _habitWidgets;
		if (habitWidgets != null && habitWidgets.Count > 0)
		{
			foreach (WidgetWindow habitWidget in _habitWidgets)
			{
				habitWidget?.ResetPosition();
			}
		}
		List<WidgetWindow> habitWeekWidgets = _habitWeekWidgets;
		if (habitWeekWidgets != null && habitWeekWidgets.Count > 0)
		{
			foreach (WidgetWindow habitWeekWidget in _habitWeekWidgets)
			{
				habitWeekWidget?.ResetPosition();
			}
		}
		ListWidget?.ResetPosition();
	}

	public static void OnHabitChanged(string widgetId)
	{
		WidgetWindow widgetWindow = _habitWidgets.FirstOrDefault((WidgetWindow x) => x.Model.Id == widgetId);
		widgetWindow?.HabitItemWidget?.LoadData();
		if (widgetWindow == null)
		{
			_habitWeekWidgets.FirstOrDefault((WidgetWindow x) => x.Model.Id == widgetId)?.HabitWeekWidget?.LoadData();
		}
	}
}
