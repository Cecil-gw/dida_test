using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Views.CountDown.ViewModels;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountdownCardTemplateSelector : DataTemplateSelector
{
	public DataTemplate DefaultTemplate { get; set; }

	public DataTemplate ImageBackgroundTemplate { get; set; }

	public DataTemplate CalendarTemplate { get; set; }

	public DataTemplate DopamineTemplate { get; set; }

	public DataTemplate TwoColorCardTemplate { get; set; }

	public DataTemplate CartoonTemplate { get; set; }

	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is CountDownViewModel { StyleType: var styleType })
		{
			switch (styleType)
			{
			case "polaroid":
			case "fullscreen_image":
				return ImageBackgroundTemplate;
			case "perpetual":
				return CalendarTemplate;
			case "dopamine":
				return DopamineTemplate;
			case "two_color_card":
				return TwoColorCardTemplate;
			case "cartoon":
				return CartoonTemplate;
			case "calendar":
			case "normal":
				return DefaultTemplate;
			default:
				return DefaultTemplate;
			}
		}
		return base.SelectTemplate(item, container);
	}
}
