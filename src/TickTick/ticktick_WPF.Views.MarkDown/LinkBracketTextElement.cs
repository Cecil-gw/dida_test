using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public sealed class LinkBracketTextElement : VisualLineText
{
	private ILinkTextEditor _editor;

	public LinkBracketTextElement(VisualLine parentVisualLine, ILinkTextEditor editor, int length = 1)
		: base(parentVisualLine, length)
	{
		_editor = editor;
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		base.TextRunProperties.SetForegroundBrush(_editor.GetBracketColor());
		return base.CreateTextRun(startVisualColumn, context);
	}
}
