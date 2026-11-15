using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace ticktick_WPF.Views.Config;

public class HotkeyModel
{
	private readonly Dictionary<Key, string> _specialSymbolDictionary = new Dictionary<Key, string>
	{
		{
			Key.Space,
			"Space"
		},
		{
			Key.Oem3,
			"~"
		}
	};

	public bool Alt { get; set; }

	public bool Shift { get; set; }

	public bool Win { get; set; }

	public bool Ctrl { get; set; }

	public Key CharKey { get; set; }

	public ModifierKeys ModifierKeys
	{
		get
		{
			ModifierKeys modifierKeys = ModifierKeys.None;
			if (Alt)
			{
				modifierKeys = ModifierKeys.Alt;
			}
			if (Shift)
			{
				modifierKeys |= ModifierKeys.Shift;
			}
			if (Win)
			{
				modifierKeys |= ModifierKeys.Windows;
			}
			if (Ctrl)
			{
				modifierKeys |= ModifierKeys.Control;
			}
			return modifierKeys;
		}
	}

	public HotkeyModel(string hotkeyString)
	{
		Parse(hotkeyString);
	}

	public HotkeyModel(bool alt, bool shift, bool win, bool ctrl, Key key)
	{
		Alt = alt;
		Shift = shift;
		Win = win;
		Ctrl = ctrl;
		CharKey = key;
	}

	private void Parse(string hotkeyString)
	{
		if (string.IsNullOrEmpty(hotkeyString))
		{
			return;
		}
		List<string> list = hotkeyString.Replace(" ", "").Split('+').ToList();
		if (list.Contains("Alt"))
		{
			Alt = true;
			list.Remove("Alt");
		}
		if (list.Contains("Shift"))
		{
			Shift = true;
			list.Remove("Shift");
		}
		if (list.Contains("Win"))
		{
			Win = true;
			list.Remove("Win");
		}
		if (list.Contains("Ctrl"))
		{
			Ctrl = true;
			list.Remove("Ctrl");
		}
		if (list.Count <= 0)
		{
			return;
		}
		string charKey = list[0];
		KeyValuePair<Key, string>? keyValuePair = _specialSymbolDictionary.FirstOrDefault((KeyValuePair<Key, string> pair) => pair.Value == charKey);
		if (keyValuePair.Value.Value != null)
		{
			CharKey = keyValuePair.Value.Key;
			return;
		}
		try
		{
			CharKey = (Key)Enum.Parse(typeof(Key), charKey);
		}
		catch (ArgumentException)
		{
		}
	}

	public override string ToString()
	{
		string text = string.Empty;
		if (Ctrl)
		{
			text += "Ctrl+";
		}
		if (Alt)
		{
			text += "Alt+";
		}
		if (Shift)
		{
			text += "Shift+";
		}
		if (Win)
		{
			text += "Win+";
		}
		if (CharKey != Key.None)
		{
			text += (_specialSymbolDictionary.ContainsKey(CharKey) ? _specialSymbolDictionary[CharKey] : ((CharKey >= Key.D0 && CharKey <= Key.D9) ? CharKey.ToString().Replace("D", "") : CharKey.ToString()));
		}
		else if (!string.IsNullOrEmpty(text))
		{
			text = text.Remove(text.Length - 3);
		}
		return HandleUpDownText(text);
	}

	public string ToRealString()
	{
		string text = string.Empty;
		if (Ctrl)
		{
			text += "Ctrl + ";
		}
		if (Alt)
		{
			text += "Alt + ";
		}
		if (Shift)
		{
			text += "Shift + ";
		}
		if (Win)
		{
			text += "Win + ";
		}
		if (CharKey != Key.None)
		{
			text += (_specialSymbolDictionary.ContainsKey(CharKey) ? _specialSymbolDictionary[CharKey] : CharKey.ToString());
		}
		else if (!string.IsNullOrEmpty(text))
		{
			text = text.Remove(text.Length - 3);
		}
		return text;
	}

	public static string HandleUpDownText(string text)
	{
		if (text.Contains("Up") && !text.Contains("PageUp"))
		{
			text = text.Replace("Up", "↑");
		}
		if (text.Contains("Down") && !text.Contains("PageDown"))
		{
			text = text.Replace("Down", "↓");
		}
		if (text.Contains("Left"))
		{
			text = text.Replace("Left", "←");
		}
		if (text.Contains("Right"))
		{
			text = text.Replace("Right", "→");
		}
		return text;
	}
}
