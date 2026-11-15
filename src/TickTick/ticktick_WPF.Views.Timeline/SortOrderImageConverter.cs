using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;

namespace ticktick_WPF.Views.Timeline;

public class SortOrderImageConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length >= 1 && values[0] is string value && Enum.TryParse<Constants.SortType>(value, out var result))
		{
			return SortOptionHelper.GetSortTypeImage(result, isTimeline: true);
		}
		return Utils.GetImageSource("TimelineDefaultSortOrderDrawingImage");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
