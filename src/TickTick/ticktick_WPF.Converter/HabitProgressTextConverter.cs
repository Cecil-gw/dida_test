using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Models;

namespace ticktick_WPF.Converter;

public class HabitProgressTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is HabitModel habitModel)
		{
			if (values[1] is HabitCheckInModel habitCheckInModel)
			{
				return habitCheckInModel.Value + "/" + habitModel.Goal + " " + habitModel.Unit;
			}
			return 0 + "/" + habitModel.Goal + " " + habitModel.Unit;
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
