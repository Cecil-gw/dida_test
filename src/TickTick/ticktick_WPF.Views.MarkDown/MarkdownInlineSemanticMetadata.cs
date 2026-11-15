using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal static class MarkdownInlineSemanticMetadata
{
	private const string Prefix = "<!--tt_inline_sem:";

	private const string Suffix = "-->";

	public static string GetPersistenceIdentity(MarkdownInlineDisplaySegment mark)
	{
		if (mark == null)
		{
			return string.Empty;
		}
		if (mark.Kind == MarkdownMarkKind.Link)
		{
			return mark.Kind.ToString() + ":" + (mark.Url ?? string.Empty);
		}
		if (mark.Kind != MarkdownMarkKind.Highlight)
		{
			return mark.Kind.ToString();
		}
		return mark.Kind.ToString() + ":" + (mark.Color ?? string.Empty);
	}

	public static bool ArePersistenceMarksEquivalent(MarkdownInlineDisplaySegment left, MarkdownInlineDisplaySegment right)
	{
		return string.Equals(GetPersistenceIdentity(left), GetPersistenceIdentity(right), StringComparison.Ordinal);
	}

	public static int GetPersistenceOrder(MarkdownMarkKind kind)
	{
		return kind switch
		{
			MarkdownMarkKind.Link => 0, 
			MarkdownMarkKind.Highlight => 30, 
			MarkdownMarkKind.Strikethrough => 40, 
			MarkdownMarkKind.Strong => 50, 
			MarkdownMarkKind.Underline => 60, 
			MarkdownMarkKind.Emphasis => 70, 
			MarkdownMarkKind.Code => 80, 
			_ => 100, 
		};
	}

	public static bool CanSerialize(MarkdownInlineDisplaySegment mark)
	{
		if (mark == null || mark.SourceLength <= 0 || mark.Kind == MarkdownMarkKind.LiteralInlineSyntax)
		{
			return false;
		}
		if (mark.Kind != MarkdownMarkKind.Link)
		{
			return true;
		}
		return !string.IsNullOrEmpty(mark.Url);
	}

	public static string Serialize(IEnumerable<MarkdownInlineDisplaySegment> marks)
	{
		List<string> list = (from mark in marks?.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.SourceLength > 0)
			orderby mark.SourcePosition, GetPersistenceOrder(mark.Kind)
			select string.Join(":", (int)mark.Kind, mark.SourcePosition, mark.SourceLength, EscapeValue(mark.Color), EscapeValue(mark.Url))).ToList();
		if (list == null || list.Count == 0)
		{
			return string.Empty;
		}
		return "<!--tt_inline_sem:" + string.Join(";", list) + "-->";
	}

	public static List<MarkdownInlineDisplaySegment> Extract(ref string rawText)
	{
		rawText = rawText ?? string.Empty;
		List<MarkdownInlineDisplaySegment> list = new List<MarkdownInlineDisplaySegment>();
		while (true)
		{
			int num = rawText.LastIndexOf("-->", StringComparison.Ordinal);
			if (num < 0)
			{
				break;
			}
			int num2 = rawText.LastIndexOf("<!--tt_inline_sem:", num, StringComparison.Ordinal);
			if (num2 < 0)
			{
				break;
			}
			int num3 = num2 + "<!--tt_inline_sem:".Length;
			string text = rawText.Substring(num3, num - num3);
			rawText = rawText.Remove(num2, num + "-->".Length - num2);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			foreach (string item in SplitEntries(text))
			{
				string[] array = item.Split(new char[1] { ':' }, 5);
				if (array.Length >= 3 && int.TryParse(array[0], out var result) && Enum.IsDefined(typeof(MarkdownMarkKind), result) && int.TryParse(array[1], out var result2) && int.TryParse(array[2], out var result3) && result3 > 0)
				{
					list.Add(new MarkdownInlineDisplaySegment
					{
						Kind = (MarkdownMarkKind)result,
						SourcePosition = Math.Max(0, result2),
						SourceLength = result3,
						OpeningDelimiterLength = 0,
						ClosingDelimiterLength = 0,
						Color = ((array.Length >= 4) ? UnescapeValue(array[3]) : null),
						Url = ((array.Length >= 5) ? UnescapeValue(array[4]) : null)
					});
				}
			}
		}
		return (from mark in list
			orderby mark.SourcePosition, GetPersistenceOrder(mark.Kind)
			select mark).ToList();
	}

	public static string GetOpeningDelimiter(MarkdownInlineDisplaySegment mark)
	{
		if (mark == null)
		{
			return null;
		}
		switch (mark.Kind)
		{
		case MarkdownMarkKind.Link:
			return "[";
		case MarkdownMarkKind.Highlight:
			if (!string.IsNullOrWhiteSpace(mark.Color))
			{
				return "=={" + mark.Color + "}";
			}
			return "==";
		default:
			return GetDelimiter(mark.Kind);
		}
	}

	public static string GetClosingDelimiter(MarkdownInlineDisplaySegment mark)
	{
		if (mark == null)
		{
			return null;
		}
		return mark.Kind switch
		{
			MarkdownMarkKind.Link => "](" + (mark.Url ?? string.Empty) + ")", 
			MarkdownMarkKind.Highlight => "==", 
			_ => GetDelimiter(mark.Kind), 
		};
	}

	public static string GetDelimiter(MarkdownMarkKind kind)
	{
		return kind switch
		{
			MarkdownMarkKind.Strong => "**", 
			MarkdownMarkKind.Emphasis => "*", 
			MarkdownMarkKind.Underline => "~", 
			MarkdownMarkKind.Strikethrough => "~~", 
			MarkdownMarkKind.Code => "`", 
			MarkdownMarkKind.Highlight => "==", 
			_ => null, 
		};
	}

	private static IEnumerable<string> SplitEntries(string payload)
	{
		if (string.IsNullOrEmpty(payload))
		{
			yield break;
		}
		StringBuilder builder = new StringBuilder(payload.Length);
		bool escaped = false;
		foreach (char c in payload)
		{
			if (escaped)
			{
				builder.Append('\\');
				builder.Append(c);
				escaped = false;
				continue;
			}
			switch (c)
			{
			case '\\':
				escaped = true;
				break;
			case ';':
				if (builder.Length > 0)
				{
					yield return builder.ToString();
					builder.Clear();
				}
				break;
			default:
				builder.Append(c);
				break;
			}
		}
		if (escaped)
		{
			builder.Append('\\');
		}
		if (builder.Length > 0)
		{
			yield return builder.ToString();
		}
	}

	private static string EscapeValue(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		return value.Replace("\\", "\\\\").Replace(":", "\\:").Replace(";", "\\;");
	}

	private static string UnescapeValue(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(value.Length);
		bool flag = false;
		foreach (char c in value)
		{
			if (flag)
			{
				stringBuilder.Append(c);
				flag = false;
			}
			else if (c == '\\')
			{
				flag = true;
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		if (flag)
		{
			stringBuilder.Append('\\');
		}
		return stringBuilder.ToString();
	}
}
