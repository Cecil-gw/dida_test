using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.ViewModels;

public class FilterListItemViewModel : BaseViewModel
{
	private string _dateText = string.Empty;

	private string _groupId;

	private string _icon;

	private bool _canSelect = true;

	private bool _isProjectGroup;

	private bool _isAssignee;

	private bool _showFeishu;

	private bool _isSecondLevel;

	private string _nDaysLaterValue;

	private string _nextNDaysValue;

	private string _projectId;

	private bool _selected;

	private bool _showIcon;

	private bool _isPriorityIcon;

	private long _sortOrder;

	private string _title;

	private object _value;

	private bool _unfold;

	private bool _isTeam;

	private bool _partSelected;

	private bool _isSplit;

	private string _imgaeUrl;

	private bool _isAllItem;

	private int _indentLevel;

	private bool _useCircleCheck;

	public bool IsTagParent { get; set; }

	public string Kind { get; set; }

	public List<FilterListItemViewModel> Children { get; set; }

	public bool Highlighted
	{
		get
		{
			if (!_partSelected || _unfold)
			{
				return _selected;
			}
			return true;
		}
	}

	public bool ShowFoldIcon
	{
		get
		{
			if (!IsProjectGroup)
			{
				return IsTeam;
			}
			return true;
		}
	}

	public int NDaysValue { get; set; }

	public FilterItemDisplayType DisplayType { get; set; }

	public string Emoji { get; set; } = string.Empty;

	public bool ShowEmoji => !string.IsNullOrEmpty(Emoji);

	public bool IsTeam
	{
		get
		{
			return _isTeam;
		}
		set
		{
			_isTeam = value;
			OnPropertyChanged("ShowFoldIcon");
		}
	}

	public bool IsSplit
	{
		get
		{
			return _isSplit;
		}
		set
		{
			_isSplit = value;
			OnPropertyChanged("IsSplit");
		}
	}

