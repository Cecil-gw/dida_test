using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class NoteSection : Section
{
	public NoteSection()
	{
		base.Name = Utils.GetString("Notes");
		base.Ordinal = 2L;
		base.SectionId = "note";
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		if (displayType == DisplayType.Note)
		{
			return true;
		}
		return false;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return false;
	}
}
