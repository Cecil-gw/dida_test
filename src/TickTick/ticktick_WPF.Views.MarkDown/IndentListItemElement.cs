using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class IndentListItemElement : FormattedTextElement
{
	public IndentListItemElement(TextLine text)
		: base(text, 1)
	{
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new IndentListItemTextRun(this, base.TextRunProperties);
	}
}
