using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class LineHeightConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length != 2)
		{
			return 0f;
		}
		double num = (double)values[0];
		return (double)values[1] * num;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
