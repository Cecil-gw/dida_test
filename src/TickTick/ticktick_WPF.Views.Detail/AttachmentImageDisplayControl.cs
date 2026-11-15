using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public sealed class AttachmentImageDisplayControl : Border, IComponentConnector
{
	public delegate void OnAttachementMouseEnter(object sender);

	private static readonly double _resolution = 76.0 / 135.0;

	private string _attachementId;

	private Image _optSyncError;

	private LoadingIndicator _optSyncing;

	private Image _optUpload;

	private Image _optDownload;

	private OnAttachementMouseEnter _onMouseEnter;

	internal AttachmentImageDisplayControl Root;

	internal Image ImageControl;

	internal Border StatusBorder;

	private bool _contentLoaded;

	private string successPath { get; set; }

	private double successWidth { get; set; }

	private int retryCount { get; set; }

	private AttachmentViewModel _viewModel { get; set; }

	public bool OptSyncing
	{
		get
		{
			LoadingIndicator optSyncing = _optSyncing;
			if (optSyncing == null)
			{
				return false;
			}
			return optSyncing.Visibility == Visibility.Visible;
		}
		set
		{
			if (_optSyncing == null)
			{
				_optSyncing = new LoadingIndicator
				{
					SpeedRatio = 3.0,
					IsActive = true,
					Style = ThemeUtil.GetStyle("LoadingIndicatorRingStyle"),
					Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80"),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
			}
			if (value)
			{
				HideAllOption();
				StatusBorder.Child = _optSyncing;
				StatusBorder.Visibility = Visibility.Visible;
			}
			_optSyncing.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
			SetOptBorderVisibility();
		}
	}

	public bool OptSyncError
	{
		get
		{
			return _optSyncError.Visibility == Visibility.Visible;
		}
		set
		{
			SetOptPath(ref _optSyncError, "AttSyncErrorDrawingImage", value);
		}
	}

	public bool OptDownload
	{
		get
		{
			return _optDownload.Visibility == Visibility.Visible;
		}
		set
		{
			SetOptPath(ref _optDownload, "AttSyncErrorDrawingImage", value);
		}
	}

	public bool OptUpload
	{
		get
		{
			return _optUpload.Visibility == Visibility.Visible;
		}
		set
		{
			SetOptPath(ref _optUpload, "AttImageUploadDrawingImage", value);
		}
	}

	public AttachmentImageDisplayControl(AttachmentViewModel viewModel, OnAttachementMouseEnter onMouseEnter)
	{
		InitializeComponent();
		base.HorizontalAlignment = HorizontalAlignment.Left;
		if (viewModel == null)
		{
			return;
		}
		PropertyChangedEventManager.AddHandler(viewModel, delegate(object sender, PropertyChangedEventArgs args)
		{
			if (sender is AttachmentViewModel attachmentViewModel2)
			{
				SwitchType(attachmentViewModel2.FileStatus);
			}
		}, "FileStatus");
		base.DataContext = (_viewModel = viewModel);
		_attachementId = viewModel.Id;
		_onMouseEnter = onMouseEnter;
		SwitchType(viewModel.FileStatus);
		UpdateImage();
	}

	private void SwitchType(OptTypeEnum type)
	{
		switch (type)
		{
		case OptTypeEnum.Download:
			OptDownload = true;
			break;
		case OptTypeEnum.Upload:
			OptUpload = true;
			break;
		case OptTypeEnum.Loading:
			OptSyncing = true;
			break;
		case OptTypeEnum.DownloadFailed:
		case OptTypeEnum.UploadFailed:
			OptSyncError = true;
			break;
		default:
			HideAllOption();
			SetOptBorderVisibility();
			break;
		}
	}

	public async Task UpdateImage()
	{
		if (ImageControl.Source != null && !(successPath != _viewModel.LocalPath) && successWidth == _viewModel.Width)
		{
			return;
		}
		ImageControl.Source = null;
		ImageControl.Source = GetImage();
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			Rect rect = new Rect(0.0, 0.0, _viewModel.Width, _viewModel.Height);
			Console.WriteLine(rect);
			ImageControl.Clip = new RectangleGeometry(rect, _viewModel.RadiusX, _viewModel.RadiusY);
		}, DispatcherPriority.Render);
		if (successWidth <= 0.0)
		{
			if (retryCount < 10)
			{
				retryCount++;
				await Task.Delay(500);
				UpdateImage();
			}
		}
		else
		{
			retryCount = 0;
		}
	}

	private BitmapImage GetImage()
	{
		if (_viewModel == null)
		{
			return ThemeUtil.GetPlaceholderAttachmentImage();
		}
		string localPath = _viewModel.LocalPath;
		double width = _viewModel.Width;
		bool flag = false;
		int width2 = ((int)(width * 1.5) / 1000 + 1) * 1000;
		if (!FileUtils.FileEmptyOrNotExists(localPath))
		{
			try
			{
				BitmapImage image = ThemeUtil.GetImage(localPath, width2);
				successPath = localPath;
				successWidth = width;
				return image;
			}
			catch (Exception arg)
			{
				UtilLog.Error($"PathImageConverter.Convert Read Image Failed, {arg}");
				flag = true;
			}
		}
		return ThemeUtil.GetPlaceholderAttachmentImage((!string.IsNullOrEmpty(localPath) && localPath.StartsWith("failed", StringComparison.OrdinalIgnoreCase)) || flag);
	}

	private void SetOptPath(ref Image path, string sourceKey, bool vis)
	{
		if (path == null)
		{
			DrawingImage imageSource = Utils.GetImageSource(sourceKey);
			if (imageSource.CanFreeze)
			{
				imageSource.Freeze();
			}
			path = new Image
			{
				Source = imageSource,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
		}
		if (vis)
		{
			HideAllOption();
			StatusBorder.Child = path;
			StatusBorder.Visibility = Visibility.Visible;
		}
		path.Visibility = ((!vis) ? Visibility.Collapsed : Visibility.Visible);
		SetOptBorderVisibility();
	}

	private void SetOptBorderVisibility()
	{
		if (_optUpload != null && _optUpload.Visibility == Visibility.Visible)
		{
			StatusBorder.Background = new SolidColorBrush(Color.FromRgb(19, 19, 19))
			{
				Opacity = 0.3
			};
		}
		else
		{
			StatusBorder.Background = Brushes.Transparent;
		}
	}

	private void HideAllOption()
	{
		if (_optDownload != null)
		{
			_optDownload.Visibility = Visibility.Collapsed;
		}
		if (_optUpload != null)
		{
			_optUpload.Visibility = Visibility.Collapsed;
		}
		if (_optSyncError != null)
		{
			_optSyncError.Visibility = Visibility.Collapsed;
		}
		if (_optSyncing != null)
		{
			_optSyncing.Visibility = Visibility.Collapsed;
		}
	}

	private async void OnMouseEnter(object sender, MouseEventArgs e)
	{
		await Task.Delay(100);
		if (sender is Border { IsMouseOver: not false })
		{
			_onMouseEnter?.Invoke(sender);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/attachmentimagedisplaycontrol.xaml", UriKind.Relative);
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
			Root = (AttachmentImageDisplayControl)target;
			Root.MouseEnter += OnMouseEnter;
			break;
		case 2:
			ImageControl = (Image)target;
			break;
		case 3:
			StatusBorder = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
