using System;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Search;

public class SearchOperationViewModel : UpDownSelectViewModel
{
	public SearchOperationType Type { get; set; }

	public string Keyword { get; set; }

	public string Title { get; set; }

	public string Shortcut { get; set; }

	public Geometry Icon { get; set; }

	public Pinyin Pinyin { get; set; }

	public string PtfId { get; set; }

	public string SearchText { get; set; }

	public string Emoji { get; set; }

	public string ColumnId { get; set; }

	public string ProjectName { get; set; }

	public SearchOperationViewModel()
	{
	}

	public SearchOperationViewModel(string sectionName)
	{
		Title = sectionName;
		base.IsEnable = false;
	}

	public SearchOperationViewModel(SearchOperationType type)
	{
		Type = type;
		Title = GetTitle(type);
		Pinyin = PinyinUtils.ToPinyin(Title);
		Icon = GetIcon(type);
		Shortcut = GetShortcut(type);
		Keyword = GetKeyword(type);
	}

	private string GetKeyword(SearchOperationType type)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		if (ShortcutModel.GoShortcutDict.ContainsKey(type))
		{
			text = "G" + ShortcutModel.GoShortcutDict[type];
		}
		switch (type)
		{
		case SearchOperationType.AddTask:
			return LocalSettings.Settings.ShortCutModel.GetPropertyValue("AddTask");
		case SearchOperationType.GlobalAddTask:
			return LocalSettings.Settings.ShortcutAddTask;
		case SearchOperationType.ShowOrHideApp:
			return LocalSettings.Settings.ShortcutOpenOrClose;
		case SearchOperationType.OpenOrCloseFocusWindow:
			return LocalSettings.Settings.PomoShortcut;
		case SearchOperationType.NewSticky:
			return LocalSettings.Settings.CreateStickyShortCut;
		case SearchOperationType.ShowHideSticky:
			return LocalSettings.Settings.ShowHideStickyShortCut;
		case SearchOperationType.SearchTask:
			return LocalSettings.Settings.ShortCutModel.GetPropertyValue("SearchTask");
		case SearchOperationType.ShowShortCut:
			return "?";
		case SearchOperationType.Task:
		case SearchOperationType.Calendar:
		case SearchOperationType.Matrix:
		case SearchOperationType.Habit:
		case SearchOperationType.Focus:
		case SearchOperationType.Anniversary:
		case SearchOperationType.AI:
			return GetNavigationModuleShortcut(type);
		case SearchOperationType.GoAll:
			text2 = LocalSettings.Settings.ShortCutModel.JumpAll;
			break;
		case SearchOperationType.GoToday:
			text2 = LocalSettings.Settings.ShortCutModel.JumpToday;
			break;
		case SearchOperationType.GoTomorrow:
			text2 = LocalSettings.Settings.ShortCutModel.JumpTomorrow;
			break;
		case SearchOperationType.GoNext7Day:
			text2 = LocalSettings.Settings.ShortCutModel.JumpWeek;
			break;
		case SearchOperationType.GoAssignToMe:
			text2 = LocalSettings.Settings.ShortCutModel.JumpAssign;
			break;
		case SearchOperationType.GoInbox:
			text2 = LocalSettings.Settings.ShortCutModel.JumpInbox;
			break;
		case SearchOperationType.GoCompleted:
			text2 = LocalSettings.Settings.ShortCutModel.JumpComplete;
			break;
		case SearchOperationType.GoAbandoned:
			text2 = LocalSettings.Settings.ShortCutModel.JumpAbandon;
			break;
		case SearchOperationType.GoTrash:
			text2 = LocalSettings.Settings.ShortCutModel.JumpTrash;
			break;
		case SearchOperationType.GoSummary:
			text2 = LocalSettings.Settings.ShortCutModel.JumpSummary;
			break;
		}
		if (!string.IsNullOrEmpty(text2))
		{
			text = text + " " + text2;
		}
		return text;
	}

	public static Geometry GetIcon(SearchOperationType type)
	{
		string text = null;
		switch (type)
		{
		case SearchOperationType.AddTask:
			text = "Ic.ProjectList.Add";
			break;
		case SearchOperationType.GlobalAddTask:
			text = "Ic.HotKey.GlobalAdd";
			break;
		case SearchOperationType.OpenOrCloseFocusWindow:
			text = "Ic.ProjectList.Focus";
			break;
		case SearchOperationType.SearchTask:
			text = "Ic.TaskOM.Search";
			break;
		case SearchOperationType.GoSettings:
			text = "Ic.Tabbar.SettingLinear";
			break;
		case SearchOperationType.Calendar:
			text = "Ic.TaskOM.DateRemind";
			break;
		case SearchOperationType.GoToday:
			text = "Ic.ProjectList.Date" + DateTime.Today.Day;
			break;
		case SearchOperationType.GoNext7Day:
			text = "Ic.ProjectList." + DateTime.Today.DayOfWeek;
			break;
		case SearchOperationType.HelpCenter:
			text = "Ic.Common.Help";
			break;
		case SearchOperationType.ShowShortCut:
			text = "Ic.Setting.HotKey";
			break;
		case SearchOperationType.NewSticky:
			text = "Ic.TaskOM.StickyNote";
			break;
		case SearchOperationType.GoTrash:
			text = "Ic.ProjectList.Delete";
			break;
		case SearchOperationType.GoAssignToMe:
			text = "Ic.ProjectList.AssignToMe";
			break;
		case SearchOperationType.ShowOrHideApp:
			text = "Ic.HotKey.ShowHideApp";
			break;
		case SearchOperationType.GoSummary:
			text = "Ic.ProjectList.Summary";
			break;
		case SearchOperationType.GoTomorrow:
			text = "Ic.ProjectList.Tomorrow";
			break;
		case SearchOperationType.GoAbandoned:
			text = "Ic.ProjectList.Abandon";
			break;
		case SearchOperationType.GoAll:
			text = "Ic.ProjectList.All";
			break;
		case SearchOperationType.GoCompleted:
			text = "Ic.ProjectList.Completed";
			break;
		case SearchOperationType.GoFilter:
			text = "Ic.ProjectList.Filter";
			break;
		case SearchOperationType.GoInbox:
			text = "Ic.ProjectList.Inbox";
			break;
		case SearchOperationType.Task:
			text = "Ic.HotKey.Completed";
			break;
		case SearchOperationType.Matrix:
			text = "Ic.HotKey.Matrix";
			break;
		case SearchOperationType.Habit:
			text = "Ic.HotKey.Habit";
			break;
		case SearchOperationType.AI:
			text = "Ic.AI.AI";
			break;
		case SearchOperationType.Focus:
			text = "Ic.HotKey.Focus";
			break;
		case SearchOperationType.Anniversary:
			text = "Ic.HotKey.Anniversary";
			break;
		}
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (text.StartsWith("Ic."))
		{
			return Utils.GetGeometry(text);
		}
		return Utils.GetIcon(text);
	}

	public static string GetTitle(SearchOperationType type)
	{
		if (type == SearchOperationType.Anniversary)
		{
			return Utils.GetString("type_anniversary");
		}
		if (SearchOperationType.GoSettings <= type && SearchOperationType.HelpCenter > type)
		{
			return string.Format(Utils.GetString("GoList"), Utils.GetString(type.ToString().Substring(2)));
		}
		return Utils.GetString(type.ToString());
	}

	public static string GetShortcut(SearchOperationType type)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		if (ShortcutModel.GoShortcutDict.ContainsKey(type))
		{
			text = string.Format(Utils.GetString("AThenB"), "G", ShortcutModel.GoShortcutDict[type]);
		}
		switch (type)
		{
		case SearchOperationType.AddTask:
			return LocalSettings.Settings.ShortCutModel.GetPropertyValue("AddTask");
		case SearchOperationType.GlobalAddTask:
			return LocalSettings.Settings.ShortcutAddTask;
		case SearchOperationType.ShowOrHideApp:
			return LocalSettings.Settings.ShortcutOpenOrClose;
		case SearchOperationType.OpenOrCloseFocusWindow:
			return LocalSettings.Settings.PomoShortcut;
		case SearchOperationType.NewSticky:
			return LocalSettings.Settings.CreateStickyShortCut;
		case SearchOperationType.ShowHideSticky:
			return LocalSettings.Settings.ShowHideStickyShortCut;
		case SearchOperationType.SearchTask:
			return LocalSettings.Settings.ShortCutModel.GetPropertyValue("SearchTask");
		case SearchOperationType.ShowShortCut:
			return "?";
		case SearchOperationType.Task:
		case SearchOperationType.Calendar:
		case SearchOperationType.Matrix:
		case SearchOperationType.Habit:
		case SearchOperationType.Focus:
		case SearchOperationType.Anniversary:
		case SearchOperationType.AI:
			return GetNavigationModuleShortcut(type);
		case SearchOperationType.GoAll:
			text2 = LocalSettings.Settings.ShortCutModel.JumpAll;
			break;
		case SearchOperationType.GoToday:
			text2 = LocalSettings.Settings.ShortCutModel.JumpToday;
			break;
		case SearchOperationType.GoTomorrow:
			text2 = LocalSettings.Settings.ShortCutModel.JumpTomorrow;
			break;
		case SearchOperationType.GoNext7Day:
			text2 = LocalSettings.Settings.ShortCutModel.JumpWeek;
			break;
		case SearchOperationType.GoAssignToMe:
			text2 = LocalSettings.Settings.ShortCutModel.JumpAssign;
			break;
		case SearchOperationType.GoInbox:
			text2 = LocalSettings.Settings.ShortCutModel.JumpInbox;
			break;
		case SearchOperationType.GoCompleted:
			text2 = LocalSettings.Settings.ShortCutModel.JumpComplete;
			break;
		case SearchOperationType.GoAbandoned:
			text2 = LocalSettings.Settings.ShortCutModel.JumpAbandon;
			break;
		case SearchOperationType.GoTrash:
			text2 = LocalSettings.Settings.ShortCutModel.JumpTrash;
			break;
		case SearchOperationType.GoSummary:
			text2 = LocalSettings.Settings.ShortCutModel.JumpSummary;
			break;
		}
		if (!string.IsNullOrEmpty(text2))
		{
			text = text + " , " + text2;
		}
		return text;
	}

	private static string GetNavigationModuleShortcut(SearchOperationType type)
	{
		return type switch
		{
			SearchOperationType.Task => GetTabBarShortcut("TASK"), 
			SearchOperationType.Calendar => GetTabBarShortcut("CALENDAR"), 
			SearchOperationType.Matrix => GetTabBarShortcut("MATRIX"), 
			SearchOperationType.Habit => GetTabBarShortcut("HABIT"), 
			SearchOperationType.Focus => GetTabBarShortcut("POMO"), 
			SearchOperationType.Anniversary => GetTabBarShortcut("COUNTDOWN"), 
			SearchOperationType.AI => GetTabBarShortcut("AI_ASSISTANT"), 
			_ => string.Empty, 
		};
	}

	private static string GetTabBarShortcut(string moduleName)
	{
		int num = TabBarModel.GetNavigationTabBars().FindIndex((TabBarModel bar) => string.Equals(bar.name, moduleName, StringComparison.OrdinalIgnoreCase));
		if (num < 0 || num >= 9)
		{
			return string.Empty;
		}
		string propertyValue = LocalSettings.Settings.ShortCutModel.GetPropertyValue($"Tab0{num + 1}");
		if (!string.IsNullOrEmpty(propertyValue))
		{
			return new HotkeyModel(propertyValue).ToString();
		}
		return string.Empty;
	}
}
