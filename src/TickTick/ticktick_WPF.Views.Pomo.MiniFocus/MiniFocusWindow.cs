using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Win32;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class MiniFocusWindow : Window, IComponentConnector
{
	private readonly Timer _statusCheckTimer = new Timer(100.0);

	private IMiniFocus _focusControl;

	private WidgetLocationModel _location = new WidgetLocationModel();

	private WidgetShowStatus _showState;

	private int _mouseMoveOverCount;

	private int _autoHideAnimationVersion;

	private readonly string _diagnosticId = Guid.NewGuid().ToString("N");

	private string _closeReason;

	private const int WM_ACTIVATEAPP = 28;

	private const int WM_CLOSE = 16;

	private const int WM_SYSCOMMAND = 274;

	private const int SC_CLOSE = 61536;

	internal MiniFocusWindow Root;

	internal DropShadowEffect WindowEffect;

	internal ContentControl FocusContent;

	internal TranslateTransform ContentTransform;

	internal Border SideContainer;

	internal RotateTransform SideRotateTransform;

	internal Grid BesideGrid;

	internal TranslateTransform SideTransform;

	internal Border BesideBorder;

	internal ContentControl SideFocusContent;

	private bool _contentLoaded;

	private static bool IsCircle => LocalSettings.Settings.PomoLocalSetting.DisplayType == "Circle";

	private static bool IsRect => LocalSettings.Settings.PomoLocalSetting.DisplayType == "Mini";

	public bool AutoHide
	{
		get
		{
			if (_location.HideType != HideType.None)
			{
				return _location.HideType != HideType.Reset;
			}
			return false;
		}
	}

	public MiniFocusWindow()
	{
		InitializeComponent();
		_statusCheckTimer.Elapsed += OnCheckMouseOver;
		_focusControl = GetFocusControl();
		if (TickFocusManager.IsPomo)
		{
			SideFocusContent.Content = new BesidePomoProgress();
		}
		else
		{
			SideFocusContent.Content = new BesideTiming();
		}
		FocusContent.Content = _focusControl;
		_focusControl.Init();
		SetTheme(LocalSettings.Settings.PomoWindowTheme);
		base.Loaded += delegate
		{
			RegisterGlobalEvents();
			OnWindowLoaded();
			base.LocationChanged += OnLocationChanged;
			LogWindowState("Loaded");
			ResetPosition();
		};
		base.Unloaded += delegate
		{
			LogWindowState("Unloaded");
			UnregisterGlobalEvents();
		};
		base.IsVisibleChanged += delegate
		{
			LogWindowState("VisibilityChanged");
		};
		UserActCollectUtils.AddClickEvent("focus", "show", "mini_mode");
		UserActCollectUtils.AddClickEvent("focus_mini", "style", IsCircle ? "circle" : (IsRect ? "square" : "detailed"));
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
		SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
	}

	private async void OnDisplaySettingsChanged(object sender, EventArgs e)
	{
		int i;
		for (i = 0; i < 2; i++)
		{
			await Task.Delay(500);
			if (base.Dispatcher == null || base.Dispatcher.HasShutdownStarted || base.Dispatcher.HasShutdownFinished)
			{
				break;
			}
			await base.Dispatcher.InvokeAsync(delegate
			{
				if (base.IsLoaded)
				{
					LogWindowState("DisplaySettingsChanged", $"pass={i + 1}");
					EnsureWindowWithinWorkingArea($"OnDisplaySettingsChanged.{i + 1}");
					RefreshLocationAfterPositionCorrection();
				}
			});
		}
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		SetTheme(LocalSettings.Settings.PomoWindowTheme);
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		Matrix? transform = (PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice;
		WidgetLocationModel locationSafely = WidgetLocationHelper.GetLocationSafely(base.Left + _focusControl.GetLeftMargin(), base.Top, _focusControl.GetActualWidth(), _focusControl.GetActualHeight(), transform);
		if (_focusControl is CircleMiniFocusControl circleMiniFocusControl)
		{
			bool positionMode = locationSafely.ScreenX + locationSafely.ScreenWidth - locationSafely.Left - circleMiniFocusControl.ActualWidth + 100.0 < 0.0;
			circleMiniFocusControl.SetPositionMode(positionMode);
		}
		LocalSettings.Settings.PomoWindowDpiScale = VisualTreeHelper.GetDpi(this).PixelsPerDip;
		LocalSettings.Settings.PomoWindowLeft = (int)base.Left;
		LocalSettings.Settings.PomoWindowTop = (int)base.Top;
		if (_showState == WidgetShowStatus.Hidden && locationSafely.HideType != _location.HideType)
		{
			ShowWindowStory();
		}
	}

	private IMiniFocus GetFocusControl()
	{
		if (!string.IsNullOrEmpty(LocalSettings.Settings.PomoLocalSetting.DisplayType) && !(LocalSettings.Settings.PomoLocalSetting.DisplayType == "Normal"))
		{
			if (IsCircle)
			{
				return new CircleMiniFocusControl();
			}
			return new RectMiniFocusControl();
		}
		return new PomoWindow();
	}

	private void OnWindowLoaded()
	{
		IntPtr handle = new WindowInteropHelper(this).Handle;
		HwndSource.FromHwnd(handle)?.AddHook(WndProc);
		NativeUtils.SetWindowLong(handle, -20, 128L);
		Matrix? transform = (PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice;
		WidgetLocationModel locationSafely = WidgetLocationHelper.GetLocationSafely(base.Left + _focusControl.GetLeftMargin(), base.Top, _focusControl.GetActualWidth(), _focusControl.GetActualHeight(), transform);
		_location = locationSafely;
		base.Topmost = LocalSettings.Settings.PomoTopMost || _location.HideType != HideType.None;
		_focusControl.SetHideStyle(_location.HideType != HideType.None);
		CheckPosition();
		if (_location.HideType != HideType.None)
		{
			SetSideDisplay();
		}
	}

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		switch (msg)
		{
		case 16:
			LogWindowState("CloseMessage");
			break;
		case 274:
			if ((wParam.ToInt64() & 0xFFF0) == 61536)
			{
				LogWindowState("SystemCloseCommand");
			}
			break;
		}
		if (msg == 28 && wParam != IntPtr.Zero && (LocalSettings.Settings.PomoTopMost || AutoHide))
		{
			NativeUtils.RestoreTopmost(this);
		}
		return IntPtr.Zero;
	}

	private void OnCheckMouseOver(object sender, ElapsedEventArgs e)
	{
		base.Dispatcher?.InvokeAsync(TryHideWindow, DispatcherPriority.Background);
	}

	private void UnregisterGlobalEvents()
	{
		GlobalEventManager.PomoWindowOpacityChanged -= OnOpacityChanged;
		GlobalEventManager.PomoWindowThemeChanged -= OnThemeChanged;
		TickFocusManager.CurrentSecondChanged -= OnCurrentSecondChanged;
		TickFocusManager.TypeChanged -= OnFocusTypeChanged;
		TickFocusManager.StatusChanged -= OnStatusChanged;
	}

	private void OnStatusChanged()
	{
		base.Dispatcher?.Invoke(delegate
		{
			_focusControl.OnStatusChanged();
			(SideFocusContent.Content as ISideMiniFocus)?.OnStatusChanged();
		});
	}

	private void OnFocusTypeChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_focusControl.OnFocusTypeChanged();
			if (TickFocusManager.IsPomo)
			{
				SideFocusContent.Content = new BesidePomoProgress();
			}
			else
			{
				SideFocusContent.Content = new BesideTiming();
			}
		});
	}

	private void OnCurrentSecondChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_focusControl.SetCountText();
			(SideFocusContent.Content as ISideMiniFocus)?.SetPercent();
		});
	}

	private void OnThemeChanged(object sender, string e)
	{
		SetTheme(LocalSettings.Settings.PomoWindowTheme);
	}

	private void OnOpacityChanged(object sender, double e)
	{
		_focusControl.SetOpacity(e);
	}

	private void RegisterGlobalEvents()
	{
		TickFocusManager.CurrentSecondChanged += OnCurrentSecondChanged;
		TickFocusManager.TypeChanged += OnFocusTypeChanged;
		TickFocusManager.StatusChanged += OnStatusChanged;
		GlobalEventManager.PomoWindowOpacityChanged += OnOpacityChanged;
		GlobalEventManager.PomoWindowThemeChanged += OnThemeChanged;
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnTopMostChanged, "PomoTopMost");
	}

	private void OnTopMostChanged(object sender, PropertyChangedEventArgs e)
	{
		int topmost;
		if (!LocalSettings.Settings.PomoTopMost)
		{
			WidgetLocationModel location = _location;
			topmost = ((location == null || location.HideType != HideType.None) ? 1 : 0);
		}
		else
		{
			topmost = 1;
		}
		base.Topmost = (byte)topmost != 0;
		base.ShowInTaskbar = !base.Topmost;
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && _showState == WidgetShowStatus.Normal && _focusControl.CanDragMove())
		{
			_focusControl.SetMoving(b: true);
			double left = base.Left;
			double top = base.Top;
			DragMove();
			EnsureWindowWithinWorkingArea("DragMove");
			WidgetLocationModel widgetLocationModel = WidgetLocationHelper.GetLocationSafely(transform: (PresentationSource.FromVisual(this)?.CompositionTarget)?.TransformFromDevice, left: base.Left + _focusControl.GetLeftMargin(), top: base.Top, width: _focusControl.GetActualWidth(), height: _focusControl.GetActualHeight());
			if (widgetLocationModel.HideType == HideType.Reset)
			{
				base.Left = _location.Left;
				base.Top = _location.Top;
				widgetLocationModel = _location;
			}
			_location = widgetLocationModel;
			_focusControl.SetHideStyle(_location.HideType != HideType.None);
			SetSideDisplay();
			int topmost;
			if (!LocalSettings.Settings.PomoTopMost)
			{
				WidgetLocationModel location = _location;
				topmost = ((location == null || location.HideType != HideType.None) ? 1 : 0);
			}
			else
			{
				topmost = 1;
			}
			base.Topmost = (byte)topmost != 0;
			base.ShowInTaskbar = !base.Topmost;
			if (Math.Abs(left - base.Left) > 1.0 || Math.Abs(top - base.Top) > 1.0)
			{
				CheckPosition();
				StartHide();
			}
			e.Handled = true;
		}
	}

	private void StartHide()
	{
		base.Topmost = LocalSettings.Settings.PomoTopMost;
		switch (_location.HideType)
		{
		case HideType.Top:
			base.Topmost = true;
			PopinTop();
			break;
		case HideType.Left:
			base.Topmost = true;
			PopinLeft();
			break;
		case HideType.Right:
			base.Topmost = true;
			PopinRight();
			break;
		}
		base.ShowInTaskbar = !base.Topmost;
		StopOrStartAutoHide(stop: true);
	}

	public void SetSideDisplay()
	{
		BesideGrid.VerticalAlignment = ((!IsRect) ? VerticalAlignment.Center : VerticalAlignment.Top);
		BesideGrid.Margin = ((IsRect && _location.HideType != HideType.Top) ? new Thickness(0.0) : new Thickness(0.0, 1.0, 0.0, 1.0));
		SideFocusContent.Margin = new Thickness((_location.HideType != HideType.Right) ? 2 : 0, 4.0, (_location.HideType == HideType.Right) ? 2 : 0, 4.0);
		if (!IsRect && !IsCircle)
		{
			if (_location.HideType == HideType.Top)
			{
				SideContainer.Height = 196.0;
				BesideGrid.Height = 194.0;
				SideFocusContent.Height = 186.0;
				BesideBorder.CornerRadius = ((_location.HideType != HideType.Right) ? new CornerRadius(0.0, 6.0, 6.0, 0.0) : new CornerRadius(6.0, 0.0, 0.0, 6.0));
			}
			else
			{
				double actualHeight = _focusControl.GetActualHeight();
				SideContainer.Height = actualHeight + 2.0;
				BesideGrid.Height = actualHeight;
				SideFocusContent.Height = actualHeight - 8.0;
				BesideBorder.CornerRadius = ((_location.HideType != HideType.Right) ? new CornerRadius(0.0, 6.0, 6.0, 0.0) : new CornerRadius(6.0, 0.0, 0.0, 6.0));
			}
		}
		else if (_location.HideType != HideType.Top)
		{
			SideContainer.Height = 70.0;
			BesideGrid.Height = (IsRect ? 36 : 42);
			SideFocusContent.Height = (IsRect ? 28 : 34);
			BesideBorder.CornerRadius = ((_location.HideType == HideType.Left) ? new CornerRadius(0.0, 4.0, 4.0, 0.0) : new CornerRadius(4.0, 0.0, 0.0, 4.0));
		}
		else
		{
			SideContainer.Height = ((_location.HideType == HideType.Top) ? 72 : 70);
			BesideGrid.Height = 70.0;
			SideFocusContent.Height = 62.0;
			BesideBorder.CornerRadius = ((_location.HideType != HideType.Right) ? new CornerRadius(0.0, 6.0, 6.0, 0.0) : new CornerRadius(6.0, 0.0, 0.0, 6.0));
		}
	}

	private async void CheckPosition()
	{
		if (_focusControl is CircleMiniFocusControl circleMiniFocusControl)
		{
			bool positionMode = _location.ScreenX + _location.ScreenWidth - _location.Left - circleMiniFocusControl.ActualWidth + 100.0 < 0.0;
			await circleMiniFocusControl.SetPositionMode(positionMode);
			_focusControl.SetMoving(b: false);
		}
		StopOrStartAutoHide(_location.HideType == HideType.None);
	}

	private void SetTheme(string theme)
	{
		theme = ((theme == "dark") ? "dark" : "light");
		ThemeUtil.SetTheme(theme, this);
	}

	private void TryHideWindow()
	{
		if (_location.HideType != HideType.None && _showState == WidgetShowStatus.Normal)
		{
			if (!base.IsMouseOver && _focusControl.CanHide())
			{
				_mouseMoveOverCount++;
			}
			else
			{
				_mouseMoveOverCount = 0;
			}
			if (_mouseMoveOverCount >= 3)
			{
				_mouseMoveOverCount = 0;
				StartHide();
			}
		}
	}

	private void PopinLeft()
	{
		if (_showState == WidgetShowStatus.Normal)
		{
			_showState = WidgetShowStatus.InAction;
			BeforeHide();
			BeginPopinLeftAnim();
			_focusControl.OnWindowStartHide();
		}
	}

	private async void BeginPopinLeftAnim()
	{
		int animationVersion = BeginAutoHideAnimation("HideLeft");
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, -2.0 - _focusControl.GetActualWidth() - _focusControl.GetExtraWidth(), IsRect ? 180 : 240);
		doubleAnimation.EasingFunction = new QuadraticEase
		{
			EasingMode = EasingMode.EaseOut
		};
		ContentTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(-20.0, 0.0, 1);
		doubleAnimation2.BeginTime = TimeSpan.FromMilliseconds(IsRect ? 120 : 160);
		SideTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
		await Task.Delay(300);
		if (IsAutoHideAnimationCurrent(animationVersion))
		{
			FocusContent.Visibility = Visibility.Hidden;
			_showState = WidgetShowStatus.Hidden;
			LogWindowState("AutoHideCompleted");
			NativeUtils.RestoreTopmost(this);
		}
	}

	private async void BeginPopoutLeftAnim()
	{
		int animationVersion = BeginAutoHideAnimation("ShowLeft");
		FocusContent.Visibility = Visibility.Visible;
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, -20.0, 10);
		SideTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, 0.0, IsRect ? 180 : 240);
		doubleAnimation2.EasingFunction = new QuadraticEase
		{
			EasingMode = EasingMode.EaseOut
		};
		ContentTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
		await Task.Delay(300);
		if (IsAutoHideAnimationCurrent(animationVersion))
		{
			_showState = WidgetShowStatus.Normal;
			LogWindowState("AutoShowCompleted");
			StopOrStartAutoHide(stop: false);
			WindowEffect.Opacity = 0.2;
		}
	}

	private void PopinRight()
	{
		if (_showState == WidgetShowStatus.Normal)
		{
			_showState = WidgetShowStatus.InAction;
			BeforeHide();
			BeginPopinRightAnim();
			_focusControl.OnWindowStartHide();
		}
	}

	private async void BeginPopinRightAnim()
	{
		int animationVersion = BeginAutoHideAnimation("HideRight");
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 2.0 + _focusControl.GetActualWidth() + _focusControl.GetExtraWidth(), IsCircle ? 300 : (IsRect ? 180 : 210));
		doubleAnimation.EasingFunction = new QuadraticEase
		{
			EasingMode = EasingMode.EaseOut
		};
		ContentTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(20.0, 0.0, 1);
		doubleAnimation2.BeginTime = TimeSpan.FromMilliseconds(60.0);
		SideTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
		await Task.Delay(300);
		if (IsAutoHideAnimationCurrent(animationVersion))
		{
			FocusContent.Visibility = Visibility.Hidden;
			_showState = WidgetShowStatus.Hidden;
			LogWindowState("AutoHideCompleted");
			WindowEffect.Opacity = 0.2;
			NativeUtils.RestoreTopmost(this);
		}
	}

	private async void BeginPopoutRightAnim()
	{
		int animationVersion = BeginAutoHideAnimation("ShowRight");
		FocusContent.Visibility = Visibility.Visible;
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 20.0, 1);
		doubleAnimation.BeginTime = TimeSpan.FromMilliseconds(60.0);
		SideTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, 0.0, IsRect ? 180 : 240);
		doubleAnimation2.EasingFunction = new QuadraticEase
		{
			EasingMode = EasingMode.EaseOut
		};
		ContentTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
		await Task.Delay(300);
		if (IsAutoHideAnimationCurrent(animationVersion))
		{
			_showState = WidgetShowStatus.Normal;
			LogWindowState("AutoShowCompleted");
			StopOrStartAutoHide(stop: false);
			SideContainer.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
		}
	}

	private void PopinTop()
	{
		if (_showState == WidgetShowStatus.Normal)
		{
			_showState = WidgetShowStatus.InAction;
			BeforeHide();
			BeginPopinTopAnim();
			_focusControl.OnWindowStartHide();
		}
	}

	private void BeforeHide()
	{
		ContentTransform.BeginAnimation(TranslateTransform.XProperty, null);
		ContentTransform.BeginAnimation(TranslateTransform.YProperty, null);
		ContentTransform.X = 0.0;
		ContentTransform.Y = 0.0;
		switch (_location.HideType)
		{
		case HideType.Top:
			base.Top = _location.ScreenY;
			SideContainer.HorizontalAlignment = HorizontalAlignment.Center;
			SideTransform.BeginAnimation(TranslateTransform.XProperty, null);
			SideRotateTransform.Angle = 90.0;
			SideTransform.X = -20.0;
			SideContainer.Margin = new Thickness(0.0, -2.0, 0.0, 0.0);
			break;
		case HideType.Right:
			base.Left = _location.ScreenX + _location.ScreenWidth - _focusControl.GetLeftMargin() - _focusControl.GetActualWidth();
			SideContainer.HorizontalAlignment = HorizontalAlignment.Left;
			SideContainer.Margin = new Thickness(_focusControl.GetLeftMargin() + _focusControl.GetActualWidth() - 11.0, 0.0, 0.0, 0.0);
			SideTransform.BeginAnimation(TranslateTransform.XProperty, null);
			SideRotateTransform.Angle = 0.0;
			SideTransform.X = 20.0;
			break;
		case HideType.Left:
			base.Left = _location.ScreenX - _focusControl.GetLeftMargin();
			SideContainer.HorizontalAlignment = HorizontalAlignment.Left;
			SideContainer.Margin = new Thickness(_focusControl.GetLeftMargin() - 2.0, 0.0, 0.0, 0.0);
			SideTransform.BeginAnimation(TranslateTransform.XProperty, null);
			SideRotateTransform.Angle = 0.0;
			SideTransform.X = -20.0;
			break;
		}
	}

	private async void BeginPopinTopAnim()
	{
		int animationVersion = BeginAutoHideAnimation("HideTop");
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 0.0 - _focusControl.GetActualHeight(), 160);
		ContentTransform.BeginAnimation(TranslateTransform.YProperty, doubleAnimation);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, 0.0, 1);
		doubleAnimation2.BeginTime = TimeSpan.FromMilliseconds(120.0);
		SideTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
		await Task.Delay(200);
		if (IsAutoHideAnimationCurrent(animationVersion))
		{
			_showState = WidgetShowStatus.Hidden;
			FocusContent.Visibility = Visibility.Hidden;
			LogWindowState("AutoHideCompleted");
			WindowEffect.Opacity = 0.2;
			NativeUtils.RestoreTopmost(this);
		}
	}

	private async void BeginPopoutTopAnim()
	{
		int animationVersion = BeginAutoHideAnimation("ShowTop");
		FocusContent.Visibility = Visibility.Visible;
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, -20.0, 10);
		doubleAnimation.BeginTime = TimeSpan.FromMilliseconds(60.0);
		SideTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, 0.0, 180);
		doubleAnimation2.EasingFunction = new QuadraticEase
		{
			EasingMode = EasingMode.EaseOut
		};
		ContentTransform.BeginAnimation(TranslateTransform.YProperty, doubleAnimation2);
		await Task.Delay(200);
		if (IsAutoHideAnimationCurrent(animationVersion))
		{
			_showState = WidgetShowStatus.Normal;
			LogWindowState("AutoShowCompleted");
			StopOrStartAutoHide(stop: false);
		}
	}

	private int BeginAutoHideAnimation(string action)
	{
		int result = ++_autoHideAnimationVersion;
		LogWindowState(action);
		return result;
	}

	private bool IsAutoHideAnimationCurrent(int animationVersion)
	{
		int num;
		if (base.IsLoaded)
		{
			num = ((animationVersion == _autoHideAnimationVersion) ? 1 : 0);
			if (num != 0)
			{
				goto IL_0032;
			}
		}
		else
		{
			num = 0;
		}
		LogWindowState("AnimationSkipped", $"callbackVersion={animationVersion}");
		goto IL_0032;
		IL_0032:
		return (byte)num != 0;
	}

	private void CancelAutoHideAnimation()
	{
		_autoHideAnimationVersion++;
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

	private async void OnSideBorderMouseEnter(object sender, MouseEventArgs e)
	{
		await Task.Delay(300);
		if (SideContainer.IsMouseOver)
		{
			ShowWindowStory();
		}
	}

	private void ShowWindowStory()
	{
		if (_showState == WidgetShowStatus.Hidden)
		{
			_showState = WidgetShowStatus.InAction;
			switch (_location.HideType)
			{
			case HideType.Top:
				BeginPopoutTopAnim();
				WindowEffect.Opacity = 0.0;
				break;
			case HideType.Left:
				BeginPopoutLeftAnim();
				WindowEffect.Opacity = 0.0;
				break;
			case HideType.Right:
				BeginPopoutRightAnim();
				WindowEffect.Opacity = 0.0;
				break;
			}
		}
	}

	public void TryShowWindow()
	{
		LogWindowState("ShowRequested");
		base.Visibility = Visibility.Visible;
		LocalSettings.Settings.PomoLocalSetting.OpenWidget = true;
		LocalSettings.Settings.Save(force: true);
		Show();
		Activate();
	}

	private void OnWindowKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.P)
		{
			LocalSettings.Settings.PomoTopMost = !LocalSettings.Settings.PomoTopMost;
			LocalSettings.Settings.Save();
		}
	}

	private async void ResetPosition()
	{
		double left = base.Left;
		double top = base.Top;
		WindowHelper.MoveTo(this, (int)base.Left, (int)base.Top);
		base.Left = left;
		base.Top = top;
		EnsureWindowWithinWorkingArea("ResetPosition.Immediate");
		RefreshLocationAfterPositionCorrection();
		await Task.Delay(500);
		if (base.IsLoaded)
		{
			EnsureWindowWithinWorkingArea("ResetPosition.Settled");
			RefreshLocationAfterPositionCorrection();
		}
	}

	private bool EnsureWindowWithinWorkingArea(string source)
	{
		Matrix? transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
		double leftMargin = _focusControl.GetLeftMargin();
		Point locationWithinWorkingAreaSafely = WidgetLocationHelper.GetLocationWithinWorkingAreaSafely(base.Left + leftMargin, base.Top, _focusControl.GetActualWidth(), _focusControl.GetActualHeight(), transform);
		double num = locationWithinWorkingAreaSafely.X - leftMargin;
		if (!(Math.Abs(base.Left - num) > 0.5) && !(Math.Abs(base.Top - locationWithinWorkingAreaSafely.Y) > 0.5))
		{
			return false;
		}
		ResetAutoHidePresentationForPositionCorrection();
		base.Left = num;
		base.Top = locationWithinWorkingAreaSafely.Y;
		LogWindowState("PositionCorrected", "source=" + source);
		return true;
	}

	private void ResetAutoHidePresentationForPositionCorrection()
	{
		CancelAutoHideAnimation();
		if (_showState != WidgetShowStatus.Normal)
		{
			LogWindowState("ResetAutoHidePresentation");
			ContentTransform.BeginAnimation(TranslateTransform.XProperty, null);
			ContentTransform.BeginAnimation(TranslateTransform.YProperty, null);
			SideTransform.BeginAnimation(TranslateTransform.XProperty, null);
			ContentTransform.X = 0.0;
			ContentTransform.Y = 0.0;
			SideTransform.X = -20.0;
			SideRotateTransform.Angle = 0.0;
			SideContainer.HorizontalAlignment = HorizontalAlignment.Left;
			SideContainer.Margin = new Thickness(0.0);
			FocusContent.Visibility = Visibility.Visible;
			_showState = WidgetShowStatus.Normal;
		}
	}

	private void RefreshLocationAfterPositionCorrection()
	{
		Matrix? transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
		WidgetLocationModel locationSafely = WidgetLocationHelper.GetLocationSafely(base.Left + _focusControl.GetLeftMargin(), base.Top, _focusControl.GetActualWidth(), _focusControl.GetActualHeight(), transform);
		if (_showState != WidgetShowStatus.Normal && (locationSafely.HideType == HideType.None || locationSafely.HideType != _location.HideType))
		{
			ResetAutoHidePresentationForPositionCorrection();
		}
		_location = locationSafely;
		_focusControl.SetHideStyle(_location.HideType != HideType.None);
		SetSideDisplay();
		base.Topmost = LocalSettings.Settings.PomoTopMost || _location.HideType != HideType.None;
		base.ShowInTaskbar = !base.Topmost;
		CheckPosition();
	}

	public void EnsureVisibleAfterContentSizeChanged(string source)
	{
		if (base.IsLoaded)
		{
			UpdateLayout();
			EnsureWindowWithinWorkingArea(source);
			RefreshLocationAfterPositionCorrection();
		}
	}

	public async void SetDisplayType()
	{
		IMiniFocus control = GetFocusControl();
		if (_focusControl.GetType() != control.GetType())
		{
			LogWindowState("DisplayTypeChanging", "target=" + control.GetType().Name);
			ResetAutoHidePresentationForPositionCorrection();
			SideContainer.Opacity = 0.0;
			FocusContent.Opacity = 0.0;
			FocusContent.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(1.0, 0.0, 80));
			await Task.Delay(100);
			UpdateLayout();
			if (!IsCircle && _focusControl is CircleMiniFocusControl)
			{
				base.Left += 76.0;
			}
			else if (IsCircle && !(_focusControl is CircleMiniFocusControl))
			{
				base.Left -= 76.0;
			}
			_focusControl = control;
			FocusContent.Content = _focusControl;
			_focusControl.Init();
			_focusControl.SetHideStyle(_location.HideType != HideType.None);
			await Task.Delay(50);
			UpdateLayout();
			EnsureWindowWithinWorkingArea("SetDisplayType");
			RefreshLocationAfterPositionCorrection();
			FocusContent.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(0.0, 1.0, 80));
			SideContainer.Opacity = 1.0;
			LogWindowState("DisplayTypeChanged");
			if (_location.HideType != HideType.None)
			{
				SetSideDisplay();
				FocusContent.Visibility = Visibility.Visible;
				_showState = WidgetShowStatus.Normal;
				BeforeHide();
				_mouseMoveOverCount = -1;
				StopOrStartAutoHide(stop: false);
			}
		}
	}

	internal void CloseWithReason(string reason)
	{
		string closeReason = _closeReason;
		_closeReason = reason;
		try
		{
			LogWindowState("CloseRequested");
			Close();
		}
		finally
		{
			_closeReason = closeReason;
		}
	}

	internal void LogWindowState(string action, string details = null)
	{
		UtilLog.Info("MiniFocusWindow " + action + " id=" + _diagnosticId + " closeReason=" + (_closeReason ?? "Unspecified") + " " + $"control={_focusControl?.GetType().Name} showState={_showState} hideType={_location?.HideType} " + $"visible={base.IsVisible} loaded={base.IsLoaded} active={base.IsActive} windowState={base.WindowState} topmost={base.Topmost} " + $"contentVisibility={FocusContent?.Visibility} position={base.Left},{base.Top} size={base.ActualWidth},{base.ActualHeight} " + $"animationVersion={_autoHideAnimationVersion} openWidget={LocalSettings.Settings.PomoLocalSetting.OpenWidget} " + $"saveAfterClose={TickFocusManager.SaveAfterClose} {details}");
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		LogWindowState("Closing", (_closeReason == null) ? ("stack=" + Environment.StackTrace) : null);
		if (TickFocusManager.SaveAfterClose)
		{
			LocalSettings.Settings.PomoLocalSetting.OpenWidget = false;
		}
		UtilLog.Info("ClosePomoWindow openWidget : " + LocalSettings.Settings.PomoLocalSetting.OpenWidget);
		UnregisterGlobalEvents();
		base.OnClosing(e);
		LogWindowState("ClosingResult", $"cancel={e.Cancel}");
	}

	protected override void OnClosed(EventArgs e)
	{
		LogWindowState("Closed");
		CancelAutoHideAnimation();
		SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		_statusCheckTimer.Stop();
		_statusCheckTimer.Elapsed -= OnCheckMouseOver;
		_statusCheckTimer.Dispose();
		base.OnClosed(e);
	}

	public void OnStatisticsChanged()
	{
		if (_focusControl is PomoWindow pomoWindow)
		{
			pomoWindow.ResetStatistics();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/minifocuswindow.xaml", UriKind.Relative);
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
			Root = (MiniFocusWindow)target;
			Root.MouseMove += OnMouseMove;
			Root.KeyDown += OnWindowKeyDown;
			break;
		case 2:
			WindowEffect = (DropShadowEffect)target;
			break;
		case 3:
			FocusContent = (ContentControl)target;
			break;
		case 4:
			ContentTransform = (TranslateTransform)target;
			break;
		case 5:
			SideContainer = (Border)target;
			SideContainer.MouseEnter += OnSideBorderMouseEnter;
			break;
		case 6:
			SideRotateTransform = (RotateTransform)target;
			break;
		case 7:
			BesideGrid = (Grid)target;
			break;
		case 8:
			SideTransform = (TranslateTransform)target;
			break;
		case 9:
			BesideBorder = (Border)target;
			break;
		case 10:
			SideFocusContent = (ContentControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
