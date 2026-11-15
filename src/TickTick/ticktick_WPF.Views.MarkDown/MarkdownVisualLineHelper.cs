using System;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal static class MarkdownVisualLineHelper
{
	internal const char DisplayCaretAnchorChar = '\u00a0';

	internal const char DisplayParagraphIndentMarkerChar = '\u2065';

	public static bool IsLineStart(ITextRunConstructionContext context, int offset)
	{
		if (context == null)
		{
			return false;
		}
		if (offset == 0)
		{
			return true;
		}
		char c = context.Document.Text[offset - 1];
		if (c != '\n')
		{
			return c == '\r';
		}
		return true;
	}

	public static bool CanRenderBlockMarker(MarkDownEditor editor, ITextRunConstructionContext context, int offset)
	{
		if (!IsLineStart(context, offset))
		{
			return false;
		}
		if (editor != null && editor.IsDocumentOffsetHiddenByHeadingFold(offset))
		{
			return false;
		}
		return !IsSemanticCodeBlockAtOffset(editor, offset);
	}

	internal static bool IsSemanticCodeBlockAtOffset(MarkDownEditor editor, int offset)
	{
		if (editor != null && editor.TryGetSemanticBlockAtOffset(offset, out var block))
		{
			if (block == null)
			{
				return false;
			}
			return block.Kind == MarkdownBlockKind.CodeBlock;
		}
		return false;
	}

	public static bool TryGetSemanticBlockAtLineStart(MarkDownEditor editor, ITextRunConstructionContext context, int offset, out MarkdownBlockNode block)
	{
		block = null;
		if (!CanRenderBlockMarker(editor, context, offset) || editor == null)
		{
			return false;
		}
		return editor.TryGetSemanticBlockAtOffset(offset, out block);
	}

	public static string GetLineText(ITextRunConstructionContext context)
	{
		if (context?.VisualLine?.FirstDocumentLine == null || context.VisualLine.LastDocumentLine == null)
		{
			return string.Empty;
		}
		int offset = context.VisualLine.FirstDocumentLine.Offset;
		int endOffset = context.VisualLine.LastDocumentLine.EndOffset;
		return context.Document.GetText(offset, endOffset - offset);
	}

	public static string GetNormalizedLineText(ITextRunConstructionContext context)
	{
		return (GetLineText(context) ?? string.Empty).TrimEnd('\r', '\n');
	}

	public static bool TryGetMatchingLineSyntax(MarkDownEditor editor, ITextRunConstructionContext context, int offset, out MarkdownBlockNode block, out string lineText, out MarkdownBlockSyntaxInfo syntax)
	{
		block = null;
		lineText = GetNormalizedLineText(context);
		syntax = null;
		if (!TryGetSemanticBlockAtLineStart(editor, context, offset, out block))
		{
			return false;
		}
		syntax = MarkdownBlockSyntax.Parse(MarkDownEditor.NormalizeDisplayTaskMarkerLine(lineText));
		return LineSyntaxMatchesSemanticBlock(syntax, block);
	}

	public static bool TryGetLineSyntaxAtLineStart(MarkDownEditor editor, ITextRunConstructionContext context, int offset, out string lineText, out MarkdownBlockSyntaxInfo syntax)
	{
		lineText = GetNormalizedLineText(context);
		syntax = null;
		if (!CanRenderBlockMarker(editor, context, offset))
		{
			return false;
		}
		syntax = MarkdownBlockSyntax.Parse(MarkDownEditor.NormalizeDisplayTaskMarkerLine(lineText));
		return syntax != null;
	}

	public static bool LineSyntaxMatchesSemanticBlock(MarkdownBlockSyntaxInfo syntax, MarkdownBlockNode block)
	{
		if (syntax == null || block == null)
		{
			return false;
		}
		switch (block.Kind)
		{
		case MarkdownBlockKind.Heading:
			if (syntax.Kind == MarkdownBlockSyntaxKind.Heading)
			{
				return syntax.HeadingLevel == block.Level;
			}
			return false;
		case MarkdownBlockKind.BlockQuote:
			return syntax.Kind == MarkdownBlockSyntaxKind.BlockQuote;
		case MarkdownBlockKind.HorizontalRule:
			return syntax.Kind == MarkdownBlockSyntaxKind.SplitLine;
		case MarkdownBlockKind.CodeBlock:
			return syntax.Kind == MarkdownBlockSyntaxKind.CodeFence;
		case MarkdownBlockKind.List:
			if (syntax.Kind != MarkdownBlockSyntaxKind.List)
			{
				return false;
			}
			if (syntax.ListKind != block.ListKind)
			{
				return false;
			}
			if (block.ListKind == MarkdownListKind.Task)
			{
				return syntax.Checked == block.Checked;
			}
			return true;
		default:
			return false;
		}
	}

	public static int CountLeadingWhitespaceCharacters(string text)
	{
		int i;
		for (i = 0; i < (text?.Length ?? 0) && char.IsWhiteSpace(text[i]); i++)
		{
		}
		return i;
	}

	public static int CountLeadingRenderPrefixCharacters(string text)
	{
		int num = CountLeadingWhitespaceCharacters(text);
		if (!string.IsNullOrEmpty(text) && num < text.Length && text[num] == '\u00a0')
		{
			num++;
		}
		return num;
	}

	public static string GetVisibleRenderPrefixText(string text, int prefixLength)
	{
		if (string.IsNullOrEmpty(text) || prefixLength <= 0)
		{
			return string.Empty;
		}
		int length = Math.Min(text.Length, prefixLength);
		return text.Substring(0, length).Replace('\u00a0'.ToString(), string.Empty);
	}
}
