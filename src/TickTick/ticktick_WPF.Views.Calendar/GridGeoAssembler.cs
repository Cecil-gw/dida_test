using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;

namespace ticktick_WPF.Views.Calendar;

public static class GridGeoAssembler
{
	private const int MaxDepth = 256;

	private static bool[,] _matrix;

	public static IEnumerable<CalendarDisplayViewModel> AssemblyMultiDayEvents(List<CalendarDisplayModel> tasks, DateTime startDate, bool showWeekend)
	{
		List<CalendarDisplayViewModel> list = new List<CalendarDisplayViewModel>();
		Dictionary<int, Dictionary<int, bool>> matrixDict = new Dictionary<int, Dictionary<int, bool>>();
		if (tasks != null && tasks.Any())
		{
			List<CalendarDisplayModel> list2 = new List<CalendarDisplayModel>();
			List<CalendarDisplayModel> list3 = new List<CalendarDisplayModel>();
			foreach (CalendarDisplayModel task in tasks)
			{
				if (IsValidSpanModel(task, weekMode: false))
				{
					list2.Add(task);
				}
				else
				{
					list3.Add(task);
				}
			}
			AssemblyMultiDaySpanEvents(list2, list, matrixDict, startDate, showWeekend);
			AssemblyMultiDayPointEvents(list3, list, matrixDict, startDate, showWeekend);
		}
		return list;
	}

	private static void AssemblyMultiDaySpanEvents(List<CalendarDisplayModel> tasks, List<CalendarDisplayViewModel> models, Dictionary<int, Dictionary<int, bool>> matrixDict, DateTime startDate, bool showWeekend)
	{
		if (tasks == null || !tasks.Any())
		{
			return;
		}
		tasks.Sort(CompareSpanTasks);
		foreach (CalendarDisplayModel task in tasks)
		{
			int num = GetSpanLength(startDate, task, checkDate: false);
			if (num < 1 || !task.DisplayStartDate.HasValue || !task.DisplayDueDate.HasValue)
			{
				continue;
			}
			int num2 = (int)(task.DisplayStartDate.Value.Date - startDate.Date).TotalDays;
			if (!showWeekend)
			{
				DateTime dateTime = task.DisplayDueDate ?? task.DisplayStartDate.Value;
				if (dateTime.Date > task.DisplayStartDate.Value.Date && (dateTime - dateTime.Date).TotalHours <= 5.0)
				{
					dateTime = dateTime.Date.AddDays(-1.0);
				}
				int weekendsCountInSpan = DateUtils.GetWeekendsCountInSpan(task.DisplayStartDate.Value, dateTime, checkEnd: true);
				num -= weekendsCountInSpan;
				if (num <= 0)
				{
					continue;
				}
				int weekendsCountInSpan2 = DateUtils.GetWeekendsCountInSpan(task.DisplayStartDate.Value, startDate, checkEnd: false);
				num2 += weekendsCountInSpan2;
			}
			for (int i = 0; i < 10000; i++)
			{
				if (CheckRowEnoughSpace(i, num2, num))
				{
					SetOccupied(i, num2, num);
					CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(task);
					calendarDisplayViewModel.Row = i;
					calendarDisplayViewModel.Column = num2;
					calendarDisplayViewModel.ColumnSpan = num;
					models.Add(calendarDisplayViewModel);
					break;
				}
			}
		}
		bool CheckRowEnoughSpace(int row, int offset, int length)
		{
			if (!matrixDict.ContainsKey(row))
			{
				return true;
			}
			Dictionary<int, bool> dictionary = matrixDict[row];
			for (int j = 0; j < length; j++)
			{
				if (row >= 256)
				{
					return false;
				}
				if (dictionary.ContainsKey(offset + j))
				{
					return false;
				}
			}
			return true;
		}
		void SetOccupied(int row, int offset, int length)
		{
			if (!matrixDict.ContainsKey(row))
			{
				matrixDict[row] = new Dictionary<int, bool>();
			}
			Dictionary<int, bool> dictionary = matrixDict[row];
			for (int j = 0; j < length; j++)
			{
				dictionary[j + offset] = true;
			}
		}
	}

