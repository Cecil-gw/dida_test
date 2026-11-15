using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Calendar.Week;

namespace ticktick_WPF.Views.Print;

public class WeekTimelinePrintView : Grid
{
	private int _days;

	private SolidColorBrush _lineColor;

	public WeekTimelinePrintView(List<CalendarTimelineDayViewModel> pointModels, double height)
	{
		base.Height = height;
		if (pointModels != null && pointModels.Count != 0)
		{
			_days = pointModels.Count;
			_lineColor = ThemeUtil.GetColorInString("#89191919");
			InitColumns();
			SetTopAndBotTask(pointModels);
			if (pointModels.Any())
			{
				SetTaskModels(pointModels);
			}
		}
	}

	private void InitColumns()
	{
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(48.0)
		});
		for (int i = 0; i < _days; i++)
		{
			base.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			Line line = new Line
			{
				Y1 = 0.0,
				Y2 = 1.0,
				HorizontalAlignment = HorizontalAlignment.Right,
				Stretch = Stretch.Fill,
				StrokeThickness = 1.0,
				Stroke = _lineColor,
				Margin = new Thickness(0.0, 0.0, 0.0, -1.0)
			};
			line.SetValue(Grid.ColumnProperty, i);
			base.Children.Add(line);
		}
		Line line2 = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 0.0, 0.0, -1.0),
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			Stroke = _lineColor
		};
		line2.SetValue(Grid.ColumnProperty, _days);
		base.Children.Add(line2);
		GetTextBlock("00:00").Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		TextBlock textBlock = GetTextBlock("24:00");
		textBlock.Margin = new Thickness(0.0, base.Height - 20.0, 0.0, 0.0);
		if (!CalendarGeoHelper.TopFolded)
		{
			AddLineChildren(25);
			AddHourText(0, 24);
			return;
		}
		AddLineChildren(CalendarGeoHelper.GetEndHour() - CalendarGeoHelper.GetStartHour() + 3);
		AddHourText(CalendarGeoHelper.GetStartHour(), CalendarGeoHelper.GetEndHour() + ((CalendarGeoHelper.GetEndHour() == 24) ? (-1) : 0), 1);
		GetTextBlock("-" + ((CalendarGeoHelper.GetStartHour() < 10) ? "0" : "") + CalendarGeoHelper.GetStartHour() + ":00").Margin = new Thickness(0.0, 100.0, 0.0, 0.0);
		GetTextBlock(((CalendarGeoHelper.GetEndHour() < 10) ? "0" : "") + CalendarGeoHelper.GetEndHour() + ":00").Margin = new Thickness(0.0, base.Height - 100.0, 0.0, 0.0);
		textBlock.Text = "-24:00";
	}

	private void AddHourText(int start, int end, int startIndex = 0)
	{
		for (int i = start + 1; i < end; i++)
		{
			string text = ((i > 9) ? "" : "0") + i + ":00";
			GetTextBlock(text).Margin = new Thickness(0.0, 120 * (i - start + startIndex) - 8, 0.0, 0.0);
		}
	}

	private TextBlock GetTextBlock(string text)
	{
		TextBlock textBlock = new TextBlock
		{
			FontSize = 16.0,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Center,
			Text = text,
			Foreground = ThemeUtil.GetColorInString("#E8191919")
		};
		base.Children.Add(textBlock);
		return textBlock;
	}

	private void AddLineChildren(int count)
	{
		Line line = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			VerticalAlignment = VerticalAlignment.Top,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			Stroke = _lineColor
		};
		line.SetValue(Grid.ColumnProperty, 0);
		line.SetValue(Grid.ColumnSpanProperty, _days + 1);
		base.Children.Add(line);
		for (int i = 1; i <= count; i++)
		{
			Line line2 = new Line
			{
				X1 = 0.0,
				X2 = 1.0,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 120 * i - 1, 0.0, 0.0),
				Stretch = Stretch.Fill,
				StrokeThickness = 1.0,
				Stroke = _lineColor
			};
			line2.SetValue(Grid.ColumnProperty, 1);
			line2.SetValue(Grid.ColumnSpanProperty, _days);
			base.Children.Add(line2);
		}
	}

	private void SetTopAndBotTask(List<CalendarTimelineDayViewModel> pointModels)
	{
		for (int i = 0; i < pointModels.Count; i++)
		{
			string text = pointModels[i].TopTasks?.Aggregate("", (string current, CalendarDisplayModel topTask) => current + topTask.DisplayStartDate?.ToString("HH:mm") + " " + topTask.Title + "\r\n");
			int? num = text?.LastIndexOf("\r\n", StringComparison.Ordinal);
			if (num > 0)
			{
				text = text.Remove(num.Value);
			}
			TextBlock textBlock = new TextBlock
			{
				Text = text,
				FontSize = 18.0,
				LineHeight = 30.0,
				Foreground = ThemeUtil.GetAlphaColor("#191919", 85),
				Padding = new Thickness(5.0, 10.0, 5.0, 10.0),
				VerticalAlignment = VerticalAlignment.Top,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			textBlock.SetValue(Grid.ColumnProperty, i + 1);
			base.Children.Add(textBlock);
			string text2 = pointModels[i].BotTasks?.Aggregate("", (string current, CalendarDisplayModel topTask) => current + topTask.DisplayStartDate?.ToString("HH:mm") + " " + topTask.Title + "\r\n");
			int? num2 = text2?.LastIndexOf("\r\n", StringComparison.Ordinal);
			if (num2 > 0)
			{
				text2 = text2.Remove(num2.Value);
			}
			TextBlock textBlock2 = new TextBlock
			{
				Text = text2,
				FontSize = 18.0,
				LineHeight = 30.0,
				Foreground = ThemeUtil.GetAlphaColor("#191919", 85),
				Padding = new Thickness(5.0, base.Height - 110.0, 5.0, 10.0),
				VerticalAlignment = VerticalAlignment.Top,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			textBlock2.SetValue(Grid.ColumnProperty, i + 1);
			base.Children.Add(textBlock2);
		}
	}

	private void SetTaskModels(List<CalendarTimelineDayViewModel> models)
	{
		for (int i = 0; i < models.Count; i++)
		{
			if (models[i].Cells == null || !models[i].Cells.Any())
			{
				continue;
			}
			foreach (TaskCellViewModel cell in models[i].Cells)
			{
				double calendarHourHeight = LocalSettings.Settings.CalendarHourHeight;
				double num = cell.Height / calendarHourHeight * 120.0;
				CalendarPrintCell calendarPrintCell = new CalendarPrintCell
				{
					Margin = new Thickness(6.0 + cell.HorizontalOffset, (double)(CalendarGeoHelper.TopFolded ? 124 : 4) + 120.0 / calendarHourHeight * (cell.VerticalOffset - (double)(CalendarGeoHelper.TopFolded ? 32 : 0)), 0.0, 0.0),
					Height = ((num >= 10.0) ? (num - 10.0) : 0.0),
					DataContext = cell,
					Width = ((cell.Width >= 6.0) ? (cell.Width - 6.0) : 0.0),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left
				};
				calendarPrintCell.SetValue(Grid.ColumnProperty, i + 1);
				base.Children.Add(calendarPrintCell);
			}
		}
	}
}
