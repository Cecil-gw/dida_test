using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class UnpinnedSection : Section
{
	public UnpinnedSection()
	{
		base.SectionId = "NotPinned";
		base.Name = Utils.GetString("NotPinned");
		base.Ordinal = 1L;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return true;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}
}
