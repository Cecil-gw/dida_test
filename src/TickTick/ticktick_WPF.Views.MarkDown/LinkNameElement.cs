using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Cache;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class LinkNameElement : FormattedTextElement
{
	private readonly ProjectTask _pt;

	private ILinkTextEditor _editor;

	public LinkNameElement(ProjectTask pt, ILinkTextEditor editor, int count, TextLine text)
		: base(text, count)
	{
		_pt = pt;
		_editor = editor;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		if (_pt != null && string.IsNullOrWhiteSpace(_pt.Title))
		{
			_pt.Title = TaskCache.GetTaskById(_pt.TaskId)?.Title.Trim();
		}
		return new LinkNameTextRun(this, string.IsNullOrWhiteSpace(_pt?.Title) ? Utils.GetString("NoTitle") : _pt.Title, _editor.GetEditBox().FontSize, base.TextRunProperties);
	}

	public override bool IsWhitespace(int visualColumn)
	{
		return visualColumn == 0;
	}

	public override bool IsListStart()
	{
		return true;
	}

	protected override void OnQueryCursor(QueryCursorEventArgs e)
	{
		e.Handled = true;
		e.Cursor = Cursors.Hand;
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && !e.Handled)
		{
			_editor.NavigateTask(_pt.ProjectId, _pt.TaskId);
		}
	}
}
