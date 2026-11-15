using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class CircleMiniFocusControl : UserControl, IMiniFocus, IComponentConnector
{
	private bool _showOp;

	public bool IsLeftMode;

	private bool _moving;

	private bool _morePopupShow;

	private bool _durationPopupShow;

	private bool _autoHideOp = true;

	private static readonly SemaphoreLocker SyncLock = new SemaphoreLocker();

	internal Grid ClockPanel;

	internal Ellipse BackEllipse;

	internal PomoProgressBar Progress;

	internal ClockControl Clock;

	internal Image GotPomo;

	internal ContentControl PopupTarget;

	internal Border OpBorder;

	internal TranslateTransform OpTransform;

	internal StackPanel OpPanel;

	internal Grid OpStart;

	internal Path OpStartIcon;

	internal Border OpStop;

	internal Border OpMore;

	internal EllipseGeometry Geometry1;

	internal EllipseGeometry Geometry2;

	private bool _contentLoaded;

	public PomoStatus Status => TickFocusManager.Status;

	public CircleMiniFocusControl()
	{
		InitializeComponent();
		Clock.ShowHourFont = 12;
	}

	public void OnStatusChanged()
	{
		SetOptionStatus();
		SetTimePanel();
	}

	private void SetOptionStatus()
	{
		bool flag = Status == PomoStatus.WaitingRelax;
		bool flag2 = Status == PomoStatus.Relaxing;
		bool flag3 = Status == PomoStatus.Pause;
		bool flag4 = OpStop.Visibility == Visibility.Visible;
		bool flag5 = OpStart.Visibility == Visibility.Visible;
		OpStart.Visibility = (flag2 ? Visibility.Collapsed : Visibility.Visible);
		OpStop.Visibility = ((!(flag2 || flag3 || flag)) ? Visibility.Collapsed : Visibility.Visible);
		bool flag6 = OpStop.Visibility == Visibility.Visible;
		bool flag7 = OpStart.Visibility == Visibility.Visible;
		if (_showOp && (flag4 != flag6 || flag5 != flag7))
		{
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, (IsLeftMode ? 1 : (-1)) * ((flag6 && flag7) ? 2 : 26), 240);
			OpTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		}
		OpStartIcon.Data = Utils.GetIcon((Status == PomoStatus.Working) ? "IcPomoPause" : "IcPomoStart");
		OpStartIcon.SetResourceReference(Shape.FillProperty, (flag2 || flag) ? "PomoGreen" : "PrimaryColor");
		Clock.SetResourceReference(ClockControl.ForegroundProperty, (flag2 || flag) ? "PomoGreen" : "BaseColorOpacity100");
		Clock.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		Progress.SetResourceReference(PomoProgressBar.TopColorProperty, (flag2 || flag) ? "PomoGreen" : "PrimaryColor");
		switch (Status)
		{
		case PomoStatus.Working:
			OpStart.ToolTip = Utils.GetString("Pause");
			break;
		case PomoStatus.WaitingWork:
			Progress.Reset();
			OpStart.ToolTip = Utils.GetString(TickFocusManager.IsPomo ? "StartPomo" : "StartTiming");
			break;
		case PomoStatus.Relaxing:
			OpStop.ToolTip = Utils.GetString("SkipRelax");
			break;
		case PomoStatus.WaitingRelax:
			OpStart.ToolTip = Utils.GetString("BeginRelax");
			OpStop.ToolTip = Utils.GetString("SkipRelax");
			break;
		case PomoStatus.Pause:
			OpStart.ToolTip = Utils.GetString("Continue");
			OpStop.ToolTip = Utils.GetString("End");
			break;
		}
	}

	public void OnFocusTypeChanged()
	{
		Progress.IsStrokeMode = TickFocusManager.IsPomo;
		Progress.Reset();
		Clock.ShowHour = !TickFocusManager.IsPomo;
		SetCountText();
		SetTimePanel();
	}

	public double GetLeftMargin()
	{
		return 86.0;
	}

	public double GetActualWidth()
	{
		return 70.0;
	}

	public double GetExtraWidth()
	{
		return 90.0;
	}

	public double GetActualHeight()
	{
		return 70.0;
	}

	public void SetMoving(bool b)
	{
		_moving = b;
	}

	public bool CanDragMove()
	{
		if (!_durationPopupShow && !_morePopupShow && !OpPanel.IsMouseOver)
		{
			if (Clock.IsMouseOver)
			{
				return !CanEditDuration();
			}
			return true;
		}
		return false;
	}

	public bool CanHide()
	{
		if (!_morePopupShow)
		{
			return !_durationPopupShow;
		}
		return false;
	}

	public void SetHideStyle(bool isHide)
	{
		_autoHideOp = !isHide;
		if (_autoHideOp)
		{
			if (!base.IsMouseOver && _autoHideOp && _showOp && !_moving && !_morePopupShow && !_durationPopupShow)
			{
				HideOpAnim();
			}
		}
		else if (!_showOp)
		{
			ShowOpAnim();
		}
	}

	public async void OnWindowStartHide()
	{
	}

	public void SetCountText()
	{
		int time = (int)Math.Round(TickFocusManager.Config.CurrentSeconds, 0, MidpointRounding.AwayFromZero);
		Progress.Angle = TickFocusManager.Config.GetDisplayAngle(next: false);
		if (!Progress.IsStrokeMode && TickFocusManager.Config.CurrentSeconds >= 30.0)
		{
			Progress.HideLeftMask();
		}
		Clock.SetTime(time);
	}

	public void Init()
	{
		OnStatusChanged();
		OnFocusTypeChanged();
		SetOpacity(LocalSettings.Settings.PomoWindowOpacity);
		GotPomo.Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/get_pomo.png", 80);
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (!_showOp && !_moving)
		{
			ShowOpAnim();
		}
	}

	public void ShowOpAnim(int duration = 240)
	{
		_showOp = true;
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, (IsLeftMode ? 1 : (-1)) * ((OpStop.IsVisible && OpStart.IsVisible) ? 2 : 26), duration);
		doubleAnimation.EasingFunction = new CubicEase
		{
			EasingMode = EasingMode.EaseOut
		};
		OpTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (_autoHideOp && _showOp && !_moving && !_morePopupShow && !_durationPopupShow)
		{
			HideOpAnim();
		}
	}

	public void HideOpAnim(int duration = 240)
	{
		_showOp = false;
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(null, (IsLeftMode ? 1 : (-1)) * 90, duration);
		doubleAnimation.EasingFunction = new CubicEase
		{
			EasingMode = EasingMode.EaseOut
		};
		OpTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
	}

	public async Task SetPositionMode(bool isRight)
	{
		await SyncLock.LockAsync(async delegate
		{
			if (IsLeftMode != isRight)
			{
				HideOpAnim(100);
				UpdateLayout();
				await Task.Delay(120);
				IsLeftMode = isRight;
				OpTransform.BeginAnimation(TranslateTransform.XProperty, null);
				OpTransform.X = (IsLeftMode ? 1 : (-1)) * 90;
				OpPanel.HorizontalAlignment = ((!isRight) ? HorizontalAlignment.Right : HorizontalAlignment.Left);
				OpBorder.HorizontalAlignment = ((!isRight) ? HorizontalAlignment.Right : HorizontalAlignment.Left);
				Geometry1.Center = (isRight ? new Point(-80.0, 21.0) : new Point(200.0, 21.0));
				Geometry2.Center = (isRight ? new Point(117.5, 21.0) : new Point(-5.5, 21.0));
				if (base.IsMouseOver)
				{
					ShowOpAnim(100);
				}
			}
		});
	}

	public void SetOpacity(double opacity)
	{
		BackEllipse.Opacity = Math.Max(0.01, opacity);
	}

	private void OnOpClick(object sender, MouseButtonEventArgs e)
	{
		switch (Status)
		{
		case PomoStatus.WaitingRelax:
			FocusTimer.BeginTimer();
			break;
		case PomoStatus.WaitingWork:
			FocusTimer.BeginTimer();
			break;
		case PomoStatus.Working:
			FocusTimer.Pause(DateTime.Now);
			break;
		case PomoStatus.Relaxing:
			FocusOptionUploader.AddOption(FocusOption.endBreak, DateTime.Now, manual: true);
			break;
		case PomoStatus.Pause:
			FocusTimer.Continue(DateTime.Now);
			break;
		}
	}

	private void OnStopClick(object sender, MouseButtonEventArgs e)
	{
		FocusTimer.Drop();
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		ShowOperation();
	}

	private void ShowOperation()
	{
		int num = ((OpStop.IsVisible && OpStart.IsVisible) ? 2 : 26);
		PopupTarget.Margin = new Thickness(IsLeftMode ? num : 100, 0.0, IsLeftMode ? 100 : num, 0.0);
		EscPopup obj = new EscPopup
		{
			PlacementTarget = PopupTarget,
			Placement = (IsLeftMode ? PlacementMode.Left : PlacementMode.Right),
			HorizontalOffset = (IsLeftMode ? 3 : (-3)),
			VerticalOffset = -8.0,
			StaysOpen = false
		};
		_morePopupShow = true;
		obj.Closed += delegate
		{
			_morePopupShow = false;
			if (!base.IsMouseOver && _showOp && !_durationPopupShow)
			{
				HideOpAnim();
			}
		};
		obj.Child = new MiniFocusMoreControl(obj, Window.GetWindow(this), FocusWindowType.Circle);
		obj.IsOpen = true;
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		ShowOperation();
	}

	private bool CanEditDuration()
	{
		if (TickFocusManager.IsPomo)
		{
			if (Status != PomoStatus.WaitingWork)
			{
				return Status == PomoStatus.WaitingRelax;
			}
			return true;
		}
		return false;
	}

	private void SetTimePanel()
	{
		if (CanEditDuration())
		{
			Clock.ToolTip = Utils.GetString("ClickToEdit");
			Clock.Cursor = Cursors.Hand;
		}
		else
		{
			Clock.ToolTip = null;
			Clock.Cursor = Cursors.Arrow;
		}
	}

	private void OnTimeAreaClick(object sender, MouseButtonEventArgs e)
	{
		if (!CanEditDuration() || _durationPopupShow)
		{
			return;
		}
		_durationPopupShow = true;
		EscPopup escPopup = new EscPopup();
		escPopup.PlacementTarget = ClockPanel;
		escPopup.Placement = PlacementMode.Bottom;
		escPopup.HorizontalOffset = -5.0;
		escPopup.VerticalOffset = -1.0;
		escPopup.StaysOpen = false;
		escPopup.Closed += delegate
		{
			_durationPopupShow = false;
			if (!base.IsMouseOver && _showOp && _autoHideOp)
			{
				HideOpAnim();
			}
		};
		escPopup.Child = new PomoModitySpanConfig(escPopup, collectEvent: true);
		escPopup.IsOpen = true;
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/circleminifocuscontrol.xaml", UriKind.Relative);
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
			((Grid)target).MouseLeave += OnMouseLeave;
			((Grid)target).MouseRightButtonUp += OnRightClick;
			break;
		case 2:
			ClockPanel = (Grid)target;
			ClockPanel.MouseEnter += OnMouseEnter;
			break;
		case 3:
			BackEllipse = (Ellipse)target;
			break;
		case 4:
			Progress = (PomoProgressBar)target;
			break;
		case 5:
			Clock = (ClockControl)target;
			break;
		case 6:
			GotPomo = (Image)target;
			break;
		case 7:
			PopupTarget = (ContentControl)target;
			break;
		case 8:
			OpBorder = (Border)target;
			break;
		case 9:
			OpTransform = (TranslateTransform)target;
			break;
		case 10:
			OpPanel = (StackPanel)target;
			break;
		case 11:
			OpStart = (Grid)target;
			OpStart.MouseLeftButtonUp += OnOpClick;
			break;
		case 12:
			OpStartIcon = (Path)target;
			break;
		case 13:
			OpStop = (Border)target;
			OpStop.MouseLeftButtonUp += OnStopClick;
			break;
		case 14:
			OpMore = (Border)target;
			OpMore.MouseLeftButtonUp += OnMoreClick;
			break;
		case 15:
			Geometry1 = (EllipseGeometry)target;
			break;
		case 16:
			Geometry2 = (EllipseGeometry)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
