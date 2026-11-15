using System.Windows.Documents;

namespace ticktick_WPF.Util.DateParser;

public class HighlightIndexModel
{
	public int start { get; set; }

	public int end { get; set; }

	public int length { get; set; }

	public string text { get; set; }

	public TextPointer endPointer { get; set; }

	public TextPointer startPointer { get; set; }

	public int type { get; set; }
}
