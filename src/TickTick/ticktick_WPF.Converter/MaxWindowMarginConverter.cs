using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;

namespace ticktick_WPF.Converter;

public class MaxWindowMarginConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (Screen.PrimaryScreen.Bounds.Width == Screen.PrimaryScreen.WorkingArea.Width && Screen.PrimaryScreen.WorkingArea.Y == 0)
		{
			return new Thickness(0.0, 0.0, 0.0, SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height);
		}
		if (Screen.PrimaryScreen.Bounds.Height == Screen.PrimaryScreen.WorkingArea.Height && Screen.PrimaryScreen.WorkingArea.X == 0)
		{
			return new Thickness(0.0, 0.0, SystemParameters.PrimaryScreenWidth - SystemParameters.WorkArea.Width, 0.0);
		}
		if (Screen.PrimaryScreen.Bounds.Width == Screen.PrimaryScreen.WorkingArea.Width && Screen.PrimaryScreen.WorkingArea.Y > 0)
		{
			return new Thickness(0.0, SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height, 0.0, 0.0);
		}
		if (Screen.PrimaryScreen.Bounds.Height == Screen.PrimaryScreen.WorkingArea.Height && Screen.PrimaryScreen.WorkingArea.X > 0)
		{
			return new Thickness(SystemParameters.PrimaryScreenWidth - SystemParameters.WorkArea.Width, 0.0, 0.0, 0.0);
		}
		return new Thickness(0.0, 0.0, 0.0, 45.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
