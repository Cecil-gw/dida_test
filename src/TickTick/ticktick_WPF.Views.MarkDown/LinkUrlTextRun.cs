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

public sealed class LinkUrlTextRun : FormattedTextRun
{
	private readonly bool _isTaskLink;

	private bool _dark;

	public LinkUrlTextRun(FormattedTextElement element, bool isTaskLink, TextRunProperties properties, bool dark)
		: base(element, properties)
	{
		_dark = dark;
		_isTaskLink = isTaskLink;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		Point location = new Point(origin.X, origin.Y - 12.0);
		string uriString = (_dark ? "pack://application:,,,/Assets/md_link_dark.png" : "pack://application:,,,/Assets/md_link_light.png");
		if (_isTaskLink)
		{
			uriString = (_dark ? "pack://application:,,,/Assets/md_task_link_dark.png" : "pack://application:,,,/Assets/md_task_link_light.png");
		}
		BitmapImage imageSource = new BitmapImage(new Uri(uriString));
		drawingContext.DrawImage(imageSource, new Rect(location, new Size(16.0, 16.0)));
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		FormattedText formattedText = new FormattedText(string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 14.0, ThemeUtil.GetPrimaryColor(1.0));
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = new TextEmbeddedObjectMetrics(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height, formattedText.Baseline);
		return new TextEmbeddedObjectMetrics(16.0, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
