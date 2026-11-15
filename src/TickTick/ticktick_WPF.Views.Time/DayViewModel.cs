using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Time;

public class DayViewModel : BaseViewModel
{
	private static readonly Geometry ShowWorkRestEllipsePath;

	private static readonly Geometry HideWorkRestEllipsePath;

	private DateTime _date;

	private bool _hasTasks;

	private bool _hover;

	private bool _isRepeat;

	private bool _selected;

	private bool _canselect = true;

	private bool _isCurrentWeek;

	private Geometry _ellipseGeometry;

	private double _tabBorderThickness;

	private bool _useInSlideMenu;

	private SelectionMode _selection;

	private ShowMode _showMode = ShowMode.CurrentMonth;

	private bool _isFixed;

	private FrameworkElement _context;

	public bool IsRest { get; set; }

	public bool IsWork { get; set; }

	public double WorkTextOpacity
	{
		get
		{
			if (!Selected && ShowMode != ShowMode.CurrentMonth && ShowMode != ShowMode.Today)
			{
				return 0.4;
			}
			return 1.0;
		}
	}

	public SolidColorBrush EllipseColor
	{
		get
		{
			if (IsFixed)
			{
				return Brushes.Transparent;
			}
			if (Selected)
			{
				if (!UseInSlideMenu)
				{
					return ThemeUtil.GetColor("PrimaryColor", _context);
				}
				return ThemeUtil.GetDateSelectedColorInMenu(_context);
			}
			if (Hover || IsRepeat)
			{
				if (!UseInSlideMenu)
				{
					return ThemeUtil.GetColor("BaseColorOpacity5", _context);
				}
				return ThemeUtil.GetColor("MiniCalHoverColor", _context);
			}
			if (ShowMode != ShowMode.Today)
			{
				return Brushes.Transparent;
			}
			if (!UseInSlideMenu)
			{
				return ThemeUtil.GetColor("PrimaryColor10", _context);
			}
			return ThemeUtil.GetColor("ProjectSelectedBackground", _context);
		}
	}

	public SolidColorBrush DateTextBrush
	{
		get
		{
			if (IsFixed)
			{
				return GetDefaultTextColor();
			}
			if (!CanSelect)
			{
				return GetMuteTextColor();
			}
			if (Selected)
			{
				return Brushes.White;
			}
			if (ShowMode == ShowMode.Today)
			{
				if (!UseInSlideMenu)
				{
					return ThemeUtil.GetColor("TextAccentColor", _context);
				}
				return ThemeUtil.GetTodayColorInMenu(_context);
			}
			if (Hover || ShowMode == ShowMode.CurrentMonth)
			{
				return GetDefaultTextColor();
			}
			if (ShowMode != ShowMode.CurrentMonth)
			{
				return GetMuteTextColor();
			}
			return GetDefaultTextColor();
		}
	}

	public Geometry EllipseGeometry
	{
		get
		{
			return _ellipseGeometry;
		}
		set
		{
			_ellipseGeometry = value;
			OnPropertyChanged("EllipseGeometry");
		}
	}

