using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

public class HideLinkBracketGenerator : VisualLineElementGenerator
{
	private readonly ILinkTextEditor _editor;

	private readonly bool _isLeft;

	private readonly Dictionary<int, LinkInfo> _dict;

	public HideLinkBracketGenerator(ILinkTextEditor editor, Dictionary<int, LinkInfo> nameDict, bool isLeft)
	{
		_editor = editor;
		_dict = nameDict;
		_isLeft = isLeft;
	}

	private void GetMatch(int startOffset, out int matchOffset)
	{
		int offset = base.CurrentContext.VisualLine.FirstDocumentLine.Offset;
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		List<MarkdownInlineDisplaySegment> list = (_editor as MarkDownEditor)?.GetSemanticMarkList();
		if (list != null && list.Any())
		{
			foreach (MarkdownInlineDisplaySegment item in list)
			{
				if (item == null || item.Kind != MarkdownMarkKind.Link || item.SourceLength <= 0)
				{
					continue;
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
		if (_dict != null && _dict.Any())
		{
			foreach (KeyValuePair<int, LinkInfo> item2 in _dict)
			{
				int num3 = (_isLeft ? (item2.Key - 1) : (item2.Key + (item2.Value?.Link?.Length).GetValueOrDefault()));
				if (num3 >= startOffset && num3 < endOffset)
				{
					matchOffset = num3;
					return;
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
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = (_editor as MarkDownEditor)?.GetSemanticMarkList()?.FirstOrDefault(delegate(MarkdownInlineDisplaySegment mark)
			{
				if (mark != null && mark.Kind == MarkdownMarkKind.Link)
				{
					if (!_isLeft)
					{
						if (mark.ClosingDelimiterLength > 0)
						{
							return mark.SourcePosition + mark.SourceLength - mark.ClosingDelimiterLength == offset;
						}
						return false;
					}
					if (mark.OpeningDelimiterLength > 0)
					{
						return mark.SourcePosition == offset;
					}
					return false;
				}
				return false;
			});
			if (markdownInlineDisplaySegment != null)
			{
				int num = (_isLeft ? markdownInlineDisplaySegment.OpeningDelimiterLength : markdownInlineDisplaySegment.ClosingDelimiterLength);
				int offset2 = base.CurrentContext.VisualLine.FirstDocumentLine.Offset;
				int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
				if (num > 0 && offset >= offset2 && offset + num <= endOffset)
				{
					return new LinkElement(num, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
				}
			}
			LinkInfo linkInfo = null;
			IEnumerable<KeyValuePair<int, LinkInfo>> dict = _dict;
			foreach (KeyValuePair<int, LinkInfo> item in dict ?? Enumerable.Empty<KeyValuePair<int, LinkInfo>>())
			{
				if (_isLeft && item.Key - 1 == offset)
				{
					linkInfo = item.Value;
					break;
				}
				if (!_isLeft && offset - (item.Value?.Link?.Length).GetValueOrDefault() == item.Key)
				{
					linkInfo = item.Value;
					break;
				}
			}
			int num2 = (_isLeft ? 1 : ((linkInfo?.Url?.Length).GetValueOrDefault() + 3));
			int offset3 = base.CurrentContext.VisualLine.FirstDocumentLine.Offset;
			int endOffset2 = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
			if (num2 <= 0 || offset < offset3 || offset + num2 > endOffset2)
			{
				return null;
			}
			return new LinkElement(num2, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
		}
		catch (Exception)
		{
			return null;
		}
	}
}
