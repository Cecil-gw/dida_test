using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.QuickAdd;

internal sealed class FirstLineIndentTextRun : FormattedTextRun
{
	private readonly double _width;

	public FirstLineIndentTextRun(double width, FormattedTextElement element, TextRunProperties properties)
		: base(element, properties)
	{
		_width = width;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(_width, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
