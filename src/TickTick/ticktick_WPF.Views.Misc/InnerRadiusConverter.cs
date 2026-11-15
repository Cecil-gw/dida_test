using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.Misc;

public class InnerRadiusConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string text = parameter as string;
		double num = (value as double?) ?? 8.0;
		if (text == null)
		{
			return 20.0 - num;
		}
		double num2 = ((text == "1") ? (20.0 - num) : (num / 2.0));
		return new Size(num2, num2);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
