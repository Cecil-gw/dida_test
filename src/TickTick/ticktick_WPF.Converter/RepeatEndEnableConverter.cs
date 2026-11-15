using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class RepeatEndEnableConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length != 2)
		{
			return true;
		}
		if (values[0] is string repeatFrom && values[1] is string repeatFlag)
		{
			return RepeatUtils.GetRepeatType(repeatFrom, repeatFlag) != RepeatFromType.Custom;
		}
		return true;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
