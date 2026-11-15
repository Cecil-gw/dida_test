using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar.Month;

public class MonthDayViewModel : BaseViewModel
{
	private static readonly Geometry ShowWorkRestEllipsePath;

	private static readonly Geometry HideWorkRestEllipsePath;

	private MonthCellState _state;

	private DateTime _date;

	private double _textWidth = 14.0;

	public DateTime Date
	{
		get
		{
			return _date;
		}
		set
		{
			_date = value;
			OnPropertyChanged("Date");
			if (Date.Day == 1)
			{
				string text = DateUtils.FormatMonthDay(Date, DateFormatLen.Middle);
				_textWidth = Utils.MeasureString(text, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 14.0).Width;
			}
		}
	}

	public bool IsToday { get; set; }

	public bool IsWeekend { get; set; }

	public bool IsCurrentMonth { get; set; }

	public bool IsPast => Date.Date < DateTime.Today;

	public bool ShowMonthday { get; set; }

	public MonthCellState State
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
			OnPropertyChanged("State");
		}
	}

	public bool IsWorkDay { get; set; }

	public bool IsRestDay { get; set; }

	public bool DayMode { get; set; }

	public double WorkRestOpacity
	{
		get
		{
			if (IsToday)
			{
				return 1.0;
			}
			if (IsCurrentMonth)
			{
				return 1.0;
			}
			return 0.4;
		}
	}

	public double IconWidth
	{
		get
		{
			if (ShowMonthday && Date.Day == 1)
			{
				return _textWidth + 10.0;
			}
			return 24.0;
		}
	}

	public Geometry EllipseGeometry
	{
		get
		{
			if (ShowMonthday && Date.Day == 1)
			{
				if (IsWorkDay || IsRestDay)
				{
					return new CombinedGeometry
					{
						GeometryCombineMode = GeometryCombineMode.Exclude,
						Geometry1 = new RectangleGeometry
						{
							RadiusX = 12.0,
							RadiusY = 12.0,
							Rect = new Rect(0.0, 0.0, IconWidth, 24.0)
						},
						Geometry2 = new EllipseGeometry
						{
							Center = new Point(IconWidth, 6.0),
							RadiusX = 7.0,
							RadiusY = 7.0
						}
					};
				}
				return new RectangleGeometry
				{
					RadiusX = 12.0,
					RadiusY = 12.0,
					Rect = new Rect(0.0, 0.0, IconWidth, 24.0)
				};
			}
			if (!IsWorkDay && !IsRestDay)
			{
				return HideWorkRestEllipsePath;
			}
			return ShowWorkRestEllipsePath;
		}
	}

	static MonthDayViewModel()
	{
		ShowWorkRestEllipsePath = new CombinedGeometry
		{
			GeometryCombineMode = GeometryCombineMode.Exclude,
			Geometry1 = new EllipseGeometry
			{
				Center = new Point(12.0, 12.0),
				RadiusX = 12.0,
				RadiusY = 12.0
			},
			Geometry2 = new EllipseGeometry
			{
				Center = new Point(24.0, 3.0),
				RadiusX = 6.0,
				RadiusY = 6.0
			}
		};
		HideWorkRestEllipsePath = new EllipseGeometry
		{
			Center = new Point(12.0, 12.0),
			RadiusX = 12.0,
			RadiusY = 12.0
		};
	}

	public void SetDate(DateTime date)
	{
		if (date != Date)
		{
			Date = date;
			List<HolidayModel> obj = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? HolidayManager.GetCacheHolidays() : new List<HolidayModel>());
			bool isWeekend = Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday;
			bool isWorkDay = obj.Exists((HolidayModel day) => day.date == Date && day.type == 1) && SettingsHelper.GetShowHoliday();
			bool isRestDay = obj.Exists((HolidayModel day) => day.date == Date && day.type == 0) && SettingsHelper.GetShowHoliday();
			IsToday = Date == DateTime.Today;
			IsWeekend = isWeekend;
			IsWorkDay = isWorkDay;
			IsRestDay = isRestDay;
		}
	}

	public void AddDays(int days)
	{
		SetDate(Date.AddDays(days));
	}

	public void SetCurrentMonth(DateTime currentMonth)
	{
		if (Date.Month == currentMonth.Month != IsCurrentMonth)
		{
			IsCurrentMonth = Date.Month == currentMonth.Month;
			OnPropertyChanged("IsCurrentMonth");
		}
	}
}
