using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public class SpellCheckBackgroundRenderer : IBackgroundRenderer
{
	public TextSegmentCollection<TextSegment> ErrorSegments { get; }

	public Func<int, bool> IsOffsetHidden { get; set; }

	public KnownLayer Layer => KnownLayer.Selection;

	public SpellCheckBackgroundRenderer()
	{
		ErrorSegments = new TextSegmentCollection<TextSegment>();
	}

	public void Draw(TextView textView, DrawingContext drawingContext)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		Pen pen = new Pen(new SolidColorBrush(GetSpellCheckColor()), 0.6);
		pen.Freeze();
		foreach (TextSegment errorSegment in ErrorSegments)
		{
			Func<int, bool> isOffsetHidden = IsOffsetHidden;
			if (isOffsetHidden != null && isOffsetHidden(errorSegment.StartOffset))
			{
				continue;
			}
			foreach (Rect item in BackgroundGeometryBuilder.GetRectsForSegment(textView, errorSegment))
			{
				Point bottomLeft = item.BottomLeft;
				int count = Math.Max((int)((item.BottomRight.X - bottomLeft.X) / 2.0) + 1, 4);
				StreamGeometry streamGeometry = new StreamGeometry();
				using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
				{
					streamGeometryContext.BeginFigure(bottomLeft, isFilled: false, isClosed: false);
					streamGeometryContext.PolyLineTo(CreatePoints(bottomLeft, 2.0, count).ToArray(), isStroked: true, isSmoothJoin: false);
				}
				streamGeometry.Freeze();
				drawingContext.DrawGeometry(Brushes.Transparent, pen, streamGeometry);
				if (stopwatch.ElapsedMilliseconds > 100)
				{
					break;
				}
			}
			if (stopwatch.ElapsedMilliseconds > 100)
			{
				break;
			}
		}
		stopwatch.Stop();
	}

	private static Color GetSpellCheckColor()
	{
		return Colors.Red;
	}

	private static IEnumerable<Point> CreatePoints(Point start, double offset, int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new Point(start.X + (double)i * offset, start.Y - (((i + 1) % 2 == 0) ? offset : 0.0));
		}
	}
}
