using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CheckListIconVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Any((object e) => e == DependencyProperty.UnsetValue))
		{
			return Visibility.Collapsed;
		}
		if (values.Length == 4)
		{
			if ((int)values[3] != 0)
			{
				return Visibility.Collapsed;
			}
			DateTime? date = (DateTime?)values[0];
			bool? flag = (bool?)values[1];
			DateTime? date2 = (DateTime?)values[2];
			if (date.HasValue && !Utils.IsEmptyDate(date) && date.Value >= DateTime.Now && flag.HasValue && !flag.Value)
			{
				return Visibility.Visible;
			}
			if (date2.HasValue && !Utils.IsEmptyDate(date2) && date2.Value > DateTime.Now)
			{
				return Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
