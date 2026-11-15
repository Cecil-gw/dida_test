using System;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class YesterdaySection : Section
{
	public YesterdaySection()
	{
		DateTime value = DateTime.Today.AddDays(-1.0);
		base.Name = Utils.GetString("PublicYesterday") + ", " + Utils.GetString(value.DayOfWeek.ToString().Substring(0, 3));
		base.Ordinal = 2L;
		base.SectionDate = value;
		base.SectionId = "yesterday";
		base.SectionEntityId = DateUtils.GetCreateTimeSectionEntityId(DateUtils.CreateTimeCategory.Yesterday);
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

	public override DateTime? GetStartDate()
	{
		return null;
	}
}
