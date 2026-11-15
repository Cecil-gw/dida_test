using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Print;

public class DayTimePointPrintPanel : UserControl, IComponentConnector
{
	internal Grid Container;

	internal Grid TopGrid;

	internal TextBlock TopEnd;

	internal StackPanel PointLinePanel;

	internal Grid ItemsPanel;

	internal Grid BotGrid;

	internal TextBlock BotStart;

	private bool _contentLoaded;

	public DayTimePointPrintPanel()
	{
		InitializeComponent();
	}

	public DayTimePointPrintPanel(WeekDayCellViewModel model)
	{
		InitializeComponent();
		if (model != null)
		{
			if (!CalendarGeoHelper.TopFolded)
			{
				TopGrid.Visibility = Visibility.Collapsed;
				BotGrid.Visibility = Visibility.Collapsed;
				AddLineChildren(0, 24);
			}
			else
			{
				AddLineChildren(CalendarGeoHelper.GetStartHour(), CalendarGeoHelper.GetEndHour());
				TopEnd.Text = ((CalendarGeoHelper.GetStartHour() < 10) ? "0" : "") + CalendarGeoHelper.GetStartHour() + ":00";
				BotStart.Text = ((CalendarGeoHelper.GetEndHour() < 10) ? "0" : "") + CalendarGeoHelper.GetEndHour() + ":00";
				SetTopAndBotTask(model);
			}
			SetTaskModels(model);
		}
	}

	private void SetTopAndBotTask(WeekDayCellViewModel model)
	{
		string text = model.TopTasks?.Aggregate("", (string current, CalendarDisplayModel topTask) => current + topTask.DisplayStartDate?.ToString("HH:mm") + " " + topTask.Title + "\r\n");
		int? num = text?.LastIndexOf("\r\n", StringComparison.Ordinal);
		if (num > 0)
		{
			text = text.Remove(num.Value);
		}
		TextBlock textBlock = new TextBlock
		{
			Text = text,
			FontSize = 20.0,
			LineHeight = 30.0,
			Foreground = ThemeUtil.GetAlphaColor("#191919", 85),
			Padding = new Thickness(5.0, 10.0, 5.0, 10.0),
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		textBlock.SetValue(Grid.ColumnProperty, 1);
		TopGrid.Children.Add(textBlock);
		string text2 = model.BotTasks?.Aggregate("", (string current, CalendarDisplayModel topTask) => current + topTask.DisplayStartDate?.ToString("HH:mm") + " " + topTask.Title + "\r\n");
		int? num2 = text2?.LastIndexOf("\r\n", StringComparison.Ordinal);
		if (num2 > 0)
		{
			text2 = text2.Remove(num2.Value);
		}
		TextBlock textBlock2 = new TextBlock
		{
			Text = text2,
			FontSize = 20.0,
			LineHeight = 30.0,
			Foreground = ThemeUtil.GetAlphaColor("#191919", 85),
			Padding = new Thickness(5.0, 10.0, 5.0, 10.0),
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		textBlock2.SetValue(Grid.ColumnProperty, 1);
		BotGrid.Children.Add(textBlock2);
	}

	private void SetTaskModels(WeekDayCellViewModel model)
	{
		if (model.Cells == null || !model.Cells.Any())
		{
			return;
		}
		foreach (TaskCellViewModel cell in model.Cells)
		{
			double calendarHourHeight = LocalSettings.Settings.CalendarHourHeight;
			double num = cell.Height / calendarHourHeight * 120.0;
			CalendarPrintCell element = new CalendarPrintCell
			{
				Margin = new Thickness(6.0 + cell.HorizontalOffset, 6.0 + 120.0 / calendarHourHeight * cell.VerticalOffset - (double)(CalendarGeoHelper.TopFolded ? 120 : 0), 0.0, 0.0),
				Height = ((num >= 10.0) ? (num - 10.0) : 0.0),
				DataContext = cell,
				Width = ((cell.Width >= 6.0) ? (cell.Width - 6.0) : 0.0),
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			ItemsPanel.Children.Add(element);
		}
	}

	private void AddLineChildren(int start, int end)
	{
		for (int i = start + 1; i <= end; i++)
		{
			PointLinePanel.Children.Add(new TimePointLine(i + ":00", i != end));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/daytimepointprintpanel.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Container = (Grid)target;
			break;
		case 2:
			TopGrid = (Grid)target;
			break;
		case 3:
			TopEnd = (TextBlock)target;
			break;
		case 4:
			PointLinePanel = (StackPanel)target;
			break;
		case 5:
			ItemsPanel = (Grid)target;
			break;
		case 6:
			BotGrid = (Grid)target;
			break;
		case 7:
			BotStart = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
