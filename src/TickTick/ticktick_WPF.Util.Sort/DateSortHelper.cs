using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Sort;

public static class DateSortHelper
{
	public static int CompareTaskByDate(TaskBaseViewModel left, TaskBaseViewModel right, bool outDateFirst)
	{
		if (left.StartDate.HasValue && right.StartDate.HasValue)
		{
			if (((left.DueDate.HasValue && left.DueDate.Value < DateTime.Today) || (!left.DueDate.HasValue && left.StartDate.Value < DateTime.Today)) && (right.DueDate?.Date >= DateTime.Today || right.StartDate?.Date >= DateTime.Today))
			{
				if (!outDateFirst)
				{
					return 1;
				}
				return -1;
			}
			if (((right.DueDate.HasValue && right.DueDate.Value < DateTime.Today) || (!right.DueDate.HasValue && right.StartDate.Value < DateTime.Today)) && (left.DueDate?.Date >= DateTime.Today || left.StartDate?.Date >= DateTime.Today))
			{
				if (!outDateFirst)
				{
					return -1;
				}
				return 1;
			}
			if (((right.DueDate.HasValue && right.DueDate.Value < DateTime.Today) || (!right.DueDate.HasValue && right.StartDate.Value < DateTime.Today)) && ((left.DueDate.HasValue && left.DueDate.Value < DateTime.Today) || (!left.DueDate.HasValue && left.StartDate.Value < DateTime.Today)))
			{
				return CompareOutDate(left, right);
			}
			if ((right.DueDate?.Date >= DateTime.Today || right.StartDate?.Date >= DateTime.Today) && (left.DueDate?.Date >= DateTime.Today || left.StartDate?.Date >= DateTime.Today))
			{
				return CompareDate(left, right);
			}
		}
		return 0;
	}

	public static int CompareDate(TaskBaseViewModel left, TaskBaseViewModel right, bool ignoreDuration = false)
	{
		if (left.StartDate.HasValue && right.StartDate.HasValue)
		{
			DateTime date = left.StartDate.Value.Date;
			DateTime date2 = right.StartDate.Value.Date;
			if (date != date2)
			{
				return date.CompareTo(date2);
			}
			bool flag = CheckAllDay(left);
			bool flag2 = CheckAllDay(right);
			if (flag != flag2)
			{
				if (!flag)
				{
					return -1;
				}
				return 1;
			}
			if (ignoreDuration)
			{
				return left.StartDate.Value.CompareTo(right.StartDate.Value);
			}
			DateTime dateTime = left.StartDate.Value.Date;
			DateTime dateTime2 = right.StartDate.Value.Date;
			if (left.DueDate.HasValue)
			{
				dateTime = left.DueDate.Value.Date;
				if (flag)
				{
					dateTime = dateTime.AddDays(-1.0);
				}
			}
			if (right.DueDate.HasValue)
			{
				dateTime2 = right.DueDate.Value.Date;
				if (flag2)
				{
					dateTime2 = dateTime2.AddDays(-1.0);
				}
			}
			if (dateTime != dateTime2)
			{
				return dateTime.CompareTo(dateTime2);
			}
			int num = CompareTimeOrDuration(left, right);
			if (num != 0)
			{
				return num;
			}
		}
		return 0;
	}

	private static int CompareOutDate(TaskBaseViewModel left, TaskBaseViewModel right)
	{
		if (left.StartDate.HasValue && right.StartDate.HasValue)
		{
			DateTime dateTime = left.StartDate.Value.Date;
			DateTime dateTime2 = right.StartDate.Value.Date;
			bool flag = CheckAllDay(left);
			bool flag2 = CheckAllDay(right);
			if (left.DueDate.HasValue)
			{
				dateTime = left.DueDate.Value;
				if (flag)
				{
					dateTime = dateTime.Date.AddDays(-1.0);
				}
			}
			if (right.DueDate.HasValue)
			{
				dateTime2 = right.DueDate.Value;
				if (flag2)
				{
					dateTime2 = dateTime2.Date.AddDays(-1.0);
				}
			}
			if (dateTime != dateTime2)
			{
				return dateTime.CompareTo(dateTime2);
			}
			DateTime date = left.StartDate.Value.Date;
			DateTime date2 = right.StartDate.Value.Date;
			if (date != date2)
			{
				return date.CompareTo(date2);
			}
			if (flag != flag2)
			{
				if (!flag)
				{
					return -1;
				}
				return 1;
			}
			return CompareTimeOrDuration(left, right);
		}
		return 0;
	}

	private static int CompareTimeOrDuration(TaskBaseViewModel left, TaskBaseViewModel right)
	{
		if (left.StartDate.HasValue && right.StartDate.HasValue)
		{
			DateTime dateTime = left.StartDate.Value;
			DateTime dateTime2 = right.StartDate.Value;
			if (left.IsAllDay.HasValue && left.IsAllDay.Value)
			{
				dateTime = left.StartDate.Value.Date;
			}
			if (right.IsAllDay.HasValue && right.IsAllDay.Value)
			{
				dateTime2 = right.StartDate.Value.Date;
			}
			if (dateTime != dateTime2)
			{
				return dateTime.CompareTo(dateTime2);
			}
			int num = 0;
			int num2 = 0;
			if (left.DueDate.HasValue)
			{
				num = (int)(left.DueDate.Value - left.StartDate.Value).TotalMinutes;
			}
			if (right.DueDate.HasValue)
			{
				num2 = (int)(right.DueDate.Value - right.StartDate.Value).TotalMinutes;
			}
			if (num != num2)
			{
				return num.CompareTo(num2);
			}
		}
		return 0;
	}

	private static bool CheckAllDay(TaskBaseViewModel model)
	{
		if (model.IsAllDay.HasValue)
		{
			return model.IsAllDay.Value;
		}
		return false;
	}
}
