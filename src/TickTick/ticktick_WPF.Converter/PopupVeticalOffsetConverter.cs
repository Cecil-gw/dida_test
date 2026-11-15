using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class PopupVeticalOffsetConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values[0] is double num && values[1] is double num2)
		{
			return num / 2.0 + num2 / 2.0 + 11.0;
		}
		return 0;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
