using System.Windows;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class FileInlineRun : InlineObjectRun
{
	private readonly double _height;

	private readonly double _width;

	private FrameworkElement _element;

	private readonly double _baseline;

	public FileInlineRun(int length, TextRunProperties properties, FrameworkElement element, double width, double height, double baseline)
		: base(length, properties, element)
	{
		_width = width;
		_height = height;
		_element = element;
		_baseline = baseline;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		double height = ((_element.ActualHeight > 0.0) ? _element.ActualHeight : _height);
		return new TextEmbeddedObjectMetrics(_width, height, _baseline);
	}
}
