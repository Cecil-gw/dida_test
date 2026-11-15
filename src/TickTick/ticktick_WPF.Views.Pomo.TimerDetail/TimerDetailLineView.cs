using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerDetailLineView : Grid
{
	private Line _averageLine;

	private TextBlock _averageText;

	private Grid _durTextGrid;

	private List<TimerDayItemViewModel> _items;

	private double _topMargin = 10.0;

	private int _lineNum = 4;

	public TimerDetailLineView()
	{
		base.Height = 160.0 + _topMargin;
		base.ColumnDefinitions.Add(new ColumnDefinition());
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(54.0)
		});
		_averageLine = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 2.0,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 120.0 + _topMargin, 0.0, 0.0)
		};
		_averageLine.Stroke = Brushes.Red;
		_averageLine.SetValue(Panel.ZIndexProperty, 100);
		base.Children.Add(_averageLine);
		_averageText = new TextBlock
		{
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(8.0, _averageLine.Margin.Top - 7.0 + _topMargin, 0.0, 0.0),
			Foreground = Brushes.Red
		};
		_averageText.SetResourceReference(FrameworkElement.StyleProperty, "Tag03");
		_averageText.SetValue(Grid.ColumnProperty, 1);
		base.Children.Add(_averageText);
		_durTextGrid = new Grid();
		_durTextGrid.SetValue(Grid.ColumnProperty, 1);
		base.Children.Add(_durTextGrid);
		SetValueText(1);
		Line line = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, _topMargin, 0.0, 40.0),
			StrokeDashArray = new DoubleCollection { 3.0, 3.0 }
		};
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity10");
		base.Children.Add(line);
	}

	private void SetLine(int hour)
	{
		int lineNum = ((hour == 2 || hour == 4) ? 3 : 4);
		_lineNum = lineNum;
	}

	private void SetValueText(int hour)
	{
		int num = ((hour == 2 || hour == 4) ? 3 : 4);
		int num2 = hour * 60;
		if (_durTextGrid.Children.Count == num)
		{
			for (int i = 0; i < _durTextGrid.Children.Count; i++)
			{
				if (_durTextGrid.Children[i] is TextBlock { Margin: { Top: var top } } textBlock)
				{
					bool flag = Math.Abs(top - _averageText.Margin.Top) >= 14.0;
					textBlock.Text = (flag ? GetDurText(num2 * i / (num - 1)) : string.Empty);
				}
			}
			return;
		}
		_durTextGrid.Children.Clear();
		int num3 = 120 / (num - 1);
		for (int j = 0; j < num; j++)
		{
			TextBlock textBlock2 = new TextBlock
			{
				Margin = new Thickness(8.0, 120.0 + _topMargin - (double)(num3 * j) - 8.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Top
			};
			bool flag2 = Math.Abs(textBlock2.Margin.Top - _averageText.Margin.Top) >= 14.0;
			textBlock2.Text = (flag2 ? GetDurText(num2 * j / (num - 1)) : string.Empty);
			textBlock2.SetResourceReference(FrameworkElement.StyleProperty, "Tag03");
			_durTextGrid.Children.Add(textBlock2);
		}
	}

	private string GetDurText(int m)
	{
		if (m < 60)
		{
			return m + "m";
		}
		return m / 60 + "h";
	}

	public void SetData(TimerTimelineItemViewModel model, Dictionary<string, long> statisticsData)
	{
		long num = ((statisticsData.Count > 0) ? statisticsData.Values.Max() : 0);
		long num2 = ((statisticsData.Count > 0) ? statisticsData.Values.Sum() : 0);
		long num3 = num / 60 + ((num % 60 > 0) ? 1 : 0);
		if (num3 > 6)
		{
			num3 += ((num3 % 3 > 0) ? (3 - num3 % 3) : 0);
		}
		num3 = Math.Max(1L, num3);
		int avgMin = (int)(num2 / Math.Max(statisticsData.Count((KeyValuePair<string, long> kv) => kv.Value > 0), 1));
		SetAverage(avgMin, num3);
		SetValueText((int)num3);
		SetLine((int)num3);
		List<TimerDayItemViewModel> list = new List<TimerDayItemViewModel>();
		if (model.Interval == "year")
		{
			DateTime dateTime = model.StartDate.AddMonths(1 - model.StartDate.Month).AddDays(1 - model.StartDate.Day);
			for (int num4 = 0; num4 < 12; num4++)
			{
				DateTime date = dateTime.AddMonths(num4);
				string key = DateUtils.GetDateNum(date).ToString();
				long minutes = (statisticsData.ContainsKey(key) ? statisticsData[key] : 0);
				list.Add(new TimerDayItemViewModel(date, minutes, model.Interval, num3 * 60));
			}
		}
		else
		{
			DateTime startDate = model.StartDate;
			int num5 = ((model.Interval == "month") ? startDate.AddMonths(1).AddDays(-1.0).Day : 7);
			for (int num6 = 0; num6 < num5; num6++)
			{
				DateTime date2 = startDate.AddDays(num6);
				string key2 = DateUtils.GetDateNum(date2).ToString();
				long minutes2 = (statisticsData.ContainsKey(key2) ? statisticsData[key2] : 0);
				list.Add(new TimerDayItemViewModel(date2, minutes2, model.Interval, num3 * 60));
			}
		}
		_items = list;
		InvalidateVisual();
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		Pen pen = new Pen();
		pen.Brush = ThemeUtil.GetColor("BaseColorOpacity10");
		pen.DashStyle = new DashStyle(new double[2] { 3.0, 3.0 }, 0.0);
		pen.Thickness = 1.0;
		Pen pen2 = pen;
		DrawLine(dc);
		SolidColorBrush color = ThemeUtil.GetColor("PrimaryColor");
		List<TimerDayItemViewModel> items = _items;
		if (items == null || items.Count <= 0)
		{
			return;
		}
		double num = (base.ActualWidth - 56.0) / (double)_items.Count;
		double num2 = (num - (double)_items[0].ColumnWidth) / 2.0;
		for (int i = 0; i < _items.Count; i++)
		{
			TimerDayItemViewModel timerDayItemViewModel = _items[i];
			if (timerDayItemViewModel.LineVisible)
			{
				dc.DrawLine(pen2, new Point((double)i * num, _topMargin), new Point((double)i * num, 120.0 + _topMargin));
			}
			if (timerDayItemViewModel.ColumnHeight > 0)
			{
				Geometry geometry = GetGeometry(num2 + (double)i * num, timerDayItemViewModel.ColumnWidth, timerDayItemViewModel.ColumnHeight);
				dc.DrawGeometry(color, null, geometry);
			}
			if (!string.IsNullOrEmpty((num >= 16.0 || !timerDayItemViewModel.HideText) ? timerDayItemViewModel.DayText : string.Empty))
			{
				dc.DrawText(timerDayItemViewModel.TextFormat, new Point((double)i * num + (num - timerDayItemViewModel.TextFormat.Width) / 2.0, 130.0 + _topMargin));
			}
		}
	}

	private void DrawLine(DrawingContext dc)
	{
		Pen pen = new Pen
		{
			Brush = ThemeUtil.GetColor("BaseColorOpacity10"),
			Thickness = 1.0
		};
		int num = 120 / (_lineNum - 1);
		double x = base.ActualWidth - 54.0;
		for (int i = 0; i < _lineNum; i++)
		{
			dc.DrawLine(pen, new Point(0.0, _topMargin + (double)(i * num)), new Point(x, _topMargin + (double)(i * num)));
		}
	}

	private Geometry GetGeometry(double x, int pathWidth, int pathHeight)
	{
		PathGeometry pathGeometry = new PathGeometry();
		PathFigure pathFigure = new PathFigure
		{
			StartPoint = new Point(x, 120.0 + _topMargin),
			IsClosed = true
		};
		int num = Math.Min(pathWidth / 2, Math.Min(4, pathHeight));
		pathFigure.Segments.Add(new LineSegment
		{
			Point = new Point(x, 120.0 + _topMargin - (double)pathHeight + (double)num)
		});
		pathFigure.Segments.Add(new ArcSegment
		{
			Size = new Size(num, num),
			Point = new Point(x + (double)num, 120.0 + _topMargin - (double)pathHeight),
			SweepDirection = SweepDirection.Clockwise
		});
		pathFigure.Segments.Add(new LineSegment
		{
			Point = new Point(x + (double)pathWidth - (double)num, 120.0 + _topMargin - (double)pathHeight)
		});
		pathFigure.Segments.Add(new ArcSegment
		{
			Size = new Size(num, num),
			Point = new Point(x + (double)pathWidth, 120.0 + _topMargin - (double)pathHeight + (double)num),
			SweepDirection = SweepDirection.Clockwise
		});
		pathFigure.Segments.Add(new LineSegment
		{
			Point = new Point(x + (double)pathWidth, 120.0 + _topMargin)
		});
		pathGeometry.Figures.Add(pathFigure);
		return pathGeometry;
	}

	private void SetAverage(int avgMin, long hour)
	{
		if (avgMin == 0)
		{
			_averageLine.Visibility = Visibility.Collapsed;
			_averageText.Visibility = Visibility.Collapsed;
			_averageLine.Margin = new Thickness(0.0, 160.0 + _topMargin, 0.0, 0.0);
			_averageText.Margin = new Thickness(8.0, 160.0 + _topMargin, 0.0, 0.0);
		}
		else
		{
			_averageLine.Visibility = Visibility.Visible;
			_averageText.Visibility = Visibility.Visible;
			double num = 120.0 + _topMargin - (double)(avgMin * 2 / hour);
			_averageLine.Margin = new Thickness(0.0, num, 0.0, 0.0);
			_averageText.Margin = new Thickness(8.0, num - 8.0, 0.0, 0.0);
			_averageText.Text = Utils.GetShortDurationString(avgMin * 60, showZeroMinute: false);
		}
	}

	public (double, TimerDayItemViewModel) GetHoverItem(MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		if (position.Y < 120.0 + _topMargin)
		{
			List<TimerDayItemViewModel> items = _items;
			if (items != null && items.Count > 0)
			{
				double num = (base.ActualWidth - 56.0) / (double)_items.Count;
				int num2 = (int)(position.X / num);
				if (_items.Count > num2)
				{
					return (((double)num2 + 0.5) * num, _items[num2]);
				}
			}
		}
		return (0.0, null);
	}
}
