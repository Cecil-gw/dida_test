using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.QuickAdd;

internal sealed class FirstLineIndentGenerator : VisualLineElementGenerator
{
	private readonly QuickAddText _quickAddText;

	public FirstLineIndentGenerator(QuickAddText quickAddText)
	{
		_quickAddText = quickAddText;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (_quickAddText.FirstLineIndent <= 0.0)
		{
			return -1;
		}
		VisualLine visualLine = base.CurrentContext?.VisualLine;
		if (visualLine?.FirstDocumentLine == null || visualLine.FirstDocumentLine.LineNumber != 1)
		{
			return -1;
		}
		int offset = visualLine.FirstDocumentLine.Offset;
		if (startOffset > offset)
		{
			return -1;
		}
		return offset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		double firstLineIndent = _quickAddText.FirstLineIndent;
		if (firstLineIndent <= 0.0)
		{
			return null;
		}
		return new FirstLineIndentElement(firstLineIndent, _quickAddText, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
	}
}
