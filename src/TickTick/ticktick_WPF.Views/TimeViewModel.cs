using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public class TimeViewModel : BaseViewModel
{
	private bool _selected;

	private bool _suggested;

	private bool _showSpanText;

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

	public bool Suggested
	{
		get
		{
			return _suggested;
		}
		set
		{
			_suggested = value;
			OnPropertyChanged("Suggested");
		}
	}

	public string DisplayText { get; set; }

	public string TimeSpanText { get; set; }

	public bool ShowSpanText
	{
		get
		{
			return _showSpanText;
		}
		set
		{
			_showSpanText = value;
			OnPropertyChanged("ShowSpanText");
		}
	}

	public DateTime Value { get; set; }
}
