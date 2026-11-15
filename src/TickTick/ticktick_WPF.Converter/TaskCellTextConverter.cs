using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskCellTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3 && values[0] is double num && values[1] is double num2)
		{
			string text = values[2] as string;
			if (text != null)
			{
				int num3 = (int)(num / 15.4);
				int num4 = Math.Max(4, num3 * (int)num2 / 5);
				if (text.Length > num4)
				{
					text = text.Substring(0, num4);
				}
				return text;
			}
		}
		return "";
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
