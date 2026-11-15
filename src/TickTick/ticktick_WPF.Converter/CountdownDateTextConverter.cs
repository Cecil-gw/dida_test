using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CountdownDateTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length < 1 || values[0] == DependencyProperty.UnsetValue)
		{
			return string.Empty;
		}
		try
		{
			DateTime date = (DateTime)values[0];
			bool ignoreYear = values.Length > 1 && values[1] != DependencyProperty.UnsetValue && (bool)values[1];
			if (values.Length > 2 && values[2] != DependencyProperty.UnsetValue && !(bool)values[2])
			{
				return FormatLunarDate(date, ignoreYear);
			}
			return FormatGregorianDate(date, ignoreYear);
		}
		catch (Exception ex)
		{
			UtilLog.Error("CountdownDateTextConverter DateText Error: " + ex.Message);
			return "Error Format";
		}
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public static string FormatGregorianDate(DateTime date, bool ignoreYear)
	{
		if (ignoreYear)
		{
			return DateUtils.FormatMonthDay(date, DateFormatLen.Long);
		}
		return DateUtils.FormatYearMonthDay(date, DateFormatLen.Middle);
	}

	public static string FormatLunarDate(DateTime date, bool ignoreYear)
	{
		LunarUtils.ChineseCalendar chineseCalendar = new LunarUtils.ChineseCalendar(date);
		int chineseYear = chineseCalendar.ChineseYear;
		int chineseMonth = chineseCalendar.ChineseMonth;
		int chineseDay = chineseCalendar.ChineseDay;
		bool isChineseLeapMonth = chineseCalendar.IsChineseLeapMonth;
		string text = (ignoreYear ? "" : (DateUtils.GetChineseYearName(chineseYear) + $"({chineseYear})" + " "));
		string text2 = (isChineseLeapMonth ? "闰" : "") + DateUtils.GetChineseMonthName(chineseMonth);
		string chineseDayName = DateUtils.GetChineseDayName(chineseDay);
		return text + text2 + chineseDayName;
	}
}
