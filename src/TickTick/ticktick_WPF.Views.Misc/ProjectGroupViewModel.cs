using System.Collections.Generic;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Views.Misc;

public class ProjectGroupViewModel : SelectableItemViewModel
{
	public ProjectGroupViewModel(ProjectGroupModel model)
	{
		base.Id = model.id;
		base.Title = model.name;
		string emojiIcon = EmojiHelper.GetEmojiIcon(model.name);
		if (!string.IsNullOrEmpty(emojiIcon) && model.name.StartsWith(emojiIcon))
		{
			base.Title = model.name.Remove(0, emojiIcon.Length);
			base.Emoji = emojiIcon;
		}
		else
		{
			base.Title = model.name;
			base.Icon = Utils.GetGeometry(model.open ? "Ic.ProjectList.ExpandFolder" : "Ic.ProjectList.Folder");
		}
		base.Type = "group";
		base.IsParent = true;
		base.Open = model.open;
	}

	protected override void SetOpenIcon()
	{
		if (string.IsNullOrEmpty(base.Emoji))
		{
			base.Icon = Utils.GetGeometry(base.Open ? "Ic.ProjectList.ExpandFolder" : "Ic.ProjectList.Folder");
		}
	}

	public override ProjectIdentity ToIdentity()
	{
		ProjectGroupModel groupById = CacheManager.GetGroupById(base.Id);
		if (groupById != null && groupById.deleted != 1)
		{
			List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(base.Id);
			return new GroupProjectIdentity(groupById, projectsInGroup);
		}
		return null;
	}
}
