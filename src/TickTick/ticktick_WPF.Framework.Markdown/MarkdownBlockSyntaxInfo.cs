namespace ticktick_WPF.Framework.Markdown;

public sealed class MarkdownBlockSyntaxInfo
{
	public MarkdownBlockSyntaxKind Kind { get; set; }

	public int Indent { get; set; }

	public int? HeadingLevel { get; set; }

	public MarkdownListKind? ListKind { get; set; }

	public bool? Checked { get; set; }

	public string CustomOrder { get; set; }

	public string Content { get; set; }
}
