using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Twitter;

namespace ticktick_WPF.ViewModels;

public class SettingsMenuHelper
{
	public static bool IsNewFeature(SettingsType type)
	{
		if (type == SettingsType.Matrix)
		{
			bool flag = !LocalSettings.Settings.CheckedNewFeature.Contains(SettingsType.Matrix.ToString()) && DateTime.Today < new DateTime(2022, 5, 5);
			if (flag && LocalSettings.Settings.ShowMatrix)
			{
				flag = false;
				CheckNewFeature(SettingsType.Matrix);
			}
			return flag;
		}
		return false;
	}

	public static void CheckNewFeature(SettingsType type)
	{
		List<string> list = LocalSettings.Settings.CheckedNewFeature.Split(';').ToList();
		list.Remove("");
		if (!list.Contains(type.ToString()))
		{
			list.Add(type.ToString());
		}
		LocalSettings.Settings.CheckedNewFeature = list.Join(";");
	}
}
