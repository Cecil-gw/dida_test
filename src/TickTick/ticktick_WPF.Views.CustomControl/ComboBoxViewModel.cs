using System.Windows.Media;

namespace ticktick_WPF.Views.CustomControl;

public class ComboBoxViewModel : UpDownSelectViewModel
{
	private string _title;

	private string _selectedTitle;

	private Geometry _image;

	private bool _showImage;

	private bool _showTopSeparator;

	private bool _isHeader;

	private double _itemHeight = 36.0;

	public bool CanSelect = true;

	public object Value;

	public double ItemHeight
	{
		get
		{
			return _itemHeight;
		}
		set
		{
			_itemHeight = value;
			OnPropertyChanged("ItemHeight");
		}
	}

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

	public string SelectedTitle
	{
		get
		{
			return _selectedTitle;
		}
		set
		{
			_selectedTitle = value;
			OnPropertyChanged("SelectedTitle");
		}
	}

	public Geometry Image
	{
		get
		{
			return _image;
		}
		set
		{
			_image = value;
			OnPropertyChanged("Image");
		}
	}

	public bool ShowImage
	{
		get
		{
			return _showImage;
		}
		set
		{
			_showImage = value;
			OnPropertyChanged("ShowImage");
		}
	}

	public bool ShowTopSeparator
	{
		get
		{
			return _showTopSeparator;
		}
		set
		{
			_showTopSeparator = value;
			OnPropertyChanged("ShowTopSeparator");
		}
	}

	public bool IsHeader
	{
		get
		{
			return _isHeader;
		}
		set
		{
			_isHeader = value;
			OnPropertyChanged("IsHeader");
		}
	}

	public ComboBoxViewModel()
	{
	}

	public ComboBoxViewModel(object value, string title, double height = 36.0)
	{
		Value = value;
		Title = title;
		SelectedTitle = title;
		ItemHeight = height;
	}
}
