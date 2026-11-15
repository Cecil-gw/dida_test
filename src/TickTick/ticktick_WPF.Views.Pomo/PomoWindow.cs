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
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Lock;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo.MainFocus;
using ticktick_WPF.Views.Pomo.MiniFocus;

namespace ticktick_WPF.Views.Pomo;

public class PomoWindow : UserControl, IMiniFocus, IComponentConnector
{
	[Flags]
	private enum DragSizeMode
	{
		None = 0,
		Top = 1,
		Bottom = 2,
		Left = 4,
		Right = 8
	}

	private bool _popupOpened;

	private DragSizeMode _sizeMode;

	private double _width = 194.0;

	private readonly FocusBreakViewModel _gotPomoViewModel = new FocusBreakViewModel();

	internal PomoWindow Root;

	internal Grid Container;

	internal Border BottomBorder;

	internal Grid MainContainer;

	internal ContentControl MorePopupTarget;

	internal StackPanel OperationPanel;

	internal Border ExpandBtn;

	internal Path ExpandIcon;

	internal Border MoreIcon;

	internal Border CloseBorder;

	internal Path CloseIcon;

	internal ClockControl TimePanel;

	internal StackPanel GotPomoPanel;

	internal Image GotPomo;

	internal StackPanel GotPomoDurationPanel;

	internal TextBlock GotPomoText;

	internal Grid GotPomoAppendBorder;

	internal Rectangle GotPomoAppendRectangle;

	internal TextBlock GotPomoAppendText;

	internal Popup ModifySpanPopup;

	internal DockPanel FilterPanel;

	internal EmjTextBlock FocusTitle;

	internal Path TitlePath;

	internal Popup FilterPopup;

	internal Grid OptionGrid;

	internal PomoProgressBar ProgressBar;

	internal Grid OpStart;

	internal Path LeftOption;

	internal Border OptionSplite;

	internal Border OpStop;

	internal Line SplitLine;

	internal MiniFocusStatisticsView StatisticsView;

	internal Border SizeChangeBorder;

	private bool _contentLoaded;

	private FocusConfig _config => TickFocusManager.Config;

	private PomoStatus Status => _config.Status;

	private bool _isPomo => _config.Type == 0;

	private PomoFilterControl FocusTaskFilter { get; set; }

	public PomoWindow()
	{
		PomoWindow pomoWindow = this;
		InitializeComponent();
		_gotPomoViewModel.PropertyChanged += OnGotPomoViewModelPropertyChanged;
		PomoNotifier.PageChanged += OnPomoPageChanged;
		PomoNotifier.Changed += OnPomoChanged;
		PomoNotifier.ServiceChanged += OnPomoServiceChanged;
		MiniFocusWindow ownerWindow = null;
		base.Loaded += delegate
		{
			ownerWindow = Utils.FindParent<MiniFocusWindow>(pomoWindow);
		};
		base.Unloaded += delegate
		{
			ownerWindow?.LogWindowState("ContentUnloaded", "unloadedControl=PomoWindow");
			pomoWindow.MouseLeave -= pomoWindow.OnMouseLeave;
			pomoWindow._gotPomoViewModel.PropertyChanged -= pomoWindow.OnGotPomoViewModelPropertyChanged;
			pomoWindow._gotPomoViewModel.Dispose();
			PomoNotifier.PageChanged -= pomoWindow.OnPomoPageChanged;
			PomoNotifier.Changed -= pomoWindow.OnPomoChanged;
			PomoNotifier.ServiceChanged -= pomoWindow.OnPomoServiceChanged;
			DelayActionHandlerCenter.RemoveAction("HidePomoWindowOption");
		};
		SetStatisticsVisible();
		SetWidth(LocalSettings.Settings.PomoLocalSetting.MiniWindowWidth);
	}

	private void InitTaskFocusModel(FocusViewModel model = null)
	{
		SetTitle();
		PropertyChangedEventManager.AddHandler(model, OnModelTitleChanged, "Title");
	}

