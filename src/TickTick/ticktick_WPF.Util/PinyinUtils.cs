using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Emoji.Wpf;

namespace ticktick_WPF.Util;

public static class PinyinUtils
{
	private static Dictionary<string, string[]> _pinyin;

	private static ConcurrentDictionary<string, Pinyin> _text2Pinyin = new ConcurrentDictionary<string, Pinyin>();

	public static Pinyin ToPinyin(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return new Pinyin(string.Empty, string.Empty, new List<int>());
		}
		if (_text2Pinyin.TryGetValue(text, out var value))
		{
			return value;
		}
		if (_pinyin == null)
		{
			InitData();
		}
		text = EmojiData.MatchMultiple.Replace(text, "");
		List<int> list = new List<int>();
		if (_pinyin != null && _pinyin.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			List<string[]> list2 = new List<string[]>();
			int num = 0;
			string text2 = text;
			for (int i = 0; i < text2.Length; i++)
			{
				char c = text2[i];
				list.Add(num);
				if (_pinyin.ContainsKey(c.ToString()))
				{
					string[] array = _pinyin[c.ToString()];
					string text3 = array[0];
					stringBuilder.Append(text3);
					stringBuilder2.Append(text3.Substring(0, 1));
					num += text3.Length;
					list2.Add(array);
				}
				else
				{
					string text4 = c.ToString();
					stringBuilder.Append(text4);
					stringBuilder2.Append(text4);
					num++;
					list2.Add(new string[1] { text4 });
				}
			}
			Pinyin pinyin = new Pinyin(stringBuilder.ToString(), stringBuilder2.ToString(), list, list2);
			_text2Pinyin.TryAdd(text, pinyin);
			return pinyin;
		}
		return new Pinyin(string.Empty, string.Empty, new List<int>());
	}

	private static void InitData()
	{
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (directoryName == null)
		{
			return;
		}
		string path = Path.Combine(directoryName, "pinyin.txt");
		if (!File.Exists(path))
		{
			return;
		}
		_pinyin = new Dictionary<string, string[]>();
		try
		{
			string[] array = File.ReadAllLines(path);
			foreach (string text in array)
			{
				if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("#"))
				{
					string[] array2 = text.Split(' ');
					if (array2.Length == 2 && !_pinyin.ContainsKey(array2[0]))
					{
						_pinyin.Add(array2[0], array2[1].Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries));
					}
				}
			}
		}
		catch
		{
		}
	}
}
