using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Files;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MainListView.DetailView;

namespace ticktick_WPF.Views.Detail;

public class AttachmentOptionPanel : System.Windows.Controls.UserControl, IComponentConnector
{
	private AttachmentInfo _modelInfo;

	private AttachmentModel _model;

	private bool _enable;

	private OptTypeEnum _optType = OptTypeEnum.None;

	internal Border BackBorder;

	internal Grid ImageOption;

	internal Border ImageOptionMenuBorder;

	internal Popup ImageOptionMenu;

	internal System.Windows.Controls.Button SwitchToSmallSizeButton;

	internal System.Windows.Controls.Button SwitchToNormalSizeButton;

	internal StackPanel FileOption;

	internal StackPanel AIAudioButtonPanel;

	internal Popup AIAudioOptionMenu;

	internal Popup AttachmentOptionMenu;

	internal System.Windows.Controls.ProgressBar DownloadProgress;

	private bool _contentLoaded;

	private OptTypeEnum OptType
	{
		get
		{
			return _optType;
		}
		set
		{
			_optType = value;
			if (_optType != OptTypeEnum.Menu)
			{
				ImageOption.Opacity = 0.0;
				FileOption.Opacity = 0.0;
			}
			else
			{
				ImageOption.Opacity = 1.0;
				FileOption.Opacity = 1.0;
			}
		}
	}

	public event EventHandler<AttachmentInfo> Delete;

	public event EventHandler<AttachmentModel> CheckListDelete;

	public event EventHandler<int> ImageModeChanged;

	public AttachmentOptionPanel()
	{
		InitializeComponent();
	}

	public bool IsAnyPopupOpen()
	{
		if (!AttachmentOptionMenu.IsOpen && !ImageOptionMenu.IsOpen)
		{
			return AIAudioOptionMenu.IsOpen;
		}
		return true;
	}

	private void OnFileDownLoadClick(object sender, RoutedEventArgs e)
	{
		AttachmentOptionMenu.IsOpen = false;
		SaveAs();
	}

	private void OnFileDeleteClick(object sender, RoutedEventArgs e)
	{
		AttachmentOptionMenu.IsOpen = false;
		FileDelete();
	}

	private void FileDelete()
	{
		if (_modelInfo == null)
		{
			this.CheckListDelete?.Invoke(this, _model);
		}
		else
		{
			this.Delete?.Invoke(this, _modelInfo);
		}
	}

	private async Task Download()
	{
		await AttachmentDownloadUtils.DownloadFileAttachment(_model.id);
	}

