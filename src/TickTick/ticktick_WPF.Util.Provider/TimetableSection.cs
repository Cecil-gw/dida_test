using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class TimetableSection : Section
{
	public TimetableSection()
	{
		base.Name = Utils.GetString("Timetable");
		base.Ordinal = 20L;
		base.SectionId = "timeTable";
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return false;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return false;
	}
}
