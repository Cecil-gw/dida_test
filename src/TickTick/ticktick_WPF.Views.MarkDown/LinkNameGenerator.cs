using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

public class LinkNameGenerator : VisualLineElementGenerator
{
	private readonly ILinkTextEditor _editor;

	private readonly bool _showUnderLine;

	private int _hoveredLinkOffset = -1;

	private int _hoveredLinkLength;

	public LinkNameGenerator(ILinkTextEditor editor, bool showUnderLine = false)
	{
		_editor = editor;
		_showUnderLine = showUnderLine;
	}

	private void GetMatch(int startOffset, out int matchOffset)
	{
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		int num = base.CurrentContext.VisualLine.Document?.TextLength ?? 0;
		Dictionary<int, LinkInfo> linkNameDict = _editor.GetLinkNameDict();
		if (linkNameDict.Any())
		{
			foreach (KeyValuePair<int, LinkInfo> item in linkNameDict.OrderBy((KeyValuePair<int, LinkInfo> link) => link.Key))
			{
				int valueOrDefault = (item.Value?.Link?.Length).GetValueOrDefault();
				if (valueOrDefault > 0 && item.Key >= startOffset && item.Key < endOffset && item.Key + valueOrDefault <= endOffset && item.Key + valueOrDefault <= num)
				{
					matchOffset = item.Key;
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
			if (_editor.GetLinkNameDict().TryGetValue(offset, out var value))
			{
				int num = value.Link?.Length ?? 0;
				int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
				int num2 = base.CurrentContext.VisualLine.Document?.TextLength ?? 0;
				if (num > 0 && offset >= base.CurrentContext.VisualLine.FirstDocumentLine.Offset && offset + num <= endOffset && offset + num <= num2)
				{
					LinkUrlTextElement linkUrlTextElement = new LinkUrlTextElement(offset, value, base.CurrentContext.VisualLine, num, value.Url, _editor, _showUnderLine, this);
					if (_editor is LinkTextEditBox && MarkdownSemanticConverter.DecodeEscapedSyntaxForDisplay(value.Link) != value.Link)
					{
						return new TitleLinkTextElement(value.Link, linkUrlTextElement);
					}
					return linkUrlTextElement;
				}
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	internal bool IsHoveredLink(int offset, int length)
	{
		if (_hoveredLinkOffset == offset)
		{
			return _hoveredLinkLength == length;
		}
		return false;
	}

	internal void SetHoveredLink(int offset, int length, VisualLine visualLine)
	{
		if (!IsHoveredLink(offset, length))
		{
			int hoveredLinkOffset = _hoveredLinkOffset;
			int hoveredLinkLength = _hoveredLinkLength;
			_hoveredLinkOffset = offset;
			_hoveredLinkLength = length;
			RedrawLink(hoveredLinkOffset, hoveredLinkLength);
			RedrawLink(offset, length);
		}
	}

	internal void ClearHoveredLink(int offset, int length, VisualLine visualLine)
	{
		if (IsHoveredLink(offset, length))
		{
			_hoveredLinkOffset = -1;
			_hoveredLinkLength = 0;
			RedrawLink(offset, length);
		}
	}

	internal bool ClearHoveredLink()
	{
		if (_hoveredLinkOffset < 0 || _hoveredLinkLength <= 0)
		{
			return false;
		}
		int hoveredLinkOffset = _hoveredLinkOffset;
		int hoveredLinkLength = _hoveredLinkLength;
		_hoveredLinkOffset = -1;
		_hoveredLinkLength = 0;
		RedrawLink(hoveredLinkOffset, hoveredLinkLength);
		return true;
	}

	private void RedrawLink(int offset, int length)
	{
		TextView textView = _editor?.GetEditBox()?.TextArea?.TextView;
		if (offset >= 0 && length > 0)
		{
			textView?.Redraw(offset, length);
		}
	}
}
