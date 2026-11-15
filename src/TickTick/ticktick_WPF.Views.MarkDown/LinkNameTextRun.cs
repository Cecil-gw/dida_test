using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class LinkNameTextRun : FormattedTextRun
{
	private readonly string _title;

	private readonly FormattedText _format;

	public LinkNameTextRun(FormattedTextElement element, string title, double fontSize, TextRunProperties properties)
		: base(element, properties)
	{
		_title = title;
		_format = new FormattedText(_title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), fontSize, ThemeUtil.GetColor("PrimaryColor"));
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		Point origin2 = new Point(origin.X, origin.Y);
		drawingContext.DrawText(_format, new Point(origin.X, 0.0));
		base.Draw(drawingContext, origin2, rightToLeft, sideways);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(_format.Width, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
