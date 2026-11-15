using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CheckBoxElement : FormattedTextElement
{
	private readonly ITextRunConstructionContext _context;

	private readonly MarkDownEditor _editor;

	private readonly int _length;

	private readonly TextAnchor _offsetAnchor;

	private readonly int _indentLevel;

	public CheckBoxElement(int offset, int consumedLength, int indentLevel, TextLine text, ITextRunConstructionContext context, MarkDownEditor editor)
		: base(text, consumedLength)
	{
		_offsetAnchor = context?.VisualLine?.Document?.CreateAnchor(offset);
		if (_offsetAnchor != null)
		{
			_offsetAnchor.MovementType = AnchorMovementType.AfterInsertion;
		}
		_context = context;
		_length = consumedLength;
		_editor = editor;
		_indentLevel = indentLevel;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new CheckBoxTextRun(this, base.TextRunProperties, _length, _indentLevel, _editor);
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && _offsetAnchor != null && !_offsetAnchor.IsDeleted)
		{
			_editor.UnRegisterCaretChanged();
			e.Handled = true;
			_editor.SuppressCaretVerticalOffsetNotificationForCurrentMouseGesture();
			_editor.SetTaskListCheckedStateAtLineOffset(_offsetAnchor.Offset, isChecked: false);
			_editor.RegisterCaretChanged();
		}
	}

	protected override void OnQueryCursor(QueryCursorEventArgs e)
	{
		e.Handled = true;
		e.Cursor = Cursors.Hand;
	}

	public override bool IsWhitespace(int visualColumn)
	{
		return visualColumn == 0;
	}

	public override bool IsListStart()
	{
		return true;
	}
}
