using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Views.CountDown.Infrastructure;

namespace ticktick_WPF.Views.CountDown.Views;

public class SelectImageViewModel : ObservableObject
{
	private bool _isSelected;

	private ImageBrush _backgroundImage;

	private Rect _clipRect;

	public string ImageKey { get; set; }

	public string CustomImagePath { get; set; }

	public bool Selected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			SetProperty(ref _isSelected, value, "Selected");
		}
	}

	public ImageBrush BackgroundImage
	{
		get
		{
			return _backgroundImage;
		}
		set
		{
			SetProperty(ref _backgroundImage, value, "BackgroundImage");
		}
	}

	public Rect ClipRect
	{
		get
		{
			return _clipRect;
		}
		set
		{
			SetProperty(ref _clipRect, value, "ClipRect");
		}
	}
}
