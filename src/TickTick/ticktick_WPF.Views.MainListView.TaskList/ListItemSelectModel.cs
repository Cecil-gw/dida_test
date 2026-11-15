using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.TaskList;

public class ListItemSelectModel
{
	public string Id;

	public string ChildId;

	public DisplayType ItemType;

	public TaskSelectType Type;

	public ListItemSelectModel(string id, string childId, DisplayType itemType, TaskSelectType type)
	{
		Id = id;
		ChildId = childId;
		ItemType = itemType;
		Type = type;
	}
}
