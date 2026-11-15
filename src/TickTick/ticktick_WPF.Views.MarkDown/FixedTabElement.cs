using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class FixedTabElement : FormattedTextElement
{
	internal const int DisplaySpaceCount = 4;

	private static readonly string DisplayText = new string(' ', 4);

	public FixedTabElement()
		: base(DisplayText, 1)
	{
	}

	public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		if (mode != CaretPositioningMode.Normal && mode != CaretPositioningMode.EveryCodepoint)
		{
			return -1;
		}
		return base.GetNextCaretPosition(visualColumn, direction, mode);
	}

	public override bool IsWhitespace(int visualColumn)
	{
		return true;
	}
}
