using System.ComponentModel;

namespace ticktick_WPF.Views.CountDown.Views;

public class HolidayItem : INotifyPropertyChanged
{
	private string _name;

	private string _date;

	private string _imageSource;

	private string _iconBackground;

	private bool _isSelected;

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (_name != value)
			{
				_name = value;
				OnPropertyChanged("Name");
			}
		}
	}

	public string Date
	{
		get
		{
			return _date;
		}
		set
		{
			if (_date != value)
			{
				_date = value;
				OnPropertyChanged("Date");
			}
		}
	}

	public int TargetDate { get; set; }

	public string RepeatFlag { get; set; }

	public string ImageSource
	{
		get
		{
			return _imageSource;
		}
		set
		{
			if (_imageSource != value)
			{
				_imageSource = value;
				OnPropertyChanged("ImageSource");
			}
		}
	}

	public string IconBackground
	{
		get
		{
			return _iconBackground;
		}
		set
		{
			if (_iconBackground != value)
			{
				_iconBackground = value;
				OnPropertyChanged("IconBackground");
			}
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChanged("IsSelected");
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public HolidayItem(string name, string dateText, string imageSource, string iconBackground, int targetDate, string repeatFlag)
	{
		Name = name;
		Date = dateText;
		ImageSource = imageSource;
		IconBackground = iconBackground;
		IsSelected = false;
		TargetDate = targetDate;
		RepeatFlag = repeatFlag;
	}

	protected void OnPropertyChanged(string propertyName)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
