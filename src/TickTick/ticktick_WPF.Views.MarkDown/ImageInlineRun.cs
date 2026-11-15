using System.Windows;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class ImageInlineRun : InlineObjectRun
{
	private readonly double _height;

	private readonly double _width;

	private UIElement _element;

	private readonly double _baselineExtra;

	private readonly double _baseline;

	public ImageInlineRun(int length, TextRunProperties properties, UIElement element, double width, double height, double baselineExtra, double baseline)
		: base(length, properties, element)
	{
		_width = width;
		_height = height;
		_element = element;
		_baselineExtra = baselineExtra;
		_baseline = baseline;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		return new TextEmbeddedObjectMetrics(_width, _height, _baseline + _baselineExtra);
	}
}
