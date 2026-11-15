using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ticktick_WPF.Views.CountDown.Infrastructure;

public class IconNameToElementConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string text = value as string;
		int result = 32;
		if (parameter != null && parameter is string s)
		{
			int.TryParse(s, out result);
		}
		if (string.IsNullOrEmpty(text))
		{
			text = "Default";
		}
		if (text.StartsWith("Text:"))
		{
			string[] array = text.Split(':');
			if (array.Length >= 3)
			{
				string text2 = array[1];
				string value2 = array[2];
				try
				{
					Brush background = (Brush)new BrushConverter().ConvertFrom(value2);
					return IconService.CreateTextIcon(text2, background);
				}
				catch
				{
					return IconService.CreateTextIcon(text2, new SolidColorBrush(Colors.LightBlue));
				}
			}
			return IconService.GetIcon("Default", result);
		}
		return IconService.GetIcon(text, result);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
