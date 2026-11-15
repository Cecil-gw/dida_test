using ticktick_WPF.Framework.Markdown;

namespace ticktick_WPF.Views.MarkDown;

public sealed class MarkdownInlineDisplaySegment
{
	public MarkdownMarkKind Kind { get; set; }

	public int SourcePosition { get; set; }

	public int SourceLength { get; set; }

	public int OpeningDelimiterLength { get; set; }

	public int ClosingDelimiterLength { get; set; }

	public string Color { get; set; }

	public string LinkText { get; set; }

	public string Url { get; set; }

	public int UrlPosition { get; set; }

	public int UrlLength { get; set; }

	public int ContentPosition => SourcePosition + OpeningDelimiterLength;

	public int ContentLength => SourceLength - OpeningDelimiterLength - ClosingDelimiterLength;
}
