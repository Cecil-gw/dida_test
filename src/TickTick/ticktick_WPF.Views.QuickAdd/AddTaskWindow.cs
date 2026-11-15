using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.QuickAdd;

public class AddTaskWindow : Window, IComponentConnector
{
	public static readonly DependencyProperty TabSelectedIndexProperty = DependencyProperty.Register("TabSelectedIndex", typeof(int), typeof(AddTaskWindow), new PropertyMetadata(0, null));

	private readonly QuickAddView _taskTitle;

	private bool _imeDown;

	private bool _enterPressed;

	private int _saveAndNewTrigger;

	private bool _waitingCalendarDetail;

	private static ProjectIdentity _identity;

	private static double _top = -1.0;

	private static double _left = -1.0;

	private const double DefaultTopRatio = 0.3;

	private long _activeTime = -1L;

	internal AddTaskWindow Root;

	internal Grid QuickAddContainer;

	private bool _contentLoaded;

	public int TabSelectedIndex
	{
		get
		{
			return (int)GetValue(TabSelectedIndexProperty);
		}
		set
		{
			if (TabSelectedIndex != value)
			{
				SetValue(TabSelectedIndexProperty, value);
				OnIndexChanged();
			}
		}
	}

	private static AddTaskWindow globalInstance { get; set; }

	private static AddTaskWindow quickAddInstance { get; set; }

	public bool isGlobal { get; set; }

	public bool IsHide { get; set; }

	public AddTaskWindow()
	{
		InitializeComponent();
		_taskTitle = new QuickAddView(ProjectIdentity.GetDefaultProject(), QuickAddView.Scenario.QuickAdd);
		_taskTitle.GotFocus -= OnTitleGotFocus;
		_taskTitle.GotFocus += OnTitleGotFocus;
		_taskTitle.BatchTaskAdded -= OnBatchTaskAdded;
		_taskTitle.BatchTaskAdded += OnBatchTaskAdded;
		_taskTitle.CalendarDetailOpened -= OnCalendarDetailOpened;
		_taskTitle.CalendarDetailOpened += OnCalendarDetailOpened;
		_taskTitle.CalendarDetailRestored -= OnCalendarDetailRestored;
		_taskTitle.CalendarDetailRestored += OnCalendarDetailRestored;
		_taskTitle.CalendarEventAdded -= OnCalendarEventAdded;
		_taskTitle.CalendarEventAdded += OnCalendarEventAdded;
		_taskTitle.VerticalAlignment = VerticalAlignment.Top;
		_taskTitle.MinHeight = 60.0;
		QuickAddContainer.Children.Add(_taskTitle);
		base.PreviewKeyUp += OnPreviewKeyUp;
		_taskTitle.OperationPanel.Visibility = Visibility.Visible;
		_taskTitle.TaskAdded += delegate
		{
			if (isGlobal)
			{
				globalInstance = null;
			}
			else
			{
				quickAddInstance = null;
			}
			Close();
			base.Owner?.Activate();
			if (Utils.IfCtrlPressed() && Utils.IfShiftPressed() && _enterPressed)
			{
				DoShowAddTaskWindow(base.Owner, isGlobal, _identity, forceCreate: true);
			}
			else if (!isGlobal)
			{
				_identity = null;
			}
			_enterPressed = false;
		};
		_taskTitle.TitleText.EditBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
		_taskTitle.TitleText.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
		_taskTitle.TitleText.EditBox.WordWrap = true;
		_taskTitle.TitleText.TextChanged -= OnTextChanged;
		_taskTitle.TitleText.TextChanged += OnTextChanged;
		_taskTitle.VerticalAlignment = VerticalAlignment.Top;
		_taskTitle.AddTaskGrid.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
		_taskTitle.InitWindowStyle();
		UserActCollectUtils.AddClickEvent("add_task", "add_dialog", "in_app_dailog");
		base.PreviewKeyDown -= OnKeyDown;
		base.PreviewKeyDown += OnKeyDown;
	}

