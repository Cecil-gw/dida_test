using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

public sealed class NumberedListItemTextRun : FormattedTextRun
{
	private readonly int _length;

	private string _numberText;

	private readonly TextRunProperties _properties;

	private readonly SolidColorBrush _brush;

	private readonly int _indentLevel;

	public NumberedListItemTextRun(FormattedTextElement element, string numberText, int length, int indentLevel, TextRunProperties properties, SolidColorBrush brush)
		: base(element, properties)
	{
		_brush = brush;
		_numberText = numberText;
		_properties = properties;
		_length = length;
		_indentLevel = indentLevel;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		Point origin2 = new Point(origin.X, origin.Y);
		FormattedText formattedText = CreateNumberText();
		double reservedWidth = GetReservedWidth(formattedText);
		double x = origin2.X + MarkdownListStyleHelper.GetCenteredMarkerX(_indentLevel, reservedWidth, formattedText.Width);
		drawingContext.DrawText(formattedText, new Point(x, 0.0));
		base.Draw(drawingContext, origin2, rightToLeft, sideways);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = CreateNumberText();
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);
		return new TextEmbeddedObjectMetrics(GetReservedWidth(formattedText) + base.Format(remainingParagraphWidth).Width, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}

	private FormattedText CreateNumberText()
	{
		return new FormattedText(_numberText.TrimStart(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Constants.DefaultFont), _properties.FontRenderingEmSize, _brush);
	}

	private double GetReservedWidth(FormattedText text)
	{
		return MarkdownListStyleHelper.GetReservedWidth(_indentLevel, text?.WidthIncludingTrailingWhitespace ?? 0.0);
	}
}
