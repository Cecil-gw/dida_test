using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class QuoteItemElement : FormattedTextElement
{
	private readonly int _indentLevel;

	public override bool IsSelectionBackgroundVisible => false;

	public override bool HandlesLineBorders => true;

	public QuoteItemElement(int prefixLength, int indentLevel, TextLine text)
		: base(text, prefixLength)
	{
		_indentLevel = indentLevel;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new QuoteItemTextRun(this, base.TextRunProperties, _indentLevel);
	}

	public override bool IsWhitespace(int visualColumn)
	{
		return visualColumn == 0;
	}

	public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		int num = base.VisualColumn + base.VisualLength;
		if (direction == LogicalDirection.Backward)
		{
			if (visualColumn <= num)
			{
				return -1;
			}
			return num;
		}
		if (visualColumn >= num)
		{
			return -1;
		}
		return num;
	}

	public override bool IsListStart()
	{
		return true;
	}
}
