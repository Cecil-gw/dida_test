using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Misc;

public class ItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is SelectableItemViewModel selectableItemViewModel && container is FrameworkElement frameworkElement)
		{
			if (selectableItemViewModel.IsSplit)
			{
				return frameworkElement.FindResource("SplitTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("ItemTemplate") as DataTemplate;
		}
		return null;
	}
}
