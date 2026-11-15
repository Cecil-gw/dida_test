using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Emoji.Wpf;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Newtonsoft.Json;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Event;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Activity;
using ticktick_WPF.Views.Agenda;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CheckList;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomPopup;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView.DetailView.GoogleEventInfoBlock;
using ticktick_WPF.Views.MainListView.DetailView.NotionBlock;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;
using ticktick_WPF.Views.NewUser;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Print;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Summary;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Template;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.MainListView.DetailView;

public class TaskDetailView : Grid, IBatchEditable
{
	private enum TaskTextPart
	{
		Content,
		Description
	}

	private sealed class PeerTaskTextChangedEventArgs : EventArgs
	{
		public string TaskId { get; }

		public TaskTextPart Part { get; }

		public string Text { get; }

		public PeerTaskTextChangedEventArgs(string taskId, TaskTextPart part, string text)
		{
			TaskId = taskId;
			Part = part;
			Text = text ?? string.Empty;
		}
	}

	private sealed class SubtaskTreeBuildResult
	{
		public Node<DisplayItemModel> Root { get; }

		public List<DisplayItemModel> AllChildren { get; }

		public Dictionary<string, List<string>> InvalidChildIdsByParent { get; }

		public int ParentTaskLevel { get; }

		public SubtaskTreeBuildResult(Node<DisplayItemModel> root, List<DisplayItemModel> allChildren, Dictionary<string, List<string>> invalidChildIdsByParent, int parentTaskLevel)
		{
			Root = root;
			AllChildren = allChildren;
			InvalidChildIdsByParent = invalidChildIdsByParent;
			ParentTaskLevel = parentTaskLevel;
		}
	}

	private StackPanel _guidePanel;

	private static readonly string _guideString = "![file](https://pull.dida365.com";

	private string _guideLink;

	private string _guideTaskId;

	private ContentControl _videoContainer;

	private ContentControl _actionButton;

	private Stopwatch _stopWatch;

	private string _label;

	private TTMediaPlayer _videoPlayer;

	private Dictionary<string, KeyBinding> _keyBindings = new Dictionary<string, KeyBinding>();

	private readonly ScrollViewer _detailScrollViewer = new ScrollViewer
	{
		Focusable = false,
		AllowDrop = true,
		MinHeight = 110.0
	};

	internal const double TextEditorMenuBottomPadding = 56.0;

	private const double FocusSummaryLeftMargin = 2.0;

	private Window _linkParentTaskPopupOwnerWindow;

	private string _uid = Utils.GetGuid();

	private bool? _isDark;

	private TaskDetailHeadView _detailHead;

	private Grid _scrollContent;

	private Grid _titleUpperContent;

	private TaskActivityControl _activityPanel;

	private RowDefinition _scrollButtonRow;

	private TaskDetailBottomControl _detailBottom;

	private int _quadrantLevel;

	private bool _navigating;

	private TaskActivityPanel _taskActivityPanel;

	private bool _needFocusDetail;

	private bool _canScroll = true;

	private DragAutoScrollHelper _autoScroller;

	private TaskNotionPanel _notionView;

	private bool _skipNextPopupClose;

	private StackPanel _attachmentPanel;

	private AttachmentProvider _attachmentProvider;

	private readonly TTAsyncLocker _lockAttachment = new TTAsyncLocker(1, 1);

	private readonly AttachmentOptionPanel _attachmentOptionPanel = new AttachmentOptionPanel
	{
		Width = 0.0,
		Height = 0.0,
		Opacity = 1.0,
		HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private CommentDisplayControl _commentDisplayCtrl;

	private AddCommentControl _addCommentCtrl;

	private int _commentLoadVersion;

	private Grid _contentGrid = new Grid
	{
		MinHeight = 50.0
	};

	private MarkDownEditor _contentText;

	private MarkDownEditor _descText;

	private ChecklistControl _checklist;

	private EditorMenu _textEditorMenu;

	private TagDisplayControl _tagsControl;

	private bool _switchTextClick;

	private string _switchListOriginText;

	private StackPanel _hintTextPanel;

	private const double ContentHintLeftMargin = 4.0;

	private readonly SemaphoreSlim _taskContentSaveLock = new SemaphoreSlim(1, 1);

	private static readonly object TaskContentSaveStateLock = new object();

	private static readonly Dictionary<string, int> TaskContentSaveCounts = new Dictionary<string, int>(StringComparer.Ordinal);

	private static WeakReference _pendingImmersiveSource;

	private TaskGoogleEventInfoPanel _googleEventInfoView;

	private BatchTaskEditHelper _batchHelper;

	private StackPanel _subtaskGrid;

	private TaskListView _subTaskList;

	private Grid _addSubTaskGrid;

	private Border _addSubTaskButtonBorder;

	private readonly TTAsyncLocker<int> _subTaskAsyncLocker = new TTAsyncLocker<int>();

	private readonly TTAsyncLocker _addSubTaskAsyncLocker = new TTAsyncLocker(1, 1);

	private int _subtaskRequestVersion;

	private const double TitleTextLeftMargin = 4.0;

	private const double TitleTextStatusLeftMargin = 24.0;

	private readonly DetailTextBox _titleText = new DetailTextBox
	{
		Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
		MaxLength = 2048
	};

	private TextBlock _titleHint;

	private HoverIconButton _kindSwitcher;

	private TitleDateParseMode _titleParseMode;

	private bool _titleParseDisabledByManualTime;

	private Task<bool> _titleParseCommitTask;

	private TaskDetailViewModel _titleParseCommitModel;

	private int _titleParseVersion;

	private int _titleParseCommitVersion;

	private System.Windows.Shapes.Path _statusIcon;

	private (string, List<TaskDetailItemModel>) _savedItems;

	private ImmersiveContent _immersiveContent;

	private LinkParentTaskMenu LinkParentTaskMenu { get; set; } = new LinkParentTaskMenu();

	private Popup _linkParentTaskPopup { get; set; }

	private Border _parentTitleBorder { get; set; }

	private TaskDetailViewModel _task => base.DataContext as TaskDetailViewModel;

	public string TaskId => _task.TaskId;

	public string ProjectId => _task.ProjectId;

	private bool _popupClosing { get; set; }

	private bool _popupShowing { get; set; }

	public Constants.DetailMode Mode { get; set; }

	public bool Immerse { get; set; }

	public bool DatePopupShow { get; set; }

	public bool IsBlank { get; set; }

	public ProjectIdentity ParentIdentity { get; set; }

	public bool UseInWidget { get; set; }

	public bool PopupShowing => _popupShowing;

	public bool PopupClosing => _popupClosing;

	public ScrollViewer ScrollViewer => _detailScrollViewer;

	public bool KeepDetailOnDelete { get; set; }

	public bool KeepDetailOnCopy { get; set; }

	public bool MorePopupOpen => _detailBottom?.MorePopup.IsOpen ?? false;

	public DateTime? DisplayDate { get; set; }

	public bool TitleFocused => _titleText.TextArea.IsKeyboardFocused;

	private List<TaskDetailItemModel> convertInitItems { get; set; }

	public event EventHandler<string> TaskDateSaved;

	public event EventHandler<string> TagClick;

	public event EventHandler<string> TaskNavigated;

	public event EventHandler<DragMouseEvent> SubtaskDragOver;

	public event EventHandler EscKeyUp;

	public event EventHandler<string> ShowUndoOnTaskDeleted;

	public event EventHandler<string> TaskDeleted;

	public event EventHandler NotifyCloseWindow;

	public event EnterImmersiveDelegate EnterImmersive;

	public event EventHandler ExitImmersive;

	public event EventHandler<string> TaskCopied;

	public event EventHandler FocusList;

	public event EventHandler NavigateBack;

	public event EventHandler ForceHideWindow;

	public event EventHandler SubTaskChanged;

	public event EventHandler<ProjectTask> NavigateTask;

	public event EventHandler<TaskDetailItemModel> CheckItemsDeleted;

	private static event EventHandler<PeerTaskTextChangedEventArgs> PeerTaskTextChanged;

	private void TryShowGuide()
	{
		string taskId = _task.TaskId;
		if (_guideTaskId == taskId)
		{
			return;
		}
		_videoPlayer?.PlayVideo(null);
		if (!string.IsNullOrEmpty(_guideTaskId) && !string.IsNullOrEmpty(_label))
		{
			RecordLabelTime();
		}
		_label = _task.Label;
		if (!string.IsNullOrEmpty(_label))
		{
			_stopWatch = _stopWatch ?? new Stopwatch();
			_stopWatch.Restart();
		}
		_guideTaskId = taskId;
		string text = string.Empty;
		GuideProjectTaskResource guideProjectTaskResource = _task.GuideResources?.FirstOrDefault((GuideProjectTaskResource g) => g.type == "video");
		if (guideProjectTaskResource != null)
		{
			text = guideProjectTaskResource.url;
		}
		else if (_task.TaskContent != null && _task.TaskContent.Contains(_guideString))
		{
			string taskContent = _task.TaskContent;
			int num = taskContent.LastIndexOf(_guideString, StringComparison.Ordinal);
			int num2 = taskContent.IndexOf(")", num, StringComparison.Ordinal);
			if (num2 > num)
			{
				string text2 = taskContent.Substring(num, num2 - num + 1);
				text = text2.Substring(8, text2.Length - 9);
			}
		}
		string fileName = System.IO.Path.GetFileName(text);
		if (fileName != null && fileName.EndsWith(".mp4"))
		{
			_guideLink = "![file](" + text + ")";
			List<string> list = text.Split('/').ToList();
			fileName = fileName.Replace(".mp4", ".avi");
			fileName = fileName.Insert(fileName.Length - 4, "_" + ((list.Count > 1) ? list[list.Count - 2] : ""));
			text = text.Replace(".mp4", ".avi");
			AddGuideVideo(text, fileName);
		}
		else
		{
			_guideLink = "";
			if (_videoContainer != null)
			{
				_videoContainer.Content = null;
			}
			_videoPlayer?.PlayVideo(null);
			_videoPlayer = null;
		}
		if (_task != null)
		{
			List<string> guideActions = _task.GuideActions;
			if (guideActions != null && guideActions.Count > 0)
			{
				AddAction(_task.GuideActions[0]);
				return;
			}
		}
		if (_actionButton != null)
		{
			_actionButton.Content = null;
			_actionButton.MouseLeftButtonUp -= OnGuideActionClick;
		}
	}

	public void RecordLabelTime()
	{
		if (!string.IsNullOrEmpty(_label) && _stopWatch != null && _stopWatch.IsRunning)
		{
			_stopWatch.Stop();
			long elapsedMilliseconds = _stopWatch.ElapsedMilliseconds;
			UserActCollectUtils.AddTaskLabelEvent("preset_list", "stay_time", _label, elapsedMilliseconds);
		}
	}

	private string GetActionName(string url)
	{
		return url switch
		{
			"ticktick://project?from=select-common" => Utils.GetString("ChooseCommonLists"), 
			"ticktick://matrix?from=detail" => string.Format(Utils.GetString(LocalSettings.Settings.ShowMatrix ? "GoFeature" : "EnableFeature"), Utils.GetString("Matrix")), 
			"ticktick://focus?from=detail" => string.Format(Utils.GetString(LocalSettings.Settings.EnableFocus ? "GoFeature" : "EnableFeature"), Utils.GetString("PomoFocus")), 
			"ticktick://habit?from=detail" => string.Format(Utils.GetString(LocalSettings.Settings.ShowHabit ? "GoFeature" : "EnableFeature"), Utils.GetString("Habit")), 
			_ => null, 
		};
	}

	private void OnGuideActionClick(object sender, MouseButtonEventArgs e)
	{
		string text = _actionButton.Tag as string;
		switch (text)
		{
		case "ticktick://project?from=select-common":
		{
			UserActCollectUtils.AddClickEvent("preset_list", "preset_list_btn", "select_list");
			if (!UserDao.IsPro() && CacheManager.GetProjectsWithoutInbox().Count((ProjectModel p) => !p.delete_status) >= 9)
			{
				ProChecker.CheckPro(ProType.MoreLists);
				break;
			}
			ChooseCommonProjectWindow chooseCommonProjectWindow = new ChooseCommonProjectWindow();
			chooseCommonProjectWindow.Owner = Window.GetWindow(this);
			chooseCommonProjectWindow.ShowDialog();
			break;
		}
		case "ticktick://matrix?from=detail":
			if (LocalSettings.Settings.ShowMatrix)
			{
				UserActCollectUtils.AddClickEvent("preset_list", "preset_list_btn", "go_matrix");
				App.Window.SwitchModule(DisplayModule.Matrix);
				App.Window.TryShowMainWindow();
				break;
			}
			UserActCollectUtils.AddClickEvent("preset_list", "preset_list_btn", "enable_matrix");
			LocalSettings.Settings.ShowOrHideTabBar("MATRIX", show: true);
			if (_actionButton.Content is EmjTextBlock emjTextBlock3)
			{
				emjTextBlock3.Text = "\ud83d\udc49" + GetActionName(text);
			}
			break;
		case "ticktick://focus?from=detail":
			if (LocalSettings.Settings.EnableFocus)
			{
				UserActCollectUtils.AddClickEvent("preset_list", "preset_list_btn", "go_pomo");
				App.Window.SwitchModule(DisplayModule.Pomo);
				App.Window.TryShowMainWindow();
				break;
			}
			UserActCollectUtils.AddClickEvent("preset_list", "preset_list_btn", "enable_pomo");
			LocalSettings.Settings.ShowOrHideTabBar("POMO", show: true);
			if (_actionButton.Content is EmjTextBlock emjTextBlock2)
			{
				emjTextBlock2.Text = "\ud83d\udc49" + GetActionName(text);
			}
			break;
		case "ticktick://habit?from=detail":
			if (LocalSettings.Settings.ShowHabit)
			{
				UserActCollectUtils.AddClickEvent("preset_list", "preset_list_btn", "go_habit");
				App.Window.SwitchModule(DisplayModule.Habit);
				App.Window.TryShowMainWindow();
				break;
			}
			UserActCollectUtils.AddClickEvent("preset_list", "preset_list_btn", "enable_habit");
			LocalSettings.Settings.ShowOrHideTabBar("HABIT", show: true);
			if (_actionButton.Content is EmjTextBlock emjTextBlock)
			{
				emjTextBlock.Text = "\ud83d\udc49" + GetActionName(text);
			}
			break;
		}
	}

	private async void AddAction(string action)
	{
		InitGuidePanel();
		EmjTextBlock emjTextBlock = new EmjTextBlock
		{
			Background = Brushes.Transparent,
			Text = "\ud83d\udc49 " + GetActionName(action),
			Margin = new Thickness(0.0, 21.0, 0.0, 4.0)
		};
		emjTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
		emjTextBlock.SetResourceReference(TextBlock.FontSizeProperty, "Font14");
		_actionButton.Content = emjTextBlock;
		_actionButton.Tag = action;
		_actionButton.MouseLeftButtonUp -= OnGuideActionClick;
		_actionButton.MouseLeftButtonUp += OnGuideActionClick;
	}

	private async Task AddGuideVideo(string url, string fileName)
	{
		InitGuidePanel();
		if (_videoPlayer == null)
		{
			_videoPlayer = new TTMediaPlayer
			{
				Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
			};
			_videoContainer.Content = _videoPlayer;
		}
		if (!File.Exists(AppPaths.ImageDir + fileName))
		{
			await IOUtils.DownloadFile(AppPaths.ImageDir, fileName, url);
		}
		if (File.Exists(AppPaths.ImageDir + fileName))
		{
			try
			{
				_videoPlayer.PlayVideo(AppPaths.ImageDir + fileName);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		if (_videoContainer != null)
		{
			_videoContainer.Content = null;
		}
		_videoPlayer?.PlayVideo(null);
		_videoPlayer = null;
	}

	private void InitGuidePanel()
	{
		if (_guidePanel == null)
		{
			_guidePanel = new StackPanel();
			_guidePanel.SetValue(Grid.RowProperty, 3);
			_contentGrid.Children.Add(_guidePanel);
		}
		if (_videoContainer == null)
		{
			_videoContainer = new ContentControl
			{
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left
			};
			_guidePanel.Children.Add(_videoContainer);
		}
		if (_actionButton == null)
		{
			_actionButton = new ContentControl
			{
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				Background = Brushes.Transparent,
				Cursor = System.Windows.Input.Cursors.Hand,
				Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
			};
			_guidePanel.Children.Add(_actionButton);
		}
	}

	public void RemoveKeyBinding()
	{
		lock (_keyBindings)
		{
			foreach (KeyValuePair<string, KeyBinding> keyBinding in _keyBindings)
			{
				KeyBindingManager.RemoveKeyBinding(keyBinding.Key, keyBinding.Value);
				keyBinding.Value.CommandParameter = null;
				keyBinding.Value.CommandTarget = null;
			}
			base.InputBindings.Clear();
			_keyBindings.Clear();
		}
		if (_checklist != null)
		{
			_checklist.RemoveEvents();
		}
		RecordLabelTime();
	}

	private void InitShortcut()
	{
		if (Mode != Constants.DetailMode.Sticky)
		{
			AddKeyBinding("SetNoPriority", GetKeyBinding(DetailControlCommands.SetPriorityNoneCommand));
			AddKeyBinding("SetLowPriority", GetKeyBinding(DetailControlCommands.SetPriorityLowCommand));
			AddKeyBinding("SetMediumPriority", GetKeyBinding(DetailControlCommands.SetPriorityMediumCommand));
			AddKeyBinding("SetHighPriority", GetKeyBinding(DetailControlCommands.SetPriorityHighCommand));
			AddKeyBinding("ClearDate", GetKeyBinding(DetailControlCommands.ClearDateCommand));
			AddKeyBinding("SetToday", GetKeyBinding(DetailControlCommands.SetTodayCommand));
			AddKeyBinding("SetTomorrow", GetKeyBinding(DetailControlCommands.SetTomorrowCommand));
			AddKeyBinding("SetNextWeek", GetKeyBinding(DetailControlCommands.SetNextWeekCommand));
			AddKeyBinding("SetDate", GetKeyBinding(DetailControlCommands.SelectDateCommand));
			AddKeyBinding("Print", GetKeyBinding(DetailControlCommands.PrintCommand));
			AddKeyBinding("PinTask", GetKeyBinding(DetailControlCommands.PinCommand));
			AddKeyBinding("OpenSticky", GetKeyBinding(DetailControlCommands.OpenAsSticky));
		}
		AddKeyBinding("CompleteTask", GetKeyBinding(DetailControlCommands.CompleteCommand));
		KeyBinding inputBinding = new KeyBinding(DetailControlCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		};
		base.InputBindings.Add(inputBinding);
		void AddKeyBinding(string key, KeyBinding kb)
		{
			lock (_keyBindings)
			{
				_keyBindings[key] = kb;
				KeyBindingManager.TryAddKeyBinding(key, kb);
			}
		}
		KeyBinding GetKeyBinding(ICommand command)
		{
			if (command != null)
			{
				KeyBinding keyBinding = new KeyBinding(command, new KeyGesture(Key.None))
				{
					CommandParameter = this
				};
				base.InputBindings.Add(keyBinding);
				return keyBinding;
			}
			return null;
		}
	}

	public void SetPriority(int priority)
	{
		if (_task.Enable && _task.Kind != "NOTE")
		{
			ITaskOperation focusedTaskItem = GetFocusedTaskItem();
			if (focusedTaskItem != null)
			{
				focusedTaskItem.SetPriority(priority);
			}
			else
			{
				SetTaskPriority(priority);
			}
		}
	}

	public async void SetDate(string key)
	{
		if (!_task.Enable)
		{
			return;
		}
		ITaskOperation focusedTaskItem = GetFocusedTaskItem();
		if (focusedTaskItem != null)
		{
			focusedTaskItem.SetDate(key);
			return;
		}
		DateTime time = DateTime.Today;
		switch (key)
		{
		case "today":
			time = DateTime.Today;
			UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_today");
			break;
		case "tomorrow":
			time = DateTime.Today.AddDays(1.0);
			UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_tomorrow");
			break;
		case "nextweek":
			time = DateTime.Today.AddDays(7.0);
			UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_next_week");
			break;
		}
		TryClearParseDate();
		await OnDateSelected(time);
	}

	public void SelectDate()
	{
		UserActCollectUtils.AddShortCutEvent("edit_tasks", "set_date");
		if (_task.Enable && !DatePopupShow)
		{
			ITaskOperation focusedTaskItem = GetFocusedTaskItem();
			if (focusedTaskItem != null)
			{
				focusedTaskItem.SelectDate(relative: false);
			}
			else
			{
				ShowSelectDateDialog();
			}
		}
	}

	public async void ClearDate()
	{
		if (_task.Enable)
		{
			ITaskOperation focusedTaskItem = GetFocusedTaskItem();
			if (focusedTaskItem != null)
			{
				focusedTaskItem.ClearDate();
			}
			else
			{
				await ClearTaskDate(_task);
			}
		}
	}

	public async void ToggleTaskCompleted()
	{
		if (_task.Enable && _task.Kind != "NOTE")
		{
			UserActCollectUtils.AddShortCutEvent("edit_tasks", "complete_tasks");
			ITaskOperation focusedTaskItem = GetFocusedTaskItem();
			if (focusedTaskItem != null)
			{
				focusedTaskItem.ToggleTaskCompleted();
			}
			else
			{
				await CompleteOrUndoneTask();
			}
		}
	}

	public async void OnExport(bool markdown)
	{
		if (_task == null)
		{
			return;
		}
		OnShowDialog();
		try
		{
			TaskDetailViewModel task = _task;
			string description = ((!(_task.Kind == "CHECKLIST")) ? (_contentText?.GetPersistedText() ?? _task.TaskContent) : (_descText?.GetPersistedText() ?? _task.Desc));
			string checklist = ((_task.Kind == "CHECKLIST" && _checklist != null) ? string.Join("\n", from item in _checklist.ChecklistItems
				where item.IsValid
				select ((item.Status == 0) ? "- [ ] " : "- [x] ") + item.Title) : string.Empty);
			string plainChecklist = ((_task.Kind == "CHECKLIST" && _checklist != null) ? string.Join("\n", from item in _checklist.ChecklistItems
				where item.IsValid
				select ((item.Status == 0) ? "- [ ] " : "- [x] ") + LinkTextEditBox.GetPlainTextWithLinks(item.Title)) : string.Empty);
			string title = _titleText.Text ?? task.Title;
			string plainTitle = _titleText.GetDisplayText();
			string attachments = string.Empty;
			if (task.Kind == "CHECKLIST")
			{
				List<AttachmentModel> list = await AttachmentDao.GetTaskAttachments(task.TaskId, active: true);
				if (_task != task)
				{
					return;
				}
				if (list != null && list.Count > 0)
				{
					List<AttachmentModel> attachments2 = (from model in list.Select((AttachmentModel model) => new AttachmentViewModel(model, task.ImageMode)).OrderBy((AttachmentViewModel model) => model, new AttachmentComparer())
						select model.AttachmentModel).ToList();
					attachments = AttachmentDao.AddAttachmentStrings(string.Empty, attachments2, preserveOrder: true);
				}
			}
			TaskExportWindow taskExportWindow = new TaskExportWindow(title, description, markdown, checklist, plainTitle, plainChecklist, attachments);
			taskExportWindow.Owner = Window.GetWindow(this);
			taskExportWindow.ShowDialog();
		}
		finally
		{
			OnCloseDialog();
		}
	}

	public async void OnPrint()
	{
		SetPopupClosing();
		this.ForceHideWindow?.Invoke(this, null);
		object dataContext = base.DataContext;
		TaskDetailViewModel model = dataContext as TaskDetailViewModel;
		if (model == null)
		{
			return;
		}
		string avatarUrl = await AvatarHelper.GetAvatarUrl(model.Assignee, model.ProjectId);
		TaskDetailPrintViewModel printModel = new TaskDetailPrintViewModel(model)
		{
			Avatar = AvatarHelper.GetAvatarByUrl(avatarUrl)
		};
		List<CommentViewModel> source = await GetLocalCommentViewModels(model.TaskId, model.ProjectId, model.Deleted);
		if (source.Any((CommentViewModel c) => c.Model.taskSid == model.TaskId))
		{
			printModel.Comments = source.Where((CommentViewModel c) => c.Model.deleted == 0).ToList();
		}
		List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(model.TaskId);
		if (list != null && list.Count > 0)
		{
			list.Sort(delegate(TaskDetailItemModel a, TaskDetailItemModel b)
			{
				if (a.status != 0 && b.status == 0)
				{
					return 1;
				}
				if (a.status == 0 && b.status != 0)
				{
					return -1;
				}
				return (a.status != 0 && b.status != 0 && a.completedTime.HasValue && b.completedTime.HasValue) ? b.completedTime.Value.CompareTo(a.completedTime.Value) : a.sortOrder.CompareTo(b.sortOrder);
			});
			printModel.SubtaskPrintViewModels = list.Select((TaskDetailItemModel i) => new SubtaskPrintViewModel(i)).ToList();
		}
		SubtaskTreeBuildResult taskTree = await BuildSubtaskTreeAsync(model.TaskId, model.ProjectId, !LocalSettings.Settings.HideComplete);
		await RemoveInvalidChildIdsAsync(taskTree);
		List<DisplayItemModel> allChildren = taskTree.AllChildren;
		if (allChildren != null && allChildren.Count > 0)
		{
			printModel.RealSubtaskPrintViewModels = allChildren.Select((DisplayItemModel i) => new SubtaskPrintViewModel(i)).ToList();
		}
		await printModel.LoadAttachment();
		PrintPreviewWindow printWindow = new PrintPreviewWindow(printModel);
		await Task.Delay(300);
		printWindow.Show();
		printWindow.Activate();
	}

	public void OnEsc()
	{
		this.EscKeyUp?.Invoke(this, null);
	}

	public async void OnPin()
	{
		if (CheckEnable())
		{
			await TaskService.TogglesStarred(projectId: (Utils.FindParent<ListViewContainer>(this)?.GetSelectedProject())?.CatId, taskId: _task.TaskId);
		}
	}

	private ITaskOperation GetFocusedTaskItem()
	{
		if (_subtaskGrid != null)
		{
			IInputElement focusedElement = Keyboard.FocusedElement;
			if (focusedElement is ITaskOperation result)
			{
				return result;
			}
			if (focusedElement is TextArea child)
			{
				ITaskOperation taskOperation = Utils.FindParent<ITaskOperation>(child);
				if (taskOperation != null)
				{
					return taskOperation;
				}
			}
		}
		return null;
	}

	public void OpenAsSticky()
	{
		if (!_task.IsNewAdd)
		{
			UserActCollectUtils.AddShortCutEvent("edit_tasks", "open_as_sticky_note");
			this.ForceHideWindow?.Invoke(this, null);
			TaskStickyWindow.ShowTaskSticky(new List<string> { _task.TaskId });
		}
	}

	public void DeleteTask()
	{
		OnDelete();
	}

	public TaskDetailView()
		: this(Constants.DetailMode.Page)
	{
	}

	public TaskDetailView(Constants.DetailMode mode)
	{
		Mode = mode;
		base.DataContext = new TaskDetailViewModel(new TaskBaseViewModel());
		InitShortcut();
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition());
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.PreviewKeyDown += OnDetailKeyDown;
		base.SizeChanged += OnDetailSizeChanged;
		InitDetailHead();
		InitScrollContent(mode);
		InitBottom();
		base.Loaded += OnLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
	}

	private void InitDetailHead()
	{
		if (Mode != Constants.DetailMode.Sticky && Mode != Constants.DetailMode.Editor)
		{
			_detailHead = new TaskDetailHeadView
			{
				Margin = new Thickness(21.0, (Mode == Constants.DetailMode.Page) ? 25 : 4, 20.0, 0.0)
			};
			base.Children.Add(_detailHead);
		}
	}

	private void InitBottom()
	{
		if (Mode != Constants.DetailMode.Sticky && Mode != Constants.DetailMode.Editor)
		{
			_detailBottom = new TaskDetailBottomControl(this);
			_detailBottom.SetValue(Grid.RowProperty, 4);
			base.Children.Add(_detailBottom);
		}
	}

	private void InitScrollContent(Constants.DetailMode mode)
	{
		_detailScrollViewer.Margin = new Thickness(0.0, 0.0, 3.0, 0.0);
		_detailScrollViewer.SetResourceReference(FrameworkElement.StyleProperty, "for_scrollviewer");
		_detailScrollViewer.SetValue(Grid.RowProperty, 1);
		_detailScrollViewer.Drop += OnAttachmentDrop;
		_detailScrollViewer.PreviewMouseWheel += OnMouseScrollOnTaskDetail;
		_detailScrollViewer.ScrollChanged += TaskDetailScrollChanged;
		_detailScrollViewer.MouseLeftButtonUp += OnDetailContentClick;
		base.Children.Add(_detailScrollViewer);
		_scrollContent = new Grid
		{
			Margin = GetScrollContentMargin(mode)
		};
		_scrollContent.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_scrollContent.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_scrollContent.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_scrollContent.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_scrollContent.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_scrollContent.RowDefinitions.Add(new RowDefinition());
		_scrollContent.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_scrollButtonRow = new RowDefinition
		{
			Height = new GridLength(0.0)
		};
		_scrollContent.RowDefinitions.Add(_scrollButtonRow);
		_detailScrollViewer.Content = _scrollContent;
		_titleUpperContent = new Grid
		{
			Margin = new Thickness(0.0, (Mode != Constants.DetailMode.Sticky) ? 10 : 0, 0.0, 0.0)
		};
		_scrollContent.Children.Add(_titleUpperContent);
		InitDetailTitle();
		InitContentPanel();
	}

	private static Thickness GetScrollContentMargin(Constants.DetailMode mode, bool showTextEditorMenu = false)
	{
		if (mode == Constants.DetailMode.Sticky)
		{
			return new Thickness(10.0, 5.0, 10.0, 5.0);
		}
		double bottom = ((mode == Constants.DetailMode.Editor || showTextEditorMenu) ? 56.0 : 0.0);
		return new Thickness(20.0, 0.0, 20.0, bottom);
	}

	private void SetUpperContent(TaskDetailViewModel model)
	{
		SetParentTitle(model, checkPomo: false);
		TryLoadPomoCount(model);
	}

	private void SetParentTitle(TaskDetailViewModel model, bool checkPomo = true)
	{
		bool num = Mode != Constants.DetailMode.Sticky && Mode != Constants.DetailMode.Editor;
		TaskBaseViewModel taskBaseViewModel = (string.IsNullOrEmpty(model.ParentId) ? null : TaskCache.GetTaskById(model.ParentId));
		if (num && !CanShowParent(model, taskBaseViewModel))
		{
			taskBaseViewModel = TaskCache.FindParentByTaskTree(model.TaskId, model.ProjectId);
		}
		if (num && CanShowParent(model, taskBaseViewModel))
		{
			SetPrentTitle(taskBaseViewModel);
		}
		else
		{
			ClearParentTitle();
		}
		if (checkPomo)
		{
			TaskDetailPomoSummaryControl taskDetailPomoSummaryControl = Utils.FindSingleVisualChildren<TaskDetailPomoSummaryControl>(_titleUpperContent);
			if (taskDetailPomoSummaryControl != null)
			{
				taskDetailPomoSummaryControl.Margin = new Thickness(2.0, (GetParentTitle() == null) ? (-4) : 20, 0.0, 0.0);
			}
		}
	}

	private static bool CanShowParent(TaskDetailViewModel model, TaskBaseViewModel parent)
	{
		if (parent != null && parent.ProjectId == model.ProjectId && parent.Deleted == 0)
		{
			return parent.Kind != "NOTE";
		}
		return false;
	}

	private async void TryLoadPomoCount(TaskDetailViewModel model)
	{
		bool num = Mode != Constants.DetailMode.Sticky && Mode != Constants.DetailMode.Editor;
		TaskDetailPomoSummaryControl pomoCount = Utils.FindSingleVisualChildren<TaskDetailPomoSummaryControl>(_titleUpperContent);
		if (!num || !LocalSettings.Settings.EnableFocus)
		{
			if (pomoCount != null)
			{
				_titleUpperContent.Children.Remove(pomoCount);
			}
			return;
		}
		PomodoroSummaryModel pomodoroSummaryModel = await PomoService.GetAdjustPomoSummary(model.TaskId);
		if (pomodoroSummaryModel != null && GetDisplayDateDiff() > 0.0)
		{
			pomodoroSummaryModel.PomoDuration = 0L;
			pomodoroSummaryModel.count = 0;
			pomodoroSummaryModel.StopwatchDuration = 0L;
			pomodoroSummaryModel.focuses = null;
		}
		if (pomodoroSummaryModel != null && !pomodoroSummaryModel.IsEmpty())
		{
			if (pomoCount == null)
			{
				pomoCount = new TaskDetailPomoSummaryControl
				{
					Background = Brushes.Transparent,
					Cursor = System.Windows.Input.Cursors.Hand,
					HorizontalAlignment = System.Windows.HorizontalAlignment.Left
				};
				pomoCount.MouseUp += OnTaskPomoClick;
				_titleUpperContent.Children.Add(pomoCount);
			}
			pomoCount.Margin = new Thickness(2.0, (GetParentTitle() == null) ? (-4) : 20, 0.0, 0.0);
			pomoCount.SetData(pomodoroSummaryModel);
		}
		else
		{
			_titleUpperContent.Children.Remove(pomoCount);
		}
	}

	private EmjTextBlock GetParentTitle()
	{
		return Utils.FindSingleVisualChildren<EmjTextBlock>(_titleUpperContent);
	}

	private void SetPrentTitle(TaskBaseViewModel parent)
	{
		EmjTextBlock parentTitle = GetParentTitle();
		if (parentTitle != null)
		{
			parentTitle.Text = parent.Title;
			return;
		}
		_parentTitleBorder = new Border
		{
			Cursor = System.Windows.Input.Cursors.Hand,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		_parentTitleBorder.MouseEnter += OnParentTitleMouseEnter;
		_parentTitleBorder.MouseLeave += OnParentTitleMouseLeave;
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		};
		grid.MouseLeftButtonUp += OnParentTitleClick;
		_parentTitleBorder.Child = grid;
		EmjTextBlock emjTextBlock = new EmjTextBlock
		{
			Background = Brushes.Transparent,
			TextTrimming = TextTrimming.CharacterEllipsis,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			Padding = new Thickness(2.0, 2.0, 15.0, 2.0)
		};
		emjTextBlock.SetResourceReference(FrameworkElement.StyleProperty, "Body05");
		emjTextBlock.SetResourceReference(TextBlock.FontSizeProperty, "Font13");
		emjTextBlock.Text = parent.Title;
		grid.Children.Add(emjTextBlock);
		System.Windows.Shapes.Path arrow = UiUtils.GetArrow(12.0, -90.0, "BaseColorOpacity40");
		arrow.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		grid.Children.Add(arrow);
		_titleUpperContent.Children.Add(_parentTitleBorder);
	}

	private void ClearParentTitle()
	{
		HideLinkParentTaskPopup();
		if (_parentTitleBorder != null)
		{
			_parentTitleBorder.MouseEnter -= OnParentTitleMouseEnter;
			_parentTitleBorder.MouseLeave -= OnParentTitleMouseLeave;
			_titleUpperContent.Children.Remove(_parentTitleBorder);
			if (_linkParentTaskPopup != null && _linkParentTaskPopup.PlacementTarget == _parentTitleBorder)
			{
				_linkParentTaskPopup.PlacementTarget = null;
			}
			_parentTitleBorder = null;
		}
	}

	private async void OnParentTitleMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (!(sender is Border parentTitleBorder))
		{
			return;
		}
		await Task.Delay(100);
		Point startPos = Mouse.GetPosition(this);
		await Task.Delay(100);
		if (parentTitleBorder != _parentTitleBorder || !parentTitleBorder.IsMouseOver)
		{
			return;
		}
		Point position = Mouse.GetPosition(this);
		if (!(Math.Pow(startPos.X - position.X, 2.0) + Math.Pow(startPos.Y - position.Y, 2.0) > 10.0) && (_linkParentTaskPopup == null || !_linkParentTaskPopup.IsOpen))
		{
			if (_linkParentTaskPopup == null)
			{
				_linkParentTaskPopup = new Popup
				{
					StaysOpen = true,
					AllowsTransparency = true,
					Placement = PlacementMode.Relative,
					PopupAnimation = PopupAnimation.Fade,
					Child = LinkParentTaskMenu
				};
			}
			Point position2 = Mouse.GetPosition(this);
			_linkParentTaskPopup.PlacementTarget = parentTitleBorder;
			_linkParentTaskPopup.Placement = PlacementMode.Top;
			_linkParentTaskPopup.HorizontalOffset = position2.X - 60.0;
			_linkParentTaskPopup.IsOpen = true;
		}
	}

	private async void OnParentTitleMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		Border parentTitleBorder = sender as Border;
		if (_linkParentTaskPopup != null && parentTitleBorder != null)
		{
			await Task.Delay(300);
			if (_linkParentTaskPopup != null && parentTitleBorder == _parentTitleBorder && !_linkParentTaskPopup.IsMouseOver && !_parentTitleBorder.IsMouseOver)
			{
				_linkParentTaskPopup.IsOpen = false;
			}
		}
	}

