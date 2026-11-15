using System;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class OutdatedSection : Section
{
	public bool ShowPostpone { get; set; }

	public OutdatedSection()
	{
		base.Name = Utils.GetString("overdue");
		base.Ordinal = 1L;
		base.SectionDate = DateTime.Now.AddDays(-1.0).Date;
		base.SectionId = "outdated";
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
