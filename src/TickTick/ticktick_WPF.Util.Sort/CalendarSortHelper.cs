using System;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Calendar.Agenda;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Util.Sort;

public static class CalendarSortHelper
{
	public static int CompareForAgendaView(CalendarAgendaItemViewModel left, CalendarAgendaItemViewModel right, DateTime today)
	{
		bool flag = left.Type == DisplayType.Countdown;
		bool flag2 = right.Type == DisplayType.Countdown;
		if (flag != flag2)
		{
			if (!flag)
			{
				return 1;
			}
			return -1;
		}
		bool showAsAllDay = left.ShowAsAllDay;
		bool showAsAllDay2 = right.ShowAsAllDay;
		if (showAsAllDay != showAsAllDay2)
		{
			if (!showAsAllDay)
			{
				return 1;
			}
			return -1;
		}
		if (showAsAllDay)
		{
			return CompareAgendaAllDayItems(left, right, today);
		}
		return CompareTimePointItems(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.Type, left.Status, left.Priority, GetContainerSortOrder(left.SourceViewModel), left.SourceViewModel?.SortOrder ?? 0, right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.Type, right.Status, right.Priority, GetContainerSortOrder(right.SourceViewModel), right.SourceViewModel?.SortOrder ?? 0, today);
	}

	public static int CompareForGridSpanItems(CalendarDisplayModel left, CalendarDisplayModel right, DateTime today)
	{
		GridGroupSortValue gridGroupSortValue = GetGridGroupSortValue(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.Type, left.Status, today);
		GridGroupSortValue gridGroupSortValue2 = GetGridGroupSortValue(right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.Type, right.Status, today);
		if (gridGroupSortValue != gridGroupSortValue2)
		{
			return gridGroupSortValue.CompareTo(gridGroupSortValue2);
		}
		return CompareSpanItemsInternal(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.Type, left.Status, left.Priority, GetContainerSortOrder(left.SourceViewModel), left.SortOrder, left.Title, right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.Type, right.Status, right.Priority, GetContainerSortOrder(right.SourceViewModel), right.SortOrder, right.Title, today, CalendarViewType.GridMonth);
	}

	public static int CompareForGridPointItems(CalendarDisplayModel left, CalendarDisplayModel right, DateTime today)
	{
		return CompareGridPointItemsInternal(left, right, today);
	}

	public static int CompareForTimelineView(TimelineCellViewModel left, TimelineCellViewModel right, DateTime today, bool isAllDaySection)
	{
		if (isAllDaySection)
		{
			return CompareForTimelineAllDaySection(left, right, today);
		}
		return CompareForTimelineTimeSection(left, right, today);
	}

	public static int CompareForTaskCell(TaskCellViewModel left, TaskCellViewModel right, DateTime today)
	{
		if (left.DisplayStartDate?.Date != right.DisplayStartDate?.Date)
		{
			return left.DisplayStartDate?.CompareTo(right.DisplayStartDate) ?? 0;
		}
		if (left.DisplayStartDate.HasValue && right.DisplayStartDate.HasValue)
		{
			int num = left.DisplayStartDate.Value.TimeOfDay.CompareTo(right.DisplayStartDate.Value.TimeOfDay);
			if (num != 0)
			{
				return num;
			}
		}
		bool num2 = IsSpanItem(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay);
		bool flag = IsSpanItem(right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay);
		if (!num2 && !flag && left.DisplayStartDate == right.DisplayStartDate)
		{
			bool flag2 = !left.DisplayDueDate.HasValue;
			bool flag3 = !right.DisplayDueDate.HasValue;
			if (flag2 != flag3)
			{
				if (!flag2)
				{
					return 1;
				}
				return -1;
			}
			if (left.DisplayDueDate.HasValue && right.DisplayDueDate.HasValue && left.DisplayDueDate != right.DisplayDueDate)
			{
				return left.DisplayDueDate.Value.CompareTo(right.DisplayDueDate.Value);
			}
		}
		TimePointType timePointTypeSortWeight = GetTimePointTypeSortWeight(left.Type);
		TimePointType timePointTypeSortWeight2 = GetTimePointTypeSortWeight(right.Type);
		if (timePointTypeSortWeight != timePointTypeSortWeight2)
		{
			return timePointTypeSortWeight.CompareTo(timePointTypeSortWeight2);
		}
		if (left.Priority != right.Priority)
		{
			return right.Priority.CompareTo(left.Priority);
		}
		if (GetContainerSortOrder(left.SourceViewModel) != GetContainerSortOrder(right.SourceViewModel))
		{
			return GetContainerSortOrder(left.SourceViewModel).CompareTo(GetContainerSortOrder(right.SourceViewModel));
		}
		if (left.SourceViewModel?.SortOrder != right.SourceViewModel?.SortOrder)
		{
			return (left.SourceViewModel?.SortOrder ?? 0).CompareTo(right.SourceViewModel?.SortOrder ?? 0);
		}
		return 0;
	}

