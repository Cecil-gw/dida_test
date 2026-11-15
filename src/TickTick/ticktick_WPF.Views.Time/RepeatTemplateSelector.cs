using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Time;

public class RepeatTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is RepeatViewModel repeatViewModel && container is FrameworkElement frameworkElement)
		{
			if (repeatViewModel.IsSplit)
			{
				return frameworkElement.FindResource("SplitTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("ItemDataTemplate") as DataTemplate;
		}
		return null;
	}
}
