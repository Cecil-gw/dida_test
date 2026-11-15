using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockFenceElementGenerator : VisualLineElementGenerator
{
	private const double HiddenFenceWidth = 0.01;

	private readonly MarkDownEditor _editor;

	public CodeBlockFenceElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (!TryMatchFenceLine(startOffset, out var matchOffset, out var _, out var _))
		{
			return -1;
		}
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		if (!TryMatchFenceLine(offset, out var matchOffset, out var consumedLength, out var height) || matchOffset != offset)
		{
			return null;
		}
		return new HiddenCodeBlockFenceElement(consumedLength, 0.01, height);
	}

	private bool TryMatchFenceLine(int startOffset, out int matchOffset, out int consumedLength, out double height)
	{
		matchOffset = -1;
		consumedLength = 0;
		height = 0.0;
		TextDocument textDocument = _editor?.EditBox?.Document;
		DocumentLine documentLine = base.CurrentContext?.VisualLine?.FirstDocumentLine;
		if (textDocument == null || documentLine == null || documentLine.Length <= 0 || documentLine.Offset < startOffset || !_editor.TryGetSemanticBlockAtOffset(documentLine.Offset, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock || _editor.IsCodeBlockAttachment(block))
		{
			return false;
		}
		int num = documentLine.LineNumber - 1;
		if (!CodeBlockLineClassifier.IsFenceLine(textDocument, documentLine))
		{
			return false;
		}
		if (num == block.StartLine)
		{
			height = 24.0;
		}
		else
		{
			if (num != block.EndLine || num <= block.StartLine)
			{
				return false;
			}
			height = 12.0;
		}
		matchOffset = documentLine.Offset;
		consumedLength = documentLine.Length;
		return true;
	}
}
