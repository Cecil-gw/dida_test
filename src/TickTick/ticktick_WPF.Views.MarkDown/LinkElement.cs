using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class LinkElement : FormattedTextElement
{
	public LinkElement(int length, TextLine text)
		: base(text, length)
	{
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new LinkTextRun(this, base.TextRunProperties);
	}
}
