using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockContentSpacerRun : TextEmbeddedObject
{
	private readonly TextLine _metricsLine;

	private readonly double _paddingWidth;

	private readonly TextRunProperties _properties;

	public override LineBreakCondition BreakBefore => LineBreakCondition.BreakPossible;

	public override LineBreakCondition BreakAfter => LineBreakCondition.BreakPossible;

	public override bool HasFixedSize => true;

	public override CharacterBufferReference CharacterBufferReference => default(CharacterBufferReference);

	public override int Length => 1;

	public override TextRunProperties Properties => _properties;

	public CodeBlockContentSpacerRun(TextLine metricsLine, double paddingWidth, TextRunProperties properties)
	{
		_metricsLine = metricsLine;
		_paddingWidth = paddingWidth;
		_properties = properties;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		return new TextEmbeddedObjectMetrics(_paddingWidth, _metricsLine.Height, _metricsLine.Baseline);
	}

	public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
	{
		return new Rect(0.0, 0.0, _paddingWidth, _metricsLine.Height);
	}

	public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
	{
	}
}
