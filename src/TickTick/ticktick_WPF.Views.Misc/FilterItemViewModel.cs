using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Views.Misc;

public class FilterItemViewModel : SelectableItemViewModel
{
	public FilterItemViewModel(string id, string title)
	{
		base.Id = id;
		base.Title = title;
		base.Type = "filter";
		base.CanMultiSelect = false;
		string emojiIcon = EmojiHelper.GetEmojiIcon(title);
		if (!string.IsNullOrEmpty(emojiIcon) && title.StartsWith(emojiIcon))
		{
			base.Title = title.Remove(0, emojiIcon.Length);
			base.Emoji = emojiIcon;
		}
		else
		{
			base.Title = title;
			base.Icon = Utils.GetGeometry("Ic.ProjectList.Filter");
		}
	}

	public override ProjectIdentity ToIdentity()
	{
		FilterModel filterById = CacheManager.GetFilterById(base.Id);
		if (filterById != null && filterById.deleted != 1)
		{
			return new FilterProjectIdentity(filterById);
		}
		return null;
	}
}
