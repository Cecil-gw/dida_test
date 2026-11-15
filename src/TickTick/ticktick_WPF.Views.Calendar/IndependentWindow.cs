using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Editing;
using KotlinModels;
using Microsoft.Win32;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar;

public class IndependentWindow : MyWindow, IToastShowWindow, IKeyBinding, IComponentConnector
{
	private DateTime _headerClickOldTime;

	protected bool _mouseDown;

	private bool _canDragToNormal;

	private static ImageBrush _backBrush;

	protected string Id;

	private DateTime _lastActiveTime;

	private readonly bool _persistWindowModel;

	private Dictionary<string, KeyBinding> _keyBindings = new Dictionary<string, KeyBinding>();

	private bool _enableHandleMinMax;

	private bool _inPrimaryScreen;

	protected double InitLeft;

	protected double InitTop;

	private bool _altKeyDown;

	internal IndependentWindow Window;

	internal Border WindowBackground;

	internal Grid Container;

	internal ColumnDefinition Column1;

	internal ColumnDefinition Column2;

	internal System.Windows.Controls.Button MinButton;

	internal System.Windows.Controls.Button MaxButton;

	internal System.Windows.Controls.Button NormalButton;

	internal System.Windows.Controls.Button CloseButton;

	internal Polygon X;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	public IndependentWindow(WindowModel windowModel)
		: this(windowModel.Left, windowModel.Top, windowModel.Width, windowModel.Height, windowModel.MaxSize, persistWindowModel: true)
	{
	}

	protected IndependentWindow(CalendarWidgetModel widgetModel)
		: this(widgetModel.left, widgetModel.top, widgetModel.width, widgetModel.height, maxSize: false, persistWindowModel: false)
	{
	}

	private IndependentWindow(double left, double top, double width, double height, bool maxSize, bool persistWindowModel)
	{
		_persistWindowModel = persistWindowModel;
		InitializeComponent();
		Utils.SetWindowChrome(this, new Thickness(5.0, 5.0, 3.0, 5.0));
		base.Width = width;
		base.Height = height;
		InitLeft = left;
		InitTop = top;
		base.Left = left;
		base.Top = top;
		SetBackBrush();
		base.SizeChanged += OnSizeChanged;
		base.LocationChanged += OnLocationChanged;
		base.Loaded += OnLoaded;
		base.PreviewKeyUp += OnWindowKeyUp;
		base.PreviewKeyDown += OnWindowKeyDown;
		SystemEvents.SessionSwitch += OnSessionSwitch;
		base.Deactivated += OnDeactivated;
		_lastActiveTime = DateTime.Now;
		base.WindowState = (maxSize ? WindowState.Maximized : WindowState.Normal);
	}

