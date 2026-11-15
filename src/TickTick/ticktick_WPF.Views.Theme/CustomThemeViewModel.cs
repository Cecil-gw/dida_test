using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class CustomThemeViewModel : ThemeBaseModel
{
	private ImageSource _image;

	private Rect _viewBox = new Rect(0.0, 0.0, 1.0, 1.0);

	public bool IsEmpty => Image == null;

	public ImageSource Image
	{
		get
		{
			return _image;
		}
		set
		{
			_image = value;
			OnPropertyChanged("Image");
			OnPropertyChanged("IsEmpty");
		}
	}

	public Rect ViewBox
	{
		get
		{
			return _viewBox;
		}
		set
		{
			_viewBox = value;
			OnPropertyChanged("ViewBox");
		}
	}

	public CustomThemeViewModel()
	{
		base.Key = "Custom";
		base.Selected = "Custom" == LocalSettings.Settings.ThemeId;
		base.Name = Utils.GetString("Custom");
		if (!Directory.Exists(AppPaths.ThemeDir))
		{
			Directory.CreateDirectory(AppPaths.ThemeDir);
		}
		SetImage();
	}

	public void SetImage()
	{
		if (!UserDao.IsPro())
		{
			Image = null;
			return;
		}
		List<string> list = Directory.GetFiles(AppPaths.ThemeDir, "custom*.*").ToList();
		_viewBox = Utils.GetRectByString(LocalSettings.Settings.Common.CustomThemeLocation);
		if (_viewBox.Width == 0.0 || _viewBox.Height == 0.0)
		{
			_viewBox = new Rect(0.0, 0.0, 1.0, 1.0);
		}
		OnPropertyChanged("ViewBox");
		if (list.Count > 0 && new FileInfo(list[0]).Length > 0)
		{
			BitmapImage image = Utils.LoadBitmap(list[0], 320, 200);
			Image = image;
		}
	}
}