	private void OnTextChanged(object sender, string e)
	{
		_saveAndNewTrigger = 0;
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.ImeProcessed:
			_imeDown = true;
			break;
		case Key.Return:
			if (Utils.IfShiftPressed() && Utils.IfCtrlPressed())
			{
				_enterPressed = true;
			}
			break;
		}
	}

	private void OnBatchTaskAdded(object sender, List<TaskModel> e)
	{
		if (isGlobal)
		{
			globalInstance = null;
		}
		else
		{
			quickAddInstance = null;
		}
		Close();
		base.Owner?.Activate();
	}

	private void OnCalendarDetailOpened(object sender, EventArgs e)
	{
		_waitingCalendarDetail = true;
		IsHide = true;
		Hide();
	}

	private void OnCalendarDetailRestored(object sender, EventArgs e)
	{
		_waitingCalendarDetail = false;
		IsHide = false;
		if (!base.IsVisible)
		{
			Show();
		}
		Activate();
		if (TabSelectedIndex != 0)
		{
			TabSelectedIndex = 0;
		}
		else
		{
			_taskTitle.FocusText();
		}
	}

	private void OnCalendarEventAdded(object sender, CalendarEventModel e)
	{
		_waitingCalendarDetail = false;
		if (isGlobal)
		{
			globalInstance = null;
		}
		else
		{
			quickAddInstance = null;
		}
		Close();
		base.Owner?.Activate();
	}

	private void OnPreviewKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape && !_imeDown)
		{
			HideWindow();
		}
		_imeDown = false;
	}

	private void OnIndexChanged()
	{
		if (TabSelectedIndex > 0)
		{
			Keyboard.ClearFocus();
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
		else
		{
			_taskTitle.FocusText();
		}
	}

	private void HideWindow()
	{
		Hide();
		base.Owner?.Activate();
	}

	protected override async void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);
		await Task.Delay(10);
		if (!_waitingCalendarDetail)
		{
			if (new TimeSpan(DateTime.Now.Ticks - _activeTime).Milliseconds > 200 && !_taskTitle.IsInOperation)
			{
				IsHide = true;
				_taskTitle?.TryEnlargeScrollContent();
				HideWindow();
			}
			else if (!_taskTitle.Model.UseDetailStyle)
			{
				_taskTitle.FocusText();
			}
		}
	}

	protected override async void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		await Task.Delay(10);
		_activeTime = DateTime.Now.Ticks;
		ApplyWindowLocation();
	}

	public static async void ShowWindow(Window owner, ProjectIdentity identity)
	{
		bool flag = owner == null;
		if (flag)
		{
			if (globalInstance == null)
			{
				DoShowAddTaskWindow(owner, flag, identity);
				return;
			}
			AddTaskWindow addTaskWindow = globalInstance;
			if (addTaskWindow.IsHide)
			{
				addTaskWindow._waitingCalendarDetail = false;
				addTaskWindow.IsHide = false;
				addTaskWindow.Show();
				addTaskWindow.Activate();
				addTaskWindow._taskTitle.FocusText();
			}
			else
			{
				addTaskWindow.Hide();
				addTaskWindow.IsHide = true;
			}
		}
		else if (quickAddInstance == null)
		{
			DoShowAddTaskWindow(owner, flag, identity);
		}
		else
		{
			AddTaskWindow addTaskWindow2 = quickAddInstance;
			UpdateAddTaskWindowProject(addTaskWindow2, identity);
			if (addTaskWindow2.IsHide)
			{
				addTaskWindow2._waitingCalendarDetail = false;
				addTaskWindow2.IsHide = false;
			}
			addTaskWindow2.Show();
			addTaskWindow2.Activate();
			addTaskWindow2._taskTitle.FocusText();
		}
	}

	private static void UpdateAddTaskWindowProject(AddTaskWindow window, ProjectIdentity identity, bool forceCreate = false)
	{
		if (forceCreate || !(_identity?.Id == identity?.Id))
		{
			_identity = identity;
			if (string.IsNullOrEmpty(identity.GetProjectId()))
			{
				identity = ProjectIdentity.GetDefaultProject();
			}
			AddTaskViewModel addTaskViewModel = AddTaskViewModel.Build(identity, null, useInKanban: false, allowUseDetailMode: true, QuickAddView.Scenario.QuickAdd);
			addTaskViewModel.UseDetailStyle = true;
			addTaskViewModel.BorderThickness = new Thickness(0.0, 0.0, 0.0, 0.0);
			window._taskTitle.Reset(addTaskViewModel, QuickAddView.Scenario.QuickAdd);
		}
	}

	private static void DoShowAddTaskWindow(Window owner, bool isGlobal, ProjectIdentity identity, bool forceCreate = false)
	{
		AddTaskWindow addTaskWindow = new AddTaskWindow
		{
			Owner = owner
		};
		addTaskWindow.isGlobal = isGlobal;
		addTaskWindow.WindowStartupLocation = WindowStartupLocation.Manual;
		addTaskWindow._taskTitle.InitGlobalAddWindowStyle();
		if (!isGlobal)
		{
			UpdateAddTaskWindowProject(addTaskWindow, identity, forceCreate);
		}
		addTaskWindow.ApplyWindowLocation();
		addTaskWindow.Show();
		addTaskWindow.Activate();
		addTaskWindow.TabSelectedIndex = 0;
		addTaskWindow.IsHide = false;
		addTaskWindow._taskTitle.FocusText();
		if (isGlobal)
		{
			globalInstance = addTaskWindow;
		}
		else
		{
			quickAddInstance = addTaskWindow;
		}
	}

	private void ApplyWindowLocation()
	{
		if (base.Owner == null)
		{
			ApplyGlobalWindowLocation();
		}
		else
		{
			ApplyOwnerWindowLocation();
		}
	}

	private void ApplyGlobalWindowLocation()
	{
		if (Math.Abs(_left - -1.0) > 0.1)
		{
			Matrix? transform = (PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice;
			Point defaultGlobalWindowPoint = GetDefaultGlobalWindowPoint();
			Point pomoLocationSafely = WidgetLocationHelper.GetPomoLocationSafely(_left, _top, 760.0, 100.0, transform, defaultGlobalWindowPoint);
			base.Left = pomoLocationSafely.X;
			base.Top = pomoLocationSafely.Y;
		}
		else
		{
			Point defaultGlobalWindowPoint2 = GetDefaultGlobalWindowPoint();
			base.Left = defaultGlobalWindowPoint2.X;
			base.Top = defaultGlobalWindowPoint2.Y;
		}
	}

	private void ApplyOwnerWindowLocation()
	{
		Point defaultOwnerWindowPoint = GetDefaultOwnerWindowPoint();
		Matrix? transform = (PresentationSource.FromVisual(base.Owner)?.CompositionTarget)?.TransformFromDevice;
		Point locationWithinWorkingAreaSafely = WidgetLocationHelper.GetLocationWithinWorkingAreaSafely(defaultOwnerWindowPoint.X, defaultOwnerWindowPoint.Y, GetWindowWidth(), GetWindowHeight(), transform);
		base.Left = locationWithinWorkingAreaSafely.X;
		base.Top = locationWithinWorkingAreaSafely.Y;
	}

	private static Point GetDefaultGlobalWindowPoint()
	{
		Rect workArea = SystemParameters.WorkArea;
		return new Point(workArea.Left + Math.Max(0.0, (workArea.Width - 640.0) / 2.0), workArea.Top + Math.Max(0.0, workArea.Height * 0.3));
	}

	private Point GetDefaultOwnerWindowPoint()
	{
		double ownerWidth = GetOwnerWidth();
		double ownerHeight = GetOwnerHeight();
		Point point = new Point(base.Owner.Left, base.Owner.Top);
		CompositionTarget compositionTarget = PresentationSource.FromVisual(base.Owner)?.CompositionTarget;
		if (compositionTarget != null)
		{
			point = compositionTarget.TransformFromDevice.Transform(base.Owner.PointToScreen(default(Point)));
		}
		return new Point(point.X + Math.Max(0.0, (ownerWidth - GetWindowWidth()) / 2.0), point.Y + Math.Max(0.0, ownerHeight * 0.3));
	}

	private double GetOwnerWidth()
	{
		if (base.Owner.ActualWidth > 0.0)
		{
			return base.Owner.ActualWidth;
		}
		if (double.IsNaN(base.Owner.Width) || !(base.Owner.Width > 0.0))
		{
			return SystemParameters.WorkArea.Width;
		}
		return base.Owner.Width;
	}

	private double GetOwnerHeight()
	{
		if (base.Owner.ActualHeight > 0.0)
		{
			return base.Owner.ActualHeight;
		}
		if (double.IsNaN(base.Owner.Height) || !(base.Owner.Height > 0.0))
		{
			return SystemParameters.WorkArea.Height;
		}
		return base.Owner.Height;
	}

	private double GetWindowWidth()
	{
		if (base.ActualWidth > 0.0)
		{
			return base.ActualWidth;
		}
		if (double.IsNaN(base.Width) || !(base.Width > 0.0))
		{
			return 640.0;
		}
		return base.Width;
	}

	private double GetWindowHeight()
	{
		if (base.ActualHeight > 0.0)
		{
			return base.ActualHeight;
		}
		if (double.IsNaN(base.Height) || !(base.Height > 0.0))
		{
			return 100.0;
		}
		return base.Height;
	}

	private void OnTitleGotFocus(object sender, RoutedEventArgs e)
	{
		TabSelectedIndex = 0;
	}

	private void TryDragWindow(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			double top = base.Top;
			double left = base.Left;
			DragMove();
			if ((Math.Abs(base.Top - top) > 4.0 || Math.Abs(base.Left - left) > 4.0) && base.Owner == null)
			{
				_top = base.Top;
				_left = base.Left;
			}
		}
	}

	private void Root_Closing(object sender, CancelEventArgs e)
	{
		if (isGlobal)
		{
			globalInstance = null;
		}
		else
		{
			quickAddInstance = null;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/addtaskwindow.xaml", UriKind.Relative);
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
			Root = (AddTaskWindow)target;
			Root.Closing += Root_Closing;
			break;
		case 2:
			QuickAddContainer = (Grid)target;
			break;
		case 3:
			((Grid)target).MouseMove += TryDragWindow;
			break;
		case 4:
			((Grid)target).MouseMove += TryDragWindow;
			break;
		case 5:
			((Grid)target).MouseMove += TryDragWindow;
			break;
		case 6:
			((Grid)target).MouseMove += TryDragWindow;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
