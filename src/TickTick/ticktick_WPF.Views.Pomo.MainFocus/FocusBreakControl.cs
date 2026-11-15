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
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.MainFocus;

public class FocusBreakControl : UserControl, IComponentConnector
{
	private bool _isDark;

	internal Grid MainTimeGrid;

	internal Grid AppendTimeGrid;

	private bool _contentLoaded;

	public bool IsDark
	{
		get
		{
			return _isDark;
		}
		set
		{
			_isDark = value;
			viewModel.IsDark = value;
		}
	}

	private bool isloaded { get; set; }

	private FocusBreakViewModel viewModel { get; set; }

	public FocusBreakControl()
	{
		InitializeComponent();
		base.DataContext = (viewModel = new FocusBreakViewModel());
		FocusBreakViewModel focusBreakViewModel2 = viewModel;
		focusBreakViewModel2.OnShowAppendTimeAnimate = (Action<bool>)Delegate.Combine(focusBreakViewModel2.OnShowAppendTimeAnimate, new Action<bool>(OnShowAppendTimeAnimate));
		PomoNotifier.PageChanged += PomoNotifier_PageChanged;
	}

	private async void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		await Task.Delay(1000);
		isloaded = true;
	}

	private void OnShowAppendTimeAnimate(bool show)
	{
		if (!isloaded)
		{
			if (show)
			{
				MainTimeGrid.Margin = new Thickness(0.0, 0.0, 0.0, 18.0);
				AppendTimeGrid.Opacity = 1.0;
			}
			else
			{
				MainTimeGrid.Margin = new Thickness(0.0);
				AppendTimeGrid.Opacity = 0.0;
			}
		}
		else
		{
			ThicknessAnimation animation = new ThicknessAnimation
			{
				Duration = TimeSpan.FromSeconds(0.2),
				To = (show ? new Thickness(0.0, 0.0, 0.0, 18.0) : new Thickness(0.0))
			};
			MainTimeGrid.BeginAnimation(FrameworkElement.MarginProperty, animation);
			DoubleAnimation animation2 = new DoubleAnimation
			{
				Duration = TimeSpan.FromSeconds(0.2),
				To = (show ? 1 : 0)
			};
			AppendTimeGrid.BeginAnimation(UIElement.OpacityProperty, animation2);
		}
	}

	private void PomoNotifier_PageChanged(object sender, PomoPageChangeArgs e)
	{
		viewModel.HandlePageChange(e);
	}

	public void SetRelaxText()
	{
		viewModel.SetText();
	}

	private void OnEditDuration(object sender, MouseButtonEventArgs e)
	{
		EditDuration();
	}

	public void EditDuration()
	{
		if (IsDark)
		{
			FocusImmerseWindow focusImmerseWindow = Utils.FindParent<FocusImmerseWindow>(this);
			TickFocusManager.EditDuration(null, null, focusImmerseWindow?.ToastGrid, focusImmerseWindow, isDark: true);
			return;
		}
		MainFocusControl mainFocusControl = Utils.FindParent<MainFocusControl>(this);
		Point point = mainFocusControl.PointToScreen(new Point(0.0, 0.0));
		DpiScale dpi = VisualTreeHelper.GetDpi(App.Window);
		Rect value = new Rect(point.X / dpi.PixelsPerDip, point.Y / dpi.PixelsPerDip, mainFocusControl.ActualWidth, mainFocusControl.ActualHeight);
		TickFocusManager.EditDuration(null, value, mainFocusControl?.ToastGrid);
	}

	private void OnAppendTime(object sender, MouseButtonEventArgs e)
	{
		DateTime now = DateTime.Now;
		if (IsDark)
		{
			TickFocusManager.AppendDuration(now, Utils.FindParent<FocusImmerseWindow>(this)?.ToastGrid);
		}
		else
		{
			TickFocusManager.AppendDuration(now, Utils.FindParent<MainFocusControl>(this)?.ToastGrid);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/mainfocus/focusbreakcontrol.xaml", UriKind.Relative);
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
			((FocusBreakControl)target).Loaded += UserControl_Loaded;
			break;
		case 2:
			MainTimeGrid = (Grid)target;
			break;
		case 3:
			((Grid)target).PreviewMouseLeftButtonDown += OnEditDuration;
			break;
		case 4:
			AppendTimeGrid = (Grid)target;
			AppendTimeGrid.MouseLeftButtonUp += OnAppendTime;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
