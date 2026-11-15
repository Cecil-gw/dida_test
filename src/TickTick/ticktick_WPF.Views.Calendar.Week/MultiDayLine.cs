using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar.Week;

public class MultiDayLine : Border
{
	public double Offset;

	private DateTime _date;

	private Line _line;

	public DateTime Date
	{
		get
		{
			return _date;
		}
		set
		{
			_date = value;
			if (TickTickUtils.DateUtils.IsWeekends(_date))
			{
				SetResourceReference(Border.BackgroundProperty, "BaseColorOpacity2");
			}
			else
			{
				base.Background = Brushes.Transparent;
			}
			bool flag = ThemeUtil.IsDark();
			_line.SetResourceReference(Shape.StrokeProperty, flag ? "BaseColorOpacity2" : "BaseColorOpacity4");
		}
	}

	public MultiDayLine()
	{
		base.IsHitTestVisible = false;
		base.Margin = new Thickness(0.0, 33.0, 0.0, 0.0);
		base.HorizontalAlignment = HorizontalAlignment.Left;
		InitLine();
	}

	private void InitLine()
	{
		_line = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			IsHitTestVisible = false
		};
		bool flag = ThemeUtil.IsDark();
		_line.SetResourceReference(Shape.StrokeProperty, flag ? "BaseColorOpacity2" : "BaseColorOpacity4");
		Child = _line;
	}
}
