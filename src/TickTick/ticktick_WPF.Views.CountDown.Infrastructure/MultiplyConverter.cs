using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.CountDown.Infrastructure;

public class MultiplyConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double num && parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return num * result;
		}
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
