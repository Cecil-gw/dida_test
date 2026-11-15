using System;
using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class TodayListData : SortProjectData
{
	public TodayListData()
	{
		base.AddTaskHint = string.Format(Utils.GetString("CenterAddTaskDateTextBoxPreviewText"), Utils.GetString("Today"), Utils.GetString("Inbox"), Utils.GetString("Task").ToLower());
		TitleInProjectGroup = Utils.GetString("Inbox");
		base.DefaultTaskDate = DateTime.Now;
		base.ShowProjectSort = true;
	}

	public override void SaveSortOption(SortOption sortOption)
	{
		SortProjectData.SaveSpecialProjectSortType("SortTypeOfToday", sortOption);
		LocalSettings.Settings.SaveSmartProjectOptions("today", sortOption);
		DataChangedNotifier.NotifySortOptionChanged("today");
	}

	public override string GetTitle()
	{
		return Utils.GetString("Today");
	}

	public override string GetEmptyTitle(bool allCompleted)
	{
		if (allCompleted)
		{
			return Utils.GetString("TodayTaskAllCompleted");
		}
		return Utils.GetString("NoTaskToday");
	}

	public override string GetEmptyContent(bool allCompleted)
	{
		if (allCompleted)
		{
			return Utils.GetString("TodayTaskAllCompeletedDesc");
		}
		return GetEmptyType() switch
		{
			1 => Utils.GetString("NoTaskTodayMorningDesc"), 
			2 => Utils.GetString("NoTaskTodayAfternoonDesc"), 
			3 => Utils.GetString("NoTaskTodayNightDesc"), 
			4 => Utils.GetString("NoTaskWeekendDesc"), 
			_ => null, 
		};
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		if (allCompleted)
		{
			return new EmptyIconsSourceModel("TaskCompletedEmpty");
		}
		return GetEmptyType() switch
		{
			1 => new EmptyIconsSourceModel("TodayMorningEmpty", new List<int> { 1, 2, 4, 5 }), 
			2 => new EmptyIconsSourceModel("TodayAfternoonEmpty"), 
			3 => new EmptyIconsSourceModel("TodayNightEmpty", new List<int> { 2, 4, 5 }), 
			4 => new EmptyIconsSourceModel("TodayWeekendEmpty"), 
			_ => null, 
		};
	}

	private static int GetEmptyType()
	{
		bool num = HolidayManager.IsNonWorkWeekend(DateTime.Today);
		int hour = DateTime.Now.Hour;
		if (num && hour >= 5 && hour < 18)
		{
			return 4;
		}
		if (hour >= 5 && hour < 12)
		{
			return 1;
		}
		if (hour >= 12 && hour < 18)
		{
			return 2;
		}
		if (hour >= 18 && hour < 23)
		{
			return 3;
		}
		if (hour >= 23 || hour < 5)
		{
			return 3;
		}
		return 1;
	}
}
