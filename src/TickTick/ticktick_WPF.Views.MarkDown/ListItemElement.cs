using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class ListItemElement : FormattedTextElement
{
	private readonly int _length;

	private readonly MarkDownEditor _editor;

	private readonly int _level;

	public ListItemElement(int length, int level, TextLine text, MarkDownEditor editor)
		: base(text, length)
	{
		_length = length;
		_editor = editor;
		_level = level;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new ListItemTextRun(this, _length, _level, base.TextRunProperties, _editor);
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
