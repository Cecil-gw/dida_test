using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public class WidgetListItemSelector : DataTemplateSelector
{
	private readonly ProjectWidget _parent;

	public WidgetListItemSelector(ProjectWidget parent)
	{
		_parent = parent;
	}

	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is DisplayItemModel displayItemModel && container is FrameworkElement frameworkElement)
		{
			string resourceKey = ((_parent.ViewThemeId == "light") ? "LightTaskItemTemplate" : "DarkTaskItemTemplate");
			string resourceKey2 = ((_parent.ViewThemeId == "light") ? "LightSectionTemplate" : "DarkSectionTemplate");
			if (displayItemModel.IsTask || displayItemModel.IsItem)
			{
				return frameworkElement.FindResource(resourceKey) as DataTemplate;
			}
			if (displayItemModel.IsSection)
			{
				return frameworkElement.FindResource(resourceKey2) as DataTemplate;
			}
			return frameworkElement.FindResource(resourceKey) as DataTemplate;
		}
		return null;
	}
}
