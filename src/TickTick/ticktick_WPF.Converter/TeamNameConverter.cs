using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TeamNameConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			string teamId = value as string;
			if (teamId != null && !string.IsNullOrEmpty(teamId))
			{
				TeamModel teamModel = CacheManager.GetTeams().FirstOrDefault((TeamModel t) => t.id == teamId);
				if (teamModel != null)
				{
					return teamModel.name;
				}
			}
		}
		return Utils.GetString("PersonalSpace");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
