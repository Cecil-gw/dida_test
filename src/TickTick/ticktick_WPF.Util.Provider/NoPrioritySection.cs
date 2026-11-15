using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class NoPrioritySection : PrioritySection
{
	public NoPrioritySection()
	{
		base.SectionId = "noPriority";
		base.Name = Utils.GetString("PriorityNull");
		base.Ordinal = 0L;
		base.SectionEntityId = "0";
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}

	public override int GetPriority()
	{
		return 0;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return displayType != DisplayType.Habit;
	}
}
