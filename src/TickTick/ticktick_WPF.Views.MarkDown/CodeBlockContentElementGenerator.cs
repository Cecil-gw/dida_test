using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockContentElementGenerator : VisualLineElementGenerator
{
	private const double CodeBlockContentPadding = 16.0;

	internal const double CodeBlockContentRightPadding = 16.0;

	private readonly MarkDownEditor _editor;

	public CodeBlockContentElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (!TryMatchCodeBlockContentLine(startOffset, out var matchOffset))
		{
			return -1;
		}
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		if (!TryMatchCodeBlockContentLine(offset, out var matchOffset) || matchOffset != offset)
		{
			return null;
		}
		return new CodeBlockContentSpacerElement(FormattedTextElement.PrepareText(TextFormatter.Create(), " ", new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)), 16.0, 16.0);
	}

	private bool TryMatchCodeBlockContentLine(int startOffset, out int matchOffset)
	{
		matchOffset = -1;
		if (_editor?.EditBox?.Document == null || base.CurrentContext?.VisualLine?.FirstDocumentLine == null || !_editor.TryGetSemanticBlockAtOffset(startOffset, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock || _editor.IsCodeBlockAttachment(block))
		{
			return false;
		}
		DocumentLine firstDocumentLine = base.CurrentContext.VisualLine.FirstDocumentLine;
		_ = firstDocumentLine.LineNumber;
		if (!CodeBlockLineClassifier.IsContentLine(_editor.EditBox.Document, firstDocumentLine, block))
		{
			return false;
		}
		if (startOffset != firstDocumentLine.Offset)
		{
			return false;
		}
		matchOffset = firstDocumentLine.Offset;
		return true;
	}
}
