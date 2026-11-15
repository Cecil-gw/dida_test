using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using Newtonsoft.Json;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Models;

public class ShortcutModel
{
	public static Dictionary<SearchOperationType, string> GoShortcutDict = new Dictionary<SearchOperationType, string>
	{
		{
			SearchOperationType.GoSettings,
			"S"
		},
		{
			SearchOperationType.GoAll,
			"A"
		},
		{
			SearchOperationType.GoToday,
			"T"
		},
		{
			SearchOperationType.GoTomorrow,
			"R"
		},
		{
			SearchOperationType.GoNext7Day,
			"N"
		},
		{
			SearchOperationType.GoAssignToMe,
			"M"
		},
		{
			SearchOperationType.GoInbox,
			"I"
		},
		{
			SearchOperationType.GoCompleted,
			"C"
		},
		{
			SearchOperationType.GoAbandoned,
			"W"
		},
		{
			SearchOperationType.GoSummary,
			"B"
		},
		{
			SearchOperationType.GoTrash,
			"G"
		}
	};

	[JsonIgnore]
	public bool CanNotify;

	private string _clearDate = "Ctrl+Shift+D";

	private string _setToday = "";

	private string _setTomorrow = "";

	private string _setNextWeek = "";

	private string _setNoPriority = "";

	private string _setLowPriority = "";

	private string _setMediumPriority = "";

	private string _setHighPriority = "";

	private string _print = "Ctrl+P";

	private string _printDetail = "";

	private string _jumpAll = "";

	private string _jumpToday = "Alt+Shift+T";

	private string _jumpTomorrow = "";

	private string _jumpWeek = "Alt+Shift+N";

	private string _jumpAssign = "";

	private string _jumpInbox = "Alt+Shift+I";

	private string _jumpComplete = "";

	private string _jumpTrash = "";

	private string _jumpSummary = "";

	private string _jumpAbandon = "";

	private string _sync = "Ctrl+S";

	private string _input = "Ctrl+N";

	private string _setDate = "Ctrl+D";

	private string _complete = "Ctrl+M";

	private string _deleteTask = "Alt+Delete";

	private string _expandAllTask = "";

	private string _expandAllSection = "";

	private string _omListView = "";

	private string _omKanbanView = "";

	private string _omTimelineView = "";

	private string _jumpCalendar = "";

	private string _jumpHabit = "";

	private string _toggleParallelView = "";

	private string _aiNewConversation = "";

	private string _aiToggleFloatingWindow = "";

	private string _search = "Ctrl+F";

	private string _pinTask = "";

	private string _openSticky = "";

	private string _stickyColor = "Ctrl+Shift+C";

	private string _pinSticky = "F12";

	private string _openSetting = "";

	private string _stickyAlignTop = "Alt+Up";

	private string _stickyAlignLeft = "Alt+Left";

	private string _stickyAlignRight = "Alt+Right";

	private string _stickyCollapse = "Ctrl+E";

	private string _continueAndPause = "";

	private string _startAndDrop = "";

	private string _tab01;

	private string _tab02;

	private string _tab03;

	private string _tab04;

	private string _tab05;

	private string _tab06;

	private string _tab07;

	private string _tab08;

	private string _tab09;

	[JsonIgnore]
	public string Tab01
	{
		get
		{
			return _tab01;
		}
		set
		{
			_tab01 = value;
			UpdateShortcut("Tab01", value);
		}
	}

	[JsonIgnore]
	public string Tab02
	{
		get
		{
			return _tab02;
		}
		set
		{
			_tab02 = value;
			UpdateShortcut("Tab02", value);
		}
	}

	[JsonIgnore]
	public string Tab03
	{
		get
		{
			return _tab03;
		}
		set
		{
			_tab03 = value;
			UpdateShortcut("Tab03", value);
		}
	}

	[JsonIgnore]
	public string Tab04
	{
		get
		{
			return _tab04;
		}
		set
		{
			_tab04 = value;
			UpdateShortcut("Tab04", value);
		}
	}

	[JsonIgnore]
	public string Tab05
	{
		get
		{
			return _tab05;
		}
		set
		{
			_tab05 = value;
			UpdateShortcut("Tab05", value);
		}
	}

