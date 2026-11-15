using System;
using System.Globalization;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class TodaySection : Section
{
	private readonly bool _createTimeGroup;

	public TodaySection(bool createTimeGroup = false)
	{
		_createTimeGroup = createTimeGroup;
		base.Name = Utils.GetString("Today");
		if (createTimeGroup)
		{
			base.Name = Utils.GetString("Today") + ", " + Utils.GetString(DateTime.Today.DayOfWeek.ToString().Substring(0, 3));
		}
		base.Ordinal = 2L;
		base.SectionDate = DateTime.Today;
		base.SectionEntityId = DateTime.Today.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
		if (createTimeGroup)
		{
			base.SectionEntityId = DateUtils.GetCreateTimeSectionEntityId(DateUtils.CreateTimeCategory.Today);
		}
		base.SectionId = "today";
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return displayType != DisplayType.Habit;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}

	public override DateTime? GetStartDate()
	{
		if (!_createTimeGroup)
		{
			return base.GetStartDate();
		}
		return null;
	}
}
