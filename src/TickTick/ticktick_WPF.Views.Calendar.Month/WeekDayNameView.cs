using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar.Month;

public class WeekDayNameView : Grid
{
	private readonly Dictionary<int, TextBlock> _dayNames = new Dictionary<int, TextBlock>();

	private readonly Line _hLine;

	private bool? _showWeekend;

	public WeekDayNameView()
	{
		base.Height = 32.0;
		_hLine = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		_hLine.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		base.Children.Add(_hLine);
		SetColumns();
		SetDayNames();
	}

	public void ResetColumns(bool force = false)
	{
		if (force || !_showWeekend.HasValue || _showWeekend.Value != LocalSettings.Settings.ShowCalWeekend)
		{
			SetColumns();
			SetDayNames();
		}
	}

	private void SetColumns()
	{
		_showWeekend = LocalSettings.Settings.ShowCalWeekend;
		int num = (LocalSettings.Settings.ShowCalWeekend ? 7 : 5);
		if (num != base.ColumnDefinitions.Count)
		{
			int count = base.ColumnDefinitions.Count;
			if (num > count)
			{
				for (int i = 0; i < num - count; i++)
				{
					base.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					});
					TextBlock textBlock = new TextBlock
					{
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Center,
						FontSize = 12.0
					};
					textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
					textBlock.SetValue(Grid.ColumnProperty, count + i);
					_dayNames[count + i] = textBlock;
					base.Children.Add(textBlock);
				}
			}
			else
			{
				base.ColumnDefinitions.RemoveRange(num, count - num);
				for (int j = num; j < count; j++)
				{
					if (_dayNames.TryGetValue(j, out var value))
					{
						_dayNames.Remove(j);
						base.Children.Remove(value);
					}
				}
			}
		}
		_hLine.SetValue(Grid.ColumnSpanProperty, num);
	}

	private void SetDayNames()
	{
		DateTime dateTime = Utils.GetWeekStart(DateTime.Today);
		int num = (LocalSettings.Settings.ShowCalWeekend ? 7 : 5);
		for (int i = 0; i < num; i++)
		{
			if (!LocalSettings.Settings.ShowCalWeekend)
			{
				while (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday)
				{
					dateTime = dateTime.AddDays(1.0);
				}
			}
			if (_dayNames.TryGetValue(i, out var value))
			{
				value.Text = dateTime.ToString("ddd", App.Ci);
				dateTime = dateTime.AddDays(1.0);
			}
		}
	}
}
