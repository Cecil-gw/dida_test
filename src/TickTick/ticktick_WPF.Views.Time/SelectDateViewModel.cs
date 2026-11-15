using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Time;

public class SelectDateViewModel : BaseViewModel
{
	private DateTime _selectedDate;

	public DateTime SelectedDate
	{
		get
		{
			return _selectedDate;
		}
		set
		{
			_selectedDate = value;
			OnPropertyChanged("SelectedDate");
		}
	}
}
