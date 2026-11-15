using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views;

public class EmojiTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is EmojiSelectViewModel emojiSelectViewModel && container is FrameworkElement frameworkElement)
		{
			if (emojiSelectViewModel.IsEmojiItems)
			{
				return frameworkElement.FindResource("EmojiItem") as DataTemplate;
			}
			if (emojiSelectViewModel.IsSection)
			{
				return frameworkElement.FindResource("SectionItem") as DataTemplate;
			}
			return frameworkElement.FindResource("TextItem") as DataTemplate;
		}
		return null;
	}
}
