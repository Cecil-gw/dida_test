using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusRecordEmptyItem : StackPanel
{
	public FocusRecordEmptyItem()
	{
		base.Margin = new Thickness(20.0);
		EmptyView emptyView = new EmptyView();
		emptyView.SetIconsSource(new EmptyIconsSourceModel("FocusTaskEmpty", new List<int> { 2, 4, 5 }));
		base.Children.Add(emptyView);
		TextBlock textBlock = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Center,
			FontSize = 14.0
		};
		textBlock.Text = Utils.GetString("FocusRecordEmpty");
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		base.Children.Add(textBlock);
	}
}
