using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Views.Pomo.FocusStatistics;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerDetailItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is FocusStatisticsPanelItemViewModel focusStatisticsPanelItemViewModel && container is FrameworkElement frameworkElement)
		{
			if (focusStatisticsPanelItemViewModel.IsRecord)
			{
				return frameworkElement.FindResource("FocusRecordTemplate") as DataTemplate;
			}
			if (focusStatisticsPanelItemViewModel.IsLoadMore)
			{
				return frameworkElement.FindResource("FocusLoadMoreTemplate") as DataTemplate;
			}
			if (focusStatisticsPanelItemViewModel is TimerDetailItemViewModel timerDetailItemViewModel)
			{
				if (timerDetailItemViewModel.TimerTitle)
				{
					return frameworkElement.FindResource("TimerDetailTitleItemTemplate") as DataTemplate;
				}
				if (timerDetailItemViewModel.TimerTimeline)
				{
					return frameworkElement.FindResource("TimerStatisticsTimelineTemplate") as DataTemplate;
				}
				if (timerDetailItemViewModel.TimerOverview)
				{
					return frameworkElement.FindResource("TimerStatisticsOverviewTemplate") as DataTemplate;
				}
			}
		}
		return null;
	}
}
