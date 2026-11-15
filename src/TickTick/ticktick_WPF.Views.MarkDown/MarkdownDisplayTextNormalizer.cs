using System;
using System.Text;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal static class MarkdownDisplayTextNormalizer
{
	private const string RawUncheckedTaskListMarker = "- [ ] ";

	private const string RawCheckedTaskListMarker = "- [x] ";

	private const string RawBlockQuoteMarker = "> ";

	private const string RawBulletDashMarker = "- ";

	private const string RawBulletAsteriskMarker = "* ";

	private const string RawBulletPlusMarker = "+ ";

	private const string RawHorizontalRuleMarker = "---";

	private const string RawCodeFenceMarker = "```";

	private const string DisplayUncheckedTaskListMarker = "\u200b\u200c\u200d \u200e\u200f";

	private const string DisplayCheckedTaskListMarker = "\u200b\u200c\u200d\u2060\u200e\u200f";

	private const string DisplayBlockQuoteMarker = "\u2061\u2062";

	private const string DisplayBulletDashMarker = "\u2063\u2064";

	private const string DisplayBulletAsteriskMarker = "\u2064\u2063";

	private const string DisplayBulletPlusMarker = "\u2061\u2064";

	private const char DisplayOrderedListDot = '\u206a';

	private const char DisplayOrderedListSpace = ' ';

	private const char DisplayHeadingMarker = '\u200b';

	private const char DisplayHeadingSpace = ' ';

	private const string DisplayHorizontalRuleMarker = "\u200c\u200d\u2060";

	private const string DisplayParagraphIndentMarker = "\u2065";

	public static bool StartsWithAt(string text, int offset, string value)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value) || offset < 0 || offset + value.Length > text.Length)
		{
			return false;
		}
		return string.Compare(text, offset, value, 0, value.Length, StringComparison.Ordinal) == 0;
	}

	public static string NormalizeDisplayTaskMarkersForPersistence(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		int num = 0;
		while (num <= text.Length)
		{
			int i;
			for (i = num; i < text.Length && text[i] != '\r' && text[i] != '\n'; i++)
			{
			}
			stringBuilder.Append(NormalizeDisplayTaskMarkerLine(text.Substring(num, i - num)));
			if (i >= text.Length)
			{
				break;
			}
			if (text[i] == '\r')
			{
				stringBuilder.Append('\r');
				i++;
				if (i < text.Length && text[i] == '\n')
				{
					stringBuilder.Append('\n');
					i++;
				}
			}
			else
			{
				stringBuilder.Append('\n');
				i++;
			}
			num = i;
		}
		return stringBuilder.ToString();
	}

	public static string NormalizeDisplayTaskMarkerLine(string lineText)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			return lineText ?? string.Empty;
		}
		lineText = NormalizeLegacyDisplayPlaceholders(lineText);
		int num = lineText.Length - lineText.TrimStart().Length;
		if (StartsWithAt(lineText, num, "\u200b\u200c\u200d \u200e\u200f"))
		{
			return lineText.Substring(0, num) + "- [ ] " + lineText.Substring(num + "\u200b\u200c\u200d \u200e\u200f".Length);
		}
		if (StartsWithAt(lineText, num, "\u200b\u200c\u200d\u2060\u200e\u200f"))
		{
			return lineText.Substring(0, num) + "- [x] " + lineText.Substring(num + "\u200b\u200c\u200d\u2060\u200e\u200f".Length);
		}
		if (StartsWithAt(lineText, num, "\u2061\u2062"))
		{
			return lineText.Substring(0, num) + "> " + lineText.Substring(num + "\u2061\u2062".Length);
		}
		if (TryGetDisplayBulletMarker(lineText, num, out var rawMarker))
		{
			return lineText.Substring(0, num) + rawMarker + lineText.Substring(num + rawMarker.Length);
		}
		if (TryGetDisplayHeadingLevel(lineText, num, out var level))
		{
			int num2 = level + 1;
			return lineText.Substring(0, num) + new string('#', level) + " " + lineText.Substring(num + num2);
		}
		if (TryGetDisplayOrderedNumberText(lineText, num, out var numberText, out var markerLength))
		{
			return lineText.Substring(0, num) + numberText + ". " + lineText.Substring(num + markerLength);
		}
		if (StartsWithAt(lineText, num, "\u200c\u200d\u2060"))
		{
			return lineText.Substring(0, num) + "---" + lineText.Substring(num + "\u200c\u200d\u2060".Length);
		}
		return lineText;
	}

	public static string NormalizePersistenceTextForEditorDisplay(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		text = NormalizeLegacyDisplayPlaceholders(text);
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		int num = 0;
		while (num < text.Length)
		{
			int i;
			for (i = num; i < text.Length && text[i] != '\r' && text[i] != '\n'; i++)
			{
			}
			stringBuilder.Append(ConvertRawLineToDisplayLine(text.Substring(num, i - num)));
			if (i >= text.Length)
			{
				break;
			}
			if (text[i] == '\r')
			{
				stringBuilder.Append('\r');
				i++;
				if (i < text.Length && text[i] == '\n')
				{
					stringBuilder.Append('\n');
					i++;
				}
			}
			else
			{
				stringBuilder.Append('\n');
				i++;
			}
			num = i;
		}
		return stringBuilder.ToString();
	}

	public static string NormalizeLegacyDisplayPlaceholders(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		string text2 = text.Replace("\ue120\ue121\ue122 \ue123\ue124", "\u200b\u200c\u200d \u200e\u200f").Replace("\ue120\ue121\ue122x\ue123\ue124", "\u200b\u200c\u200d\u2060\u200e\u200f").Replace("\ue125\ue126", "\u2061\u2062")
			.Replace("\ue127\ue128", "\u2063\u2064")
			.Replace("\ue129\ue128", "\u2064\u2063")
			.Replace("\ue12a\ue128", "\u2061\u2064")
			.Replace("\ue12f\ue130\ue131", "\u200c\u200d\u2060")
			.Replace("\ue132\ue133\ue134", "```")
			.Replace('\ue12b', '\u206a')
			.Replace('\ue12c', ' ')
			.Replace('\ue12d', '\u200b')
			.Replace('\ue12e', ' ');
		if (text2.IndexOf("\ufe0c\ufe0d", StringComparison.Ordinal) < 0 && text2.IndexOf("\ufe0e\ufe0f", StringComparison.Ordinal) < 0)
		{
			return text2;
		}
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		int num = 0;
		while (num <= text2.Length)
		{
			int i;
			for (i = num; i < text2.Length && text2[i] != '\r' && text2[i] != '\n'; i++)
			{
			}
			stringBuilder.Append(NormalizeLegacyVariationSelectorBlockMarkerLine(text2.Substring(num, i - num)));
			if (i >= text2.Length)
			{
				break;
			}
			if (text2[i] == '\r')
			{
				stringBuilder.Append('\r');
				i++;
				if (i < text2.Length && text2[i] == '\n')
				{
					stringBuilder.Append('\n');
					i++;
				}
			}
			else
			{
				stringBuilder.Append('\n');
				i++;
			}
			num = i;
		}
		return stringBuilder.ToString();
	}

	private static string NormalizeLegacyVariationSelectorBlockMarkerLine(string lineText)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			return lineText ?? string.Empty;
		}
		int num = lineText.Length - lineText.TrimStart().Length;
		int i;
		for (i = num; i < lineText.Length && char.IsDigit(lineText[i]); i++)
		{
		}
		if (i > num && i + 1 < lineText.Length && lineText[i] == '\ufe0c' && lineText[i + 1] == '\ufe0d')
		{
			return lineText.Substring(0, i) + "\u206a " + lineText.Substring(i + 2);
		}
		for (i = num; i < lineText.Length && lineText[i] == '\ufe0e'; i++)
		{
		}
		int num2 = i - num;
		if (num2 > 0 && num2 <= 6 && i < lineText.Length && lineText[i] == '\ufe0f')
		{
			return lineText.Substring(0, num) + BuildDisplayHeadingMarker(num2) + lineText.Substring(i + 1);
		}
		return lineText;
	}

	public static string ConvertRawLineToDisplayLine(string lineText)
	{
		MarkdownBlockNode block = ConvertSyntaxInfoToBlockNode(MarkdownBlockSyntax.Parse(lineText ?? string.Empty));
		if (!TryGetDisplayBlockPrefixReplacement(lineText ?? string.Empty, block, out var markerOffset, out var markerLength, out var displayMarker))
		{
			return lineText ?? string.Empty;
		}
		return (lineText ?? string.Empty).Substring(0, markerOffset) + displayMarker + (lineText ?? string.Empty).Substring(markerOffset + markerLength);
	}

	public static MarkdownBlockNode ConvertSyntaxInfoToBlockNode(MarkdownBlockSyntaxInfo syntax)
	{
		if (syntax == null)
		{
			return null;
		}
		int indent = ConvertIndentLevelsToWhitespaceCount(syntax.Indent);
		return syntax.Kind switch
		{
			MarkdownBlockSyntaxKind.Heading => new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Heading,
				Level = syntax.HeadingLevel,
				Indent = indent
			}, 
			MarkdownBlockSyntaxKind.BlockQuote => new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.BlockQuote,
				Indent = indent
			}, 
			MarkdownBlockSyntaxKind.List => new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.List,
				ListKind = syntax.ListKind.GetValueOrDefault(),
				Checked = syntax.Checked,
				Indent = indent
			}, 
			MarkdownBlockSyntaxKind.SplitLine => new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.HorizontalRule,
				Indent = indent
			}, 
			MarkdownBlockSyntaxKind.CodeFence => new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.CodeBlock,
				Indent = indent
			}, 
			_ => new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				Indent = indent
			}, 
		};
	}

	public static int ConvertIndentLevelsToWhitespaceCount(int indentLevels)
	{
		return Math.Max(0, indentLevels) * 4;
	}

	public static string BuildDisplayHeadingMarker(int level)
	{
		return new string('\u200b', Math.Max(1, Math.Min(6, level))) + " ";
	}

	public static string BuildDisplayOrderedListMarker(string numberText)
	{
		return (numberText ?? string.Empty) + "\u206a ";
	}

	public static string GetDisplayBulletMarker(string rawMarker)
	{
		if (!(rawMarker == "- "))
		{
			if (rawMarker == "+ ")
			{
				return "\u2061\u2064";
			}
			return "\u2064\u2063";
		}
		return "\u2063\u2064";
	}

	public static bool TryGetDisplayBulletMarker(string lineText, int offset, out string rawMarker)
	{
		rawMarker = string.Empty;
		if (StartsWithAt(lineText, offset, "\u2063\u2064"))
		{
			rawMarker = "- ";
			return true;
		}
		if (StartsWithAt(lineText, offset, "\u2064\u2063"))
		{
			rawMarker = "* ";
			return true;
		}
		if (StartsWithAt(lineText, offset, "\u2061\u2064"))
		{
			rawMarker = "+ ";
			return true;
		}
		return false;
	}

	public static bool TryGetDisplayHeadingLevel(string lineText, int offset, out int level)
	{
		level = 0;
		if (string.IsNullOrEmpty(lineText) || offset < 0 || offset >= lineText.Length)
		{
			return false;
		}
		int i;
		for (i = offset; i < lineText.Length && lineText[i] == '\u200b'; i++)
		{
		}
		level = i - offset;
		if (level > 0 && level <= 6 && i < lineText.Length)
		{
			return lineText[i] == ' ';
		}
		return false;
	}

	public static bool TryGetDisplayOrderedNumberText(string lineText, int offset, out string numberText, out int markerLength)
	{
		numberText = string.Empty;
		markerLength = 0;
		if (string.IsNullOrEmpty(lineText) || offset < 0 || offset >= lineText.Length)
		{
			return false;
		}
		int i;
		for (i = offset; i < lineText.Length && char.IsDigit(lineText[i]); i++)
		{
		}
		if (i == offset || i + 1 >= lineText.Length || lineText[i] != '\u206a' || lineText[i + 1] != ' ')
		{
			return false;
		}
		numberText = lineText.Substring(offset, i - offset);
		markerLength = i - offset + 2;
		return true;
	}

	public static bool TryGetBulletListMarkerInfo(string lineText, out int markerOffset, out int markerLength, out string rawMarker)
	{
		markerOffset = 0;
		markerLength = 0;
		rawMarker = string.Empty;
		int num = (lineText ?? string.Empty).Length - (lineText ?? string.Empty).TrimStart().Length;
		if (TryGetDisplayBulletMarker(lineText ?? string.Empty, num, out rawMarker))
		{
			markerOffset = num;
			markerLength = 2;
			return true;
		}
		if (StartsWithAt(lineText, num, "- ") || StartsWithAt(lineText, num, "* ") || StartsWithAt(lineText, num, "+ "))
		{
			markerOffset = num;
			markerLength = 2;
			rawMarker = (lineText ?? string.Empty).Substring(num, 2);
			return true;
		}
		return false;
	}

	private static bool TryGetDisplayBlockPrefixReplacement(string lineText, MarkdownBlockNode block, out int markerOffset, out int markerLength, out string displayMarker)
	{
		markerOffset = 0;
		markerLength = 0;
		displayMarker = string.Empty;
		if (block == null)
		{
			return false;
		}
		switch (block.Kind)
		{
		case MarkdownBlockKind.BlockQuote:
			if (!MarkdownBlockSyntax.TryGetBlockQuoteDisplayPrefixLength(lineText, out markerLength))
			{
				return false;
			}
			markerOffset = 0;
			displayMarker = string.Empty;
			return true;
		case MarkdownBlockKind.Heading:
		{
			int num3 = lineText.Length - lineText.TrimStart().Length;
			int num4 = Math.Max(1, Math.Min(6, block.Level ?? 1));
			markerOffset = num3;
			markerLength = num4 + 1;
			displayMarker = string.Empty;
			return true;
		}
		case MarkdownBlockKind.List:
		{
			int num2 = lineText.Length - lineText.TrimStart().Length;
			markerOffset = num2;
			switch (block.ListKind)
			{
			case MarkdownListKind.Task:
				markerLength = "- [ ] ".Length;
				displayMarker = string.Empty;
				return true;
			case MarkdownListKind.Bullet:
				markerLength = 2;
				displayMarker = string.Empty;
				return true;
			case MarkdownListKind.Numbered:
				markerLength = Math.Max(1, (block.CustomOrder ?? "1").Length) + 2;
				displayMarker = string.Empty;
				return true;
			}
			break;
		}
		case MarkdownBlockKind.Paragraph:
			if (block.Indent > 0)
			{
				markerOffset = 0;
				markerLength = 0;
				displayMarker = "\u2065";
				return true;
			}
			break;
		case MarkdownBlockKind.HorizontalRule:
		{
			int num = (markerOffset = lineText.Length - lineText.TrimStart().Length);
			markerLength = lineText.Length - num;
			displayMarker = "\u200c\u200d\u2060";
			return true;
		}
		case MarkdownBlockKind.CodeBlock:
			return false;
		}
		return false;
	}
}
