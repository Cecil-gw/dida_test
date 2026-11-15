using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

public class LinkUrlGenerator : VisualLineElementGenerator
{
	private readonly ILinkTextEditor _editor;

	public LinkUrlGenerator(ILinkTextEditor editor)
	{
		_editor = editor;
	}

	private void GetMatch(int startOffset, out int matchOffset)
	{
		DocumentLine lastDocumentLine = base.CurrentContext.VisualLine.LastDocumentLine;
		List<MarkdownInlineDisplaySegment> list = (_editor as MarkDownEditor)?.GetSemanticMarkList();
		if (list != null && list.Any())
		{
			foreach (MarkdownInlineDisplaySegment item in list)
			{
				if (item != null && item.Kind == MarkdownMarkKind.Link && item.UrlLength > 0)
				{
					int urlPosition = item.UrlPosition;
					if (urlPosition >= startOffset && urlPosition < lastDocumentLine.EndOffset)
					{
						matchOffset = urlPosition;
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
			MarkdownInlineDisplaySegment markdownInlineDisplaySegment = (_editor as MarkDownEditor)?.GetSemanticMarkList()?.FirstOrDefault((MarkdownInlineDisplaySegment mark) => mark != null && mark.Kind == MarkdownMarkKind.Link && mark.UrlLength > 0 && mark.UrlPosition == offset);
			LinkInfo linkInfo = (_editor.GetLinkUrlDict().ContainsKey(offset) ? _editor.GetLinkUrlDict()[offset] : null);
			if (markdownInlineDisplaySegment == null || linkInfo == null)
			{
				return null;
			}
			bool dark = ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId);
			if (_editor is FrameworkElement frameworkElement)
			{
				dark = frameworkElement.FindResource("IsDarkTheme") as bool? == true;
			}
			return new LinkUrlElement(offset, markdownInlineDisplaySegment.UrlLength, linkInfo.Url, linkInfo.Link, base.CurrentContext.VisualLine, _editor, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)), dark);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
