using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Calendar;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.CountDown.ViewModels;

namespace ticktick_WPF.ViewModels;

public class CalendarColorConfigViewModel : INotifyPropertyChanged
{
	public enum CalColorGroupType
	{
		Task,
		Habit,
		Countdown
	}

	public enum ChangedType
	{
		Mode,
		FixedColor,
		ItemColor
	}

	private bool _isShowGroupTitle;

	private string _displayWayText;

	private string _fixedColorText;

	private string _tipText;

	private bool _isShowTip;

	private bool _isShowFixColor;

	private bool _isShowItems;

	private string _fixedColorStr = "#FFAE58";

	public Action<CalColorGroupType> OnGroupChange { get; set; }

	public Action<int> OnModeChange { get; set; }

	public bool IsShowGroupTitle
	{
		get
		{
			return _isShowGroupTitle;
		}
		set
		{
			_isShowGroupTitle = value;
			NotifyPropertyChanged("IsShowGroupTitle");
		}
	}

	public string DisplayWayText
	{
		get
		{
			return _displayWayText;
		}
		set
		{
			_displayWayText = value;
			NotifyPropertyChanged("DisplayWayText");
		}
	}

	public string FixedColorText
	{
		get
		{
			return _fixedColorText;
		}
		set
		{
			_fixedColorText = value;
			NotifyPropertyChanged("FixedColorText");
		}
	}

	public string TipText
	{
		get
		{
			return _tipText;
		}
		set
		{
			_tipText = value;
			NotifyPropertyChanged("TipText");
		}
	}

	public bool IsShowTip
	{
		get
		{
			return _isShowTip;
		}
		set
		{
			_isShowTip = value;
			NotifyPropertyChanged("IsShowTip");
		}
	}

	public bool IsShowFixColor
	{
		get
		{
			return _isShowFixColor;
		}
		set
		{
			_isShowFixColor = value;
			NotifyPropertyChanged("IsShowFixColor");
		}
	}

	public bool IsShowItems
	{
		get
		{
			return _isShowItems;
		}
		set
		{
			_isShowItems = value;
			NotifyPropertyChanged("IsShowItems");
		}
	}

	public string FixedColorStr
	{
		get
		{
			return _fixedColorStr;
		}
		set
		{
			_fixedColorStr = value;
			NotifyPropertyChanged("FixedColorStr");
		}
	}

	public ObservableCollection<CalColorConfigItem> Items { get; set; } = new ObservableCollection<CalColorConfigItem>();

	private Dictionary<CalColorGroupType, HashSet<ChangedType>> changedTrackDict { get; set; } = new Dictionary<CalColorGroupType, HashSet<ChangedType>>();

	private bool isInboxColorChanged { get; set; }

	private CalColorGroupType currentGroup { get; set; }

	private CalTaskColorMode taskColorMode { get; set; }

	private CalHabitColorMode habitColorMode { get; set; }

	private CalCountdownColorMode countDownColorMode { get; set; }

	private List<CalColorConfigItem> items { get; set; } = new List<CalColorConfigItem>();

