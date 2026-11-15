using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class LinkTextReadOnlyConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is bool flag && values[1] is bool flag2)
		{
			return !flag || !flag2;
		}
		return false;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
