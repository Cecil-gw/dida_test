using ICSharpCode.AvalonEdit.Rendering;

namespace ticktick_WPF.Views.MarkDown;

public class HighlightExMarkElementGenerator : VisualLineElementGenerator
{
	private readonly MarkDownEditor _editor;

	public HighlightExMarkElementGenerator(MarkDownEditor editor)
	{
		_editor = editor;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		return -1;
	}

	public override VisualLineElement ConstructElement(int offset)
	{
		return null;
	}
}
