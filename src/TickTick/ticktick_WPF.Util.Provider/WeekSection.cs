using System;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class WeekSection : Section
{
	public WeekSection(string name, int oridinal, DateTime? sectionDate, string sectionId, string sectionUserId = "")
		: base(name, oridinal, sectionDate, sectionId, sectionUserId)
	{
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return displayType != DisplayType.Habit;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return sortBy == Constants.SortType.sortOrder;
	}
}
