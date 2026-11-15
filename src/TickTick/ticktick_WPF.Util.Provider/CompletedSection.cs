using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class CompletedSection : Section
{
	public CompletedSection()
	{
		base.Name = Utils.GetString("SectionCompletedAndAbandoned");
		base.Ordinal = -2147483648L;
		base.SectionId = "completed";
	}

	public void SetCompletedOnly()
	{
		base.Name = Utils.GetString("Completed");
	}

	public void SetAbandonedOnly()
	{
		base.Name = Utils.GetString("Abandoned");
	}

	public override int GetTaskStatus()
	{
		return 2;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		if (displayType == DisplayType.Task || displayType == DisplayType.CheckItem || displayType == DisplayType.Agenda)
		{
			return true;
		}
		return false;
	}

	public override bool CanSort(Constants.SortType sortType)
	{
		return false;
	}
}
