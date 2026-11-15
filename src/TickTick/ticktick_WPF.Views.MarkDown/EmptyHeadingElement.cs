using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class EmptyHeadingElement : FormattedTextElement
{
	private readonly double _magnify;

	private readonly double _baseFontSize;

	public EmptyHeadingElement(int consumedLength, double magnify, double baseFontSize, TextLine text)
		: base(text, consumedLength)
	{
		_magnify = magnify;
		_baseFontSize = baseFontSize;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new EmptyHeadingTextRun(this, base.TextRunProperties, _magnify, _baseFontSize);
	}
}
