using System.Windows;
using System.Windows.Controls;
using Emoji.Wpf;

namespace ticktick_WPF.Views.MainListView.DetailView.NotionBlock;

public class TaskNotionTextItem : StackPanel
{
	private readonly EmjTextBlock _keyText;

	private readonly EmjTextBlock _text;

	public TaskNotionTextItem()
	{
		base.Margin = new Thickness(0.0, 10.0, 0.0, 10.0);
		_keyText = new EmjTextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			LineHeight = 20.0,
			FontSize = 14.0,
			Margin = new Thickness(16.0, 0.0, 16.0, 0.0),
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		_keyText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		_keyText.SetBinding(EmjTextBlock.TextProperty, "key");
		base.Children.Add(_keyText);
		_text = new EmjTextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			LineHeight = 20.0,
			FontSize = 13.0,
			Margin = new Thickness(16.0, 6.0, 16.0, 0.0),
			MaxHeight = 66.0,
			TextTrimming = TextTrimming.WordEllipsis,
			TextWrapping = TextWrapping.Wrap
		};
		_text.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity60");
		_text.SetBinding(EmjTextBlock.TextProperty, "text");
		base.Children.Add(_text);
	}
}
