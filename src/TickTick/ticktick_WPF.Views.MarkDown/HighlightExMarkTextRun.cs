using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class HighlightExMarkTextRun : FormattedTextRun
{
	private readonly bool _showMark;

	public HighlightExMarkTextRun(FormattedTextElement element, TextRunProperties properties, bool isDark, bool showMark)
		: base(element, properties)
	{
		_showMark = false;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		Point location = new Point(origin.X, origin.Y - 12.0);
		BitmapImage imageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/md_highlight_mark.png"));
		drawingContext.DrawImage(imageSource, new Rect(location, new Size(_showMark ? 8 : 0, 16.0)));
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = new FormattedText("::", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 14.0, ThemeUtil.GetPrimaryColor(1.0));
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);
		return new TextEmbeddedObjectMetrics(_showMark ? textEmbeddedObjectMetrics.Width : 0.0, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
