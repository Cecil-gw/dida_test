using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using Emoji.Wpf;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.QuickAdd;

public sealed class TagTokenRun : FormattedTextRun
{
	private class TokenTextSegment
	{
		public string Text { get; set; }

		public ImageSource Bitmap { get; set; }

		public double Width { get; set; }
	}

	private readonly double _fontSize;

	private readonly QuickAddToken _token;

	private double _emojiScale;

	private Brush _foreground;

	private Brush _background;

	public TagTokenRun(FormattedTextElement element, TextRunProperties properties, QuickAddToken token, double fontSize, Brush foreground, Brush background)
		: base(element, properties)
	{
		_token = token;
		_fontSize = fontSize;
		_emojiScale = Math.Pow(2.0, Math.Max(0.0, Math.Ceiling(5.0 - Math.Log(_fontSize, 2.0))));
		_foreground = foreground;
		_background = background;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		TextEmbeddedObjectMetrics metrics = GetMetrics(_foreground);
		double num = ((origin.Y < metrics.Height) ? 0.0 : (origin.Y - metrics.Baseline));
		drawingContext.DrawRoundedRectangle(_background, null, new Rect(origin.X, num, metrics.Width + 8.0, metrics.Height), 2.0, 2.0);
		double num2 = origin.X + 4.0;
		foreach (TokenTextSegment item in BuildSegments(_foreground))
		{
			if (item.Bitmap != null)
			{
				double num3 = item.Bitmap.Width / _emojiScale;
				double num4 = item.Bitmap.Height / _emojiScale;
				double y = num + (metrics.Height - num4) / 2.0 - 1.0;
				drawingContext.DrawImage(item.Bitmap, new Rect(new Point(num2, y), new Size(num3, num4)));
				num2 += num3;
			}
			else if (!string.IsNullOrEmpty(item.Text))
			{
				drawingContext.DrawText(CreateFormattedText(item.Text, _foreground), new Point(num2, num));
				num2 += item.Width;
			}
		}
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics metrics = GetMetrics(_foreground);
		return new TextEmbeddedObjectMetrics(metrics.Width + 9.0, metrics.Height, metrics.Baseline);
	}

	private TextEmbeddedObjectMetrics GetMetrics(Brush foreground)
	{
		FormattedText formattedText = CreateFormattedText(" ", foreground);
		return new TextEmbeddedObjectMetrics(BuildSegments(foreground).Sum((TokenTextSegment segment) => segment.Width), formattedText.Height, formattedText.Baseline);
	}

	private FormattedText CreateFormattedText(string text, Brush foreground)
	{
		return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(LocalSettings.Settings.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), _fontSize, foreground);
	}

	private List<TokenTextSegment> BuildSegments(Brush foreground)
	{
		List<TokenTextSegment> list = new List<TokenTextSegment>();
		string text = _token.Value ?? string.Empty;
		int num = 0;
		foreach (Match item in ((text.Length > 200) ? EmojiData.MatchOne2 : EmojiData.MatchOne).Matches(text))
		{
			if (item.Index > num)
			{
				AddTextSegment(list, text.Substring(num, item.Index - num), foreground);
			}
			AddEmojiSegment(list, item.Value, foreground);
			num = item.Index + item.Length;
		}
		if (num < text.Length)
		{
			AddTextSegment(list, text.Substring(num), foreground);
		}
		return list;
	}

	private void AddTextSegment(ICollection<TokenTextSegment> segments, string text, Brush foreground)
	{
		if (!string.IsNullOrEmpty(text))
		{
			segments.Add(new TokenTextSegment
			{
				Text = text,
				Width = CreateFormattedText(text, foreground).WidthIncludingTrailingWhitespace
			});
		}
	}

	private void AddEmojiSegment(ICollection<TokenTextSegment> segments, string emoji, Brush foreground)
	{
		BitmapSource bitmapSource = EmojiRenderer.RenderBitmap(emoji, _fontSize * _emojiScale, foreground);
		segments.Add(new TokenTextSegment
		{
			Bitmap = bitmapSource,
			Width = bitmapSource.Width / _emojiScale
		});
	}
}
