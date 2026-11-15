using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class ConditionItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is FilterListItemViewModel filterListItemViewModel && container is FrameworkElement frameworkElement)
		{
			if (filterListItemViewModel.IsSplit)
			{
				return frameworkElement.FindResource("SplitTemplate") as DataTemplate;
			}
			if (filterListItemViewModel.DisplayType == FilterItemDisplayType.Span)
			{
				return frameworkElement.FindResource("SpanDateItemTemplate") as DataTemplate;
			}
			if (filterListItemViewModel.DisplayType != FilterItemDisplayType.Normal)
			{
				return frameworkElement.FindResource("DateItemTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("NormalItemTemplate") as DataTemplate;
		}
		return null;
	}
}
