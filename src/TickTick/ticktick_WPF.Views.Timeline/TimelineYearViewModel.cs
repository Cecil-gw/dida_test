using System;
using System.Windows;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Timeline;

public class TimelineYearViewModel : BaseViewModel
{
	private string _yearName;

	private double _left;

	private Visibility _visibility;

	public string YearName
	{
		get
		{
			return _yearName;
		}
		set
		{
			_yearName = value;
			OnPropertyChanged("YearName");
		}
	}

	public double Left
	{
		get
		{
			return _left;
		}
		set
		{
			_left = value;
			OnPropertyChanged("Left");
		}
	}

	public Visibility Visibility
	{
		get
		{
			return _visibility;
		}
		set
		{
			_visibility = value;
			OnPropertyChanged("Visibility");
		}
	}

	public override bool Equals(object obj)
	{
		if (this == obj)
		{
			return true;
		}
		if (obj is TimelineYearViewModel timelineYearViewModel)
		{
			if (YearName == timelineYearViewModel.YearName && Math.Abs(Left - timelineYearViewModel.Left) < 0.1)
			{
				return Visibility == timelineYearViewModel.Visibility;
			}
			return false;
		}
		return false;
	}
}
