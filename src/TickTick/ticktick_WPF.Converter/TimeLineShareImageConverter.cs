using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ticktick_WPF.Converter;

public class TimeLineShareImageConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2)
		{
			switch (values[0] as string)
			{
			case "read":
				return (DrawingImage)(Application.Current?.FindResource("ReadOnlyDrawingImage"));
			case "comment":
				return (DrawingImage)(Application.Current?.FindResource("CanCommentDrawingImage"));
			default:
				return (DrawingImage)(Application.Current?.FindResource("CanEditDrawingImage"));
			case null:
				break;
			}
		}
		return (DrawingImage)(Application.Current?.FindResource("CanEditDrawingImage"));
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
