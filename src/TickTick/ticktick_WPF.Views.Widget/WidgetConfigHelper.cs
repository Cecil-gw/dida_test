using System;
using System.Collections.Generic;
using System.Windows;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public static class WidgetConfigHelper
{
	private static readonly Dictionary<string, WidgetSettings> SettingWindows = new Dictionary<string, WidgetSettings>();

	public static WidgetSettings TryGetSettings(WidgetViewModel model)
	{
		SettingWindows.TryGetValue(model.Id, out var value);
		return value;
	}

	public static WidgetSettings TryShowSettings(WidgetViewModel model, Window owner = null)
	{
		SettingWindows.TryGetValue(model.Id, out var value);
		if (value == null)
		{
			WidgetSettings widgetSettings = new WidgetSettings(model, model.IsSingleMode);
			widgetSettings.Closed += OnWindowClosed;
			if (owner != null)
			{
				owner.Activate();
				widgetSettings.Owner = owner;
			}
			SettingWindows.Add(model.Id, widgetSettings);
			widgetSettings.Show();
			return widgetSettings;
		}
		value.Rebind(model);
		return value;
	}

	private static void OnWindowClosed(object sender, EventArgs e)
	{
		WidgetSettings widgetSettings = (WidgetSettings)sender;
		SettingWindows.Remove(widgetSettings.WidgetId);
	}
}
