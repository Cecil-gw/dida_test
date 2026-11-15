using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public static class MarkdownTextStatistics
{
	private static readonly Regex WordSeparatorRegex = new Regex("\\s+");

	private static readonly Regex CjkWordCharacterRegex = new Regex("[\\u4E00-\\u9FFF\\u3400-\\u4DBF\\uF900-\\uFAFF\\uFF01-\\uFF60\\u3000-\\u303F\\u3040-\\u309F\\u30A0-\\u30FF\\u1100-\\u11FF\\u3130-\\u318F\\uAC00-\\uD7AF]");

	public static MarkdownTextStatisticsResult Count(string markdown)
	{
		string plainText = MarkdownSemanticConverter.GetPlainText(RemoveMarkdownImages(markdown ?? string.Empty));
		return new MarkdownTextStatisticsResult
		{
			PlainText = plainText,
			WordCount = CountWords(plainText),
			CharacterCount = plainText.Count((char character) => character != '\r' && character != '\n')
		};
	}

	private static string RemoveMarkdownImages(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return string.Empty;
		}
		List<Match> list = (from Match match in MarkDownEditor.AttachmentRegex.Matches(markdown)
			where MarkDownEditor.IsUnescapedAttachmentMatch(markdown, match)
			orderby match.Index descending
			select match).ToList();
		if (list.Count == 0)
		{
			return markdown;
		}
		StringBuilder stringBuilder = new StringBuilder(markdown);
		foreach (Match item in list)
		{
			stringBuilder.Remove(item.Index, item.Length);
		}
		return stringBuilder.ToString();
	}

	private static int CountWords(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int num = 0;
		string[] array = WordSeparatorRegex.Split(text);
		foreach (string text2 in array)
		{
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			int num2 = 0;
			foreach (Match item in CjkWordCharacterRegex.Matches(text2))
			{
				if (num2 < item.Index)
				{
					num++;
				}
				num++;
				num2 = item.Index + item.Length;
			}
			if (num2 < text2.Length)
			{
				num++;
			}
		}
		return num;
	}
}
