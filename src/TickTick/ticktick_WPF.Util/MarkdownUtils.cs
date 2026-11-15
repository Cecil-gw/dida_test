using System;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Util;

public class MarkdownUtils
{
	private const string FoldedMetadataCommentPattern = "(?:\\\\?<\\\\?!--|\\\\?&lt;\\\\?!--)\\s*\\\\?\\{\\s*\\\\?\"folded\\\\?\"\\s*\\\\?:\\s*true\\s*\\\\?\\}\\s*(?:--\\\\?>|--\\\\?&gt;)";

	private static readonly Regex FoldedMetadataLineRegex = new Regex("(?m)^[ \\t]*(?:\\\\?<\\\\?!--|\\\\?&lt;\\\\?!--)\\s*\\\\?\\{\\s*\\\\?\"folded\\\\?\"\\s*\\\\?:\\s*true\\s*\\\\?\\}\\s*(?:--\\\\?>|--\\\\?&gt;)[ \\t]*(?:\\r?\\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex FoldedMetadataCommentRegex = new Regex("(?:\\\\?<\\\\?!--|\\\\?&lt;\\\\?!--)\\s*\\\\?\\{\\s*\\\\?\"folded\\\\?\"\\s*\\\\?:\\s*true\\s*\\\\?\\}\\s*(?:--\\\\?>|--\\\\?&gt;)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex BlockMetadataCommentRegex = new Regex("<!--\\s*(?<json>\\{.*?\\})\\s*-->", RegexOptions.Compiled | RegexOptions.Singleline);

	private static readonly Regex BlockMetadataKeyRegex = new Regex("\"(?:folded|softTab|indent|customOrder)\"\\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static string RemoveFoldedMetadataComments(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string input = FoldedMetadataLineRegex.Replace(text, string.Empty);
		return FoldedMetadataCommentRegex.Replace(input, string.Empty);
	}

	public static string RemoveBlockMetadataComments(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string input = RemoveFoldedMetadataComments(text);
		return BlockMetadataCommentRegex.Replace(input, (Match match) => (!BlockMetadataKeyRegex.IsMatch(match.Groups["json"].Value)) ? match.Value : string.Empty);
	}

	public static string GetParagraphType(string checkText)
	{
		checkText = MarkDownEditor.NormalizeDisplayTaskMarkersForPersistence(checkText ?? string.Empty);
		string text = checkText.TrimStart();
		if (text.StartsWith("/") || text.StartsWith("、"))
		{
			checkText = text.Substring(1);
		}
		MarkdownSemanticDocument markdownSemanticDocument = MarkdownSemanticConverter.Parse(checkText);
		if (markdownSemanticDocument.Blocks.Count == 1)
		{
			return GetBlockAction(markdownSemanticDocument.Blocks[0], checkText);
		}
		string blockAction = GetBlockAction(markdownSemanticDocument.Blocks[0], checkText);
		for (int i = 1; i < markdownSemanticDocument.Blocks.Count; i++)
		{
			if (GetBlockAction(markdownSemanticDocument.Blocks[i], checkText) != blockAction)
			{
				return string.Empty;
			}
		}
		return blockAction;
	}

	private static string GetBlockAction(MarkdownBlockNode block, string originalText)
	{
		if (block == null)
		{
			return string.Empty;
		}
		switch (block.Kind)
		{
		case MarkdownBlockKind.Heading:
			return Math.Max(1, Math.Min(6, block.Level ?? 1)) switch
			{
				1 => "Heading1", 
				2 => "Heading2", 
				3 => "Heading3", 
				4 => "Heading4", 
				5 => "Heading5", 
				_ => "Heading6", 
			};
		case MarkdownBlockKind.BlockQuote:
			return "Quote";
		case MarkdownBlockKind.List:
			return block.ListKind switch
			{
				MarkdownListKind.Task => "CheckItem", 
				MarkdownListKind.Numbered => "NumberedList", 
				_ => "BulletList", 
			};
		case MarkdownBlockKind.CodeBlock:
			return "CodeBlock";
		case MarkdownBlockKind.FileBlock:
			return "Text";
		case MarkdownBlockKind.HorizontalRule:
			return "SplitLine";
		default:
			if (!block.Inlines.Any((MarkdownInlineNode inline) => !string.IsNullOrWhiteSpace(inline?.Text)))
			{
				return string.Empty;
			}
			return "Text";
		}
	}

	public static string GetRelatedText(TextDocument document, string fullText, int caretOffset, int selectionStart, int selectionLength, string selectedText)
	{
		fullText = MarkDownEditor.NormalizeDisplayTaskMarkersForPersistence(fullText ?? string.Empty);
		selectedText = MarkDownEditor.NormalizeDisplayTaskMarkersForPersistence(selectedText ?? string.Empty);
		if (document == null || string.IsNullOrEmpty(fullText))
		{
			return string.Empty;
		}
		if (selectionLength > 0)
		{
			int num = Math.Max(0, Math.Min(selectionStart, fullText.Length));
			int num2 = Math.Max(num, Math.Min(selectionStart + selectionLength, fullText.Length));
			if (num2 > num)
			{
				num2--;
			}
			int val = FindLineStart(fullText, num);
			int val2 = FindLineEnd(fullText, num2);
			val = Math.Max(0, Math.Min(val, fullText.Length));
			val2 = Math.Max(val, Math.Min(val2, fullText.Length));
			return fullText.Substring(val, val2 - val);
		}
		int offset = Math.Max(0, Math.Min(caretOffset, fullText.Length));
		int val3 = FindLineStart(fullText, offset);
		int val4 = FindLineEnd(fullText, offset);
		val3 = Math.Max(0, Math.Min(val3, fullText.Length));
		val4 = Math.Max(val3, Math.Min(val4, fullText.Length));
		return fullText.Substring(val3, val4 - val3);
	}

	private static int FindLineStart(string text, int offset)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int num;
		for (num = Math.Max(0, Math.Min(offset, text.Length)); num > 0; num--)
		{
			char c = text[num - 1];
			if (c == '\n' || c == '\r')
			{
				break;
			}
		}
		return num;
	}

	private static int FindLineEnd(string text, int offset)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int i;
		for (i = Math.Max(0, Math.Min(offset, text.Length)); i < text.Length; i++)
		{
			char c = text[i];
			if (c == '\n' || c == '\r')
			{
				break;
			}
		}
		return i;
	}

	public static string GetParagraphType(TextDocument document, string fullText, int caretOffset, int selectionStart, int selectionLength, string selectedText)
	{
		return GetParagraphType(GetRelatedText(document, fullText, caretOffset, selectionStart, selectionLength, selectedText));
	}
}