	private static void AssemblyMultiDayPointEvents(List<CalendarDisplayModel> points, List<CalendarDisplayViewModel> models, Dictionary<int, Dictionary<int, bool>> matrixDict, DateTime startDate, bool showWeekend)
	{
		if (points == null || !points.Any())
		{
			return;
		}
		points.Sort(CompareTasks);
		foreach (CalendarDisplayModel point in points)
		{
			if (!point.DisplayStartDate.HasValue && !point.CompletedTime.HasValue)
			{
				continue;
			}
			DateTime dateTime = point.DisplayStartDate ?? point.CompletedTime.Value;
			if (!showWeekend && DateUtils.IsWeekEnds(dateTime))
			{
				continue;
			}
			int num = 0;
			if (point.DisplayStartDate.HasValue)
			{
				num = (int)(point.DisplayStartDate.Value.Date - startDate.Date).TotalDays;
			}
			else if (point.CompletedTime.HasValue)
			{
				num = (int)(point.CompletedTime.Value.Date - startDate.Date).TotalDays;
			}
			if (!showWeekend)
			{
				int weekendsCountInSpan = DateUtils.GetWeekendsCountInSpan(dateTime, startDate, checkEnd: false);
				num += weekendsCountInSpan;
			}
			for (int i = 0; i < 10000; i++)
			{
				bool flag = false;
				if (!matrixDict.ContainsKey(i))
				{
					matrixDict[i] = new Dictionary<int, bool> { { num, true } };
					flag = true;
				}
				else
				{
					Dictionary<int, bool> dictionary = matrixDict[i];
					if (!dictionary.ContainsKey(num))
					{
						dictionary[num] = true;
						flag = true;
					}
				}
				if (flag)
				{
					CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(point);
					calendarDisplayViewModel.Row = i;
					calendarDisplayViewModel.Column = num;
					calendarDisplayViewModel.ColumnSpan = 1;
					models.Add(calendarDisplayViewModel);
					break;
				}
			}
		}
	}

	public static IEnumerable<CalendarDisplayViewModel> AssemblyEvents(List<CalendarDisplayModel> tasks, DateTime startDate)
	{
		List<CalendarDisplayViewModel> list = new List<CalendarDisplayViewModel>();
		_matrix = new bool[7, 256];
		if (tasks != null && tasks.Any())
		{
			List<CalendarDisplayModel> list2 = new List<CalendarDisplayModel>();
			List<CalendarDisplayModel> list3 = new List<CalendarDisplayModel>();
			foreach (CalendarDisplayModel task in tasks)
			{
				if (IsValidSpanModel(task, weekMode: false))
				{
					list2.Add(task);
				}
				else
				{
					list3.Add(task);
				}
			}
			AssemblySpanEvents(list2, list, startDate);
			AssemblyPointEvents(list3, list, startDate);
		}
		return list;
	}

	private static void AssemblyPointEvents(List<CalendarDisplayModel> points, List<CalendarDisplayViewModel> models, DateTime startDate)
	{
		if (points == null || !points.Any())
		{
			return;
		}
		points.Sort(CompareTasks);
		foreach (CalendarDisplayModel point in points)
		{
			if ((!point.DisplayStartDate.HasValue && !point.CompletedTime.HasValue) || IsValidSpanModel(point, weekMode: false))
			{
				continue;
			}
			int num = 0;
			if (point.DisplayStartDate.HasValue)
			{
				num = GetStartOffset(point.DisplayStartDate.Value, startDate);
			}
			else if (point.CompletedTime.HasValue)
			{
				num = GetStartOffset(point.CompletedTime.Value, startDate);
			}
			if (num < 0 || num >= 7)
			{
				continue;
			}
			for (int i = 0; i < _matrix.Length; i++)
			{
				if (i < 256 && !_matrix[num, i])
				{
					_matrix[num, i] = true;
					CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(point);
					calendarDisplayViewModel.Row = i;
					calendarDisplayViewModel.Column = num;
					calendarDisplayViewModel.ColumnSpan = 1;
					models.Add(calendarDisplayViewModel);
					break;
				}
			}
		}
	}

	private static void AssemblySpanEvents(List<CalendarDisplayModel> tasks, List<CalendarDisplayViewModel> models, DateTime startDate)
	{
		if (tasks == null || !tasks.Any())
		{
			return;
		}
		tasks.Sort(CompareSpanTasks);
		foreach (CalendarDisplayModel task in tasks)
		{
			if (!IsValidSpanModel(task, weekMode: false))
			{
				continue;
			}
			int spanLength = GetSpanLength(startDate, task);
			if (spanLength < 1 || !task.DisplayStartDate.HasValue || !task.DisplayDueDate.HasValue)
			{
				continue;
			}
			int num = GetStartOffset(task.DisplayStartDate.Value, startDate);
			if (num < 0)
			{
				num = 0;
			}
			for (int i = 0; i < _matrix.Length; i++)
			{
				if (CheckRowHasEnoughSpace(i, num, spanLength))
				{
					SetRowOccupied(i, num, spanLength);
					CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(task);
					calendarDisplayViewModel.Row = i;
					calendarDisplayViewModel.Column = num;
					calendarDisplayViewModel.ColumnSpan = spanLength;
					models.Add(calendarDisplayViewModel);
					break;
				}
			}
		}
	}

