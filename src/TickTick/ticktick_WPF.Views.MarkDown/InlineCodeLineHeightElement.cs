using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class InlineCodeLineHeightElement : FormattedTextElement
{
	private readonly double _baseFontSize;

	public InlineCodeLineHeightElement(double baseFontSize, TextLine text)
		: base(text, 0)
	{
		_baseFontSize = baseFontSize;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new InlineCodeLineHeightTextRun(this, base.TextRunProperties, _baseFontSize);
	}
}
