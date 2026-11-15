using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class ItemIconOpacityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3 && values[0] is DisplayType displayType && values[1] is int num && values[2] is int num2)
		{
			if (displayType != DisplayType.Task && displayType != DisplayType.CheckItem && displayType != DisplayType.Agenda)
			{
				return 0.36;
			}
			if (num2 != 0)
			{
				return 0.18;
			}
			return (num != 0) ? 1.0 : 0.36;
		}
		return 0.36;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
