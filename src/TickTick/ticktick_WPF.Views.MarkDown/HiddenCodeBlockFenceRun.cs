using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class HiddenCodeBlockFenceRun : TextEmbeddedObject
{
	private readonly HiddenCodeBlockFenceElement _element;

	private readonly TextRunProperties _properties;

	private readonly double _width;

	private readonly double _height;

	public override LineBreakCondition BreakBefore => LineBreakCondition.BreakPossible;

	public override LineBreakCondition BreakAfter => LineBreakCondition.BreakPossible;

	public override bool HasFixedSize => true;

	public override CharacterBufferReference CharacterBufferReference => default(CharacterBufferReference);

	public override int Length => _element.VisualLength;

	public override TextRunProperties Properties => _properties;

	public HiddenCodeBlockFenceRun(HiddenCodeBlockFenceElement element, TextRunProperties properties, double width, double height)
	{
		_element = element;
		_properties = properties;
		_width = width;
		_height = height;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		return new TextEmbeddedObjectMetrics(_width, _height, _height);
	}

	public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
	{
		return new Rect(0.0, 0.0, _width, _height);
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
	}
}
