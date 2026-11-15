using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class AssigneeSection : Section
{
	public string Assingee { get; set; }

	public override string GetAssignee()
	{
		return Assingee;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		if (displayType != DisplayType.Note && displayType != DisplayType.Task)
		{
			return displayType == DisplayType.Agenda;
		}
		return true;
	}

	public override bool CanSort(Constants.SortType sortType)
	{
		return true;
	}
}
