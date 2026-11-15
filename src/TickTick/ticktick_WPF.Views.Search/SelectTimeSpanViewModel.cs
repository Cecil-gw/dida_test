using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

public class SelectTimeSpanViewModel : BaseViewModel
{
	private DateTime? _startDate;

	private DateTime? _endDate;

	public DateTime? StartDate
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

	public DateTime? EndDate
	{
		get
		{
			return _endDate;
		}
		set
		{
			_endDate = value;
			OnPropertyChanged("EndDate");
		}
	}
}
