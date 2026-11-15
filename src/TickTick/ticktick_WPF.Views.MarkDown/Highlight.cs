using System.Windows;
using System.Windows.Media;

namespace ticktick_WPF.Views.MarkDown;

public class Highlight
{
	public string Name { get; set; }

	public SolidColorBrush Background { get; set; }

	public SolidColorBrush Foreground { get; set; }

	public string FontWeight { get; set; }

	public string FontStyle { get; set; }

	public bool Underline { get; set; }

	public bool Strikethrough { get; set; }

	public TextDecorationCollection TextDecorations { get; set; }

	public string FontType { get; set; }
}
