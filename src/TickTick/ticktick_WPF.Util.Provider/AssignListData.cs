using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class AssignListData : SortProjectData
{
	public AssignListData()
	{
		base.EmptyTitle = Utils.GetString("NoTaskAssignedToMe");
		base.EmptyContent = Utils.GetString("NoTaskAssignToMeDesc");
		base.AddTaskHint = string.Empty;
		base.ShowProjectSort = true;
	}

	public override string GetTitle()
	{
		return Utils.GetString("AssignToMe");
	}

	public override void SaveSortOption(SortOption sortOption)
	{
		SortProjectData.SaveSpecialProjectSortType("SortTypeOfAssignMe", sortOption);
		LocalSettings.Settings.SaveSmartProjectOptions("assignToMe", sortOption);
		DataChangedNotifier.NotifySortOptionChanged("assigned");
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("AssignToMeEmpty", new List<int> { 1, 2, 4, 5 });
	}
}
