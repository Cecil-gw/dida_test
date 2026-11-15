using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class CheckBoxTextRun : FormattedTextRun
{
	internal const string CompletedTaskIconResourceKey = "Ic.CalendarType.Completed";

	private readonly int _length;

	private readonly int _indentLevel;

	private readonly double _size;

	private readonly MarkDownEditor _editor;

	public CheckBoxTextRun(FormattedTextElement element, TextRunProperties properties, int length, int indentLevel, MarkDownEditor editor)
		: base(element, properties)
	{
		_editor = editor;
		_length = length;
		_indentLevel = indentLevel;
		_size = properties.FontRenderingEmSize + 2.0;
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(double.PositiveInfinity);
		Point location = new Point(origin.X + MarkdownListStyleHelper.GetCenteredMarkerX(_indentLevel, MarkdownListStyleHelper.GetReservedWidth(_indentLevel), _size), origin.Y + (_size - 10.0) / 4.0 - textEmbeddedObjectMetrics.Baseline);
		DrawCompletedTaskIcon(drawingContext, location);
		base.Draw(drawingContext, origin, rightToLeft, sideways);
	}

	private void DrawCompletedTaskIcon(DrawingContext drawingContext, Point location)
	{
		Geometry geometry = Utils.GetGeometry("Ic.CalendarType.Completed");
		if (geometry != null && !geometry.Bounds.IsEmpty)
		{
			Rect bounds = geometry.Bounds;
			double num = _size / Math.Max(bounds.Width, bounds.Height);
			Geometry geometry2 = geometry.Clone();
			geometry2.Transform = new MatrixTransform(new Matrix(num, 0.0, 0.0, num, location.X - bounds.X * num, location.Y - bounds.Y * num));
			drawingContext.DrawGeometry(ThemeUtil.GetColor("BaseColorOpacity20", _editor), null, geometry2);
		}
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(MarkdownListStyleHelper.GetReservedWidth(_indentLevel) + textEmbeddedObjectMetrics.Width, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
