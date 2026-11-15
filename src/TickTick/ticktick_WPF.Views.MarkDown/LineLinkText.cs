using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class LineLinkText : VisualLineLinkText
{
	private readonly string _url;

	private readonly ILinkTextEditor _editor;

	private readonly CustomLinkElementGenerator _owner;

	private readonly TextAnchor _offsetAnchor;

	private LinkLineInfoModel linkLineInfo { get; set; }

	private int CurrentOffset => linkLineInfo?.StartIndex ?? (-1);

	public LineLinkText(int offset, VisualLine parentVisualLine, string url, ILinkTextEditor editor, CustomLinkElementGenerator owner)
		: base(parentVisualLine, url.Length)
	{
		_url = url;
		_editor = editor;
		_owner = owner;
		_offsetAnchor = parentVisualLine?.Document?.CreateAnchor(offset);
		if (_offsetAnchor != null)
		{
			_offsetAnchor.MovementType = AnchorMovementType.AfterInsertion;
		}
		linkLineInfo = new LinkLineInfoModel
		{
			StartIndex = offset,
			Url = url,
			VisualLine = parentVisualLine,
			IsNew = false,
			Source = LinkInfoSource.Link
		};
		linkLineInfo.StartIndexProvider = () => (_offsetAnchor != null && !_offsetAnchor.IsDeleted) ? new int?(_offsetAnchor.Offset) : ((int?)null);
	}

	private LinkLineInfoModel GetCurrentLinkLineInfo()
	{
		linkLineInfo.StartIndex = CurrentOffset;
		return linkLineInfo;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		base.TextRunProperties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
		base.TextRunProperties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
		CustomLinkElementGenerator owner = _owner;
		if (owner != null && owner.IsHoveredLink(CurrentOffset, _url?.Length ?? 0))
		{
			base.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
		}
		int num = startVisualColumn - base.VisualColumn;
		StringSegment text = context.GetText(context.VisualLine.FirstDocumentLine.Offset + base.RelativeTextOffset + num, base.DocumentLength - num);
		return new TextCharacters(text.Text, text.Offset, text.Count, base.TextRunProperties);
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
		if (string.IsNullOrEmpty(_url))
		{
			return;
		}
		ProjectTask projectTask = TaskUtils.ParseTaskUrlWithoutTitle(_url);
		if (projectTask != null)
		{
			_editor.NavigateTask(projectTask.ProjectId, projectTask.TaskId);
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = _url,
				UseShellExecute = true
			});
		}
		catch (Exception)
		{
		}
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		_owner?.ClearHoveredLink(CurrentOffset, _url?.Length ?? 0, linkLineInfo.VisualLine);
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
		base.OnQueryCursor(e);
		if (!LinkIsClickable())
		{
			_owner?.ClearHoveredLink(CurrentOffset, _url?.Length ?? 0, linkLineInfo.VisualLine);
			return;
		}
		_owner?.SetHoveredLink(CurrentOffset, _url?.Length ?? 0, linkLineInfo.VisualLine);
		if (_editor is ILinkMenuHost linkMenuHost)
		{
			linkMenuHost.TryShowLinkMenuPopup(GetCurrentLinkLineInfo());
		}
	}
}
