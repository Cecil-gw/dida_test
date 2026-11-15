using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class TaskListDateConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 12)
		{
			bool flag = default(bool);
			int num;
			object obj;
			if ((values[3] as int?).GetValueOrDefault() == 0)
			{
				obj = values[4];
				if (obj is bool)
				{
					flag = (bool)obj;
					num = 1;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 0;
			}
			bool flag2 = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
			bool? flag3 = ((values[5] is bool value) ? new bool?(value) : ((parameter is string text) ? new bool?(text == "True") : ((bool?)null)));
			DateTime? startDate = values[0] as DateTime?;
			DateTime? dueDate = values[1] as DateTime?;
			bool? isAllDay = values[2] as bool?;
			bool? flag4 = values[6] as bool?;
			obj = values[10];
			bool flag5 = default(bool);
			int num2;
			if (obj is bool)
			{
				flag5 = (bool)obj;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			if (((uint)num2 & (flag5 ? 1u : 0u)) != 0 && startDate.HasValue)
			{
				int num3 = 0;
				if (values[11] is CountDownBaseViewModel countDownBaseViewModel)
				{
					CountdownModel countdown = countDownBaseViewModel.Countdown;
					DateTime? target = startDate.Value;
					DisplayTimerMode? timerMode = DisplayTimerMode.CountDown;
					num3 = countdown.GetDaysRemaining(target, null, timerMode);
				}
				else
				{
					num3 = (int)(startDate.Value - DateTime.Today).TotalDays;
				}
				if (num3 == 0)
				{
					return Utils.GetString("Today");
				}
				if (num3 > 0)
				{
					if (num3 != 1)
					{
						return string.Format(Utils.GetString("days_until_in_list"), num3);
					}
					return Utils.GetString("day_until_in_list");
				}
				num3 = Math.Abs(num3);
				if (num3 != 1)
				{
					return string.Format(Utils.GetString("days_since_in_list"), num3);
				}
				return Utils.GetString("day_since_in_list");
			}
			obj = values[7];
			bool flag6 = default(bool);
			int num4;
			if (obj is bool)
			{
				flag6 = (bool)obj;
				num4 = 1;
			}
			else
			{
				num4 = 0;
			}
			if (((uint)num4 & (flag6 ? 1u : 0u)) != 0)
			{
				flag3 = false;
			}
			if (flag4 == true && DateUtils.IsOutDated(startDate, dueDate, isAllDay))
			{
				return string.Empty;
			}
			if (startDate.HasValue && !Utils.IsEmptyDate(startDate.Value))
			{
				bool valueOrDefault = values[9] as bool? == true;
				bool valueOrDefault2 = values[8] as bool? == true;
				if (!flag2)
				{
					return DateUtils.FormatListDateString(startDate.Value, dueDate, isAllDay, valueOrDefault, valueOrDefault2);
				}
				return DateUtils.FormatCountDownDateString(startDate.Value, dueDate, isAllDay == true, flag3 == true);
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