	public static bool IsStartedBeforeToday(DateTime? startDate, DateTime today)
	{
		if (startDate.HasValue)
		{
			return startDate.Value.Date < today;
		}
		return false;
	}

	public static bool IsStartedToday(DateTime? startDate, DateTime today)
	{
		if (startDate.HasValue)
		{
			return startDate.Value.Date == today;
		}
		return false;
	}

	public static bool HasSpecificTime(DateTime? startDate, bool? isAllDay)
	{
		if (!startDate.HasValue)
		{
			return false;
		}
		if (isAllDay == true)
		{
			return false;
		}
		return startDate.Value.TimeOfDay != TimeSpan.Zero;
	}

	public static bool IsSpanItem(DateTime? startDate, DateTime? dueDate, bool? isAllDay)
	{
		if (!startDate.HasValue || !dueDate.HasValue)
		{
			return false;
		}
		double totalDays = (dueDate.Value.Date - startDate.Value.Date).TotalDays;
		if (isAllDay == true)
		{
			return totalDays >= 1.0;
		}
		if (!((dueDate.Value - startDate.Value).TotalHours > 24.0))
		{
			return totalDays >= 1.0;
		}
		return true;
	}

	public static bool IsUncompleted(int status)
	{
		return status == 0;
	}

	public static int GetSpanTypeSortWeight(DisplayType type)
	{
		switch (type)
		{
		case DisplayType.Task:
		case DisplayType.CheckItem:
		case DisplayType.Agenda:
			return 0;
		case DisplayType.Event:
			return 1;
		default:
			return 2;
		}
	}

	private static TimePointType GetTimePointTypeSortWeight(DisplayType type)
	{
		return type switch
		{
			DisplayType.Countdown => TimePointType.Countdown, 
			DisplayType.Task => TimePointType.Task, 
			DisplayType.CheckItem => TimePointType.Task, 
			DisplayType.Agenda => TimePointType.Task, 
			DisplayType.Event => TimePointType.Event, 
			DisplayType.Course => TimePointType.Course, 
			DisplayType.Note => TimePointType.Note, 
			DisplayType.Habit => TimePointType.Habit, 
			DisplayType.Pomo => TimePointType.Pomo, 
			_ => TimePointType.Other, 
		};
	}

	private static AgendaGroupSortValue GetAgendaAllDayGroupSortValue(DateTime? startDate, DateTime? dueDate, bool? isAllDay, DisplayType type, DateTime today)
	{
		if (IsSpanItem(startDate, dueDate, isAllDay))
		{
			if (IsStartedBeforeToday(startDate, today))
			{
				return AgendaGroupSortValue.SpanBeforeToday;
			}
			if (IsStartedToday(startDate, today) && HasSpecificTime(startDate, isAllDay))
			{
				return AgendaGroupSortValue.SpanTodayWithTime;
			}
			return AgendaGroupSortValue.SpanTodayAllDay;
		}
		switch (type)
		{
		case DisplayType.Task:
		case DisplayType.CheckItem:
		case DisplayType.Agenda:
			return AgendaGroupSortValue.AllDayTask;
		case DisplayType.Event:
			return AgendaGroupSortValue.AllDayEvent;
		case DisplayType.Note:
			return AgendaGroupSortValue.AllDayNote;
		case DisplayType.Habit:
			return AgendaGroupSortValue.AllDayHabit;
		default:
			return AgendaGroupSortValue.TimePoint;
		}
	}

