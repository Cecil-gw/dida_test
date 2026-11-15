using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.ViewModels;

[Serializable]
public class ProjectItemViewModel : BaseViewModel
{
	private int _count;

	private bool _countLoadRequested;

	private string _color;

	private bool _dragSelected;

	private bool _dropSelected;

	private bool _hover;

	private Geometry _icon;

	private Geometry _infoIcon;

	private string _info;

	private Brush _infoIconBrush = new SolidColorBrush();

	private bool _isSubItem;

	private bool _isTabSelected;

	private bool _open = true;

	private bool _selected;

	private bool _isSplitLine;

	private bool _isGroupItem;

	private bool _isPtfAll;

	private bool _isPtfItem;

	private bool _isEmptySubItem;

	private bool _isEmptyProject;

	private string _title = string.Empty;

	public ProjectListType ListType;

	public string _emojiText;

	public string Id { get; set; }

	public bool CanDrag { get; protected set; }

	public bool CanDrop { get; protected set; }

	public bool IsAddProject { get; set; }

	public long SortOrder { get; set; }

	public bool IsNormalItem
	{
		get
		{
			if (!IsEmptyProject && !IsSplitLine)
			{
				return !IsEmptySubItem;
			}
			return false;
		}
	}

	public bool Expired { get; set; }

	public string ViewMode { get; set; }

	public string ParallelViewMode { get; set; }

	public bool ShowCount { get; protected set; } = true;

	public string IconText { get; set; }

	public string NotifyMessage { get; set; }

	public string TeamId { get; set; }

	public bool IsProjectPermit { get; set; } = true;

	public bool IsEmptyProject
	{
		get
		{
			return _isEmptyProject;
		}
		set
		{
			_isEmptyProject = value;
			OnPropertyChanged("IsEmptyProject");
			OnPropertyChanged("ItemVisible");
		}
	}

	public Visibility ItemVisible
	{
		get
		{
			if (!DragSelected)
			{
				if (!IsNormalItem)
				{
					return Visibility.Collapsed;
				}
				return Visibility.Visible;
			}
			return Visibility.Hidden;
		}
	}

	public bool ShowIcon
	{
		get
		{
			if (!IsPtfAll)
			{
				if (!string.IsNullOrEmpty(_emojiText))
				{
					return !(_title?.StartsWith(_emojiText) ?? false);
				}
				return true;
			}
			return false;
		}
	}

	public bool ShowEmoji
	{
		get
		{
			if (!IsPtfAll && !string.IsNullOrEmpty(_emojiText))
			{
				return _title?.StartsWith(_emojiText) ?? false;
			}
			return false;
		}
	}

	public bool IsEmptySubItem
	{
		get
		{
			return _isEmptySubItem;
		}
		set
		{
			_isEmptySubItem = value;
			OnPropertyChanged("IsEmptySubItem");
			OnPropertyChanged("ItemVisible");
		}
	}

	public bool IsPtfItem
	{
		get
		{
			return _isPtfItem;
		}
		set
		{
			_isPtfItem = value;
			OnPropertyChanged("IsPtfItem");
		}
	}

	public bool IsGroupItem
	{
		get
		{
			return _isGroupItem;
		}
		set
		{
			_isGroupItem = value;
			OnPropertyChanged("IsGroupItem");
		}
	}

	public bool IsPtfAll
	{
		get
		{
			return _isPtfAll;
		}
		set
		{
			_isPtfAll = value;
			OnPropertyChanged("IsPtfAll");
		}
	}

	public string Color
	{
		get
		{
			if (!string.IsNullOrEmpty(_color))
			{
				return _color;
			}
			return "Transparent";
		}
		set
		{
			_color = value;
			OnPropertyChanged("Color");
			OnPropertyChanged("ColorNeedsContrastBorder");
		}
	}

	public bool ColorNeedsContrastBorder => ColorUtils.NeedsContrastBorder(Color, ThemeUtil.IsDark());

