using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class MaxValueConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		return values.Where((object item) => item is double).Cast<double>().Max();
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
