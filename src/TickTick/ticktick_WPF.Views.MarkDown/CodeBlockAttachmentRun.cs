using System.Windows;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockAttachmentRun : InlineObjectRun
{
	private readonly double _width;

	private readonly double _height;

	private readonly double _baseline;

	public CodeBlockAttachmentRun(int length, TextRunProperties properties, UIElement element, double width, double height, double baseline)
		: base(length, properties, element)
	{
		_width = width;
		_height = height;
		_baseline = baseline;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		return new TextEmbeddedObjectMetrics(_width, _height, _baseline);
	}
}
