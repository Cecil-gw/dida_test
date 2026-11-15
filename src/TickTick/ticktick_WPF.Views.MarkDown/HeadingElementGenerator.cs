using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public class HeadingElementGenerator : VisualLineElementGenerator
{
	public static readonly Regex DefaultRegex = new Regex("^#{1,6} ");

	private readonly MarkDownEditor _editor;

	public HeadingElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
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
		if (!TryGetMatch(offset, out var matchOffset, out var consumedLength, out var magnify) || matchOffset != offset || MarkdownVisualLineHelper.IsSemanticCodeBlockAtOffset(_editor, offset))
		{
			return null;
		}
		return new EmptyHeadingElement(consumedLength, magnify, base.CurrentContext.GlobalTextRunProperties.FontRenderingEmSize, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
	}

	private bool TryGetMatch(int startOffset, out int matchOffset, out int consumedLength, out double magnify)
	{
		matchOffset = -1;
		consumedLength = 0;
		magnify = 1.0;
		int num = base.CurrentContext?.VisualLine?.FirstDocumentLine?.Offset ?? (-1);
		if (startOffset != num)
		{
			return false;
		}
		if (!MarkdownVisualLineHelper.TryGetSemanticBlockAtLineStart(_editor, base.CurrentContext, num, out var block) || block.Kind != MarkdownBlockKind.Heading || !block.Level.HasValue)
		{
			return false;
		}
		string normalizedLineText = MarkdownVisualLineHelper.GetNormalizedLineText(base.CurrentContext);
		if (normalizedLineText.Any((char ch) => !IsInvisibleHeadingContent(ch)))
		{
			return false;
		}
		matchOffset = num;
		consumedLength = normalizedLineText.Length;
		magnify = GetHeadingMagnify(block.Level.Value);
		return true;
	}

	internal static double GetHeadingMagnify(int level)
	{
		return Math.Max(1, Math.Min(6, level)) switch
		{
			1 => 1.29, 
			2 => 1.14, 
			3 => 1.0, 
			4 => 1.0, 
			5 => 1.0, 
			_ => 1.0, 
		};
	}

	private static bool IsInvisibleHeadingContent(char ch)
	{
		if (ch != '\u00a0' && ch != '\u2065' && ch != '\u200b' && ch != '\u200c' && ch != '\u200d' && ch != '\u200e' && ch != '\u200f' && ch != '\u2060' && ch != '\u2061' && ch != '\u2062' && ch != '\u2063' && ch != '\u2064' && ch != '\u2065' && ch != '\u2066' && ch != '\u206a')
		{
			return char.IsWhiteSpace(ch);
		}
		return true;
	}
}
