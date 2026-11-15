using System.Collections.Generic;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Filter;

namespace ticktick_WPF.ViewModels;

public class DateCardViewModel : FilterListDisplayModel<string>
{
	private static readonly Dictionary<string, string> DateMap = new Dictionary<string, string>
	{
		{
			"nodue",
			Utils.GetString("NoDate")
		},
		{
			"overdue",
			Utils.GetString("overdue")
		},
		{
			"recurring",
			Utils.GetString("RecurringDate")
		},
		{
			"today",
			Utils.GetString("Today")
		},
		{
			"tomorrow",
			Utils.GetString("Tomorrow")
		},
		{
			"thisweek",
			Utils.GetString("ThisWeek")
		},
		{
			"nextweek",
			Utils.GetString("NextWeek")
		},
		{
			"thismonth",
			Utils.GetString("ThisMonth")
		}
	};

	public List<string> Values
	{
		get
		{
			return ValueList;
		}
		set
		{
			ValueList = value;
			base.Content = ToDisplayList(ValueList);
			OnPropertyChanged("Values");
		}
	}

	public DateCardViewModel()
	{
		base.ConditionName = "dueDate";
	}

	public string ToDisplayList(List<string> dates)
	{
		return FormatDisplayText(dates, " " + Utils.GetString("or") + " ");
	}

	public static string ToNormalDisplayText(List<string> dates)
	{
		return FormatDisplayText(dates, " ,");
	}

	public static string FormatDisplayText(List<string> dates, string divider)
	{
		string text = string.Empty;
		if (dates != null && dates.Count > 0)
		{
			for (int i = 0; i < dates.Count; i++)
			{
				text = ((i >= dates.Count - 1) ? (text + GetDateText(dates[i])) : (text + GetDateText(dates[i]) + divider));
			}
		}
		return text;
	}

	private static string GetDateText(string key)
	{
		if (DateMap.ContainsKey(key))
		{
			return DateMap[key];
		}
		if (key.EndsWith("days"))
		{
			return string.Format(Utils.GetString("NextNDays"), key.Replace("days", string.Empty));
		}
		if (key.EndsWith("dayslater"))
		{
			return string.Format(Utils.GetString("NDaysLater"), key.Replace("dayslater", string.Empty));
		}
		if (key.EndsWith("daysfromtoday"))
		{
			if (key.StartsWith("-"))
			{
				return string.Format(Utils.GetString("NDaysAgo"), key.Replace("daysfromtoday", string.Empty).Replace("-", string.Empty), "th");
			}
			return string.Format(Utils.GetString("AfterNDays"), key.Replace("daysfromtoday", string.Empty), "th");
		}
		if (key.StartsWith("offset") && key.Length > 8)
		{
			string text = key.Substring(7);
			switch (text.Substring(0, text.Length - 1))
			{
			case "-1D":
				return Utils.GetString("PublicYesterday");
			case "1M":
				return Utils.GetString("NextMonth");
			case "-1M":
				return Utils.GetString("LastMonth");
			case "1W":
				return Utils.GetString("NextWeek");
			case "-1W":
				return Utils.GetString("LastWeek");
			}
		}
		if (key.StartsWith("span"))
		{
			(int?, int?) spanPairInRule = FilterUtils.GetSpanPairInRule(key);
			int? item = spanPairInRule.Item1;
			int? item2 = spanPairInRule.Item2;
			string obj = (item.HasValue ? FilterUtils.GetNthDayString(item.Value) : "-");
			string text2 = (item2.HasValue ? FilterUtils.GetNthDayString(item2.Value) : "-");
			return obj + " ~ " + text2;
		}
		return string.Empty;
	}

	public override ConditionEditDialog GenerateEditDialog()
	{
		return new DateEditDialog();
	}

	public override Rule<string> GetParseRule()
	{
		return new DueDateRule();
	}

	public override string ToCardText()
	{
		switch (ValueList.Count)
		{
		case 0:
			return Utils.GetString("date");
		case 1:
			if (base.LogicType == LogicType.Not)
			{
				return Utils.GetString("Exclude") + " " + GetDateText(ValueList[0]);
			}
			return GetDateText(ValueList[0]);
		default:
			if (base.LogicType == LogicType.Not)
			{
				return FilterListDisplayModel<string>.GetLogicString(base.LogicType) + " ( " + FormatDisplayText(ValueList, ", ") + " )";
			}
			return ToDisplayList(ValueList);
		}
	}
}