	public double TabBorderThickness
	{
		get
		{
			return _tabBorderThickness;
		}
		set
		{
			_tabBorderThickness = value;
			OnPropertyChanged("TabBorderThickness");
		}
	}

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
			OnPropertyChanged("EllipseColor");
			OnPropertyChanged("DateTextBrush");
			OnPropertyChanged("WorkTextOpacity");
			OnPropertyChanged("ShowLunarMonthFirstDay");
			OnPropertyChanged("ShowHasTasks");
		}
	}

	public bool CanSelect
	{
		get
		{
			return _canselect;
		}
		set
		{
			_canselect = value;
			OnPropertyChanged("CanSelect");
			OnPropertyChanged("EllipseColor");
			OnPropertyChanged("DateTextBrush");
			OnPropertyChanged("WorkRestOpacity");
		}
	}

	public bool CanDoubleSelect { get; set; } = true;

	public bool LunarMonthFirstDay { get; set; }

	public bool ShowLunarMonthFirstDay
	{
		get
		{
			if (LunarMonthFirstDay)
			{
				return !Selected;
			}
			return false;
		}
	}

	public string LunarText { get; set; }

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
		}
	}

	public bool IsFixed
	{
		get
		{
			return _isFixed;
		}
		set
		{
			_isFixed = value;
			OnPropertyChanged("EllipseColor");
			OnPropertyChanged("DateTextBrush");
		}
	}

	public ShowMode ShowMode
	{
		get
		{
			return _showMode;
		}
		set
		{
			_showMode = value;
			OnPropertyChanged("WorkRestOpacity");
		}
	}

	public SelectionMode Selection
	{
		get
		{
			return _selection;
		}
		set
		{
			_selection = value;
			OnPropertyChanged("Selection");
		}
	}

	public bool IsRepeat
	{
		get
		{
			return _isRepeat;
		}
		set
		{
			_isRepeat = value;
			OnPropertyChanged("EllipseColor");
			OnPropertyChanged("DateTextBrush");
		}
	}

	public bool Hover
	{
		get
		{
			return _hover;
		}
		set
		{
			if (_hover != value)
			{
				_hover = value;
				OnPropertyChanged("EllipseColor");
				OnPropertyChanged("DateTextBrush");
			}
		}
	}

	public Visibility ShowHasTasks
	{
		get
		{
			if (!HasTasks || _selected)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Thickness HasTasksMargin
	{
		get
		{
			if (string.IsNullOrEmpty(LunarText))
			{
				return new Thickness(0.0, 0.0, 0.0, 2.0);
			}
			return new Thickness(0.0, 0.0, 0.0, 0.0);
		}
	}

	public bool HasTasks
	{
		get
		{
			return _hasTasks;
		}
		set
		{
			_hasTasks = value;
			OnPropertyChanged("ShowHasTasks");
		}
	}

	public bool UseInSlideMenu
	{
		get
		{
			return _useInSlideMenu;
		}
		set
		{
			_useInSlideMenu = value;
			OnPropertyChanged("UseInSlideMenu");
		}
	}

	public double WorkRestOpacity
	{
		get
		{
			if (!CanSelect)
			{
				return 0.4;
			}
			if (ShowMode == ShowMode.Today)
			{
				return 1.0;
			}
			if (Selected || ShowMode == ShowMode.CurrentMonth)
			{
				return 1.0;
			}
			if (ShowMode != ShowMode.CurrentMonth)
			{
				return 0.4;
			}
			return 1.0;
		}
	}

	static DayViewModel()
	{
		ShowWorkRestEllipsePath = new CombinedGeometry
		{
			GeometryCombineMode = GeometryCombineMode.Exclude,
			Geometry1 = new EllipseGeometry
			{
				Center = new Point(15.0, 15.0),
				RadiusX = 15.0,
				RadiusY = 15.0
			},
			Geometry2 = new EllipseGeometry
			{
				Center = new Point(25.0, 5.0),
				RadiusX = 6.0,
				RadiusY = 6.0
			}
		};
		HideWorkRestEllipsePath = new EllipseGeometry
		{
			Center = new Point(15.0, 15.0),
			RadiusX = 15.0,
			RadiusY = 15.0
		};
	}

	public DayViewModel(DateTime date, FrameworkElement context)
	{
		Date = date;
		_context = context;
		SetHoliday();
	}

	public void SetHoliday()
	{
		HolidayModel holidayModel = ((LocalSettings.Settings.EnableHoliday && Utils.IsDida()) ? HolidayManager.GetCacheHolidays() : new List<HolidayModel>())?.FirstOrDefault((HolidayModel h) => h.date == Date.Date);
		EllipseGeometry = ((holidayModel == null) ? HideWorkRestEllipsePath : ShowWorkRestEllipsePath);
		if (holidayModel != null)
		{
			IsWork = holidayModel.type == 1;
			IsRest = holidayModel.type == 0;
		}
		else
		{
			IsWork = false;
			IsRest = false;
		}
		OnPropertyChanged("IsRest");
	}

	private SolidColorBrush GetMuteTextColor()
	{
		return ThemeUtil.GetColor(_useInSlideMenu ? "ProjectSectionColor" : "BaseColorOpacity40", _context);
	}

	private SolidColorBrush GetDefaultTextColor()
	{
		return ThemeUtil.GetColor(_useInSlideMenu ? "ProjectMenuColorOpacity100_80" : "BaseColorOpacity100_80", _context);
	}

	public void NotifyColorChanged()
	{
		OnPropertyChanged("EllipseColor");
		OnPropertyChanged("DateTextBrush");
	}
}