	public bool IsSplitLine
	{
		get
		{
			return _isSplitLine;
		}
		set
		{
			_isSplitLine = value;
			OnPropertyChanged("IsSplitLine");
			OnPropertyChanged("ItemVisible");
		}
	}

	public bool InSubSection { get; set; }

	public bool IsSubItem
	{
		get
		{
			return _isSubItem;
		}
		set
		{
			_isSubItem = value;
			OnPropertyChanged("Level");
		}
	}

	public int Level => (InSubSection ? 1 : 0) + (IsSubItem ? 1 : 0);

	public bool Open
	{
		get
		{
			return _open;
		}
		set
		{
			_open = value;
			OnPropertyChanged("Open");
		}
	}

	public List<ProjectItemViewModel> Children { get; set; } = new List<ProjectItemViewModel>();

	public string EmojiText
	{
		get
		{
			if (!ShowEmoji)
			{
				return string.Empty;
			}
			return _emojiText;
		}
	}

	public string TitleText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(_title))
			{
				return string.Empty;
			}
			if (ShowEmoji && _emojiText.Length <= _title.Length)
			{
				return _title.Substring(_emojiText.Length).Trim();
			}
			return _title.Trim();
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value ?? string.Empty;
			OnPropertyChanged("Title");
			OnPropertyChanged("ShowIcon");
			OnPropertyChanged("ShowEmoji");
			OnPropertyChanged("EmojiText");
			OnPropertyChanged("TitleText");
		}
	}

	public Geometry Icon
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

	public string Info
	{
		get
		{
			return _info;
		}
		set
		{
			_info = value;
			OnPropertyChanged("_info");
		}
	}

	public Geometry InfoIcon
	{
		get
		{
			return _infoIcon;
		}
		set
		{
			_infoIcon = value;
			OnPropertyChanged("InfoIcon");
		}
	}

	public Brush InfoIconBrush
	{
		get
		{
			return _infoIconBrush;
		}
		set
		{
			_infoIconBrush = value;
			OnPropertyChanged("InfoIconBrush");
		}
	}

	public int Count
	{
		get
		{
			if (_infoIcon != null)
			{
				return 0;
			}
			return _count;
		}
		set
		{
			if (_count != value)
			{
				_count = value;
				OnPropertyChanged("ShowCount");
				OnPropertyChanged("Count");
			}
		}
	}

	public bool Hover
	{
		get
		{
			return _hover;
		}
		set
		{
			_hover = value;
			OnPropertyChanged("EmptySubItemVisibility");
		}
	}

	public Visibility EmptySubItemVisibility
	{
		get
		{
			if (!Hover)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
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
		}
	}

	public bool DragSelected
	{
		get
		{
			return _dragSelected;
		}
		set
		{
			_dragSelected = value;
			OnPropertyChanged("DragSelected");
		}
	}

	public bool DropSelected
	{
		get
		{
			return _dropSelected;
		}
		set
		{
			_dropSelected = value;
			OnPropertyChanged("DropSelected");
		}
	}

	public bool IsTabSelected
	{
		get
		{
			return _isTabSelected;
		}
		set
		{
			_isTabSelected = value;
			OnPropertyChanged("IsTabSelected");
		}
	}

	public virtual string GetSaveIdentity()
	{
		return string.Empty;
	}

	public virtual async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		return null;
	}

	public virtual void LoadCount()
	{
		Count = 0;
	}

	public void EnsureCountLoaded()
	{
		if (!_countLoadRequested)
		{
			_countLoadRequested = true;
			LoadCount();
		}
	}

	public virtual ProjectIdentity GetIdentity()
	{
		return null;
	}

	public virtual PtfType GetPtfType()
	{
		return PtfType.Null;
	}

	public static ProjectItemViewModel BuildProject(ProjectIdentity identity)
	{
		if (!(identity is NormalProjectIdentity))
		{
			if (identity is SmartProjectIdentity smartProjectIdentity)
			{
				return SmartProjectViewModel.BuildModel(SpecialListUtils.GetSmartTypeById(smartProjectIdentity.Id));
			}
			if (!(identity is GroupProjectIdentity))
			{
				if (!(identity is FilterProjectIdentity))
				{
					if (!(identity is TagProjectIdentity))
					{
						if (identity is DateProjectIdentity dateProjectIdentity)
						{
							return new DateProjectViewModel(dateProjectIdentity.DateStamp);
						}
						if (identity is SubscribeCalendarProjectIdentity subscribeCalendarProjectIdentity)
						{
							return new SubscribeCalendarProjectViewModel(subscribeCalendarProjectIdentity.Profile);
						}
						if (identity is BindAccountCalendarProjectIdentity bindAccountCalendarProjectIdentity)
						{
							return new BindCalendarAccountProjectViewModel(bindAccountCalendarProjectIdentity.Account);
						}
					}
					else
					{
						TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel t) => t.name == identity.Id.ToLower());
						if (tagModel != null)
						{
							return new TagProjectViewModel(tagModel);
						}
					}
				}
				else
				{
					FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == identity.Id);
					if (filterModel != null)
					{
						return new FilterProjectViewModel(filterModel);
					}
				}
			}
			else
			{
				ProjectGroupModel projectGroupModel = CacheManager.GetProjectGroups().FirstOrDefault((ProjectGroupModel p) => p.id == identity.QueryId);
				if (projectGroupModel != null)
				{
					return new ProjectGroupViewModel(projectGroupModel);
				}
			}
		}
		else
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == identity.Id);
			if (projectModel != null)
			{
				return new NormalProjectViewModel(projectModel);
			}
		}
		return new InboxProjectViewModel(new InboxProject());
	}

	public static ProjectItemViewModel BuildProject(string saveId)
	{
		if (!string.IsNullOrEmpty(saveId) && saveId.Contains(":"))
		{
			string[] array = saveId.Split(':');
			if (array.Length == 2)
			{
				string text = array[0];
				string identity = array[1];
				switch (text)
				{
				case "project":
				{
					ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == identity);
					if (projectModel != null)
					{
						return new NormalProjectViewModel(projectModel);
					}
					break;
				}
				case "smart":
					return SmartProjectViewModel.BuildModel(SpecialListUtils.GetSmartTypeById(identity));
				case "group":
				{
					ProjectGroupModel projectGroupModel = CacheManager.GetProjectGroups().FirstOrDefault((ProjectGroupModel p) => p.id == identity);
					if (projectGroupModel != null)
					{
						return new ProjectGroupViewModel(projectGroupModel);
					}
					break;
				}
				case "filter":
				{
					FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == identity);
					if (filterModel != null)
					{
						return new FilterProjectViewModel(filterModel);
					}
					break;
				}
				case "tag":
				{
					string tagName = identity.ToLower();
					List<TagModel> tags = CacheManager.GetTags();
					TagModel tagModel = tags.FirstOrDefault((TagModel t) => t.name == tagName) ?? tags.FirstOrDefault((TagModel t) => t.label == identity);
					if (tagModel != null)
					{
						return new TagProjectViewModel(tagModel);
					}
					break;
				}
				case "subscribe_calendar":
				{
					CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel c) => c.Id == identity);
					if (calendarSubscribeProfileModel != null)
					{
						return new SubscribeCalendarProjectViewModel(calendarSubscribeProfileModel);
					}
					break;
				}
				case "bind_account":
				{
					BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel account) => account.Id == identity);
					if (bindCalendarAccountModel != null)
					{
						return new BindCalendarAccountProjectViewModel(bindCalendarAccountModel);
					}
					break;
				}
				}
			}
		}
		if (LocalSettings.Settings.SmartListInbox == 1)
		{
			ProjectModel projectModel2 = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == TaskDefaultDao.GetDefaultSafely().ProjectId);
			if (projectModel2 != null)
			{
				return new NormalProjectViewModel(projectModel2);
			}
		}
		return new InboxProjectViewModel(new InboxProject());
	}
}
