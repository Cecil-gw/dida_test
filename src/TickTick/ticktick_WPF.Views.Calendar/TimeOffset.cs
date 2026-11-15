using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class TimeOffset : BaseViewModel
{
	private bool _foldExpanded;

	private bool _showBottomHandle;

	private bool _showTopHandle;

	private bool _isFirst;

	public int Offset { get; set; }

	public int BaseOffset { get; set; }

	public bool Folded { get; set; }

	public bool ShowDrag { get; set; }

	public bool ShowDivider { get; set; }

	public bool IsFirst
	{
		get
		{
			return _isFirst;
		}
		set
		{
			_isFirst = value;
			OnPropertyChanged("IsFirst");
		}
	}

	public bool ShowBottomHandle
	{
		get
		{
			if (_showBottomHandle)
			{
				return ShowDrag;
			}
			return false;
		}
		set
		{
			_showBottomHandle = value;
			OnPropertyChanged("ShowBottomHandle");
		}
	}

	public bool ShowTopHandle
	{
		get
		{
			if (_showTopHandle)
			{
				return ShowDrag;
			}
			return false;
		}
		set
		{
			_showTopHandle = value;
			OnPropertyChanged("ShowTopHandle");
		}
	}

	public bool FoldExpanded
	{
		get
		{
			return _foldExpanded;
		}
		set
		{
			_foldExpanded = value;
			OnPropertyChanged("FoldExpanded");
		}
	}

	public DateTime DateTime { get; set; }

	public TimeZoneInfo TimeZone { get; set; }

	public TimeOffset Clone()
	{
		return (TimeOffset)MemberwiseClone();
	}
}
