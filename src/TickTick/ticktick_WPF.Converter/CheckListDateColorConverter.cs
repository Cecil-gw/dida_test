using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CheckListDateColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Any((object e) => e == DependencyProperty.UnsetValue))
		{
			return ThemeUtil.GetColor("PrimaryColor");
		}
		int valueOrDefault = (values[0] as int?).GetValueOrDefault();
		DateTime? dateTime = values[1] as DateTime?;
		if (valueOrDefault != 0)
		{
			return ThemeUtil.GetColor("BaseColorOpacity20");
		}
		if (dateTime.HasValue && dateTime.Value < DateTime.Today.Date)
		{
			return ThemeUtil.GetColor("OutDateColor");
		}
		return ThemeUtil.GetColor("PrimaryColor");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
