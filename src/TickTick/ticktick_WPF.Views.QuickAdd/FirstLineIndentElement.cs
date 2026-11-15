using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.QuickAdd;

internal sealed class FirstLineIndentElement : FormattedTextElement
{
	private readonly QuickAddText _quickAddText;

	private readonly double _width;

	public override bool HandlesLineBorders => true;

	public FirstLineIndentElement(double width, QuickAddText quickAddText, TextLine text)
		: base(text, 0)
	{
		_quickAddText = quickAddText;
		_width = width;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new FirstLineIndentTextRun(_width, this, base.TextRunProperties);
	}

	public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		int num = base.VisualColumn + base.VisualLength;
		if (direction == LogicalDirection.Forward && visualColumn < num)
		{
			return num;
		}
		if (direction == LogicalDirection.Backward && visualColumn > num)
		{
			return num;
		}
		return -1;
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			_quickAddText.EditBox.Focus();
			_quickAddText.EditBox.CaretOffset = 0;
			e.Handled = true;
		}
	}
}
