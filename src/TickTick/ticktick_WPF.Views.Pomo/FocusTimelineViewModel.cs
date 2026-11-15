using System;
using System.Collections.Generic;
using System.Windows;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo;

public class FocusTimelineViewModel : BaseViewModel
{
	private string _time;

	private string _date;

	private double _lineWidth;

	private double _timeWidth = 16.0;

	private double _height = 20.0;

	private bool _isZero;

	private Thickness _margin = new Thickness(0.0, 0.0, 0.0, 40.0);

	public bool LastOne { get; set; }

	public bool IsZero
	{
		get
		{
			return _isZero;
		}
		set
		{
			_isZero = value;
			OnPropertyChanged("IsZero");
		}
	}

	public string Date
	{
		get
		{
			return _date;
		}
		set
		{
			_date = value;
			OnPropertyChanged("Date");
		}
	}

	public string Time
	{
		get
		{
			return _time;
		}
		set
		{
			_time = value;
			OnPropertyChanged("Time");
		}
	}

	public double LineWidth
	{
		get
		{
			return _lineWidth;
		}
		set
		{
			_lineWidth = value;
			OnPropertyChanged("LineWidth");
		}
	}

	public double TimeWidth
	{
		get
		{
			return _timeWidth;
		}
		set
		{
			_timeWidth = value;
			OnPropertyChanged("TimeWidth");
		}
	}

	public double Height
	{
		get
		{
			return _height;
		}
		set
		{
			_height = value;
			OnPropertyChanged("Height");
		}
	}

	public Thickness Margin
	{
		get
		{
			return _margin;
		}
		set
		{
			_margin = value;
			OnPropertyChanged("Margin");
		}
	}

	private FocusTimelineViewModel(DateTime date)
	{
		SetHour(date);
	}

	public void SetHour(DateTime date)
	{
		if (LocalSettings.Settings.TimeFormat == "24Hour")
		{
			TimeWidth = 36.0;
			Time = date.Hour.ToString() ?? "";
		}
		else
		{
			TimeWidth = 58.0;
			int hour = date.Hour;
			int num = hour % 12;
			if (num == 0)
			{
				num = 12;
			}
			Time = num + " " + ((hour >= 12 || hour < 0) ? "PM" : "AM");
		}
		if (date.Hour == 0)
		{
			IsZero = true;
			Date = date.ToString("MM'.'dd", App.Ci);
		}
		else
		{
			IsZero = false;
			Date = null;
		}
	}

	public static List<FocusTimelineViewModel> InitModels(DateTime date)
	{
		return new List<FocusTimelineViewModel>
		{
			new FocusTimelineViewModel(date),
			new FocusTimelineViewModel(date.AddHours(1.0)),
			new FocusTimelineViewModel(date.AddHours(2.0)),
			new FocusTimelineViewModel(date.AddHours(3.0)),
			new FocusTimelineViewModel(date.AddHours(4.0))
			{
				LastOne = true,
				Margin = new Thickness(0.0)
			}
		};
	}

	public void SetMargin(double bottom)
	{
		if (!LastOne)
		{
			Margin = new Thickness(0.0, 0.0, 0.0, bottom);
		}
	}
}
