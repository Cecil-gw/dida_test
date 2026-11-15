using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Calendar;

public class TimeLineExpandHeightConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is int num && values[1] is double num2 && parameter is string text)
		{
			if (text == "True")
			{
				return (double)num * num2;
			}
			return (double)(25 - num) * num2;
		}
		return 0;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
