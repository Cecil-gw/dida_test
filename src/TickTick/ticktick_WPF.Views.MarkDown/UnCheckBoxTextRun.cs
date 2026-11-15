using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class UnCheckBoxTextRun : FormattedTextRun
{
	private readonly int _length;

	private readonly int _indentLevel;

	private readonly double _size;

	private bool _isDark;

	public UnCheckBoxTextRun(FormattedTextElement element, TextRunProperties properties, int length, int indentLevel, bool isDark)
		: base(element, properties)
	{
		_isDark = isDark;
		_length = length;
		_indentLevel = indentLevel;
		_size = properties.FontRenderingEmSize + 2.0;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(double.PositiveInfinity);
		Point location = new Point(origin.X + MarkdownListStyleHelper.GetCenteredMarkerX(_indentLevel, MarkdownListStyleHelper.GetReservedWidth(_indentLevel), _size), origin.Y + (_size - 10.0) / 4.0 - textEmbeddedObjectMetrics.Baseline);
		BitmapImage imageSource = new BitmapImage(new Uri(_isDark ? "pack://application:,,,/Assets/md_unchecked_dark.png" : "pack://application:,,,/Assets/md_unchecked_light.png"));
		drawingContext.DrawImage(imageSource, new Rect(location, new Size(_size, _size)));
		base.Draw(drawingContext, origin, rightToLeft, sideways);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(MarkdownListStyleHelper.GetReservedWidth(_indentLevel) + textEmbeddedObjectMetrics.Width, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
