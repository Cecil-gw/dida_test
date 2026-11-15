using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CountdownRepeatDisplayTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length != 3)
		{
			return string.Empty;
		}
		string repeatFrom = values[0] as string;
		string text = values[1] as string;
		DateTime? dateTime = values[2] as DateTime?;
		if (text != null)
		{
			bool flag = text.Contains("FREQ=DAILY");
			bool flag2 = text.Contains("FREQ=WEEKLY");
			bool flag3 = text.Contains("FREQ=MONTHLY");
			bool flag4 = text.Contains("FREQ=YEARLY");
			bool flag5 = text.Contains("CUSTOM");
			bool flag6 = text.Contains("RRULE");
			if (flag5)
			{
				return Utils.GetString("EveryYear");
			}
			int result;
			int num = ((!int.TryParse(Regex.Match(text, "INTERVAL=(\\d+)").Groups[1]?.Value, out result)) ? 1 : result);
			bool flag7 = true;
			if (flag6 && RepeatUtils.CheckIfCountdownCustom(text, dateTime ?? DateTime.Today))
			{
				flag7 = false;
			}
			if (flag7)
			{
				if (num == 1)
				{
					if (flag)
					{
						return Utils.GetString("EveryDay");
					}
					if (flag2)
					{
						return Utils.GetString("EveryWeek");
					}
					if (flag3)
					{
						return Utils.GetString("EveryMonth");
					}
					if (flag4)
					{
						return Utils.GetString("EveryYear");
					}
				}
				else
				{
					if (flag)
					{
						return string.Format("{0} {1} {2}", Utils.GetString("Every"), num, Utils.GetString("PublicDays"));
					}
					if (flag2)
					{
						return string.Format("{0} {1} {2}", Utils.GetString("Every"), num, Utils.GetString("PublicWeeks"));
					}
					if (flag3)
					{
						return string.Format("{0} {1} {2}", Utils.GetString("Every"), num, Utils.GetString("PublicMonths"));
					}
					if (flag4)
					{
						return string.Format("{0} {1} {2}", Utils.GetString("Every"), num, Utils.GetString("PublicYears"));
					}
				}
			}
		}
		return DefaultRepeatDisplayTextConverter.GetRepeatText(text, repeatFrom, dateTime ?? DateTime.Today, withDescription: true, countdownMode: true).Replace("农历", "");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
