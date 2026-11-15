using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Models;

namespace ticktick_WPF.Converter;

public class ShowHabitIndicatorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is HabitModel habitModel && values[1] is HabitCheckInModel habitCheckInModel && habitModel.Type.ToLower() != "boolean" && habitCheckInModel.Value > 0.0)
		{
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