	private static TimelineAllDayGroupSortValue GetTimelineAllDayGroupSortValue(DateTime? startDate, DateTime? dueDate, bool? isAllDay, DisplayType type, int status, DateTime today)
	{
		bool num = IsSpanItem(startDate, dueDate, isAllDay);
		bool flag = !IsUncompleted(status);
		if (num)
		{
			if (IsStartedBeforeToday(startDate, today))
			{
				return TimelineAllDayGroupSortValue.SpanBeforeToday;
			}
			return TimelineAllDayGroupSortValue.SpanToday;
		}
		if (flag)
		{
			return TimelineAllDayGroupSortValue.CompletedAllDay;
		}
		switch (type)
		{
		case DisplayType.Countdown:
			return TimelineAllDayGroupSortValue.Countdown;
		case DisplayType.Task:
		case DisplayType.CheckItem:
		case DisplayType.Agenda:
			return TimelineAllDayGroupSortValue.AllDayTask;
		case DisplayType.Event:
			return TimelineAllDayGroupSortValue.AllDayEvent;
		case DisplayType.Note:
			return TimelineAllDayGroupSortValue.AllDayNote;
		case DisplayType.Habit:
			return TimelineAllDayGroupSortValue.AllDayHabit;
		default:
			return TimelineAllDayGroupSortValue.TimePoint;
		}
	}

	private static GridGroupSortValue GetGridGroupSortValue(DateTime? startDate, DateTime? dueDate, bool? isAllDay, DisplayType type, int status, DateTime today)
	{
		bool flag = IsSpanItem(startDate, dueDate, isAllDay);
		bool flag2 = !IsUncompleted(status);
		if (type == DisplayType.Pomo)
		{
			return GridGroupSortValue.Pomo;
		}
		if (flag)
		{
			if (IsStartedBeforeToday(startDate, today))
			{
				return GridGroupSortValue.SpanBeforeToday;
			}
			return GridGroupSortValue.SpanToday;
		}
		if (flag2)
		{
			return GridGroupSortValue.Completed;
		}
		switch (type)
		{
		case DisplayType.Countdown:
			return GridGroupSortValue.Countdown;
		case DisplayType.Task:
		case DisplayType.CheckItem:
		case DisplayType.Agenda:
			if (isAllDay == true)
			{
				return GridGroupSortValue.AllDayTask;
			}
			return GridGroupSortValue.MixedArea;
		case DisplayType.Event:
			if (isAllDay == true)
			{
				return GridGroupSortValue.AllDayEvent;
			}
			return GridGroupSortValue.MixedArea;
		case DisplayType.Course:
			return GridGroupSortValue.MixedArea;
		case DisplayType.Note:
			return GridGroupSortValue.Note;
		case DisplayType.Habit:
			return GridGroupSortValue.Habit;
		case DisplayType.Pomo:
			return GridGroupSortValue.Pomo;
		default:
			return GridGroupSortValue.Other;
		}
	}

	private static int CompareAgendaAllDayItems(CalendarAgendaItemViewModel left, CalendarAgendaItemViewModel right, DateTime today)
	{
		AgendaGroupSortValue agendaAllDayGroupSortValue = GetAgendaAllDayGroupSortValue(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.Type, today);
		AgendaGroupSortValue agendaAllDayGroupSortValue2 = GetAgendaAllDayGroupSortValue(right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.Type, today);
		if (agendaAllDayGroupSortValue != agendaAllDayGroupSortValue2)
		{
			return agendaAllDayGroupSortValue.CompareTo(agendaAllDayGroupSortValue2);
		}
		return CompareSameAgendaGroup(left, right, (int)agendaAllDayGroupSortValue, today);
	}

	private static int CompareSameAgendaGroup(CalendarAgendaItemViewModel left, CalendarAgendaItemViewModel right, int groupValue, DateTime today)
	{
		if (groupValue <= 4 && IsSpanItem(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay))
		{
			return CompareSpanItemsInternal(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.Type, left.Status, left.Priority, GetContainerSortOrder(left.SourceViewModel), left.SourceViewModel?.SortOrder ?? 0, left.Title, right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.Type, right.Status, right.Priority, GetContainerSortOrder(right.SourceViewModel), right.SourceViewModel?.SortOrder ?? 0, right.Title, today, CalendarViewType.Agenda);
		}
		if ((groupValue == 2 || groupValue == 3) && left.Priority != right.Priority)
		{
			return right.Priority.CompareTo(left.Priority);
		}
		return CompareFallback(GetContainerSortOrder(left.SourceViewModel), left.SourceViewModel?.SortOrder ?? 0, left.Title, GetContainerSortOrder(right.SourceViewModel), right.SourceViewModel?.SortOrder ?? 0, right.Title);
	}