	private async Task SaveAs()
	{
		if (_model == null)
		{
			return;
		}
		AttachmentModel attachment = _model;
		TaskModel task = await TaskDao.GetThinTaskById(_model.taskId);
		if (task == null)
		{
			return;
		}
		string fileName = FileUtils.TrimFileName(attachment.fileName, 200);
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			FileName = fileName,
			Filter = "File|*" + Path.GetExtension(attachment.fileName)
		};
		TaskDetailView detailView = Utils.FindParent<TaskDetailView>(this);
		detailView?.OnShowDialog();
		if (saveFileDialog.ShowDialog() == DialogResult.OK)
		{
			string foldPath = saveFileDialog.FileName;
			try
			{
				if (!string.IsNullOrEmpty(attachment.localPath) && File.Exists(attachment.localPath))
				{
					File.Copy(attachment.localPath, foldPath);
				}
				else
				{
					attachment.localPath = foldPath;
					await AttachmentDao.UpdateAttachment(attachment);
					AttachmentDownloadUtils.DownloadFile(task.projectId, task.id, attachment.id, foldPath);
				}
			}
			catch (Exception)
			{
			}
		}
		detailView?.OnCloseDialog();
	}

	private async Task Upload()
	{
		if (_model == null)
		{
			return;
		}
		if (AttachmentLoadHelper.IsLoading(_model.id))
		{
			OptType = OptTypeEnum.Loading;
		}
		else if (AttachmentCache.GetAttachmentById(_model.id) != null)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(_model.taskId);
			if (taskModel != null)
			{
				AttachmentUploadUtils.UpFile(taskModel.projectId, taskModel.id, _model, Utils.FindParent<IToastShowWindow>(this));
			}
		}
	}

	public void SetPanel(AttachmentViewModel model, double width, double height, bool enable, int imageMode = 0)
	{
		_model = AttachmentCache.GetAttachmentById(model.Id);
		_modelInfo = null;
		_enable = enable && model != null;
		base.DataContext = new AttachmentOptionPanelModel
		{
			FileName = model?.FileName,
			BackBrush = ((model == null) ? Brushes.Transparent : new SolidColorBrush(AttachmentResourceHelper.GetFileTypeColor(model?.FileType).Color))
		};
		DownloadProgress.Visibility = Visibility.Collapsed;
		OptType = AttachmentLoadHelper.AttachmentStatus(_model, enable);
		ImageOption.Visibility = Visibility.Collapsed;
		FileOption.Visibility = Visibility.Collapsed;
		if (model != null && enable)
		{
			OptType = AttachmentLoadHelper.AttachmentStatus(_model, enable);
			if (model.FileType == "IMAGE")
			{
				ImageOption.Visibility = Visibility.Visible;
				SwitchToSmallSizeButton.Visibility = ((imageMode != 0) ? Visibility.Collapsed : Visibility.Visible);
				SwitchToNormalSizeButton.Visibility = ((imageMode == 0) ? Visibility.Collapsed : Visibility.Visible);
				ImageOptionMenuBorder.Margin = new Thickness((imageMode == 0) ? 6 : 3);
			}
			else
			{
				FileOption.Visibility = Visibility.Visible;
			}
		}
		AIAudioButtonPanel.Visibility = ((!enable || !ABTestManager.IsAIVoiceEnabled() || !(model?.FileType == Constants.AttachmentKind.AUDIO.ToString()) || string.IsNullOrEmpty(model?.LocalPath) || !File.Exists(model?.LocalPath)) ? Visibility.Collapsed : Visibility.Visible);
		base.Width = width;
		base.Height = height;
	}

	public void SetPanel(AttachmentMDDisplayModel model, double width, double height, bool enable, int imageMode = 0)
	{
		if (model.Attachment != null)
		{
			model.Attachment = AttachmentCache.GetAttachmentById(model.Attachment.id);
		}
		_model = model.Attachment;
		_modelInfo = model.Info;
		_enable = enable && model.Attachment != null;
		base.DataContext = new AttachmentOptionPanelModel
		{
			FileName = model?.Attachment?.fileName,
			BackBrush = ((model?.Attachment == null) ? Brushes.Transparent : new SolidColorBrush(AttachmentResourceHelper.GetFileTypeColor(model?.Attachment?.fileType).Color))
		};
		DownloadProgress.Visibility = Visibility.Collapsed;
		OptType = AttachmentLoadHelper.AttachmentStatus(_model, enable);
		ImageOption.Visibility = Visibility.Collapsed;
		FileOption.Visibility = Visibility.Collapsed;
		if (model.Attachment != null && enable)
		{
			if (model.Info?.Kind == "image")
			{
				ImageOption.Visibility = Visibility.Visible;
				SwitchToSmallSizeButton.Visibility = ((imageMode != 0) ? Visibility.Collapsed : Visibility.Visible);
				SwitchToNormalSizeButton.Visibility = ((imageMode == 0) ? Visibility.Collapsed : Visibility.Visible);
				ImageOptionMenuBorder.Margin = new Thickness((imageMode == 0) ? 6 : 3);
			}
			else
			{
				FileOption.Visibility = Visibility.Visible;
			}
		}
		AIAudioButtonPanel.Visibility = ((!enable || !ABTestManager.IsAIVoiceEnabled() || !(model?.Attachment?.fileType == Constants.AttachmentKind.AUDIO.ToString()) || string.IsNullOrEmpty(model?.Attachment?.localPath) || !File.Exists(model?.Attachment?.localPath)) ? Visibility.Collapsed : Visibility.Visible);
		base.Width = width;
		base.Height = height;
	}

	private async void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (FileOption.IsMouseOver || ImageOption.IsMouseOver || AttachmentOptionMenu.IsMouseOver || ImageOptionMenu.IsMouseOver || AIAudioOptionMenu.IsOpen || _model == null)
		{
			return;
		}
		e.Handled = true;
		if (!FileUtils.FileEmptyOrNotExists(_model.localPath))
		{
			try
			{
				await Task.Delay(50);
				Process.Start(_model.localPath);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		if (_model != null && _model.fileType != "IMAGE")
		{
			Download();
		}
	}

	private void OnOptionRightMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (OptType == OptTypeEnum.Menu)
		{
			if (ImageOption.IsVisible)
			{
				ImageOptionMenu.Placement = PlacementMode.Mouse;
				ImageOptionMenu.IsOpen = true;
			}
			else
			{
				AttachmentOptionMenu.Placement = PlacementMode.Mouse;
				AttachmentOptionMenu.IsOpen = true;
			}
		}
	}

	private void OnImageMorePreviewMouseUp(object sender, MouseButtonEventArgs e)
	{
		switch (OptType)
		{
		case OptTypeEnum.Upload:
		case OptTypeEnum.UploadFailed:
			Upload();
			break;
		case OptTypeEnum.Download:
		case OptTypeEnum.DownloadFailed:
			Download();
			break;
		case OptTypeEnum.Menu:
			ImageOptionMenu.Placement = PlacementMode.Bottom;
			ImageOptionMenu.PlacementTarget = sender as UIElement;
			ImageOptionMenu.IsOpen = true;
			break;
		}
	}

	private void OnImageDeleteClick(object sender, MouseButtonEventArgs e)
	{
		ImageOptionMenu.IsOpen = false;
		FileDelete();
	}

	private void OnImageDownloadClick(object sender, RoutedEventArgs e)
	{
		ImageOptionMenu.IsOpen = false;
		SaveAs();
	}

	private void OnNormalSizeClick(object sender, RoutedEventArgs e)
	{
		ImageOptionMenu.IsOpen = false;
		this.ImageModeChanged?.Invoke(this, 0);
	}

	private void OnSmallSizeClick(object sender, RoutedEventArgs e)
	{
		ImageOptionMenu.IsOpen = false;
		this.ImageModeChanged?.Invoke(this, 1);
	}

	private void OnAttachmentOptionClick(object sender, MouseButtonEventArgs e)
	{
		switch (OptType)
		{
		case OptTypeEnum.Upload:
		case OptTypeEnum.UploadFailed:
			Upload();
			break;
		case OptTypeEnum.Download:
		case OptTypeEnum.DownloadFailed:
			Download();
			break;
		case OptTypeEnum.Menu:
			AttachmentOptionMenu.Placement = PlacementMode.Bottom;
			AttachmentOptionMenu.PlacementTarget = sender as UIElement;
			AttachmentOptionMenu.IsOpen = true;
			break;
		}
	}

	private void OnImageCopyClick(object sender, RoutedEventArgs e)
	{
		if (_model != null && File.Exists(_model.localPath))
		{
			try
			{
				System.Windows.Clipboard.SetFileDropList(new StringCollection { _model.localPath });
				Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("Copied"));
			}
			catch (Exception ex)
			{
				Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("OperateFailed"));
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
			ImageOptionMenu.IsOpen = false;
		}
	}

	private async void OnAIAudioOptionClick(object sender, MouseButtonEventArgs e)
	{
		if (OptType != OptTypeEnum.Menu)
		{
			return;
		}
		(HandleState, HandleState, AudioAttachmentAIResultModel) tuple = await AIAudioService.CheckHandleState(_model.id);
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		switch (tuple.Item1)
		{
		case HandleState.None:
			list.Add(new CustomMenuItemViewModel("GenerAITranscribe", Utils.GetString("AITranscribe"), Utils.GetGeometry("Ic.AI.TranscribeText"))
			{
				ShowSelectedIcon = false
			});
			break;
		case HandleState.Handling:
			list.Add(new CustomMenuItemViewModel("ViewAITranscribe", Utils.GetString("AIViewTranscript"), Utils.GetGeometry("Ic.AI.ViewText"))
			{
				ShowSelectedIcon = false
			});
			break;
		case HandleState.Handled:
			if (!AIAudioService.CheckIfEmpty(tuple.Item3))
			{
				list.Add(new CustomMenuItemViewModel("ViewAITranscribe", Utils.GetString("AIViewTranscript"), Utils.GetGeometry("Ic.AI.ViewText"))
				{
					ShowSelectedIcon = false
				});
				break;
			}
			Utils.Toast(Utils.GetString("AINoTextDetected"));
			return;
		case HandleState.Error:
			if (AIAudioService.CheckErrorCanRetry(tuple.Item3.ErrorCode))
			{
				list.Add(new CustomMenuItemViewModel("GenerAITranscribe", Utils.GetString("AITranscribe"), Utils.GetGeometry("Ic.AI.TranscribeText"))
				{
					ShowSelectedIcon = false
				});
				break;
			}
			Utils.Toast(AIAudioService.GetErrorTextByCode(tuple.Item3.ErrorCode, AIAudioViewType.Transcript));
			return;
		}
		switch (tuple.Item2)
		{
		case HandleState.None:
			if ((tuple.Item1 != HandleState.Handled || !string.IsNullOrEmpty(tuple.Item3?.Raw)) && !AIAudioService.CheckIfTooShort(tuple.Item3))
			{
				list.Add(new CustomMenuItemViewModel("GenerAISummarize", Utils.GetString("AISummarize"), Utils.GetGeometry("Ic.AI.GenerateSummary"))
				{
					ShowSelectedIcon = false
				});
			}
			break;
		case HandleState.Error:
			if (AIAudioService.CheckErrorCanRetry(tuple.Item3.ErrorCode))
			{
				list.Add(new CustomMenuItemViewModel("GenerAISummarize", Utils.GetString("AISummarize"), Utils.GetGeometry("Ic.AI.GenerateSummary"))
				{
					ShowSelectedIcon = false
				});
				break;
			}
			Utils.Toast(AIAudioService.GetErrorTextByCode(tuple.Item3.ErrorCode, AIAudioViewType.Transcript));
			return;
		default:
			list.Add(new CustomMenuItemViewModel("ViewAISummarize", Utils.GetString("AIViewSummary"), Utils.GetGeometry("Ic.AI.ViewSummary"))
			{
				ShowSelectedIcon = false
			});
			break;
		}
		if (list.Count > 0)
		{
			CustomMenuList customMenuList = new CustomMenuList(list, AIAudioOptionMenu);
			customMenuList.Operated += OnAIAudioItemSelected;
			customMenuList.Show();
		}
	}

	private async void OnAIAudioItemSelected(object sender, object key)
	{
		TaskModel task = await TaskDao.GetThinTaskById(_model.taskId);
		if (task == null)
		{
			return;
		}
		switch (key as string)
		{
		case "GenerAITranscribe":
		case "GenerAISummarize":
			if (!UserDao.IsPro())
			{
				ProChecker.ShowUpgradeDialog(ProType.AIAudio);
				break;
			}
			if (await CheckNeedShowAIAudioWarn())
			{
				if (new CustomerDialog(Utils.GetString("AIGenerSummaryPrivacyTitle"), Utils.GetString("AIGenerSummaryPrivacyDesc"), Utils.GetString("Agree"), Utils.GetString("Cancel")).ShowDialog() != true)
				{
					break;
				}
				LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIAudioPrivacyWarn);
			}
			switch (key as string)
			{
			case "GenerAITranscribe":
				AIAudioService.Transcript(_model.id, task.projectId, _model.taskId);
				break;
			case "GenerAISummarize":
				AIAudioService.Summary(_model.id, task.projectId, _model.taskId);
				break;
			}
			break;
		case "ViewAITranscribe":
		case "ViewAISummarize":
		{
			AIAudioViewType type = ((key != "ViewAITranscribe") ? AIAudioViewType.Summary : AIAudioViewType.Transcript);
			ShowAIAudioWindow(task.projectId, type);
			break;
		}
		}
	}

	private async Task<bool> CheckNeedShowAIAudioWarn()
	{
		if (LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.AIAudioPrivacyWarn))
		{
			return false;
		}
		if (await AudioAttachmentAIResultDao.ExistRecord())
		{
			LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.AIAudioPrivacyWarn);
			return false;
		}
		return true;
	}

	private async void OnDescribeMouseLeftUp(object sender, MouseButtonEventArgs e)
	{
		if (_model == null)
		{
			return;
		}
		if (_model.fileType != Constants.AttachmentKind.AUDIO.ToString())
		{
			OnMouseUp(sender, e);
			return;
		}
		TaskModel task = await TaskDao.GetThinTaskById(_model.taskId);
		if (task != null && task.deleted == 0 && task.status != 1 && task.CheckEnable())
		{
			AIAudioViewType type = AIAudioViewType.Transcript;
			(HandleState, HandleState, AudioAttachmentAIResultModel) tuple = await AIAudioService.CheckHandleState(_model.id);
			if (tuple.Item2 == HandleState.Handling || tuple.Item2 == HandleState.Handled)
			{
				type = AIAudioViewType.Summary;
			}
			ShowAIAudioWindow(task.projectId, type);
		}
	}

	private void ShowAIAudioWindow(string projectId, AIAudioViewType type)
	{
		TaskDetailView detailView = Utils.FindParent<TaskDetailView>(this);
		TaskDetailPopup taskDetailPopup = Utils.FindParent<TaskDetailPopup>(this);
		detailView?.OnShowDialog();
		AIAudioTransResultWindow aIAudioTransResultWindow = new AIAudioTransResultWindow(_model, projectId, type);
		if (taskDetailPopup != null)
		{
			aIAudioTransResultWindow.Owner = taskDetailPopup;
		}
		else
		{
			aIAudioTransResultWindow.Owner = App.Window;
		}
		aIAudioTransResultWindow.Closed += delegate
		{
			detailView?.OnCloseDialog();
		};
		aIAudioTransResultWindow.Show();
	}

	private void DecribeGridMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_model != null && _model.fileType != Constants.AttachmentKind.AUDIO.ToString())
		{
			OnOptionRightMouseUp(sender, e);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/attachmentoptionpanel.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			BackBorder = (Border)target;
			break;
		case 2:
			((Grid)target).PreviewMouseLeftButtonUp += OnMouseUp;
			((Grid)target).PreviewMouseRightButtonUp += OnOptionRightMouseUp;
			break;
		case 3:
			ImageOption = (Grid)target;
			break;
		case 4:
			ImageOptionMenuBorder = (Border)target;
			ImageOptionMenuBorder.PreviewMouseLeftButtonUp += OnImageMorePreviewMouseUp;
			break;
		case 5:
			ImageOptionMenu = (Popup)target;
			break;
		case 6:
			SwitchToSmallSizeButton = (System.Windows.Controls.Button)target;
			SwitchToSmallSizeButton.Click += OnSmallSizeClick;
			break;
		case 7:
			SwitchToNormalSizeButton = (System.Windows.Controls.Button)target;
			SwitchToNormalSizeButton.Click += OnNormalSizeClick;
			break;
		case 8:
			((System.Windows.Controls.Button)target).Click += OnImageCopyClick;
			break;
		case 9:
			((System.Windows.Controls.Button)target).Click += OnImageDownloadClick;
			break;
		case 10:
			FileOption = (StackPanel)target;
			break;
		case 11:
			AIAudioButtonPanel = (StackPanel)target;
			break;
		case 12:
			((Border)target).PreviewMouseLeftButtonUp += OnAIAudioOptionClick;
			break;
		case 13:
			AIAudioOptionMenu = (Popup)target;
			break;
		case 14:
			((Border)target).PreviewMouseLeftButtonUp += OnAttachmentOptionClick;
			break;
		case 15:
			AttachmentOptionMenu = (Popup)target;
			break;
		case 16:
			((System.Windows.Controls.Button)target).Click += OnFileDownLoadClick;
			break;
		case 17:
			((System.Windows.Controls.Button)target).Click += OnFileDeleteClick;
			break;
		case 18:
			DownloadProgress = (System.Windows.Controls.ProgressBar)target;
			break;
		case 19:
			((Grid)target).MouseLeftButtonUp += OnDescribeMouseLeftUp;
			((Grid)target).MouseRightButtonUp += DecribeGridMouseRightButtonUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
