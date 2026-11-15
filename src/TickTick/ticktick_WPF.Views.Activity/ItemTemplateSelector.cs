using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Activity;

public class ItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is ProjectActivityViewModel projectActivityViewModel && container is FrameworkElement frameworkElement)
		{
			if (projectActivityViewModel.IsSection)
			{
				return frameworkElement.FindResource("DateSectionTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("ModifyItemTemplate") as DataTemplate;
		}
		return null;
	}
}