	private static int CompareSpanItemsInternal(DateTime? leftStartDate, DateTime? leftDueDate, bool? leftIsAllDay, DisplayType leftType, int leftStatus, int leftPriority, long leftParentOrder, long leftSortOrder, string leftTitle, DateTime? rightStartDate, DateTime? rightDueDate, bool? rightIsAllDay, DisplayType rightType, int rightStatus, int rightPriority, long rightParentOrder, long rightSortOrder, string rightTitle, DateTime today, CalendarViewType viewType)
	{
		DateTime? spanDisplayEndDate = GetSpanDisplayEndDate(leftStartDate, leftDueDate, leftIsAllDay, viewType);
		DateTime? spanDisplayEndDate2 = GetSpanDisplayEndDate(rightStartDate, rightDueDate, rightIsAllDay, viewType);
		if (leftStartDate?.Date != rightStartDate?.Date)
		{
			return DateTime.Compare(leftStartDate?.Date ?? DateTime.MinValue, rightStartDate?.Date ?? DateTime.MinValue);
		}
		if (spanDisplayEndDate != spanDisplayEndDate2)
		{
			if (viewType == CalendarViewType.Agenda)
			{
				return DateTime.Compare(spanDisplayEndDate ?? DateTime.MinValue, spanDisplayEndDate2 ?? DateTime.MinValue);
			}
			return DateTime.Compare(spanDisplayEndDate2 ?? DateTime.MinValue, spanDisplayEndDate ?? DateTime.MinValue);
		}
		if (leftIsAllDay != rightIsAllDay)
		{
			if (leftIsAllDay != true)
			{
				return -1;
			}
			return 1;
		}
		if (leftIsAllDay == false && rightIsAllDay == false && leftStartDate.HasValue && rightStartDate.HasValue && leftStartDate.Value.TimeOfDay != rightStartDate.Value.TimeOfDay)
		{
			return leftStartDate.Value.TimeOfDay.CompareTo(rightStartDate.Value.TimeOfDay);
		}
		if (leftDueDate.HasValue && rightDueDate.HasValue && leftDueDate != rightDueDate)
		{
			if (leftIsAllDay == false && rightIsAllDay == false)
			{
				if (viewType == CalendarViewType.Agenda)
				{
					return leftDueDate.Value.CompareTo(rightDueDate.Value);
				}
				return rightDueDate.Value.CompareTo(leftDueDate.Value);
			}
			if (viewType == CalendarViewType.Agenda)
			{
				return leftDueDate.Value.CompareTo(rightDueDate.Value);
			}
			return rightDueDate.Value.CompareTo(leftDueDate.Value);
		}
		int spanTypeSortWeight = GetSpanTypeSortWeight(leftType);
		int spanTypeSortWeight2 = GetSpanTypeSortWeight(rightType);
		if (spanTypeSortWeight != spanTypeSortWeight2)
		{
			return spanTypeSortWeight.CompareTo(spanTypeSortWeight2);
		}
		bool flag = !IsUncompleted(leftStatus);
		bool flag2 = !IsUncompleted(rightStatus);
		if (viewType != CalendarViewType.GridMonth && viewType != CalendarViewType.TimelineDay && flag != flag2)
		{
			if (!flag)
			{
				return -1;
			}
			return 1;
		}
		if (leftPriority != rightPriority)
		{
			return rightPriority.CompareTo(leftPriority);
		}
		return CompareFallback(leftParentOrder, leftSortOrder, leftTitle, rightParentOrder, rightSortOrder, rightTitle);
	}

