using ICSharpCode.AvalonEdit.Document;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

internal static class CodeBlockLineClassifier
{
	public static bool IsFenceLine(TextDocument document, DocumentLine line)
	{
		if (document == null || line == null)
		{
			return false;
		}
		return MarkdownBlockSyntax.Parse(document.GetText(line.Offset, line.Length)).Kind == MarkdownBlockSyntaxKind.CodeFence;
	}

	public static bool IsContentLine(TextDocument document, DocumentLine line, MarkdownBlockNode block)
	{
		if (document == null || line == null || block == null || block.Kind != MarkdownBlockKind.CodeBlock)
		{
			return false;
		}
		int num = line.LineNumber - 1;
		if (num <= block.StartLine || num > block.EndLine)
		{
			return false;
		}
		if (num >= block.EndLine)
		{
			return !IsFenceLine(document, line);
		}
		return true;
	}
}
