using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is ProjectItemViewModel projectItemViewModel && container is FrameworkElement frameworkElement)
		{
			if (projectItemViewModel.IsPtfAll)
			{
				return frameworkElement.FindResource("SectionGroupItemTemplate") as DataTemplate;
			}
			if (projectItemViewModel.IsEmptyProject)
			{
				return frameworkElement.FindResource("EmptyProjectTemplate") as DataTemplate;
			}
			if (projectItemViewModel.IsSplitLine)
			{
				return frameworkElement.FindResource("SplitLineTemplate") as DataTemplate;
			}
			if (projectItemViewModel.IsEmptySubItem)
			{
				return frameworkElement.FindResource("EmptySubItemTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("ProjectItemTemplate") as DataTemplate;
		}
		return null;
	}
}
