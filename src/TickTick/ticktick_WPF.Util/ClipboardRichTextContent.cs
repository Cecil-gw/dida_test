namespace ticktick_WPF.Util;

public sealed class ClipboardRichTextContent
{
	public string PlainText { get; set; } = string.Empty;

	public string PreferredText { get; set; } = string.Empty;

	public string HtmlFragment { get; set; } = string.Empty;

	public bool HasRichText => !string.IsNullOrWhiteSpace(HtmlFragment);

	public bool HasExplicitMarkdown { get; set; }

	public bool ShouldParseEmphasis { get; set; }

	public bool IsLiteralExport { get; set; }
}
