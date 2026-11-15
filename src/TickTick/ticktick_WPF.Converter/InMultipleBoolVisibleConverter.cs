using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class InMultipleBoolVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Any() && parameter != null && parameter is string text)
		{
			string[] array = text.Split(',');
			if (values.Length == array.Length)
			{
				bool flag = true;
				for (int i = 0; i < array.Length; i++)
				{
					if (values[i] is bool flag2 && bool.Parse(array[i]) != flag2)
					{
						flag = false;
						break;
					}
				}
				return flag ? Visibility.Collapsed : Visibility.Visible;
			}
		}
		return Visibility.Visible;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
