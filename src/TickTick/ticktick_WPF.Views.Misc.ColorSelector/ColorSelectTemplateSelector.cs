using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Misc.ColorSelector;

internal class ColorSelectTemplateSelector : DataTemplateSelector
{
	public DataTemplate NormalTemplate { get; set; }

	public DataTemplate CustomItemTemplate { get; set; }

	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item == null)
		{
			return NormalTemplate;
		}
		if (item is ItemColorViewModel { IsAddCustom: not false })
		{
			return CustomItemTemplate;
		}
		return NormalTemplate;
	}
}
