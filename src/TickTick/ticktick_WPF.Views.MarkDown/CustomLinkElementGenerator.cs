using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class CustomLinkElementGenerator : VisualLineElementGenerator
{
	private readonly Regex _linkRegex;

	private ILinkTextEditor _editor;

	private bool _ignoreUrlDict;

	private bool _parseTaskTitle;

	private int _hoveredLinkOffset = -1;

	private int _hoveredLinkLength;

	public CustomLinkElementGenerator(ILinkTextEditor editor, bool ignoreUrlDict = false, bool parseTaskTitle = false)
	{
		_editor = editor;
		_linkRegex = new Regex("(https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]?\\.[^\\s]{2,}|www\\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\\.[^\\s]{2,}|https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9]\\.[^\\s]{2,}|www\\.[a-zA-Z0-9]\\.[^\\s]{2,})");
		_ignoreUrlDict = ignoreUrlDict;
		_parseTaskTitle = parseTaskTitle;
	}

	public void SetParseTaskTitle(bool parseTaskTitle)
	{
		_parseTaskTitle = parseTaskTitle;
	}

	private Match GetMatch(int startOffset, out int matchOffset)
	{
		if (_editor == null)
		{
			matchOffset = -1;
			return null;
		}
		int endOffset = base.CurrentContext.VisualLine.LastDocumentLine.EndOffset;
		StringSegment text = base.CurrentContext.GetText(startOffset, endOffset - startOffset);
		string text2 = text.Text;
		Dictionary<int, LinkInfo> dictionary = (_ignoreUrlDict ? null : _editor.GetLinkUrlDict());
		if (dictionary != null && dictionary.Any())
		{
			foreach (KeyValuePair<int, LinkInfo> item in dictionary)
			{
				int num = item.Key - (endOffset - text2.Length);
				if (item.Key >= startOffset && num + item.Value.Url.Length <= text2.Length)
				{
					text2 = text2.Remove(num, item.Value.Url.Length).Insert(num, BuildEmptyString(item.Value.Url.Length));
				}
			}
		}
		Match match = _linkRegex.Match(text2, text.Offset, text.Count);
		matchOffset = (match.Success ? (match.Index - text.Offset + startOffset) : (-1));
		return match;
	}

	private static string BuildEmptyString(int length)
	{
		StringBuilder stringBuilder = new StringBuilder(length);
		for (int i = 0; i < length; i++)
		{
			stringBuilder.Append(" ");
		}
		return stringBuilder.ToString();
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		GetMatch(startOffset, out var matchOffset);
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		int matchOffset;
		Match match = GetMatch(offset, out matchOffset);
		if (match.Success && matchOffset == offset)
		{
			return ConstructElementFromMatch(matchOffset, match);
		}
		return null;
	}

	private VisualLineElement ConstructElementFromMatch(int offset, Capture m)
	{
		string uri;
		Uri uriFromMatch = GetUriFromMatch(m, out uri);
		if (uriFromMatch == null || _editor == null)
		{
			return null;
		}
		try
		{
			if (_parseTaskTitle)
			{
				ProjectTask projectTask = TaskUtils.ParseTaskUrlWithoutTitle(uri);
				if (projectTask != null)
				{
					return new LinkNameElement(projectTask, _editor, m.Length, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
				}
			}
			return new LineLinkText(offset, base.CurrentContext.VisualLine, uri, _editor, this)
			{
				NavigateUri = uriFromMatch,
				RequireControlModifierForClick = false
			};
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static Uri GetUriFromMatch(Capture match, out string uri)
	{
		string text = (uri = match.Value.TrimEnd("!\"#$%&'()*+,-.@:;<=>[\\]^_`{|}~".ToCharArray()));
		if (text.StartsWith("www.", StringComparison.Ordinal))
		{
			text = "http://" + text;
		}
		if (!Uri.IsWellFormedUriString(text, UriKind.Absolute))
		{
			return null;
		}
		return new Uri(text);
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
