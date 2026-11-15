using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class HideMarkElement : FormattedTextElement
{
	private double _width;

	public HideMarkElement(int length, double width, TextLine text)
		: base(text, length)
	{
		_width = width;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new HideMarkTextRun(_width, this, base.TextRunProperties);
	}
}
