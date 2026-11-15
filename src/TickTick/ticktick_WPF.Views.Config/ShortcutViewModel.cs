using System;
using System.Collections.Generic;
using System.Windows;
using NHotkey;
using NHotkey.Wpf;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Config;

public class ShortcutViewModel : BaseViewModel
{
	private static readonly string SetShortcut = Utils.GetString("SetShortcut");

	private string _originHotKey;

	private string _newHotKey;

	private string _shortCut;

	private string _toolTip;

	public string OriginHotKey
	{
		get
		{
			return _originHotKey;
		}
		set
		{
			_originHotKey = value;
			OnPropertyChanged("OriginHotKey");
		}
	}

	public string NewHotKey
	{
		get
		{
			return _newHotKey;
		}
		set
		{
			_newHotKey = value;
			OnPropertyChanged("NewHotKey");
		}
	}

	public string Name { get; set; }

	public string Title { get; set; }

	public bool IsSection { get; set; }

	public string Description { get; set; }

	public string DisplayShortcut { get; set; }

	public CornerRadius ItemCornerRadius { get; set; } = new CornerRadius(0.0);

	public Thickness BorderMargin { get; set; } = new Thickness(0.0);

	public Thickness SectionMargin { get; set; } = new Thickness(18.0, 32.0, 4.0, 0.0);

	public bool EnableOneWord { get; set; }

	public string Shortcut
	{
		get
		{
			return _shortCut;
		}
		set
		{
			_shortCut = value;
			if (!string.IsNullOrEmpty(value))
			{
				int num = value.IndexOf('D');
				if (num != -1 && Shortcut?.Length > num + 1 && '0' <= Shortcut[num + 1] && Shortcut[num + 1] <= '9')
				{
					DisplayShortcut = HotkeyModel.HandleUpDownText(value.Remove(num, 1));
				}
				else
				{
					DisplayShortcut = HotkeyModel.HandleUpDownText(value);
				}
			}
		}
	}

	public string ExtraShortCut { get; set; }

	public bool Editable { get; set; }

	public bool IsGlobal { get; set; }

	public string ToolTip
	{
		get
		{
			return _toolTip;
		}
		set
		{
			_toolTip = value;
			IsKeyValid = string.IsNullOrEmpty(_toolTip);
			OnPropertyChanged("ToolTip");
			OnPropertyChanged("IsKeyValid");
		}
	}

	public bool IsKeyValid { get; set; } = true;

	public ShortcutViewModel()
	{
	}

	private ShortcutViewModel(string name, string shortcut, bool editable = true)
	{
		Name = name;
		Title = Utils.GetString(name);
		Shortcut = shortcut;
		Editable = editable;
		OriginHotKey = (string.IsNullOrEmpty(DisplayShortcut) ? SetShortcut : DisplayShortcut);
	}

	private ShortcutViewModel(string name, string title, string shortcut, bool editable, bool isGlobal)
	{
		Name = name;
		Title = title;
		Shortcut = shortcut;
		Editable = editable;
		OriginHotKey = (string.IsNullOrEmpty(DisplayShortcut) ? SetShortcut : DisplayShortcut);
		IsGlobal = isGlobal;
		if (isGlobal)
		{
			CheckShortKey();
		}
	}

