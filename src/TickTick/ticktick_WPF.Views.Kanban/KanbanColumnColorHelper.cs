using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Views.Kanban;

public static class KanbanColumnColorHelper
{
	public const int DisplayOpacityPercent = 10;

	public const int DarkDisplayOpacityPercent = 8;

	public const string SpecialColumnColor = "#A5A5A5";

	public const string AssigneeColumnColor = "#A5A5A5";

	public const string TransparentColor = "transparent";

	private const string DatePrefix = "date:";

	private const string PriorityPrefix = "priority:";

	private const string AssigneePrefix = "assign:";

	private const string NoteColumn = "note";

	private const string HabitColumn = "habit";

	private const string CountdownColumn = "countdown";

	private const string CourseColumn = "course";

	private const string SubscriptionCalendarColumn = "calendar";

	private const string LegacySubscriptionCalendarColumn = "subsCalendar";

	private const string CreateTimeTodayColumn = "today";

	private const string CreateTimeYesterdayColumn = "yesterday";

	private const string CreateTimeLastWeekColumn = "p7ds";

	private const string CreateTimeLastMonthColumn = "p30ds";

	private const string CreateTimeEarlierColumn = "earlier";

	private const string CompletedColumn = "completed";

	private static readonly string[] IndexedCustomColors = new string[8] { "#6387F7", "#24ABF6", "#2FBFBC", "#46C176", "#FAAD14", "#ED8467", "#FC70BC", "#BE8FFF" };

	private static readonly string[] RecommendedCustomColors = new string[8]
	{
		IndexedCustomColors[5],
		IndexedCustomColors[4],
		IndexedCustomColors[3],
		IndexedCustomColors[2],
		IndexedCustomColors[1],
		IndexedCustomColors[0],
		IndexedCustomColors[7],
		IndexedCustomColors[6]
	};

	public static IEnumerable<string> GetDefaultCustomColors()
	{
		return IndexedCustomColors;
	}

	public static IEnumerable<string> GetRecommendedCustomColors()
	{
		return RecommendedCustomColors;
	}

	public static string GetInitialCustomColor(int ordinal)
	{
		int num = ordinal % IndexedCustomColors.Length;
		if (num < 0)
		{
			num += IndexedCustomColors.Length;
		}
		return IndexedCustomColors[num];
	}

	public static bool IsIndexedCustomColor(string color)
	{
		string normalized = NormalizeColor(color);
		return IndexedCustomColors.Any((string item) => item == normalized);
	}

	public static bool HasColor(string color)
	{
		if (!string.IsNullOrWhiteSpace(color))
		{
			return !string.Equals(color, "transparent", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static bool NeedsDefaultCustomColor(string color)
	{
		return string.IsNullOrWhiteSpace(color);
	}

	public static string SelectColorForInsertion(IList<string> columnColors, int insertionIndex)
	{
		if (columnColors == null)
		{
			throw new ArgumentNullException("columnColors");
		}
		insertionIndex = Math.Max(0, Math.Min(insertionIndex, columnColors.Count - 1));
		Dictionary<string, int> usageByColor = IndexedCustomColors.ToDictionary((string color) => color, (string color) => columnColors.Count((string value) => NormalizeColor(value) == color));
		foreach (int usage in (from count in usageByColor.Values.Distinct()
			orderby count
			select count).ToList())
		{
			List<string> list = IndexedCustomColors.Where((string color) => usageByColor[color] == usage).ToList();
			if (list.Count == 1)
			{
				return list[0];
			}
			List<string> list2 = list.Where((string color) => !IsAdjacentColor(columnColors, insertionIndex, color)).ToList();
			if (list2.Count > 0)
			{
				return SelectFurthestColor(list2, columnColors, insertionIndex);
			}
		}
		return SelectFurthestColor(IndexedCustomColors, columnColors, insertionIndex);
	}

	public static string GetFixedColor(string columnId, bool isWeekDateView)
	{
		if (string.IsNullOrEmpty(columnId))
		{
			return null;
		}
		switch (columnId)
		{
		case "note":
		case "habit":
		case "countdown":
		case "course":
		case "completed":
			return "#A5A5A5";
		default:
			if (columnId.StartsWith("priority:", StringComparison.Ordinal))
			{
				int.TryParse(columnId.Substring("priority:".Length), out var result);
				return result switch
				{
					5 => "#F6252E", 
					3 => "#FFA300", 
					1 => "#4772FA", 
					_ => "#8F8F8F", 
				};
			}
			if (columnId.StartsWith("assign:", StringComparison.Ordinal))
			{
				return "#A5A5A5";
			}
			if (columnId.StartsWith("date:", StringComparison.Ordinal))
			{
				return GetDateColor(columnId, isWeekDateView);
			}
			return GetCreateTimeColor(columnId);
		}
	}

	public static bool FollowsThemeColor(string columnId)
	{
		if (!(columnId == "calendar"))
		{
			return columnId == "subsCalendar";
		}
		return true;
	}

	private static string GetCreateTimeColor(string columnId)
	{
		return columnId switch
		{
			"today" => "#6387F7", 
			"yesterday" => "#2FBFBC", 
			"p7ds" => "#FAAD14", 
			"p30ds" => "#FF5A61", 
			"earlier" => "#C69DF0", 
			_ => null, 
		};
	}

	private static string GetDateColor(string columnId, bool isWeekDateView)
	{
		string s = columnId.Substring("date:".Length);
		if (isWeekDateView && int.TryParse(s, out var result))
		{
			switch (result)
			{
			case -1:
				return "#FF5C62";
			case 0:
				return "#6387F7";
			case 1:
				return "#2FBFBC";
			case 2:
				return "#52C41A";
			case 3:
				return "#FAAD14";
			case 4:
				return "#E03131";
			case 5:
				return "#B069EE";
			case 6:
				return "#8F8F8F";
			}
		}
		return columnId switch
		{
			"date:-1" => "#FF5C62", 
			"date:0" => "#6387F7", 
			"date:1" => "#FAAD14", 
			"date:week" => "#2FBFBC", 
			"date:later" => "#52C41A", 
			"date:no" => "#8F8F8F", 
			_ => null, 
		};
	}

	private static bool IsAdjacentColor(IList<string> columnColors, int insertionIndex, string color)
	{
		if (insertionIndex <= 0 || !(NormalizeColor(columnColors[insertionIndex - 1]) == color))
		{
			if (insertionIndex < columnColors.Count - 1)
			{
				return NormalizeColor(columnColors[insertionIndex + 1]) == color;
			}
			return false;
		}
		return true;
	}

	private static string SelectFurthestColor(IEnumerable<string> candidates, IList<string> columnColors, int insertionIndex)
	{
		int num = -1;
		string text = null;
		foreach (string candidate in candidates)
		{
			int num2 = int.MaxValue;
			for (int i = 0; i < columnColors.Count; i++)
			{
				if (NormalizeColor(columnColors[i]) == candidate)
				{
					num2 = Math.Min(num2, Math.Abs(i - insertionIndex));
				}
			}
			if (num2 > num)
			{
				num = num2;
				text = candidate;
			}
		}
		return text ?? IndexedCustomColors[0];
	}

	private static string NormalizeColor(string color)
	{
		if (!HasColor(color))
		{
			return null;
		}
		string text = color.Trim().ToUpperInvariant();
		if (text.Length == 6 && !text.StartsWith("#"))
		{
			text = "#" + text;
		}
		return text;
	}
}