	public static bool IsValidSpanModelInDay(CalendarDisplayModel model, DateTime date)
	{
		if (!model.IsAllDay.HasValue || model.IsAllDay.Value)
		{
			return true;
		}
		if (model.DisplayStartDate.HasValue && model.DisplayDueDate.HasValue && model.DisplayStartDate.Value.Date != date.Date && model.DisplayDueDate.Value.Date == date.Date && (model.DisplayDueDate.Value - date.Date).TotalHours <= 5.0)
		{
			return false;
		}
		return true;
	}

	public static bool IsValidSpanModel(CalendarDisplayModel model, bool weekMode)
	{
		if (weekMode)
		{
			if (model.DisplayStartDate.HasValue && model.DisplayDueDate.HasValue)
			{
				if (model.IsAllDay.HasValue && model.IsAllDay.Value && model.DisplayStartDate.Value.Date != model.DisplayDueDate.Value.Date)
				{
					return true;
				}
				if ((model.DisplayDueDate.Value - model.DisplayStartDate.Value).TotalHours > 24.0)
				{
					return true;
				}
			}
			if (!model.DisplayStartDate.HasValue && model.CompletedTime.HasValue)
			{
				return true;
			}
		}
		else if (model.DisplayStartDate.HasValue && model.DisplayDueDate.HasValue)
		{
			int num = (int)(model.DisplayDueDate.Value.Date - model.DisplayStartDate.Value.Date).TotalDays;
			if (num > 1)
			{
				return true;
			}
			if (num == 1 && (model.DisplayDueDate.Value - model.DisplayDueDate.Value.Date).TotalHours > 5.0)
			{
				return true;
			}
		}
		return false;
	}

	private static void SetRowOccupied(int row, int offset, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (offset + i < 7)
			{
				_matrix[i + offset, row] = true;
			}
		}
	}

	private static bool CheckRowHasEnoughSpace(int row, int offset, int length)
	{
		if (offset < 0)
		{
			return false;
		}
		for (int i = 0; i < length; i++)
		{
			if (offset + i < 7)
			{
				if (offset + i >= 7 || row >= 256)
				{
					return false;
				}
				if (_matrix[offset + i, row])
				{
					return false;
				}
			}
		}
		return true;
	}

	private static int GetStartOffset(DateTime modelStart, DateTime start)
	{
		return (int)(modelStart.Date - start.Date).TotalDays;
	}

	public static int GetDateSpan(DateTime start, DateTime? end, bool hideWeekend = false)
	{
		if (!end.HasValue)
		{
			return 1;
		}
		int num = (int)(end.Value.AddSeconds(-1.0).Date - start.Date).TotalDays + 1;
		if (hideWeekend)
		{
			int num2 = num;
			for (int i = 0; i < num2; i++)
			{
				DateTime dateTime = start.AddDays(i);
				if (dateTime.DayOfWeek == DayOfWeek.Sunday || dateTime.DayOfWeek == DayOfWeek.Saturday)
				{
					num--;
				}
			}
		}
		return num;
	}

	private static int GetSpanLength(DateTime startDate, CalendarDisplayModel model, bool checkDate = true)
	{
		if (model.DisplayStartDate.HasValue && model.DisplayDueDate.HasValue)
		{
			DateTime dateTime = model.DisplayStartDate.Value.Date;
			DateTime dateTime2 = model.DisplayDueDate.Value.Date;
			if (!(model.IsAllDay ?? true) && (model.DisplayDueDate.Value - model.DisplayDueDate.Value.Date).TotalHours <= 5.0)
			{
				dateTime2 = dateTime2.AddDays(-1.0);
			}
			if ((!model.IsAllDay.HasValue || !model.IsAllDay.Value) && model.DisplayDueDate.Value > dateTime2)
			{
				dateTime2 = dateTime2.AddDays(1.0);
			}
			if (dateTime < startDate && checkDate)
			{
				dateTime = startDate;
			}
			if (dateTime2 > startDate.AddDays(7.0) && checkDate)
			{
				dateTime2 = startDate.AddDays(7.0);
			}
			return (int)(dateTime2 - dateTime).TotalDays;
		}
		if (model.DisplayStartDate.HasValue && model.IsAllDay.HasValue && model.IsAllDay.Value)
		{
			return 1;
		}
		if (!model.DisplayStartDate.HasValue && model.CompletedTime.HasValue)
		{
			return 1;
		}
		return 0;
	}

	public static int CompareSpanTasks(CalendarDisplayModel left, CalendarDisplayModel right)
	{
		return CalendarSortHelper.CompareForGridSpanItems(left, right, DateTime.Today);
	}

	public static int CompareTasks(CalendarDisplayModel left, CalendarDisplayModel right)
	{
		return CalendarSortHelper.CompareForGridPointItems(left, right, DateTime.Today);
	}
}
