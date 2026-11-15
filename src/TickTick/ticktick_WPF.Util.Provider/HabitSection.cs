using System;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class HabitSection : Section
{
	public HabitSection(bool useInWeek = false, bool sortAsDate = false)
	{
		if (useInWeek)
		{
			if (Utils.IsCn())
			{
				base.Name = Utils.GetString("TodayHabit") + ", " + DateUtils.FormatWeekDayName(DateTime.Today);
			}
			else
			{
				base.Name = DateUtils.FormatWeekDayName(DateTime.Today) + ", " + Utils.GetString("TodayHabit");
			}
		}
		else
		{
			base.Name = Utils.GetString("TodayHabit");
		}
		base.Ordinal = 24L;
		base.SectionId = "habits";
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return false;
	}

	public override bool CanSort(Constants.SortType sortType)
	{
		return false;
	}
}
