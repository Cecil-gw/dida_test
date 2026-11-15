using System.Windows;
using ICSharpCode.AvalonEdit.Document;

namespace ticktick_WPF.Views.MarkDown;

public class OrderExtra
{
	public int LastIndent { get; set; }

	public int Indent { get; set; }

	public string Last { get; set; }

	public string Current { get; set; }

	public DocumentLine Line { get; set; }

	public Point Position { get; set; }
}
