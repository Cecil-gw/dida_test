using System;
using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class TomorrowListData : SortProjectData
{
	public TomorrowListData()
	{
		base.EmptyTitle = Utils.GetString("NoTaskTomorrow");
		base.AddTaskHint = string.Format(Utils.GetString("CenterAddTaskDateTextBoxPreviewText"), Utils.GetString("Tomorrow"), Utils.GetString("Inbox"), Utils.GetString("Task").ToLower());
		TitleInProjectGroup = Utils.GetString("Inbox");
		base.DefaultTaskDate = DateTime.Now.AddDays(1.0);
		base.ShowProjectSort = true;
	}

	public override string GetTitle()
	{
		return Utils.GetString("Tomorrow");
	}

	public override void SaveSortOption(SortOption sortOption)
	{
		SortProjectData.SaveSpecialProjectSortType("SortTypeOfTomorrow", sortOption);
		LocalSettings.Settings.SaveSmartProjectOptions("tomorrow", sortOption);
		DataChangedNotifier.NotifySortOptionChanged("tomorrow");
	}

	public override string GetEmptyContent(bool allCompleted)
	{
		if (GetEmptyType() == 1)
		{
			return Utils.GetString("NoTaskTomorrowWorkDayDesc");
		}
		return Utils.GetString("NoTaskWeekendDesc");
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		if (GetEmptyType() == 1)
		{
			return new EmptyIconsSourceModel("TomorrowWorkdayEmpty", new List<int> { 1, 2, 3, 4, 6 });
		}
		return new EmptyIconsSourceModel("TomorrowWeekendEmpty");
	}

	private static int GetEmptyType()
	{
		if (HolidayManager.IsWorkDay(DateTime.Today.AddDays(1.0)))
		{
			return 1;
		}
		return 2;
	}
}
