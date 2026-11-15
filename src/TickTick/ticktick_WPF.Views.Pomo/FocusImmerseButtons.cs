using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class FocusImmerseButtons : FocusOptionButtons
{
	public FocusImmerseButtons()
	{
		base.Orientation = Orientation.Horizontal;
	}

	protected override void AddButton(string option, string textKey, bool isFill = false, bool isRest = false)
	{
		if (!(option == "Exit"))
		{
			Button button = new Button
			{
				Margin = new Thickness(6.0, 0.0, 6.0, 0.0),
				Tag = option,
				Content = Utils.GetString(textKey),
				Height = 48.0,
				Width = 148.0
			};
			button.SetResourceReference(FrameworkElement.StyleProperty, "FocusOptionButtonStyle");
			if (!isFill)
			{
				button.Background = ThemeUtil.GetColorInString("#1F1F1F");
			}
			else if (isRest)
			{
				button.SetResourceReference(Control.BackgroundProperty, "PomoGreen");
			}
			button.Click += base.OnOptionClick;
			base.Children.Add(button);
		}
	}

	protected override async void AddTodayStatistic()
	{
	}
}
