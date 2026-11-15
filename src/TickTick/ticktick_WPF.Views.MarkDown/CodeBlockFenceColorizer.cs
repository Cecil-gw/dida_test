using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockFenceColorizer : DocumentColorizingTransformer
{
	private readonly MarkDownEditor _editor;

	public CodeBlockFenceColorizer(MarkDownEditor editor)
	{
		_editor = editor;
	}

	protected override void ColorizeLine(DocumentLine line)
	{
		if (_editor == null || line == null || !_editor.TryGetSemanticBlockAtOffset(line.Offset, out var block) || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return;
		}
		int num = line.LineNumber - 1;
		if ((num == block.StartLine || num == block.EndLine) && CodeBlockLineClassifier.IsFenceLine(_editor.EditBox?.Document, line))
		{
			ChangeLinePart(line.Offset, line.EndOffset, delegate(VisualLineElement element)
			{
				element.TextRunProperties.SetForegroundBrush(Brushes.Transparent);
			});
		}
	}
}
