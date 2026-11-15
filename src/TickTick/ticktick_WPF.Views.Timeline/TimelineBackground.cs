using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineBackground : Canvas
{
	public static readonly DependencyProperty LineCountProperty = DependencyProperty.Register("LineCount", typeof(int), typeof(TimelineBackground), new PropertyMetadata(0));

	public static readonly DependencyProperty GroupDictModelsProperty = DependencyProperty.Register("GroupDictModels", typeof(Dictionary<int, bool>), typeof(TimelineBackground), new PropertyMetadata(null, OnDependencyChanged));

	public static readonly DependencyProperty OneLineHeightProperty = DependencyProperty.Register("OneLineHeight", typeof(double), typeof(TimelineBackground), new PropertyMetadata(0.0, OnDependencyChanged));

	public static readonly DependencyProperty OneDayWidthProperty = DependencyProperty.Register("OneDayWidth", typeof(double), typeof(TimelineBackground), new PropertyMetadata(0.0, OnDependencyChanged));

	public static readonly DependencyProperty StartEndTupleProperty = DependencyProperty.Register("StartEndTuple", typeof(Tuple<DateTime, DateTime>), typeof(TimelineBackground), new PropertyMetadata(null, OnDependencyChanged));

	public static readonly DependencyProperty TimelineDayWidthIndexProperty = DependencyProperty.Register("TimelineDayWidthIndex", typeof(int), typeof(TimelineBackground), new PropertyMetadata(4, OnDependencyChanged));

	private bool _transparent;

	public int LineCount
	{
		get
		{
			return (int)GetValue(LineCountProperty);
		}
		set
		{
			SetValue(LineCountProperty, value);
		}
	}

	public Dictionary<int, bool> GroupDictModels
	{
		get
		{
			return (Dictionary<int, bool>)GetValue(GroupDictModelsProperty);
		}
		set
		{
			SetValue(GroupDictModelsProperty, value);
		}
	}

	public double OneLineHeight
	{
		get
		{
			return (double)GetValue(OneLineHeightProperty);
		}
		set
		{
			SetValue(OneLineHeightProperty, value);
		}
	}

	public double OneDayWidth
	{
		get
		{
			return (double)GetValue(OneDayWidthProperty);
		}
		set
		{
			SetCurrentValue(OneDayWidthProperty, value);
		}
	}

	public Tuple<DateTime, DateTime> StartEndTuple
	{
		get
		{
			return (Tuple<DateTime, DateTime>)GetValue(StartEndTupleProperty);
		}
		set
		{
			SetCurrentValue(StartEndTupleProperty, value);
		}
	}

	public int TimelineDayWidthIndex
	{
		get
		{
			return (int)GetValue(TimelineDayWidthIndexProperty);
		}
		set
		{
			SetCurrentValue(TimelineDayWidthIndexProperty, value);
		}
	}

	private static void OnDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((TimelineBackground)d).InvalidateVisual();
	}

	public TimelineBackground()
	{
		base.ClipToBounds = true;
		base.IsHitTestVisible = false;
		base.Loaded += delegate
		{
			LocalSettings.Settings.PropertyChanged += OnSettingsChanged;
		};
		base.Unloaded += delegate
		{
			LocalSettings.Settings.PropertyChanged -= OnSettingsChanged;
		};
	}

	private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "ThemeId")
		{
			InvalidateVisual();
		}
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		if (!(base.ActualWidth <= 0.0) && StartEndTuple != null)
		{
			if (LocalSettings.Settings.ThemeId == "White" || LocalSettings.Settings.ThemeId == "Blue" || LocalSettings.Settings.ThemeId == "Gray")
			{
				dc.DrawRectangle(_transparent ? Brushes.Transparent : ThemeUtil.GetColor("BaseColorOpacity3"), null, new Rect(0.0, 0.0, base.ActualWidth, base.ActualHeight));
			}
			else if (ThemeKey.IsProTheme(LocalSettings.Settings.ThemeId))
			{
				dc.DrawRectangle(TimelineThemes.BackgroundBrush, null, new Rect(0.0, 0.0, base.ActualWidth, base.ActualHeight));
			}
			DrawDay(dc);
			DrawGroupHideModels(dc);
			DrawToday(dc);
		}
	}

	private void DrawGroupHideModels(DrawingContext dc)
	{
		Dictionary<int, bool> groupDictModels = GroupDictModels;
		double oneLineHeight = OneLineHeight;
		SolidColorBrush solidColorBrush = new SolidColorBrush
		{
			Color = Colors.White,
			Opacity = 0.6
		};
		if (LocalSettings.Settings.ThemeId == "Pink" || LocalSettings.Settings.ThemeId == "Green" || LocalSettings.Settings.ThemeId == "Yellow")
		{
			solidColorBrush.Opacity = 0.4;
		}
		else if (ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId))
		{
			solidColorBrush.Opacity = 0.05;
		}
		Pen pen = new Pen
		{
			Brush = ThemeUtil.GetColor("BaseColorOpacity10")
		};
		int num = LineCount - 1 - 3;
		if (groupDictModels == null)
		{
			return;
		}
		foreach (KeyValuePair<int, bool> item in groupDictModels)
		{
			int key = item.Key;
			bool value = item.Value;
			double num2 = (double)key * oneLineHeight + 0.5;
			if (key != 0)
			{
				dc.DrawLine(pen, new Point(0.0, num2), new Point(base.ActualWidth, num2));
			}
			if (!value)
			{
				int num3 = ((key > 0) ? 1 : 0);
				dc.DrawRectangle(solidColorBrush, null, (key == num) ? new Rect(0.0, num2 + (double)num3, base.ActualWidth, base.ActualHeight - num2 - (double)num3) : new Rect(0.0, num2 + (double)num3, base.ActualWidth, oneLineHeight - (double)num3));
			}
		}
	}

	private void DrawToday(DrawingContext dc)
	{
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var _);
		DateTime dateTime = item;
		double x = (double)(DateTime.Today - dateTime).Days * OneDayWidth + OneDayWidth / 2.0;
		Pen pen = new Pen
		{
			Brush = new SolidColorBrush(TimelineThemes.DayLineColor)
			{
				Opacity = 0.2
			},
			Thickness = 2.0
		};
		dc.DrawLine(pen, new Point(x, 0.0), new Point(x, base.ActualHeight));
	}

	private void DrawDay(DrawingContext dc)
	{
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var item2);
		DateTime dateTime = item;
		DateTime dateTime2 = item2;
		double oneDayWidth = OneDayWidth;
		int num = 0;
		while (dateTime <= dateTime2)
		{
			double x = (double)num * oneDayWidth;
			if (DateUtils.IsWeekEnds(dateTime))
			{
				dc.DrawRectangle(TimelineThemes.WeekendBrush, null, new Rect(x, 0.0, oneDayWidth, base.ActualHeight));
			}
			dateTime = dateTime.AddDays(1.0);
			num++;
		}
	}

	private void DrawMonth(DrawingContext dc)
	{
		Pen pen = new Pen
		{
			Brush = TimelineThemes.GridBrush
		};
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var item2);
		DateTime dateTime = item;
		DateTime dateTime2 = item2;
		double oneDayWidth = OneDayWidth;
		double num = 0.0;
		while (dateTime <= dateTime2)
		{
			double num2 = (double)DateTime.DaysInMonth(dateTime.Year, dateTime.Month) * oneDayWidth;
			dc.DrawLine(pen, new Point(num, 0.0), new Point(num, base.ActualHeight));
			num += num2;
			dateTime = dateTime.AddMonths(1);
		}
	}

	private void DrawWeek(DrawingContext dc)
	{
		TupleExtensions.Deconstruct(StartEndTuple, out var item, out var item2);
		DateTime dateTime = item;
		DateTime dateTime2 = item2;
		double num = 0.0;
		double oneDayWidth = OneDayWidth;
		double num2 = oneDayWidth * 7.0;
		double num3 = oneDayWidth * 5.0;
		double width = oneDayWidth * 2.0;
		while (dateTime <= dateTime2)
		{
			dc.DrawRectangle(TimelineThemes.WeekendBrush, null, new Rect(num + num3, 0.0, width, base.ActualHeight));
			num += num2;
			dateTime = dateTime.AddDays(7.0);
		}
	}

	internal void SetBackground(bool transparent)
	{
		_transparent = transparent;
		InvalidateVisual();
	}
}
