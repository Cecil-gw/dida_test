using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.CountDown.Infrastructure;

public class BooleanToPathConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length < 3 || !(values[0] is bool))
		{
			return DependencyProperty.UnsetValue;
		}
		if ((bool)values[0])
		{
			return values[1];
		}
		return values[2];
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