	public bool PartSelected
	{
		get
		{
			return _partSelected;
		}
		set
		{
			_partSelected = value;
			OnPropertyChanged("PartSelected");
			OnPropertyChanged("Highlighted");
		}
	}

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
			OnPropertyChanged("Highlighted");
			OnPropertyChanged("ShowNormalCheck");
			OnPropertyChanged("ShowCircleCheck");
		}
	}

	public object Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
			OnPropertyChanged("Value");
		}
	}

	public string ImageUrl
	{
		get
		{
			return _imgaeUrl;
		}
		set
		{
			_imgaeUrl = value;
			OnPropertyChanged("ImageUrl");
			SetAvatar();
		}
	}

	public BitmapImage Avatar { get; set; }

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public bool IsAllItem
	{
		get
		{
			return _isAllItem;
		}
		set
		{
			_isAllItem = value;
			OnPropertyChanged("IsAllItem");
		}
	}

	public bool UseCircleCheck
	{
		get
		{
			return _useCircleCheck;
		}
		set
		{
			_useCircleCheck = value;
			OnPropertyChanged("UseCircleCheck");
			OnPropertyChanged("ShowNormalCheck");
			OnPropertyChanged("ShowCircleCheck");
		}
	}

	public bool ShowNormalCheck
	{
		get
		{
			if (Selected)
			{
				return !UseCircleCheck;
			}
			return false;
		}
	}

	public bool ShowCircleCheck
	{
		get
		{
			if (Selected)
			{
				return UseCircleCheck;
			}
			return false;
		}
	}

	public int IndentLevel
	{
		get
		{
			return _indentLevel;
		}
		set
		{
			_indentLevel = value;
			OnPropertyChanged("IndentLevel");
		}
	}

	public string Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			_icon = value;
			OnPropertyChanged("Icon");
		}
	}

	public bool ShowIcon
	{
		get
		{
			return _showIcon;
		}
		set
		{
			_showIcon = value;
			OnPropertyChanged("ShowIcon");
		}
	}

	public bool IsPriorityIcon
	{
		get
		{
			return _isPriorityIcon;
		}
		set
		{
			_isPriorityIcon = value;
			OnPropertyChanged("IsPriorityIcon");
		}
	}

	public string ProjectId
	{
		get
		{
			return _projectId;
		}
		set
		{
			_projectId = value;
		}
	}

	public string GroupId
	{
		get
		{
			return _groupId;
		}
		set
		{
			_groupId = value;
		}
	}

	public bool IsSecondLevel
	{
		get
		{
			return _isSecondLevel;
		}
		set
		{
			_isSecondLevel = value;
			if (value && _indentLevel == 0)
			{
				IndentLevel = 1;
			}
			OnPropertyChanged("IsSecondLevel");
		}
	}

	public bool IsProjectGroup
	{
		get
		{
			return _isProjectGroup;
		}
		set
		{
			_isProjectGroup = value;
			OnPropertyChanged("ShowFoldIcon");
		}
	}

	public bool IsAssignee
	{
		get
		{
			return _isAssignee;
		}
		set
		{
			_isAssignee = value;
			OnPropertyChanged("IsAssignee");
		}
	}

	public bool ShowFeishu
	{
		get
		{
			return _showFeishu;
		}
		set
		{
			_showFeishu = value;
			OnPropertyChanged("ShowFeishu");
		}
	}

	public long SortOrder
	{
		get
		{
			return _sortOrder;
		}
		set
		{
			_sortOrder = value;
		}
	}

	public string DateText
	{
		get
		{
			return _dateText;
		}
		set
		{
			_dateText = value;
			OnPropertyChanged("DateText");
		}
	}

	public bool CanSelect
	{
		get
		{
			return _canSelect;
		}
		set
		{
			_canSelect = value;
			OnPropertyChanged("IsNormal");
		}
	}

	public string NextNDaysValue
	{
		get
		{
			return _nextNDaysValue;
		}
		set
		{
			_nextNDaysValue = value;
			OnPropertyChanged("NextNDaysValue");
		}
	}

	public string NDaysLaterValue
	{
		get
		{
			return _nDaysLaterValue;
		}
		set
		{
			_nDaysLaterValue = value;
			OnPropertyChanged("NDaysLaterValue");
		}
	}

	public bool Unfold
	{
		get
		{
			return _unfold;
		}
		set
		{
			_unfold = value;
			OnPropertyChanged("UnFold");
			OnPropertyChanged("Highlighted");
		}
	}

	public int? DaysFrom { get; set; }

	public int? DaysTo { get; set; }

	private async void SetAvatar()
	{
		Avatar = await AvatarHelper.GetAvatarByUrlAsync(_imgaeUrl);
		OnPropertyChanged("Avatar");
	}

	public FilterListItemViewModel()
	{
	}

	public FilterListItemViewModel(TagModel tag, ICollection<string> selectedTags)
	{
		string displayEmoji = tag.GetDisplayEmoji();
		if (!string.IsNullOrEmpty(displayEmoji))
		{
			Emoji = displayEmoji;
			Title = tag.GetDisplayNameWithoutEmoji();
		}
		else
		{
			Title = tag.GetDisplayName();
		}
		Value = tag.name;
		Icon = "Ic.ProjectList.Tag";
		ShowIcon = true;
		IsProjectGroup = tag.IsParent();
		IsSecondLevel = tag.IsChild();
		GroupId = tag.parent;
		IsTagParent = IsProjectGroup;
		Unfold = IsProjectGroup && !tag.collapsed;
		Selected = selectedTags.Contains(tag.name) || selectedTags.Contains(tag.parent);
	}

	public static FilterListItemViewModel BuildItem(SelectableItemViewModel model, List<string> projectIds, List<string> groupIds)
	{
		FilterListItemViewModel result = new FilterListItemViewModel();
		ticktick_WPF.Views.Misc.ProjectViewModel pm = model as ticktick_WPF.Views.Misc.ProjectViewModel;
		if (pm != null)
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == pm.Id);
			if (projectModel != null)
			{
				string text = EmojiHelper.GetEmojiIcon(projectModel.name);
				string title;
				if (!string.IsNullOrEmpty(text) && projectModel.name.StartsWith(text))
				{
					title = projectModel.name.Remove(0, text.Length);
				}
				else
				{
					text = string.Empty;
					title = projectModel.name;
				}
				return new FilterListItemViewModel
				{
					Emoji = text,
					Title = title,
					Icon = GetProjectIcon(projectModel),
					IsSecondLevel = (!string.IsNullOrEmpty(projectModel.groupId) && projectModel.groupId != "NONE"),
					SortOrder = projectModel.sortOrder,
					Value = projectModel.id,
					GroupId = projectModel.groupId,
					Selected = (projectIds.Contains(projectModel.id) || groupIds.Contains(projectModel.groupId)),
					ShowIcon = true
				};
			}
		}
		else
		{
			ticktick_WPF.Views.Misc.ProjectGroupViewModel pg = model as ticktick_WPF.Views.Misc.ProjectGroupViewModel;
			if (pg != null)
			{
				ProjectGroupModel projectGroupModel = CacheManager.GetProjectGroups().FirstOrDefault((ProjectGroupModel p) => p.id == pg.Id);
				if (projectGroupModel != null)
				{
					return new FilterListItemViewModel
					{
						Title = projectGroupModel.name,
						Icon = "Ic.ProjectList.ExpandFolder",
						SortOrder = projectGroupModel.sortOrder.GetValueOrDefault(),
						Value = projectGroupModel.id,
						Selected = groupIds.Contains(projectGroupModel.id),
						ShowIcon = true,
						IsProjectGroup = true,
						Unfold = projectGroupModel.open,
						Children = model.Children.Select((SelectableItemViewModel child) => BuildItem(child, projectIds, groupIds)).ToList()
					};
				}
			}
			else
			{
				if (model is TeamSectionViewModel teamSectionViewModel)
				{
					return new FilterListItemViewModel
					{
						Title = teamSectionViewModel.Title,
						Value = teamSectionViewModel.TeamId,
						Selected = false,
						ShowIcon = false,
						IsProjectGroup = false,
						CanSelect = false,
						IsTeam = true
					};
				}
				if (model is FilterCalendarViewModel filterCalendarViewModel)
				{
					return new FilterListItemViewModel
					{
						Title = filterCalendarViewModel.Title,
						Value = filterCalendarViewModel.Id,
						Selected = projectIds.Contains(filterCalendarViewModel.Id),
						ShowIcon = true,
						IsProjectGroup = false,
						CanSelect = true,
						Icon = "Ic.TaskOM.DateRemind"
					};
				}
			}
		}
		return result;
	}

	private static string GetProjectIcon(ProjectModel project)
	{
		if (project.IsNote)
		{
			if (!project.IsShareList())
			{
				return "Ic.ProjectList.Note";
			}
			return "Ic.ProjectList.NoteMember";
		}
		if (project.Isinbox)
		{
			return "Ic.ProjectList.Inbox";
		}
		if (project.IsShareList())
		{
			return "Ic.ProjectList.SharedList";
		}
		return "Ic.ProjectList.List";
	}

	public static FilterListItemViewModel BuildDateItem(string title, string value, string icon)
	{
		return new FilterListItemViewModel
		{
			Title = title,
			Value = value,
			ShowIcon = true,
			Icon = icon
		};
	}

	public static FilterListItemViewModel BuildDateItem(string title, string value, string dateText, string icon)
	{
		return new FilterListItemViewModel
		{
			Title = title,
			Value = value,
			ShowIcon = true,
			DateText = dateText,
			Icon = icon
		};
	}

	public void SetNDaysValue(int days)
	{
		NDaysValue = days;
		switch (DisplayType)
		{
		case FilterItemDisplayType.NextNDays:
			Value = $"{days}days";
			break;
		case FilterItemDisplayType.NDaysLater:
			Value = $"{days}dayslater";
			break;
		case FilterItemDisplayType.AfterNDays:
			if (days <= 0)
			{
				days = 7;
			}
			Value = $"{days}daysfromtoday";
			break;
		case FilterItemDisplayType.NDaysAgo:
			Value = $"-{days}daysfromtoday";
			break;
		}
	}
}
