using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CustomRepeatDisplayTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length != 3)
		{
			return string.Empty;
		}
		string text = values[0] as string;
		string text2 = values[1] as string;
		DateTime? startDate = values[2] as DateTime?;
		if (text != "2")
		{
			return RRuleUtils.RRule2String(text, text2, startDate);
		}
		if (text2 != null && text2.ToLower().Contains("lunar") && startDate.HasValue)
		{
			return "农历 每年" + DateUtils.GetLunarMonthDay(startDate.Value);
		}
		return Utils.GetString("Endlessly");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
