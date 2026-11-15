using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Project;

public class SelectInviteUserTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is InviteSelectViewModel inviteSelectViewModel && container is FrameworkElement frameworkElement)
		{
			if (inviteSelectViewModel.IsInput)
			{
				return frameworkElement.FindResource("InputTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("SelectedUserTemplate") as DataTemplate;
		}
		return null;
	}
}
