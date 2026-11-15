using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class SubProjectViewModel : ProjectViewModel
{
	public SubProjectViewModel(string id, string parentId, string title, bool share, bool isNote)
	{
		string emojiIcon = EmojiHelper.GetEmojiIcon(title);
		if (!string.IsNullOrEmpty(emojiIcon) && title.StartsWith(emojiIcon))
		{
			base.Title = title.Remove(0, emojiIcon.Length);
			base.Emoji = emojiIcon;
		}
		else
		{
			base.Title = title;
			base.Icon = (isNote ? Utils.GetGeometry(share ? "Ic.ProjectList.NoteMember" : "Ic.ProjectList.Note") : Utils.GetGeometry(share ? "Ic.ProjectList.SharedList" : "Ic.ProjectList.List"));
		}
		base.Id = id;
		base.ParentId = parentId;
		base.IsSubItem = true;
		base.Type = "normal";
		base.IsNote = isNote;
		base.IsShare = share;
	}
}
