using System.Collections.Generic;

namespace ticktick_WPF.Framework.Markdown;

public sealed class MarkdownBlockNode
{
	public MarkdownBlockKind Kind { get; set; }

	public int StartLine { get; set; }

	public int EndLine { get; set; }

	public int Indent { get; set; }

	public int? Level { get; set; }

	public MarkdownListKind? ListKind { get; set; }

	public bool? Checked { get; set; }

	public bool? Folded { get; set; }

	public bool? DisplayFolded { get; set; }

	public bool? SoftTab { get; set; }

	public string CustomOrder { get; set; }

	public string Language { get; set; }

	public string CodeText { get; set; }

	public string CodeBlockAttachmentIdentity { get; set; }

	public MarkdownFileBlockKind? FileKind { get; set; }

	public string Source { get; set; }

	public string AltText { get; set; }

	public string Caption { get; set; }

	public List<MarkdownInlineNode> Inlines { get; } = new List<MarkdownInlineNode>();
}
