using System;
using System.Text.RegularExpressions;

namespace ticktick_WPF.Framework.Markdown;

public static class MarkdownBlockSyntax
{
	public const string CanonicalSplitLineMarker = "---";

	public const string AlternateSplitLineMarker = "***";

	private static readonly Regex OrderedListRegex = new Regex("^(\\d+)\\.(?: |$)", RegexOptions.Compiled);

	private static readonly Regex TaskListRegex = new Regex("^[-\\*\\+] \\[([ xX])\\] ", RegexOptions.Compiled);

	public static MarkdownBlockSyntaxInfo Parse(string line)
	{
		string text = line ?? string.Empty;
		bool flag = HasTabInLeadingWhitespace(text);
		string content;
		int num = CountIndentLevels(text, out content);
		string text2 = NormalizeOptionalMarkerPrefix(content);
		string text3 = NormalizeOptionalListMarkerPrefix(content);
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = new MarkdownBlockSyntaxInfo
		{
			Kind = MarkdownBlockSyntaxKind.Paragraph,
			Indent = num,
			Content = content
		};
		if (string.IsNullOrEmpty(content))
		{
			return markdownBlockSyntaxInfo;
		}
		int headingLevel = GetHeadingLevel(text2);
		if (headingLevel > 0)
		{
			markdownBlockSyntaxInfo.Kind = MarkdownBlockSyntaxKind.Heading;
			markdownBlockSyntaxInfo.HeadingLevel = headingLevel;
			markdownBlockSyntaxInfo.Content = text2.Substring(headingLevel + 1);
			return markdownBlockSyntaxInfo;
		}
		if (text2.StartsWith("> ", StringComparison.Ordinal))
		{
			string content2;
			int num2 = CountIndentLevels(text2.Substring(2), out content2);
			markdownBlockSyntaxInfo.Kind = MarkdownBlockSyntaxKind.BlockQuote;
			markdownBlockSyntaxInfo.Indent += num2;
			markdownBlockSyntaxInfo.Content = content2;
			return markdownBlockSyntaxInfo;
		}
		Match match = TaskListRegex.Match(text3);
		if (match.Success)
		{
			markdownBlockSyntaxInfo.Kind = MarkdownBlockSyntaxKind.List;
			markdownBlockSyntaxInfo.ListKind = MarkdownListKind.Task;
			markdownBlockSyntaxInfo.Checked = string.Equals(match.Groups[1].Value, "x", StringComparison.OrdinalIgnoreCase);
			markdownBlockSyntaxInfo.Content = text3.Substring(match.Length);
			return markdownBlockSyntaxInfo;
		}
		Match match2 = OrderedListRegex.Match(text3);
		if (match2.Success)
		{
			markdownBlockSyntaxInfo.Kind = MarkdownBlockSyntaxKind.List;
			markdownBlockSyntaxInfo.ListKind = MarkdownListKind.Numbered;
			markdownBlockSyntaxInfo.CustomOrder = match2.Groups[1].Value;
			markdownBlockSyntaxInfo.Content = text3.Substring(match2.Length);
			return markdownBlockSyntaxInfo;
		}
		if (text3.StartsWith("- ", StringComparison.Ordinal) || text3.StartsWith("* ", StringComparison.Ordinal) || text3.StartsWith("+ ", StringComparison.Ordinal))
		{
			markdownBlockSyntaxInfo.Kind = MarkdownBlockSyntaxKind.List;
			markdownBlockSyntaxInfo.ListKind = MarkdownListKind.Bullet;
			markdownBlockSyntaxInfo.Content = text3.Substring(2);
			return markdownBlockSyntaxInfo;
		}
		if (text2.StartsWith("```", StringComparison.Ordinal))
		{
			markdownBlockSyntaxInfo.Kind = MarkdownBlockSyntaxKind.CodeFence;
			return markdownBlockSyntaxInfo;
		}
		if (!flag && IsSplitLineMarker(text2))
		{
			markdownBlockSyntaxInfo.Kind = MarkdownBlockSyntaxKind.SplitLine;
			return markdownBlockSyntaxInfo;
		}
		if (num > 0)
		{
			markdownBlockSyntaxInfo.Indent = 0;
			markdownBlockSyntaxInfo.Content = text;
		}
		return markdownBlockSyntaxInfo;
	}

	public static bool IsSplitLine(string line)
	{
		return Parse(line).Kind == MarkdownBlockSyntaxKind.SplitLine;
	}

	public static bool TryGetQuotePrefixLength(string line, out int prefixLength)
	{
		prefixLength = 0;
		string text = line ?? string.Empty;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int num = CountLeadingWhitespaceCharacters(text);
		int num2 = 0;
		while (num + 1 < text.Length && text[num] == '>' && text[num + 1] == ' ')
		{
			num += 2;
			num2++;
		}
		if (num2 == 0)
		{
			return false;
		}
		prefixLength = num;
		return true;
	}

