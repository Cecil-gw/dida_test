using System;
using System.Collections.Generic;

namespace ticktick_WPF.Util;

public struct Pinyin
{
	public string Text;

	public string Inits;

	public List<int> EachStartIndex;

	private readonly List<string[]> _syllables;

	public Pinyin(string text, string inits, List<int> indexes)
	{
		Text = text;
		Inits = inits;
		EachStartIndex = indexes;
		_syllables = null;
	}

	internal Pinyin(string text, string inits, List<int> indexes, List<string[]> syllables)
	{
		Text = text;
		Inits = inits;
		EachStartIndex = indexes;
		_syllables = syllables;
	}

	public bool Contains(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return true;
		}
		if (!InitialsContain(text))
		{
			return PinyinContains(text);
		}
		return true;
	}

	private bool InitialsContain(string text)
	{
		if (_syllables == null)
		{
			return Inits?.StartsWith(text) ?? false;
		}
		return MatchSyllables(text, 0, 0, 0, initialsOnly: true, new HashSet<long>());
	}

	private bool PinyinContains(string text)
	{
		if (_syllables != null)
		{
			for (int i = 0; i < _syllables.Count; i++)
			{
				if (MatchSyllables(text, i, i, 0, initialsOnly: false, new HashSet<long>()))
				{
					return true;
				}
			}
			return false;
		}
		if (Text == text)
		{
			return true;
		}
		if (Text == null || EachStartIndex == null)
		{
			return false;
		}
		int num = Text.IndexOf(text, StringComparison.Ordinal);
		if (num < 0)
		{
			return false;
		}
		if (num == 0)
		{
			return true;
		}
		int num2 = num + text.Length;
		for (int j = 0; j < EachStartIndex.Count - 1; j++)
		{
			if (EachStartIndex[j] == num && EachStartIndex[j + 1] < num2)
			{
				return true;
			}
		}
		return false;
	}

	private bool MatchSyllables(string text, int startSyllableIndex, int syllableIndex, int textIndex, bool initialsOnly, HashSet<long> failedStates)
	{
		if (textIndex == text.Length)
		{
			return true;
		}
		if (syllableIndex >= _syllables.Count)
		{
			return false;
		}
		long item = ((long)syllableIndex << 32) | (uint)textIndex;
		if (failedStates.Contains(item))
		{
			return false;
		}
		string[] array = _syllables[syllableIndex];
		foreach (string text2 in array)
		{
			string text3 = (initialsOnly ? text2.Substring(0, 1) : text2);
			int num = text.Length - textIndex;
			int length = Math.Min(num, text3.Length);
			if (string.CompareOrdinal(text, textIndex, text3, 0, length) != 0)
			{
				continue;
			}
			if (num <= text3.Length)
			{
				if (startSyllableIndex == 0 || syllableIndex > startSyllableIndex)
				{
					return true;
				}
			}
			else if (MatchSyllables(text, startSyllableIndex, syllableIndex + 1, textIndex + text3.Length, initialsOnly, failedStates))
			{
				return true;
			}
		}
		failedStates.Add(item);
		return false;
	}
}
