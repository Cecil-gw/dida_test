using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class ListItemTextRun : FormattedTextRun
{
	private readonly int _length;

	private readonly MarkDownEditor _editor;

	private readonly int _level;

	public ListItemTextRun(FormattedTextElement element, int length, int level, TextRunProperties properties, MarkDownEditor editor)
		: base(element, properties)
	{
		if (length <= 0)
		{
			length = 0;
		}
		_editor = editor;
		_length = length;
		_level = level;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		Point origin2 = new Point(origin.X + 1.5, origin.Y + 3.0);
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(double.PositiveInfinity);
		SolidColorBrush color = ThemeUtil.GetColor("PrimaryColor", _editor);
		double num = (textEmbeddedObjectMetrics.Height - 6.0) / 2.0;
		double bulletX = MarkdownListStyleHelper.GetBulletX(_level);
		switch (MarkdownListStyleHelper.GetBulletStyle(_level))
		{
		case MarkdownBulletStyle.HollowCircle:
			drawingContext.DrawEllipse(null, new Pen(color, 1.1), new Point(bulletX + 3.0, num + 3.0), 2.6, 2.6);
			break;
		case MarkdownBulletStyle.SolidSquare:
			drawingContext.DrawRectangle(color, null, new Rect(bulletX, num, 5.2, 5.2));
			break;
		default:
			drawingContext.DrawRoundedRectangle(color, null, new Rect(bulletX, num, 5.0, 5.0), 3.0, 3.0);
			break;
		}
		base.Draw(drawingContext, origin2, rightToLeft, sideways);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(MarkdownListStyleHelper.GetReservedWidth(_level) + textEmbeddedObjectMetrics.Width, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
