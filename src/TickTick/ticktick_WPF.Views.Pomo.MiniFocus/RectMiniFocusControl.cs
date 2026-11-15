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
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class RectMiniFocusControl : UserControl, IMiniFocus, IComponentConnector
{
	private bool _popupShowing;

	internal ContentControl PopupRect;

	internal Rectangle BackRect;

	internal ClockControl Clock;

	internal BlurEffect ClockBlur;

	internal Image GotPomo;

	internal Grid OpPanel;

	internal Border OpMore;

	internal Border OpStart;

	internal Path OpStartIcon;

	internal Border OpStop;

	private bool _contentLoaded;

	public PomoStatus Status => TickFocusManager.Status;

	public RectMiniFocusControl()
	{
		InitializeComponent();
		Clock.ShowHourFont = 14;
	}

	private async void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (Math.Abs(OpPanel.Opacity - 1.0) > 0.1)
		{
			await Task.Delay(600);
			if (base.IsMouseOver || _popupShowing)
			{
				OpPanel.Opacity = 1.0;
				ClockBlur.Radius = 6.0;
			}
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (!_popupShowing)
		{
			ClockBlur.Radius = 0.0;
			OpPanel.Opacity = 0.0;
		}
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		ShowOperation();
	}

	private void ShowOperation()
	{
		_popupShowing = true;
		EscPopup obj = new EscPopup
		{
			PlacementTarget = PopupRect,
			Placement = PlacementMode.Right,
			HorizontalOffset = -2.0,
			VerticalOffset = -8.0,
			StaysOpen = false
		};
		obj.Child = new MiniFocusMoreControl(obj, Window.GetWindow(this), FocusWindowType.Mini);
		obj.Closed += delegate
		{
			_popupShowing = false;
			if (!base.IsMouseOver)
			{
				ClockBlur.Radius = 0.0;
				OpPanel.Opacity = 0.0;
			}
		};
		obj.IsOpen = true;
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

	public void OnStatusChanged()
	{
		SetTimePanel();
		SetOptionStatus();
	}

	private void SetOptionStatus()
	{
		bool flag = Status == PomoStatus.Relaxing;
		bool flag2 = Status == PomoStatus.Pause;
		bool flag3 = Status == PomoStatus.WaitingRelax;
		OpStart.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		OpStop.Visibility = ((!(flag || flag2 || flag3)) ? Visibility.Collapsed : Visibility.Visible);
		OpStartIcon.Data = Utils.GetIcon((Status == PomoStatus.Working) ? "IcPomoPause" : "IcPomoStart");
		OpStartIcon.SetResourceReference(Shape.FillProperty, (flag || flag3) ? "PomoGreen" : "PrimaryColor");
		Clock.SetResourceReference(ClockControl.ForegroundProperty, (flag || flag3) ? "PomoGreen" : "BaseColorOpacity100");
		Clock.Visibility = (flag3 ? Visibility.Collapsed : Visibility.Visible);
		switch (Status)
		{
		case PomoStatus.Working:
			OpStart.ToolTip = Utils.GetString("Pause");
			break;
		case PomoStatus.WaitingWork:
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

	private void SetTimePanel()
	{
		if (TickFocusManager.IsPomo && (Status == PomoStatus.WaitingRelax || Status == PomoStatus.WaitingWork))
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

	public void OnFocusTypeChanged()
	{
		Clock.ShowHour = !TickFocusManager.IsPomo;
		SetCountText();
	}

	public double GetLeftMargin()
	{
		return 16.0;
	}

	public double GetActualWidth()
	{
		return 70.0;
	}

	public double GetExtraWidth()
	{
		return 0.0;
	}

	public double GetActualHeight()
	{
		return 36.0;
	}

	public void SetMoving(bool b)
	{
	}

	public bool CanDragMove()
	{
		if (!OpMore.IsMouseOver && !OpStart.IsMouseOver)
		{
			return !OpStop.IsMouseOver;
		}
		return false;
	}

	public bool CanHide()
	{
		return !_popupShowing;
	}

	public void SetHideStyle(bool isHide)
	{
	}

	public void OnWindowStartHide()
	{
	}

	public void SetCountText()
	{
		int time = (int)Math.Round(TickFocusManager.Config.CurrentSeconds, 0, MidpointRounding.AwayFromZero);
		Clock.SetTime(time);
	}

	public void Init()
	{
		OnStatusChanged();
		OnFocusTypeChanged();
		SetOpacity(LocalSettings.Settings.PomoWindowOpacity);
		GotPomo.Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/get_pomo.png", 52);
	}

	public void SetOpacity(double opacity)
	{
		BackRect.Opacity = Math.Max(0.01, opacity);
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		ShowOperation();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/rectminifocuscontrol.xaml", UriKind.Relative);
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
			((Grid)target).MouseEnter += OnMouseEnter;
			((Grid)target).MouseLeave += OnMouseLeave;
			break;
		case 2:
			PopupRect = (ContentControl)target;
			break;
		case 3:
			BackRect = (Rectangle)target;
			break;
		case 4:
			Clock = (ClockControl)target;
			break;
		case 5:
			ClockBlur = (BlurEffect)target;
			break;
		case 6:
			GotPomo = (Image)target;
			break;
		case 7:
			OpPanel = (Grid)target;
			OpPanel.MouseRightButtonUp += OnRightClick;
			break;
		case 8:
			OpMore = (Border)target;
			OpMore.MouseLeftButtonUp += OnMoreClick;
			break;
		case 9:
			OpStart = (Border)target;
			OpStart.MouseLeftButtonUp += OnOpClick;
			break;
		case 10:
			OpStartIcon = (Path)target;
			break;
		case 11:
			OpStop = (Border)target;
			OpStop.MouseLeftButtonUp += OnStopClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
