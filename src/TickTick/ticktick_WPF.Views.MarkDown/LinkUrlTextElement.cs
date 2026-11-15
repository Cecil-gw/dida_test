using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class LinkUrlTextElement : VisualLineText
{
	private readonly ILinkTextEditor _editor;

	private readonly string _url;

	private readonly bool _showUnderLine;

	private readonly LinkNameGenerator _owner;

	private readonly int _length;

	private readonly TextAnchor _offsetAnchor;

	private LinkLineInfoModel linkLineInfo { get; set; }

	private int CurrentOffset => linkLineInfo?.StartIndex ?? (-1);

	public LinkUrlTextElement(int offset, LinkInfo linkinfo, VisualLine parentVisualLine, int length, string url, ILinkTextEditor editor, bool showUnderLine, LinkNameGenerator owner)
		: base(parentVisualLine, length)
	{
		_url = url;
		_showUnderLine = showUnderLine;
		_editor = editor;
		_owner = owner;
		_length = length;
		_offsetAnchor = parentVisualLine?.Document?.CreateAnchor(offset);
		if (_offsetAnchor != null)
		{
			_offsetAnchor.MovementType = AnchorMovementType.AfterInsertion;
		}
		linkLineInfo = new LinkLineInfoModel
		{
			StartIndex = offset,
			Text = linkinfo.Link,
			Url = _url,
			VisualLine = parentVisualLine,
			IsNew = false,
			Source = LinkInfoSource.TextLine
		};
		linkLineInfo.StartIndexProvider = () => (_offsetAnchor != null && !_offsetAnchor.IsDeleted) ? new int?(_offsetAnchor.Offset) : ((int?)null);
	}

	private LinkLineInfoModel GetCurrentLinkLineInfo()
	{
		linkLineInfo.StartIndex = CurrentOffset;
		return linkLineInfo;
	}

	internal void ApplyLinkStyle(VisualLineElementTextRunProperties properties)
	{
		properties.SetForegroundBrush(_editor.GetHighLightColor());
		if (!_showUnderLine)
		{
			LinkNameGenerator owner = _owner;
			if (owner == null || !owner.IsHoveredLink(CurrentOffset, _length))
			{
				return;
			}
		}
		properties.SetTextDecorations(TextDecorations.Underline);
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		ApplyLinkStyle(base.TextRunProperties);
		return base.CreateTextRun(startVisualColumn, context);
	}

	internal void HandleTitleMouseLeave(MouseEventArgs e)
	{
		OnMouseLeave(e);
	}

	internal void HandleTitleQueryCursor(QueryCursorEventArgs e)
	{
		OnQueryCursor(e);
	}

	internal void HandleTitleMouseUp(MouseButtonEventArgs e)
	{
		OnMouseUp(e);
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		_owner?.ClearHoveredLink(CurrentOffset, _length, linkLineInfo.VisualLine);
		if (!e.Handled)
		{
			base.OnMouseLeave(e);
			if (_editor is ILinkMenuHost linkMenuHost)
			{
				linkMenuHost.HideLinkMenuPopup();
			}
		}
	}

	protected override void OnQueryCursor(QueryCursorEventArgs e)
	{
		e.Handled = true;
		e.Cursor = Cursors.Hand;
		base.OnQueryCursor(e);
		_owner?.SetHoveredLink(CurrentOffset, _length, linkLineInfo.VisualLine);
		if (_editor is ILinkMenuHost linkMenuHost)
		{
			linkMenuHost.TryShowLinkMenuPopup(GetCurrentLinkLineInfo());
		}
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
	}

	protected override void OnMouseUp(MouseButtonEventArgs e)
	{
		if (e.ChangedButton != MouseButton.Left || e.Handled || (_editor?.GetEditBox()?.SelectionLength).GetValueOrDefault() != 0)
		{
			return;
		}
		e.Handled = true;
		if (!string.IsNullOrEmpty(_url))
		{
			ProjectTask projectTask = TaskUtils.ParseTaskUrlWithoutTitle(_url);
			if (projectTask != null)
			{
				_editor.NavigateTask(projectTask.ProjectId, projectTask.TaskId);
			}
			else
			{
				OpenUrl();
			}
		}
	}

	private void OpenUrl()
	{
		try
		{
			if (_url == "https://dida365.com/payment?from=welcome")
			{
				Utils.TryProcessStartUrlWithToken("/payment?from=welcome");
			}
			else
			{
				Process.Start(_url);
			}
		}
		catch (Exception)
		{
		}
	}
}
