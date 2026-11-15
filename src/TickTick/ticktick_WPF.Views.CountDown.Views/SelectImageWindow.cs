using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Views.Theme;

namespace ticktick_WPF.Views.CountDown.Views;

public class SelectImageWindow : Window, IComponentConnector, IStyleConnector
{
	private readonly string _customImagePath;

	private Rect _clipRect;

	private string _presetImageKey;

	private CustomThemeDialog _cropDialog;

	private string _saveImagePath;

	private string _countdownId;

	private string _attachmentId;

	internal ItemsControl ImageGallery;

	internal System.Windows.Controls.Button UnlockButton;

	private bool _contentLoaded;

	public string SelectedImagePath { get; private set; }

	public string SelectedPresetImage { get; private set; }

	public string CountdownId { get; set; }

	public event EventHandler OpenImage;

	public event EventHandler<OpenImageFinishedEventArgs> OpenImageFinished;

	public event EventHandler<string> PresetImageSelected;

	public event EventHandler<string> ImageSelected;

	public SelectImageWindow(string customImagePath = "", string imageKey = "", Rect clipRect = default(Rect))
	{
		InitializeComponent();
		_customImagePath = customImagePath;
		_clipRect = clipRect;
		_presetImageKey = imageKey;
		base.PreviewKeyDown += OnKeyDown;
		base.Loaded += OnLoaded;
	}