	private void SetTitle()
	{
		if (Status == PomoStatus.Relaxing)
		{
			FocusTitle.Text = Utils.GetString("Relaxing");
			TitlePath.Visibility = Visibility.Collapsed;
			FocusTitle.SetResourceReference(TextBlock.ForegroundProperty, "PomoGreen");
		}
		else
		{
			FocusTitle.Text = (string.IsNullOrEmpty(_config.FocusVModel.Title) ? Utils.GetString("Focus") : _config.FocusVModel.Title);
			FocusTitle.SetResourceReference(TextBlock.ForegroundProperty, string.IsNullOrEmpty(_config.FocusVModel.Title) ? "BaseColorOpacity60" : "BaseColorOpacity100");
			TitlePath.Visibility = Visibility.Visible;
		}
	}

	private void OnModelTitleChanged(object sender, PropertyChangedEventArgs e)
	{
		SetTitle();
	}

	private void InitPomo()
	{
		OnStatusChanged();
		TimePanel.ShowHour = !TickFocusManager.IsPomo;
		ProgressBar.IsStrokeMode = TickFocusManager.IsPomo;
		SetCountText();
	}

	public double GetLeftMargin()
	{
		return 10.0;
	}

	public double GetActualWidth()
	{
		return _width;
	}

	public double GetExtraWidth()
	{
		return 0.0;
	}

	public double GetActualHeight()
	{
		return (double)((LocalSettings.Settings.PomoExpand ? 55 : 0) + 72) * _width / 194.0;
	}

	public void SetMoving(bool b)
	{
	}

	public bool CanDragMove()
	{
		if (!_popupOpened && !StatisticsView.TitleMouseOver)
		{
			return !SizeChangeBorder.IsMouseOver;
		}
		return false;
	}

	public bool CanHide()
	{
		if (!_popupOpened)
		{
			return !StatisticsView.PopupOpened;
		}
		return false;
	}

	public void SetHideStyle(bool isHide)
	{
	}

	public void OnWindowStartHide()
	{
	}

