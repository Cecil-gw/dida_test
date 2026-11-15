using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views;

public class ReminderItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is AdvanceDateModel advanceDateModel && container is FrameworkElement frameworkElement)
		{
			if (advanceDateModel.IsSplit)
			{
				return frameworkElement.FindResource("SplitTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("ItemDataTemplate") as DataTemplate;
		}
		return null;
	}
}
