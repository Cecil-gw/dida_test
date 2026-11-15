using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Misc;

public class GroupTitleViewModel : BaseViewModel
{
	private string _title;

	public object _tag;

	private string _content = string.Empty;

	private bool _isSelected;

	private bool _isTabSelected;

	private double _maxWidth = 1000.0;

	private bool _isNoBackStyle;

	public int Index { get; set; }

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public object Tag
	{
		get
		{
			return _tag;
		}
		set
		{
			_tag = value;
			OnPropertyChanged("Tag");
		}
	}

	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			OnPropertyChanged("Content");
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
			_isSelected = value;
			OnPropertyChanged("IsSelected");
		}
	}

	public bool IsTabSelected
	{
		get
		{
			return _isTabSelected;
		}
		set
		{
			_isTabSelected = value;
			OnPropertyChanged("IsTabSelected");
		}
	}

	public double MaxWidth
	{
		get
		{
			return _maxWidth;
		}
		set
		{
			_maxWidth = value;
			OnPropertyChanged("MaxWidth");
		}
	}

	public bool IsNoBackStyle
	{
		get
		{
			return _isNoBackStyle;
		}
		set
		{
			_isNoBackStyle = value;
			OnPropertyChanged("IsNoBackStyle");
		}
	}
}
