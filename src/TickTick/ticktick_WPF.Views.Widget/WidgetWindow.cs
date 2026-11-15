using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shell;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.Win32;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Widget;

public class WidgetWindow : MyWindow, IComponentConnector
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public readonly ProjectWidget ProjectWidget;

	public readonly CalendarWidget CalendarWidget;

	public readonly MatrixWidget MatrixWidget;

	public readonly CountDownListWidget CountDownListWidget;

	public readonly CountDownItemWidget CountDownItemWidget;

	public readonly HabitItemWidget HabitItemWidget;

	public readonly HabitListWiget HabitListWiget;

	public readonly HabitWeekWidget HabitWeekWidget;

	private readonly WidgetBaseUserControl _widgetChild;

	private Storyboard _hideAnim;

	private Storyboard _showAnim;

	private Storyboard _popInLeftAnim;

	private Storyboard _popInRightAnim;

	private Storyboard _popoutLeftAnim;

	private Storyboard _popoutRightAnim;

	private WidgetShowStatus _showState;

	private readonly DelayActionHandler _modelSaver = new DelayActionHandler(1000);

	private int _mouseMoveOverCount;

	private readonly System.Timers.Timer _statusCheckTimer = new System.Timers.Timer(150.0);

	private readonly DelayActionHandler _popoutDelayAction = new DelayActionHandler(200);

	private int _oldWindowLong;

	private WidgetLocationModel _location = new WidgetLocationModel();

	private IntPtr _desktopPointer;

	private bool _dragMouseDown;

	private bool _moveWindow;

	private bool _altKeyDown;

	private bool _noResize;

	private readonly WidgetDesktopWindowBehavior _desktopWindowBehavior;

	private const int WM_SIZING = 532;

	internal WidgetWindow ProjectWindow;

	internal Grid MainGrid;

	internal Grid Container;

	internal Grid CollapseGrid;

	internal Grid StickLeftGrid;

	internal Grid StickRightGrid;

	private bool _contentLoaded;

	public WidgetViewModel Model => (WidgetViewModel)base.DataContext;

	private Thickness withShadowMargin { get; } = new Thickness(10.0);

	private Thickness withoutShadowMargin { get; } = new Thickness(0.0);

	private Thickness resizeNormalMargin { get; } = new Thickness(5.0);

	private Thickness resizePaddingMargin { get; } = new Thickness(12.0);

	private Thickness resizeHideMargin { get; } = new Thickness(2.0);

	private float _lastOpacity { get; set; }

	public WidgetWindow(WidgetViewModel model)
	{
		ThemeUtil.SetTheme(model.ThemeId, this);
		InitializeComponent();
		Utils.SetWindowChrome(this, resizeNormalMargin);
		base.DataContext = model;
		_lastOpacity = model.Opacity;
		System.Windows.Size? minSize = WidgetConstants.GetMinSize(model.WidgetType, model.Opacity == 1f);
		if (minSize.HasValue)
		{
			base.MinHeight = minSize.Value.Height;
			base.MinWidth = minSize.Value.Width;
		}
		System.Windows.Size? maxSize = WidgetConstants.GetMaxSize(model.WidgetType, model.Opacity == 1f);
		if (maxSize.HasValue)
		{
			base.MaxHeight = maxSize.Value.Height;
			base.MaxWidth = maxSize.Value.Width;
		}
		if (model.IsSingleMode)
		{
			base.Title = Utils.GetString("GridWidget");
			switch (model.WidgetType)
			{
			case WidgetType.Calendar:
				CalendarWidget = new CalendarWidget(model);
				_widgetChild = CalendarWidget;
				Container.Children.Clear();
				Container.Children.Add(CalendarWidget);
				break;
			case WidgetType.Matrix:
				MatrixWidget = new MatrixWidget(model);
				_widgetChild = MatrixWidget;
				Container.Children.Clear();
				Container.Children.Add(MatrixWidget);
				model.CollapseTitle = Utils.GetString("Matrix");
				break;
			case WidgetType.CountdownList:
				CountDownListWidget = new CountDownListWidget(model);
				_widgetChild = CountDownListWidget;
				Container.Children.Clear();
				Container.Children.Add(CountDownListWidget);
				model.CollapseTitle = Utils.GetString("CountdownTitle");
				break;
			case WidgetType.HabitList:
				HabitListWiget = new HabitListWiget(model);
				_widgetChild = HabitListWiget;
				Container.Children.Clear();
				Container.Children.Add(HabitListWiget);
				model.CollapseTitle = Utils.GetString("Habit");
				break;
			}
		}
		else
		{
			switch (model.WidgetType)
			{
			case WidgetType.CountdownItem:
				CountDownItemWidget = new CountDownItemWidget(model);
				_widgetChild = CountDownItemWidget;
				Container.Children.Clear();
				Container.Children.Add(CountDownItemWidget);
				model.CollapseTitle = Utils.GetString("CountdownTitle");
				break;
			case WidgetType.HabitItem:
				HabitItemWidget = new HabitItemWidget(model);
				_widgetChild = HabitItemWidget;
				Container.Children.Clear();
				Container.Children.Add(HabitItemWidget);
				_noResize = true;
				model.CollapseTitle = Utils.GetString("Habit");
				break;
			case WidgetType.HabitWeekItem:
				HabitWeekWidget = new HabitWeekWidget(model);
				_widgetChild = HabitWeekWidget;
				Container.Children.Clear();
				Container.Children.Add(HabitWeekWidget);
				_noResize = true;
				model.CollapseTitle = Utils.GetString("Habit");
				break;
			default:
				ProjectWidget = new ProjectWidget(model);
				_widgetChild = ProjectWidget;
				Container.Children.Clear();
				Container.Children.Add(ProjectWidget);
				break;
			}
		}
		if (_noResize)
		{
			base.ResizeMode = ResizeMode.NoResize;
		}
		InitSizeAndLocation();
		_desktopWindowBehavior = WidgetDesktopWindowBehavior.Attach(this, () => Model.WidgetType.ToString(), () => Model.DisplayOption);
		base.Loaded += OnWidgetLoaded;
		UtilLog.Info($"ShowWidget: isSingleMode {model.IsSingleMode} WidgetType " + model.WidgetType);
		UpdateModelViewThemeId();
		base.Resources["IsDarkTheme"] = model.ViewThemeId == "dark";
		SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
		TimeChangeNotifier.DayChanged += TimeChangeNotifier_DayChanged;
	}

	private void TimeChangeNotifier_DayChanged(object sender, EventArgs e)
	{
		if (CalendarWidget != null)
		{
			CalendarWidget.CalendarControl.OnCurrentDateChanged();
			RefreshCalendarCollapseTitle();
		}
	}

	private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
	{
		if (Model.ThemeId == "auto")
		{
			bool flag = ThemeUtil.IsSystemDark();
			if (Model.ViewThemeId == "dark" != flag)
			{
				UpdateModelViewThemeId();
				SetTheme(Model.ThemeId);
				_widgetChild?.OnThemeChange(Model.ViewThemeId);
			}
		}
	}

	private void UpdateModelViewThemeId()
	{
		switch (Model.ThemeId)
		{
		case "dark":
		case "light":
			Model.ViewThemeId = Model.ThemeId;
			break;
		case "auto":
			Model.ViewThemeId = (ThemeUtil.IsSystemDark() ? "dark" : "light");
			break;
		}
	}

	private void UpdateDisplayOption()
	{
		_desktopWindowBehavior.UpdateDisplayOption();
		if (!base.Topmost)
		{
			switch (_location.HideType)
			{
			case HideType.Top:
				ShowWindow();
				break;
			case HideType.Left:
				PopoutLeft();
				break;
			case HideType.Right:
				PopoutRight();
				break;
			}
		}
	}

	public static int SetDesktopOwnerCompatibility(bool disabled)
	{
		return WidgetDesktopWindowBehavior.SetDesktopOwnerCompatibility(disabled);
	}

	private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 126 && Screen.AllScreens.Length > 1)
		{
			DelayActionHandlerCenter.TryDoAction("DelayRestart", delegate
			{
				App.Instance.Restart();
			}, 1000);
		}
		return IntPtr.Zero;
	}

	public void ShowWidget()
	{
		Show();
	}

	public void CloseWidget()
	{
		if (_widgetChild is IToastShowWindow toastShowWindow && Utils.ToastWindow == toastShowWindow)
		{
			Utils.ToastWindow = null;
		}
		DisposeDelaySaver();
		Close();
		lock (_statusCheckTimer)
		{
			_statusCheckTimer.Close();
			_statusCheckTimer.Dispose();
		}
	}

	private void InitSizeAndLocation()
	{
		if (Model != null)
		{
			base.Width = Model.Width;
			base.Height = Model.Height;
			base.Left = Model.Left;
			base.Top = Model.Top;
		}
	}

	private async void OnWidgetLoaded(object sender, EventArgs e)
	{
		HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
		WindowHelper.MoveTo(this, (int)Model.Left, (int)Model.Top);
		base.Left = Model.Left;
		base.Top = Model.Top;
		Matrix? transform = (PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice;
		_location = WidgetLocationHelper.GetLocationSafely(base.Left, base.Top, base.Width, base.Height, transform);
		UpdateBlurByShowState(show: true);
		if (_location.HideType == HideType.Reset)
		{
			await Task.Delay(5000);
			_location = WidgetLocationHelper.GetLocationSafely(base.Left, base.Top, base.Width, base.Height, transform);
		}
		_location.ShowShadow = Model.Opacity == 1f;
		InitStatusCheckTimer();
		InitDelaySaver();
		if (Model.DisplayOption == "top")
		{
			SetHideLocation();
		}
		else if (Model.DisplayOption != "top" && _location.HideType != HideType.None)
		{
			switch (_location.HideType)
			{
			case HideType.Top:
				_location.Top = _location.HideTopIn + _location.Height;
				break;
			case HideType.Left:
				_location.Left += _location.HideLeftIn + _location.Width;
				break;
			case HideType.Right:
				_location.Left = _location.HideRightIn - _location.Width;
				break;
			}
			base.Top = _location.Top;
			base.Left = _location.Left;
		}
		UtilLog.Info(string.Format("WidgetStartLocation top: {0} left: {1} isHide: {2}", base.Top, base.Left, Model.DisplayOption == "top"));
		RefreshCalendarCollapseTitle();
	}

	private void RefreshCalendarCollapseTitle()
	{
		if (CalendarWidget != null)
		{
			Model.CollapseTitle = DateUtils.FormatMonth(CalendarWidget.CalendarControl.HeadView.GetCurrentMonthDate());
		}
	}

	private void SetHideLocation()
	{
		Container.BeginAnimation(UIElement.OpacityProperty, null);
		if (_location.HideType == HideType.Top)
		{
			ResetOnHidden();
			CollapseGrid.BeginAnimation(UIElement.OpacityProperty, null);
			CollapseGrid.Opacity = 1.0;
			base.Top = _location.HideTopIn + 20.0;
			Container.Opacity = 0.0;
			return;
		}
		if (_location.HideType == HideType.Left)
		{
			ResetOnHidden();
			StickLeftGrid.BeginAnimation(UIElement.OpacityProperty, null);
			StickLeftGrid.Opacity = 1.0;
			base.Left = _location.HideLeftIn + 11.0;
			Container.Opacity = 0.0;
			return;
		}
		if (_location.HideType == HideType.Right)
		{
			ResetOnHidden();
			StickRightGrid.BeginAnimation(UIElement.OpacityProperty, null);
			StickRightGrid.Opacity = 1.0;
			base.Left = _location.HideRightIn - 11.0;
			Container.Opacity = 0.0;
			return;
		}
		Container.Opacity = 1.0;
		StickRightGrid.BeginAnimation(UIElement.OpacityProperty, null);
		StickRightGrid.Opacity = 0.0;
		StickLeftGrid.BeginAnimation(UIElement.OpacityProperty, null);
		StickLeftGrid.Opacity = 0.0;
		CollapseGrid.BeginAnimation(UIElement.OpacityProperty, null);
		CollapseGrid.Opacity = 0.0;
		ResetOnShown();
		if (_location.HideType == HideType.Reset)
		{
			base.Left = _location.Left;
			base.Top = _location.Top;
		}
	}

	private void InitStatusCheckTimer()
	{
		_statusCheckTimer.Elapsed -= OnCheckMouseOver;
		_statusCheckTimer.Elapsed += OnCheckMouseOver;
	}

	private void OnCheckMouseOver(object sender, ElapsedEventArgs e)
	{
		base.Dispatcher?.InvokeAsync(CheckStatus, DispatcherPriority.Background);
	}

	private void CheckStatus()
	{
		TryHideWindow();
	}

	private void InitDelaySaver()
	{
		_modelSaver.SetAction(OnSizeOrLocChanged);
	}

	private void DisposeDelaySaver()
	{
		_modelSaver.StopAndClear();
	}

	private void OnSizeOrLocChanged(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			ResetLocation();
			UtilLog.Info(string.Format("WidgetChangeLocation top: {0} left: {1} isHide: {2}", base.Top, base.Left, Model.DisplayOption == "top"));
		});
	}

	private void ResetLocation()
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			if ((_location.HideType == HideType.Left && Math.Abs(base.Left - (_location.HideLeftIn + 11.0)) > 1.0) || (_location.HideType == HideType.Top && Math.Abs(base.Top - (_location.HideTopIn + 20.0)) > 1.0) || (_location.HideType == HideType.Right && Math.Abs(base.Left - (_location.HideRightIn - 11.0)) > 1.0))
			{
				Matrix? transform = (PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice;
				_location = WidgetLocationHelper.GetLocationSafely(base.Left, base.Top, base.Width, base.Height, transform);
				_location.ShowShadow = Model.Opacity == 1f;
				SetHideLocation();
				return;
			}
		}
		else
		{
			ResetOnShown();
			Container.BeginAnimation(UIElement.OpacityProperty, null);
			Container.Opacity = 1.0;
		}
		Model.Top = base.Top;
		Model.Left = base.Left;
		Model.Width = base.Width;
		Model.Height = base.Height;
		_location.Height = base.Height;
		_location.Width = base.Width;
		_widgetChild.Save();
	}

	private void TryHideWindow()
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		int windowLong = NativeUtils.GetWindowLong(handle, -16);
		NativeUtils.SetWindowLong(handle, WinParameter.GWL_STYLE, windowLong & -65537 & -131073);
		if (_location.HideType == HideType.None || !(Model.DisplayOption == "top") || !Model.AutoHide || _showState != WidgetShowStatus.Normal || _widgetChild.IsEditing())
		{
			return;
		}
		double num = ((PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice)?.M11 ?? 1.0;
		System.Windows.Point mousePosition = GetMousePosition();
		double num2 = mousePosition.X * num;
		double num3 = mousePosition.Y * num;
		if (!(num2 - 5.0 <= base.Left + base.ActualWidth) || !(num2 + 5.0 >= base.Left) || !(num3 + 5.0 >= base.Top) || !(num3 - 5.0 <= base.Top + base.ActualHeight))
		{
			_mouseMoveOverCount++;
		}
		else
		{
			_mouseMoveOverCount = 0;
		}
		if (_mouseMoveOverCount >= 3 && !PopupStateManager.IsViewPopOpened())
		{
			_mouseMoveOverCount = 0;
			switch (_location.HideType)
			{
			case HideType.Top:
				HideWindow();
				break;
			case HideType.Left:
				PopinLeft();
				break;
			case HideType.Right:
				PopinRight();
				break;
			}
			StopOrStartAutoHide(stop: true);
		}
	}

	private void StopOrStartAutoHide(bool stop)
	{
		lock (_statusCheckTimer)
		{
			try
			{
				if (stop)
				{
					_statusCheckTimer.Stop();
				}
				else
				{
					_statusCheckTimer.Start();
				}
			}
			catch (Exception)
			{
			}
		}
	}

	private static System.Windows.Point GetMousePosition()
	{
		System.Drawing.Point mousePosition = System.Windows.Forms.Control.MousePosition;
		return new System.Windows.Point(mousePosition.X, mousePosition.Y);
	}

	public async void SetTopMost()
	{
		if (Model.DisplayOption != "top" && _location.HideType != HideType.None)
		{
			switch (_location.HideType)
			{
			case HideType.Top:
				Model.Top = _location.TopOut;
				break;
			case HideType.Left:
				Model.Left = _location.LeftOut;
				break;
			case HideType.Right:
				Model.Left = _location.RightOut;
				break;
			}
			await Model.Save();
		}
		UpdateDisplayOption();
	}

	public void OnOpacityChange(float opacity)
	{
		UpdateBlurShadowSize(opacity);
	}

	private void UpdateBlurByShowState(bool show)
	{
		if (show)
		{
			UpdateBlur(Model.Opacity);
			UpdateShadow(Model.Opacity);
		}
		else
		{
			EnableBlur(enable: false);
			UpdateResizeThickness();
		}
	}

	private void UpdateShadow(float opacity)
	{
		if (opacity == 1f)
		{
			EnableBlur(enable: false);
			MainGrid.Effect = TryFindResource("WidgetShadowEffect") as DropShadowEffect;
			MainGrid.Margin = withShadowMargin;
		}
		else
		{
			bool enable = opacity > 0f;
			EnableBlur(enable);
			MainGrid.Effect = null;
			MainGrid.Margin = withoutShadowMargin;
		}
		UpdateResizeThickness();
	}

	private void UpdateBlur(float opacity)
	{
		if (opacity == 1f)
		{
			EnableBlur(enable: false);
			return;
		}
		bool enable = opacity > 0f;
		EnableBlur(enable);
	}

	private void UpdateBlurShadowSize(float opacity)
	{
		bool num = opacity != _lastOpacity && (opacity == 1f || _lastOpacity == 1f);
		_lastOpacity = opacity;
		UpdateBlur(opacity);
		if (!num)
		{
			return;
		}
		UpdateShadow(opacity);
		try
		{
			bool flag = opacity == 1f;
			base.Visibility = Visibility.Hidden;
			System.Windows.Size size = new System.Windows.Size(base.Width, base.Height);
			System.Windows.Size? minSize = WidgetConstants.GetMinSize(Model.WidgetType, flag);
			if (minSize.HasValue)
			{
				base.MinHeight = minSize.Value.Height;
				base.MinWidth = minSize.Value.Width;
			}
			System.Windows.Size? maxSize = WidgetConstants.GetMaxSize(Model.WidgetType, flag);
			if (maxSize.HasValue)
			{
				base.MaxHeight = maxSize.Value.Height;
				base.MaxWidth = maxSize.Value.Width;
			}
			double num2 = 10.0;
			if (flag)
			{
				base.Width = size.Width + num2 * 2.0;
				base.Height = size.Height + num2 * 2.0;
				base.Top -= num2;
				base.Left -= num2;
			}
			else
			{
				base.Width = size.Width - num2 * 2.0;
				base.Height = size.Height - num2 * 2.0;
				base.Top += num2;
				base.Left += num2;
			}
			_location.ShowShadow = flag;
		}
		catch (Exception ex)
		{
			UtilLog.Error("UpdateBlurShadowSize Error:" + ex);
		}
		base.Visibility = Visibility.Visible;
	}

	private void UpdateResizeThickness()
	{
		System.Windows.Shell.WindowChrome windowChrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
		if (windowChrome != null)
		{
			if (_showState == WidgetShowStatus.Hidden)
			{
				windowChrome.ResizeBorderThickness = resizeHideMargin;
			}
			else if (Model.Opacity == 1f)
			{
				windowChrome.ResizeBorderThickness = resizePaddingMargin;
			}
			else
			{
				windowChrome.ResizeBorderThickness = resizeNormalMargin;
			}
		}
	}

	public void EnableBlur(bool enable = true)
	{
		NativeUtils.EnableBlur(enable, this);
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
		if (e.Key == Key.F5)
		{
			ProjectWidget?.SyncTask();
		}
		CalendarWidget?.CalendarControl.OnKeyUp(sender, e);
		if (Utils.IfCtrlPressed() && e.Key == Key.Z)
		{
			CalendarWidget?.TryUndo();
		}
	}

	protected override void OnDeactivated(EventArgs e)
	{
		CheckAltKeyDown();
		base.OnDeactivated(e);
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
		string text = ((ProjectWidget != null) ? "in_task_tab" : ((CalendarWidget != null) ? "in_calendar_tab" : ((MatrixWidget != null) ? "in_matrix_tab" : null)));
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

	private void OnCollapseGridEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			_popoutDelayAction.SetAction(delegate
			{
				Utils.RunOnUiThread(base.Dispatcher, ShowWindow);
			});
			_popoutDelayAction.TryDoAction();
		}
	}

	private void OnPopLeftGridEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			_popoutDelayAction.SetAction(delegate
			{
				Utils.RunOnUiThread(base.Dispatcher, PopoutLeft);
			});
			_popoutDelayAction.TryDoAction();
		}
	}

	private void OnPopoutGridLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		_popoutDelayAction.CancelAction();
	}

	private void OnPopRightGridEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			_popoutDelayAction.SetAction(delegate
			{
				Utils.RunOnUiThread(base.Dispatcher, PopoutRight);
			});
			_popoutDelayAction.TryDoAction();
		}
	}

	private void InitPopinRightAnim()
	{
		_popInRightAnim = (Storyboard)FindResource("PopInRightStoryBoard");
		if (_popInRightAnim.Children[0] is DoubleAnimation doubleAnimation)
		{
			doubleAnimation.To = _location.HideRightIn;
		}
		if (_popInRightAnim.Children[1] is DoubleAnimation doubleAnimation2)
		{
			doubleAnimation2.To = _location.HideRightIn - 11.0;
		}
		_popInRightAnim.Completed -= OnPopRightHidden;
		_popInRightAnim.Completed += OnPopRightHidden;
	}

	private void OnPopRightHidden(object sender, EventArgs e)
	{
		BeginAnimation(Window.LeftProperty, null);
		base.Left = _location.HideRightIn - 11.0;
		ResetOnHidden();
		_popInRightAnim = null;
	}

	private void InitPopoutRightAnim()
	{
		_popoutRightAnim = (Storyboard)FindResource("PopOutRightStoryBoard");
		if (_popoutRightAnim.Children[0] is DoubleAnimation doubleAnimation)
		{
			doubleAnimation.To = _location.RightOut;
		}
		_popoutRightAnim.Completed -= OnPopRightShown;
		_popoutRightAnim.Completed += OnPopRightShown;
	}

	private void OnPopRightShown(object sender, EventArgs e)
	{
		BeginAnimation(Window.LeftProperty, null);
		base.Left = _location.RightOut;
		ResetOnShown();
		_popoutRightAnim = null;
	}

	private void InitPopinLeftAnim()
	{
		_popInLeftAnim = (Storyboard)FindResource("PopInLeftStoryBoard");
		if (_popInLeftAnim.Children[0] is DoubleAnimation doubleAnimation)
		{
			doubleAnimation.To = _location.HideLeftIn;
		}
		if (_popInLeftAnim.Children[1] is DoubleAnimation doubleAnimation2)
		{
			doubleAnimation2.To = _location.HideLeftIn + 11.0;
		}
		_popInLeftAnim.Completed -= OnPopLeftHidden;
		_popInLeftAnim.Completed += OnPopLeftHidden;
	}

	private void OnPopLeftHidden(object sender, EventArgs e)
	{
		BeginAnimation(Window.LeftProperty, null);
		base.Left = _location.HideLeftIn + 11.0;
		ResetOnHidden();
		_popInLeftAnim = null;
	}

	private void InitPopoutLeftAnim()
	{
		_popoutLeftAnim = (Storyboard)FindResource("PopOutLeftStoryBoard");
		if (_popoutLeftAnim.Children[0] is DoubleAnimation doubleAnimation)
		{
			doubleAnimation.To = _location.LeftOut;
		}
		_popoutLeftAnim.Completed -= OnPopLeftShown;
		_popoutLeftAnim.Completed += OnPopLeftShown;
	}

	private void OnPopLeftShown(object sender, EventArgs e)
	{
		BeginAnimation(Window.LeftProperty, null);
		base.Left = _location.LeftOut;
		ResetOnShown();
		_popoutLeftAnim = null;
	}

	private void InitHideWindowAnim()
	{
		_hideAnim = (Storyboard)FindResource("HideStoryBoard");
		if (_hideAnim.Children[0] is DoubleAnimation doubleAnimation)
		{
			doubleAnimation.To = _location.HideTopIn;
		}
		if (_hideAnim.Children[1] is DoubleAnimation doubleAnimation2)
		{
			doubleAnimation2.To = _location.HideTopIn + 20.0;
		}
		_hideAnim.Completed -= OnWindowHidden;
		_hideAnim.Completed += OnWindowHidden;
	}

	private void OnWindowHidden(object sender, EventArgs e)
	{
		BeginAnimation(Window.TopProperty, null);
		base.Top = _location.HideTopIn + 20.0;
		ResetOnHidden();
		_hideAnim = null;
	}

	private void InitShowWindowAnim()
	{
		_showAnim = (Storyboard)FindResource("ShowStoryBoard");
		if (_showAnim.Children[0] is DoubleAnimation doubleAnimation)
		{
			doubleAnimation.To = _location.TopOut;
		}
		_showAnim.Completed -= OnWindowShown;
		_showAnim.Completed += OnWindowShown;
	}

	private void OnWindowShown(object sender, EventArgs e)
	{
		BeginAnimation(Window.TopProperty, null);
		base.Top = _location.TopOut;
		ResetOnShown();
		_showAnim.Remove(ProjectWindow);
	}

	private void ResetOnShown()
	{
		SetStatus(WidgetShowStatus.Normal);
		UpdateBlurByShowState(show: true);
		StopOrStartAutoHide(stop: false);
	}

	private void ShowWindow()
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			SetStatus(WidgetShowStatus.InAction);
			InitShowWindowAnim();
			_showAnim?.Begin();
			UpdateBlurByShowState(show: true);
		}
	}

	private void ResetOnHidden()
	{
		UpdateBlurByShowState(show: false);
		if (base.ResizeMode == ResizeMode.CanResize)
		{
			base.ResizeMode = ResizeMode.NoResize;
		}
		StopOrStartAutoHide(stop: false);
		SetStatus(WidgetShowStatus.Hidden);
		if (Model.DisplayOption == "top")
		{
			NativeUtils.RestoreTopmost(this);
		}
	}

	private void HideWindow()
	{
		if (_showState == WidgetShowStatus.Normal)
		{
			RefreshCalendarCollapseTitle();
			SetStatus(WidgetShowStatus.InAction);
			InitHideWindowAnim();
			_hideAnim?.Begin();
			UpdateBlurByShowState(show: false);
		}
	}

	private void PopinRight()
	{
		if (_showState == WidgetShowStatus.Normal)
		{
			SetStatus(WidgetShowStatus.InAction);
			InitPopinRightAnim();
			_popInRightAnim.Begin();
			UpdateBlurByShowState(show: false);
		}
	}

	private void PopoutRight()
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			SetStatus(WidgetShowStatus.InAction);
			InitPopoutRightAnim();
			_popoutRightAnim?.Begin();
			UpdateBlurByShowState(show: true);
		}
	}

	private void PopinLeft()
	{
		if (_showState == WidgetShowStatus.Normal)
		{
			SetStatus(WidgetShowStatus.InAction);
			InitPopinLeftAnim();
			_popInLeftAnim.Begin();
			UpdateBlurByShowState(show: false);
		}
	}

	private void PopoutLeft()
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			SetStatus(WidgetShowStatus.InAction);
			InitPopoutLeftAnim();
			_popoutLeftAnim?.Begin();
			UpdateBlurByShowState(show: true);
		}
	}

	private void SetStatus(WidgetShowStatus status)
	{
		Log.d("set_status:" + status);
		_showState = status;
	}

	public void Reload(bool loadDate)
	{
		ProjectWidget?.Reload(loadDate);
	}

	public void OnDragMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_dragMouseDown && e.LeftButton == MouseButtonState.Pressed)
		{
			base.ResizeMode = ResizeMode.NoResize;
			try
			{
				_moveWindow = true;
				DragMove();
			}
			catch (Exception)
			{
			}
			finally
			{
				_moveWindow = false;
			}
			if (!_noResize)
			{
				base.ResizeMode = ResizeMode.CanResize;
			}
			Matrix? transform = (PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice;
			WidgetLocationModel widgetLocationModel = (_location = WidgetLocationHelper.GetLocationSafely(base.Left, base.Top, base.Width, base.Height, transform));
			_location.ShowShadow = Model.Opacity == 1f;
			StopOrStartAutoHide(widgetLocationModel.HideType == HideType.None);
		}
		_dragMouseDown = false;
	}

	public void OnDragBarMouseDown(object sender, MouseButtonEventArgs e)
	{
		_dragMouseDown = true;
	}

	public void SetTheme(string option)
	{
		ThemeUtil.SetTheme(Model.ThemeId, this);
		UpdateModelViewThemeId();
		base.Resources["IsDarkTheme"] = Model.ViewThemeId == "dark";
		CalendarWidget?.Reload();
		MatrixWidget?.Reload();
	}

	private void OnStatusChanged(object sender, EventArgs e)
	{
		if (base.WindowState == WindowState.Maximized || base.WindowState == WindowState.Minimized)
		{
			base.WindowState = WindowState.Normal;
		}
	}

	protected override void OnActivated(EventArgs e)
	{
		MainWindowManager.Window.CheckUserStatus().ContinueWith(UtilRun.LogTask);
		if (_widgetChild is IToastShowWindow toastWindow)
		{
			Utils.ToastWindow = toastWindow;
		}
		App.CheckLastActiveTime();
		base.OnActivated(e);
	}

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 532 && Model.WidgetType == WidgetType.CountdownItem)
		{
			RECT structure = Marshal.PtrToStructure<RECT>(lParam);
			double num = structure.Right - structure.Left;
			double num2 = structure.Bottom - structure.Top;
			if (num < num2)
			{
				structure.Right = structure.Left + (int)num2;
				Marshal.StructureToPtr(structure, lParam, fDeleteOld: false);
				handled = true;
			}
		}
		return IntPtr.Zero;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_modelSaver?.TryDoAction();
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		_modelSaver?.TryDoAction();
	}

	public void ResetPosition()
	{
		base.Left = 200.0;
		base.Top = 200.0;
	}

	private void WidgetWindow_Closing(object sender, CancelEventArgs e)
	{
		SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
		TimeChangeNotifier.DayChanged -= TimeChangeNotifier_DayChanged;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/widgetwindow.xaml", UriKind.Relative);
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
			ProjectWindow = (WidgetWindow)target;
			break;
		case 2:
			MainGrid = (Grid)target;
			break;
		case 3:
			Container = (Grid)target;
			break;
		case 4:
			CollapseGrid = (Grid)target;
			CollapseGrid.MouseEnter += OnCollapseGridEnter;
			CollapseGrid.MouseLeave += OnPopoutGridLeave;
			break;
		case 5:
			StickLeftGrid = (Grid)target;
			StickLeftGrid.MouseEnter += OnPopLeftGridEnter;
			StickLeftGrid.MouseLeave += OnPopoutGridLeave;
			break;
		case 6:
			StickRightGrid = (Grid)target;
			StickRightGrid.MouseEnter += OnPopRightGridEnter;
			StickRightGrid.MouseLeave += OnPopoutGridLeave;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
