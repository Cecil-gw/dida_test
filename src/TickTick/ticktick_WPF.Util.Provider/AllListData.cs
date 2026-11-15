using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class AllListData : SortProjectData
{
	public AllListData()
	{
		base.AddTaskHint = Utils.GetString("CenterAddTaskTextBoxPreviewTextInbox");
		TitleInProjectGroup = Utils.GetString("Inbox");
		base.ShowProjectSort = true;
		base.ShowLoadMore = true;
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 });
	}

	public override string GetEmptyTitle(bool allCompleted)
	{
		return Utils.GetString("NoTasks");
	}

	public override string GetEmptyContent(bool allCompleted)
	{
		return Utils.GetString("NoTaskNormalDesc");
	}

	public override string GetTitle()
	{
		return Utils.GetString("All");
	}

	public override void SaveSortOption(SortOption sortOption)
	{
		SortProjectData.SaveSpecialProjectSortType("SortTypeOfAllProject", sortOption);
		LocalSettings.Settings.SaveSmartProjectOptions("all", sortOption);
		DataChangedNotifier.NotifySortOptionChanged("all");
	}
}
