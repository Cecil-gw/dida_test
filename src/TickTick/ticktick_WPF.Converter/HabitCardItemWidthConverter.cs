using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class HabitCardItemWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3)
		{
			double num = (values[0] as double?) ?? 500.0;
			if ((values[1] as bool? == true || values[2] as int? == 1) && num > 28.0)
			{
				num -= 28.0;
				if (num >= 1400.0)
				{
					return num / 5.0;
				}
				if (num >= 1120.0)
				{
					return num / 4.0;
				}
				if (num >= 840.0)
				{
					return num / 3.0;
				}
				return num / 2.0;
			}
			return num - 28.0;
		}
		return 236;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
