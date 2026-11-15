using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using KotlinModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Detail;

public class TaskDetailWindows : MyWindow, IToastShowWindow, IComponentConnector
{
	private const double WindowSizePadding = 16.0;

	private bool _isUserResizing;

	private bool _programmaticResize;

	private string _taskId;

	public static List<TaskDetailWindows> OpenedWindows = new List<TaskDetailWindows>();

	private WindowState _originState;

	private bool _closed;

	internal TaskDetailWindows Root;

	internal ContentControl Control;

	internal Grid OperationPanel;

	internal TaskDetailPopupView Detail;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	private Dictionary<string, KeyBinding> _keyBindings { get; set; } = new Dictionary<string, KeyBinding>();

	public TaskDetailWindows(int windowWidth, int windowHeight)
	{
		InitializeComponent();
		ApplyInitialWindowSize(windowWidth, windowHeight);
		Utils.SetWindowChrome(this, new Thickness(5.0));
		InitWindowEvent();
		InitGlobalEvent();
		base.Loaded += OnWindowLoaded;
	}

	private void ApplyInitialWindowSize(double windowWidth, double windowHeight)
	{
		double num = windowWidth;
		double num2 = windowHeight;
		Screen screen = ((App.Window != null) ? Screen.FromHandle(new WindowInteropHelper(App.Window).Handle) : Screen.PrimaryScreen);
		if (screen != null)
		{
			double num3 = ((App.Window != null) ? ScreenPositionUtils.GetPointDpi(new Point(App.Window.Left + 1.0, App.Window.Top + 1.0)) : 1.0);
			double val = Math.Max(base.MinWidth, (double)screen.WorkingArea.Width / num3 - 16.0);
			double val2 = Math.Max(base.MinHeight, (double)screen.WorkingArea.Height / num3 - 16.0);
			num = Math.Min(num, val);
			num2 = Math.Min(num2, val2);
		}
		base.Width = num;
		base.Height = num2;
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnWindowLoaded;
		InitKeyBinding();
	}

	private async void ResetPosition()
	{
		WindowHelper.MoveToCenter(this);
		Matrix? transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
		await Task.Delay(1000);
		Point point = WidgetLocationHelper.GetPomoLocationSafely(defaultPoint: new Point(SystemParameters.PrimaryScreenWidth / 2.0, SystemParameters.PrimaryScreenHeight / 2.0), left: base.Left, top: base.Top, width: base.Width, height: base.Height, transform: transform);
		base.Left = point.X;
		base.Top = point.Y;
	}

