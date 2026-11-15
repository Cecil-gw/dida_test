using System;
using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Util;

public class EmojiKeyLists
{
	public string CnKeys;

	public string EnKeys;

	public string CiKeys;

	public void AddToList(string s, string listName)
	{
		switch (listName)
		{
		case "CnKeys":
			CnKeys = s;
			break;
		case "EnKeys":
			EnKeys = s;
			break;
		case "CiKeys":
			CiKeys = s;
			break;
		}
	}

	public bool CheckKeys(List<string> keys)
	{
		keys.RemoveAll(string.IsNullOrEmpty);
		if (!CnMatch() && !CiMatch())
		{
			return EnMatch();
		}
		return true;
		bool CiMatch()
		{
			return keys.All((string key) => CiKeys != null && CiKeys.Contains(key));
		}
		bool CnMatch()
		{
			return keys.All((string key) => CnKeys != null && CnKeys.Contains(key));
		}
		bool EnMatch()
		{
			return keys.All(delegate(string key)
			{
				if (key.Length < 3 && key.All(CharUtils.IsAbc))
				{
					return StartWith(EnKeys, key);
				}
				return EnKeys != null && EnKeys.Contains(key);
			});
		}
		static bool StartWith(string text, string val)
		{
			if (text == null)
			{
				return false;
			}
			int num = text.IndexOf(val, StringComparison.Ordinal);
			if (num < 0)
			{
				return false;
			}
			if (num == 0)
			{
				return true;
			}
			if (text[num - 1] != ',')
			{
				return text[num - 1] == ' ';
			}
			return true;
		}
	}
}
