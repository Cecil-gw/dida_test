using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using KotlinModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Detail;

public class TaskDetailWindow : MyWindow, IToastShowWindow, IComponentConnector
{
	private readonly Stack<ProjectTask> _navigateStack = new Stack<ProjectTask>();

	private double _lastTop;

	public IToastShowWindow DependentWindow;

	public UIElement Target;

	private DateTime _operationTime;

	private static TaskDetailWindow _navigateWindow;

	private Dictionary<string, KeyBinding> _keyBindings = new Dictionary<string, KeyBinding>();

	internal TaskDetailWindow Root;

	internal ContentControl Control;

	internal Grid OperationPanel;

	internal TaskDetailPopupView Detail;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	public event EventHandler<ProjectTask> NavigateTask;

	public event EventHandler<string> Disappear;

	public event EventHandler<string> TaskSaved;

	public TaskDetailWindow()
	{
		InitializeComponent();
		Utils.SetWindowChrome(this, new Thickness(5.0));
		InitWindowEvent();
		BindEvents();
		base.Closed += delegate
		{
			UnbindEvents();
		};
		base.Loaded += OnWindowLoaded;
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnWindowLoaded;
		ResetPosition();
	}

	private async void ResetPosition()
	{
		double left = base.Left;
		double top = base.Top;
		WindowHelper.MoveTo(this, (int)base.Left, (int)base.Top);
		base.Left = left;
		base.Top = top;
		Matrix? transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
		await Task.Delay(1000);
		Point point = WidgetLocationHelper.GetPomoLocationSafely(defaultPoint: new Point(SystemParameters.PrimaryScreenWidth / 2.0, SystemParameters.PrimaryScreenHeight / 2.0), left: base.Left, top: base.Top, width: base.Width, height: base.Height, transform: transform);
		base.Left = point.X;
		base.Top = point.Y;
	}

	private void UnbindEvents()
	{
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.MainWindowHidden -= OnMainWindowHidden;
		RemoveKeyBinding();
	}

