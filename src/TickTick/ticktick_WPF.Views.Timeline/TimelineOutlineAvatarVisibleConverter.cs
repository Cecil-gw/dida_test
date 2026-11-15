using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineOutlineAvatarVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is double num)
		{
			if (num < 70.0)
			{
				return Visibility.Collapsed;
			}
			if (values[1] != null)
			{
				switch (values[1] as string)
				{
				case "":
				case "-1":
					break;
				default:
					goto IL_005e;
				}
			}
			return Visibility.Collapsed;
		}
		goto IL_005e;
		IL_005e:
		return Visibility.Visible;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
