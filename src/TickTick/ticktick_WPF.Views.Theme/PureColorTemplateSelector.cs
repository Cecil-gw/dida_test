using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Theme;

internal class PureColorTemplateSelector : DataTemplateSelector
{
	public DataTemplate NormalTemplate { get; set; }

	public DataTemplate DarkItemTemplate { get; set; }

	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item == null)
		{
			return NormalTemplate;
		}
		if (item is ColorThemeViewModel { IsDark: not false })
		{
			return DarkItemTemplate;
		}
		return NormalTemplate;
	}
}
