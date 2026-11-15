using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Converter;

public class SharedProjectAssignVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string projectId)
		{
			ProjectModel projectById = CacheManager.GetProjectById(projectId);
			if (projectById != null && projectById.IsShareList())
			{
				return Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
