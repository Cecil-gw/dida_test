using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusStatisticsPanelItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is FocusStatisticsPanelItemViewModel focusStatisticsPanelItemViewModel && container is FrameworkElement frameworkElement)
		{
			if (focusStatisticsPanelItemViewModel.IsTitle)
			{
				return frameworkElement.FindResource("FocusStatisticsTitleTemplate") as DataTemplate;
			}
			if (focusStatisticsPanelItemViewModel.IsOverview)
			{
				return frameworkElement.FindResource("FocusStatisticsTemplate") as DataTemplate;
			}
			if (focusStatisticsPanelItemViewModel.IsRecord)
			{
				return frameworkElement.FindResource("FocusRecordTemplate") as DataTemplate;
			}
			if (focusStatisticsPanelItemViewModel.IsLoadMore)
			{
				return frameworkElement.FindResource("FocusLoadMoreTemplate") as DataTemplate;
			}
			if (focusStatisticsPanelItemViewModel.IsEmpty)
			{
				return frameworkElement.FindResource("FocusRecordEmptyTemplate") as DataTemplate;
			}
		}
		return null;
	}
}
