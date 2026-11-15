using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Views.Misc;

public class TagViewModel : SelectableItemViewModel
{
	public TagViewModel(TagModel tag, bool showIndent = false, bool inCal = false)
	{
		base.Id = tag.name;
		string displayEmoji = tag.GetDisplayEmoji();
		if (!string.IsNullOrEmpty(displayEmoji))
		{
			base.Title = tag.GetDisplayNameWithoutEmoji();
			base.Emoji = displayEmoji;
		}
		else
		{
			base.Title = tag.GetDisplayName();
			base.Icon = Utils.GetGeometry("Ic.ProjectList.Tag");
		}
		base.ShowIndent = showIndent;
		base.IsSubItem = tag.IsChild();
		base.IsParent = tag.IsParent();
		base.Open = !tag.collapsed;
		base.Type = "tag";
		base.ParentId = tag.parent;
		base.InCalFilter = inCal;
	}

	public override ProjectIdentity ToIdentity()
	{
		TagModel tagByName = CacheManager.GetTagByName(base.Id);
		if (tagByName == null)
		{
			return null;
		}
		return new TagProjectIdentity(tagByName);
	}
}
