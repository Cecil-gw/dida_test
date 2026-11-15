using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class PinnedSection : Section
{
	public PinnedSection()
	{
		base.SectionId = "pinned";
		base.Name = Utils.GetString("Pinned");
		base.Ordinal = 0L;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		if (displayType != DisplayType.Task && displayType != DisplayType.Note)
		{
			return displayType == DisplayType.Agenda;
		}
		return true;
	}
}
