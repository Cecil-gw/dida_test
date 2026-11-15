using System;
using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Views.Pomo.FocusStatistics;

namespace ticktick_WPF.ViewModels;

internal class CheckFocusRecordItemViewModel : FocusRecordItemViewModel
{
	private bool? _isSelected = false;

	public bool? IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				SelectedChanged?.Invoke(this);
			}
			NotifyPropertyChanged("IsSelected");
		}
	}

	public Action<CheckFocusRecordItemViewModel> SelectedChanged { get; set; }

	public CheckFocusRecordItemViewModel()
	{
	}

	public CheckFocusRecordItemViewModel(PomodoroModel focus, DateTime dateTime, IEnumerable<PomoTask> tasks, bool showTitle = true)
		: base(focus, dateTime, tasks, showTitle)
	{
	}

	public CheckFocusRecordItemViewModel(DateTime dateTime)
		: base(dateTime)
	{
	}
}