	private void InitGlobalEvent()
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if ((e.DeletedChangedIds.Any() && e.DeletedChangedIds.Contains(Detail.TaskId)) || (e.UndoDeletedIds.Any() && e.UndoDeletedIds.Contains(Detail.TaskId)))
		{
			TryClose();
		}
	}

	private void InitWindowEvent()
	{
		Detail.ShowUndoOnTaskDeleted -= TaskDeleted;
		Detail.ShowUndoOnTaskDeleted += TaskDeleted;
		Detail.EscKeyUp -= OnEscKeyUp;
		Detail.EscKeyUp += OnEscKeyUp;
		Detail.NavigateTask -= OnNavigateTask;
		Detail.NavigateTask += OnNavigateTask;
		Detail.TaskNavigated -= OnTaskNavigated;
		Detail.TaskNavigated += OnTaskNavigated;
		Detail.TagClick -= OnTagClick;
		Detail.TagClick += OnTagClick;
		Detail.NotifyCloseWindow -= OnNotifyClose;
		Detail.NotifyCloseWindow += OnNotifyClose;
		InitDetailEvents();
		base.SizeChanged -= MainWindow_LocationOrSizeChanged;
		base.SizeChanged += MainWindow_LocationOrSizeChanged;
		base.SourceInitialized -= MainWindow_SourceInitialized;
		base.SourceInitialized += MainWindow_SourceInitialized;
	}

	private void MainWindow_SourceInitialized(object sender, EventArgs e)
	{
		HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).AddHook(WndProc);
	}

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		switch (msg)
		{
		case 561:
			_isUserResizing = true;
			break;
		case 562:
			if (_isUserResizing)
			{
				_isUserResizing = false;
				SaveWindowSettings();
			}
			break;
		}
		return IntPtr.Zero;
	}

	private void MainWindow_LocationOrSizeChanged(object sender, EventArgs e)
	{
		if (base.WindowState == WindowState.Normal && _isUserResizing)
		{
			SaveWindowSettings();
		}
	}

	private void SaveWindowSettings()
	{
		LocalSettings settings = LocalSettings.Settings;
		settings.ExtraSettings.TaskWindowWidth = base.Width;
		settings.ExtraSettings.TaskWindowHeight = base.Height;
		LocalSettings.Settings.Save();
	}

	private void InitKeyBinding()
	{
		AddKeyBinding("CompleteTask", GetKeyBinding(new RelayCommand(delegate(object o)
		{
			((TaskDetailWindows)o).CompletedTaskCommand();
		})));
		AddKeyBinding("PinTask", GetKeyBinding(new RelayCommand(delegate(object o)
		{
			((TaskDetailWindows)o).PinTask();
		})));
		AddKeyBinding("DeleteTask", GetKeyBinding(new RelayCommand(delegate(object o)
		{
			((TaskDetailWindows)o).DeleteTask();
		})));
	}

	private void AddKeyBinding(string key, KeyBinding kb)
	{
		lock (_keyBindings)
		{
			_keyBindings[key] = kb;
			KeyBindingManager.TryAddKeyBinding(key, kb);
		}
	}

	private KeyBinding GetKeyBinding(ICommand command)
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

	private async void OnNotifyClose(object sender, EventArgs e)
	{
		TryClose();
	}

	private void OnTagClick(object sender, string e)
	{
		TryClose();
	}

	private void OnTaskNavigated(object sender, string e)
	{
		_taskId = e;
	}

	private async void OnNavigateTask(object sender, ProjectTask pt)
	{
		if (!(await ShowTaskWindows(pt.TaskId)))
		{
			WindowToastHelper.ToastString(ToastGrid, Utils.GetString("NoTaskFound"));
		}
	}

	private void OnEscKeyUp(object sender, EventArgs e)
	{
		if (Detail.Mode == Constants.DetailMode.Editor)
		{
			Detail.Mode = Constants.DetailMode.Page;
			Detail.SetEditMenuMode(inImmerse: false);
			Detail.Reload();
		}
		else if (base.IsActive && !Detail.PopupShowing)
		{
			TryClose();
		}
	}

	public void TryHideToast()
	{
		if (ToastGrid.Children.Count > 0)
		{
			if (ToastGrid.Children[0] is UndoToast undoToast)
			{
				undoToast.OnFinished();
			}
			ToastGrid.Children[0].Visibility = Visibility.Collapsed;
		}
	}

	public void TryToastString(object sender, string e)
	{
		ToastString(sender, e);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		if (tasks == null)
		{
			return false;
		}
		return await TaskService.BatchDeleteTasks(tasks, showUndo: true, 1, tasks.Any((TaskModel t) => t.id == _taskId) ? null : ToastGrid);
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, undo);
	}

	public async void TaskDeleted(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.deleted = 1;
			task.modifiedTime = DateTime.Now;
			await TaskService.UpdateTaskOnDeletedChanged(task);
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(taskId, task.title);
			ShowUndoToast(undoToast, taskId);
		}
	}

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
	}

	public void ToastMoveProjectControl(string projectProjectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, uiElement);
	}

	private async Task ShowUndoToast(UndoToast undoToast, string taskId = null)
	{
		WindowToastHelper.ShowAndHideToast((taskId == _taskId) ? null : ToastGrid, undoToast);
	}

	private void ToastString(object sender, string e)
	{
		WindowToastHelper.ToastString(ToastGrid, e);
	}

	private void CheckItemsDeleted(object sender, TaskDetailItemModel e)
	{
		UndoToast undoToast = new UndoToast();
		undoToast.InitSubtaskUndo(e);
		ShowUndoToast(undoToast);
	}

	private async void TaskDeleted(object sender, string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(taskId, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				BatchDeleteTask(list);
			}
			else
			{
				TaskDeleted(taskId);
			}
		}
	}

	public static async Task<bool> ShowTaskWindows(string taskId, bool focus = false)
	{
		TaskBaseViewModel taskModel = TaskCache.GetTaskById(taskId);
		if (taskModel != null)
		{
			TaskDetailWindows exist = OpenedWindows.FirstOrDefault((TaskDetailWindows w) => w._taskId == taskId);
			if (exist != null && exist._closed)
			{
				OpenedWindows.Remove(exist);
				exist = null;
			}
			if (exist == null)
			{
				OpenNew();
			}
			else
			{
				try
				{
					exist.Detail.Navigate(taskId);
					if (!string.IsNullOrEmpty(taskModel.Title))
					{
						exist.Title = taskModel.Title;
					}
					exist.Show();
					await Task.Delay(10);
					exist.Activate();
					if (focus)
					{
						if (string.IsNullOrEmpty(taskModel.Title))
						{
							exist.Detail.TryFocusTitle();
						}
						else
						{
							exist.Detail.TryFocusDetail();
						}
					}
				}
				catch (Exception)
				{
					OpenedWindows.Remove(exist);
					OpenNew();
				}
			}
			return true;
		}
		return false;
		async Task OpenNew()
		{
			await Task.Delay(50);
			int.TryParse(taskModel.CommentCount, out var result);
			double num = ((LocalSettings.Settings.ExtraSettings.TaskWindowWidth > 0.0) ? LocalSettings.Settings.ExtraSettings.TaskWindowWidth : 438.0);
			double num2 = ((LocalSettings.Settings.ExtraSettings.TaskWindowHeight > 0.0) ? LocalSettings.Settings.ExtraSettings.TaskWindowHeight : ((double)((result > 0 || !string.IsNullOrEmpty(taskModel.ChildrenIds)) ? 520 : 360)));
			TaskDetailWindows window = new TaskDetailWindows((int)num, (int)num2)
			{
				_taskId = taskId
			};
			window.Detail.Navigate(taskId);
			if (!string.IsNullOrEmpty(taskModel.Title))
			{
				window.Title = taskModel.Title;
			}
			window.Show();
			OpenedWindows.Add(window);
			await Task.Delay(100);
			if (!window._closed)
			{
				window.Visibility = Visibility.Visible;
				window.Activate();
				if (focus)
				{
					if (string.IsNullOrEmpty(taskModel.Title))
					{
						window.Detail.TryFocusTitle();
					}
					else
					{
						window.Detail.TryFocusDetail();
					}
				}
			}
		}
	}

	private void InitDetailEvents()
	{
		Detail.EnterImmersive -= OnEnterImmersive;
		Detail.EnterImmersive += OnEnterImmersive;
		Detail.ExitImmersive -= OnExitImmersive;
		Detail.ExitImmersive += OnExitImmersive;
	}

	private void OnExitImmersive(object sender, EventArgs e)
	{
		Detail.Mode = Constants.DetailMode.Popup;
		Detail.SetEditMenuMode(inImmerse: false);
		Detail.SetImmerse();
		Detail.Reload();
	}

	private async void OnEnterImmersive(string id, int caret)
	{
		Detail.Mode = Constants.DetailMode.Editor;
		Detail.Reload();
		Detail.SetImmerse();
		await Task.Delay(400);
		Detail.SetEditMenuMode(inImmerse: true);
	}

	private async Task BeforeClose()
	{
		Detail.TryClosePopup();
		await TrySaveTask();
	}

	public async Task TryClose()
	{
		Close();
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		_closed = true;
		Detail.RemoveKeyBinding();
		OpenedWindows.Remove(this);
		await BeforeClose();
		base.OnClosing(e);
	}

	public async Task TrySaveTask(bool force = false)
	{
		object dataContext = Detail.DataContext;
		if (!(dataContext is TaskDetailViewModel model))
		{
			return;
		}
		bool dateSaved = await Detail.TrySaveParseDate();
		if (!(NeedSaveNew(model) || force))
		{
			return;
		}
		if (string.IsNullOrEmpty(model.TaskId))
		{
			model.SourceViewModel.Id = Utils.GetGuid();
		}
		TaskModel task = await TaskDetailViewModel.ToTaskModel(model);
		if (task == null)
		{
			return;
		}
		TaskDetailItemModel[] items = model.Items;
		if (items != null && items.Length != 0)
		{
			TaskDetailItemModel[] items2 = model.Items;
			foreach (TaskDetailItemModel obj in items2)
			{
				obj.TaskServerId = task.id;
				await TaskDetailItemDao.InsertChecklistItem(obj);
			}
			if (model.Items.All((TaskDetailItemModel item) => item.status != 0))
			{
				task.status = 2;
				task.completedTime = DateTime.Now;
			}
		}
		if (await TaskDao.GetThinTaskById(task.id) != null)
		{
			await TaskService.SaveTask(task);
		}
		else
		{
			PopupStateManager.OnAddPopupClosed(needWait: false);
			task.createdTime = DateTime.Now;
			await TaskService.AddTask(task);
			model.IsNewAdd = false;
			if (dateSaved)
			{
				await TaskService.SaveTaskReminders(task);
			}
		}
		await TaskService.AddPendingTemplateChildren(model);
	}

	private bool NeedSaveNew(TaskDetailViewModel model)
	{
		if (!model.IsNewAdd)
		{
			return false;
		}
		bool flag = !string.IsNullOrEmpty(model.Title) || !string.IsNullOrEmpty(model.TaskContent) || !string.IsNullOrEmpty(model.Desc) || !Detail.IsBlank;
		List<CheckItemViewModel> checklistItems = Detail.GetChecklistItems();
		if (checklistItems != null)
		{
			flag = flag || !IsItemsEmpty(checklistItems);
			if (flag)
			{
				List<TaskDetailItemModel> list = new List<TaskDetailItemModel>();
				foreach (CheckItemViewModel item in checklistItems)
				{
					TaskDetailItemModel taskDetailItemModel = new TaskDetailItemModel
					{
						id = (string.IsNullOrEmpty(item.Id) ? Utils.GetGuid() : item.Id),
						TaskId = 0,
						title = item.Title,
						sortOrder = item.SortOrder,
						completedTime = null,
						startDate = item.StartDate,
						isAllDay = item.IsAllDay,
						snoozeReminderTime = item.SnoozeReminderTime,
						status = item.Status
					};
					if (taskDetailItemModel.status != 0)
					{
						taskDetailItemModel.completedTime = DateTime.Now;
					}
					list.Add(taskDetailItemModel);
				}
				model.Items = list.ToArray();
			}
		}
		return flag;
	}

	private bool IsItemsEmpty(List<CheckItemViewModel> items)
	{
		if (items != null && items.Count > 1)
		{
			return false;
		}
		if (items != null && items.Count == 1 && !string.IsNullOrEmpty(items[0].Title))
		{
			return false;
		}
		return true;
	}

	public void EnterCommand()
	{
	}

	public void CompletedTaskCommand()
	{
		Detail.ToggleTaskCompleted();
	}

	public void PinTask()
	{
		Detail.OnPin();
	}

	public void DeleteTask()
	{
		Detail.DeleteTask();
	}

	public async void ReLoad()
	{
		Activate();
		object dataContext = Detail.DataContext;
		if (dataContext is TaskDetailViewModel model)
		{
			await Task.Delay(100);
			Detail.Navigate(model.TaskId);
		}
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		TryClose();
	}

	private void OnNormalButtonClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Normal;
	}

	private void OnMaxButtonClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Maximized;
	}

	private void OnMinButtonClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void OnDragMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	public void ReLoad(string taskId, string itemId, TaskModifyType type)
	{
		if (taskId == Detail.TaskId)
		{
			ReLoad();
		}
	}

	private void OnStateChanged(object sender, EventArgs e)
	{
		if (base.WindowState == WindowState.Maximized && _originState != WindowState.Minimized)
		{
			Hide();
			Show();
		}
		_originState = base.WindowState;
	}

	public static void CloseAll()
	{
		for (int i = 0; i < OpenedWindows.Count; i++)
		{
			OpenedWindows[i].DelayClose();
		}
	}

	private async void DelayClose()
	{
		await Task.Delay(200);
		TryClose();
	}

	protected override async void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		Detail.InvalidContentMargin();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);
		Detail.InvalidContentMargin();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskdetailwindows.xaml", UriKind.Relative);
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
			Root = (TaskDetailWindows)target;
			break;
		case 2:
			Control = (ContentControl)target;
			break;
		case 3:
			OperationPanel = (Grid)target;
			OperationPanel.MouseMove += OnDragMove;
			break;
		case 4:
			((System.Windows.Controls.Button)target).Click += OnMinButtonClick;
			break;
		case 5:
			((System.Windows.Controls.Button)target).Click += OnMaxButtonClick;
			break;
		case 6:
			((System.Windows.Controls.Button)target).Click += OnNormalButtonClick;
			break;
		case 7:
			((System.Windows.Controls.Button)target).Click += OnCloseClick;
			break;
		case 8:
			Detail = (TaskDetailPopupView)target;
			break;
		case 9:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
