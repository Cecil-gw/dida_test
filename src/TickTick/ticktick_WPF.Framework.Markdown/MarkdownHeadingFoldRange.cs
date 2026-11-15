namespace ticktick_WPF.Framework.Markdown;

public sealed class MarkdownHeadingFoldRange
{
	public MarkdownBlockNode Heading { get; set; }

	public int HeadingLineIndex { get; set; }

	public int StartLineIndex { get; set; }

	public int EndLineIndex { get; set; }
}
