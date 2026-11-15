using System.Collections.Generic;

namespace ticktick_WPF.Framework.Markdown;

public sealed class MarkdownInlineNode
{
	public string Text { get; set; }

	public string RawText { get; set; }

	public int SourceStart { get; set; }

	public int SourceLength { get; set; }

	public int OpeningDelimiterLength { get; set; }

	public int ClosingDelimiterLength { get; set; }

	public List<MarkdownMark> Marks { get; } = new List<MarkdownMark>();
}
