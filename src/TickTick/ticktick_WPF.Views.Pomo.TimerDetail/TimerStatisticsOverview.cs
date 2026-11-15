using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerStatisticsOverview : Grid
{
	private TextBlock _totalDays;

	private TextBlock _todayDuration;

	private TextBlock _totalDuration;

	public TimerStatisticsOverview()
	{
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		InitTotalDays();
		InitTodayFocusDuration();
		InitTotalFocusDuration();
		base.Margin = new Thickness(22.0, 20.0, 22.0, 20.0);
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is TimerStatisticsOverviewItem timerStatisticsOverviewItem)
		{
			_totalDays.Text = timerStatisticsOverviewItem.TotalDays.ToString() ?? "";
			UiUtils.SetTimeTextRun(_todayDuration, timerStatisticsOverviewItem.TodayDuration, 14, "BaseColorOpacity80");
			UiUtils.SetTimeTextRun(_totalDuration, timerStatisticsOverviewItem.TotalDuration, 14, "BaseColorOpacity80");
		}
	}

	private void InitTotalDays()
	{
		StackPanel container = GetContainer(0);
		TextBlock textBlock = GetTextBlock(12.0, "BaseColorOpacity60");
		textBlock.Text = Utils.GetString("FocusedDays");
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		container.Children.Add(textBlock);
		_totalDays = GetTextBlock(24.0, "BaseColorOpacity90");
		container.Children.Add(_totalDays);
	}

	private void InitTodayFocusDuration()
	{
		StackPanel container = GetContainer(1);
		TextBlock textBlock = GetTextBlock(12.0, "BaseColorOpacity60");
		textBlock.Text = Utils.GetString("TodayFocus");
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		container.Children.Add(textBlock);
		_todayDuration = GetTextBlock(24.0, "BaseColorOpacity90");
		container.Children.Add(_todayDuration);
	}

	private void InitTotalFocusDuration()
	{
		StackPanel container = GetContainer(2);
		TextBlock textBlock = GetTextBlock(12.0, "BaseColorOpacity60");
		textBlock.Text = Utils.GetString("TotalFocus");
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		container.Children.Add(textBlock);
		_totalDuration = GetTextBlock(24.0, "BaseColorOpacity90");
		container.Children.Add(_totalDuration);
	}

	private TextBlock GetTextBlock(double fontsize, string foregroundKey)
	{
		TextBlock textBlock = new TextBlock();
		textBlock.FontSize = fontsize;
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, foregroundKey);
		return textBlock;
	}

	private StackPanel GetContainer(int column)
	{
		Border border = new Border
		{
			Height = 72.0,
			CornerRadius = new CornerRadius(6.0),
			Margin = new Thickness(0.0, 0.0, (column != 2) ? 12 : 0, 0.0)
		};
		border.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity3");
		border.SetValue(Grid.ColumnProperty, column);
		StackPanel result = (StackPanel)(border.Child = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(12.0, 0.0, 12.0, 0.0)
		});
		base.Children.Add(border);
		return result;
	}
}
