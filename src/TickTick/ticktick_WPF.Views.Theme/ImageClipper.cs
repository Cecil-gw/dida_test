using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class ImageClipper : UserControl, IComponentConnector
{
	public delegate void CutImageDelegate(BitmapSource bit);

	private enum DragPointMode
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
		Top,
		Bottom,
		Left,
		Right
	}

	public CutImageDelegate OnCutImage;

	private double _clipRatio = 1.6;

	private BitmapImage _bitSource;

	private double BorderWidth = 1.0;

	private bool _useForCountdown;

	private bool _clipBorderMouseDown;

	private DragPointMode _pointMode;

	private bool _imageAreaMouseDown;

	private Point _imageAreaMouseDownPoint = new Point(-1.0, -1.0);

	private BitmapImage _bitmapImage;

	private PixelColor[,] _colors;

	internal Image SourceImage;

	internal System.Windows.Shapes.Path MaskPath;

	internal RectangleGeometry RectOutside;

	internal RectangleGeometry RectInside;

	internal Canvas Container;

	internal Border ImageArea;

	internal Border ClipBorder;

	private bool _contentLoaded;

	public double MaxImageWidth { get; set; } = 640.0;

	public double MaxImageHeight { get; set; } = 400.0;

	public BitmapImage BitSource
	{
		get
		{
			return _bitSource;
		}
		set
		{
			_bitSource = value;
			SourceImage.Source = value;
		}
	}

	public event EventHandler<List<SelectThemeColorViewModel>> OnThemeColorsChanged;

	public ImageClipper()
	{
		InitializeComponent();
	}

	public void SetImage(string imageUrl, string location = null, bool reLoadTheme = true)
	{
		try
		{
			LoadBitmap(imageUrl);
		}
		catch (Exception)
		{
		}
		if (_bitmapImage == null)
		{
			return;
		}
		double num = MaxImageWidth;
		double height = MaxImageHeight;
		if (_bitmapImage != null && _bitmapImage.Height > 0.0)
		{
			double num2 = _bitmapImage.Width / _bitmapImage.Height;
			if (num2 > _clipRatio)
			{
				num = MaxImageWidth;
				height = MaxImageWidth / num2;
			}
			else if (num2 < _clipRatio)
			{
				num = MaxImageHeight * num2;
				height = MaxImageHeight;
			}
			else
			{
				num = MaxImageWidth;
				height = MaxImageHeight;
			}
		}
		SourceImage.Width = num;
		SourceImage.Height = height;
		SourceImage.Source = _bitmapImage;
		Container.Width = num;
		Container.Height = height;
		Rect imageRect = GetImageRect(num, height, location);
		ImageArea.Width = imageRect.Width;
		ImageArea.Height = imageRect.Height;
		Canvas.SetLeft(ImageArea, imageRect.Left);
		Canvas.SetTop(ImageArea, imageRect.Top);
		double left = (MaxImageWidth - num) / 2.0;
		MaskPath.Margin = new Thickness(left, 0.0, 0.0, 0.0);
		RectOutside.Rect = new Rect(0.0, 0.0, num, height);
		RectInside.Rect = imageRect;
		if (reLoadTheme)
		{
			GetThemeColor();
			TrySetTheme(widthImage: true);
		}
	}

	private void GetThemeColor()
	{
		List<SelectThemeColorViewModel> list = new List<SelectThemeColorViewModel>();
		if (_colors != null && _colors.Length > 0)
		{
			Rect currentLocation = GetCurrentLocation();
			currentLocation.Width /= 3.0;
			Dictionary<PixelColor, long> dictionary = new Dictionary<PixelColor, long>();
			int num = Math.Max(0, (int)(currentLocation.Left * (double)_colors.GetLength(1)));
			int num2 = Math.Min(_colors.GetLength(1), (int)(currentLocation.Right * (double)_colors.GetLength(1)));
			int num3 = Math.Max(0, (int)(currentLocation.Top * (double)_colors.GetLength(0)));
			int num4 = Math.Min(_colors.GetLength(0), (int)(currentLocation.Bottom * (double)_colors.GetLength(0)));
			for (int i = num; i < num2; i++)
			{
				for (int j = num3; j < num4; j++)
				{
					PixelColor key = _colors[j, i];
					if (!key.IsTooLight())
					{
						if (dictionary.ContainsKey(key))
						{
							dictionary[key]++;
						}
						else
						{
							dictionary[key] = 1L;
						}
					}
				}
			}
			List<KeyValuePair<PixelColor, long>> list2 = dictionary.ToList();
			list2.Sort((KeyValuePair<PixelColor, long> a, KeyValuePair<PixelColor, long> b) => b.Value.CompareTo(a.Value));
			if (list2.Count > 3)
			{
				list2 = list2.Take(3).ToList();
			}
			foreach (KeyValuePair<PixelColor, long> item in list2)
			{
				SolidColorBrush solidColorBrush = item.Key.ToColorBrush();
				list.Add(new SelectThemeColorViewModel
				{
					Color = solidColorBrush.Color.ToString()
				});
			}
		}
		foreach (string color in new List<string> { "#FF4772FA", "#FFFF739F", "#FFFFA14D", "#FF00C598", "#FF000000", "#FF7D7F84" })
		{
			if (list.All((SelectThemeColorViewModel c) => c.Color != color))
			{
				list.Add(new SelectThemeColorViewModel
				{
					Color = color
				});
			}
		}
		this.OnThemeColorsChanged?.Invoke(this, list);
	}

	private Rect GetImageRect(double width, double height, string location)
	{
		double num = width / height;
		double num2 = width;
		double num3 = height;
		Rect rectByString = Utils.GetRectByString(location);
		double num4;
		double num5;
		if (rectByString.Height == 0.0 || rectByString.Width == 0.0)
		{
			num2 = ((num > _clipRatio) ? (height * _clipRatio) : width);
			num3 = ((num > _clipRatio) ? height : (width / _clipRatio));
			num4 = (width - num2) / 2.0;
			num5 = (height - num3) / 2.0;
		}
		else
		{
			num4 = width * rectByString.Left;
			num5 = height * rectByString.Top;
			num2 = width * rectByString.Width;
			num3 = height * rectByString.Height;
		}
		num2 = Math.Min(width - num4, num2);
		num3 = Math.Min(height - num5, num3);
		return new Rect(num4, num5, num2, num3);
	}

	private void OnClipMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Released)
		{
			if (_imageAreaMouseDownPoint.X > 0.0)
			{
				_imageAreaMouseDownPoint = new Point(-1.0, -1.0);
			}
			if (ClipBorder.IsMouseOver)
			{
				Point position = e.GetPosition(ClipBorder);
				bool flag = position.Y <= 6.0;
				bool flag2 = position.X <= 6.0;
				bool flag3 = position.Y >= ClipBorder.ActualHeight - 6.0;
				bool flag4 = position.X >= ClipBorder.ActualWidth - 6.0;
				if ((flag && flag2) || (flag3 && flag4))
				{
					Mouse.OverrideCursor = Cursors.SizeNWSE;
					_pointMode = ((!flag) ? DragPointMode.BottomRight : DragPointMode.TopLeft);
				}
				else if ((flag && flag4) || (flag3 && flag2))
				{
					Mouse.OverrideCursor = Cursors.SizeNESW;
					_pointMode = (flag ? DragPointMode.TopRight : DragPointMode.BottomLeft);
				}
				else if (flag || flag3)
				{
					Mouse.OverrideCursor = Cursors.SizeNS;
					_pointMode = (flag ? DragPointMode.Top : DragPointMode.Bottom);
				}
				else if (flag4 || flag2)
				{
					Mouse.OverrideCursor = Cursors.SizeWE;
					_pointMode = (flag2 ? DragPointMode.Left : DragPointMode.Right);
				}
			}
			else
			{
				Mouse.OverrideCursor = null;
			}
		}
		else if (_clipBorderMouseDown)
		{
			Point position2 = e.GetPosition(Container);
			switch (_pointMode)
			{
			case DragPointMode.TopLeft:
			{
				Point targetPoint8 = new Point(Canvas.GetLeft(ImageArea) + ImageArea.Width, Canvas.GetTop(ImageArea) + ImageArea.Height);
				position2.X = Math.Min(position2.X, targetPoint8.X);
				position2.Y = Math.Min(position2.Y, targetPoint8.Y);
				SetImageArea(targetPoint8, position2, _pointMode);
				break;
			}
			case DragPointMode.TopRight:
			{
				Point targetPoint7 = new Point(Canvas.GetLeft(ImageArea), Canvas.GetTop(ImageArea) + ImageArea.Height);
				position2.X = Math.Max(position2.X, targetPoint7.X);
				position2.Y = Math.Min(position2.Y, targetPoint7.Y);
				SetImageArea(targetPoint7, position2, _pointMode);
				break;
			}
			case DragPointMode.BottomLeft:
			{
				Point targetPoint6 = new Point(Canvas.GetLeft(ImageArea) + ImageArea.Width, Canvas.GetTop(ImageArea));
				position2.X = Math.Min(position2.X, targetPoint6.X);
				position2.Y = Math.Max(position2.Y, targetPoint6.Y);
				SetImageArea(targetPoint6, position2, _pointMode);
				break;
			}
			case DragPointMode.BottomRight:
			{
				Point targetPoint5 = new Point(Canvas.GetLeft(ImageArea), Canvas.GetTop(ImageArea));
				position2.X = Math.Max(position2.X, targetPoint5.X);
				position2.Y = Math.Max(position2.Y, targetPoint5.Y);
				SetImageArea(targetPoint5, position2, _pointMode);
				break;
			}
			case DragPointMode.Top:
			{
				Point targetPoint4 = new Point(Canvas.GetLeft(ImageArea), Canvas.GetTop(ImageArea) + ImageArea.Height);
				position2.X = targetPoint4.X;
				position2.Y = Math.Min(position2.Y, targetPoint4.Y);
				SetImageArea(targetPoint4, position2, _pointMode);
				break;
			}
			case DragPointMode.Bottom:
			{
				Point targetPoint3 = new Point(Canvas.GetLeft(ImageArea), Canvas.GetTop(ImageArea));
				position2.X = targetPoint3.X;
				position2.Y = Math.Max(position2.Y, targetPoint3.Y);
				SetImageArea(targetPoint3, position2, _pointMode);
				break;
			}
			case DragPointMode.Left:
			{
				Point targetPoint2 = new Point(Canvas.GetLeft(ImageArea) + ImageArea.Width, Canvas.GetTop(ImageArea));
				position2.X = Math.Min(position2.X, targetPoint2.X);
				position2.Y = targetPoint2.Y;
				SetImageArea(targetPoint2, position2, _pointMode);
				break;
			}
			case DragPointMode.Right:
			{
				Point targetPoint = new Point(Canvas.GetLeft(ImageArea), Canvas.GetTop(ImageArea));
				position2.X = Math.Max(position2.X, targetPoint.X);
				position2.Y = targetPoint.Y;
				SetImageArea(targetPoint, position2, _pointMode);
				break;
			}
			}
			TrySetTheme();
		}
		else if (_imageAreaMouseDownPoint.X > 0.0 && _imageAreaMouseDownPoint.Y > 0.0)
		{
			double val = Container.Width - ImageArea.Width;
			double val2 = Container.Height - ImageArea.Height;
			Point position3 = e.GetPosition(Container);
			double length = Math.Max(Math.Min(val, position3.X - _imageAreaMouseDownPoint.X), 0.0);
			double length2 = Math.Max(Math.Min(val2, position3.Y - _imageAreaMouseDownPoint.Y), 0.0);
			Canvas.SetLeft(ImageArea, length);
			Canvas.SetTop(ImageArea, length2);
			GetInsideRect();
			TrySetTheme();
		}
	}

	private void SetImageArea(Point targetPoint, Point point, DragPointMode pointMode)
	{
		double x = targetPoint.X;
		double num = Math.Min(Container.Width, Math.Max(point.X, 0.0));
		bool flag = pointMode == DragPointMode.TopLeft || pointMode == DragPointMode.BottomLeft || pointMode == DragPointMode.Left;
		double y = targetPoint.Y;
		double num2 = Math.Min(Container.Height, Math.Max(point.Y, 0.0));
		bool flag2 = pointMode == DragPointMode.TopLeft || pointMode == DragPointMode.TopRight || pointMode == DragPointMode.Top;
		double num3 = (flag ? targetPoint.X : (Container.Width - targetPoint.X));
		double val = Math.Min(flag2 ? targetPoint.Y : (Container.Height - targetPoint.Y), num3 / 1.6);
		double num4 = Math.Max(16.0, Math.Min(Math.Abs(num - x) / 1.6, val));
		double num5 = Math.Max(10.0, Math.Min(Math.Abs(num2 - y), val));
		if (num4 > num5)
		{
			double num6 = num4 * 1.6;
			ImageArea.Width = num6;
			ImageArea.Height = num4;
			if (flag)
			{
				Canvas.SetLeft(ImageArea, x - num6);
			}
			if (flag2)
			{
				double val2 = y - num4;
				Canvas.SetTop(ImageArea, Math.Max(val2, 0.0));
			}
		}
		else
		{
			double num7 = num5 * 1.6;
			ImageArea.Width = num7;
			ImageArea.Height = num5;
			if (flag)
			{
				double val3 = x - num7;
				Canvas.SetLeft(ImageArea, Math.Max(val3, 0.0));
			}
			if (flag2)
			{
				Canvas.SetTop(ImageArea, y - num5);
			}
		}
		GetInsideRect();
	}

	private void GetInsideRect()
	{
		RectInside.Rect = new Rect(Canvas.GetLeft(ImageArea), Canvas.GetTop(ImageArea), ImageArea.Width, ImageArea.Height);
	}

	private void OnClipBorderMouseDown(object sender, MouseButtonEventArgs e)
	{
		_clipBorderMouseDown = true;
		CaptureMouse();
	}

	private void OnImageAreaMouseDown(object sender, MouseButtonEventArgs e)
	{
		_imageAreaMouseDownPoint = e.GetPosition(ImageArea);
	}

	private void OnClipMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_imageAreaMouseDownPoint.X > 0.0 || _imageAreaMouseDownPoint.Y > 0.0 || base.IsMouseCaptured)
		{
			GetThemeColor();
		}
		_clipBorderMouseDown = false;
		_imageAreaMouseDownPoint = new Point(-1.0, -1.0);
		if (base.IsMouseCaptured)
		{
			ReleaseMouseCapture();
		}
	}

	private void TrySetTheme(bool widthImage = false)
	{
		if (!_useForCountdown)
		{
			Rect currentLocation = GetCurrentLocation();
			App.Window.SetCustomTheme(widthImage ? _bitmapImage : null, currentLocation);
		}
	}

	public Rect GetCurrentActualLocation()
	{
		double left = Canvas.GetLeft(ImageArea);
		double top = Canvas.GetTop(ImageArea);
		double width = ImageArea.Width;
		double height = ImageArea.Height;
		return new Rect(new Point(left, top), new Point(left + width, top + height));
	}

	public Rect GetCurrentLocation()
	{
		double width = Container.Width;
		double height = Container.Height;
		double x = Canvas.GetLeft(ImageArea) / width;
		double y = Canvas.GetTop(ImageArea) / height;
		double width2 = ImageArea.Width / width;
		double height2 = ImageArea.Height / height;
		return new Rect(x, y, width2, height2);
	}

	private void LoadBitmap(string imagePath)
	{
		SourceImage.Source = null;
		_bitmapImage = null;
		try
		{
			if (File.Exists(imagePath))
			{
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
				using Stream streamSource = new MemoryStream(File.ReadAllBytes(imagePath));
				bitmapImage.StreamSource = streamSource;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				_bitmapImage = bitmapImage;
				_colors = ImageColorUtils.GetPixels(bitmapImage);
				return;
			}
		}
		catch (ArgumentException)
		{
		}
		catch (IOException)
		{
		}
	}

	private void ImageClipper_OnMouseLeave(object sender, MouseEventArgs e)
	{
		Mouse.OverrideCursor = null;
	}

	public void SetUseForCountdown(bool useForCountdown)
	{
		_useForCountdown = useForCountdown;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/imageclipper.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((ImageClipper)target).MouseMove += OnClipMouseMove;
			((ImageClipper)target).MouseLeftButtonUp += OnClipMouseUp;
			((ImageClipper)target).MouseLeave += ImageClipper_OnMouseLeave;
			break;
		case 2:
			SourceImage = (Image)target;
			break;
		case 3:
			MaskPath = (System.Windows.Shapes.Path)target;
			break;
		case 4:
			RectOutside = (RectangleGeometry)target;
			break;
		case 5:
			RectInside = (RectangleGeometry)target;
			break;
		case 6:
			Container = (Canvas)target;
			break;
		case 7:
			ImageArea = (Border)target;
			ImageArea.MouseLeftButtonDown += OnImageAreaMouseDown;
			break;
		case 8:
			ClipBorder = (Border)target;
			ClipBorder.MouseLeftButtonDown += OnClipBorderMouseDown;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
