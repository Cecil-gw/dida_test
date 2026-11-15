using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class CommentLineVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value.ToString() == Visibility.Visible.ToString())
		{
			return Visibility.Collapsed;
		}
		if (value != null)
		{
			_ = value.ToString() == Visibility.Collapsed.ToString();
			return Visibility.Visible;
		}
		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
