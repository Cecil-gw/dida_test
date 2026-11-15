using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class FixedTabElementGenerator : VisualLineElementGenerator
{
	public override int GetFirstInterestedOffset(int startOffset)
	{
		TextDocument textDocument = base.CurrentContext?.Document;
		DocumentLine documentLine = base.CurrentContext?.VisualLine?.LastDocumentLine;
		if (textDocument == null || documentLine == null || startOffset < 0)
		{
			return -1;
		}
		for (int i = startOffset; i < documentLine.EndOffset; i++)
		{
			if (textDocument.GetCharAt(i) == '\t')
			{
				return i;
			}
		}
		return -1;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		TextDocument textDocument = base.CurrentContext?.Document;
		if (textDocument == null || offset < 0 || offset >= textDocument.TextLength || textDocument.GetCharAt(offset) != '\t')
		{
			return null;
		}
		return new FixedTabElement();
	}
}
