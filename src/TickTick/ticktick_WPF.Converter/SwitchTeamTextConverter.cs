using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class SwitchTeamTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string value2 && !string.IsNullOrEmpty(value2))
		{
			return string.Format(Utils.GetString("MoveToA"), Utils.GetString("PersonalSpace"));
		}
		List<TeamModel> teams = CacheManager.GetTeams();
		if (!teams.Any())
		{
			return null;
		}
		return string.Format(Utils.GetString("MoveToA"), teams[0].name);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
