using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class InlineCodeLineHeightTextRun : FormattedTextRun
{
	private readonly double _baseFontSize;

	public InlineCodeLineHeightTextRun(FormattedTextElement element, TextRunProperties properties, double baseFontSize)
		: base(element, properties)
	{
		_baseFontSize = baseFontSize;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = MeasureText();
		return new TextEmbeddedObjectMetrics(0.01, formattedText.Height, formattedText.Baseline);
	}

	public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
	{
		FormattedText formattedText = MeasureText();
		return new Rect(0.0, 0.0, 0.01, formattedText.Height);
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
	}

	private FormattedText MeasureText()
	{
		return new FormattedText(" ", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Properties.Typeface, Math.Max(1.0, _baseFontSize), Brushes.Transparent);
	}
}
