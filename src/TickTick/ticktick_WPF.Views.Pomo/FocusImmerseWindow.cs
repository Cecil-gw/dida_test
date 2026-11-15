using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo.MainFocus;

namespace ticktick_WPF.Views.Pomo;

public class FocusImmerseWindow : MyWindow, IComponentConnector
{
	internal Grid ClockGrid;

	internal TranslateTransform _clockTranslate;

	internal StackPanel ClockNumPanel;

	internal ClockControl _clock;

	internal TextBlock _textBlock1;

	internal FocusBreakControl ClockPomoControl;

	internal FocusImmerseButtons _buttons1;

	internal Grid PageTurnGrid;

	internal TranslateTransform _pageTurnTranslate;

	internal StackPanel PageTurnNumPanel;

	internal PageTurnClock _pageTurn;

	internal TextBlock _textBlock;

	internal FocusBreakControl PageTurnPomoControl;

	internal FocusImmerseButtons _buttons;

	internal Border _clockEll;

	internal Border _pageTurnEll;

	internal Border _closeButton;

	internal Grid ToastGrid;

	private bool _contentLoaded;

	private bool _usePageTurn { get; set; } = true;

	private bool _switchingClock { get; set; }

	private Point _mouseDownPoint { get; set; }

	private DispatcherTimer _hideTimer { get; set; } = new DispatcherTimer
	{
		Interval = TimeSpan.FromMilliseconds(4000.0)
	};

	public FocusImmerseWindow()
	{
		InitializeComponent();
		_hideTimer.Tick += HideButton;
		WindowTouchPadHandler.AddHook(this, HandleTouchPadScroll);
	}

	private void MyWindow_Loaded(object sender, RoutedEventArgs e)
	{
		_clockTranslate.X = (_usePageTurn ? base.ActualWidth : 0.0);
		_pageTurnTranslate.X = ((!_usePageTurn) ? (-1.0 * base.ActualWidth) : 0.0);
		_pageTurn.SetWidth(Math.Min(base.ActualWidth / 4.0, base.ActualHeight / 3.0));
		_clock.FontSize = base.ActualHeight / 8.0;
		SetCountText();
		SetText();
		_buttons?.SetButtons();
		_buttons1?.SetButtons();
		TickFocusManager.StatusChanged += OnStatusChanged;
		TickFocusManager.CurrentSecondChanged += SetCountText;
		TickFocusManager.TypeChanged += OnFocusTypeChanged;
		PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
	}

	private void MyWindow_Closing(object sender, CancelEventArgs e)
	{
		TickFocusManager.ImmerseWindow = null;
		_hideTimer.Stop();
		TickFocusManager.StatusChanged -= OnStatusChanged;
		TickFocusManager.CurrentSecondChanged -= SetCountText;
		TickFocusManager.TypeChanged -= OnFocusTypeChanged;
		PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
	}

