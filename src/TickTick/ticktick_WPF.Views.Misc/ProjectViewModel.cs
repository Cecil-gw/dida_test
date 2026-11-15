using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Views.Misc;

public class ProjectViewModel : SelectableItemViewModel
{
	protected ProjectViewModel()
	{
	}

	public ProjectViewModel(string id, string title, bool share, bool isNote)
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
		base.Type = "normal";
		base.IsNote = isNote;
		base.IsShare = share;
	}

	public override ProjectIdentity ToIdentity()
	{
		ProjectModel projectById = CacheManager.GetProjectById(base.Id);
		if (projectById == null)
		{
			if (base.Id.Contains("inbox"))
			{
				return new NormalProjectIdentity(new ProjectModel
				{
					id = Utils.GetInboxId(),
					name = Utils.GetString("Inbox")
				});
			}
		}
		else
		{
			if (projectById.closed.HasValue && projectById.closed.Value)
			{
				return new ClosedProjectIdentity(projectById.id);
			}
			if (!projectById.delete_status)
			{
				return new NormalProjectIdentity(projectById);
			}
		}
		return null;
	}
}
