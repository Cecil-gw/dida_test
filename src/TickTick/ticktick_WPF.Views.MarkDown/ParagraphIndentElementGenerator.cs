using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public class ParagraphIndentElementGenerator : VisualLineElementGenerator
{
	private readonly MarkDownEditor _editor;

	public ParagraphIndentElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	private bool TryGetMatch(int startOffset, out int matchOffset, out int consumedLength, out int indentLevel)
	{
		matchOffset = -1;
		consumedLength = 0;
		indentLevel = 0;
		if (!MarkdownVisualLineHelper.TryGetSemanticBlockAtLineStart(_editor, base.CurrentContext, startOffset, out var block) || block.Kind != MarkdownBlockKind.Paragraph || block.Indent <= 0)
		{
			return false;
		}
		string normalizedLineText = MarkdownVisualLineHelper.GetNormalizedLineText(base.CurrentContext);
		if (string.IsNullOrEmpty(normalizedLineText) || normalizedLineText[0] != '\u2065')
		{
			return false;
		}
		matchOffset = startOffset;
		consumedLength = 1;
		indentLevel = MarkdownListStyleHelper.GetIndentLevel(block.Indent);
		return true;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (!TryGetMatch(startOffset, out var matchOffset, out var _, out var _))
		{
			return -1;
		}
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		if (TryGetMatch(offset, out var matchOffset, out var consumedLength, out var indentLevel) && matchOffset == offset && !MarkdownVisualLineHelper.IsSemanticCodeBlockAtOffset(_editor, offset))
		{
			return new ParagraphIndentElement(consumedLength, indentLevel, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
		}
		return null;
	}
}
