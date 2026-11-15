using System;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class NumberedListElementGenerator : VisualLineElementGenerator
{
	private readonly MarkDownEditor _editor;

	public NumberedListElementGenerator(MarkDownEditor markDownEditor)
	{
		_editor = markDownEditor;
	}

	private bool TryGetMatch(int startOffset, out int matchOffset, out string numberText, out int indentLevel, out int consumedLength)
	{
		matchOffset = -1;
		numberText = string.Empty;
		indentLevel = 0;
		consumedLength = 0;
		if (!MarkdownVisualLineHelper.TryGetSemanticBlockAtLineStart(_editor, base.CurrentContext, startOffset, out var block) || block.Kind != MarkdownBlockKind.List || block.ListKind != MarkdownListKind.Numbered)
		{
			return false;
		}
		int num = MarkdownVisualLineHelper.CountLeadingRenderPrefixCharacters(MarkdownVisualLineHelper.GetNormalizedLineText(base.CurrentContext));
		matchOffset = startOffset;
		numberText = (string.IsNullOrEmpty(block.CustomOrder) ? "1" : block.CustomOrder);
		indentLevel = MarkdownListStyleHelper.GetIndentLevel(block.Indent);
		consumedLength = num;
		return true;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (!TryGetMatch(startOffset, out var matchOffset, out var _, out var _, out var _))
		{
			return -1;
		}
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		try
		{
			if (TryGetMatch(offset, out var matchOffset, out var numberText, out var indentLevel, out var consumedLength) && matchOffset == offset && !MarkdownVisualLineHelper.IsSemanticCodeBlockAtOffset(_editor, offset))
			{
				string visibleRenderPrefixText = MarkdownVisualLineHelper.GetVisibleRenderPrefixText(MarkdownVisualLineHelper.GetNormalizedLineText(base.CurrentContext), consumedLength);
				return new NumberedListItemElement(numberText + ". ", consumedLength, indentLevel, base.CurrentContext.VisualLine, FormattedTextElement.PrepareText(TextFormatter.Create(), visibleRenderPrefixText, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)), ThemeUtil.GetColor("PrimaryColor", _editor), _editor);
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
