using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectPinItemViewModel : BaseViewModel
{
	public string EntityId;

	public int Type;

	public long SortOrder;

	private Geometry _icon;

	private string _emoji;

	private string _color;

	private string _title;

	private bool _selected;

	private bool _isMore;

	private bool _dragging;

	private double _itemWidth = 46.0;

	public bool IsBindAccount { get; set; }

	public string SyncEntityId
	{
		get
		{
			if (Type == 11)
			{
				return EntityId?.Replace("_special_id_", "");
			}
			return EntityId;
		}
	}

	public string DisplayTitle
	{
		get
		{
			string text = Title.Substring(0, Math.Min(Title.Length, 9));
			double width = Utils.MeasureString(text, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 10.0).Width;
			if (width < 42.0)
			{
				string text2 = text;
				while (width < 42.0 && text.Length < Title.Length)
				{
					text = text2;
					if (text2.Length == Title.Length)
					{
						break;
					}
					text2 = Title.Substring(0, text2.Length + 1);
					width = Utils.MeasureString(text2, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 10.0).Width;
				}
			}
			else
			{
				while (width > 42.0 && text.Length > 0)
				{
					text = text.Substring(0, text.Length - 1);
					width = Utils.MeasureString(text, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 10.0).Width;
				}
			}
			return text;
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

	public string Emoji
	{
		get
		{
			return _emoji;
		}
		set
		{
			_emoji = value;
			OnPropertyChanged("Emoji");
		}
	}

	public string Color
	{
		get
		{
			return _color;
		}
		set
		{
			_color = value;
			OnPropertyChanged("Color");
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
			_title = value;
			OnPropertyChanged("Title");
			OnPropertyChanged("DisplayTitle");
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

	public bool IsMore
	{
		get
		{
			return _isMore;
		}
		set
		{
			_isMore = value;
			OnPropertyChanged("IsMore");
		}
	}

	public bool Dragging
	{
		get
		{
			return _dragging;
		}
		set
		{
			_dragging = value;
			OnPropertyChanged("Dragging");
		}
	}

	public double ItemWidth
	{
		get
		{
			return _itemWidth;
		}
		set
		{
			_itemWidth = value;
			OnPropertyChanged("ItemWidth");
		}
	}

	public SolidColorBrush IconBackground
	{
		get
		{
			string themeId = LocalSettings.Settings.ThemeId;
			if (themeId == "White" || themeId == "Blue")
			{
				string text = Color;
				if (string.IsNullOrEmpty(text) || text == "transparent")
				{
					text = (Application.Current?.FindResource("ColorPrimary") as Color?)?.ToString() ?? "#000000";
				}
				return ThemeUtil.GetAlphaColor(text, 10);
			}
			if (ThemeKey.IsDarkTheme(themeId))
			{
				return ThemeUtil.GetAlphaColor("#FFFFFF", 8);
			}
			if (LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card)
			{
				return ThemeUtil.GetAlphaColor((Application.Current?.FindResource("ColorPrimary") as Color?)?.ToString() ?? "#000000", 10);
			}
			return ThemeUtil.GetAlphaColor("#FFFFFF", 80);
		}
	}

	public SolidColorBrush IconColor
	{
		get
		{
			string text = Color;
			if (string.IsNullOrEmpty(text) || text == "transparent")
			{
				text = (Application.Current?.FindResource("ColorPrimary") as Color?)?.ToString() ?? "#66000000";
			}
			return ThemeUtil.GetColorInString(text);
		}
	}

	public ProjectPinItemViewModel()
	{
	}

	public ProjectPinItemViewModel(SyncSortOrderModel pinSortOrderModel, ProjectModel project)
	{
		EntityId = project.id;
		Type = pinSortOrderModel.Type;
		SortOrder = pinSortOrderModel.SortOrder;
		Color = project.color;
		string emojiIcon = EmojiHelper.GetEmojiIcon(project.name);
		if (!string.IsNullOrEmpty(emojiIcon) && project.name.StartsWith(emojiIcon))
		{
			Emoji = emojiIcon;
			Title = project.name.Substring(emojiIcon.Length);
		}
		else
		{
			Icon = (project.IsNote ? Utils.GetGeometry(project.IsShareList() ? "Ic.ProjectList.NoteMember" : "Ic.ProjectList.Note") : Utils.GetGeometry(project.IsShareList() ? "Ic.ProjectList.SharedList" : "Ic.ProjectList.List"));
			Title = project.name;
		}
	}

	public ProjectPinItemViewModel(SyncSortOrderModel pinSortOrderModel, ProjectGroupModel group)
	{
		EntityId = group.id;
		Type = pinSortOrderModel.Type;
		SortOrder = pinSortOrderModel.SortOrder;
		string emojiIcon = EmojiHelper.GetEmojiIcon(group.name);
		if (!string.IsNullOrEmpty(emojiIcon) && group.name.StartsWith(emojiIcon))
		{
			Emoji = emojiIcon;
			Title = group.name.Substring(emojiIcon.Length);
		}
		else
		{
			Icon = Utils.GetGeometry("Ic.ProjectList.Folder");
			Title = group.name;
		}
	}

	public ProjectPinItemViewModel(SyncSortOrderModel pinSortOrderModel, TagModel tag)
	{
		EntityId = tag.name;
		Type = pinSortOrderModel.Type;
		SortOrder = pinSortOrderModel.SortOrder;
		Color = tag.color;
		string displayEmoji = tag.GetDisplayEmoji();
		if (!string.IsNullOrEmpty(displayEmoji))
		{
			Emoji = displayEmoji;
			Title = tag.GetDisplayNameWithoutEmoji();
		}
		else
		{
			Icon = Utils.GetGeometry("Ic.ProjectList.Tag");
			Title = tag.GetDisplayName();
		}
	}

	public ProjectPinItemViewModel(SyncSortOrderModel pinSortOrderModel, FilterModel filter)
	{
		EntityId = filter.id;
		Type = pinSortOrderModel.Type;
		SortOrder = pinSortOrderModel.SortOrder;
		string emojiIcon = EmojiHelper.GetEmojiIcon(filter.name);
		if (!string.IsNullOrEmpty(emojiIcon) && filter.name.StartsWith(emojiIcon))
		{
			Emoji = emojiIcon;
			Title = filter.name.Substring(emojiIcon.Length);
		}
		else
		{
			Icon = Utils.GetGeometry("Ic.ProjectList.Filter");
			Title = filter.name;
		}
	}

	public ProjectPinItemViewModel(SyncSortOrderModel pinSortOrderModel, CalendarSubscribeProfileModel subscribe)
	{
		EntityId = pinSortOrderModel.EntityId;
		Type = pinSortOrderModel.Type;
		Color = subscribe.Color;
		SortOrder = pinSortOrderModel.SortOrder;
		Icon = Utils.GetGeometry("Ic.ProjectList.Url");
		Title = subscribe.Name;
	}

	public ProjectPinItemViewModel(SyncSortOrderModel pinSortOrderModel, BindCalendarAccountModel account)
	{
		EntityId = pinSortOrderModel.EntityId;
		Type = pinSortOrderModel.Type;
		SortOrder = pinSortOrderModel.SortOrder;
		Title = account.DisplayName;
		if (account.Site == "feishu")
		{
			Title = Utils.GetString("FeishuCalendar");
		}
		Icon = SubscribeCalendarHelper.GetCalendarProjectIcon(account);
		IsBindAccount = true;
	}

	public ProjectPinItemViewModel(SyncSortOrderModel pinSortOrderModel, SmartProject smart)
	{
		EntityId = smart.Id;
		Type = pinSortOrderModel.Type;
		SortOrder = pinSortOrderModel.SortOrder;
		Title = smart.Name;
		Icon = smart.Icon;
	}

	public static List<ProjectPinItemViewModel> BuildModels(List<SyncSortOrderModel> pinnedModel)
	{
		List<ProjectPinItemViewModel> list = new List<ProjectPinItemViewModel>();
		ConcurrentDictionary<string, ProjectModel> projectDict = CacheManager.GetProjectDict();
		ConcurrentDictionary<string, ProjectGroupModel> groupDict = CacheManager.GetGroupDict();
		ConcurrentDictionary<string, TagModel> tagDict = CacheManager.GetTagDict();
		ConcurrentDictionary<string, FilterModel> filterDict = CacheManager.GetFilterDict();
		ConcurrentDictionary<string, CalendarSubscribeProfileModel> subscribeDict = CacheManager.GetSubscribeDict();
		ConcurrentDictionary<string, BindCalendarAccountModel> accountCalDict = CacheManager.GetAccountCalDict();
		foreach (SyncSortOrderModel item in pinnedModel)
		{
			if (string.IsNullOrEmpty(item.EntityId))
			{
				continue;
			}
			switch (item.Type)
			{
			case 5:
				if (projectDict.ContainsKey(item.EntityId))
				{
					ProjectModel projectModel = projectDict[item.EntityId];
					if (!projectModel.delete_status)
					{
						list.Add(new ProjectPinItemViewModel(item, projectModel));
					}
				}
				break;
			case 6:
				if (groupDict.ContainsKey(item.EntityId))
				{
					ProjectGroupModel projectGroupModel = groupDict[item.EntityId];
					if (projectGroupModel.deleted == 0)
					{
						list.Add(new ProjectPinItemViewModel(item, projectGroupModel));
					}
				}
				break;
			case 7:
				if (LocalSettings.Settings.SmartListTag != 1 && tagDict.ContainsKey(item.EntityId))
				{
					TagModel tagModel = tagDict[item.EntityId];
					if (tagModel.deleted == 0)
					{
						list.Add(new ProjectPinItemViewModel(item, tagModel));
					}
				}
				break;
			case 8:
				if (LocalSettings.Settings.ShowCustomSmartList == 0 && filterDict.ContainsKey(item.EntityId))
				{
					FilterModel filterModel = filterDict[item.EntityId];
					if (filterModel.deleted == 0)
					{
						list.Add(new ProjectPinItemViewModel(item, filterModel));
					}
				}
				break;
			case 9:
				if (subscribeDict.ContainsKey(item.EntityId))
				{
					CalendarSubscribeProfileModel calendarSubscribeProfileModel = subscribeDict[item.EntityId];
					if (calendarSubscribeProfileModel.Show != "hidden")
					{
						list.Add(new ProjectPinItemViewModel(item, calendarSubscribeProfileModel));
					}
				}
				else if (accountCalDict.ContainsKey(item.EntityId))
				{
					BindCalendarAccountModel account = accountCalDict[item.EntityId];
					list.Add(new ProjectPinItemViewModel(item, account));
				}
				break;
			case 11:
			{
				SmartProject smartProject = null;
				switch (item.EntityId)
				{
				case "completed":
					if (LocalSettings.Settings.SmartListComplete != 1)
					{
						smartProject = new CompletedProject();
					}
					break;
				case "abandoned":
					if (LocalSettings.Settings.SmartListAbandoned != 1)
					{
						smartProject = new AbandonedProject();
					}
					break;
				case "trash":
					if (LocalSettings.Settings.SmartListTrash != 1)
					{
						smartProject = new TrashProject();
					}
					break;
				}
				if (smartProject != null)
				{
					list.Add(new ProjectPinItemViewModel(item, smartProject));
				}
				break;
			}
			}
		}
		return list;
	}
}
