using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class EmptySubViewModel : ProjectItemViewModel
{
	public readonly ProjectGroupModel ProjectGroup;

	public readonly bool IsEmptyTag;

	public readonly string ParentId;

	public EmptySubViewModel(ProjectGroupModel group)
	{
		base.Id = group.id + "empty";
		ProjectGroup = group;
		ParentId = group.id;
		base.TeamId = group.teamId;
		base.IsEmptySubItem = true;
	}

	public EmptySubViewModel(TagModel tag)
	{
		base.Id = tag.id + "empty";
		IsEmptyTag = true;
		ParentId = tag.name;
		base.IsEmptySubItem = true;
	}
}
