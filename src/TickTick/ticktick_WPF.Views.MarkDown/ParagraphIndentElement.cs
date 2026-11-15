using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class ParagraphIndentElement : FormattedTextElement
{
	private readonly int _indentLevel;

	public ParagraphIndentElement(int prefixLength, int indentLevel, TextLine text)
		: base(text, prefixLength)
	{
		_indentLevel = indentLevel;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new ParagraphIndentTextRun(this, base.TextRunProperties, _indentLevel);
	}

	public override bool IsWhitespace(int visualColumn)
	{
		return visualColumn == 0;
	}
}
