using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar.Month;

namespace ticktick_WPF.Views.Print;

public class MultiDayAllDayPrintRowView : Grid
{
	private int _days;

	public MultiDayAllDayPrintRowView(int days, bool showAllDayText)
	{
		base.Height = 40.0;
		_days = days;
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(48.0)
		});
		SolidColorBrush colorInString = ThemeUtil.GetColorInString("#89191919");
		for (int i = 0; i < days; i++)
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
				Stroke = colorInString,
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
			Stroke = colorInString
		};
		line2.SetValue(Grid.ColumnProperty, days);
		base.Children.Add(line2);
		if (showAllDayText)
		{
			TextBlock element = new TextBlock
			{
				FontSize = 16.0,
				Margin = new Thickness(0.0, 0.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				Text = Utils.GetString("AllDay"),
				Foreground = ThemeUtil.GetColorInString("#E8191919")
			};
			base.Children.Add(element);
		}
	}

	public void SetData(IEnumerable<WeekEventModel> rowData, bool showBottom, double topMargin = 0.0)
	{
		foreach (WeekEventModel rowDatum in rowData)
		{
			if (rowDatum.Column < 0)
			{
				rowDatum.ColumnSpan += rowDatum.Column;
				rowDatum.Column = 0;
			}
			if (rowDatum.Column + rowDatum.ColumnSpan > _days)
			{
				rowDatum.ColumnSpan = _days - rowDatum.Column;
			}
			if (rowDatum.ColumnSpan > 0 && rowDatum.Column < _days)
			{
				CalendarPrintCell calendarPrintCell = new CalendarPrintCell
				{
					Margin = new Thickness(6.0, 3.0 + topMargin, 6.0, 3.0)
				};
				calendarPrintCell.SetValue(Grid.ColumnProperty, rowDatum.Column + 1);
				calendarPrintCell.SetValue(Grid.ColumnSpanProperty, rowDatum.ColumnSpan);
				calendarPrintCell.Height = 25.0;
				base.Height = (rowDatum.Data.IsLoadMore ? 32.0 : (32.0 + topMargin));
				calendarPrintCell.DataContext = rowDatum.Data;
				base.Children.Add(calendarPrintCell);
			}
		}
		if (showBottom)
		{
			base.Height += 3.0;
		}
	}
}
