using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class LowPrioritySection : PrioritySection
{
	public LowPrioritySection()
	{
		base.SectionId = "lowPriority";
		base.Name = Utils.GetString("PriorityLow");
		base.Ordinal = 1L;
		base.SectionEntityId = "1";
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}

	public override int GetPriority()
	{
		return 1;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return displayType != DisplayType.Habit;
	}
}
