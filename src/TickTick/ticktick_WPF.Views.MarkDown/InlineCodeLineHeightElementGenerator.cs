using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public class InlineCodeLineHeightElementGenerator : VisualLineElementGenerator
{
	private readonly MarkDownEditor _editor;

	public InlineCodeLineHeightElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (!TryGetMatch(startOffset, out var matchOffset))
		{
			return -1;
		}
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		if (!TryGetMatch(offset, out var matchOffset) || matchOffset != offset)
		{
			return null;
		}
		DocumentLine firstDocumentLine = base.CurrentContext.VisualLine.FirstDocumentLine;
		_editor.TryGetSemanticBlockAtOffset(firstDocumentLine.Offset, out var block);
		return new InlineCodeLineHeightElement(GetLineHeightFontSize(base.CurrentContext.GlobalTextRunProperties.FontRenderingEmSize, block), FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
	}

	private static double GetLineHeightFontSize(double baseFontSize, MarkdownBlockNode block)
	{
		if (block == null || block.Kind != MarkdownBlockKind.Heading || !block.Level.HasValue)
		{
			return Math.Max(1.0, baseFontSize);
		}
		double headingMagnify = HeadingElementGenerator.GetHeadingMagnify(block.Level.Value);
		return Math.Max(1.0, baseFontSize * headingMagnify);
	}

	private bool TryGetMatch(int startOffset, out int matchOffset)
	{
		matchOffset = -1;
		if (_editor == null || base.CurrentContext?.VisualLine?.FirstDocumentLine == null || base.CurrentContext.Document == null)
		{
			return false;
		}
		DocumentLine firstDocumentLine = base.CurrentContext.VisualLine.FirstDocumentLine;
		int lineStart = firstDocumentLine.Offset;
		int lineEnd = firstDocumentLine.EndOffset;
		if (startOffset > lineEnd || lineEnd <= lineStart)
		{
			return false;
		}
		if (_editor.TryGetSemanticBlockAtOffset(lineStart, out var block) && block != null && block.Kind == MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		List<MarkdownInlineDisplaySegment> list = _editor.GetSemanticMarkList()?.Where((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Code && mark.SourcePosition < lineEnd && mark.SourcePosition + mark.SourceLength > lineStart).ToList();
		if (list == null || list.Count == 0)
		{
			return false;
		}
		bool flag = false;
		int offset;
		for (offset = lineStart; offset < lineEnd; offset++)
		{
			if (!IsInvisibleOrWhitespace(base.CurrentContext.Document.GetCharAt(offset)))
			{
				if (!list.Any((MarkdownInlineDisplaySegment mark) => offset >= mark.SourcePosition && offset < mark.SourcePosition + mark.SourceLength))
				{
					return false;
				}
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		matchOffset = lineEnd;
		return true;
	}

	private static bool IsInvisibleOrWhitespace(char ch)
	{
		if (ch != '\u00a0' && ch != '\u2065' && ch != '\u200b' && ch != '\u200c' && ch != '\u200d' && ch != '\u200e' && ch != '\u200f' && ch != '\u2060' && ch != '\u2061' && ch != '\u2062' && ch != '\u2063' && ch != '\u2064' && ch != '\u2065' && ch != '\u2066' && ch != '\u206a')
		{
			return char.IsWhiteSpace(ch);
		}
		return true;
	}
}
