using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public static class KeyBindingManager
{
	public static ConcurrentDictionary<string, List<KeyBinding>> AppKeyBinding = new ConcurrentDictionary<string, List<KeyBinding>>();

	public static event EventHandler<string> ShortCutChanged;

	public static void TryAddKeyBinding(string key, KeyBinding kb)
	{
		if (kb == null)
		{
			return;
		}
		try
		{
			AddKeyBinding(key, kb);
		}
		catch (Exception)
		{
			string propertyValue = new ShortcutModel().GetPropertyValue(key);
			LocalSettings.Settings.ShortCutModel.SetPropertyValue(key, propertyValue);
			AddKeyBinding(key, kb);
		}
	}

	private static void AddKeyBinding(string key, KeyBinding kb)
	{
		SetKeyGesture(key, kb);
		if (AppKeyBinding.ContainsKey(key))
		{
			AppKeyBinding[key].Add(kb);
			return;
		}
		AppKeyBinding.TryAdd(key, new List<KeyBinding> { kb });
	}

	public static void ChangeShortCut(string key, string newShort)
	{
		ThreadUtil.DetachedRunOnUiBackThread(delegate
		{
			if (AppKeyBinding.ContainsKey(key))
			{
				foreach (KeyBinding item in AppKeyBinding[key])
				{
					KeyGesture keyGesture = GetKeyGesture(newShort);
					if (keyGesture != null && keyGesture.Modifiers != ModifierKeys.None)
					{
						item.Key = Key.None;
						item.Modifiers = ModifierKeys.None;
					}
					item.Gesture = keyGesture ?? new KeyGesture(Key.None);
				}
			}
			KeyBindingManager.ShortCutChanged?.Invoke(null, key);
		});
	}

	private static KeyGesture GetKeyGesture(string newShort)
	{
		if (string.IsNullOrEmpty(newShort))
		{
			return null;
		}
		string[] source = newShort.Split('+');
		List<string> list = source.Where((string p) => p != "Ctrl" && p != "Shift" && p != "Alt" && p != "Windows").ToList();
		string text = ((list.Count == 1) ? list[0] : string.Empty);
		if (text == "~")
		{
			text = "Oem3";
		}
		if (Enum.TryParse<Key>(text, out var result))
		{
			ModifierKeys modifierKeys = (source.Contains("Ctrl") ? ModifierKeys.Control : ModifierKeys.None);
			if (source.Contains("Shift"))
			{
				modifierKeys |= ModifierKeys.Shift;
			}
			if (source.Contains("Alt"))
			{
				modifierKeys |= ModifierKeys.Alt;
			}
			if (source.Contains("Windows"))
			{
				modifierKeys |= ModifierKeys.Windows;
			}
			return modifierKeys switch
			{
				ModifierKeys.Shift => null, 
				ModifierKeys.None => new KeyGesture(result), 
				_ => new KeyGesture(result, modifierKeys), 
			};
		}
		return null;
	}

	public static void SetKeyGesture(string key, KeyBinding kb)
	{
		KeyGesture keyGesture = GetKeyGesture(LocalSettings.Settings.ShortCutModel.GetPropertyValue(key));
		if (keyGesture != null && keyGesture.Modifiers != ModifierKeys.None)
		{
			kb.Key = Key.None;
			kb.Modifiers = ModifierKeys.None;
		}
		kb.Gesture = keyGesture ?? new KeyGesture(Key.None);
	}

	public static bool HasCtrlNumKeyBinding()
	{
		foreach (string allPropertyName in LocalSettings.Settings.ShortCutModel.GetAllPropertyNames())
		{
			if (!allPropertyName.StartsWith("Tab0"))
			{
				string propertyValue = LocalSettings.Settings.ShortCutModel.GetPropertyValue(allPropertyName);
				if (propertyValue != null && propertyValue.Length == 7 && propertyValue.ToLower().StartsWith("ctrl+d"))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void RemoveKeyBinding(string key, KeyBinding keyBinding)
	{
		if (AppKeyBinding.ContainsKey(key))
		{
			AppKeyBinding[key].Remove(keyBinding);
		}
	}

	public static void ResetTabShortCut()
	{
		bool flag = HasCtrlNumKeyBinding();
		bool flag2 = LocalSettings.Settings.ShortCutModel.Tab01 == "Ctrl+D1";
		for (int i = 1; i <= 9; i++)
		{
			if (flag2 && flag)
			{
				LocalSettings.Settings.ShortCutModel.SetPropertyValue($"Tab0{i}", string.Empty);
			}
			if (!flag2 && !flag)
			{
				LocalSettings.Settings.ShortCutModel.SetPropertyValue($"Tab0{i}", "Ctrl+D" + i);
			}
		}
	}
}