	private void BindEvents()
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.MainWindowHidden += OnMainWindowHidden;
		InitShortcut();
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
		Detail.CheckItemsDeleted -= CheckItemsDeleted;
		Detail.CheckItemsDeleted += CheckItemsDeleted;
		Detail.NavigateBack -= OnNavigateBack;
		Detail.NavigateBack += OnNavigateBack;
		Detail.EscKeyUp -= OnEscKeyUp;
		Detail.EscKeyUp += OnEscKeyUp;
	}

	private void OnEscKeyUp(object sender, EventArgs e)
	{
		if (!Detail.MorePopupOpen)
		{
			Detail.SetPopupStatus(show: false);
			TryClose();
		}
	}

	private async void OnNavigateBack(object sender, EventArgs e)
	{
		if (_navigateStack.Count > 0)
		{
			if (_navigateStack.Count <= 1)
			{
				SetNavigateBackDisabled();
			}
			Navigate(_navigateStack.Pop(), isBack: true, _navigateStack.Count > 1);
		}
	}

	private void SetNavigateBackDisabled()
	{
		Detail.SetBackIconEnable(enable: false);
	}

	private async void OnNavigateTask(object sender, ProjectTask task)
	{
		await OnNavigateTask(task);
	}

	public async Task OnNavigateTask(ProjectTask task)
	{
		if (await TaskUtils.TryLoadTask(task.TaskId, task.ProjectId) != null)
		{
			if (OperationPanel.Visibility == Visibility.Collapsed)
			{
				InitNavigateWindow();
				await Navigate(task, isBack: false, canBack: false);
			}
			else
			{
				_navigateStack.Push(new ProjectTask
				{
					TaskId = Detail.TaskId,
					ProjectId = Detail.ProjectId
				});
				await Navigate(task);
			}
			Activate();
			if (base.WindowState == WindowState.Minimized)
			{
				base.WindowState = WindowState.Normal;
			}
		}
	}

	private void SetNavigateEnabled()
	{
		Detail.SetBackIconEnable(enable: true);
	}

	private void TrySaveOnEnter(object sender, EventArgs e)
	{
		TryClose();
	}

	public void TryToastString(object sender, string e)
	{
		ToastString(sender, e);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		return await TaskService.BatchDeleteTasks(tasks, showUndo: true, 1, ToastGrid);
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, undo);
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

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
	}

	public void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, uiElement);
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
			ShowUndoToast(undoToast);
		}
	}

	private async Task ShowUndoToast(UndoToast undoToast)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, undoToast);
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
				DependentWindow?.BatchDeleteTask(list);
			}
			else
			{
				DependentWindow?.TaskDeleted(taskId);
			}
		}
	}

	public async void ShowInNavigate(string taskId, Window target)
	{
		TaskDetailViewModel taskDetailViewModel = await TaskDetailViewModel.Build(taskId);
		if (taskDetailViewModel != null)
		{
			InitNavigateWindow();
			double height = (App.Window.Height - 384.0) / 2.0;
			double targetWidth = (App.Window.Width - 520.0) / 2.0;
			Show(taskDetailViewModel, string.Empty, new TaskWindowDisplayArgs(target, targetWidth, default(Point), height));
			base.Owner = null;
		}
	}

	private void InitNavigateWindow()
	{
		base.Width = 406.0;
		base.Height = 320.0;
		base.MinWidth = 404.0;
		Control.MaxHeight = 10000.0;
		Detail.MaxHeight = 10000.0;
		base.ShowInTaskbar = true;
		base.MaxHeight = 10000.0;
		Detail.SetNavigate();
		OperationPanel.Visibility = Visibility.Visible;
		base.SizeToContent = SizeToContent.Manual;
	}

	public async void Show(TaskDetailViewModel model, string itemId, TaskWindowDisplayArgs args, Point point = default(Point), bool canParse = false, ProjectIdentity parentIdentity = null)
	{
		Target = args.Target;
		if (Target == null)
		{
			args.Point = PopupLocationCalculator.GetMousePoint(isDefault: false);
			base.Owner = null;
		}
		else
		{
			try
			{
				Window window = Window.GetWindow(Target);
				if (window != null && window.Topmost)
				{
					base.Owner = ((!(window is LoadMoreWindow)) ? window : null);
				}
				else
				{
					base.Owner = null;
				}
				if (window is WidgetWindow widgetWindow)
				{
					base.Resources = widgetWindow.Resources;
				}
				else
				{
					base.Resources = Application.Current.Resources;
				}
				Detail.SetTheme((bool)FindResource("IsDarkTheme"));
			}
			catch (Exception)
			{
				base.Owner = null;
			}
		}
		base.Topmost = true;
		model.Mode = 1;
		InitDetailEvents();
		Detail.SetInQuadrant(args.QuadrantLevel);
		Detail.ParentIdentity = parentIdentity;
		Detail.SetTitleParseMode(canParse ? TitleDateParseMode.Full : args.TitleParseMode, delay: false);
		if (point == default(Point) && Target is Window window2)
		{
			point = new Point(window2.Left + args.TargetWidth, window2.Top);
		}
		if (model.IsNewAdd)
		{
			PopupStateManager.OnAddPopupOpened();
		}
		else
		{
			PopupStateManager.OnViewPopupOpened();
		}
		await TryShowWindow(model, itemId, args.AddHeight, point);
		Keyboard.ClearFocus();
		await Task.Delay(200);
		if (model.IsNewAdd || args.FocusTitle)
		{
			Detail.TryFocusTitle();
			return;
		}
		FocusManager.SetFocusedElement(this, this);
		Keyboard.Focus(this);
	}

	private async Task TryShowWindow(TaskDetailViewModel model, string itemId, double addHeight, Point point)
	{
		Detail.ScrollToTop();
		Detail.Navigate(model, itemId);
		await Task.Delay(10);
		BeginAnimation(Window.TopProperty, null);
		BeginAnimation(Window.LeftProperty, null);
		base.Top = point.Y + addHeight;
		base.Left = point.X;
		Control.BeginAnimation(UIElement.OpacityProperty, null);
		Control.Opacity = 1.0;
		TryShow();
	}

	private void InitDetailEvents()
	{
		Detail.EnterImmersive -= OnEnterImmersive;
		Detail.EnterImmersive += OnEnterImmersive;
		Detail.TagClick -= OnTagClick;
		Detail.TagClick += OnTagClick;
		Detail.NavigateTask -= OnNavigateTask;
		Detail.NavigateTask += OnNavigateTask;
		Detail.ForceHideWindow -= OnForceHideWindow;
		Detail.ForceHideWindow += OnForceHideWindow;
	}

	private void OnForceHideWindow(object sender, EventArgs e)
	{
		TryHideWindow();
	}

	private void OnEnterImmersive(string id, int caret)
	{
		App.Window?.OnEnterImmersive(id, caret);
		TryHideWindow();
	}

	private void OnMainWindowHidden(object sender, EventArgs e)
	{
		TryHideWindow();
	}

	private void OnTagClick(object sender, string e)
	{
		TryClose();
	}

	private async Task BeforeClose()
	{
		await Task.Delay(100);
		Detail.TryClosePopup();
		DependentWindow = null;
		Target = null;
		await TrySaveTask();
		Detail.RecordLabelTime();
		this.Disappear?.Invoke(this, (Detail.DataContext as TaskDetailViewModel)?.TaskId);
	}

	public async Task TryClose(bool needActivateOwner = true)
	{
		await BeforeClose();
		Close();
		if (needActivateOwner)
		{
			base.Owner?.Activate();
		}
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		Detail.RemoveKeyBinding();
		await BeforeClose();
		base.OnClosing(e);
	}

	private async Task TryHideWindow(bool force = false)
	{
		await BeforeClose();
		Close();
	}

	public async Task TrySaveTask(bool force = false)
	{
		object dataContext = Detail.DataContext;
		if (!(dataContext is TaskDetailViewModel model))
		{
			return;
		}
		if (string.IsNullOrEmpty(model.TaskId))
		{
			model.SourceViewModel.Id = Utils.GetGuid();
		}
		Detail.TryParseUrl();
		bool dateSaved = await Detail.TrySaveParseDate();
		if (!(NeedSaveNew(model) || force))
		{
			return;
		}
		TaskModel task = await TaskDetailViewModel.ToTaskModel(model);
		if (task == null)
		{
			return;
		}
		TaskDetailItemModel[] items = model.Items;
		if (items != null && items.Length != 0 && model.Items.All((TaskDetailItemModel item) => item.status != 0))
		{
			task.status = 2;
			task.completedTime = DateTime.Now;
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
			if (!dateSaved)
			{
				await TaskService.SaveTaskReminders(task);
			}
		}
		TaskDetailItemModel[] items2 = model.Items;
		if (items2 != null && items2.Length != 0)
		{
			TaskDetailItemModel[] items3 = model.Items;
			foreach (TaskDetailItemModel obj in items3)
			{
				obj.TaskServerId = task.id;
				await TaskDetailItemDao.InsertChecklistItem(obj);
			}
		}
		await TaskService.AddPendingTemplateChildren(model);
		this.TaskSaved?.Invoke(this, model.TaskId);
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

	public async void FocusTitle()
	{
		await Task.Delay(50);
		Detail.TryFocusTitle(focusEnd: false);
	}

	public void EnterCommand()
	{
		if (Detail.DataContext is TaskDetailViewModel { IsNewAdd: not false })
		{
			TryClose();
		}
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

	private async Task Navigate(ProjectTask projectTask, bool isBack = false, bool canBack = true)
	{
		if (await TaskUtils.TryLoadTask(projectTask.TaskId, projectTask.ProjectId, isBack) != null)
		{
			if (canBack)
			{
				SetNavigateEnabled();
			}
			Detail.Navigate(projectTask.TaskId);
		}
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		TryClose();
	}

	private void OnNormalButtonClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Normal;
		base.Top = _lastTop;
	}

	private void OnMaxButtonClick(object sender, RoutedEventArgs e)
	{
		_lastTop = base.Top;
		base.WindowState = WindowState.Maximized;
	}

	private void OnMinButtonClick(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void OnDragMove(object sender, MouseEventArgs e)
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

	private void OnWindowKeyUp(object sender, KeyEventArgs e)
	{
		if (!object.Equals(FocusManager.GetFocusedElement(this), this))
		{
			return;
		}
		switch (e.Key)
		{
		case Key.Delete:
			Detail.OnDelete();
			break;
		case Key.Return:
			Detail.TryFocusTitle();
			break;
		case Key.Escape:
			if (!Detail.PopupShowing)
			{
				TryClose();
			}
			break;
		}
	}

	public void Clear()
	{
		DependentWindow = null;
		Target = null;
		this.Disappear?.Invoke(this, null);
		PopupStateManager.OnViewPopupClosed();
		PopupStateManager.OnAddPopupClosed(needWait: false);
		Detail.Navigate(new TaskDetailViewModel(new TaskBaseViewModel()));
	}

	public async void TryShow()
	{
		try
		{
			Show();
			Activate();
			await Task.Delay(200);
			Activate();
		}
		catch (Exception)
		{
		}
	}

	public void ToastInParent(string toast)
	{
		DependentWindow?.TryToastString(null, toast);
	}

	public static async Task NavigateProjectTask(ProjectTask task, IToastShowWindow window)
	{
		if (await TaskUtils.TryLoadTask(task.TaskId, task.ProjectId) == null)
		{
			return;
		}
		if (_navigateWindow == null)
		{
			_navigateWindow = new TaskDetailWindow();
			_navigateWindow.ShowInNavigate(task.TaskId, window as Window);
			_navigateWindow.DependentWindow = window;
			_navigateWindow.Closed += delegate
			{
				_navigateWindow = null;
			};
		}
		else
		{
			_navigateWindow.OnNavigateTask(task);
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
	}

	private void InitShortcut()
	{
		AddKeyBinding("OpenSticky", GetKeyBinding(DetailWindowCommands.OpenAsSticky));
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

	public void OpenAsSticky()
	{
		Detail.OpenAsSticky();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskdetailwindow.xaml", UriKind.Relative);
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
			Root = (TaskDetailWindow)target;
			break;
		case 2:
			Control = (ContentControl)target;
			break;
		case 3:
			OperationPanel = (Grid)target;
			OperationPanel.MouseMove += OnDragMove;
			break;
		case 4:
			((Button)target).Click += OnMinButtonClick;
			break;
		case 5:
			((Button)target).Click += OnMaxButtonClick;
			break;
		case 6:
			((Button)target).Click += OnNormalButtonClick;
			break;
		case 7:
			((Button)target).Click += OnCloseClick;
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