	private void MyWindow_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			Close();
		}
	}

	private void MyWindow_MouseMove(object sender, MouseEventArgs e)
	{
		base.Cursor = Cursors.Arrow;
		if (!_hideTimer.IsEnabled || Math.Abs(_closeButton.Opacity - 1.0) > 0.1)
		{
			foreach (FrameworkElement child in _buttons.Children)
			{
				child.Cursor = Cursors.Hand;
			}
			foreach (FrameworkElement child2 in _buttons1.Children)
			{
				child2.Cursor = Cursors.Hand;
			}
			_closeButton.Opacity = 1.0;
			if (FocusTimer.IsTiming)
			{
				_hideTimer.Start();
			}
		}
		if (_buttons.IsMouseOver || _buttons1.IsMouseOver)
		{
			_hideTimer.Stop();
		}
		if (e.LeftButton != MouseButtonState.Pressed || !(_mouseDownPoint != default(Point)))
		{
			return;
		}
		double x = (e.GetPosition(this) - _mouseDownPoint).X;
		if (Math.Abs(x) > 2.0)
		{
			if (_usePageTurn)
			{
				_pageTurnTranslate.X = x;
				_clockTranslate.X = ((x > 0.0) ? (x - base.ActualWidth) : (base.ActualWidth + x));
			}
			else
			{
				_clockTranslate.X = x;
				_pageTurnTranslate.X = ((x > 0.0) ? (x - base.ActualWidth) : (base.ActualWidth + x));
			}
		}
	}

	private void MyWindow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!_switchingClock)
		{
			_pageTurnTranslate.BeginAnimation(TranslateTransform.XProperty, null);
			_pageTurnTranslate.X = (_usePageTurn ? 0.0 : (-1.0 * base.ActualWidth));
			_clockTranslate.BeginAnimation(TranslateTransform.XProperty, null);
			_clockTranslate.X = ((!_usePageTurn) ? 0.0 : base.ActualWidth);
			_mouseDownPoint = e.GetPosition(this);
		}
	}

	private async void MyWindow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (!_switchingClock && _mouseDownPoint != default(Point))
		{
			double x = (e.GetPosition(this) - _mouseDownPoint).X;
			if (Math.Abs(x) > 2.0)
			{
				_switchingClock = true;
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 0.0, 400);
				DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, (x > 0.0) ? (-1.0 * base.ActualWidth) : base.ActualWidth, 400);
				doubleAnimation.EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				doubleAnimation2.EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				if (Math.Abs(x) > base.ActualWidth / 4.0)
				{
					_usePageTurn = !_usePageTurn;
					_clockEll.Opacity = (_usePageTurn ? 0.4 : 0.6);
					_pageTurnEll.Opacity = (_usePageTurn ? 0.6 : 0.4);
					doubleAnimation2.To = ((x < 0.0) ? (-1.0 * base.ActualWidth) : base.ActualWidth);
				}
				_pageTurnTranslate.BeginAnimation(TranslateTransform.XProperty, null);
				_clockTranslate.BeginAnimation(TranslateTransform.XProperty, null);
				_pageTurnTranslate.BeginAnimation(TranslateTransform.XProperty, _usePageTurn ? doubleAnimation : doubleAnimation2);
				_clockTranslate.BeginAnimation(TranslateTransform.XProperty, (!_usePageTurn) ? doubleAnimation : doubleAnimation2);
				await Task.Delay(600);
				_switchingClock = false;
			}
		}
		_mouseDownPoint = default(Point);
	}

	private void SetCountText()
	{
		base.Dispatcher.Invoke(delegate
		{
			int displaySecond = TickFocusManager.DisplaySecond;
			_pageTurn.SetSecond(displaySecond);
			_clock.SetTime(displaySecond);
		});
	}

	private void OnStatusChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			_buttons?.SetButtons();
			_buttons1?.SetButtons();
			SetText();
		});
	}

	private void OnFocusTypeChanged()
	{
	}

	private void SetText()
	{
		switch (TickFocusManager.Status)
		{
		case PomoStatus.Working:
			_textBlock.Text = (string.IsNullOrEmpty(TickFocusManager.Config.FocusVModel.FocusId) ? Utils.GetString("Focusing") : TickFocusManager.Config.FocusVModel.Title);
			break;
		case PomoStatus.Relaxing:
			_textBlock.Text = Utils.GetString("ImmerseRelaxing");
			break;
		case PomoStatus.WaitingWork:
			_textBlock.Text = (TickFocusManager.IsPomo ? Utils.GetString(TickFocusManager.Config.FromRelax ? "GoonNextPomo" : "ImmerseWaitPomo") : Utils.GetString("ImmerseWaitTiming"));
			break;
		case PomoStatus.WaitingRelax:
			if (LocalSettings.Settings.LongBreakEvery <= 1 || (TickFocusManager.Config.PomoCount >= 1 && TickFocusManager.Config.PomoCount % LocalSettings.Settings.LongBreakEvery == 0))
			{
				_textBlock.Text = string.Format(Utils.GetString("ImmerseWaitRelaxL"), LocalSettings.Settings.LongBreakEvery);
			}
			else
			{
				_textBlock.Text = Utils.GetString("ImmerseWaitRelaxS");
			}
			break;
		case PomoStatus.Pause:
			_textBlock.Text = Utils.GetString("Paused") + (TickFocusManager.IsPomo ? (", " + Utils.GetString("PausingText")) : "");
			break;
		}
		_textBlock1.Text = _textBlock.Text;
		if (TickFocusManager.Status == PomoStatus.WaitingRelax)
		{
			StackPanel clockNumPanel = ClockNumPanel;
			Visibility visibility = (PageTurnNumPanel.Visibility = Visibility.Collapsed);
			clockNumPanel.Visibility = visibility;
			FocusBreakControl clockPomoControl = ClockPomoControl;
			visibility = (PageTurnPomoControl.Visibility = Visibility.Visible);
			clockPomoControl.Visibility = visibility;
			ClockPomoControl.SetRelaxText();
			PageTurnPomoControl.SetRelaxText();
		}
		else
		{
			StackPanel clockNumPanel2 = ClockNumPanel;
			Visibility visibility = (PageTurnNumPanel.Visibility = Visibility.Visible);
			clockNumPanel2.Visibility = visibility;
			FocusBreakControl clockPomoControl2 = ClockPomoControl;
			visibility = (PageTurnPomoControl.Visibility = Visibility.Collapsed);
			clockPomoControl2.Visibility = visibility;
		}
	}

	private void OnFocusChanged(object sender, PropertyChangedEventArgs e)
	{
		if (TickFocusManager.Status == PomoStatus.Working)
		{
			_textBlock.Text = (string.IsNullOrEmpty(TickFocusManager.Config.FocusVModel.FocusId) ? Utils.GetString("Focusing") : TickFocusManager.Config.FocusVModel.Title);
			_textBlock1.Text = _textBlock.Text;
		}
	}

	private void HideButton(object sender, EventArgs e)
	{
		base.Cursor = Cursors.None;
		foreach (FrameworkElement child in _buttons.Children)
		{
			child.Cursor = Cursors.None;
		}
		foreach (FrameworkElement child2 in _buttons1.Children)
		{
			child2.Cursor = Cursors.None;
		}
		_closeButton.Opacity = 0.0;
		_hideTimer.Stop();
	}

	private void _closeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void SwitchClock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		SwitchClock(isPageTurn: false);
	}

	private void SwitchTurn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		SwitchClock(isPageTurn: true);
	}

	private void SwitchClock(bool isPageTurn)
	{
		if (isPageTurn != _usePageTurn)
		{
			SwitchClock(withAnim: true, !isPageTurn);
		}
	}

	private void HandleTouchPadScroll(bool isLeft)
	{
		if (!_switchingClock)
		{
			SwitchClock(withAnim: true, !isLeft);
		}
	}

	private async void SwitchClock(bool withAnim, bool rightSwitch)
	{
		if (!_switchingClock)
		{
			if (withAnim)
			{
				_switchingClock = true;
				_usePageTurn = !_usePageTurn;
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, 0.0, 800);
				DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(null, rightSwitch ? (-1.0 * base.ActualWidth) : base.ActualWidth, 800);
				doubleAnimation.EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				doubleAnimation2.EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				_pageTurnTranslate.BeginAnimation(TranslateTransform.XProperty, null);
				_clockTranslate.BeginAnimation(TranslateTransform.XProperty, null);
				_pageTurnTranslate.BeginAnimation(TranslateTransform.XProperty, _usePageTurn ? doubleAnimation : doubleAnimation2);
				_clockTranslate.BeginAnimation(TranslateTransform.XProperty, (!_usePageTurn) ? doubleAnimation : doubleAnimation2);
			}
			_clockEll.Opacity = (_usePageTurn ? 0.4 : 0.6);
			_pageTurnEll.Opacity = ((!_usePageTurn) ? 0.4 : 0.6);
			UpdateLayout();
			await Task.Delay(600);
			_switchingClock = false;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/focusimmersewindow.xaml", UriKind.Relative);
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
			ClockGrid = (Grid)target;
			break;
		case 2:
			_clockTranslate = (TranslateTransform)target;
			break;
		case 3:
			ClockNumPanel = (StackPanel)target;
			break;
		case 4:
			_clock = (ClockControl)target;
			break;
		case 5:
			_textBlock1 = (TextBlock)target;
			break;
		case 6:
			ClockPomoControl = (FocusBreakControl)target;
			break;
		case 7:
			_buttons1 = (FocusImmerseButtons)target;
			break;
		case 8:
			PageTurnGrid = (Grid)target;
			break;
		case 9:
			_pageTurnTranslate = (TranslateTransform)target;
			break;
		case 10:
			PageTurnNumPanel = (StackPanel)target;
			break;
		case 11:
			_pageTurn = (PageTurnClock)target;
			break;
		case 12:
			_textBlock = (TextBlock)target;
			break;
		case 13:
			PageTurnPomoControl = (FocusBreakControl)target;
			break;
		case 14:
			_buttons = (FocusImmerseButtons)target;
			break;
		case 15:
			_clockEll = (Border)target;
			_clockEll.MouseLeftButtonUp += SwitchClock_MouseLeftButtonUp;
			break;
		case 16:
			_pageTurnEll = (Border)target;
			_pageTurnEll.MouseLeftButtonUp += SwitchTurn_MouseLeftButtonUp;
			break;
		case 17:
			_closeButton = (Border)target;
			_closeButton.MouseLeftButtonUp += _closeButton_MouseLeftButtonUp;
			break;
		case 18:
			ToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
