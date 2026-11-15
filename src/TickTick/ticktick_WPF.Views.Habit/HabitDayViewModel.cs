using System;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class HabitDayViewModel : BaseViewModel
{
	private bool _completed;

	private bool _unCompleted;

	private bool _unChecked;

	private DateTime _date;

	private bool _hasPercent;

	private bool _hover;

	private bool _isBooleanHabit;

	private double _percent;

	private bool _showBoolHint;

	private bool _useIconStyle;

	private ShowMode _showMode = ShowMode.CurrentMonth;

	private bool _showRealHint;

	private bool _isFutureDate;

	public string HabitId { get; set; }

	public HabitModel Habit { get; set; }

	public string Color { get; set; }

	public string IconText { get; set; }

	public string ImageUrl { get; set; }

	public bool UseIconStyle
	{
		get
		{
			return _useIconStyle;
		}
		set
		{
			_useIconStyle = value;
			OnPropertyChanged("UseIconStyle");
		}
	}

	public bool HasPercent
	{
		get
		{
			if (_hasPercent)
			{
				return !_unCompleted;
			}
			return false;
		}
		set
		{
			_hasPercent = value;
			OnPropertyChanged("HasPercent");
		}
	}

	public bool UnChecked
	{
		get
		{
			return _unChecked;
		}
		set
		{
			_unChecked = value;
			OnPropertyChanged("UnChecked");
		}
	}

	public bool UnCompleted
	{
		get
		{
			return _unCompleted;
		}
		set
		{
			_unCompleted = value;
			OnPropertyChanged("UnCompleted");
		}
	}

	public bool Completed
	{
		get
		{
			return _completed;
		}
		set
		{
			_completed = value;
			OnPropertyChanged("Completed");
		}
	}

	public double Percent
	{
		get
		{
			return _percent;
		}
		set
		{
			_percent = value;
			OnPropertyChanged("Percent");
		}
	}

	public bool IsFutureDate
	{
		get
		{
			return _isFutureDate;
		}
		set
		{
			_isFutureDate = value;
			OnPropertyChanged("IsFutureDate");
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
			OnPropertyChanged("ShowMode");
		}
	}

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

	public bool Hover
	{
		get
		{
			return _hover;
		}
		set
		{
			_hover = value;
			OnPropertyChanged("Hover");
		}
	}

	public bool IsBooleanHabit
	{
		get
		{
			return _isBooleanHabit;
		}
		set
		{
			_isBooleanHabit = value;
			OnPropertyChanged("IsBooleanHabit");
		}
	}

	public bool ShowBoolHint
	{
		get
		{
			return _showBoolHint;
		}
		set
		{
			_showBoolHint = value;
			OnPropertyChanged("ShowBoolHint");
		}
	}

	public bool ShowRealHint
	{
		get
		{
			return _showRealHint;
		}
		set
		{
			_showRealHint = value;
			OnPropertyChanged("ShowRealHint");
		}
	}
}
