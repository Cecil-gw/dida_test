using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ticktick_WPF.Converter;

public class PositiveVisibilityConverter : MarkupExtension, IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is int num)
		{
			if (num <= 0)
			{
				return string.Empty;
			}
			return value.ToString();
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
}
