using System.Windows;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class AttachmentElement : InlineObjectElement
{
	private readonly double _height;

	private readonly double _width;

	private readonly double _baselineExtra;

	private readonly bool _isFile;

	public AttachmentElement(int length, UIElement element, double width, double height, double baselineExtra, TextAlignment alignment, bool isFile = false)
		: base(length, element)
	{
		_width = width;
		_height = height;
		base.TextAlignment = alignment;
		_baselineExtra = baselineExtra;
		_isFile = isFile;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		if (context == null)
		{
			return null;
		}
		double baseline = EmbeddedObjectMetricsHelper.ResolveTextBaseline(base.TextRunProperties);
		if (_isFile)
		{
			return new FileInlineRun(1, base.TextRunProperties, base.Element as FrameworkElement, _width, _height, baseline);
		}
		return new ImageInlineRun(1, base.TextRunProperties, base.Element, _width, _height, _baselineExtra, baseline);
	}
}
