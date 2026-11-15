using System;
using System.Text.RegularExpressions;
using Emoji.Wpf;

namespace ticktick_WPF.Util;

public static class EmojiHelper
{
	public static bool StartWithEmoji(string text)
	{
		string emojiIcon = GetEmojiIcon(text);
		if (!string.IsNullOrEmpty(emojiIcon))
		{
			return text.StartsWith(emojiIcon);
		}
		return false;
	}

	public static string GetFirstEmoji(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		string emojiIcon = GetEmojiIcon(text);
		if (!string.IsNullOrEmpty(emojiIcon) && text.StartsWith(emojiIcon))
		{
			return emojiIcon;
		}
		return null;
	}

	public static string GetEmojiIcon(string text)
	{
		try
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			Match match = EmojiData.MatchOne.Match(text);
			return match.Success ? match.Groups[0].Value : string.Empty;
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}
}
