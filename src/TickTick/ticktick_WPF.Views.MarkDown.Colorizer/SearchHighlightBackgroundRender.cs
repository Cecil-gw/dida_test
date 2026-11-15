using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public class SearchHighlightBackgroundRender : IBackgroundRenderer
{
	private TextEditor _editor;

	private bool _textChanged = true;

	private Dictionary<int, LinkInfo> _linkDict;

	private bool _isTitle;

	public bool InDetail;

	private readonly Func<int, bool> _isOffsetHidden;

	private bool _inPreSearch;

	public KnownLayer Layer { get; }

	public List<TextSegment> Segments { get; set; } = new List<TextSegment>();

	public SearchHighlightBackgroundRender(TextEditor editor, Dictionary<int, LinkInfo> linkUrlDict, bool isTitle, bool inDetail, bool inPreSearch = false, Func<int, bool> isOffsetHidden = null)
	{
		_editor = editor;
		_linkDict = linkUrlDict;
		_isTitle = isTitle;
		InDetail = inDetail;
		_inPreSearch = inPreSearch;
		_isOffsetHidden = isOffsetHidden;
	}

	public void Draw(TextView textView, DrawingContext drawingContext)
	{
		if (!_inPreSearch && (!LocalSettings.Settings.InSearch || string.IsNullOrEmpty(SearchHelper.SearchKey) || (InDetail && SearchHelper.DetailChanged)))
		{
			return;
		}
		EnsureSegments();
		foreach (TextSegment segment in Segments)
		{
			Func<int, bool> isOffsetHidden = _isOffsetHidden;
			if (isOffsetHidden != null && isOffsetHidden(segment.StartOffset))
			{
				continue;
			}
			foreach (Rect item in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
			{
				if (item.Width > 4.0)
				{
					drawingContext.DrawRoundedRectangle(SearchHelper.GetSearchHighlightColor(), null, new Rect(item.Left, item.Top, item.Width + 1.0, item.Height), 0.0, 0.0);
				}
			}
		}
	}

	public void SetTextChanged(bool b)
	{
		_textChanged = b;
	}

	public int? GetFirstIndex()
	{
		EnsureSegments();
		return Segments.FirstOrDefault()?.StartOffset;
	}

	public IReadOnlyList<int> GetDocumentMatchIndices(bool includeHiddenOffsets)
	{
		string text = _editor?.Document?.Text;
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(SearchHelper.SearchKey))
		{
			return new List<int>();
		}
		return (from segment in GetMatchSegments(text, includeHiddenOffsets)
			select segment.StartOffset).ToList();
	}

	public int? GetFirstMatchIndex(string text)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(SearchHelper.SearchKey))
		{
			return null;
		}
		Match match = (_inPreSearch ? SearchHelper.PreSearchRegex : SearchHelper.SearchRegex)?.Match(text.ToLower());
		if (match == null || !match.Success)
		{
			return null;
		}
		return match.Index;
	}

	private void EnsureSegments()
	{
		if (_textChanged)
		{
			Segments.Clear();
			string text = _editor?.Document?.Text;
			if (!string.IsNullOrEmpty(text) && (_inPreSearch || !string.IsNullOrEmpty(SearchHelper.SearchKey)))
			{
				Segments.AddRange(GetMatchSegments(text, includeHiddenOffsets: false));
			}
			_textChanged = false;
		}
	}

	private IEnumerable<TextSegment> GetMatchSegments(string text, bool includeHiddenOffsets)
	{
		Regex regex = (_inPreSearch ? SearchHelper.PreSearchRegex : SearchHelper.SearchRegex);
		if (regex == null || string.IsNullOrEmpty(text))
		{
			yield break;
		}
		foreach (Match item in regex.Matches(text.ToLower()))
		{
			if (item.Success && item.Length > 0 && !IsOffsetInsideLinkUrl(item.Index) && (includeHiddenOffsets || !(_isOffsetHidden?.Invoke(item.Index) ?? false)))
			{
				yield return new TextSegment
				{
					StartOffset = item.Index,
					Length = item.Length
				};
			}
		}
	}

	private bool IsOffsetInsideLinkUrl(int offset)
	{
		if (_linkDict == null)
		{
			return false;
		}
		foreach (KeyValuePair<int, LinkInfo> item in _linkDict)
		{
			int num = item.Key - 1;
			int num2 = num + (item.Value?.Url?.Length).GetValueOrDefault() + 1;
			if (offset >= num && offset <= num2)
			{
				return true;
			}
		}
		return false;
	}
}