	private void OnWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (Utils.IfAltPressed() && e.Key == Key.System)
		{
			SetAltKeyDown(down: true);
		}
		else
		{
			SetAltKeyDown(down: false);
		}
	}

	private void OnDeactivated(object sender, EventArgs e)
	{
		CheckAltKeyDown();
		_lastActiveTime = DateTime.Now;
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

	private async void SetAltKeyDown(bool down)
	{
		if (_altKeyDown == down)
		{
			return;
		}
		_altKeyDown = down;
		string text = ((this is TaskListWindow || this is TaskWindow) ? "in_task_tab" : ((this is CalendarWindow) ? "in_calendar_tab" : ((this is MatrixWindow) ? "in_matrix_tab" : null)));
		if (text == null)
		{
			return;
		}
		if (down && !StaticPropertiesModel.Instance.AltKeyDown)
		{
			if (Keyboard.FocusedElement is TextArea)
			{
				await Task.Delay(500);
				if (!_altKeyDown)
				{
					return;
				}
				UserActCollectUtils.AddClickEvent("shortcut", "alt", "in_task_tab_editing");
			}
			else
			{
				UserActCollectUtils.AddClickEvent("shortcut", "alt", text);
			}
		}
		StaticPropertiesModel.Instance.AltKeyDown = down;
	}

	private async void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
	{
		await Task.Delay(3000);
		RefreshUi();
	}

	private void RefreshUi()
	{
		base.Width += 2.0;
		InvalidateVisual();
		base.Width -= 2.0;
		UpdateLayout();
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		((HwndSource)PresentationSource.FromVisual(this))?.AddHook(HookProc);
	}

	private IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 36)
		{
			if (Utils.IsWindows7())
			{
				return IntPtr.Zero;
			}
			if (base.WindowState != WindowState.Maximized || _enableHandleMinMax)
			{
				if (base.WindowState == WindowState.Maximized)
				{
					_enableHandleMinMax = false;
					DelaySetEnableHandleMinMax();
				}
				else
				{
					_enableHandleMinMax = true;
				}
				Screen screen = Screen.FromRectangle(new System.Drawing.Rectangle((int)base.Left, (int)base.Top, (int)base.Width, (int)base.Height));
				if (screen.WorkingArea.Size != screen.Bounds.Size || !screen.Primary)
				{
					SetInPrimary(inPrimary: false);
					return IntPtr.Zero;
				}
				if (Mouse.LeftButton == MouseButtonState.Pressed && base.WindowState == WindowState.Normal)
				{
					return IntPtr.Zero;
				}
				if (WindowSizing.WmGetMinMaxInfo(hwnd, lParam, base.MinWidth, base.MinHeight))
				{
					SetInPrimary(inPrimary: true);
					handled = true;
					return IntPtr.Zero;
				}
				SetInPrimary(inPrimary: false);
			}
		}
		return IntPtr.Zero;
		async void DelaySetEnableHandleMinMax()
		{
			await Task.Delay(500);
			_enableHandleMinMax = true;
		}
		void SetInPrimary(bool inPrimary)
		{
			_inPrimaryScreen = inPrimary;
			if (!inPrimary && base.WindowState == WindowState.Maximized)
			{
				((Grid)base.Content).Margin = new Thickness(7.0, 5.0, 7.0, 5.0);
			}
			else if (inPrimary)
			{
				((Grid)base.Content).Margin = new Thickness(0.0);
			}
		}
	}

	private async void OnStateChanged(object sender, EventArgs e)
	{
		if (base.WindowState == WindowState.Minimized)
		{
			ClearCopiedTaskSelectionOnWindowMinimized();
		}
		if (base.WindowState == WindowState.Maximized && !_inPrimaryScreen)
		{
			((Grid)base.Content).Margin = new Thickness(7.0, 5.0, 7.0, 5.0);
		}
		else
		{
			((Grid)base.Content).Margin = new Thickness(0.0);
		}
		if (_persistWindowModel && base.WindowState == WindowState.Maximized)
		{
			await WindowModelDao.SaveWindowMaxSize(Id, maxSize: true);
		}
		if (_persistWindowModel && base.WindowState == WindowState.Normal)
		{
			await WindowModelDao.SaveWindowMaxSize(Id, maxSize: false);
		}
	}

	protected virtual void ClearCopiedTaskSelectionOnWindowMinimized()
	{
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		InitShortcut();
		base.Loaded -= OnLoaded;
		if (_persistWindowModel)
		{
			ResetPosition();
		}
	}

	private async void ResetPosition()
	{
		WindowHelper.MoveTo(this, (int)InitLeft, (int)InitTop);
		base.Left = InitLeft;
		base.Top = InitTop;
		Matrix? transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
		await Task.Delay(1000);
		System.Windows.Point point = WidgetLocationHelper.GetPomoLocationSafely(defaultPoint: new System.Windows.Point(100.0, 100.0), left: base.Left, top: base.Top, width: base.Width, height: base.Height, transform: transform);
		base.Left = point.X;
		base.Top = point.Y;
	}

	protected override async void OnActivated(EventArgs e)
	{
		if ((DateTime.Now - _lastActiveTime).TotalMinutes > 10.0)
		{
			RefreshUi();
		}
		_lastActiveTime = DateTime.Now;
		Utils.ToastWindow = this;
		App.CheckLastActiveTime();
		base.OnActivated(e);
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		DelayActionHandlerCenter.TryDoAction(Id + "WindowLocationChange", async delegate
		{
			base.Dispatcher.Invoke(() => SaveLocation());
		});
	}

	protected virtual async Task SaveLocation()
	{
		if (_persistWindowModel && base.WindowState == WindowState.Normal)
		{
			await WindowModelDao.SaveWindowLocation(Id, base.Left, base.Top, base.Width, base.Height);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		DelayActionHandlerCenter.TryDoAction(Id + "WindowLocationChange", delegate
		{
			base.Dispatcher.Invoke((Func<Task>)async delegate
			{
				SaveLocation();
			});
		});
		OnSizeChanged(e);
	}

	protected virtual void OnSizeChanged(SizeChangedEventArgs e)
	{
	}

	protected static async Task<WindowModel> GetNewWindowModel(string id, bool isProject = false, string data = null, Window win = null)
	{
		WindowModel model = new WindowModel
		{
			UserId = LocalSettings.Settings.LoginUserId,
			Id = id,
			Width = (isProject ? 340.0 : MainWindowManager.Window.Width),
			Height = (isProject ? 550.0 : MainWindowManager.Window.Height),
			Left = (((!isProject) ? ((double?)null) : win?.GetActualLeft()) ?? MainWindowManager.Window.GetActualLeft()) + 50.0,
			Top = (((!isProject) ? ((double?)null) : win?.GetActualTop()) ?? MainWindowManager.Window.GetActualTop()) + 50.0,
			Type = "independent",
			Data = data
		};
		await BaseDao<WindowModel>.InsertAsync(model);
		return model;
	}

	protected virtual void OnClosing()
	{
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		SystemEvents.SessionSwitch -= OnSessionSwitch;
		RemoveShortcut();
		OnClosing();
		if (_persistWindowModel && !WindowManager.AppLockOrExit)
		{
			WindowModelDao.CloseAsync(Id);
		}
		if (Equals(Utils.ToastWindow))
		{
			Utils.ToastWindow = null;
		}
		base.OnClosing(e);
	}

	protected static void AddShowEvent(string data)
	{
		UserActCollectUtils.AddClickEvent("project_list_ui", "open_in_new_window", data);
	}

	private void HeaderGridButtonDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
		if ((DateTime.Now - _headerClickOldTime).TotalMilliseconds <= 300.0)
		{
			ToggleWindowState();
		}
		else
		{
			if (base.WindowState == WindowState.Maximized)
			{
				_canDragToNormal = true;
			}
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DragMove();
				e.Handled = false;
			}
		}
		_headerClickOldTime = DateTime.Now;
	}

	private void ToggleWindowState()
	{
		base.WindowState = ((base.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
	}

	private void StopDragMove(object sender, MouseButtonEventArgs e)
	{
		_canDragToNormal = false;
	}

	private void OnDragMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (base.WindowState == WindowState.Maximized && e.LeftButton == MouseButtonState.Pressed && _canDragToNormal)
		{
			UIElement obj = (UIElement)sender;
			System.Windows.Point position = Mouse.GetPosition(obj);
			System.Windows.Point point = obj.PointToScreen(new System.Windows.Point(0.0, 0.0));
			CompositionTarget compositionTarget = PresentationSource.FromVisual(obj)?.CompositionTarget;
			if (compositionTarget != null)
			{
				point = compositionTarget.TransformFromDevice.Transform(point);
			}
			base.Top = point.Y;
			double num = 0.0;
			double width = base.Width;
			if (position.X > width / 2.0)
			{
				num = position.X - width / 2.0;
			}
			base.Left = point.X + num;
			base.WindowState = WindowState.Normal;
			LocalSettings.Settings.Maxmized = false;
			_canDragToNormal = false;
			try
			{
				DragMove();
			}
			catch (Exception)
			{
			}
			e.Handled = true;
		}
	}

	private void MinButton_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void MaxButtonClick(object sender, RoutedEventArgs e)
	{
		ToggleWindowState();
	}

	private void NormalButtonClick(object sender, RoutedEventArgs e)
	{
		ToggleWindowState();
	}

	private void OnCloseButtonClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnClickButtonMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		X.Fill = System.Windows.Media.Brushes.White;
	}

	private void OnClickButtonMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		X.SetResourceReference(Shape.FillProperty, "BaseColorOpacity60");
	}

	public static void SetBackGround(ImageBrush brush)
	{
		_backBrush = brush;
		CalendarWindow.Window?.SetBackBrush();
		MatrixWindow.Window?.SetBackBrush();
		HabitWindow.Window?.SetBackBrush();
		AIWindow.Current?.SetBackBrush();
		TaskWindow.Window?.SetBackBrush();
		FocusWindow.Window?.SetBackBrush();
		CountdownWindow.Window?.SetBackBrush();
		TaskListWindow.SetWindowsBackBrush();
	}

	public void SetBackBrush()
	{
		WindowBackground.Background = _backBrush;
		WindowBackground.Visibility = ((_backBrush == null) ? Visibility.Collapsed : Visibility.Visible);
	}

	public static async Task Restore()
	{
		System.Windows.Application.Current?.Dispatcher.Invoke(delegate
		{
			CalendarWindow.ShowWindow(force: false);
			MatrixWindow.ShowWindow(force: false);
			HabitWindow.ShowWindow(force: false);
			AIWindow.ShowWindow(force: false);
			TaskWindow.ShowWindow(force: false);
			FocusWindow.ShowWindow(force: false);
			CountdownWindow.ShowWindow(force: false);
			TaskListWindow.ShowSavedWindows();
		});
	}

	public static void CloseAll()
	{
		CalendarWindow.Window?.Close();
		MatrixWindow.Window?.Close();
		HabitWindow.Window?.Close();
		AIWindow.Current?.Close();
		TaskWindow.Window?.Close();
		FocusWindow.Window?.Close();
		CountdownWindow.Window?.Close();
		TaskListWindow.CloseAllWindow();
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
			Toast(undoToast);
		}
		TryFocus();
	}

	public void TryToastString(object sender, string e)
	{
		WindowToastHelper.ToastString(ToastGrid, e);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		TaskService.BatchDeleteTasks(tasks, showUndo: true, 1, ToastGrid);
		TryFocus();
		return true;
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		Toast(undo);
		TryFocus();
	}

	private async void TryFocus()
	{
		if (!PopupStateManager.CanShowAddPopup())
		{
			await Task.Delay(200);
			if (!PopupStateManager.CanShowAddPopup())
			{
				return;
			}
		}
		if (base.IsActive)
		{
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
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

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
		if (undoModels != null && undoModels.Any())
		{
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(undoModels);
			ShowUndoToast(undoToast);
		}
	}

	public async Task ShowUndoToast(UndoToast undoToast)
	{
		Toast(undoToast);
		TryFocus();
	}

	public void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, new MoveToastControl(projectId, App.Window, taskName, moveType));
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, uiElement);
	}

	private void RemoveShortcut()
	{
		foreach (KeyValuePair<string, KeyBinding> keyBinding in _keyBindings)
		{
			KeyBindingManager.RemoveKeyBinding(keyBinding.Key, keyBinding.Value);
		}
	}

	private void InitShortcut()
	{
		_keyBindings.Add("SyncTask", GetKeyBinding(IndependentWindowCommand.SyncCommand));
		_keyBindings.Add("AddTask", GetKeyBinding(IndependentWindowCommand.InputCommand));
		_keyBindings.Add("Print", GetKeyBinding(IndependentWindowCommand.PrintCommand));
		_keyBindings.Add("SetNoPriority", GetKeyBinding(IndependentWindowCommand.SetPriorityNoneCommand));
		_keyBindings.Add("SetLowPriority", GetKeyBinding(IndependentWindowCommand.SetPriorityLowCommand));
		_keyBindings.Add("SetMediumPriority", GetKeyBinding(IndependentWindowCommand.SetPriorityMediumCommand));
		_keyBindings.Add("SetHighPriority", GetKeyBinding(IndependentWindowCommand.SetPriorityHighCommand));
		_keyBindings.Add("ClearDate", GetKeyBinding(IndependentWindowCommand.ClearDateCommand));
		_keyBindings.Add("SetToday", GetKeyBinding(IndependentWindowCommand.SetTodayCommand));
		_keyBindings.Add("SetTomorrow", GetKeyBinding(IndependentWindowCommand.SetTomorrowCommand));
		_keyBindings.Add("SetNextWeek", GetKeyBinding(IndependentWindowCommand.SetNextWeekCommand));
		_keyBindings.Add("DeleteTask", GetKeyBinding(IndependentWindowCommand.DeleteCommand));
		_keyBindings.Add("PinTask", GetKeyBinding(IndependentWindowCommand.PinTaskCommand));
		_keyBindings.Add("OpenSticky", GetKeyBinding(IndependentWindowCommand.OpenStickyCommand));
		_keyBindings.Add("OmListView", GetKeyBinding(IndependentWindowCommand.ListViewCommand));
		_keyBindings.Add("OmKanbanView", GetKeyBinding(IndependentWindowCommand.KanbanViewCommand));
		_keyBindings.Add("OmTimelineView", GetKeyBinding(IndependentWindowCommand.TimelineViewCommand));
		foreach (KeyValuePair<string, KeyBinding> keyBinding2 in _keyBindings)
		{
			KeyBindingManager.TryAddKeyBinding(keyBinding2.Key, keyBinding2.Value);
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

	public virtual void Print()
	{
	}

	public virtual void BatchSetPriorityCommand(int priority)
	{
	}

	public virtual void BatchSetDateCommand(DateTime? date)
	{
	}

	public virtual void BatchPinTaskCommand()
	{
	}

	public virtual void BatchOpenStickyCommand()
	{
	}

	public virtual void BatchDeleteCommand()
	{
	}

	private void OnWindowKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (!Utils.IfAltPressed())
		{
			SetAltKeyDown(down: false);
		}
		if (e.Key == Key.System && e.SystemKey == Key.LeftAlt)
		{
			e.Handled = true;
		}
		if (Utils.IfCtrlPressed() && e.Key == Key.Z)
		{
			TryUndo();
		}
	}

	public void TryUndo()
	{
		if (ToastGrid.Children.Count > 0 && ToastGrid.Children[0] is UndoToast { Visibility: Visibility.Visible } undoToast)
		{
			undoToast.OnUndo();
		}
	}

	public static bool CheckCount(Window owner)
	{
		int num = 0;
		if (MatrixWindow.IsShowing)
		{
			num++;
		}
		if (FocusWindow.IsShowing)
		{
			num++;
		}
		if (TaskWindow.IsShowing)
		{
			num++;
		}
		if (CalendarWindow.IsShowing)
		{
			num++;
		}
		if (HabitWindow.IsShowing)
		{
			num++;
		}
		if (CountdownWindow.IsShowing)
		{
			num++;
		}
		if (AIWindow.IsShowing)
		{
			num++;
		}
		num += TaskListWindow.Windows.Count;
		if (num < 5)
		{
			return true;
		}
		if (owner is IToastShowWindow toastShowWindow)
		{
			toastShowWindow.TryToastString(null, Utils.GetString("IndependentWindowLimit"));
		}
		return false;
	}

	public virtual void SwitchViewMode(string mode)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/independentwindow.xaml", UriKind.Relative);
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
			Window = (IndependentWindow)target;
			break;
		case 2:
			WindowBackground = (Border)target;
			break;
		case 3:
			Container = (Grid)target;
			break;
		case 4:
			Column1 = (ColumnDefinition)target;
			break;
		case 5:
			Column2 = (ColumnDefinition)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonDown += HeaderGridButtonDown;
			((Border)target).MouseLeftButtonUp += StopDragMove;
			((Border)target).MouseMove += OnDragMove;
			break;
		case 7:
			MinButton = (System.Windows.Controls.Button)target;
			MinButton.Click += MinButton_Click;
			break;
		case 8:
			MaxButton = (System.Windows.Controls.Button)target;
			MaxButton.Click += MaxButtonClick;
			break;
		case 9:
			NormalButton = (System.Windows.Controls.Button)target;
			NormalButton.Click += NormalButtonClick;
			break;
		case 10:
			CloseButton = (System.Windows.Controls.Button)target;
			CloseButton.Click += OnCloseButtonClick;
			CloseButton.MouseEnter += OnClickButtonMouseEnter;
			CloseButton.MouseLeave += OnClickButtonMouseLeave;
			break;
		case 11:
			X = (Polygon)target;
			break;
		case 12:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
