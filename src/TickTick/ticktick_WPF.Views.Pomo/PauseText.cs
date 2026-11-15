using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ticktick_WPF.Views.Pomo;

public sealed class PauseText : Border
{
	private TextBlock _text;

	public string Text => _text.Text;

	public PauseText()
	{
		SetResourceReference(Border.BackgroundProperty, "ToolTipColor");
		base.Height = 20.0;
		base.CornerRadius = new CornerRadius(2.0);
		_text = new TextBlock
		{
			FontSize = 11.0,
			Foreground = Brushes.White,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
		};
		Child = _text;
	}

	public void SetText(string text)
	{
		_text.Text = text;
	}
}
