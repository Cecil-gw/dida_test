using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.QuickAdd;

public sealed class DateTokenTextRun : FormattedTextRun
{
	private readonly double _fontSize;

	private readonly string _token;

	private string _color;

	public DateTokenTextRun(FormattedTextElement element, TextRunProperties properties, string token, double fontSize, string color)
		: base(element, properties)
	{
		_color = color;
		_token = token;
		_fontSize = fontSize;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		FormattedText formattedText = new FormattedText(_token, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), _fontSize, ThemeUtil.GetAlphaColor(_color, 100));
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);
		drawingContext.DrawText(new FormattedText(_token, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), _fontSize, ThemeUtil.GetAlphaColor(_color, 100)), new Point(origin.X + 4.0, origin.Y - textEmbeddedObjectMetrics.Baseline));
		drawingContext.DrawRoundedRectangle(ThemeUtil.GetAlphaColor(_color, 20), null, new Rect(origin.X, origin.Y - textEmbeddedObjectMetrics.Baseline, textEmbeddedObjectMetrics.Width + 8.0, textEmbeddedObjectMetrics.Height), 2.0, 2.0);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = new FormattedText(_token, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), _fontSize, ThemeUtil.GetAlphaColor(_color, 100));
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);
		return new TextEmbeddedObjectMetrics(textEmbeddedObjectMetrics.Width + 9.0, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
