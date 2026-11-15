using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class TimeLineViewModel : BaseViewModel
{
	private DateTime _startDate;

	public DateTime StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			_startDate = value;
			OnPropertyChanged("StartDate");
		}
	}
}