	public static bool TryGetBlockQuoteDisplayPrefixLength(string line, out int prefixLength)
	{
		prefixLength = 0;
		string text = line ?? string.Empty;
		int num = CountLeadingWhitespaceCharacters(text);
		if (num + 2 > text.Length || text[num] != '>' || text[num + 1] != ' ')
		{
			return false;
		}
		int num2 = num + 2;
		while (num2 < text.Length)
		{
			if (text[num2] == '\t')
			{
				num2++;
				continue;
			}
			if (num2 + 4 > text.Length || text[num2] != ' ' || text[num2 + 1] != ' ' || text[num2 + 2] != ' ' || text[num2 + 3] != ' ')
			{
				break;
			}
			num2 += 4;
		}
		prefixLength = num2;
		return true;
	}

	public static bool TryParseQuotedListSyntax(string line, out int quotePrefixLength, out MarkdownBlockSyntaxInfo info)
	{
		info = null;
		if (!TryGetQuotePrefixLength(line, out quotePrefixLength))
		{
			return false;
		}
		info = Parse((line ?? string.Empty).Substring(quotePrefixLength));
		return info.Kind == MarkdownBlockSyntaxKind.List;
	}

	public static string GetParagraphAction(string text)
	{
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = Parse(text?.Trim() ?? string.Empty);
		switch (markdownBlockSyntaxInfo.Kind)
		{
		case MarkdownBlockSyntaxKind.Heading:
			return Math.Max(1, Math.Min(6, markdownBlockSyntaxInfo.HeadingLevel ?? 1)) switch
			{
				1 => "Heading1", 
				2 => "Heading2", 
				3 => "Heading3", 
				4 => "Heading4", 
				5 => "Heading5", 
				_ => "Heading6", 
			};
		case MarkdownBlockSyntaxKind.BlockQuote:
			return "Quote";
		case MarkdownBlockSyntaxKind.List:
			return markdownBlockSyntaxInfo.ListKind switch
			{
				MarkdownListKind.Task => "CheckItem", 
				MarkdownListKind.Numbered => "NumberedList", 
				_ => "BulletList", 
			};
		case MarkdownBlockSyntaxKind.CodeFence:
			return "CodeBlock";
		case MarkdownBlockSyntaxKind.SplitLine:
			return "SplitLine";
		default:
			if (!string.IsNullOrWhiteSpace(text))
			{
				return "Text";
			}
			return string.Empty;
		}
	}

	private static int CountIndentLevels(string line, out string content)
	{
		int num = 0;
		int num2 = 0;
		while (num < line.Length)
		{
			if (line[num] == '\t')
			{
				num2++;
				num++;
				continue;
			}
			if (num + 4 > line.Length || line[num] != ' ' || line[num + 1] != ' ' || line[num + 2] != ' ' || line[num + 3] != ' ')
			{
				break;
			}
			num2++;
			num += 4;
		}
		content = line.Substring(num);
		return num2;
	}

	private static int CountLeadingWhitespaceCharacters(string text)
	{
		int i;
		for (i = 0; i < (text?.Length ?? 0) && (text[i] == ' ' || text[i] == '\t'); i++)
		{
		}
		return i;
	}

	private static int GetHeadingLevel(string content)
	{
		int i;
		for (i = 0; i < content.Length && content[i] == '#'; i++)
		{
		}
		if (i < 1 || i > 6)
		{
			return 0;
		}
		if (i >= content.Length || content[i] != ' ')
		{
			return 0;
		}
		return i;
	}

	private static string NormalizeOptionalMarkerPrefix(string content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return string.Empty;
		}
		int i;
		for (i = 0; i < content.Length && IsEditorOnlyLeadingCharacter(content[i]); i++)
		{
		}
		string text = ((i == 0) ? content : content.Substring(i));
		int j;
		for (j = 0; j < text.Length && j < 3 && (text[j] == ' ' || text[j] == '\u00a0'); j++)
		{
		}
		if (j != 0)
		{
			return text.Substring(j);
		}
		return text;
	}

	private static string NormalizeOptionalListMarkerPrefix(string content)
	{
		if (string.IsNullOrEmpty(content) || !IsEditorOnlyLeadingCharacter(content[0]))
		{
			return content ?? string.Empty;
		}
		return NormalizeOptionalMarkerPrefix(content);
	}

	private static bool IsEditorOnlyLeadingCharacter(char ch)
	{
		if (ch != '\u200b' && ch != '\u200c' && ch != '\u200d' && ch != '\u200e' && ch != '\u200f' && ch != '\u2060' && ch != '\u2061' && ch != '\u2062' && ch != '\u2063' && ch != '\u2064')
		{
			return ch == '\u206a';
		}
		return true;
	}

	private static bool IsSplitLineMarker(string content)
	{
		if (string.IsNullOrEmpty(content) || content.Length < "---".Length)
		{
			return false;
		}
		char c = content[0];
		if (c == "***"[0])
		{
			return string.Equals(content, "***", StringComparison.Ordinal);
		}
		if (c != "---"[0])
		{
			return false;
		}
		for (int i = 1; i < content.Length; i++)
		{
			if (content[i] != c)
			{
				return false;
			}
		}
		return true;
	}

	private static bool HasTabInLeadingWhitespace(string line)
	{
		for (int i = 0; i < (line?.Length ?? 0); i++)
		{
			if (line[i] == '\t')
			{
				return true;
			}
			if (line[i] != ' ' && line[i] != '\u00a0')
			{
				return false;
			}
		}
		return false;
	}
}
