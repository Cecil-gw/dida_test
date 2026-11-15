using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CountdownTypeDayOptionConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is DayStartOption dayStartOption)
		{
			switch (dayStartOption)
			{
			case DayStartOption.Default:
				return Utils.GetString("DayCalculationStandard");
			case DayStartOption.AddOneDay:
				return Utils.GetString("DayCalculationStandard1");
			}
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