	[JsonIgnore]
	public string Tab06
	{
		get
		{
			return _tab06;
		}
		set
		{
			_tab06 = value;
			UpdateShortcut("Tab06", value);
		}
	}

	[JsonIgnore]
	public string Tab07
	{
		get
		{
			return _tab07;
		}
		set
		{
			_tab07 = value;
			UpdateShortcut("Tab07", value);
		}
	}

	[JsonIgnore]
	public string Tab08
	{
		get
		{
			return _tab08;
		}
		set
		{
			_tab08 = value;
			UpdateShortcut("Tab08", value);
		}
	}

	[JsonIgnore]
	public string Tab09
	{
		get
		{
			return _tab09;
		}
		set
		{
			_tab09 = value;
			UpdateShortcut("Tab09", value);
		}
	}

	public string TaskSetFolded { get; set; } = "0";

	public string TaskOperateFolded { get; set; } = "0";

	public string AppGlobalOperateFolded { get; set; } = "0";

	public string GlobalOperateFolded { get; set; } = "0";

	public string SetTimeFolded { get; set; } = "0";

	public string SetPriorityFolded { get; set; } = "0";

	public string QuickJumpFolded { get; set; } = "0";

	public string ExpandAllTask
	{
		get
		{
			return _expandAllTask;
		}
		set
		{
			_expandAllTask = value;
			UpdateShortcut("ExpandAllTask", value);
		}
	}

	public string ExpandAllSection
	{
		get
		{
			return _expandAllSection;
		}
		set
		{
			_expandAllSection = value;
			UpdateShortcut("ExpandAllSection", value);
		}
	}

	public string OmListView
	{
		get
		{
			return _omListView;
		}
		set
		{
			_omListView = value;
			UpdateShortcut("OmListView", value);
		}
	}

	public string OmKanbanView
	{
		get
		{
			return _omKanbanView;
		}
		set
		{
			_omKanbanView = value;
			UpdateShortcut("OmKanbanView", value);
		}
	}

	public string OmTimelineView
	{
		get
		{
			return _omTimelineView;
		}
		set
		{
			_omTimelineView = value;
			UpdateShortcut("OmTimelineView", value);
		}
	}

	public string SyncTask
	{
		get
		{
			return _sync;
		}
		set
		{
			_sync = value;
			UpdateShortcut("SyncTask", value);
		}
	}

	public string OpenOperation { get; set; } = "Ctrl+K";

	public string SearchTask
	{
		get
		{
			return _search;
		}
		set
		{
			_search = value;
			UpdateShortcut("SearchTask", value);
		}
	}

	public string AddTask
	{
		get
		{
			return _input;
		}
		set
		{
			_input = value;
			UpdateShortcut("AddTask", value);
		}
	}

	public string CompleteTask
	{
		get
		{
			return _complete;
		}
		set
		{
			_complete = value;
			UpdateShortcut("CompleteTask", value);
		}
	}

	public string DeleteTask
	{
		get
		{
			return _deleteTask;
		}
		set
		{
			_deleteTask = value;
			UpdateShortcut("DeleteTask", value);
		}
	}

	public string SetDate
	{
		get
		{
			return _setDate;
		}
		set
		{
			_setDate = value;
			UpdateShortcut("SetDate", value);
		}
	}

	public string ClearDate
	{
		get
		{
			return _clearDate;
		}
		set
		{
			_clearDate = value;
			UpdateShortcut("ClearDate", value);
		}
	}

	public string SetToday
	{
		get
		{
			return _setToday;
		}
		set
		{
			_setToday = value;
			UpdateShortcut("SetToday", value);
		}
	}

	public string SetTomorrow
	{
		get
		{
			return _setTomorrow;
		}
		set
		{
			_setTomorrow = value;
			UpdateShortcut("SetTomorrow", value);
		}
	}

	public string SetNextWeek
	{
		get
		{
			return _setNextWeek;
		}
		set
		{
			_setNextWeek = value;
			UpdateShortcut("SetNextWeek", value);
		}
	}

	public string SetNoPriority
	{
		get
		{
			return _setNoPriority;
		}
		set
		{
			_setNoPriority = value;
			UpdateShortcut("SetNoPriority", value);
		}
	}

	public string SetLowPriority
	{
		get
		{
			return _setLowPriority;
		}
		set
		{
			_setLowPriority = value;
			UpdateShortcut("SetLowPriority", value);
		}
	}

