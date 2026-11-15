using System;
using System.Globalization;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class TomorrowSection : Section
{
	public TomorrowSection()
	{
		base.Name = Utils.GetString("Tomorrow");
		base.Ordinal = 2L;
		base.SectionDate = DateTime.Now.AddDays(1.0).Date;
		base.SectionId = "tomorrow";
		base.SectionEntityId = DateTime.Today.AddDays(1.0).ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return displayType != DisplayType.Habit;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}
}
