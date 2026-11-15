using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class YearCalendarCellBackConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		bool isShowHeatMap = (bool)values[0];
		int count = (int)values[1];
		bool isToday = (bool)values[2];
		bool isCurrentMonth = (bool)values[3];
		_ = (string)values[4];
		SolidColorBrush yearViewDayCellColor = CalendarUtils.GetYearViewDayCellColor(isShowHeatMap, count, isToday, isCurrentMonth);
		if (yearViewDayCellColor != null)
		{
			return yearViewDayCellColor;
		}
		return Brushes.Transparent;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
