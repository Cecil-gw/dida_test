using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public static class ProjectWidgetsHelper
{
	private static List<WidgetWindow> _widgets;

	static ProjectWidgetsHelper()
	{
		_widgets = new List<WidgetWindow>();
		DataChangedNotifier.ListSectionOpenChanged += OnListSectionOpenChanged;
	}

	private static void OnListSectionOpenChanged(object sender, (string, SortOption) e)
	{
		List<WidgetWindow> widgets = _widgets;
		if (widgets == null || widgets.Count <= 0)
		{
			return;
		}
		foreach (WidgetWindow widget in _widgets)
		{
			ProjectIdentity projectIdentity = widget?.ProjectWidget?.ProjectIdentity;
			if (projectIdentity != null && !object.Equals(widget.ProjectWidget, sender) && projectIdentity.CatId == e.Item1 && projectIdentity.SortOption.Equal(e.Item2))
			{
				widget.ProjectWidget.Reload();
			}
		}
	}

	public static async Task AddDefaultWidget()
	{
		await ProjectWidgetDao.CreateInboxWidget();
		LoadWidgets();
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
	}

	public static async Task CloseProject(string widgetId)
	{
		_widgets = _widgets.Where((WidgetWindow widget) => widget.Model.Id != widgetId).ToList();
		await ProjectWidgetDao.DeleteWidgetById(widgetId);
	}

	public static async Task LoadWidgets()
	{
		List<WidgetSettingModel> list = await ProjectWidgetDao.GetWidgets();
		UtilLog.Info("LoadProjectWidgetsCount: " + list?.Count);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (WidgetSettingModel item in list)
		{
			if (!IsWidgetAdded(item.id))
			{
				WidgetWindow widgetWindow = new WidgetWindow(new WidgetViewModel(item));
				_widgets.Add(widgetWindow);
				widgetWindow.ShowWidget();
			}
		}
	}

	public static async Task<WidgetWindow> ReopenWidget(string widgetId)
	{
		if (IsWidgetAdded(widgetId))
		{
			WidgetWindow widgetWindow = _widgets.First((WidgetWindow widget) => widget.Model.Id == widgetId);
			if (widgetWindow != null)
			{
				widgetWindow.Close();
				_widgets.Remove(widgetWindow);
			}
			WidgetSettingModel widgetSettingModel = await ProjectWidgetDao.GetWidgetById(widgetId);
			if (widgetSettingModel != null)
			{
				WidgetWindow widgetWindow2 = new WidgetWindow(new WidgetViewModel(widgetSettingModel));
				_widgets.Add(widgetWindow2);
				widgetWindow2.ShowWidget();
				return widgetWindow2;
			}
		}
		return null;
	}

	private static bool IsWidgetAdded(string widgetId)
	{
		return _widgets.Exists((WidgetWindow widget) => widget.Model.Id == widgetId);
	}

	public static void Reload(bool loadDate = false)
	{
		if (_widgets == null || _widgets.Count <= 0)
		{
			return;
		}
		foreach (WidgetWindow widget in _widgets)
		{
			widget.Reload(loadDate);
		}
	}

	public static void OnShowCountDownChanged(string id, bool showCountDown)
	{
		if (_widgets == null || _widgets.Count <= 0)
		{
			return;
		}
		foreach (WidgetWindow widget in _widgets)
		{
			widget.ProjectWidget?.SetShowCountDown(LocalSettings.Settings.ShowCountDown);
		}
	}

	public static void OnProjectChanged(ProjectIdentity project)
	{
		if (project == null)
		{
			return;
		}
		List<WidgetWindow> widgets = _widgets;
		if (widgets == null || widgets.Count <= 0)
		{
			return;
		}
		foreach (WidgetWindow widget in _widgets)
		{
			if (widget?.ProjectWidget?.ProjectIdentity?.QueryId == project.QueryId)
			{
				widget?.ProjectWidget?.ResetProject();
				if (!(project is NormalProjectIdentity))
				{
					widget?.ProjectWidget?.Reload();
				}
			}
		}
	}

	public static void OnShowAddChanged()
	{
		List<WidgetWindow> widgets = _widgets;
		if (widgets == null || widgets.Count <= 0)
		{
			return;
		}
		foreach (WidgetWindow widget in _widgets)
		{
			widget?.ProjectWidget?.SetShowAdd();
		}
	}

	public static void ReloadCount()
	{
		List<WidgetWindow> widgets = _widgets;
		if (widgets == null || widgets.Count <= 0)
		{
			return;
		}
		foreach (WidgetWindow widget in _widgets)
		{
			widget?.ProjectWidget?.ResetCount();
		}
	}

	public static void ResetPosition()
	{
		List<WidgetWindow> widgets = _widgets;
		if (widgets == null || widgets.Count <= 0)
		{
			return;
		}
		foreach (WidgetWindow widget in _widgets)
		{
			widget?.ResetPosition();
		}
	}
}
