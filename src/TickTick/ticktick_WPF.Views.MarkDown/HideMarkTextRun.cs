using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class HideMarkTextRun : FormattedTextRun
{
	private double _width;

	public HideMarkTextRun(double width, FormattedTextElement element, TextRunProperties properties)
		: base(element, properties)
	{
		_width = width;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = new FormattedText(string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), _width, ThemeUtil.GetPrimaryColor(0.0));
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);
		return new TextEmbeddedObjectMetrics(_width, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
