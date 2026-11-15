using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

public class LinkBracketGenerator : VisualLineElementGenerator
{
	private readonly ILinkTextEditor _editor;

	private readonly bool _isLeft;

	private readonly bool _isLink;

	public LinkBracketGenerator(ILinkTextEditor editor, bool isLeft, bool isLink)
	{
		_editor = editor;
		_isLeft = isLeft;
		_isLink = isLink;
	}

	private void GetMatch(int startOffset, out int matchOffset)
	{
		DocumentLine lastDocumentLine = base.CurrentContext.VisualLine.LastDocumentLine;
		base.CurrentContext.GetText(startOffset, lastDocumentLine.EndOffset - startOffset);
		Dictionary<int, LinkInfo> linkNameDict = _editor.GetLinkNameDict();
		if (linkNameDict.Any())
		{
			try
			{
				foreach (KeyValuePair<int, LinkInfo> item in linkNameDict)
				{
					if (item.Key >= lastDocumentLine.Offset && item.Key <= lastDocumentLine.EndOffset)
					{
						if (_isLink)
						{
							matchOffset = (_isLeft ? (item.Key - 1) : (item.Key + item.Value.Link.Length));
						}
						else
						{
							matchOffset = (_isLeft ? (item.Key + item.Value.Link.Length + 1) : (item.Key + item.Value.Link.Length + 2 + item.Value.Url.Length));
						}
						if (matchOffset >= startOffset && matchOffset < lastDocumentLine.EndOffset)
						{
							return;
						}
					}
				}
			}
			catch (Exception)
			{
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
			if (offset >= base.CurrentContext.VisualLine.Document.Text.Length)
			{
				return null;
			}
			return new LinkBracketTextElement(base.CurrentContext.VisualLine, _editor);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
