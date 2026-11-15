using System;
using System.Linq;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Lock;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Util;

public class HotKeyUtils
{
	public Key Key;

	public ModifierKeys Modifiers;

	public HotKeyUtils()
	{
	}

	public HotKeyUtils(string hotkeyString)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		int num = 0;
		string[] array = hotkeyString.Split('+');
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			switch (array2[i].ToLower())
			{
			case "ctrl":
				if (!flag)
				{
					flag = true;
					num++;
				}
				break;
			case "alt":
				if (!flag2)
				{
					flag2 = true;
					num += 2;
				}
				break;
			case "shift":
				if (!flag3)
				{
					flag3 = true;
					num += 4;
				}
				break;
			}
		}
		string text = array.LastOrDefault();
		if (text == "~")
		{
			text = "Oem3";
		}
		if (Enum.TryParse<Key>(text, out var result))
		{
			Key = result;
		}
		GetModifierKeys(num);
	}

	private void GetModifierKeys(int modifierCount)
	{
		switch (modifierCount)
		{
		case 0:
			Modifiers = ModifierKeys.None;
			break;
		case 1:
			Modifiers = ModifierKeys.Control;
			break;
		case 2:
			Modifiers = ModifierKeys.Alt;
			break;
		case 3:
			Modifiers = ModifierKeys.Alt | ModifierKeys.Control;
			break;
		case 4:
			Modifiers = ModifierKeys.Shift;
			break;
		case 5:
			Modifiers = ModifierKeys.Control | ModifierKeys.Shift;
			break;
		case 6:
			Modifiers = ModifierKeys.Alt | ModifierKeys.Shift;
			break;
		case 7:
			Modifiers = ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift;
			break;
		}
	}

	public static void ResignHotKey()
	{
		RegisterOpenAppShortcut();
		RegisterQuickAddShortcut();
		RegisterPinShortcut();
		if (LocalSettings.Settings.EnableFocus)
		{
			RegisterPomoShortcut();
			RegisterPomoBeginShortcut();
			RegisterPomoPauseShortcut();
		}
		RegisterLockShortcut();
		RegisterCreateSticky();
		RegisterShowHideSticky();
	}

	public static void ResignHotKey(string key)
	{
		if (key == null)
		{
			return;
		}
		switch (key.Length)
		{
		case 12:
			switch (key[0])
			{
			case 'P':
				if (key == "PomoShortcut" && LocalSettings.Settings.EnableFocus)
				{
					RegisterPomoShortcut();
				}
				break;
			case 'S':
				if (key == "StartAndDrop" && LocalSettings.Settings.EnableFocus)
				{
					RegisterPomoBeginShortcut();
				}
				break;
			case 'L':
				if (key == "LockShortcut")
				{
					RegisterLockShortcut();
				}
				break;
			}
			break;
		case 19:
			if (key == "ShortcutOpenOrClose")
			{
				TryRegHotKey("ShortcutOpenOrClose", LocalSettings.Settings.ShortcutOpenOrClose, OpenOrCloseWindowHotKey);
			}
			break;
		case 15:
			if (key == "ShortcutAddTask")
			{
				TryRegHotKey("ShortcutAddTask", LocalSettings.Settings.ShortcutAddTask, AddTaskHotKey);
			}
			break;
		case 11:
			if (key == "ShortcutPin")
			{
				TryRegHotKey("ShortcutPin", LocalSettings.Settings.ShortcutPin, PinHotKey);
			}
			break;
		case 16:
			if (key == "ContinueAndPause" && LocalSettings.Settings.EnableFocus)
			{
				RegisterPomoPauseShortcut();
			}
			break;
		case 20:
			if (key == "CreateStickyShortcut")
			{
				RegisterCreateSticky();
			}
			break;
		case 22:
			if (key == "ShowHideStickyShortcut")
			{
				RegisterShowHideSticky();
			}
			break;
		case 13:
		case 14:
		case 17:
		case 18:
		case 21:
			break;
		}
	}

	private static void RegisterCreateSticky()
	{
		TryRegHotKey("CreateStickyShortcut", LocalSettings.Settings.CreateStickyShortCut, CreateStickyHotKey);
	}

	private static void RegisterShowHideSticky()
	{
		TryRegHotKey("ShowHideStickyShortcut", LocalSettings.Settings.ShowHideStickyShortCut, ShowHideStickyHotKey);
	}

	private static void RegisterPinShortcut()
	{
		TryRegHotKey("ShortcutPin", LocalSettings.Settings.ShortcutPin, PinHotKey);
	}

	private static void RegisterQuickAddShortcut()
	{
		TryRegHotKey("ShortcutAddTask", LocalSettings.Settings.ShortcutAddTask, AddTaskHotKey);
	}

	private static void RegisterOpenAppShortcut()
	{
		TryRegHotKey("ShortcutOpenOrClose", LocalSettings.Settings.ShortcutOpenOrClose, OpenOrCloseWindowHotKey);
	}

	private static void ShowHideStickyHotKey(object sender, HotkeyEventArgs e)
	{
		UserActCollectUtils.AddShortCutEvent("global_action", "show_hide_all_stickys");
		TaskStickyWindow.ShowHideAllStickyStatic();
	}

	private static void CreateStickyHotKey(object sender, HotkeyEventArgs e)
	{
		UserActCollectUtils.AddShortCutEvent("global_action", "new_sticky_note");
		TaskStickyWindow.CreateNewStickyStatic();
	}

	private static void TryRegHotKey(string name, string hotKeyString, EventHandler<HotkeyEventArgs> handler)
	{
		if (string.IsNullOrWhiteSpace(hotKeyString))
		{
			HotkeyManager.Current.Remove(name);
			return;
		}
		HotKeyUtils hotKeyUtils = new HotKeyUtils(hotKeyString.Replace(" ", ""));
		if (hotKeyUtils.Key != Key.None && hotKeyUtils.Modifiers != ModifierKeys.None)
		{
			try
			{
				HotkeyManager.Current?.AddOrReplace(name, hotKeyUtils.Key, hotKeyUtils.Modifiers, handler);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		HotkeyManager.Current?.Remove(name);
	}

	public static void RegisterPomoShortcut()
	{
		TryRegHotKey("PomoShortcut", LocalSettings.Settings.PomoShortcut, ShowOrHidePomoHandler);
	}

	public static void RegisterPomoBeginShortcut()
	{
		TryRegHotKey("StartAndDrop", LocalSettings.Settings.ShortCutModel.StartAndDrop, StartOrEndPomoHandler);
	}

	public static void RegisterPomoPauseShortcut()
	{
		TryRegHotKey("ContinueAndPause", LocalSettings.Settings.ShortCutModel.ContinueAndPause, PauseOrContinuePomoHandler);
	}

	private static async void RegisterLockShortcut()
	{
		string lockShortcut = LocalSettings.Settings.LockShortcut;
		TryRegHotKey("LockShortcut", lockShortcut, LockAppHandler);
	}

	private static async void LockAppHandler(object sender, HotkeyEventArgs e)
	{
		bool num = await AppLockCache.GetAppLocked();
		UserActCollectUtils.AddShortCutEvent("global_action", "lock_unlock_app");
		if (!num)
		{
			App.Instance.TryLockApp();
		}
		else
		{
			AppUnlockWindow.TryUnlockApp(MainWindowManager.Window.ShowWindow);
		}
	}

	private static void ShowOrHidePomoHandler(object sender, HotkeyEventArgs e)
	{
		if (LocalSettings.Settings.EnableFocus || string.IsNullOrEmpty(LocalSettings.Settings.PomoShortcut))
		{
			UserActCollectUtils.AddShortCutEvent("global_action", "open_close_focus_window");
			TickFocusManager.HideOrShowFocusWidget(force: false, 0);
		}
	}

	private static void StartOrEndPomoHandler(object sender, HotkeyEventArgs e)
	{
		if (LocalSettings.Settings.EnableFocus)
		{
			TickFocusManager.StartOrDrop();
		}
	}

	private static void PauseOrContinuePomoHandler(object sender, HotkeyEventArgs e)
	{
		if (LocalSettings.Settings.EnableFocus)
		{
			TickFocusManager.ContinueOrPause();
		}
	}

	private static void PinHotKey(object sender, HotkeyEventArgs e)
	{
		UserActCollectUtils.AddShortCutEvent("global_action", "pin_cancelpin_app");
		LocalSettings.Settings.MainWindowTopmost = !LocalSettings.Settings.MainWindowTopmost;
	}

	private static void AddTaskHotKey(object sender, HotkeyEventArgs e)
	{
		if (!string.IsNullOrEmpty(LocalSettings.Settings.LoginUserId))
		{
			UserActCollectUtils.AddShortCutEvent("global_action", "quick_add");
			MainWindowManager.Window.OpenAddTaskWindow();
		}
	}

	private static async void OpenOrCloseWindowHotKey(object sender, HotkeyEventArgs e)
	{
		UserActCollectUtils.AddShortCutEvent("global_action", "showhide_main_window");
		MainWindowManager.Window.ShowOrHideWindow();
	}

	public static void RemoveHotkeys()
	{
		HotkeyManager.Current.Remove("ShortcutOpenOrClose");
		HotkeyManager.Current.Remove("ShortcutAddTask");
		HotkeyManager.Current.Remove("PomoShortcut");
		HotkeyManager.Current.Remove("StartAndDrop");
		HotkeyManager.Current.Remove("ContinueAndPause");
		HotkeyManager.Current.Remove("LockShortcut");
		HotkeyManager.Current.Remove("ShortcutPin");
		HotkeyManager.Current.Remove("CreateStickyShortcut");
	}
}
