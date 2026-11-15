using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class RecordLoadMoreItem : Border
{
	public RecordLoadMoreItem()
	{
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.Background = Brushes.Transparent;
		base.DataContextChanged += OnDataChanged;
		base.Margin = new Thickness(20.0, 0.0, 0.0, 0.0);
	}

	private void OnDataChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is FocusRecordLoadMoreViewModel focusRecordLoadMoreViewModel)
		{
			if (focusRecordLoadMoreViewModel.NeedClick)
			{
				TextBlock textBlock = new TextBlock
				{
					Text = Utils.GetString("LoadMore"),
					FontSize = 12.0
				};
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
				Child = textBlock;
				base.MouseLeftButtonUp += OnLoadMoreClock;
			}
			else
			{
				LoadingIndicator loadingIndicator = new LoadingIndicator
				{
					IsActive = true,
					SpeedRatio = 3.0
				};
				loadingIndicator.SetResourceReference(FrameworkElement.StyleProperty, "LoadingIndicatorRingStyle");
				Child = loadingIndicator;
			}
		}
	}

	private void OnLoadMoreClock(object sender, MouseButtonEventArgs e)
	{
		LoadingIndicator loadingIndicator = new LoadingIndicator
		{
			IsActive = true,
			SpeedRatio = 3.0
		};
		loadingIndicator.SetResourceReference(FrameworkElement.StyleProperty, "LoadingIndicatorRingStyle");
		Child = loadingIndicator;
		LoadMore();
	}

	private async void LoadMore()
	{
		Utils.FindParent<FocusStatisticsPanel>(this)?.LoadMore();
		Utils.FindParent<BatchFocusRecWindow>(this)?.LoadMore();
	}
}
