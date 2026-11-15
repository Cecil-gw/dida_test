using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class WidgetTaskDateColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 8 && values[0] is ResourceDictionary dict)
		{
			DateTime? startDate = values[1] as DateTime?;
			DateTime? dueDate = values[2] as DateTime?;
			bool? isAllDay = values[3] as bool?;
			int? num = values[4] as int?;
			if (num.HasValue && num != 0)
			{
				return ThemeUtil.GetColor(dict, "BaseColorOpacity20");
			}
			object obj = values[6];
			bool flag = default(bool);
			int num2;
			if (obj is bool)
			{
				flag = (bool)obj;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			if (((uint)num2 & (flag ? 1u : 0u)) != 0)
			{
				bool flag2 = false;
				if (values[7] is CountDownBaseViewModel countDownBaseViewModel)
				{
					CountdownModel countdown = countDownBaseViewModel.Countdown;
					DateTime? target = startDate.Value;
					DisplayTimerMode? timerMode = DisplayTimerMode.CountDown;
					flag2 = countdown.GetDaysRemaining(target, null, timerMode) < 0;
				}
				else
				{
					flag2 = DateUtils.IsOutDated(startDate, dueDate, isAllDay);
				}
				if (flag2)
				{
					return ThemeUtil.GetColor("CountdownOutdateColor", dict);
				}
			}
			obj = values[5];
			bool flag3 = default(bool);
			int num3;
			if (obj is bool)
			{
				flag3 = (bool)obj;
				num3 = 1;
			}
			else
			{
				num3 = 0;
			}
			if (((uint)num3 & (flag3 ? 1u : 0u)) != 0)
			{
				if (DateUtils.CountDownOutDate(startDate, dueDate, isAllDay == true))
				{
					return ThemeUtil.GetColor("OutDateColor", dict);
				}
			}
			else if (DateUtils.IsOutDated(startDate, dueDate, isAllDay))
			{
				return ThemeUtil.GetColor("OutDateColor");
			}
			if (num.HasValue)
			{
				_ = num == 0;
				return ThemeUtil.GetColor(dict, "PrimaryColor");
			}
			return ThemeUtil.GetColor(dict, "PrimaryColor");
		}
		return ThemeUtil.GetColor("PrimaryColor");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
