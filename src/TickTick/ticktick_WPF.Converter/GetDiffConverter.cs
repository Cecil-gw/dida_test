using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class GetDiffConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is double num && values[1] is double num2)
		{
			double num3 = Math.Abs(num - num2);
			if (parameter is string s && int.TryParse(s, out var result) && num3 > (double)result)
			{
				num3 -= (double)result;
			}
			return num3;
		}
		return double.PositiveInfinity;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
