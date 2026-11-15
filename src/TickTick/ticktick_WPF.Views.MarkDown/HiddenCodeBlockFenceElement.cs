using System;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class HiddenCodeBlockFenceElement : VisualLineElement
{
	private readonly double _width;

	private readonly double _height;

	public override bool IsSelectionBackgroundVisible => false;

	public override bool HandlesLineBorders => true;

	public HiddenCodeBlockFenceElement(int length, double width, double height)
		: base(1, Math.Max(0, length))
	{
		_width = Math.Max(0.01, width);
		_height = Math.Max(1.0, height);
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new HiddenCodeBlockFenceRun(this, base.TextRunProperties, _width, _height);
	}

	public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		int visualColumn2 = base.VisualColumn;
		int num = base.VisualColumn + base.VisualLength;
		if (direction == LogicalDirection.Backward)
		{
			if (visualColumn > num)
			{
				return num;
			}
			if (visualColumn <= visualColumn2)
			{
				return -1;
			}
			return visualColumn2;
		}
		if (visualColumn < visualColumn2)
		{
			return visualColumn2;
		}
		if (visualColumn >= num)
		{
			return -1;
		}
		return num;
	}

	public override bool IsWhitespace(int visualColumn)
	{
		return true;
	}
}
