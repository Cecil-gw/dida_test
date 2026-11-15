using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class IndentListItemTextRun : FormattedTextRun
{
	private readonly int _flagLength;

	public IndentListItemTextRun(FormattedTextElement element, TextRunProperties properties)
		: base(element, properties)
	{
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		Point origin2 = new Point(origin.X + 1.5, origin.Y + 3.0);
		double y = (base.Format(double.PositiveInfinity).Height - 6.0) / 2.0;
		drawingContext.DrawRoundedRectangle(ThemeUtil.GetPrimaryColor(1.0), null, new Rect(23.0, y, 6.0, 6.0), 3.0, 3.0);
		base.Draw(drawingContext, origin2, rightToLeft, sideways);
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(40.0, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