	private async void LinkParentTaskMenu_EditorAction(object sender, string tag)
	{
		if (_linkParentTaskPopup == null)
		{
			return;
		}
		switch (tag)
		{
		case "Edit":
			App.Window.StartLinkSearch(new List<string> { TaskId });
			break;
		case "Unlink":
		{
			List<string> tasks = new List<string> { TaskId };
			await TaskDao.BatchUpdateParent(tasks, string.Empty, null, canUndo: false);
			TaskChangeNotifier.NotifyTaskParentChanged(tasks);
			App.Window?.TryToastString(null, Utils.GetString("LinkRemoved"));
			break;
		}
		case "Leave":
			await Task.Delay(300);
			if (_parentTitleBorder?.IsMouseOver ?? false)
			{
				return;
			}
			break;
		}
		_linkParentTaskPopup.IsOpen = false;
	}

	private void OnTaskPomoClick(object sender, MouseButtonEventArgs e)
	{
		if (CheckEnable() && _task != null && _task.Status == 0)
		{
			EscPopup popup = new EscPopup
			{
				HorizontalOffset = -5.0,
				VerticalOffset = -10.0,
				Placement = PlacementMode.Bottom,
				StaysOpen = false
			};
			popup.PlacementTarget = sender as UIElement;
			popup.Closed += delegate
			{
				SetPopupClosing();
			};
			popup.Opened += delegate
			{
				SetPopupShow();
			};
			TaskPomoSetDialog taskPomoSetDialog = new TaskPomoSetDialog();
			taskPomoSetDialog.Closed += delegate
			{
				popup.IsOpen = false;
			};
			popup.Child = taskPomoSetDialog;
			taskPomoSetDialog.InitData(_task.TaskId, force: true, "task_detail", "task_detail_pomo");
			popup.IsOpen = true;
		}
	}

	private void OnParentTitleClick(object sender, MouseButtonEventArgs e)
	{
		if (!string.IsNullOrEmpty(_task.ParentId))
		{
			Navigate(_task.ParentId);
			this.TaskNavigated?.Invoke(this, _task.ParentId);
			e.Handled = true;
		}
		if (_linkParentTaskPopup != null && _linkParentTaskPopup.IsOpen)
		{
			_linkParentTaskPopup.IsOpen = false;
		}
	}

