using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Time;

public class DatePickerViewModel : BaseViewModel
{
	private DateTime _pivotDate;

	private DateTime? _selectedDate;

	private DateTime? _selectEndDate;

	private DateTime? _selectStartDate;

	private bool _miniMode;

	private int _rowCount = 6;

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

	public bool MiniMode
	{
		get
		{
			return _miniMode;
		}
		set
		{
			_miniMode = value;
			OnPropertyChanged("MiniMode");
		}
	}

	public int RowCount
	{
		get
		{
			return _rowCount;
		}
		set
		{
			_rowCount = value;
			OnPropertyChanged("RowCount");
		}
	}
}
