using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class HighPrioritySection : PrioritySection
{
	public HighPrioritySection()
	{
		base.SectionId = "highPriority";
		base.Name = Utils.GetString("PriorityHigh");
		base.Ordinal = 5L;
		base.SectionEntityId = "5";
	}

	public override int GetPriority()
	{
		return 5;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return displayType != DisplayType.Habit;
	}
}
