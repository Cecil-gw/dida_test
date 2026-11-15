using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Converter;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public class AttachmentFileDisplayControl : Border, IComponentConnector
{
	private static readonly AttachmentIconConverter _attachmentIconConverter = new AttachmentIconConverter();

	private static readonly AttachmentSizedConverter _attachmentSizedConverter = new AttachmentSizedConverter();

	private static readonly AttachmentBackgroundConverter _attachmentBackgroundConverter = new AttachmentBackgroundConverter();

	private string _fileType;

	private AttachmentConvertParam _convertParam;

	private Image _optSyncError;

	private LoadingIndicator _optSyncing;

	private Image _optUpload;

	private Image _optDownload;

	private bool _optSynced;

	private bool _isDark;

	internal Grid _mainGrid;

	internal Image _iconImage;

	internal Grid infoGrid;

	internal TextBlock nameBlock;

	internal TextBlock sizeBlock;

	internal Border _optBorder;

	internal Grid HintView;

	internal Popup AIRecHintPopup;

	internal Border PreviewAIRecBorder;

	internal StackPanel DescribePanel;

	internal TextBlock DescribeText;

	internal Button DescribeButton;

	private bool _contentLoaded;

	private AttachmentViewModel _model { get; set; }

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
			if (_optSyncing == null && value)
			{
				_optSyncing = new LoadingIndicator
				{
					SpeedRatio = 3.0,
					IsActive = true,
					Style = ThemeUtil.GetStyle("LoadingIndicatorRingStyle"),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				_optSyncing.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity100_80");
			}
			if (value)
			{
				HideAllOption();
				_optBorder.Child = _optSyncing;
			}
			if (_optSyncing != null)
			{
				_optSyncing.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
			}
		}
	}

	public bool OptSynced
	{
		get
		{
			return _optSynced;
		}
		set
		{
			_optSynced = value;
			if (value)
			{
				HideAllOption();
				_convertParam.IsFailed = false;
				ResetIconAndBackground();
			}
		}
	}

	public bool OptSyncError
	{
		get
		{
			Image optSyncError = _optSyncError;
			if (optSyncError == null)
			{
				return false;
			}
			return optSyncError.Visibility == Visibility.Visible;
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
			Image optDownload = _optDownload;
			if (optDownload == null)
			{
				return false;
			}
			return optDownload.Visibility == Visibility.Visible;
		}
		set
		{
			SetOptPath(ref _optDownload, "AttDownloadDrawingImage", value);
		}
	}

	public bool OptUpload
	{
		get
		{
			Image optUpload = _optUpload;
			if (optUpload == null)
			{
				return false;
			}
			return optUpload.Visibility == Visibility.Visible;
		}
		set
		{
			SetOptPath(ref _optUpload, "AttFileUploadDrawingImage", value);
		}
	}

	private void SetOptPath(ref Image path, string sourceKey, bool vis)
	{
		if (path == null && vis)
		{
			ResourceDictionary imageDictionary = ThemeUtil.GetImageDictionary(_isDark);
			DrawingImage imageSource = Utils.GetImageSource(sourceKey, imageDictionary);
			if (imageSource != null && imageSource.CanFreeze)
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
			_optBorder.Child = path;
		}
		if (path != null)
		{
			path.Visibility = ((!vis) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void ResetIconAndBackground()
	{
		if (_iconImage != null)
		{
			string text = _attachmentIconConverter.Convert(_fileType, null, _convertParam, null) as string;
			if (!string.IsNullOrEmpty(text))
			{
				_iconImage.Source = new BitmapImage(new Uri(text));
			}
			if (_attachmentBackgroundConverter.Convert(_fileType, null, _convertParam, null) is SolidColorBrush background)
			{
				base.Background = background;
			}
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

	public AttachmentFileDisplayControl(AttachmentViewModel model, AttachmentImageDisplayControl.OnAttachementMouseEnter mouseEnter, bool isDark)
	{
		AttachmentFileDisplayControl attachmentFileDisplayControl = this;
		InitializeComponent();
		_isDark = isDark;
		if (model == null)
		{
			return;
		}
		_model = model;
		model.PropertyChanged += delegate(object s, PropertyChangedEventArgs e)
		{
			if (s is AttachmentViewModel attachmentViewModel)
			{
				string propertyName = e.PropertyName;
				if (!(propertyName == "FileStatus"))
				{
					if (propertyName == "LocalPath")
					{
						attachmentFileDisplayControl._convertParam.IsFailed = FileUtils.FileEmptyOrNotExists(model.LocalPath);
						attachmentFileDisplayControl.ResetIconAndBackground();
					}
				}
				else
				{
					attachmentFileDisplayControl._convertParam.IsFailed = FileUtils.FileEmptyOrNotExists(model.LocalPath);
					attachmentFileDisplayControl.SetOpt(attachmentViewModel.FileStatus);
				}
			}
		};
		base.DataContext = model;
		InitStaticContent(model);
		base.MouseEnter += async delegate(object s, MouseEventArgs e)
		{
			await Task.Delay(100);
			if (s is Border { IsMouseOver: not false })
			{
				mouseEnter?.Invoke(s);
			}
		};
		_convertParam.IsDark = isDark;
		_convertParam.IsFailed = FileUtils.FileEmptyOrNotExists(model.LocalPath);
		_fileType = model.FileType;
		SetOpt(model.FileStatus);
		if (ABTestManager.IsAIVoiceEnabled())
		{
			if (AIAudioService.CheckShowExpandByCache(model.Id))
			{
				DescribePanel.Visibility = Visibility.Visible;
			}
			TryShowDescribeOrHint();
		}
	}

	private void SetOpt(OptTypeEnum opt)
	{
		switch (opt)
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
			_convertParam.IsFailed = false;
			break;
		}
		ResetIconAndBackground();
	}

	private void InitStaticContent(AttachmentViewModel model)
	{
		if (nameBlock != null)
		{
			nameBlock.Tag = model.Id;
			nameBlock.Text = model.FileName;
		}
		if (sizeBlock != null)
		{
			sizeBlock.Text = _attachmentSizedConverter.Convert(model.Size, null, null, null) as string;
		}
	}

	private async Task TryShowAIAudioHint()
	{
		if (!(_model.FileType != Constants.AttachmentKind.AUDIO.ToString()) && !string.IsNullOrEmpty(_model.LocalPath) && !LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.AIAudioButtonHint))
		{
			if (await AudioAttachmentAIResultDao.ExistRecord())
			{
				LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIAudioButtonHint);
				return;
			}
			LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIAudioButtonHint);
			HintView.Visibility = Visibility.Visible;
			AIRecHintPopup.IsOpen = true;
		}
	}

	public void HideAIAudioHint()
	{
		if (!(_model.FileType != Constants.AttachmentKind.AUDIO.ToString()))
		{
			AIRecHintPopup.IsOpen = false;
			HintView.Visibility = Visibility.Collapsed;
		}
	}

	private async Task TryShowDescribeOrHint()
	{
		await UpdateAIAudioState();
		await TryShowAIAudioHint();
	}

	private async Task UpdateAIAudioState()
	{
		if (_model.FileType != Constants.AttachmentKind.AUDIO.ToString())
		{
			return;
		}
		TaskModel taskModel = await TaskDao.GetThinTaskById(_model.TaskId);
		if (taskModel == null)
		{
			return;
		}
		bool disable = taskModel.deleted != 0 || taskModel.status == 1 || !taskModel.CheckEnable();
		bool handling = false;
		string describe = null;
		(HandleState, HandleState, AudioAttachmentAIResultModel) tuple = await AIAudioService.CheckHandleState(_model.Id);
		switch (tuple.Item2)
		{
		case HandleState.Handled:
			if (tuple.Item3 != null)
			{
				describe = Utils.GetString("AISummary") + ((Utils.IsCn() || Utils.IsJp()) ? "：" : ": ") + tuple.Item3.Topic;
			}
			break;
		case HandleState.Handling:
			describe = Utils.GetString("AISummarizing");
			handling = true;
			break;
		default:
			switch (tuple.Item1)
			{
			case HandleState.Handled:
				if (tuple.Item3 != null && !string.IsNullOrEmpty(tuple.Item3.Raw))
				{
					using StringReader stringReader = new StringReader(tuple.Item3.Raw);
					string text = stringReader.ReadLine();
					describe = Utils.GetString("AITranscriptStart") + ((Utils.IsCn() || Utils.IsJp()) ? null : " ") + text;
				}
				break;
			case HandleState.Handling:
				describe = Utils.GetString("AITranscribing");
				handling = true;
				break;
			}
			break;
		}
		base.Dispatcher.Invoke(delegate
		{
			if (!string.IsNullOrEmpty(describe))
			{
				DescribePanel.Visibility = Visibility.Visible;
				DescribeText.Text = describe;
				if (handling)
				{
					DescribeButton.Visibility = Visibility.Collapsed;
					DescribeText.Opacity = 0.5;
				}
				else
				{
					DescribeButton.Visibility = Visibility.Visible;
					DescribeText.Opacity = 1.0;
				}
				if (disable)
				{
					DescribeButton.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				DescribePanel.Visibility = Visibility.Collapsed;
			}
		});
	}

	private void Border_Loaded(object sender, RoutedEventArgs e)
	{
		AIAudioService.OnStateChange = (Action<string>)Delegate.Combine(AIAudioService.OnStateChange, new Action<string>(OnServiceResult));
	}

	private void Border_Unloaded(object sender, RoutedEventArgs e)
	{
		AIAudioService.OnStateChange = (Action<string>)Delegate.Remove(AIAudioService.OnStateChange, new Action<string>(OnServiceResult));
	}

	private void OnServiceResult(string id)
	{
		if (!(id != _model.Id))
		{
			UpdateAIAudioState();
		}
	}

	private void AIRecHintPopup_MouseLeave(object sender, MouseEventArgs e)
	{
		HideAIAudioHint();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/attachmentfiledisplaycontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((AttachmentFileDisplayControl)target).Loaded += Border_Loaded;
			((AttachmentFileDisplayControl)target).Unloaded += Border_Unloaded;
			break;
		case 2:
			_mainGrid = (Grid)target;
			break;
		case 3:
			_iconImage = (Image)target;
			break;
		case 4:
			infoGrid = (Grid)target;
			break;
		case 5:
			nameBlock = (TextBlock)target;
			break;
		case 6:
			sizeBlock = (TextBlock)target;
			break;
		case 7:
			_optBorder = (Border)target;
			break;
		case 8:
			HintView = (Grid)target;
			break;
		case 9:
			AIRecHintPopup = (Popup)target;
			AIRecHintPopup.MouseLeave += AIRecHintPopup_MouseLeave;
			break;
		case 10:
			PreviewAIRecBorder = (Border)target;
			break;
		case 11:
			DescribePanel = (StackPanel)target;
			break;
		case 12:
			DescribeText = (TextBlock)target;
			break;
		case 13:
			DescribeButton = (Button)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
