using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Agenda;

public class ItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (container is FrameworkElement frameworkElement)
		{
			if (item is UserProfileViewModel)
			{
				return frameworkElement.FindResource("AttendeeTemplate") as DataTemplate;
			}
			if (item is AttendeeExtraViewModel)
			{
				return frameworkElement.FindResource("AttendeeExtraTemplate") as DataTemplate;
			}
		}
		return null;
	}
}
