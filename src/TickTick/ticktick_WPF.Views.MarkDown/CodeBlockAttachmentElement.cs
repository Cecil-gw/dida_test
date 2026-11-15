using System.Windows;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockAttachmentElement : InlineObjectElement
{
	private readonly double _width;

	private readonly double _height;

	public CodeBlockAttachmentElement(int documentLength, UIElement element, double width, double height)
		: base(documentLength, element)
	{
		_width = width;
		_height = height;
		base.TextAlignment = TextAlignment.Left;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new CodeBlockAttachmentRun(1, base.TextRunProperties, base.Element, _width, _height, EmbeddedObjectMetricsHelper.ResolveTextBaseline(base.TextRunProperties));
	}
}
