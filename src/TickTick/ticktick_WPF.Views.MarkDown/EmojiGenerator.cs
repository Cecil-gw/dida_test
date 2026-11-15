using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public class EmojiGenerator : VisualLineElementGenerator
{
	private readonly IEmojiRender _editor;

	public EmojiGenerator(IEmojiRender editor)
	{
		_editor = editor;
	}

	private void GetMatch(int startOffset, out int matchOffset)
	{
		DocumentLine lastDocumentLine = base.CurrentContext.VisualLine.LastDocumentLine;
		if (lastDocumentLine == null)
		{
			matchOffset = -1;
			return;
		}
		Dictionary<int, string> dictionary = _editor?.GetEmojiDict();
		if (dictionary != null && dictionary.Any())
		{
			foreach (KeyValuePair<int, string> item in dictionary)
			{
				if (item.Value != null && item.Key >= startOffset && lastDocumentLine.EndOffset >= item.Key + item.Value.Length && item.Value.Length > 0)
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
			if (_editor.GetEmojiDict().ContainsKey(offset))
			{
				string text = _editor.GetEmojiDict()[offset];
				DocumentLine lastDocumentLine = base.CurrentContext.VisualLine.LastDocumentLine;
				if (offset + text.Length > lastDocumentLine.EndOffset)
				{
					return null;
				}
				return new EmojiElement(text, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
			}
		}
		catch (Exception)
		{
			return null;
		}
		return null;
	}
}
