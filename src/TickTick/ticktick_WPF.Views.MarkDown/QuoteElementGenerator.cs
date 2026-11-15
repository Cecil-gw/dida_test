using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public class QuoteElementGenerator : VisualLineElementGenerator
{
	private readonly MarkDownEditor _editor;

	public QuoteElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	private bool TryGetMatch(int startOffset, out int matchOffset, out int consumedLength, out int indentLevel)
	{
		matchOffset = -1;
		consumedLength = 0;
		indentLevel = 0;
		if (!MarkdownVisualLineHelper.TryGetSemanticBlockAtLineStart(_editor, base.CurrentContext, startOffset, out var block) || block.Kind != MarkdownBlockKind.BlockQuote)
		{
			return false;
		}
		string normalizedLineText = MarkdownVisualLineHelper.GetNormalizedLineText(base.CurrentContext);
		consumedLength = MarkdownVisualLineHelper.CountLeadingRenderPrefixCharacters(normalizedLineText);
		indentLevel = MarkdownListStyleHelper.GetIndentLevel(block.Indent);
		matchOffset = startOffset;
		return true;
	}

	internal static bool HasRenderableQuoteMarker(string lineText)
	{
		int prefixLength;
		return TryGetQuotePrefixLength(lineText, out prefixLength);
	}

	internal static bool TryGetQuotePrefixLength(string lineText, out int prefixLength)
	{
		prefixLength = 0;
		if (string.IsNullOrEmpty(lineText))
		{
			return false;
		}
		int num = MarkdownVisualLineHelper.CountLeadingWhitespaceCharacters(lineText);
		prefixLength = num + 2;
		if (lineText.Length < prefixLength)
		{
			prefixLength = 0;
			return false;
		}
		string text = lineText.Substring(num, 2);
		if (text != "> " && text != "\ue125\ue126")
		{
			prefixLength = 0;
			return false;
		}
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
			return new QuoteItemElement(consumedLength, indentLevel, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
		}
		return null;
	}
}
