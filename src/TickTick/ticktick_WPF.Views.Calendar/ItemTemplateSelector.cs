using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class ItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is CalendarDisplayViewModel calendarDisplayViewModel && container is FrameworkElement frameworkElement)
		{
			if (calendarDisplayViewModel.Type == DisplayType.Section)
			{
				return frameworkElement.FindResource("SectionTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("TaskTemplate") as DataTemplate;
		}
		return null;
	}
}
