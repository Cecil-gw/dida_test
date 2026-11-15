using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class HabitKeepTotalTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2)
		{
			object obj = values[0];
			if (obj is bool)
			{
				if ((bool)obj)
				{
					return Utils.GetString("CurrentStreak");
				}
				if (values[1] is string result)
				{
					return result;
				}
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