	private void TaskDetailScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		_commentDisplayCtrl?.UpdateRecentCommentAnchor();
	}

	public async Task TaskSelect(string taskId, string itemId)
	{
		SearchHelper.DetailChanged = false;
		if (taskId != _task.TaskId)
		{
			_detailScrollViewer.ScrollToTop();
		}
		await Navigate(taskId, itemId);
		await Task.Delay(10);
		if (!LocalSettings.Settings.InSearch)
		{
			return;
		}
		double firstSearchIndex = _titleText.GetFirstSearchIndex();
		if (firstSearchIndex > 0.0)
		{
			KeepOffsetInView(_titleText, firstSearchIndex);
		}
		else if (_contentText != null)
		{
			double firstSearchIndex2 = _contentText.GetFirstSearchIndex();
			if (firstSearchIndex2 > 0.0)
			{
				KeepOffsetInView(_contentText, firstSearchIndex2);
			}
		}
		else if (_descText != null)
		{
			double firstSearchIndex3 = _descText.GetFirstSearchIndex();
			if (firstSearchIndex3 > 0.0)
			{
				KeepOffsetInView(_descText, firstSearchIndex3);
			}
			else
			{
				_checklist?.ScrollToSearchOffset();
			}
		}
	}

	public async void TaskAdded(TaskModel task)
	{
		await Navigate(task.id);
	}

	private void OnMouseScrollOnTaskDetail(object sender, MouseWheelEventArgs e)
	{
		if (_tagsControl != null && _tagsControl.AddPopupOpened)
		{
			e.Handled = true;
		}
		else if (_canScroll && e != null)
		{
			_detailScrollViewer.ScrollToVerticalOffset(_detailScrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		BindEvents();
	}

	public double GetDisplayDateDiff()
	{
		TaskDetailViewModel task = _task;
		if (task != null && task.StartDate.HasValue)
		{
			TaskDetailViewModel task2 = _task;
			if (task2 != null && task2.DisplayStartDate.HasValue)
			{
				return (_task.DisplayStartDate.Value - _task.StartDate.Value).TotalDays;
			}
		}
		return 0.0;
	}

	public DateTime? GetDisplayDate()
	{
		if (_task.Status != 0)
		{
			return null;
		}
		TaskDetailViewModel task = _task;
		if (task != null && task.StartDate.HasValue)
		{
			TaskDetailViewModel task2 = _task;
			if (task2 != null && task2.DisplayStartDate.HasValue && !(_task.StartDate.Value == _task.DisplayStartDate.Value))
			{
				return _task.DisplayStartDate;
			}
		}
		return null;
	}

	public bool CheckEnable()
	{
		if (!_task.Enable && _task.Deleted == 0)
		{
			ProjectModel projectById = CacheManager.GetProjectById(_task.ProjectId);
			if (projectById != null && projectById.IsValid())
			{
				IsNoPermission();
			}
		}
		return _task.Enable;
	}

	public void TryToast(string getString, bool inMainWindow = false)
	{
		if (!inMainWindow)
		{
			Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, getString);
		}
		else
		{
			Utils.Toast(getString);
		}
	}

	private async void ShowSelectDateDialog()
	{
		TaskDetailViewModel task = _task;
		if (task == null)
		{
			return;
		}
		await TrySaveParseDate();
		if (_task != task || !base.IsLoaded)
		{
			return;
		}
		TimeData timeData = task.GetTimeData();
		TaskModel taskModel = await TaskDao.GetThinTaskById(task.TaskId);
		if (_task != task || !base.IsLoaded)
		{
			return;
		}
		bool canSkip = !TaskService.IsNonRepeatTask(taskModel);
		bool isNote = task.Kind == "NOTE";
		SetDateDialog dialog = SetDateDialog.GetDialog();
		dialog.ClearEventHandle();
		dialog.Clear += async delegate
		{
			if (_task == task)
			{
				if (task.IsNewAdd)
				{
					task.SetTimeData(new TimeData());
				}
				else
				{
					await ClearTaskDate(task);
				}
			}
		};
		dialog.Save += async delegate(object obj, TimeData model)
		{
			if (_task == task)
			{
				TaskService.TryFixRepeatFlag(model);
				AddActionEvent("action", "date");
				if (task.RepeatDiff.HasValue)
				{
					ModifyRepeatHandler.TryUpdateDueDate(task.TaskId, task.DisplayStartDate, task.DisplayDueDate, model, 1, 1);
				}
				else
				{
					await SaveTaskDate(task, model);
					this.TaskDateSaved?.Invoke(this, task.TaskId);
				}
			}
		};
		dialog.Hided += async delegate
		{
			if (_task == task && (!task.RepeatDiff.HasValue || !ModifyRepeatDialog.Showing))
			{
				Window window = Window.GetWindow(this);
				if (!(window is LoadMoreWindow))
				{
					window?.Activate();
				}
			}
			await Task.Delay(50);
			DatePopupShow = false;
			SetPopupClosing();
			SetValue(InputMethod.IsInputMethodEnabledProperty, true);
			if (_task == task)
			{
				HwndHelper.SetFocus(this);
			}
		};
		dialog.SkipRecurrence += delegate(object sender, EventArgs args)
		{
			OnSkipRecurrence(task, sender, args);
		};
		UIElement uIElement = _detailHead?.GetDateDropTarget();
		Point point = ((uIElement != null) ? new Point(-32.0, 22.0) : TranslatePoint(new Point((task.ShowCheckIcon == Visibility.Visible) ? 50 : 24, (_detailHead?.Margin.Top ?? 0.0) + 32.0), this));
		dialog.Show(timeData, new SetDateDialogArgs(calendarMode: false, isNote, itemMode: false, uIElement ?? this, point.X, point.Y, PlacementMode.Relative, showQuickDate: true, canSkip));
		UtilLog.Info("DetailShowSetDate");
		SetPopupShow();
		DatePopupShow = true;
	}

	public void PopupOpened(object sender, EventArgs e)
	{
		SetPopupShow();
	}

	public async void PopupClosed(object sender, EventArgs e)
	{
		SetPopupClosing();
	}

	public void OnMoveProject(EscPopup popup, bool inBatch = false)
	{
		if (!IsNoPermission())
		{
			ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(popup, new ProjectExtra
			{
				ProjectIds = new List<string> { _task.ProjectId }
			}, new ProjectSelectorExtra
			{
				showAll = false,
				batchMode = false,
				canSelectGroup = false,
				onlyShowPermission = true,
				CanSearch = true,
				ShowColumn = true,
				ColumnId = _task.ColumnId
			});
			projectOrGroupPopup.ItemSelect += delegate(object o, SelectableItemViewModel e)
			{
				OnMoveProject(e);
			};
			projectOrGroupPopup.Show();
		}
	}

	private async void OnMoveProject(SelectableItemViewModel e)
	{
		if (e == null)
		{
			return;
		}
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string projectId = projectAndColumnId.Item1;
		string item = projectAndColumnId.Item2;
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null)
		{
			if (_task != null && _task.IsNewAdd)
			{
				_task.SourceViewModel.ProjectId = projectModel.id;
				_task.SourceViewModel.ColumnId = item;
				return;
			}
			IsBlank = false;
			UtilLog.Info("TaskDetail.SetProject " + _task?.TaskId + ", value " + projectModel.id + " from:ProjectSelector");
			AddActionEvent("action", "move_project");
			await MoveProject(projectModel, item);
		}
	}

	private async Task MoveProject(ProjectModel project, string columnId = null)
	{
		string beforeProjectId = _task.ProjectId;
		await TaskService.TryMoveProject(new MoveProjectArgs(_task.TaskId, project)
		{
			ColumnId = columnId
		});
		TryToastQuadrantChanged(_task.TaskId);
		if (project.id != beforeProjectId)
		{
			Utils.GetToastWindow()?.ToastMoveProjectControl(project.id, string.Empty);
		}
	}

	private async Task CompleteOrUndoneTask(bool? isComplete = null, bool inBatch = false)
	{
		SetPopupClosing();
		int num = ((_task.Status == 0) ? 2 : 0);
		isComplete = isComplete ?? (num == 2);
		if (isComplete.Value && _task.StartDate.HasValue && _task.DisplayStartDate.HasValue && _task.StartDate.Value.Date != _task.DisplayStartDate.Value.Date)
		{
			IToastShowWindow toastWindow = Utils.FindParent<TaskDetailPopup>(this)?.DependentWindow;
			if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(_task.TaskId, _task.DisplayStartDate, toastWindow)))
			{
				return;
			}
		}
		await ToggleComplete(isComplete.Value);
	}

	private async Task ToggleComplete(bool isComplete)
	{
		bool toastInMainWindow = false;
		if (!string.IsNullOrEmpty(_task.RepeatFlag) && isComplete)
		{
			TaskDetailPopup taskDetailPopup = Utils.FindParent<TaskDetailPopup>(this);
			if (taskDetailPopup != null && taskDetailPopup.ShowInCal)
			{
				taskDetailPopup.Close();
				toastInMainWindow = true;
			}
		}
		int num = (isComplete ? 2 : 0);
		TaskCloseExtra obj = await TaskService.SetTaskStatus(_task.TaskId, num, num == 2, null, inDetail: true);
		SetProgress(_task.Progress.GetValueOrDefault(), withAnim: true);
		if (obj?.RepeatTask != null)
		{
			Reload(_task.TaskId);
			TryToast(Utils.GetString("CreatedTheNextCircle"), toastInMainWindow);
		}
		else if (isComplete && Mode == Constants.DetailMode.Sticky)
		{
			TryToast(Utils.GetString("TaskCompleted"), toastInMainWindow);
		}
	}

	public async void OnSetAssigneeMouseUp(bool inBatch, Popup popup = null)
	{
	}

	private async Task SetAssigneeImage(string assignee, string projectId)
	{
		_detailHead?.SetAvatarVisible(showAssign: true, assignee, projectId);
	}

	public async Task SetAssignee(string assignee, bool inBatch)
	{
		TryFocusDetail();
		if (_task.IsNewAdd)
		{
			_task.SourceViewModel.Assignee = assignee;
			return;
		}
		await UpdateTaskWithUndo(SetAssign());
		SetAssigneeImage(assignee, _task.ProjectId);
		async Task SetAssign()
		{
			await Task.Yield();
			await TaskService.SetAssignee(_task.TaskId, assignee);
		}
	}

	public void OnBackClick()
	{
		if (Mode == Constants.DetailMode.Page)
		{
			_detailHead?.SetBackIcon(show: false);
			TaskView taskView = Utils.FindParent<TaskView>(this);
			if (taskView != null)
			{
				taskView.TryFoldDetail();
			}
			else
			{
				this.NavigateBack?.Invoke(this, EventArgs.Empty);
			}
		}
		else
		{
			this.NavigateBack?.Invoke(this, null);
		}
	}

	public async void OnDelete()
	{
		if (IsNoPermission())
		{
			return;
		}
		if (!KeepDetailOnDelete)
		{
			OnBackClick();
		}
		AddActionEvent("om", "delete");
		UtilLog.Info("TaskDetail.DeleteTask " + _task.TaskId);
		if (!string.IsNullOrEmpty(_task.AttendId))
		{
			if (await TaskOperationHelper.CheckIfAllowDeleteTask(_task.TaskId, this))
			{
				await DeleteAgenda();
			}
			return;
		}
		if (_task?.RepeatDiff.HasValue ?? false)
		{
			TaskDetailPopup taskDetailPopup = Utils.FindParent<TaskDetailPopup>(this);
			if (taskDetailPopup != null)
			{
				IToastShowWindow toastWindow = taskDetailPopup.DependentWindow ?? Utils.FindParent<IToastShowWindow>(taskDetailPopup.Target);
				ModifyRepeatHandler.TryDeleteRecurrence(_task.TaskId, _task.DisplayStartDate, _task.DisplayDueDate, toastWindow);
				taskDetailPopup.TryClose();
				return;
			}
		}
		this.ShowUndoOnTaskDeleted?.Invoke(this, _task.TaskId);
	}

	private async Task DeleteAgenda()
	{
		await TaskService.DeleteAgenda(_task.TaskId, _task.ProjectId, _task.AttendId);
		this.TaskDeleted?.Invoke(this, _task.TaskId);
		OnBackClick();
	}

	private async Task DeleteTaskForever(int status = 1)
	{
		await TaskService.DeleteTask(_task.TaskId, status);
		this.TaskDeleted?.Invoke(this, _task.TaskId);
		this.NotifyCloseWindow?.Invoke(this, null);
		OnBackClick();
	}

	public async void OnRestoreProject()
	{
		if (!IsNoPermission())
		{
			await TaskService.BatchRestoreProject(new List<string> { _task.TaskId });
		}
	}

	public void AddActionEvent(string ctype, string label)
	{
		UserActCollectUtils.AddClickEvent("task_detail", ctype, label);
	}

	public void OnStarClick()
	{
		AddActionEvent("om", "pin_or_unpin");
		OnPin();
	}

	public void SetNeedFocusDetail(bool e)
	{
		_needFocusDetail = e;
	}

	public void SetPopupStatus(bool show)
	{
		if (show)
		{
			SetPopupShow();
		}
		else
		{
			SetPopupClosing();
		}
	}

	private async void SetPopupClosing()
	{
		if (!_popupShowing)
		{
			return;
		}
		if (_skipNextPopupClose)
		{
			_skipNextPopupClose = false;
			return;
		}
		_popupClosing = true;
		await Task.Delay(140);
		if (_popupClosing)
		{
			_popupShowing = false;
			_popupClosing = false;
			Window.GetWindow(this)?.Activate();
			if (_needFocusDetail)
			{
				TryFocusDetail();
				_needFocusDetail = false;
			}
		}
	}

	public void SkipNextPopupClose()
	{
		_skipNextPopupClose = true;
		SetPopupShow();
	}

	public void SetPopupShow()
	{
		_popupClosing = false;
		_popupShowing = true;
	}

	private void TryDisplayTags()
	{
		List<string> tags = TagSerializer.ToTags(_task.Tag);
		LoadTags(tags);
	}

	private async Task UpdateTaskWithUndo(Task t)
	{
		if (ParentIdentity is FilterProjectIdentity)
		{
			TaskModel task = await TaskDao.GetTaskById(_task.TaskId);
			await t;
			TryToastTaskChangeUndo(task);
		}
		else
		{
			await t;
		}
	}

	private void TryToastTaskChangeUndo(TaskModel task)
	{
		if (task != null && TaskViewModelHelper.GetMatchedTasks(ParentIdentity, new List<string> { task.id }).Count == 0)
		{
			Utils.FindParent<IToastShowWindow>(this).Toast(new UndoToast(new TaskUndo(task, string.Empty, Utils.GetString("TaskHasBeenFiltered"))));
			UndoHelper.NeedToastFilteredTaskId = string.Empty;
		}
	}

	private async Task TryToastQuadrantChanged(string taskId)
	{
		if (_quadrantLevel != 0 && !_task.IsNewAdd)
		{
			(int, string) obj = await MatrixManager.GetTaskQuadrantChangeString(_quadrantLevel, taskId);
			int item = obj.Item1;
			string item2 = obj.Item2;
			_quadrantLevel = item;
			if (!string.IsNullOrEmpty(item2))
			{
				TryToast(item2);
			}
		}
	}

	public void Reload(string taskId = null, bool loadComment = true)
	{
		DelayActionHandlerCenter.TryDoAction(_uid + "Reload", delegate
		{
			base.Dispatcher.Invoke(delegate
			{
				if (string.IsNullOrEmpty(taskId) || taskId == _task.TaskId)
				{
					Navigate(_task.TaskId, "", forceLoad: true, loadComment);
				}
			});
		}, 150);
	}

	internal void SetInQuadrant(int quadrantLevel)
	{
		_quadrantLevel = quadrantLevel;
	}

	public int GetQuadrantLevel()
	{
		return _quadrantLevel;
	}

	private async Task TryDelayEditSaveSync(TaskDetailViewModel taskToSave = null)
	{
		await SaveTask(taskToSave ?? _task);
		DelaySync();
	}

	private async void DelaySync()
	{
		if (!_popupShowing)
		{
			SyncManager.TryDelaySync();
		}
	}

	public async void EnterImmersivePage()
	{
		if (_task.Enable)
		{
			MarkDownEditor markDownEditor = _contentText ?? _descText;
			SetPendingImmersiveSource();
			this.EnterImmersive?.Invoke(_task.TaskId, markDownEditor?.EditBox.CaretOffset ?? 0);
		}
	}

	public void ShowBackMenu(bool hideImmersive = true)
	{
		_detailHead?.SetBackIcon(show: true);
		if (hideImmersive)
		{
			_textEditorMenu?.HideImmersive();
		}
		else
		{
			_textEditorMenu?.ShowImmersive();
		}
	}

	public void HideBackMenu()
	{
		_detailHead?.SetBackIcon(show: false);
		_textEditorMenu?.ShowImmersive();
	}

	public async Task Navigate(string taskId, string itemId = "", bool forceLoad = true, bool loadComment = true)
	{
		if (!forceLoad && _task.TaskId != taskId)
		{
			return;
		}
		bool hideEdit = _task.TaskId != taskId;
		if (string.IsNullOrEmpty(taskId) || !(await FlushPendingTaskContentBeforeNavigationAsync()))
		{
			return;
		}
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		await TaskDao.AssembleFullTask(task);
		if (task == null)
		{
			return;
		}
		await Navigate(new TaskDetailViewModel(task), itemId, hideEdit, loadComment);
		await Task.Delay(50);
		if (!LocalSettings.Settings.InSearch || Mode != Constants.DetailMode.Page)
		{
			return;
		}
		double num = _titleText?.GetFirstSearchIndex() ?? 0.0;
		if (num > 0.0)
		{
			KeepOffsetInView(_titleText, num);
		}
		else if (_contentText != null && _contentText.Visibility == Visibility.Visible)
		{
			double firstSearchIndex = _contentText.GetFirstSearchIndex();
			if (firstSearchIndex > 0.0)
			{
				KeepOffsetInView(_contentText, firstSearchIndex);
			}
		}
		else if (_descText != null && _descText.Visibility == Visibility.Visible)
		{
			double firstSearchIndex2 = _descText.GetFirstSearchIndex();
			if (firstSearchIndex2 > 0.0)
			{
				KeepOffsetInView(_descText, firstSearchIndex2);
			}
			else
			{
				_checklist?.ScrollToSearchOffset();
			}
		}
	}

	public async Task Navigate(TaskDetailViewModel taskModel, string itemId = "", bool hideEditor = true, bool loadComment = true)
	{
		await base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			if (await FlushPendingTaskContentBeforeNavigationAsync())
			{
				_navigating = true;
				UtilLog.Info("NavigateDetail TaskId: " + taskModel.TaskId + " ,commentCount " + taskModel.CommentCount);
				IsBlank = true;
				taskModel.Element = this;
				taskModel.NavigateItemId = itemId;
				if (taskModel.TaskId != _task?.TaskId)
				{
					ClearSaveItems();
					if (_subtaskGrid != null)
					{
						_subtaskGrid.Visibility = Visibility.Collapsed;
					}
					_contentGrid.Children.Remove(_hintTextPanel);
					_hintTextPanel = null;
				}
				SetNotionBlock((taskModel.Deleted == 0) ? taskModel.NotionBlock : null);
				bool restoreOffset = _task?.TaskId == taskModel.TaskId && ((_contentText != null && _contentText.CurrentFocused) || _titleText.KeyboardFocused || (_descText != null && _descText.KeyboardFocused));
				bool sameTask = _task?.TaskId == taskModel.TaskId;
				RemoveVmChangeEvent(_task);
				base.DataContext = taskModel;
				TryShowGuide();
				SetVmChangeEvent(taskModel);
				await SetGoogleEventInfoAsync(taskModel.TaskId);
				await InitInputViews(taskModel, restoreOffset, sameTask);
				InitControl();
				SetProgress(_task.Progress.GetValueOrDefault());
				LoadAttachmentAsync();
				TryDisplayTags();
				TryLoadAttendees();
				SetMoreOptionMenuOnSingleTask();
				TryLoadComments(showRecent: true, focus: false, loadLocal: true, loadComment);
				GetParentAndChildren(taskModel);
				SetUpperContent(taskModel);
				_detailHead?.SetRightIcons(_task.IsNewAdd, _task.IsNote);
				_detailHead?.SetIconColor(_task.Priority, _task.Status);
				SetAvatar();
				CloseActivityPanel();
				LoadContentView(taskModel, itemId, restoreOffset);
				if (restoreOffset)
				{
					_contentText?.FixCurrentIndex();
				}
				if ((_textEditorMenu != null && hideEditor) || !_task.Enable)
				{
					SetTextEditorMenu(show: false);
				}
				_navigating = false;
			}
		});
	}

	private void SetNotionBlock(string notionBlock)
	{
		TaskNotionBlockViewModel taskNotionBlockViewModel = null;
		if (!string.IsNullOrEmpty(notionBlock))
		{
			taskNotionBlockViewModel = TaskNotionBlockViewModel.Build(notionBlock);
		}
		if (taskNotionBlockViewModel != null && taskNotionBlockViewModel.pageUrl != null)
		{
			if (_notionView == null)
			{
				_notionView = new TaskNotionPanel(Mode == Constants.DetailMode.Sticky);
				_notionView.SetValue(Grid.RowProperty, 2);
				_scrollContent.Children.Add(_notionView);
			}
			_notionView.Visibility = Visibility.Visible;
			_notionView.SetModel(taskNotionBlockViewModel);
		}
		else if (_notionView != null)
		{
			_notionView.Visibility = Visibility.Collapsed;
			_notionView.SetModel(null);
		}
	}

	private void InitControl()
	{
		_detailBottom?.OnDataBind(_task);
		SetStarGrid();
	}

	public void SetImmerse()
	{
		if (Mode == Constants.DetailMode.Editor)
		{
			if (_detailHead != null)
			{
				_detailHead.Visibility = Visibility.Collapsed;
			}
			if (_commentDisplayCtrl != null)
			{
				_commentDisplayCtrl.Visibility = Visibility.Collapsed;
			}
			if (_addCommentCtrl != null)
			{
				_addCommentCtrl.Visibility = Visibility.Collapsed;
			}
			_detailBottom.Visibility = Visibility.Collapsed;
		}
		else
		{
			if (_detailHead != null)
			{
				_detailHead.Visibility = Visibility.Visible;
			}
			_detailBottom.Visibility = Visibility.Visible;
		}
	}

	public async void TryLoadAttendees()
	{
		if (Utils.IsDida())
		{
			_task.IsOwner = string.IsNullOrEmpty(_task.AttendId) || AgendaHelper.CanAccessAgenda(_task);
			if (_detailBottom != null)
			{
				await _detailBottom.LoadAttendees(_task, Mode);
			}
			if (_contentText != null)
			{
				_contentText.EditBox.IsEnabled = _task.IsOwner;
			}
			if (_descText != null)
			{
				_descText.IsEnabled = _task.IsOwner;
			}
		}
		else
		{
			_task.IsOwner = true;
		}
	}

	private void SetProgress(int progress, bool withAnim = false)
	{
		if (_task.Status == 0)
		{
			_detailHead?.SetProgress((progress > 0) ? progress : 0, withAnim);
		}
		else
		{
			_detailHead?.SetProgress(0);
		}
	}

	private void SetMoreOptionMenuOnSingleTask()
	{
		string tag = null;
		bool visible = true;
		if (_task.Status != 0 || !string.IsNullOrEmpty(_task.AttendId))
		{
			visible = false;
		}
		else if (TaskCache.IsParentTask(_task.TaskId) || !string.IsNullOrEmpty(_task.ParentId))
		{
			tag = Utils.GetString("CannotConvertMultiLevelToNote");
		}
		_detailBottom?.SetSwitchTagAndVisible(tag, visible);
	}

	private async void GetParentAndChildren(TaskDetailViewModel taskModel)
	{
		SetSubtasks(taskModel, restoreSelected: false);
	}

	private async void SetAvatar(string assignee = null, string projectId = null)
	{
		assignee = assignee ?? _task.Assignee;
		projectId = projectId ?? _task.ProjectId;
		if (string.IsNullOrEmpty(projectId))
		{
			_detailHead?.SetAvatarVisible(showAssign: false);
			return;
		}
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.ProjectId == projectId);
		bool showAssign = false;
		if (projectModel != null && projectModel.IsShareList() && (string.IsNullOrEmpty(_task.AttendId) || AgendaHelper.CanAccessAgenda(_task)))
		{
			showAssign = true;
		}
		_detailHead?.SetAvatarVisible(showAssign, assignee, projectId);
	}

	private void CloseActivityPanel()
	{
		ToggleTaskActivityPanel(show: false);
	}

	private async void LoadContentView(TaskDetailViewModel taskModel, string itemId = "", bool restoreOffset = false)
	{
		TryShowOptionMenu();
		if (_task.Kind == "CHECKLIST")
		{
			LoadChecklistView(taskModel, itemId);
			_descText?.RegisterCaretChanged();
		}
		else if (_contentText != null)
		{
			_contentText.CurrentFocused = restoreOffset;
			_contentText.RegisterCaretChanged();
		}
	}

	private void TryShowOptionMenu()
	{
		_detailBottom?.SetItemVisible("EditorIcon", _task.IsOwner && _task.Enable && !_task.IsNewAdd);
	}

	private async void LoadChecklistView(TaskDetailViewModel model, string itemId)
	{
		if (model.Kind != "CHECKLIST")
		{
			return;
		}
		List<CheckItemViewModel> checklistItems = new List<CheckItemViewModel>();
		List<TaskDetailItemModel> list;
		if (model.Items != null && model.Items.Any())
		{
			list = model.Items.ToList();
		}
		else
		{
			list = await TaskDetailItemDao.GetCheckItemsByTaskId(model.TaskId);
			if (list == null || list.Count == 0)
			{
				await Task.Delay(1000);
				if (model.Kind != "CHECKLIST")
				{
					return;
				}
				await TaskService.GetRemoteTaskCheckItems(model.ProjectId, model.TaskId);
				list = await TaskDetailItemDao.GetCheckItemsByTaskId(model.TaskId);
			}
			model.Items = list?.ToArray();
		}
		if (list != null && list.Any())
		{
			List<IGrouping<string, TaskDetailItemModel>> list2 = (from i in list
				group i by i.id).ToList();
			bool flag = false;
			foreach (IGrouping<string, TaskDetailItemModel> item in list2)
			{
				if (item.Count() > 1)
				{
					flag = true;
					TaskDetailItemDao.ClearSameItemById(item.Key);
				}
			}
			if (flag)
			{
				list = list2.Select((IGrouping<string, TaskDetailItemModel> g) => g.FirstOrDefault()).ToList();
			}
			_task.SourceViewModel.CheckCheckItems(list);
			foreach (TaskBaseViewModel item2 in _task.SourceViewModel.CheckItems.ToList())
			{
				if (item2.Deleted == 0)
				{
					checklistItems.Add(new CheckItemViewModel(item2, model)
					{
						IsAgendaOwner = _task.IsOwner,
						Enable = (_task.IsOwner && _task.Enable),
						IsValid = true
					});
				}
			}
		}
		SetCheckItems(checklistItems);
		if (!string.IsNullOrEmpty(itemId))
		{
			await Task.Delay(100);
			_checklist?.ScrollToItem(itemId);
		}
	}

	private void SetCheckItems(List<CheckItemViewModel> source)
	{
		if (source != null && source.Count == 1)
		{
			source[0].ShowAddHint = true;
		}
		_checklist?.SetTaskId(_task.TaskId);
		_checklist?.SetData(source);
	}

	private void RemoveVmChangeEvent(TaskDetailViewModel taskModel)
	{
		if (taskModel != null)
		{
			PropertyChangedEventManager.RemoveHandler(taskModel, OnTaskPropertyChanged, "");
		}
	}

	private void SetVmChangeEvent(TaskDetailViewModel taskModel)
	{
		if (taskModel != null)
		{
			PropertyChangedEventManager.AddHandler(taskModel, OnTaskPropertyChanged, "");
		}
	}

	public void ScrollToTop()
	{
		_detailScrollViewer.ScrollToTop();
	}

	internal void SctollToBottom()
	{
		_detailScrollViewer.ScrollToBottom();
	}

	public async void SelectDateClick(MouseButtonEventArgs e)
	{
		if ((!DatePopupShow || (SetDateDialog.GetDialog()?.IsOpen() ?? false)) && !e.Handled && CheckEnable())
		{
			if (_task.IsOwner)
			{
				ShowSelectDateDialog();
				e.Handled = true;
			}
			else
			{
				TryToast(Utils.GetString("AttendeeSetDate"));
			}
		}
	}

	private async Task ClearTaskDate(TaskDetailViewModel task)
	{
		if (TaskOperationHelper.CheckIfAgendaAllowClearDate(task.AttendId, this))
		{
			await DoClearTaskDate(task);
			TryToastQuadrantChanged(task.TaskId);
		}
	}

	private async Task DoClearTaskDate(TaskDetailViewModel task)
	{
		task.Reminders = null;
		task.ExDates = null;
		await UpdateTaskWithUndo(SetTime());
		TaskChangeNotifier.NotifyTaskDateChanged(task.TaskId, this);
		if (_task == task && _titleText.TextArea.IsKeyboardFocused)
		{
			OnTitleGotFocus(null, null);
		}
		async Task SetTime()
		{
			await Task.Yield();
			if (!string.IsNullOrEmpty(task.AttendId))
			{
				await TaskService.ClearAgendaDate(task.TaskId);
			}
			else
			{
				await TaskService.ClearDate(task.TaskId);
			}
		}
	}

	private async Task SaveTaskDate(TaskDetailViewModel task, TimeData model)
	{
		CheckIfReminderPassed(model);
		task.Reminders = model.Reminders?.ToArray() ?? new TaskReminderModel[0];
		task.AnnoyingAlert = model.AnnoyingAlert;
		task.ExDates = new List<string>();
		if (!task.IsNewAdd)
		{
			await UpdateTaskWithUndo(SetTime());
			TaskChangeNotifier.NotifyTaskDateChanged(task.TaskId, this);
			TryToastQuadrantChanged(task.TaskId);
			return;
		}
		task.SetTimeData(model);
		if (!model.IsDefault)
		{
			DisableTitleParseForManualTime();
		}
		async Task SetTime()
		{
			await Task.Yield();
			await TaskService.SetDate(task.TaskId, model);
		}
	}

	private void CheckIfReminderPassed(TimeData model)
	{
		if (model != null && model.IsAllDay.HasValue && !model.IsAllDay.Value && model.StartDate.HasValue)
		{
			List<TaskReminderModel> reminders = model.Reminders;
			if (reminders != null && reminders.Count > 0 && (!(model.StartDate.Value >= DateTime.Now) || model.Reminders.Count((TaskReminderModel r) => !r.trigger.Contains("END")) <= 0) && (!model.DueDate.HasValue || !(model.DueDate.Value >= DateTime.Now) || model.Reminders.Count((TaskReminderModel r) => r.trigger.Contains("END")) <= 0))
			{
				TryToast(Utils.GetString("InvalidReminder"));
			}
		}
	}

	public async void PrioritySelect(int priority)
	{
		IsBlank = false;
		AddActionEvent("action", "priority");
		SetTaskPriority(priority);
	}

	private async void SetTaskPriority(int priority)
	{
		if (_task.SourceViewModel.Priority == priority)
		{
			return;
		}
		if (_task.IsNewAdd)
		{
			_task.SourceViewModel.Priority = priority;
			return;
		}
		await UpdateTaskWithUndo(SetPri());
		TryToastQuadrantChanged(_task.TaskId);
		if (Mode == Constants.DetailMode.Sticky)
		{
			string getString = Utils.GetString("SetAsNonePriority");
			switch (priority)
			{
			case 5:
				getString = Utils.GetString("SetAsHighPriority");
				break;
			case 3:
				getString = Utils.GetString("SetAsMediumPriority");
				break;
			case 1:
				getString = Utils.GetString("SetAsLowPriority");
				break;
			}
			TryToast(getString);
		}
		async Task SetPri()
		{
			await Task.Yield();
			await TaskService.SetPriority(_task.TaskId, priority);
		}
	}

	private Task SaveTask(CheckMatchedType checkType = CheckMatchedType.None)
	{
		return SaveTask(_task, checkType);
	}

	private async Task SaveTask(TaskDetailViewModel task, CheckMatchedType checkType = CheckMatchedType.None)
	{
		if (task != null && !task.IsNewAdd && !string.IsNullOrEmpty(task.TaskId))
		{
			TaskModel taskModel = await TaskDetailViewModel.ToTaskModel(task);
			if (taskModel != null)
			{
				taskModel.modifiedTime = DateTime.Now;
				await TaskService.UpdateTaskOnDetailChanged(taskModel, checkType);
				await SyncStatusDao.AddModifySyncStatus(task.TaskId);
				await PomoService.UpdatePomoTask(taskModel);
			}
		}
	}

	public bool IsNoPermission(bool isComment = false)
	{
		string permission = _task.Permission;
		if (!(permission == "comment"))
		{
			if (permission == "read")
			{
				TryToast(string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("ReadOnly")));
				return true;
			}
		}
		else if (!isComment)
		{
			TryToast(string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("CanComment")));
			return true;
		}
		return false;
	}

	private void OnDetailKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.V && Utils.IfCtrlPressed() && (_addCommentCtrl == null || !_addCommentCtrl.CommentInputTxt.Focused))
		{
			try
			{
				if (CheckPasteEnable())
				{
					UtilLog.Info($"StartPastFileCaret {_contentText?.CurrentFocused},{_contentText?.KeyboardFocused},{_contentText?.EditBox.CaretOffset},{_contentText?.EditBox.Text.Length}");
					BitmapSource pastImage = GetPastImage();
					if (pastImage != null)
					{
						e.Handled = true;
						TryPasteImage(pastImage);
						return;
					}
					List<string> pastFiles = FileUtils.GetPastFiles();
					if (pastFiles != null && pastFiles.Any())
					{
						e.Handled = true;
						TryPasteFile(pastFiles);
						return;
					}
				}
			}
			catch (Exception)
			{
				TryToast(Utils.GetString("FileNotFound"));
			}
		}
		if (e.Key == Key.D0 && Utils.IfCtrlPressed() && Utils.IfShiftPressed() && Mode == Constants.DetailMode.Page)
		{
			EnterImmersivePage();
		}
	}

	public async Task OnCopy(bool inBatch = false)
	{
		AddActionEvent("om", "duplicate");
		if (await ProChecker.CheckTaskLimit(_task.ProjectId))
		{
			return;
		}
		SetPopupStatus(show: true);
		TaskModel taskModel = await TaskService.CopyTask(_task.TaskId, this);
		SetPopupStatus(show: false);
		if (taskModel != null)
		{
			this.TaskCopied?.Invoke(this, taskModel.id);
			taskModel.projectId = _task.ProjectId;
			taskModel.Color = _task.Color;
			if (Mode == Constants.DetailMode.Page && !KeepDetailOnCopy)
			{
				await Navigate(taskModel.id);
			}
		}
	}

	private async void OnSkipRecurrence(TaskDetailViewModel task, object sender, EventArgs e)
	{
		if (_task == task)
		{
			if (!task.DisplayStartDate.HasValue || !(task.DisplayStartDate != task.StartDate))
			{
				await UpdateTaskWithUndo(SetTime());
				return;
			}
			await TaskService.SkipRecurrenceByDate(task.TaskId, task.DisplayStartDate.Value.Date, (Window.GetWindow(this) as TaskDetailWindow)?.DependentWindow);
			TaskDetailPopup.TryCloseWindow();
		}
		async Task SetTime()
		{
			await Task.Yield();
			await TaskService.SkipCurrentRecurrence(task.TaskId, notify: true, Window.GetWindow(this) as TaskDetailWindow);
		}
	}

	private async Task OnDateSelected(DateTime time)
	{
		if (_task != null)
		{
			if (_task.IsNewAdd)
			{
				TimeData timeData = _task.GetTimeData();
				timeData.SetDate(time);
				timeData.IsDefault = false;
				_task.SetTimeData(timeData);
				DisableTitleParseForManualTime();
			}
			else if (!string.IsNullOrEmpty(_task.AttendId) && !AgendaHelper.CanAccessAgenda(_task))
			{
				TryToast(Utils.GetString("AttendeeSetDate"));
			}
			else if (_task.RepeatDiff.HasValue)
			{
				TimeData timeData2 = new TimeData
				{
					StartDate = _task.DisplayStartDate,
					DueDate = _task.DisplayDueDate,
					IsAllDay = (!_task.DisplayStartDate.HasValue || (_task.IsAllDay ?? true)),
					Reminders = _task.Reminders?.ToList(),
					RepeatFrom = _task.RepeatFrom,
					RepeatFlag = _task.RepeatFlag,
					ExDates = _task.ExDates
				};
				ModifyRepeatHandler.TryUpdateDueDateOnlyDate(_task.TaskId, _task.DisplayStartDate, _task.DisplayDueDate, timeData2, time, 1, 1);
			}
			else
			{
				await UpdateTaskWithUndo(SetTime());
				TaskChangeNotifier.NotifyTaskDateChanged(_task.TaskId, this);
				TryToastQuadrantChanged(_task.TaskId);
			}
		}
		async Task SetTime()
		{
			await Task.Yield();
			await TaskService.SetStartDate(_task.TaskId, time);
		}
	}

	public async void OnTagsSelected(TagSelectData data, bool inBatch)
	{
		UtilLog.Info($"TaskDetail.SetTag {_task?.Id} from:TagWindow");
		NotifyTagsChanged(data.OmniSelectTags);
	}

	private void OnTagAdded(object sender, string tag)
	{
		List<string> list = TagSerializer.ToTags(_task.Tag);
		if (!list.Contains(tag))
		{
			list.Add(tag);
			NotifyTagsChanged(list);
		}
	}

	private void OnTagsChanged(object sender, List<string> tags)
	{
		OnTagPanelVisibleChanged(this, Visibility.Collapsed);
		NotifyTagsChanged(tags);
	}

	private async void NotifyTagsChanged(List<string> tags)
	{
		IsBlank = false;
		if (!_task.IsNewAdd)
		{
			await UpdateTaskWithUndo(SetTag());
			SyncManager.TryDelaySync();
			TryToastQuadrantChanged(_task.TaskId);
		}
		else
		{
			_task.SourceViewModel.SetTags(tags);
		}
		async Task SetTag()
		{
			await Task.Yield();
			await TaskService.SetTags(_task.TaskId, tags);
		}
	}

	private async Task ToggleTaskActivityPanel(bool show)
	{
		if (show)
		{
			bool isNote = _task.Kind == "NOTE";
			if (!ProChecker.CheckPro(isNote ? ProType.NoteActivities : ProType.TaskActivities))
			{
				return;
			}
			if (!Utils.IsNetworkAvailable())
			{
				TryToast(Utils.GetString("NoNetwork"));
				return;
			}
			TaskActivityPanel activityPanel = _taskActivityPanel;
			if (activityPanel == null)
			{
				activityPanel = (_taskActivityPanel = new TaskActivityPanel
				{
					Margin = new Thickness(20.0, 0.0, 20.0, 0.0)
				});
				if (Mode == Constants.DetailMode.Popup)
				{
					activityPanel.ActivityControl.Items.MaxHeight = 200.0;
				}
				activityPanel.Closed += OnCloseActivityClick;
				activityPanel.SetValue(Grid.RowProperty, 2);
				base.Children.Add(activityPanel);
			}
			UserInfoModel userInfoModel = await UserManager.GetUserInfo();
			string userName = LocalSettings.Settings.LoginUserName;
			if (userInfoModel != null)
			{
				userName = (string.IsNullOrEmpty(userInfoModel.name) ? userInfoModel.username : userInfoModel.name);
			}
			bool isAgendaCopy = false;
			TaskModel task = await TaskDao.GetThinTaskById(_task.TaskId);
			if (task != null)
			{
				isAgendaCopy = !string.IsNullOrEmpty(task.attendId) && task.attendId != task.id;
			}
			string value = await Communicator.GetTaskActivities(_task.ProjectId, _task.TaskId);
			int maxDescLength = 3 * (int)base.ActualWidth / 4;
			try
			{
				List<TaskModifyModel> list = JsonConvert.DeserializeObject<List<TaskModifyModel>>(value);
				if (list != null)
				{
					activityPanel.ActivityCountText.Text = list.Count.ToString();
					List<TaskActivityViewModel> list2 = new List<TaskActivityViewModel>();
					DateTime dateTime = default(DateTime);
					for (int num = list.Count - 1; num >= 0; num--)
					{
						if (list[num].action == "C_CONTENT")
						{
							DateTime.TryParse(list[num].when, out var result);
							if (Utils.IsEmptyDate(result) || (!Utils.IsEmptyDate(dateTime) && !((dateTime - result).TotalMinutes >= 5.0)))
							{
								continue;
							}
							dateTime = result;
						}
						if (!list[num].action.StartsWith("AT") && !IsInvalidAgendaAction(list[num].action, isAgendaCopy))
						{
							list2.Add(new TaskActivityViewModel(list[num], userName, isAgendaCopy, task.Floating, task.kind)
							{
								MaxDescLength = maxDescLength
							});
						}
					}
					activityPanel.ActivityControl.SetItems(list2);
				}
			}
			catch (Exception)
			{
			}
			activityPanel.ActivitiesName.Text = Utils.GetString(isNote ? "NoteActivities" : "TaskActivities");
		}
		else if (_taskActivityPanel != null)
		{
			base.Children.Remove(_taskActivityPanel);
			_taskActivityPanel.Closed -= OnCloseActivityClick;
			_taskActivityPanel = null;
		}
	}

	private bool IsInvalidAgendaAction(string action, bool isAgendaCopy)
	{
		if (isAgendaCopy)
		{
			if (!(action == "T_DONE") && !(action == "T_UNDONE"))
			{
				return action == "T_REPEAT";
			}
			return true;
		}
		return false;
	}

	private void OnCloseActivityClick(object sender, EventArgs e)
	{
		ToggleTaskActivityPanel(show: false);
	}

	private void OnDetailSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_detailBottom?.AttendeePanel.NotifyWidthChanged(base.ActualWidth);
		if (_task != null && _task.Kind == "CHECKLIST")
		{
			LoadAttachmentAsync();
		}
	}

	public void OnPomoCompleted(object sender, List<string> taskIds)
	{
		if (_task?.TaskId != null && base.Visibility == Visibility.Visible && taskIds != null && taskIds.Contains(_task.TaskId))
		{
			Reload();
		}
	}

	public void OnTasksDeleted(List<string> taskIds)
	{
		if (_subTaskList != null && _subTaskList.DisplayModels.Any((DisplayItemModel m) => taskIds.Contains(m.Id)))
		{
			SetSubtasks(_task);
		}
	}

	public bool TryClosePopup()
	{
		if (_detailBottom != null && _detailBottom.MorePopup.IsOpen)
		{
			_detailBottom.MorePopup.IsOpen = false;
			return true;
		}
		return false;
	}

	public void SetNavigate()
	{
		_detailBottom?.SetNavigate();
	}

	public void SetEditMenuMode(bool inImmerse)
	{
		if (inImmerse)
		{
			if (_textEditorMenu == null)
			{
				SetTextEditorMenu(show: true);
			}
			_textEditorMenu.Opacity = 0.24;
			_scrollButtonRow.Height = new GridLength(24.0);
			_textEditorMenu.Margin = new Thickness(0.0, 0.0, 0.0, 15.0);
			_textEditorMenu.MouseEnter -= OnMenuMouseEnter;
			_textEditorMenu.MouseEnter += OnMenuMouseEnter;
			_textEditorMenu.MouseLeave -= OnMenuMouseLeave;
			_textEditorMenu.MouseLeave += OnMenuMouseLeave;
			_textEditorMenu.IsImmersiveMode = true;
		}
		else
		{
			if (_textEditorMenu != null)
			{
				_textEditorMenu.Margin = new Thickness(0.0);
				_textEditorMenu.MouseEnter -= OnMenuMouseEnter;
				_textEditorMenu.MouseLeave -= OnMenuMouseLeave;
				_textEditorMenu.BeginAnimation(UIElement.OpacityProperty, null);
				_textEditorMenu.Opacity = 1.0;
				_textEditorMenu.IsImmersiveMode = false;
			}
			_scrollButtonRow.Height = new GridLength(0.0);
		}
	}

	private void OnMenuMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_textEditorMenu != null)
		{
			Storyboard storyboard = new Storyboard();
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
				AutoReverse = false,
				FillBehavior = FillBehavior.HoldEnd,
				From = 0.23999999463558197,
				To = 1.0,
				Duration = TimeSpan.FromMilliseconds(120.0)
			};
			storyboard.Children.Add(doubleAnimation);
			Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
			storyboard.Begin(_textEditorMenu);
		}
	}

	private void OnMenuMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_textEditorMenu != null)
		{
			Storyboard storyboard = new Storyboard();
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
				AutoReverse = false,
				BeginTime = TimeSpan.FromMilliseconds(500.0),
				FillBehavior = FillBehavior.HoldEnd,
				From = 1.0,
				To = 0.23999999463558197,
				Duration = TimeSpan.FromMilliseconds(120.0)
			};
			storyboard.Children.Add(doubleAnimation);
			Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
			storyboard.Begin(_textEditorMenu);
		}
	}

	private async Task AbandonedOrReopenTask(bool? isAbandone = null)
	{
		SetPopupClosing();
		if (!CheckEnable() || _task == null)
		{
			return;
		}
		isAbandone = isAbandone ?? (!_task.IsAbandoned);
		int status = (isAbandone.Value ? (-1) : 0);
		if (_task.Status == 0 && _task.StartDate.HasValue && _task.DisplayStartDate.HasValue && _task.StartDate.Value.Date != _task.DisplayStartDate.Value.Date)
		{
			IToastShowWindow toastWindow = Utils.FindParent<TaskDetailPopup>(this)?.DependentWindow;
			if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(_task.TaskId, _task.DisplayStartDate, toastWindow)))
			{
				return;
			}
		}
		TaskCloseExtra obj = await TaskService.SetTaskStatus(_task.TaskId, status, needUndo: true, null, inDetail: true);
		TryToast((status != -1) ? Utils.GetString("TaskReopenedTips") : Utils.GetString("TaskAbandonedTips"));
		if (obj?.RepeatTask != null)
		{
			TryToast(Utils.GetString("CreatedTheNextCircle"));
		}
	}

	public void SetStickyMode()
	{
		ThemeUtil.SetTheme("light", this);
	}

	public void TryTabInputBox()
	{
		if (_titleText.KeyboardFocused)
		{
			if (Utils.IfShiftPressed())
			{
				this.FocusList?.Invoke(this, null);
			}
			else if (_contentText != null)
			{
				_contentText.FocusEditBox();
			}
			else if (_checklist != null)
			{
				_checklist.FocusFirstItem();
			}
		}
	}

	public void SetTitleAndContent(TaskBaseViewModel model)
	{
		base.DataContext = new TaskDetailViewModel(model);
		_titleText.SetTextOffset(model.Title, restoreOffset: false);
		SetTitleHint(model.Title);
		_contentText?.SetImageGeneratorTaskId(model.Id);
		_contentText?.SetTextAndOffset(model.Content, restoreOffset: false);
		_descText?.SetTextAndOffset(model.Desc, restoreOffset: false);
	}

	public void TryParseUrl()
	{
		if (_titleText.CurrentFocused)
		{
			OnTitleLostFocus(null, null);
		}
	}

	public void SetTheme(bool isDark)
	{
		if (!_isDark.HasValue || _isDark.Value != isDark)
		{
			_isDark = isDark;
			ThemeUtil.SetTheme(isDark ? "dark" : "light", this);
			SetSubtasks(_task);
			_contentText?.SetTheme(!isDark);
			_titleText.SetLightTheme();
			_descText?.SetTheme(!isDark);
			_checklist?.SetStickyTheme(isDark);
		}
	}

	public void OnShowDialog()
	{
		SetPopupShow();
	}

	public void OnCloseDialog()
	{
		SetPopupClosing();
	}

	public async void ShowAttendeeDialog()
	{
		if (CheckEnable())
		{
			TaskAttendModel taskAttendModel = await AgendaHelper.GetAgendaAttendModel(_task.AttendId, _task.TaskId);
			if (taskAttendModel?.organizer != null)
			{
				OnShowDialog();
				AttendeeListDialog attendeeListDialog = new AttendeeListDialog(_task.ProjectId, _task.AttendId, _task.TaskId, taskAttendModel);
				attendeeListDialog.Owner = Window.GetWindow(this);
				attendeeListDialog.ShowDialog();
				OnCloseDialog();
				TryLoadAttendees();
			}
		}
	}

	public void OnEditorClick()
	{
		if (_task == null || _task.Enable)
		{
			RetainCurrentEditorSelectionForToolbarInteraction();
			SetTextEditorMenu(_textEditorMenu == null);
		}
	}

	public void OnCommentClick()
	{
		if (!IsNoPermission(isComment: true))
		{
			AddActionEvent("action", "comment");
			SetTextEditorMenu(show: false);
			if (_addCommentCtrl == null)
			{
				TryInitAddCommentCtrl((_addCommentCtrl?.EditModel != null && _addCommentCtrl.EditModel.TaskId == _task.TaskId) ? _addCommentCtrl.EditModel : new CommentEditViewModel
				{
					CommentId = Utils.GetGuid(),
					TaskId = _task.TaskId,
					ProjectId = _task.ProjectId
				}, withFocus: true);
			}
			else
			{
				HideAddComment();
			}
		}
	}

	public async Task OnAbandonOrReopenClick()
	{
		AddActionEvent("om", "wont_do_reopen");
		UtilLog.Info($"TaskDetail.AbandonedOrReopenTask {_task?.TaskId}, CurrentStatus{_task?.Status} from:Om");
		await AbandonedOrReopenTask();
	}

	public void OnAddAttachmentClick()
	{
		AddActionEvent("om", "attachment");
		ShowUploadFileDialog();
	}

	public async void TryInsertSummary()
	{
		MarkDownEditor content = _contentText;
		if (_contentText != null)
		{
			_needFocusDetail = false;
			SetPopupShow();
			UtilLog.Info("DetailInsertSummary");
			AddActionEvent("om", "insert_summary");
			InsertSummaryWindow insertSummaryWindow = new InsertSummaryWindow();
			insertSummaryWindow.Owner = Window.GetWindow(this);
			insertSummaryWindow.InsertSummary += delegate(object o, string e)
			{
				int num = Math.Min(content.EditBox.CaretOffset, content.Text.Length);
				content.InsertPersistedEditorText(((num > 0) ? "\r\n" : "") + e);
			};
			insertSummaryWindow.ShowDialog();
			await Task.Delay(200);
			TryFocusDetail();
			Window.GetWindow(this)?.Activate();
			SetPopupClosing();
		}
	}

	public void ShowTaskActivities()
	{
		AddActionEvent("om", "task_activities");
		ToggleTaskActivityPanel(show: true);
	}

	public async void OnAddTemplateClick()
	{
		if (_task != null)
		{
			SetPopupShow();
			UtilLog.Info("DetailSaveTemplate");
			await TemplateDao.SaveTemplate(_task.TaskId, this);
			AddActionEvent("om", "save_as_template");
			SetPopupClosing();
			TryFocusDetail();
		}
	}

	public void OnTaskCopy()
	{
		_titleText.FocusText();
		OnCopy();
	}

	public void OnCopyLinkClick()
	{
		AddActionEvent("om", "copy_link");
		TaskUtils.CopyTaskLink(_task.GetTaskId(), _task.ProjectId, _task.Title);
	}

	public async Task SwitchTaskNoteClick()
	{
		IToastShowWindow calendarToastWindow = GetCalendarTaskDetailToastWindow();
		if (_task?.IsNewAdd ?? false)
		{
			SwitchNewTaskNoteKind();
			calendarToastWindow?.TryToastString(null, Utils.GetString("Converted"));
		}
		else if (await TaskService.SwitchTaskOrNote(_task.TaskId, calendarToastWindow == null))
		{
			calendarToastWindow?.TryToastString(null, Utils.GetString("Converted"));
			AddActionEvent("om", (_task.Kind == "TEXT") ? "convert_to_note" : "convert_to_task");
			SyncManager.TryDelaySync();
		}
	}

	private IToastShowWindow GetCalendarTaskDetailToastWindow()
	{
		if (!(Window.GetWindow(this) is TaskDetailPopup { IsCalendarContext: not false } taskDetailPopup))
		{
			return null;
		}
		return taskDetailPopup.DependentWindow ?? (taskDetailPopup.Target as IToastShowWindow) ?? Utils.FindParent<IToastShowWindow>(taskDetailPopup.Target);
	}

	private void SwitchNewTaskNoteKind()
	{
		if (_task == null)
		{
			return;
		}
		SyncNewAddEditorText();
		bool flag = _task.Kind == "NOTE";
		if (flag)
		{
			_task.CacheTaskFieldsBeforeKindConvert(keepExisting: true);
			_task.SourceViewModel.Kind = "TEXT";
			_task.RestoreTaskFieldsAfterKindConvert();
		}
		else
		{
			_task.CacheTaskFieldsBeforeKindConvert();
			if (_task.Kind == "CHECKLIST")
			{
				_task.SourceViewModel.Content = BuildChecklistTextContent();
				_task.SourceViewModel.Desc = string.Empty;
				_task.Items = null;
			}
			_task.SourceViewModel.DueDate = null;
			_task.SourceViewModel.Kind = "NOTE";
			_task.SourceViewModel.Status = 0;
			_task.SourceViewModel.CompletedTime = null;
			_task.SourceViewModel.Priority = 0;
			_task.SourceViewModel.Progress = 0;
			_task.ClearRepeatForNote();
		}
		_task.SetPropertyChanged("Kind");
		_detailHead?.SetRightIcons(_task.IsNewAdd, _task.IsNote);
		_titleText?.SetMarkRegexText(unable: false, inDetail: true, _task.IsNote);
		SetTitleHint(_task.Title);
		SetKindSwitcher(Mode != Constants.DetailMode.Sticky && Mode != Constants.DetailMode.Editor && !_task.IsNote);
		UpdateStatusIcon();
		LoadContentControl(restoreOffset: false, sameTask: false);
		TryShowOptionMenu();
		AddActionEvent("om", flag ? "convert_to_task" : "convert_to_note");
	}

	public bool CanConvertNewTaskToEvent()
	{
		TaskDetailViewModel task = _task;
		if (task != null && task.IsNewAdd && UserDao.IsPro())
		{
			return GetDefaultCalendarForNewEvent() != null;
		}
		return false;
	}

	public void ConvertNewTaskToEventClick()
	{
		if (!CanConvertNewTaskToEvent())
		{
			return;
		}
		ApplyNewAddParsedDateForEvent();
		SyncNewAddEditorText();
		BindCalendarModel bindCalendarModel = _task.KindConvertCalendar ?? GetDefaultCalendarForNewEvent();
		if (bindCalendarModel != null)
		{
			_task.CacheTaskFieldsBeforeKindConvert(_task.IsNote);
			TaskDetailPopup taskDetailPopup = Window.GetWindow(this) as TaskDetailPopup;
			CalendarDetailViewModel calendarDetailViewModel = BuildCalendarDetailModelFromNewTask(bindCalendarModel);
			calendarDetailViewModel.UseTaskPopupAlignment = taskDetailPopup != null;
			Point calendarDetailPointFromTaskPopup = GetCalendarDetailPointFromTaskPopup(taskDetailPopup);
			if (taskDetailPopup != null && taskDetailPopup.IsCalendarContext)
			{
				taskDetailPopup.BeginCalendarAddFlow();
			}
			_task.IsNewAdd = false;
			TryForceHideWindow();
			OpenCalendarDetailWindowFromNewTask(calendarDetailViewModel, taskDetailPopup, calendarDetailPointFromTaskPopup);
			AddActionEvent("om", "convert_to_calendar");
		}
	}

	private void SyncNewAddEditorText()
	{
		if (_task != null)
		{
			if (_titleText != null && _task.SourceViewModel.Title != _titleText.Text)
			{
				_task.SourceViewModel.Title = _titleText.Text;
			}
			string text = _contentText?.GetPersistedText();
			if (_contentText != null && _task.SourceViewModel.Content != text)
			{
				_task.SourceViewModel.Content = text;
			}
			string text2 = _descText?.GetPersistedText();
			if (_descText != null && _task.SourceViewModel.Desc != text2)
			{
				_task.SourceViewModel.Desc = text2;
			}
		}
	}

	private void ApplyNewAddParsedDateForEvent()
	{
		if (_task != null && _titleText != null && _titleText.ParsingDate)
		{
			TimeData timeData = _titleText.ParsedData?.ToTimeData(addDefaultReminder: true);
			string parsedText = _titleText.GetParsedText();
			_titleText.SetCanParseDate(canParseDate: false);
			_task.SourceViewModel.Title = parsedText;
			if (timeData != null)
			{
				_task.SetTimeData(timeData);
			}
			_task.ParseData = null;
			TaskListItemFocusHelper.ParseDateId = string.Empty;
			TaskListItemFocusHelper.ParseDateItem = null;
		}
	}

	private BindCalendarModel GetDefaultCalendarForNewEvent()
	{
		Dictionary<string, BindCalendarAccountModel> accounts = (from account in CacheManager.GetBindCalendarAccounts()
			where account != null && !account.Expired && !string.IsNullOrEmpty(account.Id)
			select account).ToDictionary((BindCalendarAccountModel account) => account.Id, (BindCalendarAccountModel account) => account);
		return (from calendar in CacheManager.GetBindCalendars()
			where calendar != null && !string.IsNullOrEmpty(calendar.AccountId) && accounts.ContainsKey(calendar.AccountId) && calendar.Accessible
			orderby calendar.Show == "hidden", calendar.sortOrder
			select calendar).FirstOrDefault();
	}

	private CalendarDetailViewModel BuildCalendarDetailModelFromNewTask(BindCalendarModel calendar)
	{
		TimeData eventTimeDataFromNewTask = GetEventTimeDataFromNewTask();
		ClearCalendarUnsupportedRepeat(eventTimeDataFromNewTask);
		string text = BuildNewTaskEventSourceContent();
		string text2 = TaskUtils.RemoveAttachmentMarkdown(text);
		_task.CacheAttachmentContentForCalendar(text, text2);
		CalendarDetailViewModel calendarDetailViewModel = new CalendarDetailViewModel(calendar)
		{
			Title = (_task.Title?.Trim() ?? string.Empty),
			Content = text2,
			Location = (_task.KindConvertLocation ?? string.Empty),
			ShowQuickAddConvertOptions = true,
			SourceTaskViewModel = _task.SourceViewModel
		};
		calendarDetailViewModel.IsAllDay = eventTimeDataFromNewTask.IsAllDay == true;
		if (calendarDetailViewModel.IsAllDay)
		{
			calendarDetailViewModel.StartDate = eventTimeDataFromNewTask.StartDate.Value.Date;
			calendarDetailViewModel.DueDate = (eventTimeDataFromNewTask.DueDate ?? eventTimeDataFromNewTask.StartDate.Value.Date).Date;
			if (calendarDetailViewModel.StartDate == calendarDetailViewModel.DueDate)
			{
				calendarDetailViewModel.DueDate = calendarDetailViewModel.DueDate.Value.AddDays(1.0);
			}
		}
		else
		{
			int num = TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60;
			calendarDetailViewModel.StartDate = eventTimeDataFromNewTask.StartDate.Value;
			calendarDetailViewModel.DueDate = eventTimeDataFromNewTask.DueDate ?? eventTimeDataFromNewTask.StartDate.Value.AddMinutes(num);
		}
		calendarDetailViewModel.RepeatFlag = eventTimeDataFromNewTask.RepeatFlag;
		calendarDetailViewModel.RepeatText = RRuleUtils.RRule2String(string.Empty, calendarDetailViewModel.RepeatFlag, calendarDetailViewModel.StartDate);
		calendarDetailViewModel.TimeZone = eventTimeDataFromNewTask.TimeZone?.TimeZoneName ?? Utils.GetLocalTimeZone();
		calendarDetailViewModel.Reminders = GetEventReminderMinutes(eventTimeDataFromNewTask, calendarDetailViewModel.IsAllDay, calendar.AccountId);
		return calendarDetailViewModel;
	}

	private TimeData GetEventTimeDataFromNewTask()
	{
		TimeData timeData = _task.MergeCalendarTimeForEvent(_task.GetTimeData());
		if (timeData == null || !timeData.StartDate.HasValue)
		{
			timeData = TimeData.InitDefaultDuration();
			_task.RestoreRepeatForCalendar(timeData);
			return timeData;
		}
		if (!timeData.IsAllDay.HasValue)
		{
			timeData.IsAllDay = false;
		}
		if (timeData.IsAllDay == false && !timeData.DueDate.HasValue)
		{
			int num = TaskDefaultDao.GetDefaultSafely()?.Duration ?? 60;
			timeData.DueDate = timeData.StartDate.Value.AddMinutes(num);
		}
		_task.RestoreRepeatForCalendar(timeData);
		return timeData;
	}

	private static void ClearCalendarUnsupportedRepeat(TimeData timeData)
	{
		if (timeData != null && !string.IsNullOrWhiteSpace(timeData.RepeatFlag) && !RepeatUtils.IsStandardRRule(timeData.RepeatFlag))
		{
			timeData.RepeatFlag = null;
			timeData.RepeatFrom = "2";
			timeData.ExDates = new List<string>();
		}
	}

	private string BuildNewTaskEventSourceContent()
	{
		if (_task.Kind == "CHECKLIST")
		{
			return BuildChecklistTextContent();
		}
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(_task.TaskContent))
		{
			list.Add(_task.TaskContent);
		}
		if (!string.IsNullOrWhiteSpace(_task.Desc) && _task.Desc != _task.TaskContent)
		{
			list.Add(_task.Desc);
		}
		return string.Join(Environment.NewLine, list);
	}

	private string BuildChecklistTextContent()
	{
		List<string> checklist = (from item in GetChecklistItems()?.OrderBy((CheckItemViewModel item) => item.SortOrder)
			select item.Title into title
			where !string.IsNullOrWhiteSpace(title)
			select title).ToList();
		return ChecklistUtils.Items2Text(_task.Desc, checklist);
	}

	private List<int> GetEventReminderMinutes(TimeData timeData, bool isAllDay, string accountId)
	{
		if (CacheManager.GetAccountCalById(accountId)?.Kind == "icloud")
		{
			return new List<int>();
		}
		List<int> list = (from trigger in timeData.Reminders?.Select((TaskReminderModel reminder) => reminder.trigger)
			where !string.IsNullOrEmpty(trigger)
			select trigger).Select(TriggerUtils.TriggerToReminder).ToList() ?? new List<int>();
		if (list.Any())
		{
			return list;
		}
		if (timeData.IsDefault)
		{
			TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
			string text = ((!isAllDay) ? defaultSafely?.TimeReminders : defaultSafely?.AllDayReminders);
			if (!string.IsNullOrEmpty(text))
			{
				return text.Split(',').Select(TriggerUtils.TriggerToReminder).ToList();
			}
		}
		return new List<int>();
	}

	private void OpenCalendarDetailWindowFromNewTask(CalendarDetailViewModel detailModel, TaskDetailPopup popup, Point calendarPopupPoint = default(Point))
	{
		Window window = popup?.Owner ?? App.Window ?? System.Windows.Application.Current?.MainWindow;
		UIElement target = (UIElement)(((object)window) ?? ((object)this));
		double targetWidth = ((window != null && window.ActualWidth > 0.0) ? window.ActualWidth : SystemParameters.WorkArea.Width);
		bool isCalendarContext = popup?.IsCalendarContext ?? false;
		IToastShowWindow toastWindow = ((!isCalendarContext) ? null : (popup?.DependentWindow ?? (popup?.Target as IToastShowWindow) ?? Utils.FindParent<IToastShowWindow>(popup?.Target)));
		CalendarDetailWindow window2 = new CalendarDetailWindow();
		Point restorePoint = default(Point);
		TaskDetailPopup taskDetailPopup = popup;
		if (taskDetailPopup != null && taskDetailPopup.DelayCalendarAddCleanup)
		{
			window2.EventCreated += delegate(object sender, CalendarEventModel e)
			{
				popup.NotifyCalendarEventCreated(e);
			};
			window2.Disappear += delegate
			{
				if (!window2.IsConvertingNewEventToTaskKind)
				{
					popup.CompleteCalendarAddFlow();
				}
			};
		}
		window2.ConvertNewEventToTaskKind += delegate(object sender, CalendarDetailConvertEventArgs e)
		{
			RestoreNewTaskDetailFromCalendar(e, target, targetWidth, restorePoint, toastWindow, isCalendarContext, popup);
		};
		if (popup != null && IsValidWindowPoint(popup.Left, popup.Top))
		{
			restorePoint = new Point(popup.Left, popup.Top);
			window2.Show(target, targetWidth, 0.0, byMouse: false, detailModel, (calendarPopupPoint == default(Point)) ? restorePoint : calendarPopupPoint, useAbsolutePoint: true);
			FocusCalendarDetailTitleEnd(window2);
		}
		else
		{
			window2.Show(target, targetWidth, 0.0, byMouse: false, detailModel);
			FocusCalendarDetailTitleEnd(window2);
		}
	}

	private async void FocusCalendarDetailTitleEnd(CalendarDetailWindow window)
	{
		await Task.Delay(300);
		if (window?.IsOpen ?? false)
		{
			HwndHelper.SetFocus(window, getKeyFocus: true);
			window.FocusTitleEnd();
		}
	}

	private static Point GetCalendarDetailPointFromTaskPopup(TaskDetailPopup popup)
	{
		if (popup == null || !IsValidWindowPoint(popup.Left, popup.Top))
		{
			return default(Point);
		}
		return new Point(popup.Left, popup.Top + 4.0);
	}

	private static bool IsValidWindowPoint(double left, double top)
	{
		if (!double.IsNaN(left) && !double.IsNaN(top) && !double.IsInfinity(left))
		{
			return !double.IsInfinity(top);
		}
		return false;
	}

	private void RestoreNewTaskDetailFromCalendar(CalendarDetailConvertEventArgs e, UIElement target, double targetWidth, Point restorePoint = default(Point), IToastShowWindow toastWindow = null, bool isCalendarContext = false, TaskDetailPopup calendarAddFlowSource = null)
	{
		if (_task == null || e?.Model == null)
		{
			return;
		}
		CalendarDetailViewModel model = e.Model;
		string title = model.Title ?? string.Empty;
		string content = model.Content ?? string.Empty;
		TaskBaseViewModel sourceViewModel = _task.SourceViewModel;
		if (sourceViewModel != null)
		{
			if (string.IsNullOrEmpty(sourceViewModel.Id))
			{
				sourceViewModel.Id = Utils.GetGuid();
			}
			sourceViewModel.CalendarId = null;
			sourceViewModel.Kind = (e.IsNote ? "NOTE" : "TEXT");
			sourceViewModel.Type = (e.IsNote ? DisplayType.Note : DisplayType.Task);
			sourceViewModel.SetProject(sourceViewModel.ProjectId);
			sourceViewModel.Title = title;
			sourceViewModel.Content = content;
			sourceViewModel.Desc = string.Empty;
			if (e.IsNote)
			{
				sourceViewModel.DueDate = null;
				sourceViewModel.Priority = 0;
				sourceViewModel.RepeatFlag = null;
				sourceViewModel.RepeatFrom = null;
			}
			TaskDetailViewModel taskDetailViewModel = new TaskDetailViewModel(sourceViewModel)
			{
				IsNewAdd = true
			};
			taskDetailViewModel.CopyKindConvertCacheFrom(_task);
			content = taskDetailViewModel.RestoreAttachmentMarkdownAfterKindConvert(content);
			TimeData timeData = BuildTimeDataFromCalendarDetail(model);
			taskDetailViewModel.CacheCalendarFieldsBeforeKindConvert(model.CalendarId, model.Location, timeData);
			if (e.IsNote)
			{
				taskDetailViewModel.CacheRepeatHiddenForNote(timeData);
				timeData = TimeData.Clone(timeData);
				ClearNoteOnlyTimeFields(timeData);
			}
			taskDetailViewModel.SetTimeData(timeData);
			if (!e.IsNote)
			{
				taskDetailViewModel.RestoreTaskFieldsAfterKindConvert();
			}
			else
			{
				taskDetailViewModel.RestoreAttachmentsAfterKindConvert();
			}
			taskDetailViewModel.SourceViewModel.Title = title;
			taskDetailViewModel.SourceViewModel.Content = content;
			taskDetailViewModel.SourceViewModel.Desc = string.Empty;
			TaskDetailPopup taskDetailPopup = new TaskDetailPopup();
			calendarAddFlowSource?.TransferCalendarAddFlowTo(taskDetailPopup);
			taskDetailPopup.DependentWindow = toastWindow;
			taskDetailPopup.IsCalendarContext = isCalendarContext;
			taskDetailPopup.Show(taskDetailViewModel, string.Empty, new TaskWindowDisplayArgs(target, targetWidth, default(Point), focus: true), restorePoint, show: true, canParse: true, ParentIdentity, calAdd: true);
		}
	}

	private TimeData BuildTimeDataFromCalendarDetail(CalendarDetailViewModel detailModel)
	{
		if (!detailModel.StartDate.HasValue)
		{
			return null;
		}
		return new TimeData
		{
			StartDate = detailModel.StartDate,
			DueDate = detailModel.DueDate,
			IsAllDay = detailModel.IsAllDay,
			RepeatFlag = detailModel.RepeatFlag,
			IsDefault = false,
			TimeZone = new TimeZoneViewModel(isFloat: false, detailModel.TimeZone ?? Utils.GetLocalTimeZone()),
			Reminders = (detailModel.Reminders?.Select((int minute) => new TaskReminderModel
			{
				trigger = TriggerUtils.ReminderToTrigger(minute)
			}).ToList() ?? new List<TaskReminderModel>())
		};
	}

	private static void ClearNoteOnlyTimeFields(TimeData timeData)
	{
		if (timeData != null)
		{
			timeData.DueDate = null;
			timeData.RepeatFlag = null;
			timeData.RepeatFrom = null;
			timeData.ExDates = new List<string>();
		}
	}

	public async Task OnDeleteFromTrash()
	{
		if (!IsNoPermission() && new CustomerDialog(Utils.GetString("DeleteForever"), Utils.GetString("DeleteForeverHint"), Utils.GetString("Delete"), Utils.GetString("Cancel"))
		{
			Owner = Window.GetWindow(this),
			Topmost = false
		}.ShowDialog() == true)
		{
			await DeleteTaskForever(2);
		}
	}

	public void OnBackClick(object sender)
	{
		if (Mode == Constants.DetailMode.Page)
		{
			OnBackClick();
		}
		else
		{
			this.NavigateBack?.Invoke(sender, null);
		}
	}

	public async Task OnCheckBoxClick()
	{
		if (CheckEnable())
		{
			UtilLog.Info($"TaskDetail.CompleteTask {_task?.TaskId}, CurrentStatus {_task?.Status} from:clickCheckBox");
			await CompleteOrUndoneTask();
		}
	}

	public void OnCheckBoxMouseRightUp(UIElement element)
	{
		TaskDetailViewModel task = _task;
		if (task == null || !CheckEnable())
		{
			return;
		}
		SetClosedStatusPopup instance = SetClosedStatusPopup.GetInstance(element, task.IsCompleted, task.IsAbandoned, -20.0, 10.0, (task.Status == 0) ? new int?(task.Progress.GetValueOrDefault()) : ((int?)null));
		AddActionEvent("action", "checkbox_cm");
		instance.Abandoned += async delegate
		{
			if (_task == task)
			{
				UtilLog.Info("TaskDetail.AbandonTask " + task.TaskId + " from:RightClickCheckBox");
				await AbandonedOrReopenTask(true);
			}
		};
		instance.Completed += async delegate
		{
			if (_task == task)
			{
				UtilLog.Info("TaskDetail.CompleteTask " + task.TaskId + " from:RightClickCheckBox");
				await CompleteOrUndoneTask(true);
			}
		};
		instance.ProgressChangeRequested += (int progress) => TryChangeProgress(progress, task.Progress.GetValueOrDefault(), task);
		instance.Closed += delegate
		{
			SetPopupClosing();
		};
		SetPopupShow();
		instance.Show();
	}

	public void ProgressClick(int pointerProgress, int currentProgress)
	{
		TaskDetailViewModel task = _task;
		if (task != null)
		{
			TryChangeProgressSafely(pointerProgress, currentProgress, task);
		}
	}

	private async Task TryChangeProgressSafely(int pointerProgress, int currentProgress, TaskDetailViewModel task)
	{
		try
		{
			await TryChangeProgress(pointerProgress, currentProgress, task);
		}
		catch (Exception ex)
		{
			UtilLog.Error("TaskDetailView.ChangeProgress failed: " + ex);
			TryToast(Utils.GetString("OperateFailedMessage"));
		}
	}

	private async Task<bool> TryChangeProgress(int pointerProgress, int currentProgress, TaskDetailViewModel task)
	{
		if (task == null || _task != task || !CheckEnable())
		{
			return false;
		}
		if (pointerProgress >= 0 && pointerProgress != currentProgress)
		{
			if (task.Status == 0 && task.StartDate.HasValue && task.DisplayStartDate.HasValue && task.StartDate.Value.Date != task.DisplayStartDate.Value.Date)
			{
				IToastShowWindow toastWindow = Utils.FindParent<TaskDetailPopup>(this)?.DependentWindow;
				if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(task.TaskId, task.DisplayStartDate, toastWindow)))
				{
					return false;
				}
				TaskModel taskModel = await TaskDao.GetThinTaskById(task.TaskId);
				if (taskModel != null)
				{
					await TaskService.SyncProgress(taskModel, pointerProgress);
					return true;
				}
			}
			int previousProgress = task.SourceViewModel.Progress;
			task.SourceViewModel.Progress = pointerProgress;
			try
			{
				await SaveTask(task);
			}
			catch
			{
				task.SourceViewModel.Progress = previousProgress;
				throw;
			}
			if (_task == task && _detailHead != null)
			{
				await _detailHead.SetProgress(pointerProgress, withAnim: true);
			}
			return true;
		}
		return false;
	}

	public void SetBackIconEnable(bool enable)
	{
		_detailHead?.SetBackIcon(enable, isNavigate: true);
	}

	public bool Enable()
	{
		return _task.Enable;
	}

	public bool CheckMouseOver()
	{
		AddCommentControl addCommentCtrl = _addCommentCtrl;
		if ((addCommentCtrl != null && addCommentCtrl.IsMouseOver) || (_detailScrollViewer.IsMouseOver && _contentText != null))
		{
			return Enable();
		}
		return false;
	}

	public bool TextFocus()
	{
		if (!_popupShowing)
		{
			DetailTextBox titleText = _titleText;
			if (titleText == null || !titleText.KeyboardFocused)
			{
				MarkDownEditor contentText = _contentText;
				if (contentText == null || !contentText.KeyboardFocused)
				{
					MarkDownEditor descText = _descText;
					if (descText == null || !descText.KeyboardFocused)
					{
						return _checklist?.ItemFocus() ?? false;
					}
				}
			}
		}
		return true;
	}

	public void SetCanScroll(bool b)
	{
		_canScroll = b;
	}

	public void OnCommentEdit(CommentItemControl commentItem)
	{
		Point point = commentItem.TranslatePoint(new Point(0.0, commentItem.ActualHeight), _scrollContent);
		if (_detailScrollViewer.VerticalOffset + _detailScrollViewer.ActualHeight < point.Y)
		{
			_detailScrollViewer.ScrollToVerticalOffset(point.Y - _detailScrollViewer.ActualHeight + 20.0);
		}
	}

	public void TryForceHideWindow()
	{
		this.ForceHideWindow?.Invoke(this, null);
	}

	public UIElement BatchOperaPlacementTarget()
	{
		return _detailHead?.GetDateDropTarget();
	}

	public bool TryAutoScroll(System.Windows.Input.MouseEventArgs e)
	{
		if (_autoScroller == null)
		{
			_autoScroller = new DragAutoScrollHelper(ScrollViewer);
		}
		return _autoScroller.TryAutoScroll(e);
	}

	public void StopAutoScroll()
	{
		_autoScroller?.SetAutoScrolling(autoScroll: false);
	}

	private void InitAttachmentOptionPanel()
	{
		_attachmentOptionPanel.MouseLeave += OnAttachmentMouseLeave;
		_attachmentOptionPanel.Delete += OnAttachmentDeleteClick;
		_attachmentOptionPanel.CheckListDelete += OnCheckListAttachmentDeleteClick;
		_attachmentOptionPanel.ImageModeChanged += OnImageModeChanged;
		_attachmentOptionPanel.SetValue(Grid.RowProperty, 4);
		_scrollContent.Children.Add(_attachmentOptionPanel);
	}

	private void SetAttachments(List<AttachmentViewModel> models)
	{
		bool valueOrDefault = FindResource("IsDarkTheme") as bool? == true;
		if (models != null && models.Count > 0)
		{
			if (_attachmentPanel == null)
			{
				_attachmentPanel = new StackPanel
				{
					Margin = new Thickness(2.0, 0.0, 0.0, 0.0),
					HorizontalAlignment = System.Windows.HorizontalAlignment.Left
				};
				_attachmentPanel.SetValue(Grid.RowProperty, 2);
				_contentGrid.Children.Add(_attachmentPanel);
			}
			_attachmentPanel.Children.Clear();
			{
				foreach (AttachmentViewModel model in models)
				{
					FrameworkElement element = ((model.FileType == "IMAGE") ? ((FrameworkElement)new AttachmentImageDisplayControl(model, HandleAttachmentMouseEnter)) : ((FrameworkElement)new AttachmentFileDisplayControl(model, HandleAttachmentMouseEnter, valueOrDefault)));
					_attachmentPanel.Children.Add(element);
				}
				return;
			}
		}
		if (_attachmentPanel != null)
		{
			_attachmentPanel.Children.Clear();
			_contentGrid.Children.Remove(_attachmentPanel);
			_attachmentPanel = null;
		}
	}

	private async Task LoadAttachmentAsync()
	{
		await _lockAttachment.RunAsync((Func<Task>)LoadAttachmentInternal);
	}

	private async Task LoadAttachmentInternal()
	{
		List<AttachmentModel> attachmentModelList = (await AttachmentDao.GetTaskAttachments(_task.TaskId))?.Where((AttachmentModel a) => a.status == 0).ToList();
		if (attachmentModelList == null || attachmentModelList.Count == 0)
		{
			_attachmentProvider?.Clear();
			SetAttachments(null);
			return;
		}
		foreach (AttachmentModel item in attachmentModelList)
		{
			if ((string.IsNullOrEmpty(item.refId) || item.refId == item.id) && string.IsNullOrEmpty(item.localPath) && string.IsNullOrEmpty(item.path))
			{
				await TaskService.CheckAttachmentPath(_task.ParentId, _task.TaskId, attachmentModelList);
				break;
			}
		}
		if (_task.Kind == "CHECKLIST")
		{
			List<AttachmentViewModel> models;
			if (_attachmentProvider == null)
			{
				_attachmentProvider = new AttachmentProvider(attachmentModelList, _task.ImageMode);
				models = _attachmentProvider.GetModels();
			}
			else
			{
				if (_task.TaskId != _attachmentProvider.TaskId)
				{
					_attachmentProvider.Clear();
				}
				_attachmentProvider.UpdateModels(attachmentModelList, _task.ImageMode, out var _, out var _);
				models = _attachmentProvider.GetModels();
			}
			bool isSmall = _task.ImageMode == 1;
			foreach (AttachmentViewModel item2 in models)
			{
				item2.CornerRadius = (isSmall ? new CornerRadius(3.0) : new CornerRadius(8.0));
				item2.Margin = new Thickness(0.0, 5.0, 0.0, 5.0);
			}
			_attachmentProvider.TaskId = _task.TaskId;
			int maxHeight = (isSmall ? ImageElementGenerator.SmallSizeHeight : 15000);
			double maxWidth = base.ActualWidth - (double)((Mode == Constants.DetailMode.Sticky) ? 30 : 44);
			if (maxWidth <= 0.0)
			{
				return;
			}
			SetAttachments(models);
			Task.Run(delegate
			{
				foreach (AttachmentViewModel item3 in models)
				{
					if (item3.FileType == "IMAGE")
					{
						Size imageSize = ThemeUtil.GetImageSize(item3.LocalPath);
						Size size = (isSmall ? ImageUtils.GetSmallRect(maxWidth, maxHeight, imageSize.Width, imageSize.Height) : ImageUtils.GetNormalRect(maxWidth, maxHeight, imageSize.Width, imageSize.Height));
						item3.Width = size.Width;
						item3.Height = size.Height;
						item3.Tag = item3;
					}
					else
					{
						item3.Width = maxWidth;
						item3.Tag = item3;
					}
				}
			});
		}
		else
		{
			_attachmentProvider?.Clear();
		}
	}

	private void HideAttachmentOption()
	{
		_attachmentOptionPanel.BeginAnimation(UIElement.OpacityProperty, null);
		_attachmentOptionPanel.Opacity = 0.0;
		_attachmentOptionPanel.Visibility = Visibility.Collapsed;
	}

	private async Task ShowUploadFileDialog()
	{
		UtilLog.Info("DetailShowFileSelector");
		SetPopupShow();
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = UserDao.IsPro()
		};
		int caretOffset = _contentText?.EditBox.CaretOffset ?? 0;
		DialogResult showDialog = openFileDialog.ShowDialog();
		if (_contentText != null)
		{
			_contentText.EditBox.CaretOffset = caretOffset;
			_contentText.FocusEditBox();
			await Task.Delay(100);
		}
		if (showDialog == DialogResult.OK)
		{
			int fileNum = openFileDialog.FileNames.Cast<object>().Count((object item) => File.Exists(item.ToString()));
			if (!(await CheckAttachmentLimit(fileNum)))
			{
				SetPopupClosing();
				return;
			}
			string[] fileNames = openFileDialog.FileNames;
			foreach (string text in fileNames)
			{
				if (!File.Exists(text))
				{
					continue;
				}
				string fileName = text.Split('\\').Last();
				fileName = FileUtils.TrimFileName(fileName, 240 - AppPaths.ImageDir.Length);
				string text2 = AppPaths.ImageDir + fileName;
				if (File.Exists(text2))
				{
					int num2 = fileName.LastIndexOf('.');
					if (num2 < 0)
					{
						num2 = fileName.Length;
					}
					int num3 = 1;
					do
					{
						num3++;
						text2 = AppPaths.ImageDir + fileName.Insert(num2, $"({num3})");
					}
					while (File.Exists(text2));
					fileName = fileName.Insert(num2, $"({num3})");
				}
				File.Copy(text, text2);
				await UploadAttachment(fileName, text2);
			}
			LoadAttachmentAsync();
			TaskDetailViewModel task = _task;
			task.Attachments = (await AttachmentDao.GetTaskAttachments(_task.TaskId)).ToArray();
			TaskChangeNotifier.NotifyAttachmentChanged(_task.TaskId, this);
		}
		SetPopupClosing();
	}

	private async Task UploadAttachment(string fileName, string filePath)
	{
		if (FileUtils.FileEmptyOrNotExists(filePath))
		{
			UtilLog.Warn("UploadAttachment empty file " + filePath);
			return;
		}
		Constants.AttachmentKind fileType = AttachmentProvider.GetFileType(fileName);
		FileInfo fileInfo = new FileInfo(filePath);
		AttachmentModel uploadAttachmentModel = new AttachmentModel
		{
			id = Utils.GetGuid()
		};
		uploadAttachmentModel.refId = uploadAttachmentModel.id;
		uploadAttachmentModel.taskId = _task.TaskId;
		uploadAttachmentModel.fileName = fileName;
		uploadAttachmentModel.localPath = filePath;
		uploadAttachmentModel.fileType = fileType.ToString();
		uploadAttachmentModel.createdTime = DateTime.Now;
		int level = 0;
		uploadAttachmentModel.sync_status = level.ToString();
		uploadAttachmentModel.size = (int)fileInfo.Length;
		if (uploadAttachmentModel.fileType == Constants.AttachmentKind.IMAGE.ToString())
		{
			AttachmentUploadUtils.CompressImage(uploadAttachmentModel);
		}
		FileUtils.CollectFileSize(uploadAttachmentModel.localPath);
		if (FileUtils.FileOverSize(uploadAttachmentModel.localPath))
		{
			TryToast(LocalSettings.Settings.IsPro ? Utils.GetString("AttachmentSizeLimitPro") : Utils.GetString("AttachmentSizeLimit"));
			return;
		}
		IToastShowWindow toastWindow = Utils.FindParent<IToastShowWindow>(this);
		string text = _contentText?.GetPersistedText() ?? string.Empty;
		string text2 = _contentText?.Text ?? string.Empty;
		string taskId = _task.TaskId;
		string attachmentText = string.Empty;
		int index = 0;
		bool insertBeforeHeading = false;
		UtilLog.Info($"PastFileCaret {_contentText?.CurrentFocused},{_contentText?.KeyboardFocused},{_contentText?.EditBox.CaretOffset},{_contentText?.EditBox.Text.Length}");
		if (_task.Kind != "CHECKLIST" && _contentText != null)
		{
			attachmentText = "![" + ((fileType == Constants.AttachmentKind.IMAGE) ? "image" : "file") + "](" + uploadAttachmentModel.id + "/" + Utils.UrlEncode(uploadAttachmentModel.fileName) + ")";
			index = ((_contentText.CurrentFocused || _contentText.EditBox.CaretOffset > 0) ? _contentText.EditBox.CaretOffset : _contentText.GetAttachmentDefaultInsertIndex());
			if (_contentText.TryExpandFoldedHeadingAtInsertionOffset(index))
			{
				text = _contentText.GetPersistedText();
				text2 = _contentText.Text ?? string.Empty;
				index = Math.Max(0, Math.Min(index, text2.Length));
			}
			index = _contentText.GetBlockInsertionOffsetAfterCurrentListLine(index);
			index = _contentText.GetAttachmentInsertionOffsetOutsideCodeBlock(index);
			DocumentLine lineByOffset = _contentText.EditBox.TextArea.Document.GetLineByOffset(Math.Min(index, text2.Length));
			bool folded = default(bool);
			if (lineByOffset != null && index > lineByOffset.Offset && index < lineByOffset.EndOffset && _contentText.TryGetHeadingFoldStateAtLine(lineByOffset.LineNumber - 1, out folded, out level) && folded && _contentText.TryClearHeadingFoldStateAtLine(lineByOffset.LineNumber - 1, notifyTextChanged: false))
			{
				text = _contentText.GetPersistedText();
				text2 = _contentText.Text ?? string.Empty;
				index = Math.Max(0, Math.Min(index, text2.Length));
				lineByOffset = _contentText.EditBox.TextArea.Document.GetLineByOffset(Math.Min(index, text2.Length));
			}
			insertBeforeHeading = lineByOffset != null && index == lineByOffset.Offset && IsPersistedHeadingLineAtOffset(text, _contentText.GetPersistedOffset(index));
			if (index != lineByOffset.Offset)
			{
				attachmentText = "\n" + attachmentText;
			}
			if (index != lineByOffset.EndOffset || index == text2.Length)
			{
				attachmentText += "\n";
			}
		}
		await AttachmentDao.InsertOrUpdateAttachment(uploadAttachmentModel);
		AttachmentCache.SetTodayAttachmentCount(AttachmentCache.TodayAttachmentCount() + 1);
		if (!_task.IsNewAdd)
		{
			AttachmentUploadUtils.UpFile(_task.ProjectId, _task.TaskId, uploadAttachmentModel, toastWindow, ignoreUploadCollect: true).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!(_task.Kind == "CHECKLIST"))
		{
			_ = text;
			string text3;
			if (_task.TaskId == taskId && _contentText != null)
			{
				int num = Math.Max(0, Math.Min(index, _contentText.Text?.Length ?? 0));
				string persistedText = _contentText.GetPersistedText();
				int val = (insertBeforeHeading ? GetPersistedLineStart(persistedText, _contentText.GetPersistedOffset(num)) : (attachmentText.StartsWith("\n", StringComparison.Ordinal) ? _contentText.GetPersistedOffsetAfterLineBlockMetadata(num) : _contentText.GetPersistedOffset(num)));
				text3 = persistedText.Insert(Math.Max(0, Math.Min(val, persistedText.Length)), attachmentText);
				_contentText.EditBox.Select(num, 0);
				_contentText.InsertPersistedEditorText(attachmentText);
				int caretOffset = _contentText.EditBox.CaretOffset;
				_contentText.ReapplyPersistedEditorStatePreservingHistory(text3, caretOffset);
				_contentText.FocusEditBox();
				SetContentHint(text3);
				_task.SourceViewModel.Content = text3;
				_contentText.TryAcknowledgePendingLocalDocumentChanges(text3);
			}
			else
			{
				int startIndex = ((!attachmentText.StartsWith("\n", StringComparison.Ordinal)) ? (_contentText?.GetPersistedOffset(index) ?? index) : (_contentText?.GetPersistedOffsetAfterLineBlockMetadata(index) ?? index));
				text3 = text.Insert(startIndex, attachmentText);
			}
			await TaskService.SaveTaskContent(taskId, text3);
		}
	}

	private static bool IsPersistedHeadingLineAtOffset(string persistedText, int persistedOffset)
	{
		persistedText = persistedText ?? string.Empty;
		if (persistedText.Length == 0)
		{
			return false;
		}
		int persistedLineStart = GetPersistedLineStart(persistedText, persistedOffset);
		int num = persistedText.IndexOf('\n', persistedLineStart);
		if (num < 0)
		{
			num = persistedText.Length;
		}
		MarkdownBlockSyntaxInfo markdownBlockSyntaxInfo = MarkdownBlockSyntax.Parse(persistedText.Substring(persistedLineStart, num - persistedLineStart).TrimEnd('\r'));
		if (markdownBlockSyntaxInfo == null)
		{
			return false;
		}
		return markdownBlockSyntaxInfo.Kind == MarkdownBlockSyntaxKind.Heading;
	}

	private static int GetPersistedLineStart(string persistedText, int persistedOffset)
	{
		persistedText = persistedText ?? string.Empty;
		int num = Math.Max(0, Math.Min(persistedOffset, persistedText.Length));
		return persistedText.LastIndexOf('\n', Math.Max(0, num - 1)) + 1;
	}

	public async Task<bool> CheckAttachmentLimit(int fileNum, int replaceNum = 0, bool ignoreUpload = false)
	{
		if (!ignoreUpload)
		{
			Tuple<long, long> residueAttachmentCount = Utils.GetResidueAttachmentCount(fileNum);
			if (residueAttachmentCount.Item1 < fileNum)
			{
				if (UserDao.IsPro())
				{
					SetPopupShow();
					new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("attachments_upload_limit_dialog_msg"), residueAttachmentCount.Item1, residueAttachmentCount.Item2), MessageBoxButton.OK, Window.GetWindow(this)).ShowDialog();
					SetPopupClosing();
				}
				else
				{
					TaskDetailPopup.TryCloseWindow();
					ProChecker.ShowUpgradeDialog(ProType.MoreAttachments);
				}
				return false;
			}
		}
		int num = await AttachmentDao.GetTaskAttachmentCount(_task.TaskId);
		long userLimit = Utils.GetUserLimit(Constants.LimitKind.TaskAttachmentNumber);
		if (num + fileNum - replaceNum > userLimit)
		{
			SetPopupShow();
			new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("TaskAttachmentsLimitContent"), MessageBoxButton.OK, Window.GetWindow(this)).ShowDialog();
			SetPopupClosing();
			return false;
		}
		return true;
	}

	private BitmapSource GetPastImage()
	{
		BitmapSource result = null;
		try
		{
			if (string.IsNullOrEmpty(System.Windows.Clipboard.GetText()))
			{
				result = System.Windows.Clipboard.GetImage();
			}
		}
		catch (Exception)
		{
		}
		return result;
	}

	private bool CheckPasteEnable()
	{
		if (!CheckEnable())
		{
			return false;
		}
		if (!_task.IsOwner)
		{
			TryToast(Utils.GetString("AttendeeModifyContent"));
			return false;
		}
		return true;
	}

	private async Task<bool> TryPasteFile(IReadOnlyCollection<string> files)
	{
		if (files == null || files.Count <= 0)
		{
			return false;
		}
		if (!(await CheckAttachmentLimit(files.Count)))
		{
			return true;
		}
		if (files.Count > 0)
		{
			foreach (string file in files)
			{
				string fileName = IOUtils.GetFileName(file);
				string text = FileUtils.SavePasteFile(file, fileName);
				try
				{
					if (File.Exists(text))
					{
						await UploadAttachment(fileName, text);
					}
				}
				catch (Exception)
				{
					TryToast(Utils.GetString("AddFailed"));
				}
			}
			LoadAttachmentAsync();
			TaskDetailViewModel task = _task;
			task.Attachments = (await AttachmentDao.GetTaskAttachments(_task.TaskId)).ToArray();
			TaskChangeNotifier.NotifyAttachmentChanged(_task.TaskId, this);
			return true;
		}
		return false;
	}

	private async Task TryPasteImage(BitmapSource image)
	{
		if (image != null && await CheckAttachmentLimit(1))
		{
			string text = Utils.GetGuid() + ".png";
			string text2 = ImageUtils.SavePasteImage(image, text);
			if (File.Exists(text2))
			{
				await UploadAttachment(text, text2);
				LoadAttachmentAsync();
				TaskDetailViewModel task = _task;
				task.Attachments = (await AttachmentDao.GetTaskAttachments(_task.TaskId)).ToArray();
				TaskChangeNotifier.NotifyAttachmentChanged(_task.TaskId, this);
			}
		}
	}

	private async void OnAttachmentDrop(object sender, System.Windows.DragEventArgs e)
	{
		e.Handled = true;
		TryPasteFile(GetDropFiles(e));
	}

	private static List<string> GetDropFiles(System.Windows.DragEventArgs e)
	{
		try
		{
			Array array = (Array)e.Data.GetData(System.Windows.DataFormats.FileDrop);
			List<string> list = new List<string>();
			if (array != null)
			{
				foreach (object item in array)
				{
					list.Add(item.ToString());
				}
			}
			return list;
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex.Message);
			return new List<string>();
		}
	}

	private void AttachmentDeleteGridMouseEnter(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		if (sender is Grid { DataContext: AttachmentViewModel dataContext } grid)
		{
			grid.Cursor = (dataContext.CanDelete ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.No);
		}
	}

	private async Task TryLoadComments(bool showRecent = true, bool focus = false, bool loadLocal = true, bool loadRemote = true)
	{
		if (Mode == Constants.DetailMode.Editor || Mode == Constants.DetailMode.Sticky)
		{
			return;
		}
		TaskDetailViewModel task = _task;
		if (task == null)
		{
			return;
		}
		int loadVersion = ++_commentLoadVersion;
		string taskId = task.TaskId;
		string projectId = task.ProjectId;
		int taskDeleted = task.Deleted;
		string permission = task.Permission;
		if (_addCommentCtrl?.EditModel != null)
		{
			_addCommentCtrl.EditModel.RecentScrollHeight = 0.0;
		}
		int.TryParse(task.CommentCount, out var result);
		if (result > 0)
		{
			if (loadLocal)
			{
				await TryLoadLocalComments(focus, loadVersion, taskId, projectId, taskDeleted, permission);
			}
			if (!IsCurrentCommentLoad(loadVersion, taskId))
			{
				return;
			}
			if (loadRemote)
			{
				TryLoadRemoteComments(projectId, taskId);
			}
		}
		else if (IsCurrentCommentLoad(loadVersion, taskId))
		{
			ClearComment();
		}
		if (IsCurrentCommentLoad(loadVersion, taskId) && taskDeleted != 0)
		{
			HideAddComment();
		}
	}

	private void ClearComment()
	{
		AddCommentControl addCommentCtrl = _addCommentCtrl;
		if (addCommentCtrl == null || !addCommentCtrl.IsEditing())
		{
			HideAddComment();
		}
		_scrollContent.Children.Remove(_commentDisplayCtrl);
		_commentDisplayCtrl = null;
	}

	private void TryLoadRemoteComments(string projectId, string taskId)
	{
		CommentService.TryPullRemoteComments(projectId, taskId);
	}

	private async Task TryLoadLocalComments(bool focus = false)
	{
		TaskDetailViewModel task = _task;
		if (task != null && !IsEditingCommentForTask(task.TaskId))
		{
			await TryLoadLocalComments(focus, ++_commentLoadVersion, task.TaskId, task.ProjectId, task.Deleted, task.Permission);
		}
	}

	private async Task TryLoadLocalComments(bool focus, int loadVersion, string taskId, string projectId, int taskDeleted, string taskPermission)
	{
		if (IsEditingCommentForTask(taskId))
		{
			return;
		}
		List<CommentViewModel> list = await GetLocalCommentViewModels(taskId, projectId, taskDeleted);
		if (!IsCurrentCommentLoad(loadVersion, taskId) || IsEditingCommentForTask(taskId))
		{
			return;
		}
		if (list.Count > 0)
		{
			CommentEditViewModel commentEditViewModel = ((_addCommentCtrl?.EditModel != null && _addCommentCtrl.EditModel.TaskId == taskId) ? _addCommentCtrl.EditModel : new CommentEditViewModel
			{
				TaskId = taskId,
				ProjectId = projectId,
				CommentId = Utils.GetGuid()
			});
			commentEditViewModel.RecentComment = list[list.Count - 1];
			commentEditViewModel.RecentScrollHeight = 0.0;
			commentEditViewModel.ShowTopLine = list.Count == 0;
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
			CommentListViewModel commentListViewModel = new CommentListViewModel(list, commentEditViewModel, taskDeleted == 0 && projectModel != null && projectModel.IsValid() && taskPermission != "read");
			if (taskDeleted == 0 && projectModel != null && projectModel.IsValid())
			{
				TryInitAddCommentCtrl(commentEditViewModel, focus);
			}
			TryInitCommentDisplayCtrl(commentListViewModel);
		}
		else
		{
			if (_addCommentCtrl?.EditModel != null)
			{
				_addCommentCtrl.EditModel.RecentComment = null;
				_addCommentCtrl.EditModel.RecentScrollHeight = 0.0;
			}
			if (_commentDisplayCtrl != null)
			{
				_scrollContent.Children.Remove(_commentDisplayCtrl);
				_commentDisplayCtrl = null;
			}
		}
	}

	private bool IsCurrentCommentLoad(int loadVersion, string taskId)
	{
		if (_commentLoadVersion == loadVersion)
		{
			return _task?.TaskId == taskId;
		}
		return false;
	}

	private bool IsEditingCommentForTask(string taskId)
	{
		if (_commentDisplayCtrl != null && object.Equals(_commentDisplayCtrl.Tag, taskId))
		{
			return _commentDisplayCtrl.Model.DisplayList?.Any((CommentViewModel m) => m.Editing) ?? false;
		}
		return false;
	}

	private void TryInitCommentDisplayCtrl(CommentListViewModel commentListViewModel)
	{
		if (_commentDisplayCtrl == null)
		{
			_commentDisplayCtrl = new CommentDisplayControl(commentListViewModel);
			_commentDisplayCtrl.ShowAddComment += TryShowAddComment;
			_commentDisplayCtrl.SetValue(Grid.RowProperty, 6);
			_scrollContent.Children.Add(_commentDisplayCtrl);
		}
		else
		{
			_commentDisplayCtrl.SetModel(commentListViewModel);
		}
		_commentDisplayCtrl.Tag = TaskId;
	}

	private void TryShowAddComment(object sender, CommentViewModel e)
	{
		if (!(_task.Permission == "read") || CheckEnable())
		{
			CommentEditViewModel commentEditViewModel = _addCommentCtrl?.EditModel ?? new CommentEditViewModel
			{
				TaskId = _task.TaskId,
				ProjectId = _task.ProjectId
			};
			commentEditViewModel.ReplyCommentId = e.Model.id;
			commentEditViewModel.ReplyName = e.Model.userName;
			TryInitAddCommentCtrl(commentEditViewModel, withFocus: true);
		}
	}

	private void HideAddComment()
	{
		base.Children.Remove(_addCommentCtrl);
		_addCommentCtrl = null;
		base.RowDefinitions[3].Height = new GridLength(0.0, GridUnitType.Auto);
	}

	private async void DelayResetCommentHeight()
	{
		await Task.Delay(200);
		base.RowDefinitions[3].Height = new GridLength(0.0, GridUnitType.Auto);
	}

	private void TryInitAddCommentCtrl(CommentEditViewModel editCommentModel, bool withFocus = false)
	{
		if (!_task.Enable && _task.Permission == "read")
		{
			HideAddComment();
			return;
		}
		if (_addCommentCtrl == null)
		{
			_addCommentCtrl = new AddCommentControl(editCommentModel, this);
			_addCommentCtrl.CommentGotFocus += OnCommentGotFocus;
			base.RowDefinitions[3].Height = new GridLength(52.0);
			_addCommentCtrl.SetValue(Grid.RowProperty, 3);
			DelayResetCommentHeight();
			_addCommentCtrl.PopupShowing += delegate(object sender, bool isOpen)
			{
				TaskDetailPopup.SetCanClose(!isOpen);
			};
			base.Children.Add(_addCommentCtrl);
			_addCommentCtrl.OnCommentAdded += async delegate(object sender, CommentModel content)
			{
				UserInfoModel userInfoModel = await UserManager.GetUserInfo(force: true);
				content.userName = ((!string.IsNullOrEmpty(userInfoModel?.name)) ? userInfoModel.name : userInfoModel?.username);
				content.avatarUrl = userInfoModel?.picture;
				content.createdTime = DateTime.Now;
				content.syncStatus = 0;
				content.deleted = 0;
				content.isMySelf = true;
				content.mentionstring = JsonConvert.SerializeObject(content.mentions);
				_commentDisplayCtrl?.AddNewItem(content);
				_commentDisplayCtrl?.ScrollToBottom();
				if (_addCommentCtrl.EditModel == null)
				{
					_addCommentCtrl.EditModel = new CommentEditViewModel
					{
						CommentId = Utils.GetGuid()
					};
				}
				await Task.Delay(1);
				UtilLog.Info("TaskDetail.AddComment taskId " + _task?.TaskId + ", commentId " + content.id);
				await SaveNewAddComment(content);
				await TryLoadComments(showRecent: false, focus: true);
				await SyncTaskCommentCount();
			};
		}
		else
		{
			_addCommentCtrl.SetModel(editCommentModel);
		}
		if (withFocus)
		{
			_addCommentCtrl.FocusInput();
		}
	}

	private void OnCommentGotFocus(object sender, EventArgs e)
	{
		SetTextEditorMenu(show: false);
	}

	private async Task<List<CommentViewModel>> GetLocalCommentViewModels(string taskId, string projectId, int taskDeleted)
	{
		List<CommentViewModel> comments = new List<CommentViewModel>();
		List<CommentModel> models = await CommentDao.GetCommentsByTaskId(taskId);
		foreach (CommentModel model in models)
		{
			model.candelete = taskDeleted == 0;
			if (!string.IsNullOrEmpty(model.replyCommentId))
			{
				CommentModel commentModel = models.FirstOrDefault((CommentModel v) => v.id == model.replyCommentId);
				if (commentModel != null)
				{
					model.title = Utils.GetString("reply") + " " + commentModel.userName + ": " + model.title;
				}
			}
		}
		if (models.Count > 0 && models.Count > 0)
		{
			List<ShareUserModel> users = await AvatarHelper.GetProjectUsersAsync(projectId);
			List<AttachmentModel> attachmentsInComments = await AttachmentDao.GetTaskAttachmentsInTasks(models.Select((CommentModel it) => it.id).ToList());
			IEnumerable<string> deletedUserIds = null;
			if (users != null)
			{
				deletedUserIds = from user in users
					where user.deleted
					select user.userId.ToString();
			}
			models.ForEach(delegate(CommentModel commentModel2)
			{
				if (deletedUserIds == null || !deletedUserIds.Contains(commentModel2.userId))
				{
					List<AddTaskAttachmentInfo> list = (from it in attachmentsInComments
						where it.taskId == commentModel2.id
						select new AddTaskAttachmentInfo(it.localPath)).ToList();
					CommentViewModel item = new CommentViewModel(commentModel2)
					{
						Files = new ObservableCollection<AddTaskAttachmentInfo>(list)
					};
					comments.Add(item);
				}
			});
		}
		return comments;
	}

	public void ExpandComment()
	{
		_commentDisplayCtrl?.ExpandComment();
	}

	public void ExpandCommentAndScrollIntoView()
	{
		_commentDisplayCtrl?.ExpandComment();
		base.Dispatcher.BeginInvoke(new Action(ScrollCommentDisplayIntoView), DispatcherPriority.ContextIdle);
	}

	private void ScrollCommentDisplayIntoView()
	{
		if (_commentDisplayCtrl == null || _scrollContent == null)
		{
			return;
		}
		try
		{
			_scrollContent.UpdateLayout();
			_commentDisplayCtrl.UpdateLayout();
			_detailScrollViewer.ScrollToBottom();
			_detailScrollViewer.UpdateLayout();
			_commentDisplayCtrl.UpdateRecentCommentAnchor();
		}
		catch (InvalidOperationException)
		{
		}
	}

	private async Task SaveNewAddComment(CommentModel comment)
	{
		await CommentService.AddComment(comment);
		await TaskService.SaveCommentCount(comment.taskSid);
	}

	private async Task SyncTaskCommentCount()
	{
		string commentCount = _commentDisplayCtrl?.Model.Count.ToString() ?? "1";
		TaskModel task = await TaskDao.GetThinTaskById(_task.TaskId);
		if (task != null)
		{
			task.commentCount = commentCount;
			await TaskDao.UpdateTask(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
		}
	}

	private void InitContentPanel()
	{
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		_contentGrid.SetValue(Grid.RowProperty, 4);
		_contentGrid.Margin = new Thickness(1.0, 5.0, 0.0, (Mode == Constants.DetailMode.Sticky) ? 5 : 0);
		_contentGrid.SizeChanged += OnContentSizeChanged;
		_scrollContent.Children.Add(_contentGrid);
		InitAttachmentOptionPanel();
	}

	private void OnDetailContentClick(object sender, MouseButtonEventArgs e)
	{
		MarkDownEditor contentText = _contentText;
		if (contentText != null && !_titleText.IsMouseOver && _subTaskList == null && (_commentDisplayCtrl == null || !_commentDisplayCtrl.IsMouseOver) && (_attachmentOptionPanel.Visibility != Visibility.Visible || !_attachmentOptionPanel.IsMouseOver) && !_attachmentOptionPanel.IsAnyPopupOpen() && (!contentText.KeyboardFocused || e.GetPosition(contentText).Y > contentText.ActualHeight))
		{
			Window window = Window.GetWindow(this);
			if (window != null && window.IsActive)
			{
				contentText.EditBox.CaretOffset = contentText.EditBox.Text.Length;
				contentText.FocusEditBox();
				e.Handled = true;
			}
		}
		if (_contentText != null)
		{
			SetContentHint(_task.TaskContent);
			_contentText.TryDismissEditorMenu();
			if (!_contentText.IsStyleMenuOpen() && _contentText.KeyboardFocused && _contentText.EditBox.SelectedText.Length > 0)
			{
				_contentText.EditBox.Select(_contentText.EditBox.SelectionStart + _contentText.EditBox.SelectedText.Length, 0);
			}
		}
	}

	private void LoadContentControl(bool restoreOffset, bool sameTask = true)
	{
		TaskDetailViewModel task = _task;
		if (task.Kind == "CHECKLIST")
		{
			if (_contentText != null)
			{
				RemoveContentEvent(_contentText);
				_contentGrid.Children.Remove(_contentText);
				_contentText = null;
			}
			if (_descText == null)
			{
				_descText = new MarkDownEditor
				{
					MaxLength = 160000,
					EscapePlainInlineSyntaxOnPersistence = true,
					Margin = new Thickness(-20.0, 0.0, 0.0, 4.0),
					VerticalAlignment = VerticalAlignment.Top,
					VerticalContentAlignment = VerticalAlignment.Center
				};
				_descText.SetResourceReference(MarkDownEditor.IsDarkProperty, "IsDarkTheme");
				_descText.SetResourceReference(System.Windows.Controls.Control.FontSizeProperty, (Mode == Constants.DetailMode.Sticky) ? "StickyFont13" : "Font14");
				_descText.SetValue(System.Windows.Controls.Panel.ZIndexProperty, 10);
				_descText.EditorMenu.EditorAction -= OnEditorAction;
				_descText.EditorMenu.EditorAction += OnEditorAction;
				_descText.EditorMenu.HighlightColorRequested -= OnHighlightColorRequested;
				_descText.EditorMenu.HighlightColorRequested += OnHighlightColorRequested;
				_descText.StyleMenu.EditorStyleAction -= OnStyleClick;
				_descText.StyleMenu.EditorStyleAction += OnStyleClick;
				BindDescEvent(_descText);
				_contentGrid.Children.Add(_descText);
				if (Mode == Constants.DetailMode.Sticky)
				{
					_descText.SetTheme(_isDark != true);
					_descText.Margin = new Thickness(-10.0, 0.0, 0.0, 2.0);
					_descText.SetEditorForegroundResource("StickyContentTextColor");
					_descText.SetupMargin(12.0, inSticky: true);
					_descText.LineSpacing = 4.0;
				}
				if (Mode == Constants.DetailMode.Page)
				{
					_descText.SetupSearchRender(isTitle: false, inDetail: true);
				}
			}
			_descText.ReadOnly = !task.Enable;
			SetContentHint(task.Desc);
			bool num = ShouldPreservePendingLocalEditorState(_descText, task.Desc, sameTask);
			bool flag = _descText.GetPersistedText() != task.Desc;
			if (!num && (!sameTask || flag))
			{
				if (!sameTask)
				{
					_descText.SetText(task.Desc);
				}
				else
				{
					_descText.SetExternalTextAndOffset(task.Desc, restoreOffset);
				}
			}
			if (_checklist == null)
			{
				_checklist = new ChecklistControl
				{
					Margin = new Thickness(-20.0, 4.0, 2.0, 12.0),
					Background = Brushes.Transparent
				};
				_checklist.SetValue(Grid.RowProperty, 1);
				BindChecklistEvent(_checklist);
				_contentGrid.Children.Add(_checklist);
				if (Mode == Constants.DetailMode.Sticky)
				{
					_checklist.Margin = new Thickness(-20.0, 0.0, 2.0, 0.0);
					_checklist?.SetStickyTheme(_isDark == true);
					_checklist.SetStickyMode();
				}
				if (Mode == Constants.DetailMode.Page)
				{
					_checklist.InMainDetail = true;
				}
			}
			_checklist.IsNewAddTask = task.IsNewAdd;
		}
		else
		{
			SetAttachments(null);
			if (_descText != null)
			{
				RemoveDescEvent(_descText);
				_contentGrid.Children.Remove(_descText);
				_descText = null;
			}
			if (_checklist != null)
			{
				RemoveChecklistEvent(_checklist);
				_contentGrid.Children.Remove(_checklist);
				_checklist = null;
			}
			if (_contentText == null)
			{
				_contentText = new MarkDownEditor
				{
					MaxLength = 160000,
					EscapePlainInlineSyntaxOnPersistence = true,
					Margin = new Thickness(-20.0, 0.0, 0.0, 4.0),
					VerticalAlignment = VerticalAlignment.Top,
					VerticalContentAlignment = VerticalAlignment.Center
				};
				_contentText.SetBinding(MarkDownEditor.ImageModeProperty, "ImageMode");
				_contentText.SetResourceReference(MarkDownEditor.IsDarkProperty, "IsDarkTheme");
				_contentText.SetResourceReference(System.Windows.Controls.Control.FontSizeProperty, (Mode == Constants.DetailMode.Sticky) ? "StickyFont13" : "Font14");
				_contentText.SetValue(System.Windows.Controls.Panel.ZIndexProperty, 10);
				BindContentEvent(_contentText);
				_contentGrid.Children.Add(_contentText);
				_contentText.SetMarkRegexText(unable: false, inDetail: true, _task.Kind == "NOTE");
				if (Mode == Constants.DetailMode.Sticky)
				{
					_contentText.SetTheme(_isDark != true);
					_contentText.Margin = new Thickness(-10.0, 0.0, 0.0, 2.0);
					_contentText.SetEditorForegroundResource("StickyContentTextColor");
					_contentText.SetupMargin(12.0, inSticky: true);
					_contentText.LineSpacing = 4.0;
				}
				if (Mode == Constants.DetailMode.Page)
				{
					_contentText.SetupSearchRender(isTitle: false, inDetail: true);
				}
				if (Mode == Constants.DetailMode.Editor)
				{
					_contentText.InImmersiveMode = true;
				}
				_contentText.EditorMenu.EditorAction -= OnEditorAction;
				_contentText.EditorMenu.EditorAction += OnEditorAction;
				_contentText.EditorMenu.HighlightColorRequested -= OnHighlightColorRequested;
				_contentText.EditorMenu.HighlightColorRequested += OnHighlightColorRequested;
				_contentText.StyleMenu.EditorStyleAction -= OnStyleClick;
				_contentText.StyleMenu.EditorStyleAction += OnStyleClick;
			}
			_contentText.ReadOnly = !task.Enable;
			_contentText.SetImageGeneratorTaskId(task.TaskId);
			SetContentHint(task.TaskContent);
			string text = task.TaskContent ?? string.Empty;
			if (!string.IsNullOrEmpty(_guideLink) && text.Contains("\n" + _guideLink))
			{
				text = text.TrimEnd().Replace("\n" + _guideLink, "");
			}
			bool num2 = ShouldPreservePendingLocalEditorState(_contentText, text, sameTask);
			bool flag2 = _contentText.GetPersistedText() != text;
			if (!num2 && (!sameTask || flag2))
			{
				if (!sameTask)
				{
					_contentText.SetText(text);
				}
				else
				{
					_contentText.SetExternalTextAndOffset(text, restoreOffset);
				}
			}
		}
		UpdateSelectionViewportBottomInset();
	}

	private void LoadTags(IReadOnlyCollection<string> tags)
	{
		if (tags.Count > 0)
		{
			if (_tagsControl == null)
			{
				_tagsControl = new TagDisplayControl(tags, _task.Enable)
				{
					Margin = new Thickness(0.0, 10.0, 0.0, 8.0)
				};
				_tagsControl.TagsChanged += OnTagsChanged;
				_tagsControl.TagClick += OnTagLabelClick;
				_tagsControl.TagPanelVisibleChanged += OnTagPanelVisibleChanged;
				_tagsControl.SetValue(Grid.RowProperty, 4);
				_contentGrid.Children.Add(_tagsControl);
			}
			else
			{
				if (_tagsControl != null && !_contentGrid.Children.Contains(_tagsControl))
				{
					_contentGrid.Children.Add(_tagsControl);
				}
				_tagsControl.InitData(tags, _task.Enable);
			}
		}
		else if (_tagsControl != null)
		{
			_contentGrid.Children.Remove(_tagsControl);
		}
	}

	private static bool ShouldPreservePendingLocalEditorState(MarkDownEditor editor, string persistedText, bool sameTask)
	{
		if (sameTask && editor != null && editor.HasPendingLocalDocumentChanges)
		{
			return !editor.TryAcknowledgePendingLocalDocumentChanges(persistedText);
		}
		return false;
	}

	private void OnTagLabelClick(object sender, string tag)
	{
		this.TagClick?.Invoke(this, tag);
	}

	private void OnTagPanelVisibleChanged(object sender, Visibility visibility)
	{
		if (visibility == Visibility.Visible)
		{
			SetPopupShow();
		}
		else
		{
			SetPopupClosing();
		}
	}

	private void BindChecklistEvent(ChecklistControl checklist)
	{
		checklist.Changed += ChecklistModified;
		checklist.ItemsClear += OnItemsEmpty;
		checklist.PopOpened += OnChecklistPopupOpened;
		checklist.PopClosed += OnChecklistPopupClosed;
		checklist.QuickItemSelected += OnQuickItemSelected;
		checklist.ToastUnableText += TryToastUnableText;
		checklist.Navigate += OnNavigateTask;
		checklist.DatePopOpened += PopupOpened;
		checklist.DatePopClosed += PopupClosed;
		checklist.CaretVerticalOffsetChanged += OnContentVerticalOffsetChanged;
		checklist.CaretMoveUp += OnListCaretMoveUp;
		checklist.CheckItemsDeleted += OnCheckItemsDeleted;
	}

	private void RemoveChecklistEvent(ChecklistControl checklist)
	{
		checklist.Changed -= ChecklistModified;
		checklist.ItemsClear -= OnItemsEmpty;
		checklist.PopOpened -= OnChecklistPopupOpened;
		checklist.PopClosed -= OnChecklistPopupClosed;
		checklist.QuickItemSelected -= OnQuickItemSelected;
		checklist.ToastUnableText -= TryToastUnableText;
		checklist.Navigate -= OnNavigateTask;
		checklist.DatePopOpened -= PopupOpened;
		checklist.DatePopClosed -= PopupClosed;
		checklist.CaretVerticalOffsetChanged -= OnContentVerticalOffsetChanged;
		checklist.CaretMoveUp -= OnListCaretMoveUp;
		checklist.CheckItemsDeleted -= OnCheckItemsDeleted;
	}

	private void BindDescEvent(MarkDownEditor desc)
	{
		desc.RequestBringIntoView += OnBringIntoView;
		desc.QuickPopupOpened += OnTextPopupOpened;
		desc.QuickItemSelected += OnQuickItemSelected;
		desc.LinkPopupOpened += OnLinkPopupOpened;
		desc.LinkPopupClosed += OnLinkPopupClosed;
		desc.CaretVerticalOffsetChanged += OnContentVerticalOffsetChanged;
		desc.GotFocus += OnDescGotFocus;
		desc.LostFocus += OnDescLostFocus;
		desc.Navigate += OnNavigateTask;
		desc.KeyDown += OnContentKeyDown;
		desc.MoveUp += OnContentMoveUp;
		desc.MoveDown += OnDescMoveDown;
		desc.EnterImmersive += OnEnterImmersive;
		desc.SelectDate += OnSelectDate;
		desc.TextChanged += OnDescriptionChanged;
		desc.EditorStateChanged += OnEditorStateChanged;
		desc.EditBox.PreviewTextInput += OnContentInput;
		desc.EditBox.TextArea.SelectionChanged += OnEditorSelectionChanged;
		desc.EditBox.TextArea.Caret.PositionChanged += OnEditorCaretPositionChanged;
		desc.SaveContent += TrySaveOnEnter;
		desc.EscKeyUp += OnEscKeyUp;
	}

	private void RemoveDescEvent(MarkDownEditor desc)
	{
		desc.RequestBringIntoView -= OnBringIntoView;
		desc.QuickPopupOpened -= OnTextPopupOpened;
		desc.QuickItemSelected -= OnQuickItemSelected;
		desc.LinkPopupOpened -= OnLinkPopupOpened;
		desc.LinkPopupClosed -= OnLinkPopupClosed;
		desc.CaretVerticalOffsetChanged -= OnContentVerticalOffsetChanged;
		desc.GotFocus -= OnDescGotFocus;
		desc.LostFocus -= OnDescLostFocus;
		desc.Navigate -= OnNavigateTask;
		desc.KeyDown -= OnContentKeyDown;
		desc.MoveUp -= OnContentMoveUp;
		desc.MoveDown -= OnDescMoveDown;
		desc.EnterImmersive -= OnEnterImmersive;
		desc.SelectDate -= OnSelectDate;
		desc.TextChanged -= OnDescriptionChanged;
		desc.EditorStateChanged -= OnEditorStateChanged;
		desc.EditBox.PreviewTextInput -= OnContentInput;
		desc.EditBox.TextArea.SelectionChanged -= OnEditorSelectionChanged;
		desc.EditBox.TextArea.Caret.PositionChanged -= OnEditorCaretPositionChanged;
		desc.SaveContent -= TrySaveOnEnter;
		desc.EscKeyUp -= OnEscKeyUp;
	}

	private void BindContentEvent(MarkDownEditor contentText)
	{
		contentText.RequestBringIntoView += OnBringIntoView;
		contentText.QuickPopupOpened += OnTextPopupOpened;
		contentText.QuickPopupClosed += OnTextPopupClosed;
		contentText.QuickItemSelected += OnQuickItemSelected;
		contentText.LinkPopupOpened += OnLinkPopupOpened;
		contentText.LinkPopupClosed += OnLinkPopupClosed;
		contentText.CaretVerticalOffsetChanged += OnContentVerticalOffsetChanged;
		contentText.GotFocus += OnContentGotFocus;
		contentText.LostFocus += OnContentLostFocus;
		contentText.Navigate += OnNavigateTask;
		contentText.KeyDown += OnContentKeyDown;
		contentText.MoveUp += OnContentMoveUp;
		contentText.SizeChanged += OnContentSizeChanged;
		contentText.EnterImmersive += OnEnterImmersive;
		contentText.SelectDate += OnSelectDate;
		contentText.TextChanged += OnTaskContentChanged;
		contentText.EditorStateChanged += OnEditorStateChanged;
		contentText.EditBox.PreviewTextInput += OnContentInput;
		contentText.EditBox.TextArea.SelectionChanged += OnEditorSelectionChanged;
		contentText.EditBox.TextArea.Caret.PositionChanged += OnEditorCaretPositionChanged;
		contentText.SaveContent += TrySaveOnEnter;
		contentText.EscKeyUp += OnEscKeyUp;
	}

	private void RemoveContentEvent(MarkDownEditor contentText)
	{
		contentText.RequestBringIntoView -= OnBringIntoView;
		contentText.QuickPopupOpened -= OnTextPopupOpened;
		contentText.QuickItemSelected -= OnQuickItemSelected;
		contentText.LinkPopupOpened -= OnLinkPopupOpened;
		contentText.LinkPopupClosed -= OnLinkPopupClosed;
		contentText.CaretVerticalOffsetChanged -= OnContentVerticalOffsetChanged;
		contentText.GotFocus -= OnContentGotFocus;
		contentText.LostFocus -= OnContentLostFocus;
		contentText.Navigate -= OnNavigateTask;
		contentText.KeyDown -= OnContentKeyDown;
		contentText.MoveUp -= OnContentMoveUp;
		contentText.SizeChanged -= OnContentSizeChanged;
		contentText.EnterImmersive -= OnEnterImmersive;
		contentText.SelectDate -= OnSelectDate;
		contentText.TextChanged -= OnTaskContentChanged;
		contentText.EditorStateChanged -= OnEditorStateChanged;
		contentText.EditBox.PreviewTextInput -= OnContentInput;
		contentText.EditBox.TextArea.SelectionChanged -= OnEditorSelectionChanged;
		contentText.EditBox.TextArea.Caret.PositionChanged -= OnEditorCaretPositionChanged;
		contentText.SaveContent -= TrySaveOnEnter;
		contentText.EscKeyUp -= OnEscKeyUp;
	}

	private void OnEditorSelectionChanged(object sender, EventArgs e)
	{
		RefreshTextEditorMenuState();
	}

	private void OnEditorStateChanged(object sender, EventArgs e)
	{
		RefreshTextEditorMenuState();
	}

	private void OnEditorCaretPositionChanged(object sender, EventArgs e)
	{
		RefreshTextEditorMenuState();
	}

	private MarkDownEditor GetCurrentActiveEditor()
	{
		if (_contentText != null && _contentText.KeyboardFocused)
		{
			return _contentText;
		}
		if (_descText != null && _descText.KeyboardFocused)
		{
			return _descText;
		}
		return _contentText ?? _descText;
	}

	private void RetainCurrentEditorSelectionForToolbarInteraction()
	{
		GetCurrentActiveEditor()?.RetainLastSelectionForToolbarInteraction();
	}

	private void RefreshTextEditorMenuState()
	{
		if (_textEditorMenu != null)
		{
			MarkDownEditor currentActiveEditor = GetCurrentActiveEditor();
			if (currentActiveEditor == null)
			{
				_textEditorMenu.ApplyEditorState(string.Empty, null);
				return;
			}
			string currentParagraphStyle = currentActiveEditor.GetCurrentParagraphStyle();
			_textEditorMenu.ApplyEditorState(currentParagraphStyle, currentActiveEditor.GetCurrentEditorActions(), currentActiveEditor.GetCurrentHighlightContextColor());
		}
	}

	private void TrySaveOnEnter(object sender, EventArgs e)
	{
		OnEsc();
	}

	private void OnContentInput(object sender, TextCompositionEventArgs e)
	{
		SetContentHint("text");
	}

	private async void OnTaskContentChanged(object sender, EventArgs e)
	{
		if (e is MarkDownEditorTextChangedEventArgs e2)
		{
			await TrySavePendingTaskContentAsync(e2.TaskId);
		}
	}

	private async Task<bool> FlushPendingTaskContentBeforeNavigationAsync()
	{
		string currentTaskId = _task?.TaskId;
		if (string.IsNullOrEmpty(currentTaskId))
		{
			return true;
		}
		MarkDownEditor contentText;
		do
		{
			_contentText?.CancelScheduledTextChangedNotification();
			if (!(await TrySavePendingTaskContentAsync(currentTaskId)))
			{
				return false;
			}
			if (!string.Equals(_task?.TaskId, currentTaskId, StringComparison.Ordinal))
			{
				return false;
			}
			contentText = _contentText;
		}
		while (contentText != null && contentText.HasPendingLocalDocumentChanges);
		return true;
	}

	private async Task<bool> TrySavePendingTaskContentAsync(string taskId)
	{
		await _taskContentSaveLock.WaitAsync();
		try
		{
			MarkDownEditor editor = _contentText;
			TaskDetailViewModel task = _task;
			if (task == null || !string.Equals(taskId, task.TaskId, StringComparison.Ordinal))
			{
				return false;
			}
			if (editor == null || !editor.HasPendingLocalDocumentChanges)
			{
				return true;
			}
			if (editor.IsApplyingPreparedState || !editor.TryGetPersistedTextForSave(out var editorText))
			{
				return false;
			}
			string text = editorText;
			if (!string.IsNullOrEmpty(_guideLink) && !text.Contains("\n" + _guideLink))
			{
				text = text + "\n" + _guideLink;
			}
			BeginTaskContentSave(taskId);
			try
			{
				if (!string.Equals(text, task.TaskContent, StringComparison.Ordinal))
				{
					ClearSaveItems();
					task.SourceViewModel.Content = text;
					PublishPeerTaskTextChanged(TaskTextPart.Content, text);
					TaskChangeNotifier.NotifyTaskTextChanged(taskId);
				}
				await TryDelayEditSaveSync(task);
				if (!task.IsNewAdd && _contentText == editor && string.Equals(_task?.TaskId, taskId, StringComparison.Ordinal) && string.Equals(task.TaskContent, text, StringComparison.Ordinal) && string.Equals(editor.GetPersistedText(), editorText, StringComparison.Ordinal))
				{
					await AttachmentDao.CheckTaskAttachment(taskId, MarkDownEditor.GetReferencedInternalAttachmentIds(text), text, deactivateMissingAttachments: true);
				}
				if (_contentText == editor && string.Equals(_task?.TaskId, taskId, StringComparison.Ordinal))
				{
					editor.TryAcknowledgePendingLocalDocumentChanges(editorText);
				}
				return true;
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
				return false;
			}
			finally
			{
				EndTaskContentSave(taskId);
			}
		}
		finally
		{
			_taskContentSaveLock.Release();
		}
	}

	private static void BeginTaskContentSave(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return;
		}
		lock (TaskContentSaveStateLock)
		{
			TaskContentSaveCounts.TryGetValue(taskId, out var value);
			TaskContentSaveCounts[taskId] = value + 1;
		}
	}

	private static void EndTaskContentSave(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return;
		}
		lock (TaskContentSaveStateLock)
		{
			if (!TaskContentSaveCounts.TryGetValue(taskId, out var value) || value <= 1)
			{
				TaskContentSaveCounts.Remove(taskId);
			}
			else
			{
				TaskContentSaveCounts[taskId] = value - 1;
			}
		}
	}

	private static bool IsTaskContentSaveInProgress(string taskId)
	{
		lock (TaskContentSaveStateLock)
		{
			return !string.IsNullOrEmpty(taskId) && TaskContentSaveCounts.ContainsKey(taskId);
		}
	}

	private void ClearSaveItems()
	{
		_savedItems = (null, null);
	}

	private void OnEnterImmersive(object sender, EventArgs e)
	{
		EnterImmersivePage();
	}

	private void OnContentMoveUp(object sender, EventArgs e)
	{
		_titleText.FocusEnd();
	}

	private void OnContentKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			OnEsc();
		}
	}

	private void OnDescLostFocus(object sender, RoutedEventArgs e)
	{
		_descText?.UnRegisterInputHandler();
		SetContentHint(_task.Desc);
	}

	private void OnDescGotFocus(object sender, RoutedEventArgs e)
	{
		_descText?.RegisterInputHandler();
		SetContentHint(_task.Desc);
		RefreshTextEditorMenuState();
	}

	private void OnContentLostFocus(object sender, RoutedEventArgs e)
	{
		_contentText?.UnRegisterInputHandler();
		SetContentHint(_task.TaskContent);
	}

	private void OnContentGotFocus(object sender, RoutedEventArgs e)
	{
		_contentText?.RegisterInputHandler();
		SetContentHint(_task.TaskContent);
		RefreshTextEditorMenuState();
	}

	private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 10.0 && _attachmentOptionPanel.Visibility == Visibility.Visible)
		{
			HideAttachmentOption();
		}
	}

	private void OnDescMoveDown(object sender, EventArgs e)
	{
		_checklist?.FocusFirstItem();
	}

	private async void OnDescriptionChanged(object sender, EventArgs e)
	{
		if ((_descText?.IsApplyingPreparedState ?? false) || _descText == null)
		{
			return;
		}
		string text = _descText.GetPersistedText();
		string value = _descText.Text ?? string.Empty;
		if ((!string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value)) && !(_task.Desc == text))
		{
			_task.SourceViewModel.Desc = text;
			_switchListOriginText = null;
			PublishPeerTaskTextChanged(TaskTextPart.Description, text);
			TaskChangeNotifier.NotifyTaskTextChanged(_task.TaskId);
			await TryDelayEditSaveSync();
			if (text.Length == 2048)
			{
				TryToast(Utils.GetString("DescriptionLengthError"));
			}
		}
	}

	private void SetTextEditorMenu(bool show)
	{
		if (show)
		{
			if (_textEditorMenu == null)
			{
				_textEditorMenu = new EditorMenu
				{
					UseTaskDetailToolbarTracking = true,
					HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Bottom,
					Background = Brushes.Transparent
				};
				_textEditorMenu.SetValue(Grid.RowProperty, 1);
				_textEditorMenu.SetValue(Grid.RowSpanProperty, 2);
				_textEditorMenu.EditorAction += OnEditorAction;
				_textEditorMenu.HighlightColorRequested += OnHighlightColorRequested;
				_textEditorMenu.EnterImmersive += OnImmersiveClick;
				_textEditorMenu.ExitImmersive += OnExitImmersiveClick;
				_textEditorMenu.GetCurrentStyle = () => GetCurrentActiveEditor()?.GetCurrentParagraphStyle() ?? string.Empty;
				base.Children.Add(_textEditorMenu);
				RefreshTextEditorMenuState();
			}
		}
		else if (_textEditorMenu != null)
		{
			_textEditorMenu.EditorAction -= OnEditorAction;
			_textEditorMenu.HighlightColorRequested -= OnHighlightColorRequested;
			_textEditorMenu.EnterImmersive -= OnImmersiveClick;
			_textEditorMenu.ExitImmersive -= OnExitImmersiveClick;
			_textEditorMenu.MouseEnter -= OnMenuMouseEnter;
			_textEditorMenu.MouseLeave -= OnMenuMouseLeave;
			_textEditorMenu.GetCurrentStyle = () => GetCurrentActiveEditor()?.GetCurrentParagraphStyle() ?? string.Empty;
			base.Children.Remove(_textEditorMenu);
			_textEditorMenu = null;
		}
		_scrollContent.Margin = GetScrollContentMargin(Mode, show);
		UpdateSelectionViewportBottomInset();
		_detailBottom?.SetItemVisible("EditIconSelectedBorder", show);
	}

	private void UpdateSelectionViewportBottomInset()
	{
		double selectionViewportBottomInset = ((Mode == Constants.DetailMode.Editor || _textEditorMenu != null) ? 56.0 : 0.0);
		if (_descText != null)
		{
			_descText.SelectionViewportBottomInset = selectionViewportBottomInset;
		}
		if (_contentText != null)
		{
			_contentText.SelectionViewportBottomInset = selectionViewportBottomInset;
		}
	}

	private void OnExitImmersiveClick(object sender, EventArgs e)
	{
		this.ExitImmersive?.Invoke(this, null);
	}

	private void OnImmersiveClick(object sender, EventArgs e)
	{
		EnterImmersivePage();
	}

	private async void OnStyleClick(object sender, string tag)
	{
		MarkDownEditor markDownEditor = ((_task.Kind == "CHECKLIST") ? _descText : _contentText);
		if (markDownEditor != null)
		{
			if (markDownEditor.StyleMenu.FromSplash)
			{
				markDownEditor.RemoveQuickSetText();
			}
			SetContentStyle(tag);
		}
	}

	private async void OnEditorAction(object sender, string tag)
	{
		SetContentStyle(tag);
	}

	private void OnHighlightColorRequested(object sender, ColorExtra color)
	{
		SetHighlightColor(color);
	}

	public async void SetHighlightColor(ColorExtra color)
	{
		MarkDownEditor markDownEditor = ((_task.Kind == "CHECKLIST") ? _descText : _contentText);
		if (markDownEditor != null && color != null)
		{
			markDownEditor.HighlightEx(color.Background, color.Color);
			await FocusContentEditor(markDownEditor);
		}
	}

	public async void SetContentStyle(string tag)
	{
		MarkDownEditor markDownEditor = ((_task.Kind == "CHECKLIST") ? _descText : _contentText);
		if (markDownEditor != null)
		{
			string[] array = tag.Split('_');
			string text = string.Empty;
			if (array.Length > 1)
			{
				tag = array[0];
				text = array[1];
			}
			bool flag = true;
			switch (tag)
			{
			case "Text":
				markDownEditor.RemoveStartText();
				break;
			case "Heading1":
				markDownEditor.InsertHeader(1);
				break;
			case "Heading2":
				markDownEditor.InsertHeader(2);
				break;
			case "Heading3":
				markDownEditor.InsertHeader(3);
				break;
			case "Heading4":
				markDownEditor.InsertHeader(4);
				break;
			case "Heading5":
				markDownEditor.InsertHeader(5);
				break;
			case "Heading6":
				markDownEditor.InsertHeader(6);
				break;
			case "Bold":
				markDownEditor.Bold();
				break;
			case "Italic":
				markDownEditor.Italic();
				break;
			case "UnderLine":
				markDownEditor.UnderLine();
				break;
			case "HighLight":
				markDownEditor.Highlight();
				break;
			case "StrikeThrough":
				markDownEditor.StrokeLine();
				break;
			case "DateTime":
				markDownEditor.InsertTextAtCurrentCaretAndFocus(text);
				break;
			case "Quote":
				markDownEditor.Quote();
				break;
			case "BulletList":
				markDownEditor.InsertUnOrderList();
				break;
			case "NumberedList":
				markDownEditor.InsertNumberedList();
				break;
			case "CheckItem":
				markDownEditor.InsertCheckItem();
				break;
			case "Link":
				markDownEditor.ShowInsertLinkForCurrentContext();
				break;
			case "SplitLine":
				markDownEditor.InsertLine();
				break;
			case "CodeBlock":
				markDownEditor.InsertCodeBlock();
				break;
			case "Code":
				markDownEditor.Code();
				break;
			case "Attachment":
				ShowUploadFileDialog();
				break;
			case "Tag":
				markDownEditor.TryDismissStyleMenu();
				markDownEditor.InsertTag();
				break;
			case "Template":
				OnAddTemplateClick();
				break;
			case "Subtask":
				markDownEditor.TryDismissEditorMenu();
				markDownEditor.TryDismissStyleMenu();
				flag = false;
				AddSubTaskClick(null, null);
				markDownEditor.InvalidMargin();
				break;
			case "LinkTask":
				markDownEditor.TryDismissStyleMenu();
				markDownEditor.InsertLinkTask();
				break;
			}
			if (flag)
			{
				await FocusContentEditor(markDownEditor);
			}
		}
	}

	private async Task FocusContentEditor(MarkDownEditor editor)
	{
		await Task.Delay(100);
		editor.EditBox.TryFocus();
		editor.UpdateEditorMenuContext();
		RefreshTextEditorMenuState();
	}

	private void OnCheckItemsDeleted(object sender, TaskDetailItemModel e)
	{
		this.CheckItemsDeleted?.Invoke(sender, e);
	}

	private void OnListCaretMoveUp(object sender, EventArgs e)
	{
		_descText?.FocusEnd();
	}

	private void TryToastUnableText(object sender, string e)
	{
		if (string.IsNullOrEmpty(e))
		{
			CheckEnable();
		}
		else
		{
			TryToast(e);
		}
	}

	private async void OnItemsEmpty(object sender, EventArgs e)
	{
		if (_task.IsNewAdd)
		{
			await SwitchToText();
			return;
		}
		Reload(_task.TaskId);
		await Task.Delay(500);
		_contentText?.FocusEnd();
	}

	private async void ChecklistModified(string taskid, string itemid, CheckItemModifyType type)
	{
		_switchListOriginText = null;
		if (type != CheckItemModifyType.SetTime)
		{
			await SyncTaskProgress();
		}
		switch (type)
		{
		case CheckItemModifyType.Add:
		case CheckItemModifyType.Uncheck:
			if (_task.Status != 0)
			{
				await TaskService.SetTaskStatus(_task.TaskId, 0);
			}
			break;
		case CheckItemModifyType.Check:
			if (_task.Progress == 100 && _task.Status == 0 && TaskCache.CanTaskCompletedByCheckItem(_task.TaskId))
			{
				if ((await TaskService.SetTaskStatus(_task.TaskId, 2)).RepeatTask != null)
				{
					await Navigate(_task.TaskId);
				}
				if (Mode == Constants.DetailMode.Sticky && _task.Status != 0)
				{
					TryToast(Utils.GetString("TaskCompleted"));
				}
			}
			break;
		}
		if (type != CheckItemModifyType.TitleChange)
		{
			SetProgress(_task.Progress.GetValueOrDefault(), withAnim: true);
		}
	}

	private async Task SyncTaskProgress()
	{
		int progress = await TaskService.CalculateProgress(_task.TaskId);
		_task.SourceViewModel.Progress = progress;
		await SaveTask();
	}

	private void SetContentHint(string text)
	{
		if (Mode == Constants.DetailMode.Sticky)
		{
			return;
		}
		if (string.IsNullOrEmpty(text))
		{
			if (_hintTextPanel == null)
			{
				_hintTextPanel = new StackPanel
				{
					Orientation = System.Windows.Controls.Orientation.Horizontal
				};
				TextBlock textBlock = new TextBlock
				{
					IsHitTestVisible = false
				};
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
				textBlock.SetResourceReference(TextBlock.FontSizeProperty, (Mode == Constants.DetailMode.Sticky) ? "StickyFont13" : "Font14");
				_contentGrid.Children.Add(_hintTextPanel);
				_hintTextPanel.Children.Add(textBlock);
				_hintTextPanel.SetValue(System.Windows.Controls.Panel.ZIndexProperty, 10);
			}
			TextBlock textBlock2 = (TextBlock)_hintTextPanel.Children[0];
			switch (_task.Kind)
			{
			case "TEXT":
				textBlock2.Text = ((_contentText != null && _contentText.KeyboardFocused && _task.Enable) ? Utils.GetString("enter_content_or_type_splash_for_menu") : ((_subTaskList != null) ? Utils.GetString("Description") : ""));
				if (_hintTextPanel.Children.Count >= 2)
				{
					_hintTextPanel.Children.RemoveRange(1, _hintTextPanel.Children.Count - 1);
				}
				break;
			case "CHECKLIST":
				textBlock2.Text = Utils.GetString((_descText != null && _descText.KeyboardFocused && _task.Enable) ? "enter_content_or_type_splash_for_menu" : "Description");
				if (_hintTextPanel.Children.Count >= 2)
				{
					_hintTextPanel.Children.RemoveRange(1, _hintTextPanel.Children.Count - 1);
				}
				break;
			case "NOTE":
			{
				textBlock2.Text = Utils.GetString("NoteDescribe") + " ";
				TextBlock textBlock3;
				if (_hintTextPanel.Children.Count == 2)
				{
					textBlock3 = (TextBlock)_hintTextPanel.Children[1];
				}
				else
				{
					textBlock3 = new TextBlock
					{
						Text = Utils.GetString("UseATemplate"),
						TextDecorations = TextDecorations.Underline,
						Background = Brushes.Transparent
					};
					textBlock3.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
					textBlock3.SetResourceReference(TextBlock.FontSizeProperty, (Mode == Constants.DetailMode.Sticky) ? "StickyFont13" : "Font14");
					textBlock3.MouseLeftButtonUp += ShowNoteTemplate;
					_hintTextPanel.Children.Add(textBlock3);
				}
				textBlock3.Cursor = (_task.Enable ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.No);
				break;
			}
			}
			_hintTextPanel.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
		}
		else if (_hintTextPanel != null)
		{
			_contentGrid.Children.Remove(_hintTextPanel);
			_hintTextPanel = null;
		}
	}

	private void ShowNoteTemplate(object sender, MouseButtonEventArgs e)
	{
		if (CheckEnable())
		{
			OnShowDialog();
			AddTemplateDialog addTemplateDialog = new AddTemplateDialog(TemplateKind.Note, new AddTaskViewModel
			{
				ProjectId = _task.ProjectId
			});
			addTemplateDialog.Owner = Window.GetWindow(this);
			addTemplateDialog.TemplateSelected -= OnTemplateSelected;
			addTemplateDialog.TemplateSelected += OnTemplateSelected;
			addTemplateDialog.ShowDialog();
			addTemplateDialog.Activate();
			addTemplateDialog.Topmost = true;
			OnCloseDialog();
		}
	}

	private async void OnTemplateSelected(object sender, string e)
	{
		if (sender is AddTemplateDialog addTemplateDialog)
		{
			addTemplateDialog.Close();
		}
		TaskBaseViewModel note = TaskCache.GetTaskById(_task?.TaskId);
		TaskTemplateModel template = await TemplateDao.GetTemplateById(e);
		if (template == null || !template.IsNote || _task == null || !(_task.Kind == "NOTE"))
		{
			return;
		}
		if (string.IsNullOrEmpty(_titleText.Text.Trim()))
		{
			_task.SourceViewModel.Title = template.Title;
			_titleText.ResetText(template.Title);
		}
		string text = MergeTemplateContent(_contentText?.GetPersistedText() ?? _task.TaskContent, template.Content);
		_task.SourceViewModel.Content = text;
		_contentText?.SetText(text);
		await PersistAppliedTemplateAsync();
		if (note == null)
		{
			return;
		}
		List<string> tags = template.Tags;
		if (tags == null || tags.Count <= 0)
		{
			return;
		}
		List<string> tags2 = TagSerializer.ToTags(note.Tag);
		tags2 = TagSerializer.NormalizeLabels(tags2.Concat(template.Tags));
		if (tags2.Any())
		{
			List<string> localTags = (from tag in CacheManager.GetTags()
				select tag.name).ToList();
			List<string> list = tags2.Where((string tag) => !localTags.Contains(tag.ToLower())).ToList();
			if (list.Any())
			{
				foreach (string item in list)
				{
					await TagService.TryCreateTag(item);
				}
			}
			NotifyTagsChanged(tags2);
		}
		await TaskService.SetTags(note.Id, tags2);
	}

	public async Task OnUseTemplateClick()
	{
		SkipNextPopupClose();
		bool num = _task?.Kind == "NOTE";
		AddTemplateDialog addTemplateDialog = new AddTemplateDialog(num ? TemplateKind.Note : TemplateKind.Task, new AddTaskViewModel
		{
			ProjectId = _task.ProjectId
		})
		{
			Owner = App.Window,
			Topmost = true
		};
		if (num)
		{
			addTemplateDialog.TemplateSelected -= OnTemplateSelected;
			addTemplateDialog.TemplateSelected += OnTemplateSelected;
		}
		else
		{
			addTemplateDialog.TemplateSelected -= OnTaskTemplateSelected;
			addTemplateDialog.TemplateSelected += OnTaskTemplateSelected;
		}
		addTemplateDialog.ShowDialog();
		OnCloseDialog();
	}

	private async void OnTaskTemplateSelected(object sender, string templateId)
	{
		TaskTemplateModel template = await TemplateDao.GetTemplateById(templateId);
		if (template == null || _task == null)
		{
			return;
		}
		if (_task.IsNewAdd)
		{
			_task.SetPendingTemplate(template);
		}
		bool num = template.Items != null && template.Items.Any();
		if (string.IsNullOrEmpty(_titleText.Text.Trim()))
		{
			_task.SourceViewModel.Title = template.Title;
			_titleText.ResetText(template.Title);
		}
		string currentContent = ((_task.Kind == "CHECKLIST") ? BuildChecklistTextContent() : (_contentText?.GetPersistedText() ?? _task.TaskContent));
		if (num)
		{
			string text = MergeTemplateContent(currentContent, await GetTemplateContentWithCurrentAttachments(template.Desc));
			List<TaskDetailItemModel> list = ChecklistUtils.BuildChecklist(_task.Id, _task.TaskId, template.Items);
			List<CheckItemViewModel> data = CheckItemViewModel.BuildListFromModels(list, _task);
			if (_task.Kind != "CHECKLIST")
			{
				_task.SourceViewModel.Kind = "CHECKLIST";
				_task.SourceViewModel.Content = string.Empty;
				LoadContentControl(restoreOffset: false);
			}
			_task.SourceViewModel.Desc = text;
			_task.Items = list.ToArray();
			_descText?.SetText(text);
			_checklist?.SetTaskId(_task.TaskId);
			if (_checklist != null)
			{
				await _checklist.SetData(data);
			}
		}
		else
		{
			string text2 = MergeTemplateContent(currentContent, await GetTemplateContentWithCurrentAttachments(template.Content));
			if (_task.Kind == "CHECKLIST")
			{
				_task.SourceViewModel.Desc = string.Empty;
				_task.Items = null;
				_task.SourceViewModel.Kind = "TEXT";
				LoadContentControl(restoreOffset: false);
			}
			_task.SourceViewModel.Content = text2;
			_contentText?.SetText(text2);
		}
		await SetSubtasks(_task, restoreSelected: false);
		await PersistAppliedTemplateAsync();
		List<string> tags = template.Tags;
		if (tags == null || tags.Count <= 0)
		{
			return;
		}
		List<string> tags2 = TagSerializer.ToTags(_task.Tag);
		tags2 = TagSerializer.NormalizeLabels(tags2.Concat(template.Tags));
		if (tags2.Any())
		{
			List<string> localTags = (from tag in CacheManager.GetTags()
				select tag.name).ToList();
			List<string> list2 = tags2.Where((string tag) => !localTags.Contains(tag.ToLower())).ToList();
			if (list2.Any())
			{
				foreach (string item in list2)
				{
					await TagService.TryCreateTag(item);
				}
			}
			NotifyTagsChanged(tags2);
		}
		await TaskService.SetTags(_task.TaskId, tags2);
	}

	private async Task<string> GetTemplateContentWithCurrentAttachments(string templateContent)
	{
		if (_task == null || _task.Kind == "CHECKLIST")
		{
			return templateContent;
		}
		List<AttachmentModel> list = await AttachmentDao.GetTaskAttachments(_task.TaskId, active: true);
		if (list == null || list.Count == 0)
		{
			return templateContent;
		}
		foreach (AttachmentModel item in list)
		{
			AttachmentCache.SetAttachment(item);
		}
		return AttachmentDao.AddAttachmentStrings(templateContent ?? string.Empty, list);
	}

	private async Task PersistAppliedTemplateAsync()
	{
		if (_task != null && !_task.IsNewAdd && !string.IsNullOrEmpty(_task.TaskId))
		{
			await TryDelayEditSaveSync();
		}
	}

	public static string MergeTemplateContent(string currentContent, string templateContent)
	{
		string text = NormalizeTemplateContent(currentContent).Trim();
		string text2 = NormalizeTemplateContent(templateContent).Trim();
		if (string.IsNullOrEmpty(text))
		{
			return text2;
		}
		if (string.IsNullOrEmpty(text2))
		{
			return text;
		}
		return text + "\r\n" + text2;
	}

	private static string NormalizeTemplateContent(string content)
	{
		return (content ?? string.Empty).Replace("\r\n", "\r").Replace("\r", "\r\n");
	}

	private void HandleAttachmentMouseEnter(object sender)
	{
		if (sender is Border bd)
		{
			OnAttachmentMouseEnter(bd);
		}
	}

	public async void OnAttachmentMouseEnter(Border bd)
	{
		if (bd is AttachmentFileDisplayControl attachmentFileDisplayControl)
		{
			attachmentFileDisplayControl.HideAIAudioHint();
		}
		int num = ((Mode == Constants.DetailMode.Sticky) ? 2 : 5);
		if (bd.Tag is AttachmentMDDisplayModel model)
		{
			Point point = bd.TranslatePoint(new Point(0.0, 0.0), _contentGrid);
			_attachmentOptionPanel.Margin = new Thickness(point.X, point.Y + (double)num, 0.0, 0.0);
			_attachmentOptionPanel.Visibility = Visibility.Visible;
			_attachmentOptionPanel.SetPanel(model, bd.Width, bd.ActualHeight, _task.Enable, _task.ImageMode);
			_attachmentOptionPanel.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(null, 1.0, 150));
		}
		else if (bd.Tag is AttachmentViewModel model2)
		{
			Point point2 = bd.TranslatePoint(new Point(0.0, 0.0), _contentGrid);
			_attachmentOptionPanel.Margin = new Thickness(point2.X, point2.Y + (double)num, 0.0, 0.0);
			_attachmentOptionPanel.Visibility = Visibility.Visible;
			_attachmentOptionPanel.SetPanel(model2, bd.Width, bd.ActualHeight, _task.Enable, _task.ImageMode);
			_attachmentOptionPanel.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(null, 1.0, 150));
		}
	}

	private async void OnImageModeChanged(object sender, int mode)
	{
		if (_task != null && CheckEnable())
		{
			await TaskService.SetImageMode(_task.TaskId, mode);
		}
	}

	private async void OnCheckListAttachmentDeleteClick(object sender, AttachmentModel model)
	{
		if (model != null)
		{
			if (!_task.IsOwner)
			{
				TryToast(Utils.GetString("AttendeeModifyContent"));
				return;
			}
			UtilLog.Info("TaskDetail.DeleteAttachment taskId " + _task?.TaskId + ", attachment " + model.id + ", from: checkList");
			await AttachmentDao.FakeDeleteAttachment(model.id);
			await AttachmentDao.SetAttachemntActive(_task.ProjectId, _task.TaskId, model.id, 1);
			TaskChangeNotifier.NotifyAttachmentChanged(_task.TaskId, this);
			TaskDetailViewModel task = _task;
			task.Attachments = (await AttachmentDao.GetTaskAttachments(_task.TaskId)).ToArray();
			await SyncStatusDao.AddSyncStatus(_task.TaskId, 0);
			LoadAttachmentAsync();
		}
	}

	private void OnAttachmentDeleteClick(object sender, AttachmentInfo info)
	{
		UtilLog.Info("TaskDetail.DeleteAttachment taskId " + _task?.TaskId + ", attachment " + info.Url + ", from: OptionPanel");
		_contentText?.TryDeleteAttachment(info.Offset);
	}

	private void OnAttachmentMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		HideAttachmentOption();
	}

	private async Task InitInputViews(TaskDetailViewModel task, bool restoreOffset, bool sameTask)
	{
		LoadContentControl(restoreOffset, sameTask);
		LoadTaskTitle(restoreOffset, sameTask);
		await CheckTaskAttachmentText(task);
		_detailBottom?.SetItemVisible("CommentGrid", !_task.IsNewAdd);
		if (Mode == Constants.DetailMode.Sticky)
		{
			RefreshEditorVisualLayersAfterLayout();
		}
	}

	private async Task CheckTaskAttachmentText(TaskDetailViewModel task)
	{
		if (task.Kind == "CHECKLIST")
		{
			return;
		}
		string taskId = task.TaskId;
		TaskModel taskModel = await TaskDao.GetTaskById(taskId);
		if (taskModel == null)
		{
			return;
		}
		string content = taskModel.content ?? string.Empty;
		IReadOnlyCollection<string> referencedAttachmentIds = MarkDownEditor.GetReferencedInternalAttachmentIds(content);
		await AttachmentDao.CheckTaskAttachment(taskModel.id, referencedAttachmentIds, content, deactivateMissingAttachments: false);
		List<AttachmentModel> attachments = (await AttachmentDao.GetTaskAttachments(taskModel.id)) ?? new List<AttachmentModel>();
		foreach (AttachmentModel item in attachments)
		{
			AttachmentCache.SetAttachment(item);
		}
		bool hasAttachments = attachments.Count > 0;
		HashSet<string> referencedAttachmentIdSet = new HashSet<string>(referencedAttachmentIds, StringComparer.OrdinalIgnoreCase);
		bool flag = attachments.Where((AttachmentModel attachment) => attachment.IsActive() && !referencedAttachmentIdSet.Contains(attachment.id) && !content.Contains(attachment.id)).ToList().Count > 0;
		if (flag)
		{
			flag = await CanMigrateRelationOnlyAttachments(taskId, content);
		}
		if (flag)
		{
			TaskModel latestTaskModel = await TaskDao.GetTaskById(taskId);
			if (latestTaskModel != null && !IsTaskContentSaveInProgress(taskId) && !HasPendingEditorContentForTask(taskId) && string.Equals(latestTaskModel.content ?? string.Empty, content, StringComparison.Ordinal))
			{
				List<AttachmentModel> source = (await AttachmentDao.GetTaskAttachments(taskId)) ?? new List<AttachmentModel>();
				HashSet<string> latestReferencedAttachmentIds = new HashSet<string>(MarkDownEditor.GetReferencedInternalAttachmentIds(latestTaskModel.content), StringComparer.OrdinalIgnoreCase);
				List<AttachmentModel> list = source.Where((AttachmentModel attachment) => attachment.IsActive() && !latestReferencedAttachmentIds.Contains(attachment.id) && !(latestTaskModel.content ?? string.Empty).Contains(attachment.id)).ToList();
				if (list.Count > 0 && !IsTaskContentSaveInProgress(taskId) && !HasPendingEditorContentForTask(taskId))
				{
					string expectedContent = latestTaskModel.content ?? string.Empty;
					string migratedContent = AttachmentDao.AddAttachmentStrings(expectedContent, list);
					if (await TaskDao.TryUpdateTaskContentIfUnchanged(taskId, expectedContent, migratedContent))
					{
						taskModel = latestTaskModel;
						taskModel.content = migratedContent;
						TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
						if (taskById != null && string.Equals(taskById.Content ?? string.Empty, expectedContent, StringComparison.Ordinal))
						{
							taskById.Content = migratedContent;
						}
						await SyncStatusDao.AddModifySyncStatus(taskModel.id);
						SyncManager.TryDelaySync();
					}
				}
			}
		}
		if (_task?.TaskId == task.TaskId)
		{
			if (hasAttachments)
			{
				_contentText?.RedrawImmediately();
			}
			else
			{
				_contentText?.Redraw();
			}
		}
		HashSet<string> localAttachmentIds = new HashSet<string>(attachments.Select((AttachmentModel attachment) => attachment.id), StringComparer.OrdinalIgnoreCase);
		bool flag2 = (await TaskService.RepairMissingTaskAttachments(taskModel, referencedAttachmentIds, attachments)).Any((AttachmentModel attachment) => !localAttachmentIds.Contains(attachment.id));
		if (_task?.TaskId == task.TaskId && flag2)
		{
			_contentText?.RedrawImmediately();
		}
	}

	private async Task<bool> CanMigrateRelationOnlyAttachments(string taskId, string persistedContent)
	{
		if (IsTaskContentSaveInProgress(taskId) || HasPendingEditorContentForTask(taskId))
		{
			return false;
		}
		if (((await SyncStatusDao.GetSyncStatusById(taskId))?.Any((SyncStatusModel status) => status.Type == 0 || status.Type == 4) ?? false) || IsTaskContentSaveInProgress(taskId))
		{
			return false;
		}
		TaskModel taskModel = await TaskDao.GetTaskById(taskId);
		return taskModel != null && !IsTaskContentSaveInProgress(taskId) && !HasPendingEditorContentForTask(taskId) && string.Equals(taskModel.content ?? string.Empty, persistedContent ?? string.Empty, StringComparison.Ordinal);
	}

	private bool HasPendingEditorContentForTask(string taskId)
	{
		if (string.Equals(_task?.TaskId, taskId, StringComparison.Ordinal))
		{
			return _contentText?.HasPendingLocalDocumentChanges ?? false;
		}
		return false;
	}

	public void TryFocusDetail(bool focusEnd = true, string taskId = null)
	{
		if (string.IsNullOrEmpty(taskId) || !(taskId != _task.TaskId))
		{
			if (_contentText != null)
			{
				_contentText.FocusEditBox();
			}
			else if (focusEnd)
			{
				_descText?.FocusEnd();
			}
			else
			{
				_descText?.FocusFirst();
			}
		}
	}

	public List<CheckItemViewModel> GetChecklistItems()
	{
		return _checklist?.ChecklistItems?.ToList();
	}

	public ChecklistControl GetChecklist()
	{
		return _checklist;
	}

	public MarkDownEditor GetContentText()
	{
		return _contentText;
	}

	public MarkDownEditor GetDescText()
	{
		return _descText;
	}

	public void CopyEditorStateFrom(TaskDetailView source)
	{
		if (source?._task != null && _task != null && string.Equals(source._task.TaskId, _task.TaskId, StringComparison.Ordinal))
		{
			if (_contentText != null && source._contentText != null)
			{
				_task.SourceViewModel.Content = source._task.TaskContent;
				_contentText.SetExternalTextAndCaretOffset(source._contentText.GetPersistedText(), source._contentText.EditBox.CaretOffset);
				SetContentHint(_task.TaskContent);
			}
			else if (_descText != null && source._descText != null)
			{
				_task.SourceViewModel.Desc = source._task.Desc;
				_descText.SetExternalTextAndCaretOffset(source._descText.GetPersistedText(), source._descText.EditBox.CaretOffset);
				SetContentHint(_task.Desc);
			}
		}
	}

	public void InvalidContentMargin()
	{
		_contentText?.InvalidMargin();
	}

	public void RefreshEditorVisualLayersAfterLayout()
	{
		_contentText?.RefreshVisualLayersAfterLayout();
		_descText?.RefreshVisualLayersAfterLayout();
	}

	private void BindEvents()
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		PeerTaskTextChanged += OnPeerTaskTextChanged;
		DataChangedNotifier.ThemeModeChanged += ReloadOnThemeChanged;
		DataChangedNotifier.HideCompleteChanged += TryReloadSubTasks;
		DataChangedNotifier.PomoChanged += OnPomoChanged;
		DataChangedNotifier.TagChanged += OnTagModelChanged;
		DataChangedNotifier.TaskGoogleEventInfoChanged += OnTaskGoogleEventInfoChanged;
		AttachmentLoadHelper.Downloaded += OnAttachmentLoadChanged;
		AttachmentLoadHelper.DownloadFailed += OnAttachmentLoadChanged;
		AttachmentLoadHelper.Uploaded += OnAttachmentLoadChanged;
		AttachmentLoadHelper.UploadFailed += OnAttachmentLoadChanged;
		LinkParentTaskMenu.EditorAction += LinkParentTaskMenu_EditorAction;
		BindLinkParentTaskPopupOwnerEvents();
	}

	private void UnbindEvents()
	{
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		PeerTaskTextChanged -= OnPeerTaskTextChanged;
		DataChangedNotifier.ThemeModeChanged -= ReloadOnThemeChanged;
		DataChangedNotifier.HideCompleteChanged -= TryReloadSubTasks;
		DataChangedNotifier.PomoChanged -= OnPomoChanged;
		DataChangedNotifier.TagChanged -= OnTagModelChanged;
		DataChangedNotifier.TaskGoogleEventInfoChanged -= OnTaskGoogleEventInfoChanged;
		AttachmentLoadHelper.Downloaded -= OnAttachmentLoadChanged;
		AttachmentLoadHelper.DownloadFailed -= OnAttachmentLoadChanged;
		AttachmentLoadHelper.Uploaded -= OnAttachmentLoadChanged;
		AttachmentLoadHelper.UploadFailed -= OnAttachmentLoadChanged;
		LinkParentTaskMenu.EditorAction -= LinkParentTaskMenu_EditorAction;
		HideLinkParentTaskPopup();
		UnbindLinkParentTaskPopupOwnerEvents();
	}

	private void HideLinkParentTaskPopup()
	{
		if (_linkParentTaskPopup != null)
		{
			_linkParentTaskPopup.IsOpen = false;
		}
	}

	private void OnParentWindowDeactivated(object sender, EventArgs e)
	{
		HideLinkParentTaskPopup();
	}

	private void OnParentWindowStateChanged(object sender, EventArgs e)
	{
		HideLinkParentTaskPopup();
	}

	private void OnParentWindowLocationChanged(object sender, EventArgs e)
	{
		HideLinkParentTaskPopup();
	}

	private void OnParentWindowClosing(object sender, CancelEventArgs e)
	{
		HideLinkParentTaskPopup();
	}

	private void OnDetailVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!base.IsVisible)
		{
			HideLinkParentTaskPopup();
		}
	}

	private void BindLinkParentTaskPopupOwnerEvents()
	{
		base.IsVisibleChanged -= OnDetailVisibleChanged;
		base.IsVisibleChanged += OnDetailVisibleChanged;
		Window window = Window.GetWindow(this);
		if (_linkParentTaskPopupOwnerWindow != window)
		{
			UnbindLinkParentTaskPopupOwnerEvents();
			_linkParentTaskPopupOwnerWindow = window;
			if (_linkParentTaskPopupOwnerWindow != null)
			{
				_linkParentTaskPopupOwnerWindow.Deactivated += OnParentWindowDeactivated;
				_linkParentTaskPopupOwnerWindow.StateChanged += OnParentWindowStateChanged;
				_linkParentTaskPopupOwnerWindow.LocationChanged += OnParentWindowLocationChanged;
				_linkParentTaskPopupOwnerWindow.Closing += OnParentWindowClosing;
			}
		}
	}

	private void UnbindLinkParentTaskPopupOwnerEvents()
	{
		base.IsVisibleChanged -= OnDetailVisibleChanged;
		if (_linkParentTaskPopupOwnerWindow != null)
		{
			_linkParentTaskPopupOwnerWindow.Deactivated -= OnParentWindowDeactivated;
			_linkParentTaskPopupOwnerWindow.StateChanged -= OnParentWindowStateChanged;
			_linkParentTaskPopupOwnerWindow.LocationChanged -= OnParentWindowLocationChanged;
			_linkParentTaskPopupOwnerWindow.Closing -= OnParentWindowClosing;
			_linkParentTaskPopupOwnerWindow = null;
		}
	}

	private void OnAttachmentLoadChanged(object sender, string e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (_task.Kind != "CHECKLIST")
			{
				_contentText?.Redraw();
				TryLoadLocalComments();
			}
		});
	}

	private void OnTagModelChanged(object sender, TagModel e)
	{
		TryDisplayTags();
	}

	private void OnPomoChanged(object sender, string e)
	{
		if (Mode != Constants.DetailMode.Sticky && !Immerse)
		{
			TryLoadPomoCount(_task);
		}
	}

	private void TryReloadSubTasks(object sender, EventArgs e)
	{
		SetSubtasks(_task);
	}

	private void ReloadOnThemeChanged(object sender, EventArgs e)
	{
		_task?.OnThemeChanged();
		SetSubtasks(_task);
	}

	private async void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		if (Mode != Constants.DetailMode.Sticky && !Immerse)
		{
			TryLoadPomoCount(_task);
		}
		if (e.BatchChangedIds.Any())
		{
			if (_task.TaskId != null && e.BatchChangedIds.Contains(_task.TaskId))
			{
				Reload(_task.TaskId);
			}
			else
			{
				OnTaskBatchChanged(e.BatchChangedIds.Value);
			}
			return;
		}
		if (e.DeletedChangedIds.Any())
		{
			if (KeepDetailOnDelete && _task.TaskId != null && e.DeletedChangedIds.Contains(_task.TaskId))
			{
				Reload(_task.TaskId);
			}
			OnTaskBatchDeleted(e.DeletedChangedIds.Value);
		}
		if (e.AttachmentChangedIds.Any() && _task.TaskId != null && e.AttachmentChangedIds.Contains(_task.TaskId))
		{
			LoadAttachmentAsync();
		}
		if (e.PinChangedIds.Any() && e.PinChangedIds.Contains(_task.TaskId))
		{
			SetStarGrid();
		}
		if (e.UndoDeletedIds.Any())
		{
			if (KeepDetailOnDelete && _task.TaskId != null && e.UndoDeletedIds.Contains(_task.TaskId))
			{
				Reload(_task.TaskId);
			}
			OnTaskBatchChanged(e.UndoDeletedIds.Value);
		}
		if (e.ProjectChangedIds.Any())
		{
			OnTaskBatchChanged(e.ProjectChangedIds.Value);
		}
		if (e.AddIds.Any())
		{
			OnTaskBatchChanged(e.AddIds.Value);
		}
		if (e.StatusChangedIds.Any())
		{
			OnTaskBatchChanged(e.StatusChangedIds.Value);
		}
		if (e.DateChangedIds.Any() && e.DateChangedIds.Contains(_task.TaskId))
		{
			List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(_task.TaskId);
			_task.Reminders = list.ToArray();
			_task.SetPropertyChanged("ShowSnoozeText");
			_task.SetPropertyChanged("RemindTimeText");
		}
		if (e.CheckItemChangedIds.Any() && _task.Kind == "CHECKLIST")
		{
			Reload(null, loadComment: false);
		}
		if (e.KindChangedIds.Any() && e.KindChangedIds.Contains(_task.TaskId))
		{
			Reload(null, loadComment: false);
		}
		if (e.SortOrderChangedIds.Any())
		{
			if (e.SortOrderChangedIds.Contains(_task.TaskId))
			{
				SetParentTitle(_task);
			}
			if ((_subTaskList != null && e.SortOrderChangedIds.ToList().Any((string id) => _subTaskList.ExistId(id))) || e.SortOrderChangedIds.ToList().Any((string id) => TaskCache.ExistParent(id, _task.TaskId)) || e.SortOrderChangedIds.Contains(_task.TaskId))
			{
				SetSubtasks(_task);
			}
		}
		if (e.ScrollToBottomIds.Any() && e.ScrollToBottomIds.Contains(_task.TaskId))
		{
			SctollToBottom();
		}
	}

	private void PublishPeerTaskTextChanged(TaskTextPart part, string text)
	{
		if (!string.IsNullOrEmpty(_task?.TaskId))
		{
			TaskDetailView.PeerTaskTextChanged?.Invoke(this, new PeerTaskTextChangedEventArgs(_task.TaskId, part, text));
		}
	}

	private void SetPendingImmersiveSource()
	{
		_pendingImmersiveSource = new WeakReference(this);
	}

	internal static TaskDetailView TakePendingImmersiveSource(string taskId)
	{
		TaskDetailView taskDetailView = _pendingImmersiveSource?.Target as TaskDetailView;
		_pendingImmersiveSource = null;
		if (!(taskDetailView?._task?.TaskId == taskId))
		{
			return null;
		}
		return taskDetailView;
	}

	private void OnPeerTaskTextChanged(object sender, PeerTaskTextChangedEventArgs e)
	{
		if (sender == this || !base.IsVisible || _task?.TaskId != e.TaskId)
		{
			return;
		}
		if (!base.Dispatcher.CheckAccess())
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				OnPeerTaskTextChanged(sender, e);
			});
		}
		else if (e.Part == TaskTextPart.Content && _task.Kind != "CHECKLIST")
		{
			MarkDownEditor contentText = _contentText;
			if (contentText != null && contentText.IsApplyingPreparedState)
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					OnPeerTaskTextChanged(sender, e);
				});
				return;
			}
			string text = e.Text;
			if (_guideTaskId == _task.TaskId && !string.IsNullOrEmpty(_guideLink) && text.Contains("\n" + _guideLink))
			{
				text = text.TrimEnd().Replace("\n" + _guideLink, "");
			}
			if (!ShouldPreservePendingLocalEditorState(_contentText, text, sameTask: true))
			{
				_task.SourceViewModel.Content = e.Text;
				if (_contentText != null && _contentText.GetPersistedText() != text)
				{
					_contentText.SetExternalTextAndOffset(text, restoreOffset: false);
				}
				SetContentHint(e.Text);
			}
		}
		else
		{
			if (e.Part != TaskTextPart.Description || !(_task.Kind == "CHECKLIST"))
			{
				return;
			}
			MarkDownEditor descText = _descText;
			if (descText != null && descText.IsApplyingPreparedState)
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					OnPeerTaskTextChanged(sender, e);
				});
			}
			else if (!ShouldPreservePendingLocalEditorState(_descText, e.Text, sameTask: true))
			{
				_task.SourceViewModel.Desc = e.Text;
				if (_descText != null && _descText.GetPersistedText() != e.Text)
				{
					_descText.SetExternalTextAndOffset(e.Text, restoreOffset: false);
				}
				SetContentHint(e.Text);
			}
		}
	}

	private void SetStarGrid(bool? isPin = null)
	{
		if (_task != null)
		{
			bool pin = isPin ?? _task.Pinned;
			_detailBottom?.SetPin(pin);
		}
	}

	private void OnTaskBatchDeleted(HashSet<string> ids)
	{
		if (!string.IsNullOrEmpty(_task?.TaskId))
		{
			List<DisplayItemModel> list = _subTaskList?.ViewModel.Items.ToList();
			if (list != null && list.Any((DisplayItemModel m) => ids.Contains(m.Id)))
			{
				SetSubtasks(_task);
			}
		}
	}

	private void OnTaskBatchChanged(IEnumerable<string> ids)
	{
		if (string.IsNullOrEmpty(_task?.TaskId))
		{
			return;
		}
		List<DisplayItemModel> list = _subTaskList?.ViewModel.Items.ToList();
		foreach (string id in ids)
		{
			TaskBaseViewModel task = TaskCache.GetTaskById(id);
			if (task != null)
			{
				if (id == _task.TaskId)
				{
					SetParentTitle(_task);
				}
				DisplayItemModel displayItemModel = list?.FirstOrDefault((DisplayItemModel m) => m.Id == id);
				if (displayItemModel != null && (string.IsNullOrEmpty(displayItemModel.ParentId) || displayItemModel.ProjectId != _task.ProjectId || displayItemModel.ColumnId != _task.ColumnId))
				{
					SetSubtasks(_task);
					break;
				}
				if (task.TaskRepeatId == _task.TaskId || ((list == null || list.All((DisplayItemModel m) => m.Id != task.Id)) && (task.ParentId == _task.TaskId || (list != null && list.Any((DisplayItemModel m) => m.Id == task.ParentId)))))
				{
					SetSubtasks(_task);
					break;
				}
			}
		}
	}

	private void OnTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		default:
			return;
		case 8:
			switch (propertyName[2])
			{
			default:
				return;
			case 'm':
				if (propertyName == "Comments")
				{
					OnCommentChanged();
				}
				return;
			case 's':
				if (propertyName == "Assignee")
				{
					TrySetAvatar();
				}
				return;
			case 'o':
				if (propertyName == "Progress")
				{
					OnTaskProgressChanged();
				}
				return;
			case 'i':
				break;
			}
			if (!(propertyName == "Priority"))
			{
				return;
			}
			break;
		case 9:
			switch (propertyName[0])
			{
			case 'P':
				if (propertyName == "ProjectId")
				{
					OnProjectIdChanged();
				}
				break;
			case 'I':
				if (propertyName == "ImageMode")
				{
					OnTaskImageModeChanged();
				}
				break;
			}
			return;
		case 5:
			if (propertyName == "Title")
			{
				OnTaskTitleChanged();
			}
			return;
		case 11:
			if (propertyName == "TaskContent")
			{
				OnTaskContentChanged();
			}
			return;
		case 4:
			if (propertyName == "Desc")
			{
				OnTaskDescChanged();
			}
			return;
		case 10:
			if (propertyName == "CheckItems")
			{
				OnCheckItemsChanged();
			}
			return;
		case 3:
			if (propertyName == "Tag")
			{
				OnTaskTagsChanged();
			}
			return;
		case 6:
			if (!(propertyName == "Status"))
			{
				return;
			}
			break;
		case 7:
			return;
		}
		_detailHead?.SetIconColor(_task.Priority, _task.Status);
		UpdateStatusIcon();
	}

	private void OnTaskTitleChanged()
	{
		if (!_titleText.KeyboardFocused && _titleText.Text != _task.Title)
		{
			_titleText.SetTextOffset(_task.Title, restoreOffset: false);
		}
		SetTitleHint(_task.Title);
	}

	private void OnTaskContentChanged()
	{
		if (_task.Kind != "CHECKLIST")
		{
			string text = _task.TaskContent ?? string.Empty;
			if (_guideTaskId == _task.TaskId && !string.IsNullOrEmpty(_guideLink) && text.Contains("\n" + _guideLink))
			{
				text = text.TrimEnd().Replace("\n" + _guideLink, "");
			}
			bool flag = ShouldPreservePendingLocalEditorState(_contentText, text, sameTask: true);
			if (_contentText != null && !flag && !_contentText.KeyboardFocused && !_contentText.IsApplyingPreparedState && _contentText.GetPersistedText() != text)
			{
				_contentText.SetExternalTextAndOffset(text, restoreOffset: false);
				ClearSaveItems();
			}
			SetContentHint(_task.TaskContent);
		}
	}

	private void OnTaskDescChanged()
	{
		if (_task.Kind == "CHECKLIST")
		{
			bool flag = ShouldPreservePendingLocalEditorState(_descText, _task.Desc, sameTask: true);
			if (_descText != null && !flag && !_descText.KeyboardFocused && !_descText.IsApplyingPreparedState && _descText.GetPersistedText() != _task.Desc)
			{
				_descText.SetExternalTextAndOffset(_task.Desc, restoreOffset: false);
			}
			SetContentHint(_task.Desc);
		}
	}

	private void OnCheckItemsChanged()
	{
		try
		{
			List<TaskBaseViewModel> list = _task.SourceViewModel?.CheckItems?.ToList();
			if (list == null || list.Count((TaskBaseViewModel i) => i.Deleted == 0) != (_checklist?.ChecklistItems?.Count).GetValueOrDefault())
			{
				_task.Items = null;
				LoadChecklistView(_task, null);
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			throw;
		}
	}

	private async void OnCommentChanged()
	{
		if (Mode == Constants.DetailMode.Sticky || Mode == Constants.DetailMode.Editor)
		{
			return;
		}
		if (_commentDisplayCtrl?.Model?.Count.ToString() != _task.CommentCount)
		{
			TryLoadLocalComments();
			return;
		}
		List<CommentModel> list = await CommentDao.GetCommentsByTaskId(_task.TaskId);
		if (list != null && list.Count > 0 && (_commentDisplayCtrl?.Model?.CheckComments(list) ?? true))
		{
			TryLoadLocalComments();
		}
	}

	private void OnTaskTagsChanged()
	{
		TryDisplayTags();
	}

	private void TrySetAvatar()
	{
		SetAvatar();
	}

	private void OnTaskImageModeChanged()
	{
		if (_task.Kind == "CHECKLIST")
		{
			LoadAttachmentAsync();
		}
	}

	private void OnTaskProgressChanged()
	{
		_detailHead?.CheckProgress();
	}

	private void OnProjectIdChanged()
	{
		TrySetAvatar();
		SetParentTitle(_task);
	}

	public void ClearEvents()
	{
		this.TaskNavigated = null;
		this.SubtaskDragOver = null;
		this.EscKeyUp = null;
		this.ShowUndoOnTaskDeleted = null;
		this.TaskDeleted = null;
		this.NotifyCloseWindow = null;
		this.EnterImmersive = null;
		this.ExitImmersive = null;
		this.TaskCopied = null;
		this.FocusList = null;
		this.NavigateBack = null;
		this.ForceHideWindow = null;
		this.SubTaskChanged = null;
		this.NavigateTask = null;
		this.CheckItemsDeleted = null;
		RecordLabelTime();
		if (_tagsControl != null)
		{
			_tagsControl.TagsChanged -= OnTagsChanged;
			_tagsControl.TagClick -= OnTagLabelClick;
			_tagsControl.TagPanelVisibleChanged -= OnTagPanelVisibleChanged;
			_contentGrid.Children.Remove(_tagsControl);
			_tagsControl = null;
		}
	}

	public void Dispose()
	{
		ClearEvents();
	}

	private async Task SetGoogleEventInfoAsync(string taskId)
	{
		if (Mode == Constants.DetailMode.Sticky || Mode == Constants.DetailMode.Editor || string.IsNullOrEmpty(taskId))
		{
			HideGoogleEventInfo();
			return;
		}
		TaskGoogleEventInfo taskGoogleEventInfo;
		try
		{
			taskGoogleEventInfo = await TaskGoogleEventInfoDao.GetByTaskIdAsync(taskId);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("LoadTaskGoogleEventInfo failed: " + ExceptionUtils.BuildExceptionMessage(ex));
			if (_task?.TaskId == taskId)
			{
				HideGoogleEventInfo();
			}
			return;
		}
		if (_task?.TaskId != taskId)
		{
			return;
		}
		if (taskGoogleEventInfo != null && taskGoogleEventInfo.HasDisplayContent())
		{
			if (_googleEventInfoView == null)
			{
				_googleEventInfoView = new TaskGoogleEventInfoPanel();
				_googleEventInfoView.SetValue(Grid.RowProperty, 3);
				_scrollContent.Children.Add(_googleEventInfoView);
			}
			_googleEventInfoView.SetModel(taskGoogleEventInfo);
			_googleEventInfoView.Visibility = Visibility.Visible;
		}
		else
		{
			HideGoogleEventInfo();
		}
	}

	private void HideGoogleEventInfo()
	{
		if (_googleEventInfoView != null)
		{
			_googleEventInfoView.SetModel(null);
			_googleEventInfoView.Visibility = Visibility.Collapsed;
		}
	}

	private async void OnTaskGoogleEventInfoChanged(object sender, EventArgs eventArgs)
	{
		string text = _task?.TaskId;
		if (!string.IsNullOrEmpty(text))
		{
			await SetGoogleEventInfoAsync(text);
		}
	}

	private async Task SetSubtasks(TaskDetailViewModel taskModel, bool restoreSelected = true)
	{
		int requestVersion = Interlocked.Increment(ref _subtaskRequestVersion);
		await _subTaskAsyncLocker.RunAsync(() => SetSubtasksView(taskModel, restoreSelected, requestVersion));
		if (IsCurrentSubtaskRequest(requestVersion, taskModel))
		{
			SetMoreOptionMenuOnSingleTask();
		}
	}

	private TaskListView GetSubtaskList()
	{
		StackPanel subtaskGrid = _subtaskGrid;
		if (subtaskGrid != null && subtaskGrid.Children.Count >= 2 && subtaskGrid.Children[1] is TaskListView result)
		{
			return result;
		}
		return null;
	}

	private bool IsLatestSubtaskRequest(int requestVersion)
	{
		return requestVersion == Volatile.Read(ref _subtaskRequestVersion);
	}

	private bool IsCurrentSubtaskRequest(int requestVersion, TaskDetailViewModel taskModel)
	{
		if (taskModel != null && IsLatestSubtaskRequest(requestVersion))
		{
			return _task == taskModel;
		}
		return false;
	}

	private async Task<int> SetSubtasksView(TaskDetailViewModel taskModel, bool restoreSelected, int requestVersion)
	{
		if (!IsCurrentSubtaskRequest(requestVersion, taskModel))
		{
			return 0;
		}
		if (TickDragHelper.Dragging)
		{
			await Task.Delay(50);
			if (TickDragHelper.Dragging || !IsCurrentSubtaskRequest(requestVersion, taskModel))
			{
				return 0;
			}
		}
		Node<DisplayItemModel> node = null;
		List<DisplayItemModel> list = null;
		int num = 0;
		int num2;
		if (taskModel != null && taskModel.Deleted == 0 && taskModel.IsNewAdd && Mode != Constants.DetailMode.Editor && taskModel.Kind != "NOTE")
		{
			List<DisplayItemModel> pendingTemplateSubtasks = taskModel.PendingTemplateSubtasks;
			num2 = ((pendingTemplateSubtasks != null && pendingTemplateSubtasks.Count > 0) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool showPendingTemplateSubtasks = (byte)num2 != 0;
		if (showPendingTemplateSubtasks)
		{
			list = taskModel.PendingTemplateSubtasks.ToList();
		}
		else if (taskModel != null && taskModel.Deleted == 0 && !taskModel.IsNewAdd && Mode != Constants.DetailMode.Editor && taskModel.Kind != "NOTE")
		{
			SubtaskTreeBuildResult taskTree = await BuildSubtaskTreeAsync(taskModel.TaskId, taskModel.ProjectId, !LocalSettings.Settings.HideComplete, buildByChildIds: true);
			if (!IsCurrentSubtaskRequest(requestVersion, taskModel))
			{
				return 0;
			}
			await RemoveInvalidChildIdsAsync(taskTree);
			if (!IsCurrentSubtaskRequest(requestVersion, taskModel))
			{
				return 0;
			}
			node = taskTree.Root;
			list = taskTree.AllChildren;
			num = taskTree.ParentTaskLevel;
		}
		bool inSticky = Mode == Constants.DetailMode.Sticky;
		if (list != null && list.Count > 0)
		{
			list.ForEach(delegate(DisplayItemModel m)
			{
				m.InDetail = true;
				m.InSticky = inSticky;
				if (!showPendingTemplateSubtasks)
				{
					m.Level--;
				}
				m.ShowDragBar = !inSticky && !showPendingTemplateSubtasks;
			});
			if (!showPendingTemplateSubtasks)
			{
				list = node.Value.GetChildrenModels(getAll: false);
			}
			if (_subtaskGrid == null)
			{
				_subtaskGrid = new StackPanel
				{
					Margin = (inSticky ? new Thickness(-20.0, 0.0, -16.0, 0.0) : new Thickness(-20.0, 2.0, -16.0, 12.0))
				};
				Border border = new Border
				{
					Height = 4.0,
					BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0),
					Margin = (inSticky ? new Thickness(22.0, 0.0, 22.0, 4.0) : new Thickness(22.0, 4.0, 22.0, 4.0))
				};
				border.SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity10_20");
				_subtaskGrid.Children.Add(border);
				_subTaskList = new TaskListView();
				_subTaskList.ViewModel.BatchEditor = this;
				_subTaskList.NavigateTask += OnNavigateSubTask;
				_subTaskList.NeedReload += ReloadSubTasks;
				_subtaskGrid.Children.Add(_subTaskList);
				_subtaskGrid.SetValue(Grid.RowProperty, 5);
				_scrollContent.Children.Add(_subtaskGrid);
				_batchHelper = new BatchTaskEditHelper(this);
				_batchHelper.ShowOrHideOperation += OnBatchOperation;
			}
			_subtaskGrid.Visibility = Visibility.Visible;
			UpdateAddSubTaskEntry(taskModel, Mode != Constants.DetailMode.Sticky && !showPendingTemplateSubtasks);
			_subTaskList.SetIdentity(new ParentTaskIdentity(taskModel.SourceViewModel), TaskListDisplayType.SubTask);
			int parentLevel = ((!showPendingTemplateSubtasks) ? num : 0);
			_subTaskList.SetParentLevel(parentLevel);
			_subTaskList.ViewModel.SetDetailItems(list, restoreSelected);
			if (restoreSelected)
			{
				_subTaskList.ViewModel.CheckSelectedIds();
			}
			else
			{
				_subTaskList.ViewModel.SetSelectedTaskId(null);
			}
		}
		else if (_subTaskList != null)
		{
			_subTaskList.NavigateTask -= OnNavigateSubTask;
			_subTaskList.NeedReload -= ReloadSubTasks;
			_subTaskList = null;
			_scrollContent.Children.Remove(_subtaskGrid);
			_subtaskGrid = null;
			_addSubTaskGrid = null;
			_addSubTaskButtonBorder = null;
			_batchHelper.ShowOrHideOperation -= OnBatchOperation;
			_batchHelper = null;
		}
		if (_task.Kind == "TEXT")
		{
			SetContentHint(_task.TaskContent);
		}
		return 0;
	}

	private void UpdateAddSubTaskEntry(TaskDetailViewModel taskModel, bool visible)
	{
		if (_subtaskGrid == null)
		{
			return;
		}
		if (!visible)
		{
			if (_addSubTaskGrid != null)
			{
				_subtaskGrid.Children.Remove(_addSubTaskGrid);
				_addSubTaskGrid = null;
				_addSubTaskButtonBorder = null;
			}
			return;
		}
		if (_addSubTaskGrid == null)
		{
			_addSubTaskGrid = new Grid
			{
				Margin = new Thickness(22.0, 4.0, 0.0, 0.0),
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Bottom
			};
			_addSubTaskGrid.SetResourceReference(FrameworkElement.HeightProperty, "Height30");
			_addSubTaskButtonBorder = new Border
			{
				Margin = new Thickness(0.0, 3.0, 0.0, 3.0),
				Cursor = System.Windows.Input.Cursors.Hand
			};
			_addSubTaskButtonBorder.SetResourceReference(FrameworkElement.StyleProperty, "SmoothHoverBorderStyle");
			_addSubTaskButtonBorder.MouseLeftButtonUp += AddSubTaskClick;
			_addSubTaskGrid.Children.Add(_addSubTaskButtonBorder);
			System.Windows.Shapes.Path path = UiUtils.CreatePath("Ic.ProjectList.Add", "PrimaryColor", "Path01", -1.0, geoKeyIsGeo: true);
			path.SetResourceReference(FrameworkElement.HeightProperty, "Height18");
			path.SetResourceReference(FrameworkElement.WidthProperty, "Height18");
			path.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
			path.Margin = new Thickness(4.0, -1.0, 0.0, 0.0);
			path.IsHitTestVisible = false;
			TextBlock textBlock = new TextBlock
			{
				Margin = new Thickness(28.0, 0.0, 4.0, 0.0),
				IsHitTestVisible = false,
				Text = Utils.GetString("AddSubTask"),
				VerticalAlignment = VerticalAlignment.Center
			};
			textBlock.SetResourceReference(FrameworkElement.StyleProperty, "Body01");
			textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryColor");
			_addSubTaskGrid.Children.Add(path);
			_addSubTaskGrid.Children.Add(textBlock);
			_subtaskGrid.Children.Add(_addSubTaskGrid);
		}
		_addSubTaskButtonBorder.IsEnabled = taskModel?.Enable ?? true;
	}

	private void ReloadSubTasks(object sender, EventArgs e)
	{
		SetSubtasks(_task);
	}

	private void OnNavigateSubTask(object sender, string taskId)
	{
		Navigate(taskId);
		this.TaskNavigated?.Invoke(this, taskId);
	}

	private async void OnBatchOperation(object sender, bool e)
	{
		if (!e)
		{
			SetPopupStatus(show: false);
			Utils.FindParent<TaskDetailWindow>(this)?.Activate();
		}
		else
		{
			SetPopupStatus(show: true);
		}
	}

	public async void LinkParentTaskClick(object sender, RoutedEventArgs e, string taskId, bool useInWindow = false)
	{
		this.ForceHideWindow?.Invoke(this, null);
		AddActionEvent("om", "link_parent_task");
		UserActCollectUtils.AddClickEvent("link_parent_task", "link_from", "detail_om");
		App.Window.StartLinkSearch(new List<string> { taskId }, useInWindow ? "calendar" : "detail");
	}

	public async void AddSubTaskClick(object sender, RoutedEventArgs e)
	{
		AddActionEvent("om", "add_subtask");
		await _addSubTaskAsyncLocker.RunAsync((Func<Task>)AddSubTask);
	}

	public async Task AddSubTask()
	{
		if (await ProChecker.CheckTaskLimit(_task.ProjectId))
		{
			return;
		}
		_needFocusDetail = false;
		await Task.Delay(10);
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		TaskBaseViewModel newTask = new TaskBaseViewModel
		{
			Id = Utils.GetGuid(),
			ParentId = _task.TaskId,
			ProjectId = _task.ProjectId,
			IsAllDay = true,
			Kind = "TEXT",
			Title = string.Empty,
			Priority = defaultSafely.Priority,
			Status = _task.Status,
			Type = DisplayType.Task,
			ColumnId = _task.ColumnId
		};
		if (newTask.Status != 0)
		{
			newTask.CompletedTime = DateTime.Now;
			newTask.CompletedUser = LocalSettings.Settings.LoginUserId;
		}
		if (_task.Pinned)
		{
			newTask.PinnedTime = Utils.GetNowTimeStamp();
		}
		newTask.StartDate = newTask.StartDate ?? defaultSafely.GetDefaultDateTime();
		newTask.SortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(newTask.ProjectId, newTask.ParentId, false);
		newTask.CreatedTime = DateTime.Now;
		if (newTask.StartDate.HasValue)
		{
			List<TaskReminderModel> defaultAllDayReminders = TimeData.GetDefaultAllDayReminders();
			foreach (TaskReminderModel item in defaultAllDayReminders)
			{
				item.taskserverid = newTask.Id;
				await TaskReminderDao.SaveReminders(item);
			}
		}
		TaskCache.AddToDict(newTask);
		DisplayItemModel displayItemModel = new DisplayItemModel(newTask);
		UtilLog.Info("TaskDetail.AddSubTask " + newTask.Id);
		await TaskService.AddTask(displayItemModel.GetTaskModel());
		await SetSubtasks(_task, restoreSelected: false);
		GetSubtaskList()?.TryFocusItemById(newTask.Id);
		this.SubTaskChanged?.Invoke(this, null);
	}

	public void OnAICompleteClick()
	{
		if (!UserDao.IsPro())
		{
			ProChecker.ShowUpgradeDialog(ProType.AICompleteSuggest);
		}
		else if (!_task.IsNote && _task.Status == 0)
		{
			if (AICompleteWindow.Window == null)
			{
				AICompleteWindow.Window = new AICompleteWindow(_task.SourceViewModel)
				{
					Owner = App.Window
				};
				AICompleteWindow.Window.Show();
			}
			else
			{
				Utils.Toast(Utils.GetString("AIAnotherTaskGenerating"));
			}
		}
	}

	private static Task<SubtaskTreeBuildResult> BuildSubtaskTreeAsync(string taskId, string projectId, bool withCompleted, bool buildByChildIds = false)
	{
		return Task.Run(delegate
		{
			int taskLevel;
			Node<DisplayItemModel> displayItemNode = TaskDao.GetDisplayItemNode(taskId, projectId, withCompleted, buildByChildIds, out taskLevel);
			Dictionary<string, List<string>> invalidChildIdsByParent = new Dictionary<string, List<string>>();
			SortChildrenBySortOrder(displayItemNode, projectId, withCompleted, invalidChildIdsByParent);
			return new SubtaskTreeBuildResult(displayItemNode, displayItemNode?.GetAllChildrenValue(), invalidChildIdsByParent, taskLevel);
		});
	}

	private static async Task RemoveInvalidChildIdsAsync(SubtaskTreeBuildResult result)
	{
		if (result == null)
		{
			return;
		}
		foreach (KeyValuePair<string, List<string>> item in result.InvalidChildIdsByParent)
		{
			await TaskDao.AddOrRemoveTaskChildIds(item.Key, item.Value, addChild: false);
		}
	}

	private static void SortChildrenBySortOrder(Node<DisplayItemModel> node, string projectId, bool withCompleted, Dictionary<string, List<string>> invalidChildIdsByParent, bool showLoadMore = true)
	{
		if (node?.Children == null)
		{
			return;
		}
		node.Children.Sort((Node<DisplayItemModel> a, Node<DisplayItemModel> b) => a.Value.SortOrder.CompareTo(b.Value.SortOrder));
		bool showLoadMore2 = true;
		if (withCompleted && showLoadMore)
		{
			List<string> list = ((node.Value.SourceViewModel.ChildrenIds == null) ? null : JsonConvert.DeserializeObject<List<string>>(node.Value.SourceViewModel.ChildrenIds));
			if (list != null)
			{
				int num = 0;
				List<string> list2 = new List<string>();
				foreach (string item in list)
				{
					TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
					if (taskById == null)
					{
						num++;
					}
					else if (taskById.ProjectId != projectId)
					{
						list2.Add(item);
					}
				}
				if (list2.Count > 0)
				{
					invalidChildIdsByParent[node.Value.Id] = list2;
				}
				if (num > 0)
				{
					DisplayItemModel model = new DisplayItemModel(new TaskBaseViewModel
					{
						Id = node.NodeId + "more",
						ParentId = node.NodeId,
						Type = DisplayType.LoadMore,
						SortOrder = long.MinValue
					})
					{
						MoreCount = num
					};
					node.Children.Add(new DisplayItemNode(model)
					{
						Parent = node
					});
					showLoadMore2 = false;
				}
			}
		}
		foreach (Node<DisplayItemModel> child in node.Children)
		{
			SortChildrenBySortOrder(child, projectId, withCompleted, invalidChildIdsByParent, showLoadMore2);
		}
	}

	public void ShowBatchOperationDialog()
	{
		if (_batchHelper != null)
		{
			_batchHelper.ProjectIdentity = new ParentTaskIdentity(_task.SourceViewModel);
			_batchHelper.ShowOperationDialog();
		}
	}

	public void HideBatchSelectControl()
	{
	}

	public void SetSelectedTaskIds(List<string> taskIds)
	{
		BatchTaskEditHelper batchHelper = _batchHelper;
		if (batchHelper != null)
		{
			batchHelper.SelectedTaskIds = taskIds;
		}
	}

	public void RemoveSelectedId(string id)
	{
		_batchHelper?.SelectedTaskIds?.Remove(id);
	}

	public List<string> GetSelectedTaskIds()
	{
		return _batchHelper?.SelectedTaskIds ?? new List<string>();
	}

	public void ReloadList()
	{
		SetSubtasks(_task);
	}

	private void InitDetailTitle()
	{
		_titleText.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "BaseColorOpacity100_80");
		_titleText.SetResourceReference(System.Windows.Controls.Control.FontSizeProperty, GetEditorTitleFont(Mode));
		_titleText.SetValue(Grid.RowProperty, 1);
		_titleText.FontWeight = FontWeights.SemiBold;
		_titleText.SetWordWrap(wrap: true);
		_titleText.Navigate += OnNavigateTask;
		_titleText.PopOpened += OnTextPopupOpened;
		_titleText.PopClosed += OnTextPopupClosed;
		_titleText.LinkPopOpened += OnLinkPopupOpened;
		_titleText.LinkPopClosed += OnLinkPopupClosed;
		_titleText.KeysDown += OnTitleKeyDown;
		_titleText.EscKeyUp += OnEscKeyUp;
		_titleText.IgnoreTokenChanged += OnTitleIgnoreTokenChanged;
		_titleText.QuickItemSelected += OnQuickItemSelected;
		_titleText.PreviewTextInput += OnTitleInput;
		_titleText.LinkTextChange += OnTitleChanged;
		_titleText.TextGotFocus += OnTitleGotFocus;
		_titleText.TextLostFocus += OnTitleLostFocus;
		_titleText.PreviewMouseLeftButtonDown += CheckIfTaskEditable;
		_titleText.SplitText += OnTitleSplit;
		_titleText.MoveDown += OnTitleMoveDown;
		_titleText.SelectDate += OnSelectDate;
		_titleText.CaretVerticalOffsetChanged += OnContentVerticalOffsetChanged;
		_titleText.RequestBringIntoView += OnBringIntoView;
		_titleText.DateParsed += OnDateParsed;
		_scrollContent.Children.Add(_titleText);
		if (Mode == Constants.DetailMode.Sticky)
		{
			_titleText.SetBaseColor("StickyTitleTextColor", "StickyContentTextColor", "StickyTextColor40", "StickyTextColor20");
			_titleText.SetLightTheme();
			_titleText.SetResourceReference(System.Windows.Controls.Control.FontSizeProperty, "StickyFont14");
			_titleText.LineSpacing = 4.0;
			_statusIcon = new System.Windows.Shapes.Path
			{
				Width = 16.0,
				Height = 16.0,
				Stretch = Stretch.Uniform,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				Margin = new Thickness(0.0, -4.0, 4.0, 0.0),
				Visibility = Visibility.Collapsed,
				Cursor = System.Windows.Input.Cursors.Hand
			};
			_statusIcon.SetResourceReference(Shape.FillProperty, "BaseColorOpacity20");
			_statusIcon.SetValue(Grid.RowProperty, 1);
			_statusIcon.MouseLeftButtonUp += OnStatusIconClick;
			_scrollContent.Children.Add(_statusIcon);
		}
		if (Mode == Constants.DetailMode.Page)
		{
			_titleText.SetupSearchRender(isTitle: true, inSearchDetail: true);
		}
	}

	private void OnEscKeyUp(object sender, EventArgs e)
	{
		Keyboard.ClearFocus();
		Window window = Window.GetWindow(this);
		if (window != null && window.IsActive)
		{
			FocusManager.SetFocusedElement(window, window);
			Keyboard.Focus(window);
		}
	}

	private string GetEditorTitleFont(Constants.DetailMode mode)
	{
		if (mode != Constants.DetailMode.Page && mode != Constants.DetailMode.Editor)
		{
			return "Font16";
		}
		return "Font19";
	}

	private void SetTitleHint(string title)
	{
		if (string.IsNullOrEmpty(title))
		{
			if (_titleHint == null)
			{
				_titleHint = new TextBlock
				{
					Padding = new Thickness(4.0, 0.0, 0.0, 0.0),
					LineHeight = 20.0,
					FontWeight = FontWeights.SemiBold,
					FontSize = _titleText.FontSize,
					HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
					IsHitTestVisible = false
				};
				_titleHint.SetResourceReference(System.Windows.Controls.Control.FontSizeProperty, (Mode == Constants.DetailMode.Sticky) ? "StickyFont14" : "Font16");
				_titleHint.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "BaseColorOpacity40");
				_titleHint.SetValue(Grid.RowProperty, 1);
				_scrollContent.Children.Add(_titleHint);
			}
			_titleHint.Text = Utils.GetString((_task.Kind == "NOTE") ? "NoteTitle" : "what_would_you_like_to_do");
		}
		else if (_titleHint != null)
		{
			_scrollContent.Children.Remove(_titleHint);
			_titleHint = null;
		}
	}

	private void LoadTaskTitle(bool restoreOffset, bool sameTask)
	{
		TaskDetailViewModel task = _task;
		if (!restoreOffset || _titleText.Text != task.Title || !sameTask)
		{
			if (!sameTask)
			{
				_titleText.CloseLinkMenu();
				_titleText.SetText(task.Title);
			}
			else
			{
				_titleText.SetTextOffset(task.Title, restoreOffset);
			}
		}
		if (task.IsNewAdd)
		{
			_titleText.Focus();
		}
		_titleText.SetMarkRegexText(unable: false, inDetail: true, task.IsNote);
		_titleText.ReadOnly = !task.Enable;
		SetTitleHint(task.Title);
		SetKindSwitcher(Mode != Constants.DetailMode.Sticky && Mode != Constants.DetailMode.Editor && !task.IsNote);
		UpdateStatusIcon();
	}

	private void UpdateStatusIcon()
	{
		if (_statusIcon == null || _task == null)
		{
			return;
		}
		int status = _task.Status;
		_titleText.TextStatus = status;
		if (status == 0)
		{
			_statusIcon.Visibility = Visibility.Collapsed;
			_titleText.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
			return;
		}
		_statusIcon.BeginAnimation(FrameworkElement.WidthProperty, null);
		_statusIcon.Width = 16.0;
		_titleText.BeginAnimation(FrameworkElement.MarginProperty, null);
		_statusIcon.Visibility = Visibility.Visible;
		_titleText.Margin = new Thickness(24.0, 0.0, 0.0, 0.0);
		if (status == -1)
		{
			_statusIcon.Data = Utils.GetGeometry("Ic.TaskType.Abandon");
		}
		else
		{
			_statusIcon.Data = Utils.GetGeometry("Ic.TaskType.Completed");
		}
	}

	private async void OnStatusIconClick(object sender, MouseButtonEventArgs e)
	{
		if (_task != null && CheckEnable() && _task.Status != 0)
		{
			e.Handled = true;
			DoubleAnimation animation = new DoubleAnimation(16.0, 0.0, TimeSpan.FromMilliseconds(200.0))
			{
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};
			ThicknessAnimation animation2 = new ThicknessAnimation(new Thickness(24.0, 0.0, 0.0, 0.0), new Thickness(4.0, 0.0, 0.0, 0.0), TimeSpan.FromMilliseconds(200.0))
			{
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};
			_statusIcon.BeginAnimation(FrameworkElement.WidthProperty, animation);
			_titleText.BeginAnimation(FrameworkElement.MarginProperty, animation2);
			await Task.Delay(200);
			await TaskService.SetTaskStatus(_task.TaskId, 0);
		}
	}

	private void SetKindSwitcher(bool show)
	{
		if (show)
		{
			if (_kindSwitcher == null)
			{
				_kindSwitcher = new HoverIconButton
				{
					HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Top,
					RenderTransform = new TranslateTransform(0.0, -1.0),
					Height = 26.0,
					Width = 26.0,
					ImageOpacity = 0.6
				};
				_kindSwitcher.MouseLeftButtonUp += OnKindSwitchClick;
				_kindSwitcher.SetValue(Grid.RowProperty, 1);
				_scrollContent.Children.Add(_kindSwitcher);
			}
			_kindSwitcher.SetResourceReference(HoverIconButton.IconDataProperty, (_task.Kind != "CHECKLIST") ? "Ic.TaskDetail.Checkitem" : "Ic.TaskDetail.Detail");
			_kindSwitcher.IsImage = false;
			_kindSwitcher.SetValue(ToolTipService.InitialShowDelayProperty, 500);
			_kindSwitcher.SetValue(ToolTipService.BetweenShowDelayProperty, 0);
			_kindSwitcher.ToolTip = Utils.GetString((_task.Kind != "CHECKLIST") ? "ChecklistTip" : "DescriptionTip");
		}
		else if (_kindSwitcher != null)
		{
			_scrollContent.Children.Remove(_kindSwitcher);
			_kindSwitcher = null;
		}
		_titleText.Margin = new Thickness(4.0, 0.0, show ? 26 : 0, 0.0);
	}

	private void OnKindSwitchClick(object sender, MouseButtonEventArgs e)
	{
		if (Utils.IsDida() && !_task.IsOwner)
		{
			TryToast(Utils.GetString("AttendeeModifyContent"));
		}
		else if (CheckEnable())
		{
			ChangeCheckListMode();
		}
	}

	private async void ChangeCheckListMode()
	{
		List<string> tags = TagSerializer.ToTags(_task.Tag);
		LoadTags(tags);
		AddActionEvent("action", "switch_checklist_mode");
		string kind = _task.Kind;
		if (!(kind == "CHECKLIST"))
		{
			if (kind == "TEXT")
			{
				SwitchToList();
			}
		}
		else
		{
			SwitchToText();
		}
		Utils.FindParent<IToastShowWindow>(this)?.TryHideToast();
	}

	private async Task SwitchToText()
	{
		_kindSwitcher?.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.TaskDetail.Detail");
		if (_kindSwitcher != null)
		{
			_kindSwitcher.IsImage = false;
		}
		_ = string.Empty;
		bool flag = false;
		if (convertInitItems != null && _checklist?.ChecklistItems != null && convertInitItems.Any() && convertInitItems.Count == _checklist.ChecklistItems.Count)
		{
			List<(string title, int status)> list = convertInitItems.Select((TaskDetailItemModel x) => (title: x.title, status: x.status)).ToList();
			List<(string, int)> list2 = _checklist.ChecklistItems.Select((CheckItemViewModel x) => (Title: x.Title, Status: x.Status)).ToList();
			list.Sort();
			list2.Sort();
			flag = list.SequenceEqual<(string, int)>(list2);
		}
		string content = ((!flag || string.IsNullOrEmpty(_switchListOriginText)) ? _checklist.ToText(_descText.GetPersistedText()) : _switchListOriginText);
		_switchListOriginText = null;
		LoadContentControl(restoreOffset: false);
		content = await AttachmentDao.AddAttachmentStrings(_task.TaskId, content);
		_task.SourceViewModel.Kind = "TEXT";
		_task.SourceViewModel.Desc = string.Empty;
		_task.SourceViewModel.Content = content;
		LoadContentControl(restoreOffset: false);
		TryShowOptionMenu();
		if (!_task.IsNewAdd)
		{
			List<TaskDetailItemModel> item = await TaskDetailItemDao.DeleteCheckItemsByTaskId(_task.TaskId);
			_savedItems = (_task.TaskId, item);
			await SaveTask(CheckMatchedType.CheckSmart);
			TaskChangeNotifier.NotifyTaskKindChanged(new List<string> { _task.TaskId });
		}
		LoadAttachmentAsync();
	}

	private async void SwitchToList()
	{
		if (IsChecklistOverLimit())
		{
			return;
		}
		MarkDownEditor contentText = _contentText;
		if (contentText != null && contentText.GetImageDict()?.Count > 0)
		{
			SetPopupShow();
			bool? flag = new CustomerDialog(string.Empty, Utils.GetString("SwitchTextTypeWithAttachmentNotify"), Utils.GetString("Continue"), Utils.GetString("Cancel")).ShowDialog();
			SetPopupClosing();
			if (flag != true)
			{
				return;
			}
		}
		var (desc, checkItems) = await SwitchTextToList();
		_kindSwitcher?.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.TaskDetail.Checkitem");
		if (_kindSwitcher != null)
		{
			_kindSwitcher.IsImage = false;
		}
		_task.SourceViewModel.Desc = desc;
		_task.SourceViewModel.Kind = "CHECKLIST";
		_task.SourceViewModel.Content = string.Empty;
		LoadContentControl(restoreOffset: false);
		SetCheckItems(checkItems);
		SetTextEditorMenu(show: false);
		if (!_task.IsNewAdd)
		{
			await SaveTask(CheckMatchedType.CheckSmart);
			TaskChangeNotifier.NotifyTaskKindChanged(new List<string> { _task.TaskId });
		}
		LoadAttachmentAsync();
	}

	private bool IsChecklistOverLimit()
	{
		string[] obj = _contentText?.GetPersistedText()?.Replace("\r", "").Split('\n');
		long userLimit = Utils.GetUserLimit(Constants.LimitKind.SubtaskNumber);
		if (obj?.Length > userLimit)
		{
			if (UserDao.IsPro())
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProSubtaskLimit"), userLimit), MessageBoxButton.OK);
				customerDialog.Owner = Window.GetWindow(this);
				customerDialog.ShowDialog();
			}
			else
			{
				ProChecker.ShowUpgradeDialog(ProType.MoreSubTasks);
			}
			return true;
		}
		return false;
	}

	private async Task<(string, List<CheckItemViewModel>)> SwitchTextToList()
	{
		string origin = _contentText?.GetPersistedText();
		string text = _contentText?.RemoveAttachment();
		if (text?.Length != origin?.Length)
		{
			origin = null;
		}
		ChecklistExtra extra = ChecklistUtils.Text2Items(text);
		List<CheckItemViewModel> itemModels = null;
		List<TaskDetailItemModel> list;
		if (extra.ChecklistItems.Count > 0)
		{
			if (_savedItems.Item1 == _task.TaskId)
			{
				List<TaskDetailItemModel> item = _savedItems.Item2;
				if (item != null && item.Count > 0)
				{
					list = _savedItems.Item2;
					goto IL_0144;
				}
			}
			list = ChecklistUtils.BuildChecklist(_task.Id, _task.TaskId, extra.ChecklistItems);
			goto IL_0144;
		}
		goto IL_0273;
		IL_0273:
		_switchListOriginText = origin;
		return new ValueTuple<string, List<CheckItemViewModel>>(item2: itemModels ?? new List<CheckItemViewModel>(from c in _task.SourceViewModel.CheckItems.ToList()
			select new CheckItemViewModel(c, _task)), item1: extra.Description);
		IL_0144:
		List<TaskDetailItemModel> models = (convertInitItems = list);
		if (!_task.IsNewAdd)
		{
			await TaskDetailItemDao.DeleteCheckItemsByTaskId(_task.TaskId);
			await TaskDetailItemDao.BatchInsertChecklists(models);
			_task.SourceViewModel.CheckCheckItems(models);
		}
		else
		{
			itemModels = CheckItemViewModel.BuildListFromModels(models, _task);
			SetCheckItems(new List<CheckItemViewModel>());
		}
		goto IL_0273;
	}

	private void OnNavigateTask(object sender, ProjectTask task)
	{
		this.NavigateTask?.Invoke(this, task);
		switch (Mode)
		{
		}
	}

	private void SetTextReadOnly(TaskDetailViewModel model)
	{
		bool readOnly = !model.IsOwner || !model.Enable;
		_titleText.ReadOnly = readOnly;
	}

	private void OnSelectDate(object sender, EventArgs e)
	{
		if (Mode != Constants.DetailMode.Editor && Mode != Constants.DetailMode.Sticky)
		{
			SelectDate();
		}
	}

	private void OnTitleMoveDown(object sender, EventArgs e)
	{
		TryFocusDetail(focusEnd: false);
	}

	private void OnTitleSplit(object sender, int e)
	{
		if (_contentText != null)
		{
			_contentText.TryAddNewLine();
		}
		else
		{
			_checklist?.FocusFirstItem();
		}
	}

	private void CheckIfTaskEditable(object sender, MouseButtonEventArgs e)
	{
		if (Utils.IsDida() && !_task.IsOwner)
		{
			TryToast(Utils.GetString("AttendeeModifyContent"));
		}
	}

	private async void OnTitleLostFocus(object sender, EventArgs e)
	{
		if (_titleText.CanParseDate)
		{
			if (!_titleText.SelectionPopupOpened && _task != null && (Utils.FindParent<ListViewContainer>(this) == null || !(TaskListItemFocusHelper.ParseDateItem?.TitleFocused ?? false)))
			{
				await CommitTitleParseDate(requireParsingDate: false);
				TaskListItemFocusHelper.ParseDateId = string.Empty;
				TaskListItemFocusHelper.ParseDateItem = null;
			}
		}
		else if (_task.Title != _titleText.Text)
		{
			_task.SourceViewModel.Title = _titleText.Text;
			await SaveTask();
		}
		TaskUtils.TryGetTaskLinkTitle(_task?.TaskId, _task?.Title, isCheckItem: false, OnTitleLinkGet);
	}

	private void OnTitleLinkGet(string title)
	{
		_titleText.Text = title;
	}

	private async Task SetDateAndTitle(TaskDetailViewModel task, string text, TimeData timeData = null)
	{
		if (task != null)
		{
			if (timeData != null)
			{
				task.Reminders = timeData.Reminders?.ToArray();
				task.AnnoyingAlert = timeData.AnnoyingAlert;
			}
			task.SourceViewModel.Title = text;
			task.SourceViewModel.StartDate = task.DisplayStartDate;
			task.SourceViewModel.DueDate = task.DisplayDueDate;
			task.SourceViewModel.IsAllDay = task.IsAllDay;
			task.SourceViewModel.RepeatFlag = task.RepeatFlag;
			task.SourceViewModel.RepeatFrom = task.RepeatFrom;
			await SaveTask(task);
			if (timeData != null)
			{
				await TaskService.SaveReminders(task.TaskId, timeData.Reminders);
				TaskChangeNotifier.NotifyTaskDateChanged(task.TaskId);
			}
			TaskChangeNotifier.NotifyTaskTextChanged(task.TaskId);
		}
	}

	private void OnTitleGotFocus(object sender, RoutedEventArgs e)
	{
		_titleText.DateParseBaseDate = GetTitleParseBaseDate();
		if (!_titleParseDisabledByManualTime && _titleParseMode != TitleDateParseMode.Auto)
		{
			_titleText.SetCanParseDate(canParseDate: true);
			return;
		}
		TaskDetailPopup taskDetailPopup = Window.GetWindow(this) as TaskDetailPopup;
		if (_titleText.CanParseDate || !(_task.Kind != "NOTE") || (taskDetailPopup != null && taskDetailPopup.ShowInCal))
		{
			return;
		}
		bool canParseDate = false;
		TimeData defaultDate = GetDefaultDate();
		if (!_task.DisplayStartDate.HasValue)
		{
			goto IL_015e;
		}
		if (_task.DisplayStartDate == defaultDate.StartDate)
		{
			DateTime? displayDueDate = _task.DisplayDueDate;
			DateTime? dueDate = defaultDate.DueDate;
			if (displayDueDate.HasValue == dueDate.HasValue && (!displayDueDate.HasValue || displayDueDate.GetValueOrDefault() == dueDate.GetValueOrDefault()) && _task.IsAllDay == defaultDate.IsAllDay)
			{
				goto IL_015e;
			}
		}
		goto IL_0160;
		IL_0160:
		_titleText.SetCanParseDate(canParseDate);
		return;
		IL_015e:
		canParseDate = true;
		goto IL_0160;
	}

	private TimeData GetDefaultDate()
	{
		TimeData timeData = TimeData.BuildDefaultStartAndEnd();
		ListViewContainer listViewContainer = Utils.FindParent<ListViewContainer>(this);
		if (listViewContainer != null)
		{
			TimeData timeData2 = listViewContainer.GetSelectedProject()?.GetTimeData();
			if (timeData2 == null)
			{
				return timeData;
			}
			if (timeData.StartDate.HasValue)
			{
				if (timeData.DueDate.HasValue)
				{
					double totalMinutes = (timeData.DueDate - timeData.StartDate).Value.TotalMinutes;
					timeData.DueDate = timeData.StartDate.Value.AddMinutes(totalMinutes);
				}
				timeData.StartDate = timeData2.StartDate;
			}
			else
			{
				timeData.StartDate = timeData2.StartDate;
				timeData.IsAllDay = timeData2.IsAllDay;
			}
		}
		return timeData;
	}

	private void OnBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	private void OnContentVerticalOffsetChanged(object sender, double offset)
	{
		KeepOffsetInView(sender, offset);
	}

	private ImmersiveContent GetImmersiveContent()
	{
		if (Mode == Constants.DetailMode.Editor && _immersiveContent == null)
		{
			_immersiveContent = Utils.FindParent<ImmersiveContent>(this);
		}
		return _immersiveContent;
	}

	private void KeepOffsetInView(object sender, double offset)
	{
		if (Mode == Constants.DetailMode.Editor)
		{
			GetImmersiveContent()?.KeepOffsetInView(sender, offset);
			return;
		}
		double num = _titleUpperContent.ActualHeight + 10.0;
		if (!object.Equals(sender, _titleText))
		{
			num += _notionView?.GetHeight() ?? 0.0;
			num += _googleEventInfoView?.GetHeight() ?? 0.0;
		}
		if (object.Equals(sender, _contentText) || object.Equals(sender, _descText))
		{
			num += _titleText.ActualHeight;
		}
		else if (object.Equals(sender, _checklist))
		{
			num += _titleText.ActualHeight + 18.0;
			if (_descText != null)
			{
				num += _descText.ActualHeight;
			}
		}
		double num2 = (double)FindResource("Font14") - 14.0 + 2.0;
		double num3 = offset + num2 + _scrollContent.Margin.Bottom;
		double num4 = _detailScrollViewer.VerticalOffset + _detailScrollViewer.ActualHeight - num;
		double num5 = _detailScrollViewer.VerticalOffset - num;
		if (num4 >= num3 && offset >= num5)
		{
			return;
		}
		if (num3 > num4)
		{
			_detailScrollViewer.ScrollToVerticalOffset(num3 - _detailScrollViewer.ActualHeight + num);
			return;
		}
		if (offset < num5)
		{
			_detailScrollViewer.ScrollToVerticalOffset(offset + num - 10.0);
		}
		if (offset <= 24.0 + num2)
		{
			_detailScrollViewer.ScrollToVerticalOffset(num - 24.0 - num2);
		}
	}

	private void OnTitleChanged(object sender, EventArgs e)
	{
		if (_task.Title != _titleText.Text)
		{
			_task.SourceViewModel.Title = _titleText.Text;
			TaskChangeNotifier.NotifyTaskTextChanged(_task.TaskId);
			TryDelayEditSaveSync();
		}
	}

	private void OnTitleInput(object sender, TextCompositionEventArgs e)
	{
		SetTitleHint("text");
	}

	private void OnQuickItemSelected(object sender, QuickSetModel model)
	{
		switch (model.Type)
		{
		case QuickSetType.Project:
			UtilLog.Info($"TaskDetail.SetProject {_task?.Id}, value{model.Project.id} column {model.ColumnId} from:quickSelect");
			QuickSetProject(model.Project, model.ColumnId);
			break;
		case QuickSetType.Priority:
			UtilLog.Info($"TaskDetail.SetPriority {_task?.Id}, value{model.Priority} from:quickSelect");
			SetTaskPriority(model.Priority);
			break;
		case QuickSetType.Tag:
			UtilLog.Info($"TaskDetail.SetTag {_task?.Id}, value{model.Tag} from:quickSelect");
			OnTagAdded(sender, model.Tag.ToLower());
			break;
		case QuickSetType.Date:
			TryClearParseDate();
			if (model.Date.HasValue)
			{
				OnDateSelected(model.Date.Value);
			}
			UtilLog.Info($"TaskDetail.SetDate {_task?.Id}, value{model.Date} from:quickSelect");
			break;
		case QuickSetType.Assign:
			UtilLog.Info($"TaskDetail.SetAssignee {_task?.Id}, value{model.Avatar.UserId} from:quickSelect");
			SetAssignee(model.Avatar.UserId, inBatch: false);
			break;
		}
	}

	private async void QuickSetProject(ProjectModel project, string columnId = null)
	{
		TaskDetailViewModel task = _task;
		if (project != null && task != null)
		{
			IsBlank = false;
			if (task.IsNewAdd)
			{
				task.SourceViewModel.ProjectId = project.id;
				task.SourceViewModel.ColumnId = columnId;
			}
			else
			{
				await Task.Delay(50);
				await MoveProject(project, columnId);
			}
		}
	}

	public string GetProjectIds()
	{
		return _task.ProjectId;
	}

	private void TryClearParseDate()
	{
		if (_titleText.CanParseDate)
		{
			_titleText.ClearParseDate();
			if (Utils.FindParent<ListViewContainer>(this) != null)
			{
				TaskListItemFocusHelper.ParseDateItem?.TitleTextBox.ClearParseDate();
			}
		}
	}

	private void OnTitleIgnoreTokenChanged(object sender, List<string> e)
	{
		if (Utils.FindParent<ListViewContainer>(this) != null)
		{
			TaskListItemFocusHelper.ParseDateItem?.SetIgnoreTokens(e);
		}
	}

	private void OnTitleKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Tab)
		{
			if (_contentText != null)
			{
				_contentText.FocusEditBox();
			}
			else
			{
				_descText?.Focus();
			}
			e.Handled = true;
		}
	}

	private void OnTextPopupClosed(object sender, EventArgs e)
	{
		SetPopupClosing();
	}

	private void OnTextPopupOpened(object sender, EventArgs e)
	{
		SetPopupShow();
	}

	private void OnChecklistPopupOpened(object sender, bool isWindow)
	{
		SetPopupShow();
	}

	private void OnChecklistPopupClosed(object sender, bool isWindow)
	{
		SetPopupClosing();
	}

	private void OnLinkPopupOpened(object sender, EventArgs e)
	{
		OnShowDialog();
	}

	private void OnLinkPopupClosed(object sender, EventArgs e)
	{
		OnCloseDialog();
	}

	private void OnDateParsed(object sender, IPaserDueDate e)
	{
		TimeData parseData = e?.ToTimeData(addDefaultReminder: true);
		_titleParseVersion++;
		_task.SetParseData(parseData);
	}

	public void SetTitleParseEnable(bool canParse, bool delay)
	{
		SetTitleParseMode(canParse ? TitleDateParseMode.Full : TitleDateParseMode.Auto, delay);
	}

	public async void SetTitleParseMode(TitleDateParseMode mode, bool delay)
	{
		if (delay)
		{
			await Task.Delay(200);
		}
		_titleParseDisabledByManualTime = false;
		_titleParseMode = mode;
		_titleText.DateParseMode = mode;
		_titleText.DateParseBaseDate = GetTitleParseBaseDate();
		_titleText.SetCanParseDate(!_titleParseDisabledByManualTime && mode != TitleDateParseMode.Auto);
	}

	private void DisableTitleParseForManualTime()
	{
		TaskDetailViewModel task = _task;
		if (task != null && task.IsNewAdd && !_titleParseDisabledByManualTime)
		{
			_titleParseDisabledByManualTime = true;
			_titleText.DateParseMode = TitleDateParseMode.Auto;
			_titleText.ClearParseDate();
			_task.SetParseData(null);
		}
	}

	private DateTime GetTitleParseBaseDate()
	{
		return (_task?.StartDate ?? _task?.DisplayStartDate ?? DateTime.Today).Date;
	}

	public async Task<bool> TrySaveParseDate()
	{
		return await CommitTitleParseDate(requireParsingDate: true);
	}

	private async Task<bool> CommitTitleParseDate(bool requireParsingDate)
	{
		TaskDetailViewModel task = _task;
		if (task == null)
		{
			return false;
		}
		if (_titleParseCommitTask != null && !_titleParseCommitTask.IsCompleted && _titleParseCommitModel == task)
		{
			int commitVersion = _titleParseCommitVersion;
			bool flag = await _titleParseCommitTask;
			if (_task == task && _titleParseVersion != commitVersion)
			{
				bool flag2 = flag;
				flag = flag2 | await CommitTitleParseDate(requireParsingDate);
			}
			return flag;
		}
		if ((requireParsingDate && !_titleText.ParsingDate) || (!requireParsingDate && !_titleText.CanParseDate))
		{
			return false;
		}
		bool parsingDate = _titleText.ParsingDate;
		TimeData parsedData = _titleText.ParsedData?.ToTimeData(addDefaultReminder: true);
		int titleParseVersion = _titleParseVersion;
		_titleText.SetCanParseDate(canParseDate: false);
		string parsedText = _titleText.GetParsedText();
		_titleParseCommitModel = task;
		_titleParseCommitVersion = titleParseVersion;
		_titleParseCommitTask = CommitTitleParseDateCore(task, parsedText, parsedData, parsingDate, titleParseVersion);
		return await _titleParseCommitTask;
	}

	private async Task<bool> CommitTitleParseDateCore(TaskDetailViewModel task, string text, TimeData parsedData, bool dateParsed, int parseVersion)
	{
		await SetDateAndTitle(task, text, parsedData);
		if (_task != task || _titleParseVersion == parseVersion)
		{
			task?.SetParseData(null);
		}
		return dateParsed;
	}

	public void TryFocusTitle(bool focusEnd = true, string taskId = null)
	{
		if (string.IsNullOrEmpty(taskId) || !(taskId != _task.TaskId))
		{
			Keyboard.Focus(_titleText);
			if (focusEnd)
			{
				_titleText.FocusEnd();
			}
			else
			{
				_titleText.Focus();
			}
		}
	}

	public void ClearParseDate()
	{
		_titleText.ClearParseDate();
	}

	public DetailTextBox GetTitleText()
	{
		return _titleText;
	}
}