	public string SetMediumPriority
	{
		get
		{
			return _setMediumPriority;
		}
		set
		{
			_setMediumPriority = value;
			UpdateShortcut("SetMediumPriority", value);
		}
	}

	public string SetHighPriority
	{
		get
		{
			return _setHighPriority;
		}
		set
		{
			_setHighPriority = value;
			UpdateShortcut("SetHighPriority", value);
		}
	}

	public string Print
	{
		get
		{
			return _print;
		}
		set
		{
			_print = value;
			UpdateShortcut("Print", value);
		}
	}

	public string PrintDetail
	{
		get
		{
			return _printDetail;
		}
		set
		{
			_printDetail = value;
			UpdateShortcut("PrintDetail", value);
		}
	}

	public string JumpAll
	{
		get
		{
			return _jumpAll;
		}
		set
		{
			_jumpAll = value;
			UpdateShortcut("JumpAll", value);
		}
	}

	public string JumpToday
	{
		get
		{
			return _jumpToday;
		}
		set
		{
			_jumpToday = value;
			UpdateShortcut("JumpToday", value);
		}
	}

	public string JumpTomorrow
	{
		get
		{
			return _jumpTomorrow;
		}
		set
		{
			_jumpTomorrow = value;
			UpdateShortcut("JumpTomorrow", value);
		}
	}

	public string JumpWeek
	{
		get
		{
			return _jumpWeek;
		}
		set
		{
			_jumpWeek = value;
			UpdateShortcut("JumpWeek", value);
		}
	}

	public string JumpCalendar
	{
		get
		{
			return _jumpCalendar;
		}
		set
		{
			_jumpCalendar = value;
			UpdateShortcut("JumpCalendar", value);
		}
	}

	public string JumpAssign
	{
		get
		{
			return _jumpAssign;
		}
		set
		{
			_jumpAssign = value;
			UpdateShortcut("JumpAssign", value);
		}
	}

	public string JumpInbox
	{
		get
		{
			return _jumpInbox;
		}
		set
		{
			_jumpInbox = value;
			UpdateShortcut("JumpInbox", value);
		}
	}

	public string JumpComplete
	{
		get
		{
			return _jumpComplete;
		}
		set
		{
			_jumpComplete = value;
			UpdateShortcut("JumpComplete", value);
		}
	}

	public string JumpSummary
	{
		get
		{
			return _jumpSummary;
		}
		set
		{
			_jumpSummary = value;
			UpdateShortcut("JumpSummary", value);
		}
	}

	public string JumpTrash
	{
		get
		{
			return _jumpTrash;
		}
		set
		{
			_jumpTrash = value;
			UpdateShortcut("JumpTrash", value);
		}
	}

	public string JumpAbandon
	{
		get
		{
			return _jumpAbandon;
		}
		set
		{
			_jumpAbandon = value;
			UpdateShortcut("JumpAbandon", value);
		}
	}

	public string JumpHabit
	{
		get
		{
			return _jumpHabit;
		}
		set
		{
			_jumpHabit = value;
			UpdateShortcut("JumpHabit", value);
		}
	}

	public string ToggleParallelView
	{
		get
		{
			return _toggleParallelView;
		}
		set
		{
			_toggleParallelView = value;
			UpdateShortcut("ToggleParallelView", value);
		}
	}

	public string AINewConversation
	{
		get
		{
			return _aiNewConversation;
		}
		set
		{
			_aiNewConversation = value;
			UpdateShortcut("AINewConversation", value);
		}
	}

	public string AIToggleFloatingWindow
	{
		get
		{
			return _aiToggleFloatingWindow;
		}
		set
		{
			_aiToggleFloatingWindow = value;
			UpdateShortcut("AIToggleFloatingWindow", value);
		}
	}

	public string PinTask
	{
		get
		{
			return _pinTask;
		}
		set
		{
			_pinTask = value;
			UpdateShortcut("PinTask", value);
		}
	}

	public string OpenSticky
	{
		get
		{
			return _openSticky;
		}
		set
		{
			_openSticky = value;
			UpdateShortcut("OpenSticky", value);
		}
	}

