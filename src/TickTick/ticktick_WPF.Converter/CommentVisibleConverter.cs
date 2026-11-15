using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Converter;

public class CommentVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && parameter != null)
		{
			Constants.DetailMode detailMode = (Constants.DetailMode)Enum.Parse(typeof(Constants.DetailMode), value.ToString());
			int num = int.Parse(parameter.ToString());
			return ((detailMode != Constants.DetailMode.Page || num != 0) && (detailMode != Constants.DetailMode.Popup || num != 1)) ? Visibility.Collapsed : Visibility.Visible;
		}
		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