	private bool reload { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public async Task Init()
	{
		try
		{
			CalendarColorConfig obj = LocalSettings.Settings?.UserPreference?.CalendarColorConfig;
			taskColorMode = CalendarUtils.GetTaskColorMode();
			CalHabitColorMode result = CalHabitColorMode.Theme;
			string value = obj?.Habit?.ColorType;
			if (!string.IsNullOrEmpty(value))
			{
				Enum.TryParse<CalHabitColorMode>(value, ignoreCase: true, out result);
			}
			habitColorMode = result;
			CalCountdownColorMode result2 = CalCountdownColorMode.Shared;
			string value2 = obj?.Countdown?.ColorType;
			if (!string.IsNullOrEmpty(value2))
			{
				Enum.TryParse<CalCountdownColorMode>(value2, ignoreCase: true, out result2);
			}
			countDownColorMode = result2;
			await ChangeSelectGroup(CalColorGroupType.Task);
		}
		catch (Exception ex)
		{
			UtilLog.Error("CalendarColorConfigViewModel Init Error:" + ex.Message + Environment.NewLine + ex.StackTrace);
		}
	}

	public async Task ChangeSelectGroup(CalColorGroupType type)
	{
		currentGroup = type;
		OnGroupChange?.Invoke(type);
		IsShowFixColor = false;
		IsShowTip = false;
		IsShowItems = false;
		DisplayWayText = Utils.GetString("ColorBy");
		switch (currentGroup)
		{
		case CalColorGroupType.Task:
			IsShowTip = true;
			await ChangeMode((int)taskColorMode);
			break;
		case CalColorGroupType.Habit:
			await ChangeMode((int)habitColorMode);
			break;
		case CalColorGroupType.Countdown:
			IsShowTip = true;
			await ChangeMode((int)countDownColorMode);
			break;
		}
	}

	private CalColorConfigItem GetItemFromProjectItemViewModel(ProjectItemViewModel model, CalColorConfigItem.CalColorItemDataType type)
	{
		string title = model.Title;
		CalColorConfigItem calColorConfigItem = new CalColorConfigItem
		{
			Name = title,
			ColorStr = model.Color,
			Id = model.Id,
			DataType = type
		};
		switch (type)
		{
		case CalColorConfigItem.CalColorItemDataType.Project:
		{
			string emojiIcon = EmojiHelper.GetEmojiIcon(title);
			if (!string.IsNullOrEmpty(emojiIcon) && title.StartsWith(emojiIcon))
			{
				emojiIcon = EmojiHelper.GetEmojiIcon(title);
				calColorConfigItem.Name = title.Remove(0, emojiIcon.Length);
				calColorConfigItem.IconType = IconType.Emoji;
				calColorConfigItem.Emoji = emojiIcon;
			}
			else
			{
				calColorConfigItem.IconType = IconType.Path;
				calColorConfigItem.Geometry = model.Icon;
			}
			break;
		}
		case CalColorConfigItem.CalColorItemDataType.Tag:
		{
			TagProjectViewModel tagProjectViewModel = model as TagProjectViewModel;
			calColorConfigItem.Id = tagProjectViewModel.TagModel.id;
			calColorConfigItem.IconType = IconType.Path;
			calColorConfigItem.Geometry = model.Icon;
			break;
		}
		}
		return calColorConfigItem;
	}

	public async Task ChangeMode(int value)
	{
		bool flag = false;
		reload = true;
		switch (currentGroup)
		{
		case CalColorGroupType.Task:
			if (taskColorMode != (CalTaskColorMode)value)
			{
				SetChangeFlag(currentGroup, ChangedType.Mode);
			}
			taskColorMode = (CalTaskColorMode)value;
			OnModeChange?.Invoke(value);
			switch (value)
			{
			case 0:
				TipText = Utils.GetString("ColorByListTaskDesc");
				IsShowItems = true;
				Items.Clear();
				items.Clear();
				foreach (ProjectItemViewModel item in (await ProjectDataProvider.GetProjectData(ignoreCollapse: true)).Where((ProjectItemViewModel x) => ((x is InboxProjectViewModel || x.ListType == ProjectListType.Project) && !x.IsSubItem) || (x is ProjectGroupViewModel && !(x is ClosedProjectGroupViewModel))))
				{
					CalColorConfigItem itemFromProjectItemViewModel3 = GetItemFromProjectItemViewModel(item, CalColorConfigItem.CalColorItemDataType.Project);
					if (item is ProjectGroupViewModel)
					{
						itemFromProjectItemViewModel3.CanSetColor = false;
						itemFromProjectItemViewModel3.Items = new ObservableCollection<CalColorConfigItem>();
						foreach (ProjectItemViewModel child in item.Children)
						{
							if (child.ListType == ProjectListType.Project)
							{
								CalColorConfigItem itemFromProjectItemViewModel4 = GetItemFromProjectItemViewModel(child, CalColorConfigItem.CalColorItemDataType.Project);
								itemFromProjectItemViewModel3.Items.Add(itemFromProjectItemViewModel4);
							}
						}
					}
					else if (item is InboxProjectViewModel)
					{
						itemFromProjectItemViewModel3.IsInbox = true;
					}
					items.Add(itemFromProjectItemViewModel3);
				}
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode) || CheckChangeFlag(currentGroup, ChangedType.ItemColor);
				break;
			case 1:
				TipText = Utils.GetString("ColorByTagTaskDesc");
				IsShowItems = true;
				Items.Clear();
				items.Clear();
				foreach (ProjectItemViewModel item2 in (await ProjectDataProvider.GetProjectData(ignoreCollapse: true)).Where((ProjectItemViewModel x) => x.ListType == ProjectListType.Tag && !x.IsSubItem))
				{
					CalColorConfigItem itemFromProjectItemViewModel = GetItemFromProjectItemViewModel(item2, CalColorConfigItem.CalColorItemDataType.Tag);
					if (item2.IsGroupItem)
					{
						itemFromProjectItemViewModel.Items = new ObservableCollection<CalColorConfigItem>();
						foreach (ProjectItemViewModel child2 in item2.Children)
						{
							if (child2.ListType == ProjectListType.Tag)
							{
								CalColorConfigItem itemFromProjectItemViewModel2 = GetItemFromProjectItemViewModel(child2, CalColorConfigItem.CalColorItemDataType.Tag);
								itemFromProjectItemViewModel.Items.Add(itemFromProjectItemViewModel2);
							}
						}
					}
					items.Add(itemFromProjectItemViewModel);
				}
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode) || CheckChangeFlag(currentGroup, ChangedType.ItemColor);
				break;
			case 2:
				TipText = Utils.GetString("ColorByPriorityTaskDesc");
				IsShowItems = false;
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode);
				break;
			}
			if (flag)
			{
				SaveTaskConfig(saveAndPush: false);
				DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			}
			if (items.Any())
			{
				reload = false;
				{
					foreach (CalColorConfigItem item3 in items.ToList())
					{
						if (!reload)
						{
							Items.Add(item3);
							continue;
						}
						break;
					}
					break;
				}
			}
			IsShowItems = false;
			break;
		case CalColorGroupType.Habit:
			if (habitColorMode != (CalHabitColorMode)value)
			{
				SetChangeFlag(currentGroup, ChangedType.Mode);
			}
			habitColorMode = (CalHabitColorMode)value;
			OnModeChange?.Invoke(value);
			switch (value)
			{
			case 0:
				IsShowFixColor = false;
				IsShowTip = false;
				IsShowItems = false;
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode);
				break;
			case 1:
			{
				IsShowFixColor = true;
				IsShowTip = true;
				TipText = Utils.GetString("ColorByFixedHabitDesc");
				IsShowItems = false;
				FixedColorText = Utils.GetString("Color");
				string text2 = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Habit?.SharedColor;
				if (string.IsNullOrEmpty(text2))
				{
					Color colorValue = ThemeUtil.GetColorValue("ColorPrimary");
					FixedColorStr = $"#{colorValue.R:X2}{colorValue.G:X2}{colorValue.B:X2}";
				}
				else
				{
					FixedColorStr = text2;
				}
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode) || CheckChangeFlag(currentGroup, ChangedType.FixedColor);
				break;
			}
			case 2:
			{
				IsShowFixColor = false;
				IsShowTip = true;
				TipText = Utils.GetString("ColorByHabitDesc");
				IsShowItems = true;
				Items.Clear();
				items.Clear();
				CalendarColorConfigItem config = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Habit;
				foreach (HabitModel item4 in (await HabitService.GetHabitsbyStatus(0)).OrderBy((HabitModel h) => h.SortOrder))
				{
					CalColorConfigItem calColorConfigItem2 = new CalColorConfigItem
					{
						Name = item4.Name,
						Id = item4.Id,
						DataType = CalColorConfigItem.CalColorItemDataType.Habit
					};
					if (!string.IsNullOrEmpty(item4.IconRes) && item4.IconRes.StartsWith("txt_"))
					{
						calColorConfigItem2.IconType = IconType.ImageSource;
						calColorConfigItem2.ImageSource = CountdownService.GetIcon(item4.IconRes, item4.Color);
					}
					else
					{
						calColorConfigItem2.IconType = IconType.File;
						calColorConfigItem2.ImagePath = "../../Assets/Habits/" + item4.IconRes.ToLower() + ".png";
					}
					if (config?.ColorMap != null && !string.IsNullOrEmpty(calColorConfigItem2.Id) && config.ColorMap.TryGetValue(calColorConfigItem2.Id, out var value3))
					{
						calColorConfigItem2.ColorStr = value3;
					}
					items.Add(calColorConfigItem2);
				}
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode) || CheckChangeFlag(currentGroup, ChangedType.ItemColor);
				break;
			}
			}
			if (flag)
			{
				SaveHabitConfig(saveAndPush: false);
				DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			}
			if (items.Any())
			{
				reload = false;
				{
					foreach (CalColorConfigItem item5 in items.ToList())
					{
						if (!reload)
						{
							Items.Add(item5);
							continue;
						}
						break;
					}
					break;
				}
			}
			IsShowItems = false;
			break;
		case CalColorGroupType.Countdown:
			if (countDownColorMode != (CalCountdownColorMode)value)
			{
				SetChangeFlag(currentGroup, ChangedType.Mode);
			}
			countDownColorMode = (CalCountdownColorMode)value;
			OnModeChange?.Invoke(value);
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			switch (value)
			{
			case 0:
			{
				TipText = Utils.GetString("ColorByFixedCountdownDesc");
				IsShowFixColor = true;
				IsShowItems = false;
				FixedColorText = Utils.GetString("Color");
				string text = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Countdown?.SharedColor;
				FixedColorStr = (string.IsNullOrEmpty(text) ? "#FFAE58" : text);
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode) || CheckChangeFlag(currentGroup, ChangedType.FixedColor);
				break;
			}
			case 1:
			{
				IsShowFixColor = false;
				TipText = Utils.GetString("ColorByCountdownDesc");
				IsShowItems = true;
				Items.Clear();
				items.Clear();
				CalendarColorConfigItem config = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Countdown;
				List<CountdownModel> countdowns = await CountdownDao.GetActiveCountdowns();
				foreach (CountdownModel item6 in await Task.Run(() => MainCountDownViewModel.SortAllModels(countdowns, isArchivedOrder: false)))
				{
					CalColorConfigItem calColorConfigItem = new CalColorConfigItem
					{
						Name = item6.Name,
						IconType = IconType.ImageSource,
						ImageSource = CountdownService.GetIcon(item6.IconRes, item6.Color),
						Id = item6.Id,
						DataType = CalColorConfigItem.CalColorItemDataType.Countdown
					};
					if (config?.ColorMap != null && !string.IsNullOrEmpty(calColorConfigItem.Id) && config.ColorMap.TryGetValue(calColorConfigItem.Id, out var value2))
					{
						calColorConfigItem.ColorStr = value2;
					}
					items.Add(calColorConfigItem);
				}
				flag = CheckChangeFlag(currentGroup, ChangedType.Mode) || CheckChangeFlag(currentGroup, ChangedType.ItemColor);
				break;
			}
			}
			if (flag)
			{
				SaveCountdownConfig(saveAndPush: false);
				DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			}
			if (items.Any())
			{
				reload = false;
				{
					foreach (CalColorConfigItem item7 in items.ToList())
					{
						if (!reload)
						{
							Items.Add(item7);
							continue;
						}
						break;
					}
					break;
				}
			}
			IsShowItems = false;
			break;
		}
	}

	public async Task ChangeItemColor(CalColorConfigItem item)
	{
		switch (item.DataType)
		{
		case CalColorConfigItem.CalColorItemDataType.Project:
			if (item.IsInbox)
			{
				await ProjectDao.SaveInboxColor(string.IsNullOrEmpty(item.ColorStr) ? "transparent" : item.ColorStr);
				isInboxColorChanged = true;
			}
			else
			{
				ProjectModel projectModel = await ProjectDao.GetProjectById(item.Id);
				if (projectModel == null)
				{
					return;
				}
				if (projectModel.sync_status == Constants.SyncStatus.SYNC_DONE.ToString())
				{
					projectModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				}
				projectModel.color = item.ColorStr;
				await ProjectDao.TryUpdateProject(projectModel);
			}
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			break;
		case CalColorConfigItem.CalColorItemDataType.Tag:
		{
			TagModel tagModel = await TagDao.GetTagById(item.Id);
			if (tagModel == null)
			{
				return;
			}
			tagModel.color = item.ColorStr;
			tagModel.SetUpdateStatus();
			await TagDao.UpdateTag(tagModel);
			break;
		}
		case CalColorConfigItem.CalColorItemDataType.Habit:
			SaveHabitConfig(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			break;
		case CalColorConfigItem.CalColorItemDataType.Countdown:
			SaveCountdownConfig(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			break;
		}
		SetChangeFlag(currentGroup, ChangedType.ItemColor);
	}

	public void ChangeFixedColor()
	{
		switch (currentGroup)
		{
		case CalColorGroupType.Habit:
			SaveHabitConfig(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			break;
		case CalColorGroupType.Countdown:
			SaveCountdownConfig(saveAndPush: false);
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
			break;
		}
		SetChangeFlag(currentGroup, ChangedType.FixedColor);
	}

	public void OnClosing()
	{
		IEnumerable<ChangedType> source = changedTrackDict.SelectMany((KeyValuePair<CalColorGroupType, HashSet<ChangedType>> x) => x.Value);
		if (source.Any())
		{
			CalColorGroupType key = currentGroup;
			if (!changedTrackDict.TryGetValue(key, out var value) || value == null || !value.Any())
			{
				key = changedTrackDict.FirstOrDefault((KeyValuePair<CalColorGroupType, HashSet<ChangedType>> x) => x.Value != null && x.Value.Any()).Key;
			}
			switch (key)
			{
			case CalColorGroupType.Task:
				SaveTaskConfig(saveAndPush: true);
				break;
			case CalColorGroupType.Habit:
				SaveHabitConfig(saveAndPush: true);
				break;
			case CalColorGroupType.Countdown:
				SaveCountdownConfig(saveAndPush: true);
				break;
			}
			DataChangedNotifier.NotifyCalendarConfigChanged(colorChanged: true);
		}
		if (source.Contains(ChangedType.ItemColor))
		{
			SyncManager.Sync();
		}
	}

	private void SetChangeFlag(CalColorGroupType group, ChangedType changeType)
	{
		if (!changedTrackDict.ContainsKey(group))
		{
			changedTrackDict.Add(group, new HashSet<ChangedType>());
		}
		changedTrackDict[group].Add(changeType);
	}

	private bool CheckChangeFlag(CalColorGroupType group, ChangedType changeType)
	{
		if (changedTrackDict.TryGetValue(group, out var value) && value != null)
		{
			return value.Contains(changeType);
		}
		return false;
	}

	private void SaveTaskConfig(bool saveAndPush)
	{
		string text = taskColorMode.ToString().ToLower();
		CalendarColorConfigItem calendarColorConfigItem = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Task ?? new CalendarColorConfigItem
		{
			ColorType = text
		};
		calendarColorConfigItem.ColorType = text;
		LocalSettings.Settings.SaveCalendarColorConfig(CalColorGroupType.Task, calendarColorConfigItem, saveAndPush);
		LocalSettings.Settings.CellColorType = text;
		if (isInboxColorChanged)
		{
			SettingsHelper.PushLocalSettings();
		}
	}

	private void SaveHabitConfig(bool saveAndPush)
	{
		string colorType = habitColorMode.ToString().ToLower();
		CalendarColorConfigItem calendarColorConfigItem = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Habit ?? new CalendarColorConfigItem
		{
			ColorType = colorType
		};
		calendarColorConfigItem.ColorType = colorType;
		switch (habitColorMode)
		{
		case CalHabitColorMode.Shared:
			calendarColorConfigItem.SharedColor = FixedColorStr;
			break;
		case CalHabitColorMode.Custom:
			calendarColorConfigItem.ColorMap = items.Where((CalColorConfigItem x) => !string.IsNullOrEmpty(x.ColorStr)).ToDictionary((CalColorConfigItem x) => x.Id, (CalColorConfigItem x) => x.ColorStr);
			break;
		}
		LocalSettings.Settings.SaveCalendarColorConfig(CalColorGroupType.Habit, calendarColorConfigItem, saveAndPush);
	}

	private void SaveCountdownConfig(bool saveAndPush)
	{
		string colorType = countDownColorMode.ToString().ToLower();
		CalendarColorConfigItem calendarColorConfigItem = LocalSettings.Settings?.UserPreference?.CalendarColorConfig?.Countdown ?? new CalendarColorConfigItem
		{
			ColorType = colorType
		};
		calendarColorConfigItem.ColorType = colorType;
		switch (countDownColorMode)
		{
		case CalCountdownColorMode.Shared:
			calendarColorConfigItem.SharedColor = FixedColorStr;
			break;
		case CalCountdownColorMode.Custom:
			calendarColorConfigItem.ColorMap = items.Where((CalColorConfigItem x) => !string.IsNullOrEmpty(x.ColorStr)).ToDictionary((CalColorConfigItem x) => x.Id, (CalColorConfigItem x) => x.ColorStr);
			break;
		}
		LocalSettings.Settings.SaveCalendarColorConfig(CalColorGroupType.Countdown, calendarColorConfigItem, saveAndPush);
	}
}