	public void OnFocusTypeChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			ProgressBar.IsStrokeMode = TickFocusManager.IsPomo;
			ProgressBar.Reset();
			TimePanel.ShowHour = !TickFocusManager.IsPomo;
			SetCountText();
			StatisticsView.SetTitleEnable();
			StatisticsView.SetData();
		});
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (Math.Abs(OperationPanel.Opacity - 1.0) > 0.1)
		{
			DelayActionHandlerCenter.TryDoAction("ShowPomoWindowOption", delegate
			{
				base.Dispatcher.Invoke(ShowOptionStory);
			});
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (!_popupOpened)
		{
			HideOptionStory();
		}
	}

	private void ShowOptionStory()
	{
		OperationPanel.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(null, 1.0, 90));
	}

	private void HideOptionStory()
	{
		DelayActionHandlerCenter.RemoveAction("ShowPomoWindowOption");
		OperationPanel.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(null, 0.0, 90));
	}

	public void OnStatusChanged()
	{
		base.Dispatcher?.Invoke(delegate
		{
			SetTimePanel();
			SetOptionStatus();
		});
	}

	private void SetOptionStatus()
	{
		bool flag = Status == PomoStatus.Relaxing;
		bool flag2 = Status == PomoStatus.WaitingRelax;
		bool flag3 = Status == PomoStatus.Pause;
		OpStart.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		OpStop.Visibility = ((!(flag2 || flag || flag3)) ? Visibility.Collapsed : Visibility.Visible);
		OptionSplite.Visibility = ((!(flag3 || flag2)) ? Visibility.Collapsed : Visibility.Visible);
		TimePanel.Visibility = ((Status == PomoStatus.WaitingRelax) ? Visibility.Collapsed : Visibility.Visible);
		double num = (double)((flag3 || flag2) ? 16 : 20) * _width / 194.0;
		LeftOption.Width = num;
		LeftOption.Height = num;
		LeftOption.Data = Utils.GetIcon((Status == PomoStatus.Working) ? "IcPomoPause" : "IcPomoStart");
		ProgressBar.SetResourceReference(PomoProgressBar.TopColorProperty, (flag || flag2) ? "PomoGreen" : "PrimaryColor");
		LeftOption.SetResourceReference(Shape.FillProperty, (flag || flag2) ? "PomoGreen" : "PrimaryColor");
		FilterPanel.Cursor = (flag ? Cursors.Arrow : Cursors.Hand);
		SetTitle();
		UpdateGotPomoDurationDisplay();
		switch (Status)
		{
		case PomoStatus.Working:
			OpStart.ToolTip = Utils.GetString("Pause");
			break;
		case PomoStatus.WaitingWork:
			ProgressBar.Reset();
			OpStart.ToolTip = Utils.GetString(_isPomo ? "StartPomo" : "StartTiming");
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
		if (_isPomo && Status == PomoStatus.WaitingWork)
		{
			TimePanel.ToolTip = Utils.GetString("ClickToEdit");
			TimePanel.Cursor = Cursors.Hand;
		}
		else
		{
			TimePanel.ToolTip = null;
			TimePanel.Cursor = Cursors.Arrow;
		}
		TimePanel.SetResourceReference(ClockControl.ForegroundProperty, (Status == PomoStatus.Relaxing) ? "PomoGreen" : "BaseColorOpacity100");
	}

	private void UpdateGotPomoDurationDisplay()
	{
		if (!_isPomo || Status != PomoStatus.WaitingRelax)
		{
			GotPomoAppendBorder.Visibility = Visibility.Collapsed;
		}
		else
		{
			_gotPomoViewModel.SetText();
		}
	}

	private void OnGotPomoViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		base.Dispatcher?.Invoke(delegate
		{
			switch (e.PropertyName)
			{
			case "BaseDurationText":
				GotPomoText.Text = _gotPomoViewModel.BaseDurationText;
				break;
			case "AppendTimeText":
				GotPomoAppendText.Text = "+" + _gotPomoViewModel.AppendTimeText;
				break;
			case "IsShowAppendTime":
				GotPomoAppendBorder.Visibility = ((!_isPomo || Status != PomoStatus.WaitingRelax || !_gotPomoViewModel.IsShowAppendTime) ? Visibility.Collapsed : Visibility.Visible);
				break;
			}
		});
	}

	private void OnPomoPageChanged(object sender, PomoPageChangeArgs e)
	{
		base.Dispatcher?.Invoke(delegate
		{
			if (_isPomo && Status == PomoStatus.WaitingRelax)
			{
				_gotPomoViewModel.HandlePageChange(e);
			}
		});
	}

	private void OnPomoChanged(object sender, PomoChangeArgs e)
	{
		base.Dispatcher?.Invoke(UpdateGotPomoDurationDisplay);
	}

	private void OnPomoServiceChanged(object sender, EventArgs e)
	{
		base.Dispatcher?.Invoke(UpdateGotPomoDurationDisplay);
	}

	public void Init()
	{
		InitTaskFocusModel(_config.FocusVModel);
		InitPomo();
		GotPomo.Source = ImageUtils.GetResourceImage("pack://application:,,,/Assets/get_pomo.png", 80);
	}

	public void SetOpacity(double opacity)
	{
	}

	public void SetCountText()
	{
		int time = (int)Math.Round(_config.CurrentSeconds, 0, MidpointRounding.AwayFromZero);
		ProgressBar.Angle = _config.GetDisplayAngle(next: false);
		if (!ProgressBar.IsStrokeMode && TickFocusManager.Config.CurrentSeconds >= 30.0)
		{
			ProgressBar.HideLeftMask();
		}
		TimePanel.SetTime(time);
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Utils.FindParent<MiniFocusWindow>(this)?.CloseWithReason("CloseButton");
	}

	private void OnDropClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		FocusTimer.Drop();
	}

	private void OnWindowKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			if (_popupOpened)
			{
				FilterPopup.IsOpen = false;
			}
			break;
		case Key.O:
			ShowFilterPopup();
			break;
		case Key.Return:
			if (!ModifySpanPopup.IsOpen && (Status == PomoStatus.WaitingWork || Status == PomoStatus.WaitingRelax))
			{
				OnOptionButtonClick(null, null);
			}
			break;
		}
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		ShowOperation();
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		ShowOperation();
	}

	private void ShowOperation()
	{
		EscPopup obj = new EscPopup
		{
			PlacementTarget = MorePopupTarget,
			Placement = PlacementMode.Right,
			HorizontalOffset = -2.0,
			VerticalOffset = -8.0,
			StaysOpen = false
		};
		_popupOpened = true;
		obj.Closed += delegate
		{
			_popupOpened = false;
			if (!base.IsMouseOver)
			{
				HideOptionStory();
			}
		};
		obj.Child = new MiniFocusMoreControl(obj, Window.GetWindow(this), FocusWindowType.Normal);
		DelayActionHandlerCenter.RemoveAction("HidePomoWindowOption");
		obj.IsOpen = true;
	}

	private async void OnTimeAreaClick(object sender, MouseButtonEventArgs e)
	{
		if (_isPomo && (Status == PomoStatus.WaitingWork || Status == PomoStatus.WaitingRelax))
		{
			_popupOpened = true;
			ModifySpanPopup.Child = new PomoModitySpanConfig(ModifySpanPopup);
			ModifySpanPopup.IsOpen = true;
		}
	}

	private void OnGotPomoTimeClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (_isPomo && Status == PomoStatus.WaitingRelax)
		{
			Point? durationDialogLocation = GetDurationDialogLocation();
			if (durationDialogLocation.HasValue)
			{
				TickFocusManager.EditDuration(durationDialogLocation.Value);
			}
			else
			{
				TickFocusManager.EditDuration();
			}
		}
	}

	private async void OnGotPomoAppendTimeClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (_isPomo && Status == PomoStatus.WaitingRelax && GotPomoAppendBorder.Visibility == Visibility.Visible)
		{
			await TickFocusManager.AppendDuration(DateTime.Now, null);
		}
	}

	private Point? GetDurationDialogLocation()
	{
		Window window = Window.GetWindow(this);
		if (window == null)
		{
			return null;
		}
		Point value = new Point(window.Left + window.ActualWidth - 10.0, window.Top - 10.0);
		if (SystemParameters.PrimaryScreenWidth - value.X < 240.0)
		{
			value.X = window.Left - 230.0;
		}
		return value;
	}

	private void OnOptionButtonClick(object sender, RoutedEventArgs e)
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

	private async void OnFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (Status != PomoStatus.Relaxing)
		{
			if (await AppLockCache.GetAppLocked())
			{
				AppUnlockWindow.TryUnlockApp(ShowFilterPopup);
			}
			else
			{
				ShowFilterPopup();
			}
		}
	}

	private async void ShowFilterPopup()
	{
		await Task.Delay(100);
		_popupOpened = true;
		if (FocusTaskFilter == null)
		{
			FocusTaskFilter = new PomoFilterControl(FilterPopup, inFocus: true, showDetail: true)
			{
				DataContext = TickFocusManager.Config.FocusVModel
			};
		}
		FilterPopup.IsOpen = true;
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		_popupOpened = false;
		if (!base.IsMouseOver)
		{
			OperationPanel.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(null, 0.0, 120));
		}
	}

	private void OnExpandClick(object sender, MouseButtonEventArgs e)
	{
		LocalSettings.Settings.PomoExpand = !LocalSettings.Settings.PomoExpand;
		SetStatisticsVisible();
		Utils.FindParent<MiniFocusWindow>(this)?.EnsureVisibleAfterContentSizeChanged("Expand");
	}

	private void SetStatisticsVisible()
	{
		StatisticsView.Visibility = ((!LocalSettings.Settings.PomoExpand) ? Visibility.Collapsed : Visibility.Visible);
		ExpandIcon.Data = Utils.GetGeometry(LocalSettings.Settings.PomoExpand ? "Ic.Common.CollapseUpdown" : "Ic.Common.ExpandUpdown");
		ExpandBtn.ToolTip = Utils.GetString(LocalSettings.Settings.PomoExpand ? "Collapse" : "Expand");
	}

	public void ResetStatistics()
	{
		StatisticsView.SetData();
	}

	private void DoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (!FilterPanel.IsMouseOver && !OptionGrid.IsMouseOver && !OperationPanel.IsMouseOver && !StatisticsView.IsMouseOver)
		{
			LocalSettings.Settings.PomoExpand = !LocalSettings.Settings.PomoExpand;
			SetStatisticsVisible();
			Utils.FindParent<MiniFocusWindow>(this)?.EnsureVisibleAfterContentSizeChanged("DoubleClickExpand");
		}
	}

	private void SetWidth(double width)
	{
		if (width < 194.0)
		{
			width = 194.0;
		}
		_width = width;
		LocalSettings.Settings.PomoLocalSetting.MiniWindowWidth = _width;
		double num = _width / 194.0;
		double num2 = 1.0 + (num - 1.0) * 0.87;
		BottomBorder.CornerRadius = new CornerRadius(10.0 * num);
		MainContainer.Width = 194.0 * num;
		MainContainer.Height = 72.0 * num;
		OperationPanel.Height = 10.0 * num2;
		OperationPanel.Margin = new Thickness(0.0, 6.0 * num2, 0.0, 0.0);
		ExpandBtn.Width = 14.0 * num2;
		ExpandBtn.Margin = new Thickness(0.0, 0.0, 5.0 * num2, 0.0);
		ExpandIcon.Height = 10.0 * num2;
		ExpandIcon.Width = 10.0 * num2;
		CloseBorder.Margin = new Thickness(5.0 * num2, 0.0, 6.0 * num2, 0.0);
		CloseIcon.Height = 8.0 * num2;
		CloseIcon.Width = 10.0 * num2;
		TimePanel.Height = 24.0 * num;
		TimePanel.Margin = new Thickness(73.0 * num, 34.0 * num, 4.0 * num, 0.0);
		TimePanel.FontSize = 19.0 * num;
		GotPomoPanel.Margin = new Thickness(45.0 * num, 0.0, 0.0, 0.0);
		GotPomo.Width = 50.0 * num;
		GotPomo.Height = 50.0 * num;
		GotPomoDurationPanel.Margin = new Thickness(6.0 * num, 0.0, 0.0, 0.0);
		GotPomoText.FontSize = 15.0 * num;
		GotPomoAppendBorder.Margin = new Thickness(0.0, 2.0 * num, 0.0, 0.0);
		GotPomoAppendBorder.Height = 16.0 * num;
		GotPomoAppendBorder.MinWidth = 48.0 * num;
		GotPomoAppendRectangle.RadiusX = 8.0 * num;
		GotPomoAppendRectangle.RadiusY = 8.0 * num;
		GotPomoAppendRectangle.StrokeThickness = 0.8 * num;
		GotPomoAppendText.Margin = new Thickness(6.0 * num, 0.0, 6.0 * num, 0.0);
		GotPomoAppendText.FontSize = 10.0 * num;
		FilterPanel.Margin = new Thickness(74.0 * num, 17.0 * num, 8.0 * num, 11.0 * num);
		FocusTitle.FontSize = num2 * 11.0;
		FocusTitle.Height = num2 * 15.0;
		FocusTitle.MaxWidth = 96.0 * num;
		TitlePath.Width = 12.0 * num2;
		TitlePath.Height = 12.0 * num2;
		ProgressBar.Width = 48.0 * num;
		ProgressBar.Height = 48.0 * num;
		ProgressBar.StrokeThickness = 3.0 * num;
		ProgressBar.Margin = new Thickness(15.0 * num, 10.0 * num, 10.0 * num, 10.0 * num);
		bool flag = Status == PomoStatus.WaitingRelax;
		double num3 = (double)((Status == PomoStatus.Pause || flag) ? 16 : 20) * _width / 194.0;
		LeftOption.Width = num3;
		LeftOption.Height = num3;
		OptionSplite.Height = 10.0 * num;
		SplitLine.Width = 188.0 * num;
		StatisticsView.SetSize(num, num2);
	}

	private async void OnSizeChangeMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!Utils.FindParent<MiniFocusWindow>(this).AutoHide && !SizeChangeBorder.IsMouseCaptured)
		{
			Window window = Window.GetWindow(this);
			Point position = e.GetPosition(SizeChangeBorder);
			_sizeMode = GetPointMode(position);
			SizeChangeBorder.CaptureMouse();
			SizeChangeBorder.MouseLeftButtonUp += ReleaseSizeChange;
			Point previousPoint = GetMousePosition();
			while (SizeChangeBorder.IsMouseCaptured)
			{
				await Task.Delay(20);
				Point cursorPosition = NativeUtils.GetCursorPosition();
				Point previousPoint2 = previousPoint;
				previousPoint = cursorPosition;
				ChangeSize(window, previousPoint2, cursorPosition);
			}
		}
	}

	private Point GetMousePosition()
	{
		return NativeUtils.GetCursorPosition();
	}

	private void ReleaseSizeChange(object sender, MouseButtonEventArgs e)
	{
		SizeChangeBorder.MouseLeftButtonUp -= ReleaseSizeChange;
		SizeChangeBorder.ReleaseMouseCapture();
		Utils.FindParent<MiniFocusWindow>(this)?.EnsureVisibleAfterContentSizeChanged("Resize");
	}

	private void ChangeSize(Window window, Point previousPoint, Point point)
	{
		Point point2 = new Point(point.X - previousPoint.X, point.Y - previousPoint.Y);
		if (window == null || (Math.Abs(point2.X) < 2.0 && Math.Abs(point2.Y) < 2.0))
		{
			return;
		}
		bool flag = (_sizeMode & DragSizeMode.Left) == DragSizeMode.Left;
		bool flag2 = (_sizeMode & DragSizeMode.Right) == DragSizeMode.Right;
		bool flag3 = (_sizeMode & DragSizeMode.Top) == DragSizeMode.Top;
		bool flag4 = (_sizeMode & DragSizeMode.Bottom) == DragSizeMode.Bottom;
		double actualWidth = Container.ActualWidth;
		double actualHeight = Container.ActualHeight;
		int num = 194;
		int num2 = 291;
		double num3 = window.Left + 10.0;
		double top = window.Top;
		double num4 = num3 + actualWidth;
		double num5 = top + actualHeight;
		double num6 = (flag ? (-1.0 * point2.X) : (flag2 ? point2.X : 0.0));
		double num7 = (flag3 ? (-1.0 * point2.Y) : (flag4 ? point2.Y : 0.0));
		if (num6 != 0.0 && num7 != 0.0 && ((num6 > 0.0 && num7 < 0.0) || (num6 < 0.0 && num7 > 0.0)))
		{
			num7 = 0.0;
		}
		if ((flag && ((previousPoint.X > num3 + 8.0 && point.X > num3 + 8.0) || (previousPoint.X < num3 - 8.0 && point.X < num3 - 8.0))) || (flag2 && ((previousPoint.X > num4 + 8.0 && point.X > num4 + 8.0) || (previousPoint.X < num4 - 8.0 && point.X < num4 - 8.0))))
		{
			num6 = 0.0;
		}
		if ((flag3 && ((previousPoint.Y > top + 8.0 && point.Y > top + 8.0) || (previousPoint.Y < top - 8.0 && point.Y < top - 8.0))) || (flag4 && ((previousPoint.Y > num5 + 8.0 && point.Y > num5 + 8.0) || (previousPoint.Y < num5 - 8.0 && point.Y < num5 - 8.0))))
		{
			num7 = 0.0;
		}
		double num8 = num7 * actualWidth / actualHeight;
		double num9 = ((Math.Abs(num6) > Math.Abs(num8)) ? num6 : num8);
		if (num9 == 0.0)
		{
			return;
		}
		double val = actualWidth + num9;
		val = Math.Min(num2, Math.Max(num, val));
		if (!(Math.Abs(val - actualWidth) < 1.0) || (!(Math.Abs(val - (double)num) < 0.1) && !(Math.Abs(val - (double)num2) < 0.1)))
		{
			if (flag)
			{
				window.Left = num4 - val - 10.0;
			}
			if (flag3)
			{
				window.Top = num5 - val * actualHeight / actualWidth;
			}
			SetWidth(val);
		}
	}

	private DragSizeMode GetPointMode(Point point)
	{
		if (Utils.FindParent<MiniFocusWindow>(this).AutoHide)
		{
			SizeChangeBorder.Cursor = Cursors.Arrow;
			return DragSizeMode.None;
		}
		bool flag = point.Y <= 8.0;
		bool flag2 = point.X <= 8.0;
		bool flag3 = point.Y >= SizeChangeBorder.ActualHeight - 8.0;
		bool flag4 = point.X >= SizeChangeBorder.ActualWidth - 8.0;
		if ((flag && flag2) || (flag3 && flag4))
		{
			SizeChangeBorder.Cursor = Cursors.SizeNWSE;
			if (!flag)
			{
				return DragSizeMode.Bottom | DragSizeMode.Right;
			}
			return DragSizeMode.Top | DragSizeMode.Left;
		}
		if ((flag && flag4) || (flag3 && flag2))
		{
			SizeChangeBorder.Cursor = Cursors.SizeNESW;
			if (!flag)
			{
				return DragSizeMode.Bottom | DragSizeMode.Left;
			}
			return DragSizeMode.Top | DragSizeMode.Right;
		}
		if (flag || flag3)
		{
			SizeChangeBorder.Cursor = Cursors.SizeNS;
			if (!flag)
			{
				return DragSizeMode.Bottom;
			}
			return DragSizeMode.Top;
		}
		if (flag4 || flag2)
		{
			SizeChangeBorder.Cursor = Cursors.SizeWE;
			if (!flag2)
			{
				return DragSizeMode.Right;
			}
			return DragSizeMode.Left;
		}
		return DragSizeMode.None;
	}

	private void OnBorderMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Released)
		{
			Point position = e.GetPosition(SizeChangeBorder);
			GetPointMode(position);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/pomowindow.xaml", UriKind.Relative);
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
			Root = (PomoWindow)target;
			Root.KeyDown += OnWindowKeyDown;
			Root.MouseEnter += OnMouseEnter;
			Root.MouseLeave += OnMouseLeave;
			Root.MouseDoubleClick += DoubleClick;
			break;
		case 2:
			Container = (Grid)target;
			Container.MouseRightButtonUp += OnRightClick;
			break;
		case 3:
			BottomBorder = (Border)target;
			break;
		case 4:
			MainContainer = (Grid)target;
			break;
		case 5:
			MorePopupTarget = (ContentControl)target;
			break;
		case 6:
			OperationPanel = (StackPanel)target;
			break;
		case 7:
			ExpandBtn = (Border)target;
			ExpandBtn.PreviewMouseLeftButtonUp += OnExpandClick;
			break;
		case 8:
			ExpandIcon = (Path)target;
			break;
		case 9:
			MoreIcon = (Border)target;
			MoreIcon.MouseLeftButtonUp += OnMoreClick;
			break;
		case 10:
			CloseBorder = (Border)target;
			CloseBorder.PreviewMouseLeftButtonUp += OnCloseClick;
			break;
		case 11:
			CloseIcon = (Path)target;
			break;
		case 12:
			TimePanel = (ClockControl)target;
			break;
		case 13:
			GotPomoPanel = (StackPanel)target;
			break;
		case 14:
			GotPomo = (Image)target;
			break;
		case 15:
			GotPomoDurationPanel = (StackPanel)target;
			break;
		case 16:
			GotPomoText = (TextBlock)target;
			GotPomoText.MouseLeftButtonUp += OnGotPomoTimeClick;
			break;
		case 17:
			GotPomoAppendBorder = (Grid)target;
			GotPomoAppendBorder.MouseLeftButtonUp += OnGotPomoAppendTimeClick;
			break;
		case 18:
			GotPomoAppendRectangle = (Rectangle)target;
			break;
		case 19:
			GotPomoAppendText = (TextBlock)target;
			break;
		case 20:
			ModifySpanPopup = (Popup)target;
			ModifySpanPopup.Closed += OnPopupClosed;
			break;
		case 21:
			FilterPanel = (DockPanel)target;
			FilterPanel.MouseLeftButtonUp += OnFilterClick;
			break;
		case 22:
			FocusTitle = (EmjTextBlock)target;
			FocusTitle.MouseLeftButtonUp += OnFilterClick;
			break;
		case 23:
			TitlePath = (Path)target;
			break;
		case 24:
			FilterPopup = (Popup)target;
			FilterPopup.Closed += OnPopupClosed;
			break;
		case 25:
			OptionGrid = (Grid)target;
			break;
		case 26:
			ProgressBar = (PomoProgressBar)target;
			break;
		case 27:
			OpStart = (Grid)target;
			OpStart.MouseLeftButtonUp += OnOptionButtonClick;
			break;
		case 28:
			LeftOption = (Path)target;
			break;
		case 29:
			OptionSplite = (Border)target;
			break;
		case 30:
			OpStop = (Border)target;
			OpStop.MouseLeftButtonUp += OnDropClick;
			break;
		case 31:
			SplitLine = (Line)target;
			break;
		case 32:
			StatisticsView = (MiniFocusStatisticsView)target;
			break;
		case 33:
			SizeChangeBorder = (Border)target;
			SizeChangeBorder.MouseLeftButtonDown += OnSizeChangeMouseDown;
			SizeChangeBorder.MouseMove += OnBorderMouseMove;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