	private static DateTime? GetSpanDisplayEndDate(DateTime? startDate, DateTime? dueDate, bool? isAllDay, CalendarViewType viewType)
	{
		if (!dueDate.HasValue)
		{
			return null;
		}
		DateTime date = dueDate.Value.Date;
		if (isAllDay == true)
		{
			if (startDate.HasValue && dueDate.Value.Date > startDate.Value.Date)
			{
				return date.AddDays(-1.0);
			}
			return date;
		}
		if ((viewType == CalendarViewType.GridMonth || viewType == CalendarViewType.TimelineDay) && startDate.HasValue && dueDate.Value.Date > startDate.Value.Date && (dueDate.Value - dueDate.Value.Date).TotalHours <= 5.0)
		{
			return date.AddDays(-1.0);
		}
		return date;
	}

	private static int CompareTimePointItems(DateTime? leftStartDate, DateTime? leftDueDate, bool? leftIsAllDay, DisplayType leftType, int leftStatus, int leftPriority, long leftParentOrder, long leftSortOrder, DateTime? rightStartDate, DateTime? rightDueDate, bool? rightIsAllDay, DisplayType rightType, int rightStatus, int rightPriority, long rightParentOrder, long rightSortOrder, DateTime today)
	{
		if (leftStartDate.HasValue && rightStartDate.HasValue && leftStartDate != rightStartDate)
		{
			return leftStartDate.Value.CompareTo(rightStartDate.Value);
		}
		TimePointType timePointTypeSortWeight = GetTimePointTypeSortWeight(leftType);
		TimePointType timePointTypeSortWeight2 = GetTimePointTypeSortWeight(rightType);
		if (timePointTypeSortWeight != timePointTypeSortWeight2)
		{
			return timePointTypeSortWeight.CompareTo(timePointTypeSortWeight2);
		}
		bool num = IsSpanItem(leftStartDate, leftDueDate, leftIsAllDay);
		bool flag = IsSpanItem(rightStartDate, rightDueDate, rightIsAllDay);
		if (!num && !flag && leftStartDate == rightStartDate)
		{
			bool flag2 = !leftDueDate.HasValue;
			bool flag3 = !rightDueDate.HasValue;
			if (flag2 != flag3)
			{
				if (!flag2)
				{
					return 1;
				}
				return -1;
			}
			if (leftDueDate.HasValue && rightDueDate.HasValue && leftDueDate != rightDueDate)
			{
				return leftDueDate.Value.CompareTo(rightDueDate.Value);
			}
		}
		if (leftPriority != rightPriority)
		{
			return rightPriority.CompareTo(leftPriority);
		}
		return CompareFallback(leftParentOrder, leftSortOrder, null, rightParentOrder, rightSortOrder, null);
	}

	private static int CompareForTimelineAllDaySection(TimelineCellViewModel left, TimelineCellViewModel right, DateTime today)
	{
		TimelineAllDayGroupSortValue timelineAllDayGroupSortValue = GetTimelineAllDayGroupSortValue(left.StartDate, left.EndDate, left.IsAllDay, left.DisplayModel.Type, left.Status, today);
		TimelineAllDayGroupSortValue timelineAllDayGroupSortValue2 = GetTimelineAllDayGroupSortValue(right.StartDate, right.EndDate, right.IsAllDay, right.DisplayModel.Type, right.Status, today);
		if (timelineAllDayGroupSortValue != timelineAllDayGroupSortValue2)
		{
			return timelineAllDayGroupSortValue.CompareTo(timelineAllDayGroupSortValue2);
		}
		return CompareTimelineSameGroup(left, right, (int)timelineAllDayGroupSortValue, today);
	}

