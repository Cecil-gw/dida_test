using System;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class WeekListData : SortProjectData
{
	public WeekListData()
	{
		base.EmptyTitle = Utils.GetString("NoTaskNext7Day");
		base.EmptyContent = Utils.GetString("TaskItEasy");
		base.AddTaskHint = string.Format(Utils.GetString("CenterAddTaskDateTextBoxPreviewText"), Utils.GetString("Tomorrow"), Utils.GetString("Inbox"), Utils.GetString("Task").ToLower());
		TitleInProjectGroup = Utils.GetString("Inbox");
		base.DefaultTaskDate = DateTime.Now;
		base.ShowProjectSort = true;
	}

	public override string GetTitle()
	{
		return Utils.GetString("Next7Day");
	}

	public override void SaveSortOption(SortOption sortOption)
	{
		SortProjectData.SaveSpecialProjectSortType("sortTypeOfWeek", sortOption);
		LocalSettings.Settings.SaveSmartProjectOptions("week", sortOption);
		DataChangedNotifier.NotifySortOptionChanged("n7ds");
	}

	public virtual async Task<bool> IsTaskInProject(TaskModel task)
	{
		return task.startDate.HasValue && task.startDate.Value.Date <= DateTime.Now.AddDays(6.0).Date;
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("Next7DayEmpty", "Next7DayEmpty0");
	}
}
