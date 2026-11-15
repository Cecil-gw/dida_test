using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class LineElement : FormattedTextElement
{
	private readonly TextEditor _editor;

	internal static double ReservedRightPadding => 7.0;

	public LineElement(int length, TextEditor editor, TextLine text)
		: base(text, length)
	{
		_editor = editor;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new LineTextRun(this, _editor, base.TextRunProperties);
	}

	public override double GetWordWrapRightPadding()
	{
		return ReservedRightPadding;
	}
}
