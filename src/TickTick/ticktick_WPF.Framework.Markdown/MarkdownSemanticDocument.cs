using System.Collections.Generic;

namespace ticktick_WPF.Framework.Markdown;

public sealed class MarkdownSemanticDocument
{
	public List<MarkdownBlockNode> Blocks { get; } = new List<MarkdownBlockNode>();
}
