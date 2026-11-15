using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public class TaskProgress : BaseViewModel
{
	private int _percent;

	private int _previewPercent;

	private bool _showPointer;

	private double _width;

	public double Width
	{
		get
		{
			return _width;
		}
		set
		{
			_width = value;
			OnPropertyChanged("Width");
		}
	}

	public int PreviewPercent
	{
		get
		{
			return _previewPercent;
		}
		set
		{
			_previewPercent = value;
			OnPropertyChanged("PreviewPercent");
		}
	}

	public int Percent
	{
		get
		{
			return _percent;
		}
		set
		{
			_percent = value;
			OnPropertyChanged("Percent");
		}
	}

	public bool ShowPointer
	{
		get
		{
			return _showPointer;
		}
		set
		{
			_showPointer = value;
			OnPropertyChanged("ShowPointer");
		}
	}
}
