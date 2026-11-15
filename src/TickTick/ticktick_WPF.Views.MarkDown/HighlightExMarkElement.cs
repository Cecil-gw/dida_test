using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class HighlightExMarkElement : FormattedTextElement
{
	private readonly MarkDownEditor _editor;

	private readonly int _start;

	private readonly int _end;

	public HighlightExMarkElement(int length, int start, int end, TextLine text, MarkDownEditor editor)
		: base(text, length)
	{
		_editor = editor;
		_start = start;
		_end = end;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		bool valueOrDefault = _editor.FindResource("IsDarkTheme") as bool? == true;
		int caretOffset = _editor.EditBox.CaretOffset;
		bool showMark = caretOffset >= _start && caretOffset <= _end;
		return new HighlightExMarkTextRun(this, base.TextRunProperties, valueOrDefault, showMark);
	}
}
