using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Search;

public class SearchTagAndProjectModel : SearchItemBaseModel
{
	public bool IsIcon { get; set; } = true;

	public bool IsTag { get; set; }

	public bool IsFilter { get; set; }

	public bool IsGroup { get; set; }

	public bool IsTaskGroup { get; set; }

	public bool IsProject { get; set; }

	public Geometry IconData { get; set; }

	public long SortOrder { get; set; }

	public string Name { get; set; }

	public string Emoji { get; set; } = string.Empty;

	public Pinyin Pinyin { get; set; }

	public bool IsInbox { get; set; }

	public sealed override string Id { get; set; }

	public override bool CanSelect { get; set; } = true;

	public long? ParentSortOrder { get; set; }

	public bool TeamProject { get; set; }

	public string ProjectId { get; set; }

	public string ProjectName { get; set; }

	public SearchTagAndProjectModel(ProjectGroupModel projectGroup)
	{
		IsGroup = true;
		string emojiIcon = EmojiHelper.GetEmojiIcon(projectGroup.name);
		if (!string.IsNullOrEmpty(emojiIcon) && projectGroup.name.StartsWith(emojiIcon))
		{
			IsIcon = false;
			Name = projectGroup.name.Remove(0, emojiIcon.Length);
			Emoji = emojiIcon;
		}
		else
		{
			IconData = Utils.GetGeometry("Ic.ProjectList.Folder");
			Name = projectGroup.name;
			Emoji = string.Empty;
		}
		SortOrder = projectGroup.sortOrder.GetValueOrDefault();
		Id = projectGroup.id;
		Pinyin = PinyinUtils.ToPinyin(projectGroup.name);
		IsInbox = false;
		TeamProject = false;
	}

	public SearchTagAndProjectModel(ProjectModel project)
	{
		IsProject = true;
		string emojiIcon = EmojiHelper.GetEmojiIcon(project.name);
		if (!string.IsNullOrEmpty(emojiIcon) && project.name.StartsWith(emojiIcon))
		{
			IsIcon = false;
			Name = project.name.Remove(0, emojiIcon.Length);
			Emoji = emojiIcon;
		}
		else
		{
			IconData = (project.IsNote ? Utils.GetGeometry(project.IsShareList() ? "Ic.ProjectList.NoteMember" : "Ic.ProjectList.Note") : Utils.GetGeometry(project.IsShareList() ? "Ic.ProjectList.SharedList" : "Ic.ProjectList.List"));
			Name = project.name;
			Emoji = string.Empty;
		}
		SortOrder = project.sortOrder;
		Id = project.id;
		Pinyin = PinyinUtils.ToPinyin(project.name);
		IsInbox = project.Isinbox;
		TeamProject = !string.IsNullOrEmpty(project.teamId);
	}

	public SearchTagAndProjectModel(ColumnModel column, ProjectModel project)
	{
		IsTaskGroup = true;
		IconData = Utils.GetGeometry("Ic.TaskOM.Group");
		Name = column.name;
		SortOrder = column.sortOrder.GetValueOrDefault();
		Id = column.id;
		Pinyin = PinyinUtils.ToPinyin(column.name);
		ProjectId = project.id;
		ProjectName = project.name;
		ParentSortOrder = project.sortOrder;
		IsInbox = project.Isinbox;
		TeamProject = !string.IsNullOrEmpty(project.teamId);
	}

	public void UpdateGroupSortOrder()
	{
		if (!IsProject)
		{
			return;
		}
		ProjectModel projectById = CacheManager.GetProjectById(Id);
		if (projectById != null)
		{
			ProjectGroupModel groupById = CacheManager.GetGroupById(projectById.groupId);
			if (groupById != null)
			{
				ParentSortOrder = groupById.sortOrder;
			}
			else
			{
				ParentSortOrder = null;
			}
		}
	}

	public SearchTagAndProjectModel(FilterModel filter)
	{
		IsFilter = true;
		string emojiIcon = EmojiHelper.GetEmojiIcon(filter.name);
		if (!string.IsNullOrEmpty(emojiIcon) && filter.name.StartsWith(emojiIcon))
		{
			IsIcon = false;
			Name = filter.name.Remove(0, emojiIcon.Length);
			Emoji = emojiIcon;
		}
		else
		{
			IconData = Utils.GetGeometry("Ic.ProjectList.Filter");
			Name = filter.name;
			Emoji = string.Empty;
		}
		SortOrder = filter.sortOrder;
		Id = filter.id;
		Pinyin = PinyinUtils.ToPinyin(filter.name);
	}

	public SearchTagAndProjectModel(TagModel tag)
	{
		IsTag = true;
		string displayEmoji = tag.GetDisplayEmoji();
		if (!string.IsNullOrEmpty(displayEmoji))
		{
			IsIcon = false;
			Name = tag.GetDisplayNameWithoutEmoji();
			Emoji = displayEmoji;
		}
		else
		{
			IconData = Utils.GetGeometry("Ic.ProjectList.Tag");
			Name = tag.GetDisplayName();
			Emoji = string.Empty;
		}
		SortOrder = tag.sortOrder;
		Id = tag.name;
		Pinyin = PinyinUtils.ToPinyin(tag.name);
	}

	public bool Contains(string text)
	{
		return Pinyin.Contains(text);
	}
}
