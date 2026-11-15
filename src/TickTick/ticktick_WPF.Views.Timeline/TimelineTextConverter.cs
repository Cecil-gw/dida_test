using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2)
		{
			string text = values[0] as string;
			if (text != null)
			{
				if (values[1] is double num && !double.IsInfinity(num))
				{
					if (num < 24.0)
					{
						return string.Empty;
					}
					int num2 = (int)num / 5;
					if (text.Length > num2 && num2 > 0)
					{
						text = text.Substring(0, num2);
					}
				}
				return text;
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
