using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class HabitListItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (container is FrameworkElement frameworkElement)
		{
			try
			{
				if (item is HabitItemViewModel)
				{
					return frameworkElement.FindResource("HabitListItemTemplate") as DataTemplate;
				}
				if (item is HabitSectionListViewModel)
				{
					return frameworkElement.FindResource("HabitListSectionTemplate") as DataTemplate;
				}
			}
			catch
			{
				return null;
			}
		}
		return null;
	}
}
