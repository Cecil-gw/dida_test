using System.Collections.Generic;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Timeline;

public static class TimelineConstants
{
	public const string RangeDay = "day";

	public const string RangeWeek = "week";

	public const string RangeMonth = "month";

	public const string RangeYear = "year";

	public const double OneLineHeight = 40.0;

	public const double HeadHeight = 28.0;

	public const double ShowWeekHeadHeight = 42.0;

	public const double GroupWidthMin = 100.0;

	public const double GroupWidthDefault = 140.0;

	public const double ArrangePanelWidth = 215.0;

	public const double CellMinWidth = 24.0;

	public const double ResizeThumbWidth = 8.0;

	public const double CellMargin = 2.0;

	public const double CellInlineMarginRightMin = 104.0;

	public const double CellOutlineTextMaxWidth = 120.0;

	public const double CellOutlineTextMinWidth = 30.0;

	public const string TimelineArrangeSectionStatusPrefix = "Arrange_";

	private static readonly Dictionary<int, double> OneDayWidthMap = new Dictionary<int, double>
	{
		{ 1, 216.0 },
		{ 2, 180.0 },
		{ 3, 144.0 },
		{ 4, 108.0 },
		{ 5, 72.0 },
		{ 6, 56.0 },
		{ 7, 44.0 },
		{ 8, 32.0 },
		{ 9, 20.0 }
	};

	private static readonly Dictionary<string, bool> NeedsSplitLineMap = new Dictionary<string, bool>
	{
		{ "day", false },
		{ "week", false },
		{ "month", true },
		{ "year", true }
	};

	private static readonly Dictionary<string, int> MinChangeDaysMap = new Dictionary<string, int>
	{
		{ "day", 1 },
		{ "week", 1 },
		{ "month", 1 },
		{ "year", 1 }
	};

	private static readonly Dictionary<string, int> NewTaskDefaultDaysMap = new Dictionary<string, int>
	{
		{ "day", 1 },
		{ "week", 1 },
		{ "month", 5 },
		{ "year", 14 }
	};

	private static readonly Dictionary<string, string> RangeI18nKey = new Dictionary<string, string>
	{
		{ "day", "TimelineDay" },
		{ "week", "TimelineWeek" },
		{ "month", "TimelineMonth" },
		{ "year", "TimelineYear" }
	};

	public static string RangeDefault => "day";

	public static string SortDefault => Constants.SortType.sortOrder.ToString();

	public static double GetOneDayWidth(int index)
	{
		if (!OneDayWidthMap.TryGetValue(index, out var value))
		{
			return OneDayWidthMap[GetRangeDefaultWidthIndex(RangeDefault)];
		}
		return value;
	}

	public static bool GetNeedsSplitLine(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return NeedsSplitLineMap[RangeDefault];
		}
		if (!NeedsSplitLineMap.TryGetValue(key, out var value))
		{
			return NeedsSplitLineMap[RangeDefault];
		}
		return value;
	}

	public static int GetMinChangeDays(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return MinChangeDaysMap[RangeDefault];
		}
		if (!MinChangeDaysMap.TryGetValue(key, out var value))
		{
			return MinChangeDaysMap[RangeDefault];
		}
		return value;
	}

	public static int GetNewTaskDefaultDays(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return NewTaskDefaultDaysMap[RangeDefault];
		}
		if (!NewTaskDefaultDaysMap.TryGetValue(key, out var value))
		{
			return NewTaskDefaultDaysMap[RangeDefault];
		}
		return value;
	}

	public static string GetRangeI18nKey(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return RangeI18nKey[RangeDefault];
		}
		if (!RangeI18nKey.TryGetValue(key, out var value))
		{
			return RangeI18nKey[RangeDefault];
		}
		return value;
	}

	public static string GetDayWidthRange(int index)
	{
		if (index < 8)
		{
			if (index < 6)
			{
				return "day";
			}
			return "week";
		}
		return "month";
	}

	public static int GetRangeDefaultWidthIndex(string range)
	{
		return range switch
		{
			"day" => 4, 
			"week" => 7, 
			"month" => 9, 
			_ => 4, 
		};
	}
}
