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
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit.Editing;
using KotlinModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Kanban.Item;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.TaskList.Item;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Detail;

public class TaskDetailPopup : MyWindow, IToastShowWindow, IComponentConnector
{
	private static TaskDetailPopup _window;

	private readonly Stack<ProjectTask> _navigateStack = new Stack<ProjectTask>();

	private bool _isInOperation;

	private double _lastTop;

	private string _taskId;

	private DateTime _ignoreNewAddDeactivateUntil = DateTime.MinValue;

	public IToastShowWindow DependentWindow;

	public UIElement Target;

	private DateTime _operationTime;

	private CalendarEventModel _createdCalendarEvent;

	private bool _calendarAddFlowClosed;

	private bool _completeCalendarAddFlowOnClose;

	private static TaskDetailWindow _navigateWindow;

	private Dictionary<string, KeyBinding> _keyBindings = new Dictionary<string, KeyBinding>();

	private bool _altKeyDown;

	internal TaskDetailPopup Root;

	internal ContentControl Control;

	internal TaskDetailPopupView Detail;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	public static bool Showing => _window != null;

	public bool IsCalendarContext { get; set; }

	public bool ShowInCal
	{
		get
		{
			if (Target != null && !(Target is TaskListItem) && !(Target is KanbanItemView))
			{
				return !(Target is WidgetTaskItem);
			}
			return false;
		}
	}

	public bool DelayCalendarAddCleanup { get; private set; }

	public string CalendarAddFlowTaskId { get; private set; }

	public event EventHandler<ProjectTask> NavigateTask;

	public event EventHandler<string> Disappear;

	public event EventHandler<string> TaskSaved;

	public event EventHandler<string> TaskDateSaved;

	public event EventHandler<CalendarEventModel> CalendarAddFlowClosed;

