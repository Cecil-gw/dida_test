using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class ParagraphIndentTextRun : FormattedTextRun
{
	private readonly int _indentLevel;

	private readonly TextRunProperties _properties;

	public ParagraphIndentTextRun(FormattedTextElement element, TextRunProperties properties, int indentLevel)
		: base(element, properties)
	{
		_properties = properties;
		_indentLevel = indentLevel;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(MeasureParagraphIndentWidth(_indentLevel, _properties), textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}

	private static double MeasureParagraphIndentWidth(int indentLevel, TextRunProperties properties)
	{
		if (indentLevel <= 0 || properties == null)
		{
			return 0.0;
		}
		return new FormattedText(new string('\u3000', indentLevel), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, properties.Typeface, properties.FontRenderingEmSize, Brushes.Transparent).WidthIncludingTrailingWhitespace;
	}
}
