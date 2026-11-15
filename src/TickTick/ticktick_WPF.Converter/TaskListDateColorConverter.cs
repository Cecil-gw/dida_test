using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class TaskListDateColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 8)
		{
			DateTime? startDate = values[0] as DateTime?;
			DateTime? dueDate = values[1] as DateTime?;
			bool? isAllDay = values[2] as bool?;
			int valueOrDefault = (values[3] as int?).GetValueOrDefault();
			FrameworkElement context = values[4] as FrameworkElement;
			if (valueOrDefault != 0)
			{
				return ThemeUtil.GetColor("BaseColorOpacity20", context);
			}
			object obj = values[6];
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (flag ? 1u : 0u)) != 0)
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
					return ThemeUtil.GetColor("CountdownOutdateColor", context);
				}
			}
			obj = values[5];
			bool flag3 = default(bool);
			int num2;
			if (obj is bool)
			{
				flag3 = (bool)obj;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			if (((uint)num2 & (flag3 ? 1u : 0u)) != 0)
			{
				if (DateUtils.CountDownOutDate(startDate, dueDate, isAllDay == true))
				{
					return ThemeUtil.GetColor("OutDateColor", context);
				}
			}
			else if (DateUtils.IsOutDated(startDate, dueDate, isAllDay))
			{
				return ThemeUtil.GetColor("OutDateColor", context);
			}
			return ThemeUtil.GetColor("DateColorPrimary", context, ThemeUtil.GetColor("ThemeBlue"));
		}
		return ThemeUtil.GetColor("DateColorPrimary", ThemeUtil.GetColor("ThemeBlue"));
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
