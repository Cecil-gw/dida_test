using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

[ValueConversion(typeof(Enum), typeof(Visibility))]
public class EnumToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null || parameter == null)
		{
			return Visibility.Collapsed;
		}
		if (!(value is Enum obj))
		{
			return Visibility.Collapsed;
		}
		int num = System.Convert.ToInt32(obj);
		string a = obj.ToString();
		if (parameter is int num2)
		{
			return (num != num2) ? Visibility.Collapsed : Visibility.Visible;
		}
		if (parameter is string text)
		{
			if (text.Contains("|"))
			{
				string[] array = text.Split('|');
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = array[i].Trim();
					if (string.Equals(a, text2, StringComparison.OrdinalIgnoreCase))
					{
						return Visibility.Visible;
					}
					if (int.TryParse(text2, out var result) && num == result)
					{
						return Visibility.Visible;
					}
				}
				return Visibility.Collapsed;
			}
			string text3 = text.Trim();
			if (string.Equals(a, text3, StringComparison.OrdinalIgnoreCase))
			{
				return Visibility.Visible;
			}
			if (int.TryParse(text3, out var result2) && num == result2)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException("单向转换，不支持 ConvertBack");
	}
}
