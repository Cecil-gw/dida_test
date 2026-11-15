using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ProjectMaxWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 4)
		{
			double? num = (values[0] as double?) - (values[1] as double?) - (values[2] as double?) - (values[3] as double?) - 60.0;
			return (num < 0.0) ? new double?(0.0) : num;
		}
		return 88f;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