	private static int CompareTimelineSameGroup(TimelineCellViewModel left, TimelineCellViewModel right, int groupValue, DateTime today)
	{
		if (groupValue <= 2 && IsSpanItem(left.StartDate, left.EndDate, left.IsAllDay))
		{
			return CompareSpanItemsInternal(left.StartDate, left.EndDate, left.IsAllDay, left.DisplayModel.Type, left.Status, left.DisplayModel.Priority, left.DisplayModel?.ParentOrder ?? 0, left.DisplayModel.SortOrder, left.DisplayModel.Title, right.StartDate, right.EndDate, right.IsAllDay, right.DisplayModel.Type, right.Status, right.DisplayModel.Priority, right.DisplayModel?.ParentOrder ?? 0, right.DisplayModel.SortOrder, right.DisplayModel.Title, today, CalendarViewType.TimelineDay);
		}
		if (groupValue == 8)
		{
			return CompareCompletedItems(left.StartDate, left.EndDate, left.IsAllDay, left.DisplayModel.CompletedTime, left.DisplayModel.Type, right.StartDate, right.EndDate, right.IsAllDay, right.DisplayModel.CompletedTime, right.DisplayModel.Type);
		}
		if (left.DisplayModel.Priority != right.DisplayModel.Priority)
		{
			return right.DisplayModel.Priority.CompareTo(left.DisplayModel.Priority);
		}
		return CompareFallback(GetContainerSortOrder(left.DisplayModel), left.DisplayModel.SortOrder, left.DisplayModel.Title, GetContainerSortOrder(right.DisplayModel), right.DisplayModel.SortOrder, right.DisplayModel.Title);
	}

	private static int CompareForTimelineTimeSection(TimelineCellViewModel left, TimelineCellViewModel right, DateTime today)
	{
		return CompareTimePointItems(left.StartDate, left.EndDate, left.IsAllDay, left.DisplayModel.Type, left.Status, left.DisplayModel.Priority, GetContainerSortOrder(left.DisplayModel), left.DisplayModel.SortOrder, right.StartDate, right.EndDate, right.IsAllDay, right.DisplayModel.Type, right.Status, right.DisplayModel.Priority, GetContainerSortOrder(right.DisplayModel), right.DisplayModel.SortOrder, today);
	}

	private static int CompareGridPointItemsInternal(CalendarDisplayModel left, CalendarDisplayModel right, DateTime today)
	{
		GridGroupSortValue gridGroupSortValue = GetGridGroupSortValue(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.Type, left.Status, today);
		GridGroupSortValue gridGroupSortValue2 = GetGridGroupSortValue(right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.Type, right.Status, today);
		if (gridGroupSortValue != gridGroupSortValue2)
		{
			return gridGroupSortValue.CompareTo(gridGroupSortValue2);
		}
		switch (gridGroupSortValue)
		{
		case GridGroupSortValue.Countdown:
			return CompareFallback(GetContainerSortOrder(left.SourceViewModel), left.SortOrder, left.Title, GetContainerSortOrder(right.SourceViewModel), right.SortOrder, right.Title);
		case GridGroupSortValue.MixedArea:
			return CompareTimePointItems(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.Type, left.Status, left.Priority, GetContainerSortOrder(left.SourceViewModel), left.SortOrder, right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.Type, right.Status, right.Priority, GetContainerSortOrder(right.SourceViewModel), right.SortOrder, today);
		case GridGroupSortValue.Completed:
			return CompareGridCompletedItems(left.DisplayStartDate, left.DisplayDueDate, left.IsAllDay, left.CompletedTime, left.Type, right.DisplayStartDate, right.DisplayDueDate, right.IsAllDay, right.CompletedTime, right.Type);
		case GridGroupSortValue.Note:
		{
			bool flag = HasSpecificTime(left.DisplayStartDate, left.IsAllDay);
			bool flag2 = HasSpecificTime(right.DisplayStartDate, right.IsAllDay);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			if (left.DisplayStartDate.HasValue && right.DisplayStartDate.HasValue && left.DisplayStartDate != right.DisplayStartDate)
			{
				return left.DisplayStartDate.Value.CompareTo(right.DisplayStartDate.Value);
			}
			break;
		}
		}
		if (gridGroupSortValue == GridGroupSortValue.Pomo && left.DisplayStartDate.HasValue && right.DisplayStartDate.HasValue && left.DisplayStartDate != right.DisplayStartDate)
		{
			return left.DisplayStartDate.Value.CompareTo(right.DisplayStartDate.Value);
		}
		if (left.Priority != right.Priority)
		{
			return right.Priority.CompareTo(left.Priority);
		}
		return CompareFallback(GetContainerSortOrder(left.SourceViewModel), left.SortOrder, left.Title, GetContainerSortOrder(right.SourceViewModel), right.SortOrder, right.Title);
	}

