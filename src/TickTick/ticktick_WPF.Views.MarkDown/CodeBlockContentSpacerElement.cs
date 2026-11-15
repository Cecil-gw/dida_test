using System;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

internal sealed class CodeBlockContentSpacerElement : VisualLineElement
{
	private readonly TextLine _metricsLine;

	private readonly double _paddingWidth;

	private readonly double _rightPaddingWidth;

	public override bool HandlesLineBorders => true;

	public CodeBlockContentSpacerElement(TextLine metricsLine, double paddingWidth, double rightPaddingWidth)
		: base(1, 0)
	{
		_metricsLine = metricsLine;
		_paddingWidth = paddingWidth;
		_rightPaddingWidth = Math.Max(0.0, rightPaddingWidth);
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new CodeBlockContentSpacerRun(_metricsLine, _paddingWidth, base.TextRunProperties);
	}

	public override double GetWordWrapIndentation()
	{
		return _paddingWidth;
	}

	public override double GetWordWrapRightPadding()
	{
		return _rightPaddingWidth;
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
}
