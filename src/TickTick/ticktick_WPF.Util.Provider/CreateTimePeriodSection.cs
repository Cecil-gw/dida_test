using System;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class CreateTimePeriodSection : Section
{
	public CreateTimePeriodSection(string name, int ordinal, DateTime? sectionDate, string sectionId)
		: base(name, ordinal, sectionDate, sectionId, "", canSort: false, canSwitch: true)
	{
		base.SectionEntityId = sectionId;
	}

	public override DateTime? GetStartDate()
	{
		return null;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return displayType != DisplayType.Habit;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		if (sortBy != Constants.SortType.createdTime && sortBy != Constants.SortType.modifiedTime)
		{
			return sortBy != Constants.SortType.title;
		}
		return false;
	}
}
