using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class DefaultRepeatDisplayTextConverter : IMultiValueConverter
{
	public static string GetRepeatText(string repeatFlag, string repeatFrom, DateTime? startDate, bool withDescription = true, bool countdownMode = false)
	{
		if (RepeatUtils.GetRepeatType(repeatFrom, repeatFlag) == RepeatFromType.Custom)
		{
			return Utils.GetString("RepeatByCustom");
		}
		if (string.IsNullOrEmpty(repeatFlag) || repeatFlag == "RRULE:FREQ=NONE")
		{
			if (!countdownMode)
			{
				return string.Empty;
			}
			return Utils.GetString("none");
		}
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(repeatFlag);
		if (repeatFrom == "2" && (recurrenceModel.IsEquals(RepeatUtils.WorkingDaysRrule) || repeatFlag == "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=WEEKEND"))
		{
			return Utils.GetString("EveryWeekday") + (withDescription ? (" (" + Utils.GetString("MonToFri") + ")") : "");
		}
		if (repeatFrom == "2" && recurrenceModel.IsEquals(RepeatUtils.WeekendRrule))
		{
			return Utils.GetString("every_weekend");
		}
		bool num = repeatFlag.Contains("FREQ=DAILY");
		bool flag = !repeatFlag.Contains("INTERVAL") || repeatFlag.Contains("INTERVAL=1");
		if (num && flag)
		{
			if (repeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
			{
				return Utils.GetString("official_holidays");
			}
			if (repeatFlag.Contains("WEEKEND"))
			{
				if (repeatFlag.Contains("TT_SKIP=HOLIDAY,WEEKEND"))
				{
					return Utils.GetString("OfficialWorkingDays");
				}
				return Utils.GetString("EveryWeekday") + " (" + Utils.GetString("MonToFri") + ")";
			}
		}
		return RRuleUtils.RRule2String(repeatFrom, repeatFlag, startDate, showUntil: false, withDescription);
	}

	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length != 3)
		{
			return string.Empty;
		}
		string repeatFrom = values[0] as string;
		string repeatFlag = values[1] as string;
		DateTime? startDate = values[2] as DateTime?;
		return GetRepeatText(repeatFlag, repeatFrom, startDate);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