	private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			Close();
			e.Handled = true;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		InitPreviewGallery();
	}

	private void OnImageSelected(object sender, string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			SelectedImagePath = path;
			Close();
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	private void OnPresetImageSelected(object sender, string presetImage)
	{
		if (!string.IsNullOrEmpty(presetImage))
		{
			SelectedPresetImage = presetImage;
			Close();
		}
	}

	private void InitPreviewGallery()
	{
		ObservableCollection<SelectImageViewModel> observableCollection = new ObservableCollection<SelectImageViewModel>
		{
			new SelectImageViewModel
			{
				ImageKey = "anniversary"
			},
			new SelectImageViewModel
			{
				ImageKey = "countdown"
			},
			new SelectImageViewModel
			{
				ImageKey = "birthday"
			},
			new SelectImageViewModel
			{
				ImageKey = "holiday"
			},
			new SelectImageViewModel
			{
				ImageKey = "ring"
			},
			new SelectImageViewModel
			{
				ImageKey = "bouquets"
			},
			new SelectImageViewModel
			{
				ImageKey = "daisy"
			},
			new SelectImageViewModel
			{
				ImageKey = "wave"
			},
			new SelectImageViewModel
			{
				ImageKey = "ripples"
			},
			new SelectImageViewModel
			{
				ImageKey = "foothill"
			},
			new SelectImageViewModel
			{
				ImageKey = "journey"
			},
			new SelectImageViewModel
			{
				ImageKey = "forest"
			}
		};
		if (!string.IsNullOrEmpty(_customImagePath))
		{
			observableCollection.Add(new SelectImageViewModel
			{
				CustomImagePath = _customImagePath,
				ClipRect = _clipRect,
				Selected = true
			});
		}
		else
		{
			foreach (SelectImageViewModel item in observableCollection)
			{
				item.Selected = item.ImageKey == _presetImageKey;
			}
		}
		ImageGallery.ItemsSource = observableCollection;
	}

	public void SetAttachmentIdId(string id)
	{
		_attachmentId = id;
	}

	public void SetCountdownId(string id)
	{
		_countdownId = id;
	}

	public void SetSelectedImage(string imageKey)
	{
		if (!(ImageGallery.ItemsSource is ObservableCollection<SelectImageViewModel> observableCollection))
		{
			return;
		}
		foreach (SelectImageViewModel item in observableCollection)
		{
			item.Selected = item.ImageKey == imageKey;
		}
	}

	private void OnSelectImageClick(object sender, RoutedEventArgs e)
	{
		ChooseImage();
	}

	private bool ImageAvailable(string saveImagePath)
	{
		return Utils.LoadBitmap(saveImagePath) != null;
	}

	private void ChooseImage()
	{
		this.OpenImage?.Invoke(this, EventArgs.Empty);
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = false,
			Filter = "IMAGE (*.JPG;*.JPEG;*.PJPEG;*.PNG;*.GIF;*.PJP;*.JFIF;*.BMP;*.TIFF;*.TIF;*.DIB;*.WEBP)|*.JPG;*.JPEG;*.PJPEG;*.PNG;*.GIF;*.PJP;*.JFIF;*.BMP;*.TIFF;*.TIF;*.DIB;*.WEBP",
			FilterIndex = 1
		};
		if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			try
			{
				string fileName = openFileDialog.FileName;
				if (FileUtils.CountdownFileOverSize(fileName))
				{
					CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("AttachmentSizeLimitPro"), MessageBoxButton.OK);
					customerDialog.Owner = App.Window;
					customerDialog.ShowDialog();
					return;
				}
				string text = fileName.Split('.').LastOrDefault();
				if (!string.IsNullOrEmpty(text))
				{
					if (!Directory.Exists(AppPaths.CountdownDir))
					{
						Directory.CreateDirectory(AppPaths.CountdownDir);
					}
					string text2 = AppPaths.CountdownDir + "countdown_" + _countdownId + "." + text;
					string[] files = Directory.GetFiles(AppPaths.CountdownDir, "countdown_" + _countdownId + ".*");
					for (int i = 0; i < files.Length; i++)
					{
						FileInfo fileInfo = new FileInfo(files[i]);
						fileInfo.Attributes = FileAttributes.Normal;
						fileInfo.Delete();
					}
					File.Copy(fileName, text2);
					if (!ImageAvailable(text2))
					{
						return;
					}
					SetImage(text2);
					_saveImagePath = text2;
					ShowCropWindow();
				}
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
				this.OpenImageFinished?.Invoke(this, null);
			}
		}
		this.OpenImageFinished?.Invoke(this, null);
	}

	private void ShowCropWindow()
	{
		_cropDialog = new CustomThemeDialog(_saveImagePath, LocalSettings.Settings.CustomThemeLocation, useForCountdown: true)
		{
			Owner = Window.GetWindow(this)
		};
		_cropDialog.OnRectClip -= OnImageClip;
		_cropDialog.OnRectClip += OnImageClip;
		_cropDialog.ShowDialog();
	}

	private void OnImageClip(object sender, Rect rect)
	{
		_cropDialog.Close();
		Close();
		_cropDialog = null;
		Activate();
		this.OpenImageFinished?.Invoke(this, new OpenImageFinishedEventArgs(_saveImagePath, rect));
	}

	private void SetImage(string saveImagePath)
	{
		BitmapImage bitmapImage = new BitmapImage();
		BitmapImage bitmapImage2 = new BitmapImage();
		if (File.Exists(saveImagePath))
		{
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri(saveImagePath, UriKind.Absolute);
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage2.BeginInit();
			bitmapImage2.UriSource = new Uri(saveImagePath, UriKind.Absolute);
			bitmapImage2.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage2.EndInit();
		}
	}

	private void OnImageClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border element && (ItemsControl.ContainerFromElement(ImageGallery, element) as FrameworkElement)?.DataContext is SelectImageViewModel selectImageViewModel)
		{
			if (!string.IsNullOrEmpty(selectImageViewModel.ImageKey))
			{
				this.PresetImageSelected?.Invoke(this, selectImageViewModel.ImageKey);
			}
			if (!string.IsNullOrEmpty(selectImageViewModel.CustomImagePath))
			{
				this.OpenImageFinished?.Invoke(this, new OpenImageFinishedEventArgs(selectImageViewModel.CustomImagePath, _clipRect));
			}
			NotifyImageSelected(selectImageViewModel.ImageKey, selectImageViewModel.CustomImagePath);
		}
	}

	private void NotifyImageSelected(string imageKey, string customImagePath)
	{
		if (!(ImageGallery.ItemsSource is ObservableCollection<SelectImageViewModel> observableCollection))
		{
			return;
		}
		foreach (SelectImageViewModel item in observableCollection)
		{
			item.Selected = item.ImageKey == imageKey && item.CustomImagePath == customImagePath;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/selectimagewindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			ImageGallery = (ItemsControl)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnSelectImageClick;
			break;
		case 4:
			UnlockButton = (System.Windows.Controls.Button)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((Border)target).MouseLeftButtonUp += OnImageClick;
		}
	}
}
