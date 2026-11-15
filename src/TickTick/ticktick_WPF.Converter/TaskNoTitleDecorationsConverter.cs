using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Views.MarkDown.Colorizer;

namespace ticktick_WPF.Converter;

public class TaskNoTitleDecorationsConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is int num && values[1] is int num2 && num2 == 1 && num != 0)
		{
			return DeleteLineColorizer.HintStrikethrough;
		}
		return null;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
