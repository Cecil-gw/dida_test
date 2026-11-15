using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusStatisticsOverview : Grid
{
	private TextBlock _todayPomo;

	private TextBlock _totalPomo;

	private TextBlock _todayDuration;

	private TextBlock _totalDuration;

	public FocusStatisticsOverview()
	{
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		InitTodayPomos();
		InitTotalPomos();
		InitTodayFocusDuration();
		InitTotalFocusDuration();
		base.Margin = new Thickness(22.0, 20.0, 22.0, 40.0);
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is FocusStatisticsOverviewItem focusStatisticsOverviewItem)
		{
			_todayPomo.Text = focusStatisticsOverviewItem.TodayPomos.ToString() ?? "";
			_totalPomo.Text = focusStatisticsOverviewItem.TotalPomos.ToString() ?? "";
			long num = focusStatisticsOverviewItem.TodayDuration / 60;
			long num2 = focusStatisticsOverviewItem.TodayDuration % 60;
			_todayDuration.Inlines.Clear();
			if (num > 0)
			{
				_todayDuration.Inlines.Add(new Run(num.ToString() ?? ""));
				_todayDuration.Inlines.Add(new Run(" h ")
				{
					FontSize = 14.0
				});
			}
			_todayDuration.Inlines.Add(new Run(num2.ToString() ?? ""));
			_todayDuration.Inlines.Add(new Run(" m")
			{
				FontSize = 14.0
			});
			long num3 = focusStatisticsOverviewItem.TotalDuration / 60;
			long num4 = focusStatisticsOverviewItem.TotalDuration % 60;
			_totalDuration.Inlines.Clear();
			if (num3 > 0)
			{
				_totalDuration.Inlines.Add(new Run(num3.ToString() ?? ""));
				_totalDuration.Inlines.Add(new Run(" h ")
				{
					FontSize = 14.0
				});
			}
			_totalDuration.Inlines.Add(new Run(num4.ToString() ?? ""));
			_totalDuration.Inlines.Add(new Run(" m")
			{
				FontSize = 14.0
			});
		}
	}

	private void InitTodayPomos()
	{
		StackPanel container = GetContainer(0, 0);
		TextBlock textBlock = GetTextBlock(12.0, "BaseColorOpacity60");
		textBlock.Text = Utils.GetString("PomoToday");
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		container.Children.Add(textBlock);
		_todayPomo = GetTextBlock(24.0, "BaseColorOpacity90");
		container.Children.Add(_todayPomo);
	}

	private void InitTotalPomos()
	{
		StackPanel container = GetContainer(0, 1);
		TextBlock textBlock = GetTextBlock(12.0, "BaseColorOpacity60");
		textBlock.Text = Utils.GetString("TotalPomo");
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		container.Children.Add(textBlock);
		_totalPomo = GetTextBlock(24.0, "BaseColorOpacity90");
		container.Children.Add(_totalPomo);
	}

	private void InitTodayFocusDuration()
	{
		StackPanel container = GetContainer(1, 0);
		TextBlock textBlock = GetTextBlock(12.0, "BaseColorOpacity60");
		textBlock.Text = Utils.GetString("TodayFocusDuration");
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		container.Children.Add(textBlock);
		_todayDuration = GetTextBlock(24.0, "BaseColorOpacity90");
		container.Children.Add(_todayDuration);
	}

	private void InitTotalFocusDuration()
	{
		StackPanel container = GetContainer(1, 1);
		TextBlock textBlock = GetTextBlock(12.0, "BaseColorOpacity60");
		textBlock.Text = Utils.GetString("TotalFocusDuration");
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		container.Children.Add(textBlock);
		_totalDuration = GetTextBlock(24.0, "BaseColorOpacity90");
		container.Children.Add(_totalDuration);
	}

	private TextBlock GetTextBlock(double fontsize, string foregroundKey)
	{
		TextBlock textBlock = new TextBlock();
		textBlock.FontSize = fontsize;
		textBlock.TextWrapping = TextWrapping.Wrap;
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		textBlock.MaxHeight = (fontsize + 4.0) * 2.0;
		textBlock.SetBinding(FrameworkElement.ToolTipProperty, new Binding("Text")
		{
			RelativeSource = RelativeSource.Self
		});
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, foregroundKey);
		return textBlock;
	}

	private StackPanel GetContainer(int column, int row)
	{
		Border border = new Border
		{
			Height = 72.0,
			CornerRadius = new CornerRadius(6.0),
			Margin = new Thickness((column != 0) ? 6 : 0, (row != 0) ? 6 : 0, (column == 0) ? 6 : 0, (row == 0) ? 6 : 0)
		};
		border.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity3");
		border.SetValue(Grid.ColumnProperty, column);
		border.SetValue(Grid.RowProperty, row);
		StackPanel result = (StackPanel)(border.Child = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(12.0, 0.0, 12.0, 0.0)
		});
		base.Children.Add(border);
		return result;
	}
}