	private static int CompareGridCompletedItems(DateTime? leftStartDate, DateTime? leftDueDate, bool? leftIsAllDay, DateTime? leftCompletedTime, DisplayType leftType, DateTime? rightStartDate, DateTime? rightDueDate, bool? rightIsAllDay, DateTime? rightCompletedTime, DisplayType rightType)
	{
		GetTimeType(leftStartDate, leftDueDate, leftIsAllDay);
		GetTimeType(rightStartDate, rightDueDate, rightIsAllDay);
		if (leftType == DisplayType.Habit && rightType != DisplayType.Habit)
		{
			return 1;
		}
		if (rightType == DisplayType.Habit && leftType != DisplayType.Habit)
		{
			return -1;
		}
		return CompareCompletedItems(leftStartDate, leftDueDate, leftIsAllDay, leftCompletedTime, leftType, rightStartDate, rightDueDate, rightIsAllDay, rightCompletedTime, rightType);
	}

	private static int CompareCompletedItems(DateTime? leftStartDate, DateTime? leftDueDate, bool? leftIsAllDay, DateTime? leftCompletedTime, DisplayType leftType, DateTime? rightStartDate, DateTime? rightDueDate, bool? rightIsAllDay, DateTime? rightCompletedTime, DisplayType rightType)
	{
		CompleteTimeType timeType = GetTimeType(leftStartDate, leftDueDate, leftIsAllDay);
		CompleteTimeType timeType2 = GetTimeType(rightStartDate, rightDueDate, rightIsAllDay);
		if (timeType != timeType2)
		{
			return timeType.CompareTo(timeType2);
		}
		if (leftStartDate.HasValue && rightStartDate.HasValue && leftStartDate != rightStartDate)
		{
			return leftStartDate.Value.CompareTo(rightStartDate.Value);
		}
		if (leftStartDate.HasValue && rightStartDate.HasValue && leftStartDate == rightStartDate)
		{
			bool flag = !leftDueDate.HasValue;
			bool flag2 = !rightDueDate.HasValue;
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			if (leftDueDate.HasValue && rightDueDate.HasValue && leftDueDate != rightDueDate)
			{
				return leftDueDate.Value.CompareTo(rightDueDate.Value);
			}
		}
		int spanTypeSortWeight = GetSpanTypeSortWeight(leftType);
		int spanTypeSortWeight2 = GetSpanTypeSortWeight(rightType);
		if (spanTypeSortWeight != spanTypeSortWeight2)
		{
			return spanTypeSortWeight.CompareTo(spanTypeSortWeight2);
		}
		if (leftCompletedTime.HasValue && rightCompletedTime.HasValue && leftCompletedTime != rightCompletedTime)
		{
			return rightCompletedTime.Value.CompareTo(leftCompletedTime.Value);
		}
		return 0;
	}

	private static CompleteTimeType GetTimeType(DateTime? startDate, DateTime? dueDate, bool? isAllDay)
	{
		if (!startDate.HasValue)
		{
			return CompleteTimeType.NoDate;
		}
		if (isAllDay == true)
		{
			return CompleteTimeType.AllDay;
		}
		if (dueDate.HasValue && dueDate.Value.Date > startDate.Value.Date)
		{
			return CompleteTimeType.TimeSpan;
		}
		return CompleteTimeType.TimePoint;
	}

	private static int CompareFallback(long leftParentOrder, long leftSortOrder, string leftTitle, long rightParentOrder, long rightSortOrder, string rightTitle)
	{
		if (leftParentOrder != rightParentOrder)
		{
			return leftParentOrder.CompareTo(rightParentOrder);
		}
		if (leftSortOrder != rightSortOrder)
		{
			return leftSortOrder.CompareTo(rightSortOrder);
		}
		if (leftTitle != null && rightTitle != null)
		{
			return string.Compare(leftTitle, rightTitle, StringComparison.Ordinal);
		}
		return 0;
	}

	private static long GetContainerSortOrder(TaskBaseViewModel viewModel)
	{
		if (viewModel == null)
		{
			return 0L;
		}
		if (viewModel.ParentOrder == 0L)
		{
			return viewModel.ProjectOrder;
		}
		return viewModel.ParentOrder;
	}

	private static long GetContainerSortOrder(CalendarDisplayModel displayModel)
	{
		return GetContainerSortOrder(displayModel?.SourceViewModel);
	}
}
