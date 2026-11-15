using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Time;

public class MonthViewModel : BaseViewModel
{
	private bool _selected;

	private DateTime _date;

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
}
