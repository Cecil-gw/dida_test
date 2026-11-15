using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class PercentageToClipRectConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values[0] is double num && values[1] is double num2 && values[2] is double height)
		{
			double width = Math.Max(0.0, num / 100.0 * num2);
			return new Rect(0.0, 0.0, width, height);
		}
		return new Rect(0.0, 0.0, 0.0, 0.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
