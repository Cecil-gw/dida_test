using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class NoAssingeeSection : AssigneeSection
{
	public NoAssingeeSection()
	{
		base.Name = Utils.GetString("NotAssigned");
		base.Ordinal = 9223372036854775804L;
		base.Assingee = "-1";
		base.SectionId = "-1";
		base.SectionEntityId = "-1";
	}

	public override string GetAssignee()
	{
		return "-1";
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		if (displayType != DisplayType.Note && displayType != DisplayType.Task)
		{
			return displayType == DisplayType.Agenda;
		}
		return true;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return true;
	}
}
