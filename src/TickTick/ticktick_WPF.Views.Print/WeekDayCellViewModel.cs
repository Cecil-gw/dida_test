using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Print;

public class WeekDayCellViewModel : BaseViewModel
{
	private DateTime _date;

	private MonthCellState _state;

	public List<TaskCellViewModel> Cells = new List<TaskCellViewModel>();

	public bool InWeekControl;

	public string Extra1 { get; set; }

	public string Extra2 { get; set; }

	public string Extra3 { get; set; }

	public string Extra4 { get; set; }

	public List<CalendarDisplayModel> TopTasks { get; set; }

	public List<CalendarDisplayModel> BotTasks { get; set; }

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
			OnPropertyChanged("IsToday");
		}
	}

	public bool IsToday => Date.Date == DateTime.Today;

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

	public static WeekDayCellViewModel Copy(WeekDayCellViewModel model)
	{
		return new WeekDayCellViewModel
		{
			Cells = model.Cells.Select(TaskCellViewModel.Copy).ToList(),
			Date = model.Date,
			TopTasks = model.TopTasks,
			BotTasks = model.BotTasks
		};
	}
}
