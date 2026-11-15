using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.CustomControl;

public class MenuItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is CustomMenuItemViewModel customMenuItemViewModel && container is FrameworkElement frameworkElement)
		{
			if (customMenuItemViewModel.IsMessage)
			{
				return frameworkElement.FindResource("MessageItem") as DataTemplate;
			}
			if (customMenuItemViewModel.IsStandaloneContent)
			{
				return frameworkElement.FindResource("StandaloneContentItem") as DataTemplate;
			}
			if (customMenuItemViewModel.IsImage)
			{
				return frameworkElement.FindResource("ImageItem") as DataTemplate;
			}
			if (customMenuItemViewModel.IsSplit)
			{
				return frameworkElement.FindResource("SplitLineItem") as DataTemplate;
			}
			if (customMenuItemViewModel.IsCenterText)
			{
				return frameworkElement.FindResource("TextCenterItem") as DataTemplate;
			}
			return frameworkElement.FindResource("IconItem") as DataTemplate;
		}
		return null;
	}
}
