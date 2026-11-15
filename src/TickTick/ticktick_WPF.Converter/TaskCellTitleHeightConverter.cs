using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskCellTitleHeightConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is double num && num > 36.0)
		{
			double num2 = (num - 18.0 - 4.0) / 16.0;
			return Math.Max(18, 4 + (int)num2 * 16);
		}
		return 18;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
