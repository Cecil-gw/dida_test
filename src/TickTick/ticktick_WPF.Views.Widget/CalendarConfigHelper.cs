using System;
using System.Windows;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public static class CalendarConfigHelper
{
	private static WidgetSettings _settingWindow;

	public static WidgetSettings TryShowSettings(WidgetViewModel model, Window owner = null)
	{
		if (_settingWindow == null)
		{
			_settingWindow = new WidgetSettings(model, isSingle: true);
			if (owner != null)
			{
				owner.Activate();
				_settingWindow.Owner = owner;
			}
			_settingWindow.Closed -= OnWindowClosed;
			_settingWindow.Closed += OnWindowClosed;
		}
		_settingWindow.Show();
		return _settingWindow;
	}

	public static WidgetSettings TryRegisterOnReload(WidgetViewModel model)
	{
		if (_settingWindow != null)
		{
			_settingWindow.DataContext = model;
			return _settingWindow;
		}
		return null;
	}

	private static void OnWindowClosed(object sender, EventArgs e)
	{
		_settingWindow = null;
	}
}
