using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class TabBarModel
{
	public const string AIAssistant = "AI_ASSISTANT";

	public string name { get; set; }

	public string status { get; set; }

	public long sortOrder { get; set; }

	public static List<TabBarModel> InitTabBars()
	{
		return new List<TabBarModel>
		{
			new TabBarModel
			{
				name = "TASK",
				sortOrder = 0L,
				status = "active"
			},
			new TabBarModel
			{
				name = "CALENDAR",
				sortOrder = 1L,
				status = "active"
			},
			new TabBarModel
			{
				name = "MATRIX",
				sortOrder = 2L,
				status = GetMatrixInitStatus()
			},
			new TabBarModel
			{
				name = "POMO",
				sortOrder = 3L,
				status = (LocalSettings.Settings.PomoLocalSetting.PomoTimer ? "active" : "inactive")
			},
			new TabBarModel
			{
				name = "HABIT",
				sortOrder = 4L,
				status = (LocalSettings.Settings.SettingsModel.ShowHabit ? "active" : "inactive")
			},
			new TabBarModel
			{
				name = "COUNTDOWN",
				sortOrder = 5L,
				status = "inactive"
			},
			new TabBarModel
			{
				name = "AI_ASSISTANT",
				sortOrder = 6L,
				status = "inactive"
			},
			new TabBarModel
			{
				name = "SEARCH",
				sortOrder = 7L,
				status = "active"
			}
		};
	}

	public static List<TabBarModel> GetNavigationTabBars()
	{
		List<TabBarModel> list = InitTabBars();
		List<TabBarModel> list2 = LocalSettings.Settings.UserPreference?.desktopTabBars?.bars;
		if (list2 == null || list2.Count == 0)
		{
			list2 = list;
		}
		HashSet<string> supportedNames = new HashSet<string>(from bar in list
			where bar.name != "SEARCH"
			select bar.name, StringComparer.OrdinalIgnoreCase);
		return (from @group in list2.Where((TabBarModel bar) => bar != null && supportedNames.Contains(bar.name)).GroupBy((TabBarModel bar) => bar.name, StringComparer.OrdinalIgnoreCase)
			select @group.First() into bar
			where bar.status == "active"
			where !Utils.IsWindows7() || !string.Equals(bar.name, "AI_ASSISTANT", StringComparison.OrdinalIgnoreCase)
			orderby bar.sortOrder
			select bar).ToList();
	}

	public static string GetConfigName(string moduleName)
	{
		if (!string.Equals(moduleName, "AI", StringComparison.OrdinalIgnoreCase))
		{
			return moduleName;
		}
		return "AI_ASSISTANT";
	}

	public static string GetModuleName(string configName)
	{
		if (!string.Equals(configName, "AI_ASSISTANT", StringComparison.OrdinalIgnoreCase))
		{
			return configName;
		}
		return "AI";
	}

	private static string GetMatrixInitStatus()
	{
		string text = ((LocalSettings.Settings.UserPreference?.desktop_conf?.tabs?.Contains("matrix") == true) ? "active" : "inactive");
		if (string.Equals(text, "inactive") && ABTestManager.IsTabWithMatrix())
		{
			text = "active";
		}
		return text;
	}

	public static bool IsActive(string name, List<TabBarModel> bars)
	{
		if (string.Equals(name, "AI_ASSISTANT", StringComparison.OrdinalIgnoreCase) && Utils.IsWindows7())
		{
			return false;
		}
		name = name.ToLower(CultureInfo.InvariantCulture);
		if (bars == null || bars.Count == 0)
		{
			bars = InitTabBars();
		}
		TabBarModel tabBarModel = bars.FirstOrDefault((TabBarModel t) => t.name.ToLower(CultureInfo.InvariantCulture) == name);
		if (tabBarModel != null)
		{
			return tabBarModel.status == "active";
		}
		return false;
	}

	public static long GetTabBarSort(string name, List<TabBarModel> bars)
	{
		name = name?.ToLower(CultureInfo.InvariantCulture);
		if (bars == null || bars.Count == 0)
		{
			bars = InitTabBars();
		}
		return bars.FirstOrDefault((TabBarModel t) => t != null && t.name != null && t.name.ToLower(CultureInfo.InvariantCulture) == name)?.sortOrder ?? (bars.Max((TabBarModel m) => m.sortOrder) + 1);
	}
}
