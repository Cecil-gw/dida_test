using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public class AutoHideSymbolGenerator : VisualLineElementGenerator
{
	private readonly MarkDownEditor _editor;

	private readonly bool _isLeft;

	public AutoHideSymbolGenerator(MarkDownEditor editor, bool isLeft)
	{
		_editor = editor;
		_isLeft = isLeft;
	}

	private void GetMatch(int startOffset, out int matchOffset)
	{
		List<MarkdownInlineDisplaySegment> semanticMarkList = _editor.GetSemanticMarkList();
		int offset = base.CurrentContext.VisualLine.FirstDocumentLine.Offset;
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		if (semanticMarkList != null && semanticMarkList.Any())
		{
			semanticMarkList.Sort((MarkdownInlineDisplaySegment a, MarkdownInlineDisplaySegment b) => (!_isLeft) ? (a.SourcePosition + a.SourceLength).CompareTo(b.SourcePosition + b.SourceLength) : a.SourcePosition.CompareTo(b.SourcePosition));
			foreach (MarkdownInlineDisplaySegment item in semanticMarkList)
			{
				if (item.Kind == MarkdownMarkKind.Link || (_isLeft && item.SourcePosition < startOffset) || (!_isLeft && item.SourcePosition > startOffset))
				{
					continue;
				}
				if (item.SourcePosition + item.SourceLength > endOffset)
				{
					break;
				}
				int num = (_isLeft ? item.OpeningDelimiterLength : item.ClosingDelimiterLength);
				if (num > 0)
				{
					int num2 = (_isLeft ? item.SourcePosition : (item.SourcePosition + item.SourceLength - num));
					if (num2 >= startOffset && num2 >= offset && num2 + num <= endOffset)
					{
						matchOffset = num2;
						return;
					}
				}
			}
		}
		matchOffset = -1;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		GetMatch(startOffset, out var matchOffset);
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		try
		{
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = null;
			List<MarkdownInlineDisplaySegment> semanticMarkList = _editor.GetSemanticMarkList();
			if (semanticMarkList != null && semanticMarkList.Any())
			{
				if (_isLeft)
				{
					semanticMarkList.Sort((MarkdownInlineDisplaySegment a, MarkdownInlineDisplaySegment b) => a.SourcePosition.CompareTo(b.SourcePosition));
					foreach (MarkdownInlineDisplaySegment item in semanticMarkList)
					{
						if (item.Kind != MarkdownMarkKind.Link && item.SourcePosition == offset)
						{
							markdownInlineDisplaySegment = item;
							break;
						}
					}
				}
				else
				{
					semanticMarkList.Sort((MarkdownInlineDisplaySegment a, MarkdownInlineDisplaySegment b) => (a.SourcePosition + a.SourceLength).CompareTo(b.SourcePosition + b.SourceLength));
					foreach (MarkdownInlineDisplaySegment item2 in semanticMarkList)
					{
						if (item2.Kind != MarkdownMarkKind.Link)
						{
							int closingDelimiterLength = item2.ClosingDelimiterLength;
							int num = item2.SourcePosition + item2.SourceLength - closingDelimiterLength;
							if (closingDelimiterLength > 0 && num == offset)
							{
								markdownInlineDisplaySegment = item2;
								break;
							}
						}
					}
				}
			}
			if (markdownInlineDisplaySegment != null)
			{
				int num2 = (_isLeft ? markdownInlineDisplaySegment.OpeningDelimiterLength : markdownInlineDisplaySegment.ClosingDelimiterLength);
				int offset2 = base.CurrentContext.VisualLine.FirstDocumentLine.Offset;
				int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
				if (num2 <= 0 || offset < offset2 || offset + num2 > endOffset)
				{
					return null;
				}
				return new HideMarkElement(num2, 0.01, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
