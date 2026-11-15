using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class MediumPrioritySection : PrioritySection
{
	public MediumPrioritySection()
	{
		base.SectionId = "mediumPriority";
		base.Name = Utils.GetString("PriorityMedium");
		base.Ordinal = 3L;
		base.SectionEntityId = "3";
	}

	public override int GetPriority()
	{
		return 3;
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