	public TaskDetailPopup()
	{
		InitializeComponent();
		InitWindowEvent();
		BindEvents();
		_window = this;
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
		if (!_isInOperation && !Detail.MorePopupOpen)
		{
			Detail.SetPopupStatus(show: false);
			TryClose();
		}
		else
		{
			_isInOperation = false;
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
		await Task.Delay(200);
		TaskDetailWindow taskDetailWindow = new TaskDetailWindow();
		UIElement uIElement = (DependentWindow as UIElement) ?? Target;
		taskDetailWindow.ShowInNavigate(target: (uIElement == null) ? null : Window.GetWindow(uIElement), taskId: task.TaskId);
		taskDetailWindow.DependentWindow = DependentWindow;
		TryClose();
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

	public void BeginCalendarAddFlow()
	{
		DelayCalendarAddCleanup = true;
		CalendarAddFlowTaskId = (Detail.DataContext as TaskDetailViewModel)?.TaskId ?? _taskId;
		_createdCalendarEvent = null;
		_calendarAddFlowClosed = false;
		_completeCalendarAddFlowOnClose = false;
	}

	public void NotifyCalendarEventCreated(CalendarEventModel model)
	{
		_createdCalendarEvent = model;
	}

	public void TransferCalendarAddFlowTo(TaskDetailPopup target)
	{
		if (target != null && DelayCalendarAddCleanup)
		{
			target.DelayCalendarAddCleanup = true;
			target.CalendarAddFlowTaskId = CalendarAddFlowTaskId;
			target._createdCalendarEvent = _createdCalendarEvent;
			target._calendarAddFlowClosed = false;
			target._completeCalendarAddFlowOnClose = true;
			target.CalendarAddFlowClosed = this.CalendarAddFlowClosed;
			target.TaskSaved = this.TaskSaved;
		}
	}

	public void CompleteCalendarAddFlow()
	{
		if (!_calendarAddFlowClosed)
		{
			_calendarAddFlowClosed = true;
			DelayCalendarAddCleanup = false;
			_completeCalendarAddFlowOnClose = false;
			CalendarDisplayService.IgnoreCalendarAddTempTask(CalendarAddFlowTaskId);
			this.CalendarAddFlowClosed?.Invoke(this, _createdCalendarEvent);
		}
	}

	private void CompleteCalendarAddFlowAsTask()
	{
		if (DelayCalendarAddCleanup)
		{
			_calendarAddFlowClosed = true;
			DelayCalendarAddCleanup = false;
			_completeCalendarAddFlowOnClose = false;
		}
	}

	public async Task<bool> Show(string taskId, string itemId, TaskWindowDisplayArgs args, ProjectIdentity parentIdentity = null)
	{
		TaskDetailViewModel taskDetailViewModel = await TaskDetailViewModel.Build(taskId);
		if (taskDetailViewModel != null)
		{
			Show(taskDetailViewModel, itemId, args, default(Point), show: true, canParse: false, parentIdentity);
		}
		return taskDetailViewModel != null;
	}

	public async void Show(TaskDetailViewModel model, string itemId, TaskWindowDisplayArgs args, Point point = default(Point), bool show = true, bool canParse = false, ProjectIdentity parentIdentity = null, bool calAdd = false)
	{
		_ignoreNewAddDeactivateUntil = ((model.IsNewAdd && calAdd) ? DateTime.Now.AddMilliseconds(900.0) : DateTime.MinValue);
		if (model.IsNewAdd)
		{
			PopupStateManager.OnAddPopupOpened();
		}
		_taskId = model.TaskId;
		PopupStateManager.LastTarget = args.Target;
		Target = args.Target;
		Window window = base.Owner;
		if (Target == null)
		{
			args.Point = PopupLocationCalculator.GetMousePoint(isDefault: false);
			base.Owner = null;
		}
		else
		{
			try
			{
				Window window2 = Window.GetWindow(Target);
				window = window2;
				if (window2 != null && window2.Topmost)
				{
					base.Owner = window2;
				}
				if (window2 is WidgetWindow widgetWindow)
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
		Detail.UseInWidget = args.UseInWidget;
		if (point == default(Point))
		{
			point = ((!(Target is Window window3)) ? PopupLocationCalculator.GetTaskPopupLocation(Target, args.TargetWidth, 420.0, args.Point, args.AddHeight) : new Point(window3.Left + args.TargetWidth, window3.Top));
		}
		Window window4 = window ?? App.Window;
		if (window4 != null && point.X > window4.Width + window4.Left)
		{
			point.X = window4.Width + window4.Left;
		}
		point.Y += args.AddHeight;
		double num = PopupLocationCalculator.FixVerticalLocation(point.X, point.Y, 320.0, Target);
		if (Math.Abs(num - point.Y) > 0.0)
		{
			point.Y = num;
		}
		if (model.IsNewAdd)
		{
			PopupStateManager.OnAddPopupOpened();
		}
		else
		{
			PopupStateManager.OnViewPopupOpened();
		}
		await TryShowWindow(model, itemId, point, show);
		Keyboard.ClearFocus();
		await Task.Delay(200);
		if (model.IsNewAdd || args.FocusTitle)
		{
			await Task.Delay(300);
			Detail.TryFocusTitle();
			_ignoreNewAddDeactivateUntil = DateTime.MinValue;
		}
		else
		{
			await Task.Delay(100);
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
	}

	private async Task TryShowWindow(TaskDetailViewModel model, string itemId, Point point, bool show = true)
	{
		Detail.ScrollToTop();
		base.Opacity = 0.0;
		double y = point.Y;
		double x = point.X;
		base.Top = y;
		base.Left = x;
		if (show)
		{
			TryShow();
		}
		Detail.Navigate(model, itemId);
		base.Top = y;
		base.Left = x;
		base.Opacity = 1.0;
		CheckVerticalOffset(null, null);
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
		_isInOperation = false;
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

	public async Task TryClose(bool needActivateOwner = true)
	{
		Close();
		if (needActivateOwner)
		{
			base.Owner?.Activate();
		}
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		UIElement target = Target;
		Window window = base.Owner;
		if (target != null)
		{
			Window window2 = Window.GetWindow(target);
			if (window2 != null)
			{
				window = window2;
			}
		}
		Detail.RemoveKeyBinding();
		Detail.ClearEvents();
		UnbindEvents();
		if (this == _window)
		{
			_window = null;
		}
		PopupStateManager.ClearLastTarget(Target);
		Detail.TryClosePopup();
		DependentWindow = null;
		Target = null;
		await TrySaveTask();
		PopupStateManager.OnAddPopupClosed();
		PopupStateManager.OnViewPopupClosed();
		this.Disappear?.Invoke(this, (Detail.DataContext as TaskDetailViewModel)?.TaskId);
		if (_completeCalendarAddFlowOnClose)
		{
			CompleteCalendarAddFlow();
		}
		base.OnClosing(e);
		await Task.Delay(50);
		CheckAltKeyDown();
		window?.Activate();
	}

	private async Task CheckAltKeyDown()
	{
		if (StaticPropertiesModel.Instance.AltKeyDown)
		{
			while (Utils.IfAltPressed())
			{
				await Task.Delay(100);
			}
			SetAltKeyDown(down: false);
		}
	}

	private async void OnDeactivated(object sender, EventArgs e)
	{
		bool popupShow = Detail.PopupShowing;
		bool isNew = Detail.DataContext is TaskDetailViewModel taskDetailViewModel && taskDetailViewModel.IsNewAdd;
		try
		{
			if (popupShow)
			{
				if (!Detail.PopupClosing)
				{
					return;
				}
				await Task.Delay(140);
				if (Detail.PopupShowing)
				{
					return;
				}
			}
			if (isNew)
			{
				if (DateTime.Now <= _ignoreNewAddDeactivateUntil)
				{
					Activate();
					Detail.TryFocusTitle();
				}
				else
				{
					TryHideWindow(force: true);
				}
			}
			else
			{
				TryHideWindow(force: true);
			}
		}
		finally
		{
			UtilLog.Info($"DetailWindowClose OnDeactivated PopupShowing {popupShow},isNew {isNew},inOpera {_isInOperation}");
		}
	}

	private bool GetCanClose()
	{
		if (DependentWindow != null && DependentWindow is FrameworkElement element)
		{
			IShowTaskDetailWindow mousePointVisibleItem = Utils.GetMousePointVisibleItem<IShowTaskDetailWindow>(null, element);
			if (mousePointVisibleItem != null && !mousePointVisibleItem.Equals(Target))
			{
				return false;
			}
		}
		return true;
	}

	private async Task TryHideWindow(bool force = false)
	{
		if (!_isInOperation || force)
		{
			_isInOperation = false;
			try
			{
				Close();
			}
			catch (Exception)
			{
			}
		}
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
			PopupStateManager.OnViewPopupOpened();
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
		CompleteCalendarAddFlowAsTask();
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
		if (!_isInOperation && Detail.DataContext is TaskDetailViewModel { IsNewAdd: not false })
		{
			TryClose();
		}
	}

	private async void CheckVerticalOffset(object sender, EventArgs e)
	{
		base.Deactivated -= OnDeactivated;
		base.Deactivated += OnDeactivated;
		Activate();
		await Task.Delay(100);
		double top = PopupLocationCalculator.FixVerticalLocation(base.Left, base.Top, base.ActualHeight, this);
		BeginAnimation(Window.TopProperty, null);
		base.Top = top;
		Detail.SetPopupStatus(show: false);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		double num = PopupLocationCalculator.FixVerticalLocation(base.Left, base.Top, base.ActualHeight, this);
		if (Math.Abs(num - base.Top) > 0.0)
		{
			BeginAnimation(Window.TopProperty, null);
			base.Top = num;
		}
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		base.SizeChanged -= OnSizeChanged;
		base.SizeChanged += OnSizeChanged;
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

	public void ReLoad(string taskId, string itemId, TaskModifyType type)
	{
		if (taskId == Detail.TaskId)
		{
			ReLoad();
		}
	}

	public static void TryCloseWindow(bool force = false)
	{
		if (PopupStateManager.IsViewPopOpened())
		{
			_window?.TryClose();
		}
	}

	public static async void SetCanClose(bool value)
	{
		TaskDetailPopup window = _window;
		if (window != null)
		{
			await Task.Delay(50);
			if (window.IsActive)
			{
				window.Detail.SetPopupStatus(!value);
			}
		}
	}

	public static void TryCloseWindow()
	{
		try
		{
			_window?.TryClose();
		}
		catch (Exception)
		{
		}
	}

	public static bool IsShowing()
	{
		return _window != null;
	}

	private void OnWindowKeyDown(object sender, KeyEventArgs e)
	{
		if (Utils.IfAltPressed() && e.Key == Key.System)
		{
			SetAltKeyDown(down: true);
			return;
		}
		SetAltKeyDown(down: false);
		if (object.Equals(FocusManager.GetFocusedElement(this), this))
		{
			Key key = e.Key;
			if ((key == Key.Up || key == Key.Down) && Target is IShowTaskDetailWindow showTaskDetailWindow)
			{
				showTaskDetailWindow.OnDetailKeyUp(e.Key);
			}
		}
	}

	private async void SetAltKeyDown(bool down)
	{
		if (_altKeyDown == down)
		{
			return;
		}
		_altKeyDown = down;
		if (down && !StaticPropertiesModel.Instance.AltKeyDown && Keyboard.FocusedElement is TextArea)
		{
			await Task.Delay(500);
			if (!_altKeyDown)
			{
				return;
			}
		}
		StaticPropertiesModel.Instance.AltKeyDown = down;
	}

	private void OnWindowKeyUp(object sender, KeyEventArgs e)
	{
		if (!Utils.IfAltPressed())
		{
			SetAltKeyDown(down: false);
		}
		if (e.Key == Key.System && e.SystemKey == Key.LeftAlt)
		{
			e.Handled = true;
		}
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
			if (!_isInOperation)
			{
				TryClose();
			}
			else
			{
				_isInOperation = false;
			}
			break;
		}
	}

	public bool TryShow()
	{
		try
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Show();
			Activate();
			if (Utils.IfAltPressed())
			{
				SetAltKeyDown(down: true);
			}
		}
		catch (Exception)
		{
			_window = null;
			PopupStateManager.OnViewPopupClosed();
			PopupStateManager.OnAddPopupClosed(needWait: false);
			return false;
		}
		return true;
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
		AddKeyBinding("OpenSticky", GetKeyBinding(DetailPopupCommands.OpenAsSticky));
		AddKeyBinding("CompleteTask", GetKeyBinding(DetailPopupCommands.ToggleCompleted));
		AddKeyBinding("PinTask", GetKeyBinding(DetailPopupCommands.PinTask));
		AddKeyBinding("DeleteTask", GetKeyBinding(DetailPopupCommands.DeleteTask));
		AddKeyBinding("SetNoPriority", GetKeyBinding(DetailPopupCommands.SetPriorityNoneCommand));
		AddKeyBinding("SetLowPriority", GetKeyBinding(DetailPopupCommands.SetPriorityLowCommand));
		AddKeyBinding("SetMediumPriority", GetKeyBinding(DetailPopupCommands.SetPriorityMediumCommand));
		AddKeyBinding("SetHighPriority", GetKeyBinding(DetailPopupCommands.SetPriorityHighCommand));
		AddKeyBinding("ClearDate", GetKeyBinding(DetailPopupCommands.ClearDateCommand));
		AddKeyBinding("SetToday", GetKeyBinding(DetailPopupCommands.SetTodayCommand));
		AddKeyBinding("SetTomorrow", GetKeyBinding(DetailPopupCommands.SetTomorrowCommand));
		AddKeyBinding("SetNextWeek", GetKeyBinding(DetailPopupCommands.SetNextWeekCommand));
		AddKeyBinding("SetDate", GetKeyBinding(DetailPopupCommands.SelectDateCommand));
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

	public void ToggleCompleted()
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

	public bool IsFocus()
	{
		if (Keyboard.FocusedElement != null)
		{
			if (!object.Equals(Keyboard.FocusedElement, this))
			{
				return Utils.FindParent<TaskDetailPopup>(Keyboard.FocusedElement as UIElement) != null;
			}
			return true;
		}
		return false;
	}

	private void OnTaskDataSaved(object sender, string e)
	{
		this.TaskDateSaved?.Invoke(this, e);
	}

	public static bool IsDragOver(MouseEventArgs e)
	{
		if (_window != null)
		{
			Point position = e.GetPosition(_window);
			if (position.X > 0.0 && position.X < _window.ActualWidth && position.Y > 0.0)
			{
				return position.Y < _window.ActualHeight;
			}
			return false;
		}
		return false;
	}

	public void Clear()
	{
		Close();
		if (PopupStateManager.LastTarget == Target)
		{
			PopupStateManager.LastTarget = null;
		}
		if (_window == this)
		{
			_window = null;
		}
		DependentWindow = null;
		Target = null;
		this.Disappear?.Invoke(this, _taskId);
		PopupStateManager.OnViewPopupClosed();
	}

	public void SetPriority(int pri)
	{
		Detail?.SetPriority(pri);
	}

	public void SetDate(string key)
	{
		Detail?.SetDate(key);
	}

	public void SelectDate()
	{
		Detail?.SelectDate();
	}

	public void ClearDate()
	{
		Detail?.ClearDate();
	}

	public static async Task TrySetDetailEditing(bool editing, int delay = 0)
	{
		if (_window != null)
		{
			await Task.Delay(delay);
			_window.Detail?.SetPopupStatus(editing);
		}
	}

	public static TaskDetailPopup GetOpenedWindow()
	{
		return _window;
	}

	public async void MovePosition(UIElement element, TaskDetailViewModel taskModel, string itemId, Point point, double width, double height)
	{
		BeginAnimation(UIElement.OpacityProperty, null);
		base.Opacity = 0.0;
		await Task.Delay(50);
		Target = element;
		PopupStateManager.LastTarget = Target;
		point = PopupLocationCalculator.GetTaskPopupLocation(Target, width, 420.0, point, height);
		point.Y += height;
		double num = PopupLocationCalculator.FixVerticalLocation(base.Left, point.Y, 320.0, this);
		if (Math.Abs(num - point.Y) > 0.0)
		{
			point.Y = num;
		}
		base.Top = point.Y;
		base.Left = point.X;
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(0.0, 1.0, 180);
		doubleAnimation.BeginTime = TimeSpan.FromSeconds(0.12);
		doubleAnimation.EasingFunction = new QuadraticEase
		{
			EasingMode = EasingMode.EaseOut
		};
		BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		Detail.Navigate(taskModel, itemId);
		CheckVerticalOffset(null, null);
	}

	private void Root_Closing(object sender, CancelEventArgs e)
	{
		foreach (Window window in Application.Current.Windows)
		{
			if (window != this && window.Owner == this && window is CustomerDialog)
			{
				e.Cancel = true;
				break;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskdetailpopup.xaml", UriKind.Relative);
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
			Root = (TaskDetailPopup)target;
			break;
		case 2:
			Control = (ContentControl)target;
			break;
		case 3:
			Detail = (TaskDetailPopupView)target;
			break;
		case 4:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
