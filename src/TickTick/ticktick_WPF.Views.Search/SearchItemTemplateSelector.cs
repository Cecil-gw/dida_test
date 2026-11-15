using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Search;

public class SearchItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (container is FrameworkElement frameworkElement)
		{
			if (item is SearchTaskItemViewModel)
			{
				return frameworkElement.FindResource("SearchTaskItemTemplate") as DataTemplate;
			}
			if (item is SearchTagAndProjectModel)
			{
				return frameworkElement.FindResource("SearchProjectDataTemplate") as DataTemplate;
			}
			if (item is SearchLoadMoreModel)
			{
				return frameworkElement.FindResource("SearchLoadMoreTemplate") as DataTemplate;
			}
			if (item is SearchTitleModel)
			{
				return frameworkElement.FindResource("SearchTitleTemplate") as DataTemplate;
			}
			if (item is SearchSplitModel)
			{
				return frameworkElement.FindResource("SearchSplitTemplate") as DataTemplate;
			}
			if (item is SearchProjectTemplateModel)
			{
				return frameworkElement.FindResource("SearchProjectTemplateItemTemplate") as DataTemplate;
			}
		}
		return null;
	}
}
