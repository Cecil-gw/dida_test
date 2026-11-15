using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Search;

public class SearchDateTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3 && values[0] is DateFilter dateFilter)
		{
			return GetDateFilterText(dateFilter, (DateTime?)values[1], (DateTime?)values[2]);
		}
		return Utils.GetString("AllDate");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}

	public static string FormatDateSpanDesc(DateTime startDate, DateTime endDate)
	{
		bool withYear = ShouldShowYear(startDate) || ShouldShowYear(endDate) || startDate.Year != endDate.Year;
		return " (" + FormatDateForSpan(startDate.Date, withYear) + " - " + FormatDateForSpan(endDate.Date, withYear) + ")";
	}

	public static string GetDateFilterText(DateFilter dateFilter, DateTime? startDate = null, DateTime? endDate = null)
	{
		if (dateFilter == DateFilter.Custom)
		{
			if (!startDate.HasValue || !endDate.HasValue)
			{
				return Utils.GetString("AllDate");
			}
			if (startDate.Value.Date != endDate.Value.Date)
			{
				bool withYear = ShouldShowYear(startDate.Value) || ShouldShowYear(endDate.Value) || startDate.Value.Year != endDate.Value.Year;
				return $"{FormatDateForSpan(startDate.Value, withYear)} - {FormatDateForSpan(endDate.Value, withYear)}";
			}
			return FormatDateForSpan(startDate.Value, ShouldShowYear(startDate.Value));
		}
		string key = "AllDate";
		switch (dateFilter)
		{
		case DateFilter.Today:
			key = "Today";
			break;
		case DateFilter.Tomorrow:
			key = "Tomorrow";
			break;
		case DateFilter.Yesterday:
			key = "PublicYesterday";
			break;
		case DateFilter.ThisWeek:
			key = "ThisWeek";
			break;
		case DateFilter.NextWeek:
			key = "NextWeek";
			break;
		case DateFilter.LastWeek:
			key = "LastWeek";
			break;
		case DateFilter.ThisMonth:
			key = "ThisMonth";
			break;
		case DateFilter.LastMonth:
			key = "LastMonth";
			break;
		}
		string text = Utils.GetString(key);
		if (dateFilter == DateFilter.ThisWeek || dateFilter == DateFilter.NextWeek || dateFilter == DateFilter.LastWeek)
		{
			(DateTime, DateTime) tuple = ((startDate.HasValue && endDate.HasValue) ? (startDate.Value, endDate.Value) : ConvertDateFilter2Span(dateFilter));
			text += FormatDateSpanDesc(tuple.Item1, tuple.Item2);
		}
		return text;
	}

	private static string FormatDateForSpan(DateTime date, bool withYear)
	{
		if (!withYear)
		{
			return DateUtils.FormatMonthDay(date, DateFormatLen.Middle);
		}
		return date.ToString("yyyy/M/d", CultureInfo.InvariantCulture);
	}

	private static bool ShouldShowYear(DateTime date)
	{
		return date.Year != DateTime.Today.Year;
	}

	public static (DateTime, DateTime) ConvertDateFilter2Span(DateFilter filter)
	{
		(DateTime, DateTime) tuple = (DateTime.Today, DateTime.Today);
		switch (filter)
		{
		case DateFilter.Today:
			tuple.Item1 = DateTime.Today;
			tuple.Item2 = DateTime.Today;
			break;
		case DateFilter.Yesterday:
			tuple.Item1 = DateTime.Today.AddDays(-1.0);
			(tuple.Item2, _) = tuple;
			break;
		case DateFilter.Tomorrow:
			tuple.Item1 = DateTime.Today.AddDays(1.0);
			tuple.Item2 = DateTime.Today.AddDays(1.0);
			break;
		case DateFilter.LastWeek:
			tuple.Item1 = Utils.GetWeekStart(DateTime.Today).AddDays(-7.0);
			tuple.Item2 = tuple.Item1.AddDays(6.0);
			break;
		case DateFilter.NextWeek:
			tuple.Item1 = Utils.GetWeekStart(DateTime.Today).AddDays(7.0);
			tuple.Item2 = tuple.Item1.AddDays(6.0);
			break;
		case DateFilter.LastMonth:
			tuple.Item1 = DateTime.Today.AddDays(1 - DateTime.Today.Day).AddMonths(-1);
			tuple.Item2 = tuple.Item1.AddMonths(1).AddDays(-1.0);
			break;
		case DateFilter.ThisMonth:
			tuple.Item1 = DateTime.Today.AddDays(1 - DateTime.Today.Day);
			tuple.Item2 = tuple.Item1.AddMonths(1).AddDays(-1.0);
			break;
		default:
			tuple.Item1 = Utils.GetWeekStart(DateTime.Today);
			tuple.Item2 = tuple.Item1.AddDays(6.0);
			break;
		}
		return tuple;
	}
}
