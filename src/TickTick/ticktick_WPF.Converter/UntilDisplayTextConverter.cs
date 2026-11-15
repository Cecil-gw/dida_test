using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class UntilDisplayTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length != 1)
		{
			return string.Empty;
		}
		string text = values[0] as string;
		string untilText = RRuleUtils.GetUntilText(RecurrenceModel.GetRecurrenceModel(text), text, toLower: false);
		if (string.IsNullOrEmpty(untilText))
		{
			return string.Empty;
		}
		return untilText;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
