using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.CountDown.Infrastructure;

public class SaveIconEnableConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3)
		{
			int? num = values[0] as int?;
			string value = values[1] as string;
			string value2 = values[2] as string;
			if (num == 0 && !string.IsNullOrEmpty(value))
			{
				return true;
			}
			if (num != 0 && !string.IsNullOrEmpty(value2))
			{
				return true;
			}
			return false;
		}
		return true;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
