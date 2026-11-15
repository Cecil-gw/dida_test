using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public class SplitLineElementGenerator : VisualLineElementGenerator
{
	public static readonly Regex DefaultLinkRegex = new Regex("^(?:" + BuildRepeatedMarkerPattern("---") + "|" + Regex.Escape("***") + ")$");

	private readonly MarkDownEditor _markdownEditor;

	private readonly TextEditor _editor;

	private static string BuildRepeatedMarkerPattern(string marker)
	{
		return Regex.Escape(marker[0].ToString()) + "{" + marker.Length + ",}";
	}

	public SplitLineElementGenerator(MarkDownEditor markdownEditor, TextEditor editBox)
	{
		_markdownEditor = markdownEditor;
		_editor = editBox;
	}

	private bool TryGetMatch(int startOffset, out int matchOffset, out int length)
	{
		matchOffset = -1;
		length = 0;
		if (!MarkdownVisualLineHelper.TryGetMatchingLineSyntax(_markdownEditor, base.CurrentContext, startOffset, out var block, out var _, out var _) || block.Kind != MarkdownBlockKind.HorizontalRule)
		{
			return false;
		}
		matchOffset = startOffset;
		length = base.CurrentContext.VisualLine.LastDocumentLine.Length;
		return true;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (!TryGetMatch(startOffset, out var matchOffset, out var _))
		{
			return -1;
		}
		return matchOffset;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		if (TryGetMatch(offset, out var matchOffset, out var length) && matchOffset == offset)
		{
			return new LineElement(length, _editor, FormattedTextElement.PrepareText(TextFormatter.Create(), string.Empty, new VisualLineElementTextRunProperties(base.CurrentContext.GlobalTextRunProperties)));
		}
		return null;
	}
}
