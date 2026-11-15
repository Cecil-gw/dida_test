using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class NumberedListItemElement : FormattedTextElement
{
	private readonly int _length;

	private readonly MarkDownEditor _editor;

	private readonly string _numberText;

	private readonly SolidColorBrush _brush;

	private readonly SolidColorBrush _hoverBrush;

	private readonly VisualLine _line;

	private readonly int _indentLevel;

	public NumberedListItemElement(string numberText, int length, int indentLevel, VisualLine parentVisualLine, TextLine text, SolidColorBrush brush, MarkDownEditor editor)
		: base(text, length)
	{
		_brush = brush;
		_numberText = numberText;
		_length = length;
		_line = parentVisualLine;
		_editor = editor;
		_indentLevel = indentLevel;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new NumberedListItemTextRun(this, _numberText, _length, _indentLevel, base.TextRunProperties, _brush);
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
