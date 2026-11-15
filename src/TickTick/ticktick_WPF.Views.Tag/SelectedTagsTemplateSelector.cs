using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Tag;

public class SelectedTagsTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is SelectedTagViewModel selectedTagViewModel && container is FrameworkElement frameworkElement)
		{
			if (selectedTagViewModel.IsAdd)
			{
				return frameworkElement.FindResource("InputTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("SelectedTagTemplate") as DataTemplate;
		}
		return null;
	}
}
