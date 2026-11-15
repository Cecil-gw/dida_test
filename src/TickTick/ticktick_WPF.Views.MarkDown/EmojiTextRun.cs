using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using Emoji.Wpf;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class EmojiTextRun : FormattedTextRun
{
	private readonly BitmapSource _bitmap;

	private readonly double _scale;

	private readonly double _fontSize;

	private double ImageWidth => _bitmap.Width / _scale;

	private double ImageHeight => _bitmap.Height / _scale;

	public EmojiTextRun(FormattedTextElement element, string emoji, TextRunProperties properties)
		: base(element, properties)
	{
		_fontSize = Math.Max(1.0, properties.FontRenderingEmSize);
		_scale = Math.Pow(2.0, Math.Max(0.0, Math.Ceiling(5.0 - Math.Log(_fontSize, 2.0))));
		_bitmap = EmojiRenderer.RenderBitmap(emoji ?? "", _fontSize * _scale, properties.ForegroundBrush);
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		drawingContext.DrawImage(rectangle: new Rect(new Point(origin.X, origin.Y - GetImageTopOffset()), new Size(ImageWidth, ImageHeight)), imageSource: _bitmap);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		return GetLayoutMetrics();
	}

	public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
	{
		TextEmbeddedObjectMetrics layoutMetrics = GetLayoutMetrics();
		return new Rect(0.0, 0.0, layoutMetrics.Width, layoutMetrics.Height);
	}

	private double GetImageTopOffset()
	{
		return _fontSize + (double)((_fontSize >= 16.0) ? 2 : 0);
	}

	private TextEmbeddedObjectMetrics GetLayoutMetrics()
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(0.0);
		double imageTopOffset = GetImageTopOffset();
		double num = Math.Max(textEmbeddedObjectMetrics.Baseline, imageTopOffset);
		double height = Math.Max(textEmbeddedObjectMetrics.Height, num - imageTopOffset + ImageHeight);
		return new TextEmbeddedObjectMetrics(ImageWidth, height, num);
	}
}
