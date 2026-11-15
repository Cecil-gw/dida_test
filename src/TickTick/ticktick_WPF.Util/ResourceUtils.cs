using System.Collections.Generic;
using System.Windows;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public static class ResourceUtils
{
	private static List<double> _columnWidths = new List<double> { 242.0, 282.0, 342.0 };

	public static void SetKanbanColumnWidth()
	{
		double num = 282.0;
		int kbSize = LocalSettings.Settings.ExtraSettings.KbSize;
		if (kbSize < 3 && kbSize >= 0)
		{
			num = _columnWidths[kbSize];
		}
		if (Application.Current != null)
		{
			Application.Current.Resources["KanbanColumnWidth"] = num;
		}
	}

	public static double GetDoubleResource(string key, double defaultVal = 0.0)
	{
		object obj = Application.Current.TryFindResource(key);
		if (obj is double)
		{
			return (double)obj;
		}
		return defaultVal;
	}
}
