using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class QuoteItemTextRun : FormattedTextRun
{
	private readonly int _indentLevel;

	public QuoteItemTextRun(FormattedTextElement element, TextRunProperties properties, int indentLevel)
		: base(element, properties)
	{
		_indentLevel = indentLevel;
	}

	public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
	{
		TextEmbeddedObjectMetrics textEmbeddedObjectMetrics = base.Format(remainingParagraphWidth);
		return new TextEmbeddedObjectMetrics(15 + _indentLevel * 16, textEmbeddedObjectMetrics.Height, textEmbeddedObjectMetrics.Baseline);
	}
}
