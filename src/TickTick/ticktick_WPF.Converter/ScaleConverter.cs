using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ScaleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double num && parameter != null)
		{
			string text = parameter.ToString();
			if (TryParseThickness(text, out var thickness))
			{
				return new Thickness(thickness.Left * num, thickness.Top * num, thickness.Right * num, thickness.Bottom * num);
			}
			if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return result * num;
			}
		}
		return DependencyProperty.UnsetValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	private bool TryParseThickness(string input, out Thickness thickness)
	{
		thickness = default(Thickness);
		if (string.IsNullOrEmpty(input))
		{
			return false;
		}
		string[] array = input.Split(',');
		if (array.Length == 4 && double.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && double.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && double.TryParse(array[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3) && double.TryParse(array[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var result4))
		{
			thickness = new Thickness(result, result2, result3, result4);
			return true;
		}
		return false;
	}
}
