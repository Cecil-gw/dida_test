using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Converter;

public class TaskListTextPostponeConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length != 3)
		{
			return Visibility.Collapsed;
		}
		if (values[0] is string text && values[2] is bool flag)
		{
			object obj = ((values[1] is ProjectIdentity) ? values[1] : MainWindowManager.GetSelectedProject());
			if (text == "outdated" && (obj is TodayProjectIdentity || obj is WeekProjectIdentity || obj is AllProjectIdentity) && flag)
			{
				return Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
