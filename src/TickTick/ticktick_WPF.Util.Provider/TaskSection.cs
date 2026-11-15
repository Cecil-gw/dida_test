using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class TaskSection : Section
{
	public TaskSection()
	{
		base.Name = Utils.GetString("Task");
		base.Ordinal = 2L;
		base.SectionId = "task";
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		if (displayType != DisplayType.Task)
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
