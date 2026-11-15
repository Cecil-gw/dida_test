using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class HabitKeepTotalCountTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3)
		{
			object obj = values[0];
			if (obj is bool)
			{
				if ((bool)obj)
				{
					int valueOrDefault = (values[1] as int?).GetValueOrDefault();
					return valueOrDefault + " " + Utils.GetString((valueOrDefault > 1) ? "PublicDays" : "PublicDay");
				}
				return values[2] as string;
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
