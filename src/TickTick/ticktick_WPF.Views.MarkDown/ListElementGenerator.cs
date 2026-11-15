using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public class ListElementGenerator : VisualLineElementGenerator
{
	private MarkDownEditor _editor;

	public ListElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	private bool TryGetMatch(int startOffset, out int matchOffset, out int indentLevel, out int consumedLength)
	{
		matchOffset = -1;
		indentLevel = 0;
		consumedLength = 0;
		if (!MarkdownVisualLineHelper.TryGetSemanticBlockAtLineStart(_editor, base.CurrentContext, startOffset, out var block) || block.Kind != MarkdownBlockKind.List || block.ListKind != MarkdownListKind.Bullet)
		{
			return false;
		}
		int num = MarkdownVisualLineHelper.CountLeadingRenderPrefixCharacters(MarkdownVisualLineHelper.GetNormalizedLineText(base.CurrentContext));
		matchOffset = startOffset;
		indentLevel = MarkdownListStyleHelper.GetIndentLevel(block.Indent);
		consumedLength = num;
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
		return ConstructVisualLineElement(offset);
	}

	private VisualLineElement ConstructVisualLineElement(int offset)
	{
		if (TryGetMatch(offset, out var matchOffset, out var indentLevel, out var consumedLength) && matchOffset == offset && !MarkdownVisualLineHelper.IsSemanticCodeBlockAtOffset(_editor, offset))
		{
			string visibleRenderPrefixText = MarkdownVisualLineHelper.GetVisibleRenderPrefixText(MarkdownVisualLineHelper.GetNormalizedLineText(base.CurrentContext), consumedLength);
			return new ListItemElement(consumedLength, indentLevel, FormattedTextElement.PrepareText(TextFormatter.Create(), visibleRenderPrefixText, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)), _editor);
		}
		return null;
	}
}
