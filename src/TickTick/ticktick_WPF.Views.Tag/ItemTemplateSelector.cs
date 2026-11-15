using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Tag;

public class ItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is TagLabelViewModel tagLabelViewModel && container is FrameworkElement frameworkElement)
		{
			if (tagLabelViewModel.IsAdd)
			{
				return frameworkElement.FindResource("TagAddTemplate") as DataTemplate;
			}
			return frameworkElement.FindResource("TagLabelTemplate") as DataTemplate;
		}
		return null;
	}
}
