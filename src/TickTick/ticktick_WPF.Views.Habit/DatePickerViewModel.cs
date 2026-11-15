using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class DatePickerViewModel : BaseViewModel
{
	private DateTime _pivotDate;

	private DateTime? _selectedDate;

	private DateTime? _selectEndDate;

	private DateTime? _selectStartDate;

	public DateTime PivotDate
	{
		get
		{
			return _pivotDate;
		}
		set
		{
			_pivotDate = value;
			OnPropertyChanged("PivotDate");
		}
	}

	public DateTime? SelectedDate
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

	public DateTime? SelectStartDate
	{
		get
		{
			return _selectStartDate;
		}
		set
		{
			_selectStartDate = value;
			OnPropertyChanged("SelectStartDate");
		}
	}

	public DateTime? SelectEndDate
	{
		get
		{
			return _selectEndDate;
		}
		set
		{
			_selectEndDate = value;
			OnPropertyChanged("SelectEndDate");
		}
	}
}
