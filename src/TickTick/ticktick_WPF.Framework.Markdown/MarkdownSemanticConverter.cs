using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ticktick_WPF.Util;

namespace ticktick_WPF.Framework.Markdown;

public static class MarkdownSemanticConverter
{
	private sealed class MarkdownLineMetadata
	{
		public string Content { get; set; }

		public bool? Folded { get; set; }

		public bool? SoftTab { get; set; }

		public int? Indent { get; set; }

		public string CustomOrder { get; set; }
	}

	private static readonly Regex FileBlockRegex = new Regex("^!\\[(.*?)\\]\\((\\S+?)(?:\\s+\"(.*?)\")?\\)$", RegexOptions.Compiled);

	private static readonly HashSet<char> EscapableChars = new HashSet<char>("\\!*~[]`_={}():#>-.+".ToCharArray());

	private static readonly HashSet<char> InlinePersistenceEscapableChars = new HashSet<char>("\\!*~[]`_={}()".ToCharArray());

	public static MarkdownSemanticDocument Parse(string markdown)
	{
		return Parse(markdown, parseEmphasis: true);
	}

	public static MarkdownSemanticDocument Parse(string markdown, bool parseEmphasis)
	{
		MarkdownSemanticDocument markdownSemanticDocument = new MarkdownSemanticDocument();
		string[] array = NormalizeNewLines(markdown).Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string line = array[i] ?? string.Empty;
			int num = i;
			if (TryParseCodeBlock(array, ref i, out var block))
			{
				block.StartLine = num;
				block.EndLine = Math.Min(i, array.Length - 1);
				markdownSemanticDocument.Blocks.Add(block);
			}
			else
			{
				MarkdownBlockNode markdownBlockNode = ParseBlock(line, parseEmphasis);
				markdownBlockNode.StartLine = num;
				markdownBlockNode.EndLine = num;
				markdownSemanticDocument.Blocks.Add(markdownBlockNode);
			}
		}
		if (markdownSemanticDocument.Blocks.Count == 0)
		{
			markdownSemanticDocument.Blocks.Add(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.Paragraph,
				StartLine = 0,
				EndLine = 0
			});
		}
		return markdownSemanticDocument;
	}

	public static string GetPlainText(string markdown)
	{
		MarkdownSemanticDocument markdownSemanticDocument = Parse(markdown);
		StringBuilder stringBuilder = new StringBuilder();
		foreach (MarkdownBlockNode block in markdownSemanticDocument.Blocks)
		{
			string blockPlainText = GetBlockPlainText(block);
			if (!string.IsNullOrEmpty(blockPlainText))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append('\n');
				}
				stringBuilder.Append(blockPlainText);
			}
		}
		return stringBuilder.ToString();
	}

	public static string DecodeEscapedSyntaxForDisplay(string markdown)
	{
		return DecodeEscapedInlineText(markdown);
	}

	private static string GetBlockPlainText(MarkdownBlockNode block)
	{
		if (block == null)
		{
			return string.Empty;
		}
		if (block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return block.CodeText ?? string.Empty;
		}
		if (block.Kind == MarkdownBlockKind.FileBlock)
		{
			return block.Caption ?? block.AltText ?? string.Empty;
		}
		return GetInlinePlainText(block.Inlines);
	}

	internal static string GetInlinePlainText(IEnumerable<MarkdownInlineNode> inlines, bool preserveLinks = false)
	{
		if (inlines == null)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (MarkdownInlineNode inline in inlines)
		{
			if (inline != null)
			{
				MarkdownMark markdownMark = (preserveLinks ? inline.Marks.FirstOrDefault((MarkdownMark mark) => mark != null && mark.Kind == MarkdownMarkKind.Link) : null);
				bool flag = !string.IsNullOrEmpty(markdownMark?.Url);
				string text = ((inline.Marks.Count == 0 || inline.Marks.Any((MarkdownMark mark) => mark != null && mark.Kind == MarkdownMarkKind.Code)) ? (inline.Text ?? string.Empty) : GetInlinePlainText(ParseInlineNodes(inline.RawText ?? inline.Text ?? string.Empty), preserveLinks && !flag));
				stringBuilder.Append(flag ? ApplyMark(EscapePlainTextForPersistence(text), markdownMark, MarkdownSerializeMode.Compatibility) : text);
			}
		}
		return stringBuilder.ToString();
	}

	public static string Serialize(MarkdownSemanticDocument document, MarkdownSerializeMode mode)
	{
		if (document == null || document.Blocks.Count == 0)
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		foreach (MarkdownBlockNode block in document.Blocks)
		{
			list.Add(SerializeBlock(block, mode));
		}
		return string.Join("\n", list);
	}

	public static string EscapePlainTextForPersistence(string text)
	{
		int[] rawToEscapedOffsetMap;
		return EscapePlainTextForPersistence(text, out rawToEscapedOffsetMap);
	}

	public static string EscapePlainTextLineForPersistence(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		return EscapeParagraphLikeFileBlock(EscapeParagraphBlockPrefixSyntax(EscapePlainTextForPersistence(text)));
	}

	public static string EscapePlainTextForPersistence(string text, out int[] rawToEscapedOffsetMap)
	{
		return EscapePlainText(text, out rawToEscapedOffsetMap, escapeLiteralInlineCharacters: true, includeColonHighlight: true, includeLegacyUnderscoreSyntax: true, includeGeneralSyntax: true);
	}

	public static string EscapeCompletedInlineSyntaxForPersistence(string text)
	{
		int[] rawToEscapedOffsetMap;
		return EscapeCompletedInlineSyntaxForPersistence(text, out rawToEscapedOffsetMap);
	}

	public static string EscapeCompletedInlineSyntaxForPersistence(string text, out int[] rawToEscapedOffsetMap)
	{
		return EscapePlainText(text, out rawToEscapedOffsetMap, escapeLiteralInlineCharacters: false, includeColonHighlight: true, includeLegacyUnderscoreSyntax: true, includeGeneralSyntax: false);
	}

	public static string EscapeSemanticTextForPersistence(string text, out int[] rawToEscapedOffsetMap)
	{
		return EscapePlainText(text, out rawToEscapedOffsetMap, escapeLiteralInlineCharacters: false, includeColonHighlight: true, includeLegacyUnderscoreSyntax: true, includeGeneralSyntax: true, escapeStarCharacters: true);
	}

	public static bool IsEscapableLiteralChar(char candidate)
	{
		return EscapableChars.Contains(candidate);
	}

	public static string RepairMalformedHeadingLinks(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return markdown ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(markdown.Length + 1);
		int i = 0;
		bool flag = false;
		while (i < markdown.Length)
		{
			int num = i;
			for (; i < markdown.Length && markdown[i] != '\r' && markdown[i] != '\n'; i++)
			{
			}
			string text = markdown.Substring(num, i - num);
			string text2 = text.TrimStart();
			if (!flag && !text2.StartsWith("```", StringComparison.Ordinal))
			{
				text = RepairMalformedHeadingLinkLine(text);
			}
			stringBuilder.Append(text);
			if (text2.StartsWith("```", StringComparison.Ordinal))
			{
				flag = !flag;
			}
			if (i < markdown.Length)
			{
				if (markdown[i] == '\r' && i + 1 < markdown.Length && markdown[i + 1] == '\n')
				{
					stringBuilder.Append("\r\n");
					i += 2;
				}
				else
				{
					stringBuilder.Append(markdown[i]);
					i++;
				}
			}
		}
		return stringBuilder.ToString();
	}

	private static string RepairMalformedHeadingLinkLine(string line)
	{
		line = line ?? string.Empty;
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = MarkdownBlockSyntax.Parse(line);
		if (markdownBlockSyntaxInfo.Kind != MarkdownBlockSyntaxKind.Heading)
		{
			return line;
		}
		int atxHeadingContentStart = GetAtxHeadingContentStart(line, markdownBlockSyntaxInfo.HeadingLevel.GetValueOrDefault());
		if (atxHeadingContentStart < 0 || atxHeadingContentStart >= line.Length)
		{
			return line;
		}
		int num = line.IndexOf("](", atxHeadingContentStart, StringComparison.Ordinal);
		if (num <= atxHeadingContentStart || line.IndexOf('[', atxHeadingContentStart, num - atxHeadingContentStart) >= 0)
		{
			return line;
		}
		int num2 = num + 2;
		int num3 = FindLinkUrlEnd(line, num2);
		if (num3 <= num2 || !Uri.IsWellFormedUriString(line.Substring(num2, num3 - num2), UriKind.Absolute))
		{
			return line;
		}
		return line.Insert(atxHeadingContentStart, "[");
	}

	private static int GetAtxHeadingContentStart(string line, int level)
	{
		if (string.IsNullOrEmpty(line) || level < 1 || level > 6)
		{
			return -1;
		}
		int i;
		for (i = 0; i < line.Length && (line[i] == ' ' || line[i] == '\t'); i++)
		{
		}
		int num = i;
		for (; i < line.Length && line[i] == '#'; i++)
		{
		}
		if (i - num != level || i >= line.Length || line[i] != ' ')
		{
			return -1;
		}
		return i + 1;
	}

	private static MarkdownBlockNode ParseBlock(string line, bool parseEmphasis)
	{
		MarkdownLineMetadata markdownLineMetadata = ParseLineMetadata(line);
		if (TryParseFileBlock(markdownLineMetadata.Content, out var block))
		{
			return ApplyBlockMetadata(block, markdownLineMetadata);
		}
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = MarkdownBlockSyntax.Parse(markdownLineMetadata.Content);
		int indent = ConvertIndentLevelsToWhitespaceCount(markdownBlockSyntaxInfo.Indent);
		return markdownBlockSyntaxInfo.Kind switch
		{
			MarkdownBlockSyntaxKind.Heading => ApplyBlockMetadata(BuildBlock(MarkdownBlockKind.Heading, indent, markdownBlockSyntaxInfo.Content, markdownBlockSyntaxInfo.HeadingLevel, null, null, null, parseEmphasis), markdownLineMetadata), 
			MarkdownBlockSyntaxKind.BlockQuote => ApplyBlockMetadata(BuildBlock(MarkdownBlockKind.BlockQuote, indent, markdownBlockSyntaxInfo.Content, null, null, null, null, parseEmphasis), markdownLineMetadata), 
			MarkdownBlockSyntaxKind.SplitLine => ApplyBlockMetadata(new MarkdownBlockNode
			{
				Kind = MarkdownBlockKind.HorizontalRule,
				Indent = indent
			}, markdownLineMetadata), 
			MarkdownBlockSyntaxKind.List => ApplyBlockMetadata(BuildBlock(MarkdownBlockKind.List, indent, markdownBlockSyntaxInfo.Content, null, markdownBlockSyntaxInfo.ListKind, markdownBlockSyntaxInfo.Checked, markdownBlockSyntaxInfo.CustomOrder, parseEmphasis), markdownLineMetadata), 
			_ => ApplyBlockMetadata(BuildBlock(MarkdownBlockKind.Paragraph, 0, markdownBlockSyntaxInfo.Content, null, null, null, null, parseEmphasis), markdownLineMetadata), 
		};
	}

	private static bool TryParseCodeBlock(string[] lines, ref int index, out MarkdownBlockNode block)
	{
		block = null;
		string text = lines[index] ?? string.Empty;
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = MarkdownBlockSyntax.Parse(text);
		if (markdownBlockSyntaxInfo.Kind != MarkdownBlockSyntaxKind.CodeFence)
		{
			return false;
		}
		if (index >= lines.Length - 1)
		{
			return false;
		}
		string text2 = text.TrimStart('\t');
		if (text2.StartsWith("    ", StringComparison.Ordinal))
		{
			text2 = text2.Substring(4);
		}
		string text3 = ((text2.Length > 3) ? text2.Substring(3).Trim() : string.Empty);
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		index++;
		while (index < lines.Length)
		{
			string text4 = lines[index] ?? string.Empty;
			if (IsClosingCodeFenceLine(text4))
			{
				break;
			}
			if (flag)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(text4);
			flag = true;
			index++;
		}
		block = new MarkdownBlockNode
		{
			Kind = MarkdownBlockKind.CodeBlock,
			Indent = ConvertIndentLevelsToWhitespaceCount(markdownBlockSyntaxInfo.Indent),
			Language = (string.IsNullOrWhiteSpace(text3) ? null : text3),
			CodeText = UnescapeCodeBlockText(stringBuilder.ToString())
		};
		return true;
	}

	private static string UnescapeCodeBlockText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '\\' && i + 1 < text.Length)
			{
				char c2 = text[i + 1];
				if (c2 == '\\' || c2 == '`')
				{
					stringBuilder.Append(c2);
					i++;
					continue;
				}
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	private static string EscapeCodeBlockText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			switch (c)
			{
			case '\\':
				stringBuilder.Append("\\\\");
				break;
			case '`':
				stringBuilder.Append("\\`");
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static bool IsClosingCodeFenceLine(string line)
	{
		string text = line ?? string.Empty;
		if (MarkdownBlockSyntax.Parse(text).Kind != MarkdownBlockSyntaxKind.CodeFence)
		{
			return false;
		}
		string text2 = text.TrimStart();
		if (text2.Length != 3)
		{
			if (text2.Length > 3 && text2.StartsWith("```", StringComparison.Ordinal))
			{
				return string.IsNullOrWhiteSpace(text2.Substring(3));
			}
			return false;
		}
		return true;
	}

	private static bool TryParseFileBlock(string line, out MarkdownBlockNode block)
	{
		block = null;
		Match match = FileBlockRegex.Match(line ?? string.Empty);
		if (!match.Success)
		{
			return false;
		}
		string a = match.Groups[1].Value ?? string.Empty;
		string source = match.Groups[2].Value ?? string.Empty;
		string caption = (match.Groups[3].Success ? (match.Groups[3].Value ?? string.Empty) : string.Empty);
		block = new MarkdownBlockNode
		{
			Kind = MarkdownBlockKind.FileBlock,
			FileKind = (string.Equals(a, "file", StringComparison.OrdinalIgnoreCase) ? MarkdownFileBlockKind.File : MarkdownFileBlockKind.Image),
			Source = source,
			AltText = string.Empty,
			Caption = caption
		};
		return true;
	}

	private static MarkdownBlockNode BuildBlock(MarkdownBlockKind kind, int indent, string inlineText, int? level, MarkdownListKind? listKind, bool? checkedState, string customOrder, bool parseEmphasis)
	{
		MarkdownBlockNode markdownBlockNode = new MarkdownBlockNode
		{
			Kind = kind,
			Indent = indent,
			Level = level,
			ListKind = listKind,
			Checked = ((kind == MarkdownBlockKind.List) ? new bool?(checkedState == true) : checkedState),
			Folded = ((kind == MarkdownBlockKind.Heading) ? new bool?(false) : ((bool?)null)),
			SoftTab = ((kind == MarkdownBlockKind.Paragraph || kind == MarkdownBlockKind.Heading || kind == MarkdownBlockKind.List) ? new bool?(false) : ((bool?)null)),
			CustomOrder = ((kind == MarkdownBlockKind.List && listKind == MarkdownListKind.Numbered && string.Equals(customOrder, "1", StringComparison.Ordinal)) ? null : customOrder)
		};
		foreach (MarkdownInlineNode item in ParseInlineNodes(inlineText, parseEmphasis))
		{
			markdownBlockNode.Inlines.Add(item);
		}
		return markdownBlockNode;
	}

	private static MarkdownBlockNode ApplyBlockMetadata(MarkdownBlockNode block, MarkdownLineMetadata metadata)
	{
		if (block == null || metadata == null)
		{
			return block;
		}
		if (metadata.Folded.HasValue && block.Kind == MarkdownBlockKind.Heading)
		{
			block.Folded = metadata.Folded;
		}
		if (metadata.SoftTab.HasValue && (block.Kind == MarkdownBlockKind.Paragraph || block.Kind == MarkdownBlockKind.Heading || (block.Kind == MarkdownBlockKind.List && (block.ListKind == MarkdownListKind.Bullet || block.ListKind == MarkdownListKind.Task))))
		{
			block.SoftTab = metadata.SoftTab;
		}
		if (metadata.Indent.HasValue && block.Kind == MarkdownBlockKind.Paragraph)
		{
			block.Indent = Math.Max(0, metadata.Indent.Value);
		}
		if (!string.IsNullOrWhiteSpace(metadata.CustomOrder) && block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Numbered)
		{
			block.CustomOrder = metadata.CustomOrder;
		}
		return block;
	}

	private static MarkdownLineMetadata ParseLineMetadata(string line)
	{
		string text = line ?? string.Empty;
		MarkdownLineMetadata markdownLineMetadata = new MarkdownLineMetadata
		{
			Content = text
		};
		Match match = Regex.Match(text, "<!--\\s*(\\{.*\\})\\s*-->$", RegexOptions.Singleline);
		if (!match.Success)
		{
			return markdownLineMetadata;
		}
		markdownLineMetadata.Content = text.Substring(0, match.Index);
		string value = match.Groups[1].Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return markdownLineMetadata;
		}
		if (TryMatchBoolean(value, "folded", out var value2))
		{
			markdownLineMetadata.Folded = value2;
		}
		if (TryMatchBoolean(value, "softTab", out var value3))
		{
			markdownLineMetadata.SoftTab = value3;
		}
		Match match2 = Regex.Match(value, "\"indent\"\\s*:\\s*(\\d+)", RegexOptions.IgnoreCase);
		if (match2.Success && int.TryParse(match2.Groups[1].Value, out var result))
		{
			markdownLineMetadata.Indent = Math.Max(0, result);
		}
		Match match3 = Regex.Match(value, "\"customOrder\"\\s*:\\s*(\\d+)", RegexOptions.IgnoreCase);
		if (match3.Success)
		{
			markdownLineMetadata.CustomOrder = match3.Groups[1].Value;
		}
		return markdownLineMetadata;
	}

	private static bool TryMatchBoolean(string json, string key, out bool value)
	{
		value = false;
		Match match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return false;
		}
		value = string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
		return true;
	}

	internal static List<MarkdownInlineNode> ParseInlineNodes(string text, bool parseEmphasis = true)
	{
		List<MarkdownInlineNode> list = new List<MarkdownInlineNode>();
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		StringBuilder stringBuilder = new StringBuilder();
		int bufferSourceStart = -1;
		int num = 0;
		while (num < text.Length)
		{
			char escapedChar;
			int nextIndex2;
			MarkdownInlineNode node;
			int nextIndex3;
			MarkdownInlineNode node2;
			int nextIndex4;
			MarkdownInlineNode node3;
			int nextIndex5;
			MarkdownInlineNode node4;
			int nextIndex6;
			MarkdownInlineNode node5;
			int nextIndex7;
			MarkdownInlineNode node6;
			int nextIndex8;
			MarkdownInlineNode node7;
			int nextIndex9;
			MarkdownInlineNode node8;
			int nextIndex10;
			MarkdownInlineNode node9;
			int nextIndex11;
			MarkdownInlineNode node10;
			int nextIndex12;
			if (TryParseEscapedMultiCharacterMarkLiteral(text, num, out var literal, out var nextIndex))
			{
				EnsureBufferSourceStart(ref bufferSourceStart, num);
				stringBuilder.Append(literal);
				num = nextIndex;
			}
			else if (TryParseEscapedLiteral(text, num, out escapedChar, out nextIndex2))
			{
				EnsureBufferSourceStart(ref bufferSourceStart, num);
				stringBuilder.Append(escapedChar);
				num = nextIndex2;
			}
			else if (TryParseLink(text, num, out node, out nextIndex3, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node);
				num = nextIndex3;
			}
			else if (TryParseCombinedMark(text, num, "***", MarkdownMarkKind.Strong, MarkdownMarkKind.Emphasis, out node2, out nextIndex4, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node2);
				num = nextIndex4;
			}
			else if (TryParseDelimitedMark(text, num, "`", MarkdownMarkKind.Code, out node3, out nextIndex5))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node3);
				num = nextIndex5;
			}
			else if (TryParseHighlight(text, num, "==", out node4, out nextIndex6, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node4);
				num = nextIndex6;
			}
			else if (TryParseHighlight(text, num, "::", out node5, out nextIndex7, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				node5.Marks[0].Color = null;
				list.Add(node5);
				num = nextIndex7;
			}
			else if (TryParseCombinedMark(text, num, "~~~", MarkdownMarkKind.Strikethrough, MarkdownMarkKind.Underline, out node6, out nextIndex8, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node6);
				num = nextIndex8;
			}
			else if (TryParseDelimitedMark(text, num, "**", MarkdownMarkKind.Strong, out node7, out nextIndex9, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node7);
				num = nextIndex9;
			}
			else if (TryParseDelimitedMark(text, num, "~~", MarkdownMarkKind.Strikethrough, out node8, out nextIndex10, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node8);
				num = nextIndex10;
			}
			else if (TryParseDelimitedMark(text, num, "~", MarkdownMarkKind.Underline, out node9, out nextIndex11, parseEmphasis))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node9);
				num = nextIndex11;
			}
			else if (parseEmphasis && TryParseDelimitedMark(text, num, "*", MarkdownMarkKind.Emphasis, out node10, out nextIndex12))
			{
				FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
				list.Add(node10);
				num = nextIndex12;
			}
			else
			{
				EnsureBufferSourceStart(ref bufferSourceStart, num);
				stringBuilder.Append(text[num]);
				num++;
			}
		}
		FlushTextNode(list, stringBuilder, ref bufferSourceStart, num);
		return list;
	}

	private static bool TryParseEscapedMultiCharacterMarkLiteral(string text, int startIndex, out string literal, out int nextIndex)
	{
		literal = null;
		nextIndex = startIndex;
		if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex + 2 >= text.Length || text[startIndex] != '\\')
		{
			return false;
		}
		string[] array = new string[8] { "***", "___", "~~~", "**", "__", "~~", "==", "::" };
		foreach (string text2 in array)
		{
			if (StartsWith(text, startIndex + 1, text2))
			{
				int num = startIndex + 1 + text2.Length;
				int num2 = FindUnescapedDelimiter(text, text2, num);
				if (num2 >= num)
				{
					literal = text.Substring(startIndex + 1, num2 + text2.Length - startIndex - 1);
					nextIndex = num2 + text2.Length;
					return true;
				}
			}
		}
		return false;
	}

	private static int FindUnescapedDelimiter(string text, string delimiter, int startIndex)
	{
		for (int i = Math.Max(0, startIndex); i <= text.Length - delimiter.Length; i++)
		{
			if (text[i] == '\\')
			{
				i++;
			}
			else if (StartsWith(text, i, delimiter))
			{
				return i;
			}
		}
		return -1;
	}

	private static bool TryParseHighlight(string text, int startIndex, string delimiter, out MarkdownInlineNode node, out int nextIndex, bool parseNestedEmphasis = true)
	{
		node = null;
		nextIndex = startIndex;
		if (!StartsWith(text, startIndex, delimiter))
		{
			return false;
		}
		int num = startIndex + delimiter.Length;
		string text2 = null;
		if (num < text.Length && text[num] == '{')
		{
			int num2 = text.IndexOf('}', num);
			if (num2 <= num + 1)
			{
				return false;
			}
			text2 = text.Substring(num + 1, num2 - num - 1);
			num = num2 + 1;
		}
		int num3 = FindHighlightClosingDelimiter(text, num, delimiter);
		if (num3 < 0)
		{
			return false;
		}
		string text3 = text.Substring(num, num3 - num);
		node = new MarkdownInlineNode
		{
			Text = DecodeEscapedInlineText(text3),
			RawText = text3,
			SourceStart = startIndex,
			SourceLength = num3 + delimiter.Length - startIndex,
			OpeningDelimiterLength = num - startIndex,
			ClosingDelimiterLength = delimiter.Length
		};
		node.Marks.Add(new MarkdownMark
		{
			Kind = MarkdownMarkKind.Highlight,
			Color = (string.IsNullOrWhiteSpace(text2) ? null : text2)
		});
		MergeFullyNestedInline(node, text3, nestedMarksFirst: false, parseNestedEmphasis);
		nextIndex = num3 + delimiter.Length;
		return true;
	}

	private static int FindHighlightClosingDelimiter(string text, int contentStart, string delimiter)
	{
		int num = 0;
		int num2 = Math.Max(0, contentStart);
		while (num2 <= text.Length - delimiter.Length)
		{
			if (!StartsWith(text, num2, delimiter))
			{
				num2++;
				continue;
			}
			int num3 = num2 + delimiter.Length;
			if (num3 + 1 < text.Length && text[num3] == '{' && text[num3 + 1] == '#')
			{
				int num4 = text.IndexOf('}', num3 + 2);
				if (num4 > num3 + 2)
				{
					num++;
					num2 = num4 + 1;
					continue;
				}
			}
			if (num == 0)
			{
				return num2;
			}
			num--;
			num2 += delimiter.Length;
		}
		return -1;
	}

	private static bool TryParseEscapedLiteral(string text, int startIndex, out char escapedChar, out int nextIndex)
	{
		escapedChar = '\0';
		nextIndex = startIndex;
		if (startIndex < 0 || startIndex + 1 >= text.Length || text[startIndex] != '\\')
		{
			return false;
		}
		char c = text[startIndex + 1];
		if (!EscapableChars.Contains(c))
		{
			return false;
		}
		escapedChar = c;
		nextIndex = startIndex + 2;
		return true;
	}

	private static bool TryParseLink(string text, int startIndex, out MarkdownInlineNode node, out int nextIndex, bool parseNestedEmphasis = true)
	{
		node = null;
		nextIndex = startIndex;
		if (!StartsWith(text, startIndex, "[") || (startIndex > 0 && text[startIndex - 1] == '!'))
		{
			return false;
		}
		int num = FindInlineLinkTextEnd(text, startIndex);
		if (num <= startIndex + 1 || num + 1 >= text.Length || text[num + 1] != '(')
		{
			return false;
		}
		int num2 = FindLinkUrlEnd(text, num + 2);
		if (num2 <= num + 2)
		{
			return false;
		}
		string text2 = text.Substring(startIndex + 1, num - startIndex - 1);
		string url = text.Substring(num + 2, num2 - num - 2);
		node = new MarkdownInlineNode
		{
			Text = DecodeEscapedInlineText(text2),
			RawText = text2,
			SourceStart = startIndex,
			SourceLength = num2 + 1 - startIndex,
			OpeningDelimiterLength = 1,
			ClosingDelimiterLength = num2 + 1 - num
		};
		node.Marks.Add(new MarkdownMark
		{
			Kind = MarkdownMarkKind.Link,
			Url = url
		});
		MergeFullyNestedInline(node, text2, nestedMarksFirst: false, parseNestedEmphasis);
		nextIndex = num2 + 1;
		return true;
	}

	internal static int FindInlineLinkTextEnd(string text, int startIndex)
	{
		for (int i = startIndex + 1; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '\\' && i + 1 < text.Length && (text[i + 1] == '[' || text[i + 1] == '\\'))
			{
				i++;
				continue;
			}
			switch (c)
			{
			case '\n':
			case '\r':
			case '[':
				return -1;
			case ']':
				return i;
			}
		}
		return -1;
	}

	private static int FindLinkUrlEnd(string text, int urlStart)
	{
		int num = 0;
		for (int i = urlStart; i < text.Length; i++)
		{
			if (text[i] == '\\')
			{
				i++;
			}
			else if (text[i] == '(')
			{
				num++;
			}
			else if (text[i] == ')')
			{
				if (num == 0)
				{
					return i;
				}
				num--;
			}
		}
		return -1;
	}

	private static bool TryParseCombinedMark(string text, int startIndex, string delimiter, MarkdownMarkKind firstKind, MarkdownMarkKind secondKind, out MarkdownInlineNode node, out int nextIndex, bool parseNestedEmphasis = true)
	{
		node = null;
		nextIndex = startIndex;
		if (!StartsWith(text, startIndex, delimiter))
		{
			return false;
		}
		int num = startIndex + delimiter.Length;
		int num2 = ((delimiter == "***") ? FindStarClosingDelimiter(text, delimiter, num) : text.IndexOf(delimiter, num, StringComparison.Ordinal));
		if (num2 < 0 || num2 == num)
		{
			return false;
		}
		string text2 = text.Substring(num, num2 - num);
		node = new MarkdownInlineNode
		{
			Text = DecodeEscapedInlineText(text2),
			RawText = text2,
			SourceStart = startIndex,
			SourceLength = num2 + delimiter.Length - startIndex,
			OpeningDelimiterLength = delimiter.Length,
			ClosingDelimiterLength = delimiter.Length
		};
		node.Marks.Add(new MarkdownMark
		{
			Kind = firstKind
		});
		node.Marks.Add(new MarkdownMark
		{
			Kind = secondKind
		});
		MergeFullyNestedInline(node, text2, nestedMarksFirst: true, parseNestedEmphasis);
		nextIndex = num2 + delimiter.Length;
		return true;
	}

	private static bool TryParseOrderedCombinedMark(string text, int startIndex, string openingDelimiter, string closingDelimiter, MarkdownMarkKind firstKind, MarkdownMarkKind secondKind, out MarkdownInlineNode node, out int nextIndex)
	{
		node = null;
		nextIndex = startIndex;
		if (!StartsWith(text, startIndex, openingDelimiter))
		{
			return false;
		}
		int num = startIndex + openingDelimiter.Length;
		if (openingDelimiter.Length == 2 && openingDelimiter[0] != openingDelimiter[1] && num < text.Length && text[num] == openingDelimiter[1])
		{
			return false;
		}
		int num2 = text.IndexOf(closingDelimiter, num, StringComparison.Ordinal);
		if (num2 < 0 || num2 == num)
		{
			return false;
		}
		string text2 = text.Substring(num, num2 - num);
		node = new MarkdownInlineNode
		{
			Text = DecodeEscapedInlineText(text2),
			RawText = text2,
			SourceStart = startIndex,
			SourceLength = num2 + closingDelimiter.Length - startIndex,
			OpeningDelimiterLength = openingDelimiter.Length,
			ClosingDelimiterLength = closingDelimiter.Length
		};
		node.Marks.Add(new MarkdownMark
		{
			Kind = firstKind
		});
		node.Marks.Add(new MarkdownMark
		{
			Kind = secondKind
		});
		nextIndex = num2 + closingDelimiter.Length;
		return true;
	}

	private static bool TryParseDelimitedMark(string text, int startIndex, string delimiter, MarkdownMarkKind kind, out MarkdownInlineNode node, out int nextIndex, bool parseNestedEmphasis = true)
	{
		node = null;
		nextIndex = startIndex;
		if (!StartsWith(text, startIndex, delimiter))
		{
			return false;
		}
		int num = startIndex + delimiter.Length;
		int num2 = ((delimiter == "**") ? FindStarClosingDelimiter(text, delimiter, num) : text.IndexOf(delimiter, num, StringComparison.Ordinal));
		int outerClosingDelimiterStart2;
		if (((kind == MarkdownMarkKind.Strong && (string.Equals(delimiter, "**", StringComparison.Ordinal) || string.Equals(delimiter, "__", StringComparison.Ordinal))) || (kind == MarkdownMarkKind.Strikethrough && string.Equals(delimiter, "~~", StringComparison.Ordinal))) && TryGetSharedOuterAndInnerClosingDelimiter(text, num, num2, delimiter[0], out var outerClosingDelimiterStart))
		{
			num2 = outerClosingDelimiterStart;
		}
		else if (((kind == MarkdownMarkKind.Emphasis && (string.Equals(delimiter, "*", StringComparison.Ordinal) || string.Equals(delimiter, "_", StringComparison.Ordinal))) || (kind == MarkdownMarkKind.Underline && string.Equals(delimiter, "~", StringComparison.Ordinal))) && TryGetSingleOuterClosingDelimiterAfterNestedDoubleDelimiter(text, num, num2, delimiter[0], out outerClosingDelimiterStart2))
		{
			num2 = outerClosingDelimiterStart2;
		}
		if (num2 < 0 || num2 == num)
		{
			return false;
		}
		string text2 = text.Substring(num, num2 - num);
		node = new MarkdownInlineNode
		{
			Text = DecodeEscapedInlineText(text2),
			RawText = text2,
			SourceStart = startIndex,
			SourceLength = num2 + delimiter.Length - startIndex,
			OpeningDelimiterLength = delimiter.Length,
			ClosingDelimiterLength = delimiter.Length
		};
		node.Marks.Add(new MarkdownMark
		{
			Kind = kind
		});
		if (kind != MarkdownMarkKind.Code)
		{
			MergeFullyNestedInline(node, text2, nestedMarksFirst: false, parseNestedEmphasis);
		}
		nextIndex = num2 + delimiter.Length;
		return true;
	}

	private static int FindStarClosingDelimiter(string text, string delimiter, int contentStart)
	{
		for (int i = contentStart; i < text.Length; i++)
		{
			MarkdownInlineNode node;
			int nextIndex;
			if (text[i] == '\\')
			{
				i++;
			}
			else if (text[i] == '`' && TryParseDelimitedMark(text, i, "`", MarkdownMarkKind.Code, out node, out nextIndex))
			{
				i = nextIndex - 1;
			}
			else if (StartsWith(text, i, delimiter))
			{
				return i;
			}
		}
		return -1;
	}

	private static void MergeFullyNestedInline(MarkdownInlineNode node, string innerText, bool nestedMarksFirst, bool parseEmphasis = true)
	{
		if (node == null || string.IsNullOrEmpty(innerText))
		{
			return;
		}
		List<MarkdownInlineNode> list = ParseInlineNodes(innerText, parseEmphasis);
		if (list.Count != 1)
		{
			return;
		}
		MarkdownInlineNode markdownInlineNode = list[0];
		if (markdownInlineNode != null && markdownInlineNode.SourceStart == 0 && markdownInlineNode.SourceLength == innerText.Length && markdownInlineNode.Marks.Count != 0)
		{
			node.Text = markdownInlineNode.Text;
			node.RawText = markdownInlineNode.RawText;
			node.OpeningDelimiterLength += markdownInlineNode.OpeningDelimiterLength;
			node.ClosingDelimiterLength += markdownInlineNode.ClosingDelimiterLength;
			List<MarkdownMark> collection = markdownInlineNode.Marks.Where((MarkdownMark mark) => mark != null && !ContainsEquivalentMark(node.Marks, mark)).ToList();
			if (nestedMarksFirst)
			{
				node.Marks.InsertRange(0, collection);
			}
			else
			{
				node.Marks.AddRange(collection);
			}
		}
	}

	private static bool ContainsEquivalentMark(IEnumerable<MarkdownMark> marks, MarkdownMark candidate)
	{
		return marks.Any((MarkdownMark mark) => mark != null && mark.Kind == candidate.Kind && string.Equals(mark.Color, candidate.Color, StringComparison.Ordinal) && string.Equals(mark.Url, candidate.Url, StringComparison.Ordinal));
	}

	private static bool TryGetSharedOuterAndInnerClosingDelimiter(string text, int contentStart, int firstOuterClosingDelimiterStart, char delimiterCharacter, out int outerClosingDelimiterStart)
	{
		outerClosingDelimiterStart = firstOuterClosingDelimiterStart;
		if (firstOuterClosingDelimiterStart < contentStart || firstOuterClosingDelimiterStart + 2 >= text.Length || text[firstOuterClosingDelimiterStart] != delimiterCharacter || text[firstOuterClosingDelimiterStart + 1] != delimiterCharacter || text[firstOuterClosingDelimiterStart + 2] != delimiterCharacter || !ContainsUnescapedStandaloneDelimiter(text, contentStart, firstOuterClosingDelimiterStart, delimiterCharacter))
		{
			return false;
		}
		outerClosingDelimiterStart = firstOuterClosingDelimiterStart + 1;
		return true;
	}

	private static bool TryGetSingleOuterClosingDelimiterAfterNestedDoubleDelimiter(string text, int contentStart, int firstOuterClosingDelimiterStart, char delimiterCharacter, out int outerClosingDelimiterStart)
	{
		outerClosingDelimiterStart = firstOuterClosingDelimiterStart;
		if (firstOuterClosingDelimiterStart < contentStart || firstOuterClosingDelimiterStart + 1 >= text.Length || text[firstOuterClosingDelimiterStart] != delimiterCharacter || text[firstOuterClosingDelimiterStart + 1] != delimiterCharacter)
		{
			return false;
		}
		string value = new string(delimiterCharacter, 2);
		int num = text.IndexOf(value, firstOuterClosingDelimiterStart + 2, StringComparison.Ordinal);
		if (num < firstOuterClosingDelimiterStart + 2)
		{
			return false;
		}
		if (num + 2 < text.Length && text[num + 2] == delimiterCharacter)
		{
			outerClosingDelimiterStart = num + 2;
			return true;
		}
		int num2 = FindUnescapedStandaloneDelimiter(text, num + 2, text.Length, delimiterCharacter);
		if (num2 < 0)
		{
			return false;
		}
		outerClosingDelimiterStart = num2;
		return true;
	}

	private static bool ContainsUnescapedStandaloneDelimiter(string text, int startIndex, int endIndex, char delimiterCharacter)
	{
		return FindUnescapedStandaloneDelimiter(text, startIndex, endIndex, delimiterCharacter) >= 0;
	}

	private static int FindUnescapedStandaloneDelimiter(string text, int startIndex, int endIndex, char delimiterCharacter)
	{
		if (string.IsNullOrEmpty(text))
		{
			return -1;
		}
		int num = Math.Max(0, startIndex);
		int num2 = Math.Min(text.Length, Math.Max(num, endIndex));
		for (int i = num; i < num2; i++)
		{
			if (text[i] == delimiterCharacter && !IsEscaped(text, i) && (i <= num || text[i - 1] != delimiterCharacter) && (i + 1 >= num2 || text[i + 1] != delimiterCharacter))
			{
				return i;
			}
		}
		return -1;
	}

	private static bool IsEscaped(string text, int index)
	{
		int num = 0;
		int num2 = index - 1;
		while (num2 >= 0 && text[num2] == '\\')
		{
			num++;
			num2--;
		}
		return num % 2 == 1;
	}

	private static string DecodeEscapedInlineText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\\' && i + 1 < text.Length && EscapableChars.Contains(text[i + 1]))
			{
				stringBuilder.Append(text[i + 1]);
				i++;
			}
			else
			{
				stringBuilder.Append(text[i]);
			}
		}
		return stringBuilder.ToString();
	}

	public static string GetPlainTextWithBlockMarkers(string markdown)
	{
		return string.Join("\n", Parse(markdown).Blocks.Select((MarkdownBlockNode block) => SerializeBlock(block, MarkdownSerializeMode.Standard, plainInlines: true)));
	}

	private static string SerializeBlock(MarkdownBlockNode block, MarkdownSerializeMode mode, bool plainInlines = false)
	{
		string text = new string(' ', Math.Max(0, block?.Indent ?? 0));
		string listIndentPrefix = GetListIndentPrefix(block);
		string text2 = (plainInlines ? GetInlinePlainText(block?.Inlines) : SerializeInlineNodes(block?.Inlines, mode));
		if (block == null)
		{
			return string.Empty;
		}
		switch (block.Kind)
		{
		case MarkdownBlockKind.Heading:
		{
			int count = Math.Max(1, Math.Min(6, block.Level ?? 1));
			return AppendBlockMetadata(text + new string('#', count) + " " + text2, block);
		}
		case MarkdownBlockKind.BlockQuote:
		{
			string blockIndentPrefix = GetBlockIndentPrefix(block);
			return AppendBlockMetadata((!string.IsNullOrEmpty(text2)) ? ("> " + blockIndentPrefix + text2) : ((blockIndentPrefix.Length == 0) ? ">" : ("> " + blockIndentPrefix)), block);
		}
		case MarkdownBlockKind.HorizontalRule:
			return text + "---";
		case MarkdownBlockKind.CodeBlock:
		{
			string text5 = block.Language ?? string.Empty;
			string text6 = text + "```" + text5;
			string text7 = EscapeCodeBlockText(block.CodeText ?? string.Empty);
			if (!string.IsNullOrEmpty(text7))
			{
				return text6 + "\n" + text7 + "\n" + text + "```";
			}
			return text6 + "\n" + text + "```";
		}
		case MarkdownBlockKind.FileBlock:
		{
			if (plainInlines)
			{
				return text + "[" + Utils.GetString((block.FileKind == MarkdownFileBlockKind.File) ? "File" : "Image") + "]";
			}
			string text3 = ((!string.IsNullOrEmpty(block.AltText)) ? block.AltText : ((block.FileKind == MarkdownFileBlockKind.File) ? "file" : "image"));
			string text4 = ((block.FileKind == MarkdownFileBlockKind.Image || string.IsNullOrEmpty(block.Caption)) ? string.Empty : (" \"" + block.Caption + "\""));
			return text + "![" + text3 + "](" + (block.Source ?? string.Empty) + text4 + ")";
		}
		case MarkdownBlockKind.List:
			return block.ListKind.GetValueOrDefault() switch
			{
				MarkdownListKind.Task => AppendBlockMetadata(listIndentPrefix + ((block.Checked == true) ? "- [x] " : "- [ ] ") + text2, block), 
				MarkdownListKind.Numbered => AppendBlockMetadata(listIndentPrefix + (string.IsNullOrWhiteSpace(block.CustomOrder) ? "1" : block.CustomOrder) + ". " + text2, block), 
				_ => AppendBlockMetadata(listIndentPrefix + "- " + text2, block), 
			};
		case MarkdownBlockKind.Paragraph:
			if (plainInlines)
			{
				return text2;
			}
			return AppendBlockMetadata(EscapeParagraphLikeFileBlock(EscapeParagraphBlockPrefixSyntax(text2)), block);
		default:
			return AppendBlockMetadata(text + EscapeParagraphLikeFileBlock(EscapeParagraphBlockPrefixSyntax(text2)), block);
		}
	}

	private static int ConvertIndentLevelsToWhitespaceCount(int indentLevels)
	{
		return Math.Max(0, indentLevels) * 4;
	}

	private static string GetListIndentPrefix(MarkdownBlockNode block)
	{
		return GetBlockIndentPrefix(block);
	}

	private static string GetBlockIndentPrefix(MarkdownBlockNode block)
	{
		int num = Math.Max(0, block?.Indent ?? 0);
		if (num <= 0)
		{
			return string.Empty;
		}
		int count = Math.Max(1, (num + 4 - 1) / 4);
		return new string('\t', count);
	}

	private static string SerializeInlineNodes(IEnumerable<MarkdownInlineNode> inlines, MarkdownSerializeMode mode)
	{
		if (inlines == null)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (MarkdownInlineNode inline in inlines)
		{
			stringBuilder.Append(SerializeInlineNode(inline, mode));
		}
		return stringBuilder.ToString();
	}

	private static string SerializeInlineNode(MarkdownInlineNode inline, MarkdownSerializeMode mode)
	{
		if (inline == null)
		{
			return string.Empty;
		}
		List<MarkdownMark> list = inline.Marks.Where((MarkdownMark mark) => mark != null).ToList();
		string text = inline.Text ?? string.Empty;
		if (list.Count == 0)
		{
			return EscapePlainText(text);
		}
		text = EscapePlainTextForPersistence(text);
		bool flag = list.Any((MarkdownMark mark) => mark.Kind == MarkdownMarkKind.Strong);
		bool flag2 = list.Any((MarkdownMark mark) => mark.Kind == MarkdownMarkKind.Emphasis);
		bool flag3 = list.Any((MarkdownMark mark) => mark.Kind == MarkdownMarkKind.Underline);
		bool num = list.Any((MarkdownMark mark) => mark.Kind == MarkdownMarkKind.Strikethrough);
		bool flag4 = flag && flag2;
		bool flag5 = num && flag3;
		MarkdownMarkKind[] array = new MarkdownMarkKind[6]
		{
			MarkdownMarkKind.Code,
			MarkdownMarkKind.Emphasis,
			MarkdownMarkKind.Underline,
			MarkdownMarkKind.Strong,
			MarkdownMarkKind.Strikethrough,
			MarkdownMarkKind.Highlight
		};
		foreach (MarkdownMarkKind kind in array)
		{
			if (!(kind == MarkdownMarkKind.Strong && flag4) && !(kind == MarkdownMarkKind.Emphasis && flag4) && !(kind == MarkdownMarkKind.Underline && flag5) && !(kind == MarkdownMarkKind.Strikethrough && flag5))
			{
				MarkdownMark markdownMark = list.Where((MarkdownMark candidate) => candidate.Kind == kind).OrderBy((MarkdownMark candidate) => candidate.Color ?? string.Empty, StringComparer.Ordinal).FirstOrDefault();
				if (markdownMark != null)
				{
					text = ApplyMark(text, markdownMark, mode);
				}
			}
		}
		if (flag4)
		{
			text = "***" + text + "***";
		}
		if (flag5)
		{
			text = "~~~" + text + "~~~";
		}
		foreach (MarkdownMark item in list.Where((MarkdownMark mark) => mark.Kind == MarkdownMarkKind.Link).OrderBy((MarkdownMark mark) => mark.Url ?? string.Empty, StringComparer.Ordinal))
		{
			text = ApplyMark(text, item, mode);
		}
		return text;
	}

	private static string ApplyMark(string text, MarkdownMark mark, MarkdownSerializeMode mode)
	{
		if (mark == null)
		{
			return text;
		}
		switch (mark.Kind)
		{
		case MarkdownMarkKind.Link:
			return "[" + text + "](" + (mark.Url ?? string.Empty) + ")";
		case MarkdownMarkKind.Code:
			return "`" + text + "`";
		case MarkdownMarkKind.Highlight:
		{
			string text2 = ((mode == MarkdownSerializeMode.Compatibility) ? "::" : "==");
			string text3 = (string.IsNullOrWhiteSpace(mark.Color) ? string.Empty : ("{" + mark.Color + "}"));
			return text2 + text3 + text + text2;
		}
		case MarkdownMarkKind.Strong:
			return "**" + text + "**";
		case MarkdownMarkKind.Emphasis:
			return "*" + text + "*";
		case MarkdownMarkKind.Underline:
			return "~" + text + "~";
		case MarkdownMarkKind.Strikethrough:
			return "~~" + text + "~~";
		default:
			return text;
		}
	}

	private static void EnsureBufferSourceStart(ref int bufferSourceStart, int startIndex)
	{
		if (bufferSourceStart < 0)
		{
			bufferSourceStart = startIndex;
		}
	}

	private static void FlushTextNode(List<MarkdownInlineNode> nodes, StringBuilder buffer, ref int bufferSourceStart, int currentIndex)
	{
		if (buffer.Length != 0)
		{
			nodes.Add(new MarkdownInlineNode
			{
				Text = buffer.ToString(),
				SourceStart = Math.Max(0, bufferSourceStart),
				SourceLength = Math.Max(0, currentIndex - Math.Max(0, bufferSourceStart))
			});
			buffer.Clear();
			bufferSourceStart = -1;
		}
	}

	private static string EscapePlainText(string text)
	{
		int[] rawToEscapedOffsetMap;
		return EscapePlainText(text, out rawToEscapedOffsetMap);
	}

	private static string EscapePlainText(string text, out int[] rawToEscapedOffsetMap)
	{
		return EscapePlainText(text, out rawToEscapedOffsetMap, escapeLiteralInlineCharacters: false, includeColonHighlight: false, includeLegacyUnderscoreSyntax: false, includeGeneralSyntax: true);
	}

	private static string EscapePlainText(string text, out int[] rawToEscapedOffsetMap, bool escapeLiteralInlineCharacters, bool includeColonHighlight, bool includeLegacyUnderscoreSyntax, bool includeGeneralSyntax, bool escapeStarCharacters = false)
	{
		if (string.IsNullOrEmpty(text))
		{
			rawToEscapedOffsetMap = new int[1];
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		rawToEscapedOffsetMap = new int[text.Length + 1];
		int num = 0;
		while (num < text.Length)
		{
			rawToEscapedOffsetMap[num] = stringBuilder.Length;
			int num2 = GetPlainTextEscapeLength(text, num, includeColonHighlight, includeLegacyUnderscoreSyntax, includeGeneralSyntax);
			if (num2 == 0 && ((escapeLiteralInlineCharacters && InlinePersistenceEscapableChars.Contains(text[num])) || (escapeStarCharacters && text[num] == '*')))
			{
				num2 = 1;
			}
			if (num2 > 0)
			{
				for (int i = 0; i < num2 && num + i < text.Length; i++)
				{
					rawToEscapedOffsetMap[num + i] = stringBuilder.Length;
					stringBuilder.Append('\\');
					stringBuilder.Append(text[num + i]);
				}
				num += num2;
			}
			else
			{
				stringBuilder.Append(text[num]);
				num++;
			}
		}
		rawToEscapedOffsetMap[text.Length] = stringBuilder.Length;
		return stringBuilder.ToString();
	}

	private static string EscapeParagraphLikeFileBlock(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		if (!TryParseFileBlock(text, out var _))
		{
			return text;
		}
		return "!\\" + text.Substring(1);
	}

	private static string EscapeParagraphBlockPrefixSyntax(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = null;
		int num = 0;
		int lineStart = 0;
		for (int i = 0; i <= text.Length; i++)
		{
			if (i < text.Length && text[i] != '\n')
			{
				continue;
			}
			int num2 = FindParagraphBlockPrefixEscapeIndex(text, lineStart, i);
			if (num2 >= 0)
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(text.Length + 4);
				}
				if (num2 > num)
				{
					stringBuilder.Append(text, num, num2 - num);
				}
				stringBuilder.Append('\\');
				num = num2;
			}
			lineStart = i + 1;
		}
		if (stringBuilder == null)
		{
			return text;
		}
		if (num < text.Length)
		{
			stringBuilder.Append(text, num, text.Length - num);
		}
		return stringBuilder.ToString();
	}

	private static int FindParagraphBlockPrefixEscapeIndex(string text, int lineStart, int lineEnd)
	{
		int i = lineStart;
		while (i < lineEnd)
		{
			if (text[i] == '\t')
			{
				i++;
				continue;
			}
			if (i + 4 > lineEnd || text[i] != ' ' || text[i + 1] != ' ' || text[i + 2] != ' ' || text[i + 3] != ' ')
			{
				break;
			}
			i += 4;
		}
		for (; i < lineEnd && IsEditorOnlyLeadingCharacter(text[i]); i++)
		{
		}
		int num = 0;
		while (i < lineEnd && num < 3 && (text[i] == ' ' || text[i] == '\u00a0'))
		{
			i++;
			num++;
		}
		if (i >= lineEnd)
		{
			return -1;
		}
		if (text[i] == '!' && i + 1 < lineEnd && text[i + 1] == '[')
		{
			return i + 1;
		}
		if (text[i] == '`' && i + 2 < lineEnd && text[i + 1] == '`' && text[i + 2] == '`')
		{
			return i;
		}
		switch (text[i])
		{
		case '#':
		case '-':
		case '>':
			return i;
		case '*':
		case '+':
			if (i + 1 >= lineEnd || text[i + 1] != ' ')
			{
				return -1;
			}
			return i;
		default:
			if (!char.IsDigit(text[i]))
			{
				return -1;
			}
			for (; i < lineEnd && char.IsDigit(text[i]); i++)
			{
			}
			if (i < lineEnd && text[i] == '.' && (i + 1 == lineEnd || text[i + 1] == ' '))
			{
				return i;
			}
			return -1;
		}
	}

	private static bool IsEditorOnlyLeadingCharacter(char ch)
	{
		if (ch != '\u200b' && ch != '\u200c' && ch != '\u200d' && ch != '\u200e' && ch != '\u200f' && ch != '\u2060' && ch != '\u2061' && ch != '\u2062' && ch != '\u2063' && ch != '\u2064')
		{
			return ch == '\u206a';
		}
		return true;
	}

	private static int GetPlainTextEscapeLength(string text, int startIndex, bool includeColonHighlight = false, bool includeLegacyUnderscoreSyntax = false, bool includeGeneralSyntax = true)
	{
		if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex >= text.Length)
		{
			return 0;
		}
		if (includeGeneralSyntax && text[startIndex] == '\\')
		{
			return 1;
		}
		if (includeGeneralSyntax && IsHtmlCommentStartBang(text, startIndex))
		{
			return 1;
		}
		if (includeGeneralSyntax && TryParseLink(text, startIndex, out var node, out var nextIndex))
		{
			return 1;
		}
		if (TryParseCombinedMark(text, startIndex, "***", MarkdownMarkKind.Strong, MarkdownMarkKind.Emphasis, out node, out nextIndex))
		{
			return 3;
		}
		if (includeLegacyUnderscoreSyntax && TryParseCombinedMark(text, startIndex, "___", MarkdownMarkKind.Strong, MarkdownMarkKind.Emphasis, out node, out nextIndex))
		{
			return 3;
		}
		if (TryParseOrderedCombinedMark(text, startIndex, "~*", "*~", MarkdownMarkKind.Underline, MarkdownMarkKind.Emphasis, out node, out nextIndex))
		{
			return 2;
		}
		if (TryParseOrderedCombinedMark(text, startIndex, "*~", "~*", MarkdownMarkKind.Emphasis, MarkdownMarkKind.Underline, out node, out nextIndex))
		{
			return 2;
		}
		if (TryParseHighlight(text, startIndex, "==", out node, out nextIndex))
		{
			return 2;
		}
		if (includeColonHighlight && TryParseHighlight(text, startIndex, "::", out node, out nextIndex))
		{
			return 2;
		}
		if (TryParseDelimitedMark(text, startIndex, "**", MarkdownMarkKind.Strong, out node, out nextIndex))
		{
			return 2;
		}
		if (includeLegacyUnderscoreSyntax && TryParseDelimitedMark(text, startIndex, "__", MarkdownMarkKind.Strong, out node, out nextIndex))
		{
			return 2;
		}
		if (TryParseCombinedMark(text, startIndex, "~~~", MarkdownMarkKind.Strikethrough, MarkdownMarkKind.Underline, out node, out nextIndex))
		{
			return 3;
		}
		if (TryParseDelimitedMark(text, startIndex, "~~", MarkdownMarkKind.Strikethrough, out node, out nextIndex))
		{
			return 2;
		}
		if (TryParseDelimitedMark(text, startIndex, "`", MarkdownMarkKind.Code, out node, out nextIndex))
		{
			return 1;
		}
		if (TryParseDelimitedMark(text, startIndex, "~", MarkdownMarkKind.Underline, out node, out nextIndex))
		{
			return 1;
		}
		if (TryParseDelimitedMark(text, startIndex, "*", MarkdownMarkKind.Emphasis, out node, out nextIndex))
		{
			return 1;
		}
		if (includeLegacyUnderscoreSyntax && TryParseDelimitedMark(text, startIndex, "_", MarkdownMarkKind.Emphasis, out node, out nextIndex))
		{
			return 1;
		}
		if (includeGeneralSyntax && IsClosingEmphasisDelimiter(text, startIndex, '*'))
		{
			return 1;
		}
		if (includeGeneralSyntax && includeLegacyUnderscoreSyntax && IsClosingEmphasisDelimiter(text, startIndex, '_'))
		{
			return 1;
		}
		return 0;
	}

	private static bool IsHtmlCommentStartBang(string text, int index)
	{
		if (index > 0 && text[index] == '!' && text[index - 1] == '<' && index + 2 < text.Length && text[index + 1] == '-')
		{
			return text[index + 2] == '-';
		}
		return false;
	}

	private static bool IsClosingEmphasisDelimiter(string text, int startIndex, char delimiter)
	{
		if (string.IsNullOrEmpty(text) || startIndex <= 0 || startIndex >= text.Length || text[startIndex] != delimiter)
		{
			return false;
		}
		if (startIndex + 1 < text.Length && text[startIndex + 1] == delimiter)
		{
			return false;
		}
		for (int num = startIndex - 1; num >= 0; num--)
		{
			if (text[num] == delimiter && (num <= 0 || text[num - 1] != '\\'))
			{
				return num + 1 < startIndex;
			}
		}
		return false;
	}

	private static string AppendBlockMetadata(string markdown, MarkdownBlockNode block)
	{
		if (block == null)
		{
			return markdown ?? string.Empty;
		}
		List<string> list = new List<string>();
		if (block.Kind == MarkdownBlockKind.Heading && block.Folded == true)
		{
			list.Add("\"folded\":true");
		}
		if ((block.Kind == MarkdownBlockKind.Paragraph || block.Kind == MarkdownBlockKind.Heading || (block.Kind == MarkdownBlockKind.List && (block.ListKind == MarkdownListKind.Bullet || block.ListKind == MarkdownListKind.Task))) && block.SoftTab == true)
		{
			list.Add("\"softTab\":true");
		}
		if (block.Kind == MarkdownBlockKind.Paragraph && block.Indent > 0)
		{
			list.Add("\"indent\":" + block.Indent);
		}
		if (block.Kind == MarkdownBlockKind.List && block.ListKind == MarkdownListKind.Numbered && !string.IsNullOrWhiteSpace(block.CustomOrder) && !string.Equals(block.CustomOrder, "1", StringComparison.Ordinal))
		{
			list.Add("\"customOrder\":" + block.CustomOrder);
		}
		if (list.Count == 0)
		{
			return markdown ?? string.Empty;
		}
		return (markdown ?? string.Empty) + "<!-- {" + string.Join(",", list) + "} -->";
	}

	private static string NormalizeNewLines(string markdown)
	{
		return (markdown ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
	}

	private static bool StartsWith(string text, int startIndex, string value)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value))
		{
			return false;
		}
		if (startIndex < 0 || startIndex + value.Length > text.Length)
		{
			return false;
		}
		return string.Compare(text, startIndex, value, 0, value.Length, StringComparison.Ordinal) == 0;
	}
}
