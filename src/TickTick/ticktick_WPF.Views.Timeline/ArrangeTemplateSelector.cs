using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Timeline;

public class ArrangeTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (container is FrameworkElement frameworkElement)
		{
			if (item is TimelineCellViewModel)
			{
				return frameworkElement.FindResource("CellDataTemplate") as DataTemplate;
			}
			if (item is TimelineGroupViewModel)
			{
				return frameworkElement.FindResource("GroupDataTemplate") as DataTemplate;
			}
		}
		return base.SelectTemplate(item, container);
	}
}
