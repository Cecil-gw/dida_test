using System;
using System.Windows.Media;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.ViewModels;

public class HabitDayCheckModel : BaseViewModel
{
	private DateTime _date;

	private bool _needShowPercent;

	private bool _selected;

	private double _percent;

	private string _toolTipText;

	private int _status;

	private double _btnOpacity;

	public HabitCheckInModel CheckInModel { get; set; }

	public bool IsBooleanHabit { get; set; }

	public string HabitId { get; set; }

	public string HabitName { get; set; }

	public int Column { get; set; }

	public string DayOfWeekName { get; set; }

	public Geometry Icon
	{
		get
		{
			if (Completed)
			{
				return Utils.GetGeometry("Ic.Common.Completed");
			}
			if (Uncompleted)
			{
				return Utils.GetGeometry("Ic.Common.Close");
			}
			if (!NeedCheck && string.IsNullOrEmpty(HabitId))
			{
				return Utils.GetIcon("IcNoHabitNeedCheck");
			}
			return null;
		}
	}

	public double IconSize
	{
		get
		{
			if (!NeedCheck && string.IsNullOrEmpty(HabitId))
			{
				return 24.0;
			}
			return 14.0;
		}
	}

	public SolidColorBrush IconColor
	{
		get
		{
			if (Completed)
			{
				return Brushes.White;
			}
			if (Uncompleted)
			{
				return ThemeUtil.GetColor("HabitRedColor");
			}
			if (!NeedCheck && string.IsNullOrEmpty(HabitId))
			{
				return ThemeUtil.GetColor("BaseColorOpacity5");
			}
			return Brushes.Transparent;
		}
	}

	public SolidColorBrush EllColor
	{
		get
		{
			if (!NeedCheck && string.IsNullOrEmpty(HabitId))
			{
				return Brushes.Transparent;
			}
			if (Completed)
			{
				return ThemeUtil.GetColor("PrimaryColor");
			}
			if (Uncompleted || !NeedCheck || Date > DateTime.Today)
			{
				return ThemeUtil.GetColor("BaseColorOpacity3");
			}
			if (IsBooleanHabit || (!(_percent > 0.0) && !string.IsNullOrEmpty(HabitId)))
			{
				return ThemeUtil.GetColor("BaseColorOpacity8");
			}
			return Brushes.Transparent;
		}
	}

	public bool NeedShowPercent { get; set; }

	public double Percent
	{
		get
		{
			return _percent;
		}
		set
		{
			_percent = value;
			SetToolTip();
			OnPropertyChanged("Percent");
		}
	}

	public bool IsToday => Date == DateTime.Today;

	public bool IsFuture => Date > DateTime.Today;

	public double BtnOpacity
	{
		get
		{
			return _btnOpacity;
		}
		set
		{
			_btnOpacity = value;
			OnPropertyChanged("BtnOpacity");
		}
	}

	public int IconWidth => 24;

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

	public string ToolTipText
	{
		get
		{
			return _toolTipText;
		}
		set
		{
			_toolTipText = value;
			OnPropertyChanged("ToolTipText");
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
		}
	}

	public int Status
	{
		get
		{
			return _status;
		}
		set
		{
			_status = value;
			OnPropertyChanged("Status");
			OnPropertyChanged("Completed");
			OnPropertyChanged("Uncompleted");
			OnPropertyChanged("EllColor");
			OnPropertyChanged("Icon");
			OnPropertyChanged("IconSize");
			OnPropertyChanged("IconColor");
		}
	}

	public bool Completed
	{
		get
		{
			if (Status != 1)
			{
				return CheckInModel.Value >= CheckInModel.Goal;
			}
			return false;
		}
	}

	public bool Uncompleted => Status == 1;

	public string HabitType { get; set; }

	public bool NeedCheck { get; set; } = true;

	public HabitDayCheckModel(HabitCheckInModel checkIn, DateTime date, HabitModel habit, int column)
	{
		Date = date;
		Column = column;
		HabitId = habit?.Id;
		HabitName = habit?.Name;
		SetData(checkIn, habit);
		DayOfWeekName = DateUtils.GetShortWeekTextByWeekDay((int)Date.DayOfWeek);
	}

	public async void SetToolTip()
	{
		HabitModel habitModel = await HabitDao.GetHabitById(CheckInModel?.HabitId);
		string text = Date.ToString("ddd", App.Ci);
		string text2 = (Utils.IsEn() ? (text + ", " + DateUtils.FormatMonthDay(Date, DateFormatLen.Middle)) : (DateUtils.FormatMonthDay(Date, DateFormatLen.Middle) + " " + text));
		DateUtils.FormatMonthDay(Date, DateFormatLen.Long);
		ToolTipText = ((CheckInModel == null) ? null : (text2 + (NeedShowPercent ? ("\r\n" + CheckInModel.Value + "/" + CheckInModel.Goal + " " + HabitUtils.GetUnitText(habitModel?.Unit)) : "")));
	}

	public void SetData(HabitCheckInModel checkIn, HabitModel habit)
	{
		if (checkIn == null || checkIn.Status == -1)
		{
			checkIn = new HabitCheckInModel
			{
				Value = 0.0,
				Goal = habit.Goal
			};
		}
		CheckInModel = checkIn;
		IsBooleanHabit = habit.IsBoolHabit();
		NeedShowPercent = !habit.IsBoolHabit();
		Status = checkIn.CheckStatus;
		Percent = Math.Min((int)(checkIn.Value / checkIn.Goal * 100.0), 100);
		HabitType = habit.Type;
	}

	public void UnCompleteHabit()
	{
		Status = 1;
		HabitService.UncompleteHabit(HabitId, Date);
	}
}
