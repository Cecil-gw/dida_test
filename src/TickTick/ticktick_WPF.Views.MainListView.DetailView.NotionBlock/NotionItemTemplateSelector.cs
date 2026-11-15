using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.MainListView.DetailView.NotionBlock;

public class NotionItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is TaskNotionBlockItemViewModel taskNotionBlockItemViewModel && container is FrameworkElement frameworkElement)
		{
			if (taskNotionBlockItemViewModel.type == "rich_text")
			{
				return frameworkElement.FindResource("NotionTextItem") as DataTemplate;
			}
			return frameworkElement.FindResource("NotionNormalItem") as DataTemplate;
		}
		return null;
	}
}
