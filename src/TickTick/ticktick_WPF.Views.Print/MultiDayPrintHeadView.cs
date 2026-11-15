using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Print;

public class MultiDayPrintHeadView : Grid
{
	public MultiDayPrintHeadView(DateTime date, int days, int month, bool showWeekends)
	{
		base.Height = 80.0;
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(48.0)
		});
		SolidColorBrush colorInString = ThemeUtil.GetColorInString("#89191919");
		SolidColorBrush colorInString2 = ThemeUtil.GetColorInString("#E8191919");
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
				Margin = new Thickness(0.0, 50.0, 0.0, -1.0),
				Stretch = Stretch.Fill,
				StrokeThickness = 1.0,
				Stroke = colorInString
			};
			line.SetValue(Grid.ColumnProperty, i);
			base.Children.Add(line);
			while (!showWeekends && TickTickUtils.DateUtils.IsWeekends(date))
			{
				date = date.AddDays(1.0);
			}
			TextBlock textBlock = new TextBlock
			{
				FontSize = 16.0,
				Margin = new Thickness(10.0, 10.0, 10.0, 0.0),
				VerticalAlignment = VerticalAlignment.Top,
				Text = date.ToString("ddd", App.Ci),
				Foreground = colorInString,
				Opacity = (TickTickUtils.DateUtils.IsWeekends(date) ? 0.6 : 1.0)
			};
			textBlock.SetValue(Grid.ColumnProperty, i + 1);
			base.Children.Add(textBlock);
			TextBlock textBlock2 = new TextBlock
			{
				FontSize = 24.0,
				Margin = new Thickness(10.0, 0.0, 10.0, 10.0),
				VerticalAlignment = VerticalAlignment.Bottom,
				Text = date.Day.ToString(),
				Foreground = colorInString2,
				Opacity = ((date.Month != month) ? 0.6 : 1.0)
			};
			textBlock2.SetValue(Grid.ColumnProperty, i + 1);
			base.Children.Add(textBlock2);
			date = date.AddDays(1.0);
		}
		Line line2 = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 50.0, 0.0, -1.0),
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			Stroke = colorInString
		};
		line2.SetValue(Grid.ColumnProperty, days);
		base.Children.Add(line2);
		Line line3 = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			VerticalAlignment = VerticalAlignment.Bottom,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			Stroke = colorInString
		};
		line3.SetValue(Grid.ColumnSpanProperty, days + 1);
		base.Children.Add(line3);
	}
}
