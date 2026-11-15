using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class EmptyHeadingTextRun : FormattedTextRun
{
	private readonly double _magnify;

	private readonly double _baseFontSize;

	public EmptyHeadingTextRun(FormattedTextElement element, TextRunProperties properties, double magnify, double baseFontSize)
		: base(element, properties)
	{
		_magnify = magnify;
		_baseFontSize = baseFontSize;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = MeasureHeadingText();
		return new TextEmbeddedObjectMetrics(0.01, formattedText.Height, formattedText.Baseline);
	}

	public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
	{
		FormattedText formattedText = MeasureHeadingText();
		return new Rect(0.0, 0.0, 0.01, formattedText.Height);
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
	}

	private FormattedText MeasureHeadingText()
	{
		double emSize = Math.Max(1.0, Math.Max(Properties.FontRenderingEmSize, _baseFontSize * _magnify));
		return new FormattedText(" ", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Properties.Typeface, emSize, Brushes.Transparent);
	}
}