	public string StickyColor
	{
		get
		{
			return _stickyColor;
		}
		set
		{
			_stickyColor = value;
			UpdateShortcut("StickyColor", value);
		}
	}

	public string StickyAlignTop
	{
		get
		{
			return _stickyAlignTop;
		}
		set
		{
			_stickyAlignTop = value;
			UpdateShortcut("StickyAlignTop", value);
		}
	}

	public string StickyAlignLeft
	{
		get
		{
			return _stickyAlignLeft;
		}
		set
		{
			_stickyAlignLeft = value;
			UpdateShortcut("StickyAlignLeft", value);
		}
	}

	public string StickyAlignRight
	{
		get
		{
			return _stickyAlignRight;
		}
		set
		{
			_stickyAlignRight = value;
			UpdateShortcut("StickyAlignRight", value);
		}
	}

	public string StickyCollapse
	{
		get
		{
			return _stickyCollapse;
		}
		set
		{
			_stickyCollapse = value;
			UpdateShortcut("StickyCollapse", value);
		}
	}

	public string PinSticky
	{
		get
		{
			return _pinSticky;
		}
		set
		{
			_pinSticky = value;
			UpdateShortcut("PinSticky", value);
		}
	}

	public string ContinueAndPause
	{
		get
		{
			return _continueAndPause;
		}
		set
		{
			_continueAndPause = value;
			UpdateShortcut("ContinueAndPause", value);
		}
	}

	public string StartAndDrop
	{
		get
		{
			return _startAndDrop;
		}
		set
		{
			_startAndDrop = value;
			UpdateShortcut("StartAndDrop", value);
		}
	}

	public string OpenSetting
	{
		get
		{
			return _openSetting;
		}
		set
		{
			_openSetting = value;
			UpdateShortcut("OpenSetting", value);
		}
	}

	private void UpdateShortcut(string property, string value)
	{
		if (CanNotify)
		{
			if (!property.StartsWith("Tab0"))
			{
				KeyBindingManager.ResetTabShortCut();
			}
			KeyBindingManager.ChangeShortCut(property, value);
		}
	}

	[Obsolete]
	public static ShortcutModel GetRegeditModel()
	{
		ShortcutModel shortcutModel = new ShortcutModel();
		RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey("Dida")?.OpenSubKey("Shortcuts");
		if (registryKey != null)
		{
			foreach (PropertyInfo item in shortcutModel.GetType().GetProperties().ToList())
			{
				object value = registryKey.GetValue(item.Name);
				if (value != null)
				{
					item.SetValue(shortcutModel, value);
				}
			}
		}
		return shortcutModel;
	}

	public bool ExistKey(string hotkey)
	{
		return GetType().GetProperties().Any((PropertyInfo property) => !property.Name.StartsWith("Tab0") && (string)property.GetValue(this) == hotkey);
	}

	public void ClearShortcut(string name)
	{
		SetPropertyValue(name, "");
	}

	public List<string> GetAllPropertyNames()
	{
		return (from i in GetType().GetProperties()
			select i.Name).ToList();
	}

	public string GetPropertyValue(string key)
	{
		return (string)GetType().GetProperty(key)?.GetValue(this);
	}

	public void SetPropertyValue(string key, string value)
	{
		GetType().GetProperty(key)?.SetValue(this, value);
	}

	public void Reset()
	{
		ShortcutModel shortCut = new ShortcutModel();
		CopyProperties(shortCut);
	}

	public void CopyProperties(ShortcutModel shortCut)
	{
		if (shortCut == null)
		{
			return;
		}
		PropertyInfo[] properties = GetType().GetProperties();
		PropertyInfo[] array = properties;
		foreach (PropertyInfo propertyInfo in array)
		{
			if (!propertyInfo.Name.Contains("Tab0") && !propertyInfo.Name.Contains("Folded"))
			{
				propertyInfo.SetValue(this, "");
			}
		}
		array = properties;
		foreach (PropertyInfo propertyInfo2 in array)
		{
			if (!propertyInfo2.Name.Contains("Tab0") && !propertyInfo2.Name.Contains("Folded"))
			{
				object value = propertyInfo2.GetValue(shortCut);
				if (propertyInfo2.GetValue(this) != value)
				{
					propertyInfo2.SetValue(this, value);
				}
			}
		}
	}
}