	public static IEnumerable<ShortcutViewModel> BuildShortcuts()
	{
		ShortcutModel shortCutModel = LocalSettings.Settings.ShortCutModel;
		ShortcutViewModel item = new ShortcutViewModel("SwitchTabShortcut", "Ctrl+1,2...")
		{
			Editable = false,
			ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0),
			BorderMargin = new Thickness(0.0, -4.0, 0.0, 0.0)
		};
		bool flag = !KeyBindingManager.HasCtrlNumKeyBinding();
		List<ShortcutViewModel> list = new List<ShortcutViewModel>
		{
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("General"),
				SectionMargin = new Thickness(18.0, 18.0, 4.0, 0.0)
			},
			new ShortcutViewModel("SyncTask", shortCutModel.GetPropertyValue("SyncTask"))
			{
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0),
				BorderMargin = new Thickness(0.0, -4.0, 0.0, 0.0)
			},
			new ShortcutViewModel("Cancel", "Esc", editable: false),
			new ShortcutViewModel("Undo", "Ctrl+Z", editable: false),
			new ShortcutViewModel("TextRedo", "Ctrl+Shift+Z", editable: false),
			new ShortcutViewModel("Print", shortCutModel.GetPropertyValue("Print")),
			new ShortcutViewModel("PrintDetail", shortCutModel.GetPropertyValue("PrintDetail")),
			new ShortcutViewModel("OpenOperationMenu", "Ctrl+K", editable: false),
			new ShortcutViewModel("Shortcut", "?", editable: false)
			{
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("GlobalOperate"),
				Description = Utils.GetString("GlobalOperateMessage")
			},
			new ShortcutViewModel("ShortcutAddTask", Utils.GetString("GlobalAddTask"), LocalSettings.Settings.ShortcutAddTask, editable: true, isGlobal: true)
			{
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0),
				BorderMargin = new Thickness(0.0, -4.0, 0.0, 0.0)
			},
			new ShortcutViewModel("ShortcutOpenOrClose", Utils.GetString("ShowOrHideApp"), LocalSettings.Settings.ShortcutOpenOrClose, editable: true, isGlobal: true),
			new ShortcutViewModel("PomoShortcut", Utils.GetString("OpenOrCloseFocusWindow"), LocalSettings.Settings.PomoShortcut, LocalSettings.Settings.EnableFocus, isGlobal: true),
			new ShortcutViewModel("ShortcutPin", Utils.GetString("PinOrUnPin"), LocalSettings.Settings.ShortcutPin, editable: true, isGlobal: true),
			new ShortcutViewModel("LockShortcut", Utils.GetString("LockUnlockTickTick"), LocalSettings.Settings.LockShortcut, editable: true, isGlobal: true),
			new ShortcutViewModel("CreateStickyShortcut", Utils.GetString("NewSticky"), LocalSettings.Settings.CreateStickyShortCut, editable: true, isGlobal: true),
			new ShortcutViewModel("ShowHideStickyShortcut", Utils.GetString("ShowHideSticky"), LocalSettings.Settings.ShowHideStickyShortCut, editable: true, isGlobal: true)
			{
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("AddTask")
			},
			new ShortcutViewModel("AddTask", Utils.GetString("AddTaskLower"), shortCutModel.GetPropertyValue("AddTask"), editable: true, isGlobal: false)
			{
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0),
				BorderMargin = new Thickness(0.0, -4.0, 0.0, 0.0)
			},
			new ShortcutViewModel("AddTaskUnder", "Enter", editable: false),
			new ShortcutViewModel("AddSubTask", "Shift+Enter", editable: false)
			{
				Title = Utils.GetString("AddSubTask").UpCaseFirst()
			},
			new ShortcutViewModel("ExpandAllSection", Utils.GetString("ToggleAllGroups"), shortCutModel.GetPropertyValue("ExpandAllSection"), editable: true, isGlobal: false),
			new ShortcutViewModel("ExpandAllTask", Utils.GetString("ExpandOrCollapseSubtasks"), shortCutModel.GetPropertyValue("ExpandAllTask"), editable: true, isGlobal: false)
			{
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("QuickSetTask"),
				Description = Utils.GetString("QuickSetTaskMessage")
			},
			new ShortcutViewModel("QuickSetDate", "*", editable: false)
			{
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0)
			},
			new ShortcutViewModel("QuickSetPriority", "!", editable: false),
			new ShortcutViewModel("QuickSetTag", "#", editable: false),
			new ShortcutViewModel("QuickSetProject", "~/^", editable: false),
			new ShortcutViewModel("QuickSetAssign", "@", editable: false)
			{
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("EditTask"),
				Description = Utils.GetString("EditTaskMessage")
			},
			new ShortcutViewModel("CompleteTask", shortCutModel.GetPropertyValue("CompleteTask"))
			{
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0),
				BorderMargin = new Thickness(0.0, -4.0, 0.0, 0.0)
			},
			new ShortcutViewModel("PinTask", shortCutModel.GetPropertyValue("PinTask")),
			new ShortcutViewModel("DeleteTask", shortCutModel.GetPropertyValue("DeleteTask")),
			new ShortcutViewModel("SetDate", shortCutModel.GetPropertyValue("SetDate")),
			new ShortcutViewModel("ClearDate", shortCutModel.GetPropertyValue("ClearDate")),
			new ShortcutViewModel("SetToday", shortCutModel.GetPropertyValue("SetToday")),
			new ShortcutViewModel("SetTomorrow", shortCutModel.GetPropertyValue("SetTomorrow")),
			new ShortcutViewModel("SetNextWeek", shortCutModel.GetPropertyValue("SetNextWeek")),
			new ShortcutViewModel("SetNoPriority", shortCutModel.GetPropertyValue("SetNoPriority")),
			new ShortcutViewModel("SetLowPriority", shortCutModel.GetPropertyValue("SetLowPriority")),
			new ShortcutViewModel("SetMediumPriority", shortCutModel.GetPropertyValue("SetMediumPriority")),
			new ShortcutViewModel("SetHighPriority", shortCutModel.GetPropertyValue("SetHighPriority")),
			new ShortcutViewModel("OpenSticky", shortCutModel.GetPropertyValue("OpenSticky"))
			{
				Title = Utils.GetString("OpenAsSticky"),
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("Navigation"),
				Description = Utils.GetString("NavigationMessage")
			},
			item,
			new ShortcutViewModel("ToggleParallelView", Utils.GetString("ShowHideSplitView"), shortCutModel.ToggleParallelView, editable: true, isGlobal: false)
			{
				ItemCornerRadius = (flag ? new CornerRadius(0.0) : new CornerRadius(8.0, 8.0, 0.0, 0.0)),
				BorderMargin = new Thickness(0.0, (!flag) ? (-4) : 0, 0.0, 0.0)
			},
			new ShortcutViewModel("SearchTask", shortCutModel.GetPropertyValue("SearchTask")),
			new ShortcutViewModel("OpenSetting", shortCutModel.GetPropertyValue("OpenSetting"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Settings")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "S")
			},
			new ShortcutViewModel("JumpAll", shortCutModel.GetPropertyValue("JumpAll"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("All")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "A")
			},
			new ShortcutViewModel("JumpToday", shortCutModel.GetPropertyValue("JumpToday"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Today")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "T")
			},
			new ShortcutViewModel("JumpTomorrow", shortCutModel.GetPropertyValue("JumpTomorrow"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Tomorrow")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "R")
			},
			new ShortcutViewModel("JumpWeek", shortCutModel.GetPropertyValue("JumpWeek"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Next7Day")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "N")
			},
			new ShortcutViewModel("JumpAssign", shortCutModel.GetPropertyValue("JumpAssign"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("AssignToMe")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "M")
			},
			new ShortcutViewModel("JumpInbox", shortCutModel.GetPropertyValue("JumpInbox"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Inbox")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "I")
			},
			new ShortcutViewModel("JumpComplete", shortCutModel.GetPropertyValue("JumpComplete"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Completed")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "C")
			},
			new ShortcutViewModel("JumpAbandon", shortCutModel.GetPropertyValue("JumpAbandon"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Abandoned")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "W")
			},
			new ShortcutViewModel("JumpSummary", shortCutModel.GetPropertyValue("JumpSummary"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Summary")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "B")
			},
			new ShortcutViewModel("JumpTrash", shortCutModel.GetPropertyValue("JumpTrash"))
			{
				Title = string.Format(Utils.GetString("GoList"), Utils.GetString("Trash")),
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "G", "G"),
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("SwitchView"),
				Description = Utils.GetString("SwitchViewMessage")
			},
			new ShortcutViewModel("OmListView", shortCutModel.GetPropertyValue("OmListView"))
			{
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "V", "L"),
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0)
			},
			new ShortcutViewModel("OmKanbanView", shortCutModel.GetPropertyValue("OmKanbanView"))
			{
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "V", "K")
			},
			new ShortcutViewModel("OmTimelineView", shortCutModel.GetPropertyValue("OmTimelineView"))
			{
				ExtraShortCut = string.Format(Utils.GetString("AThenB"), "V", "T")
			},
			new ShortcutViewModel("DayView", "D / 1", editable: false),
			new ShortcutViewModel("WeekView", "W / 2", editable: false),
			new ShortcutViewModel("MonthView", "M / 3", editable: false),
			new ShortcutViewModel("YearView", "Y / 4", editable: false),
			new ShortcutViewModel("AgendaView", "A / 5", editable: false),
			new ShortcutViewModel("BackToToday", "T", editable: false)
			{
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0)
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("Focus")
			},
			new ShortcutViewModel("ContinueAndPause", shortCutModel.GetPropertyValue("ContinueAndPause"))
			{
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0),
				IsGlobal = true
			},
			new ShortcutViewModel("StartAndDrop", shortCutModel.GetPropertyValue("StartAndDrop"))
			{
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				IsGlobal = true
			},
			new ShortcutViewModel
			{
				IsSection = true,
				Title = Utils.GetString("StickyNote")
			},
			new ShortcutViewModel("PinSticky", shortCutModel.GetPropertyValue("PinSticky"))
			{
				Title = Utils.GetString("Pin"),
				ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0)
			},
			new ShortcutViewModel("StickyColor", shortCutModel.GetPropertyValue("StickyColor"))
			{
				Title = Utils.GetString("Color")
			},
			new ShortcutViewModel("StickyCollapse", shortCutModel.GetPropertyValue("StickyCollapse"))
			{
				Title = Utils.GetString("CollapseOrExpandAll")
			},
			new ShortcutViewModel("StickyAlignTop", shortCutModel.GetPropertyValue("StickyAlignTop"))
			{
				Title = Utils.GetString("TopAlignment")
			},
			new ShortcutViewModel("StickyAlignLeft", shortCutModel.GetPropertyValue("StickyAlignLeft"))
			{
				Title = Utils.GetString("LeftAlignment")
			},
			new ShortcutViewModel("StickyAlignRight", shortCutModel.GetPropertyValue("StickyAlignRight"))
			{
				Title = Utils.GetString("RightAlignment"),
				ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
				BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
			}
		};
		if (!Utils.IsWindows7())
		{
			list.AddRange(new ShortcutViewModel[3]
			{
				new ShortcutViewModel
				{
					IsSection = true,
					Title = Utils.GetString("AI")
				},
				new ShortcutViewModel("AINewConversation", Utils.GetString("AIAgentNewChat"), shortCutModel.AINewConversation, editable: true, isGlobal: false)
				{
					ItemCornerRadius = new CornerRadius(8.0, 8.0, 0.0, 0.0),
					BorderMargin = new Thickness(0.0, -4.0, 0.0, 0.0)
				},
				new ShortcutViewModel("AIToggleFloatingWindow", Utils.GetString("ShowHideFloatingWindow"), shortCutModel.AIToggleFloatingWindow, editable: true, isGlobal: false)
				{
					ItemCornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
					BorderMargin = new Thickness(0.0, 0.0, 0.0, -4.0)
				}
			});
		}
		list.Add(new ShortcutViewModel
		{
			IsSection = true
		});
		if (!flag)
		{
			list.Remove(item);
		}
		return list;
	}

	public void CheckShortKey()
	{
		try
		{
			HotkeyManager.Current.Remove(Name);
			CheckHotkeyAvailability(this);
			HotKeyUtils.ResignHotKey(Name);
		}
		catch (Exception ex)
		{
			UtilLog.Warn(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private static bool CheckHotkeyAvailability(ShortcutViewModel model)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(model.Shortcut))
			{
				model.ToolTip = null;
				return true;
			}
			HotKeyUtils hotKeyUtils = new HotKeyUtils(model.Shortcut.Replace(" ", ""));
			HotkeyManager.Current.AddOrReplace("HotkeyAvailabilityTest", hotKeyUtils.Key, hotKeyUtils.Modifiers, delegate
			{
			});
			model.ToolTip = null;
			return true;
		}
		catch (Exception ex)
		{
			if (ex is HotkeyAlreadyRegisteredException)
			{
				model.ToolTip = Utils.GetString("ShortCutOccupied");
			}
			else
			{
				model.ToolTip = Utils.GetString("ShortCutOccupied");
			}
		}
		finally
		{
			HotkeyManager.Current.Remove("HotkeyAvailabilityTest");
		}
		return false;
	}

	public void ClearShortCut()
	{
		if (IsGlobal)
		{
			string name = Name;
			if (name == "ContinueAndPause" || name == "StartAndDrop")
			{
				LocalSettings.Settings.ShortCutModel.ClearShortcut(Name);
			}
			else
			{
				LocalSettings.Settings[Name] = string.Empty;
			}
			HotkeyManager.Current.Remove(Name);
		}
		else
		{
			LocalSettings.Settings.ShortCutModel.ClearShortcut(Name);
		}
	}

	public void SetShortCut(string shortcut)
	{
		Shortcut = shortcut;
		if (IsGlobal)
		{
			string name = Name;
			if (name == "ContinueAndPause" || name == "StartAndDrop")
			{
				LocalSettings.Settings.ShortCutModel.SetPropertyValue(Name, Shortcut);
			}
			else
			{
				LocalSettings.Settings[Name] = Shortcut;
			}
			CheckShortKey();
		}
		else
		{
			LocalSettings.Settings.ShortCutModel.SetPropertyValue(Name, Shortcut);
		}
		SettingsHelper.ShortCutChanged = true;
	}
}
