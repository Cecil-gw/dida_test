using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using ticktick_WPF.Views.CountDown.ViewModels;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountdownStyleTypePreview : UserControl, IComponentConnector, IStyleConnector
{
	private Point scrollStartPoint;

	private bool isDragging;

	internal Grid StyleOptionsPanel;

	internal ScrollViewer scrollViewer;

	internal ItemsControl StyleItems;

	private bool _contentLoaded;

	private CountDownStyleSetViewModel viewModel { get; set; }

	public CountdownStyleTypePreview()
	{
		InitializeComponent();
	}

	private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (viewModel != null)
		{
			CountDownStyleSetViewModel countDownStyleSetViewModel = viewModel;
			countDownStyleSetViewModel.OnCountdownChanged = (Action)Delegate.Remove(countDownStyleSetViewModel.OnCountdownChanged, new Action(OnCountdownChanged));
		}
		viewModel = e.NewValue as CountDownStyleSetViewModel;
		if (viewModel != null)
		{
			CountDownStyleSetViewModel countDownStyleSetViewModel2 = viewModel;
			countDownStyleSetViewModel2.OnCountdownChanged = (Action)Delegate.Combine(countDownStyleSetViewModel2.OnCountdownChanged, new Action(OnCountdownChanged));
		}
	}

	private void UserControl_Unloaded(object sender, RoutedEventArgs e)
	{
		if (viewModel != null)
		{
			CountDownStyleSetViewModel countDownStyleSetViewModel = viewModel;
			countDownStyleSetViewModel.OnCountdownChanged = (Action)Delegate.Remove(countDownStyleSetViewModel.OnCountdownChanged, new Action(OnCountdownChanged));
		}
	}

	private void OnCountdownChanged()
	{
		if (viewModel != null && viewModel.PreviewViewModel.IsProStyle)
		{
			scrollViewer.ScrollToHorizontalOffset(200.0);
		}
	}

	private void OnCardSelected(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		UIElement uIElement = frameworkElement.Parent as UIElement;
		while (uIElement != null && !(uIElement is ContentPresenter))
		{
			uIElement = VisualTreeHelper.GetParent(uIElement) as UIElement;
		}
		if (uIElement != null)
		{
			Point point = uIElement.TransformToAncestor(scrollViewer).Transform(new Point(0.0, 0.0));
			double viewportWidth = scrollViewer.ViewportWidth;
			int num = 100;
			double val = point.X - viewportWidth / 2.0 + (double)(num / 2);
			val = Math.Max(0.0, Math.Min(val, scrollViewer.ScrollableWidth));
			SmoothScrollTo(scrollViewer, val);
			if (frameworkElement.DataContext is CountDownViewModel model)
			{
				viewModel?.OnStyleCardSelect(model);
			}
		}
	}

	private void SmoothScrollTo(ScrollViewer scrollViewer, double targetOffset)
	{
		double currentOffset = scrollViewer.HorizontalOffset;
		double num = Math.Abs(targetOffset - currentOffset);
		if (num < 0.5)
		{
			return;
		}
		double num2 = 250.0;
		double num3 = 500.0;
		double num4 = scrollViewer.ViewportWidth * 0.7;
		double num5 = Math.Min(1.0, num / num4);
		double duration = num2 + num5 * (num3 - num2);
		DispatcherTimer scrollTimer = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(8.0)
		};
		double animationProgress = 0.0;
		DateTime startTime = DateTime.Now;
		scrollTimer.Tick += delegate
		{
			double totalMilliseconds = (DateTime.Now - startTime).TotalMilliseconds;
			animationProgress = Math.Min(1.0, totalMilliseconds / duration);
			if (animationProgress >= 1.0)
			{
				scrollViewer.ScrollToHorizontalOffset(targetOffset);
				scrollTimer.Stop();
			}
			else
			{
				double num6 = EaseOutCubic(animationProgress);
				double a = currentOffset + (targetOffset - currentOffset) * num6;
				a = Math.Round(a);
				scrollViewer.ScrollToHorizontalOffset(a);
			}
		};
		scrollTimer.Start();
	}

	private double EaseOutCubic(double progress)
	{
		return 1.0 - Math.Pow(1.0 - progress, 3.0);
	}

	private void ScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		scrollViewer = sender as ScrollViewer;
		if (scrollViewer != null)
		{
			scrollStartPoint = e.GetPosition(scrollViewer);
			isDragging = true;
		}
	}

	private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (isDragging && scrollViewer != null)
		{
			Point position = e.GetPosition(scrollViewer);
			double num = scrollStartPoint.X - position.X;
			scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + num);
			scrollStartPoint = position;
			scrollViewer.CaptureMouse();
		}
	}

	private void ScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (isDragging && scrollViewer != null)
		{
			isDragging = false;
			scrollViewer.ReleaseMouseCapture();
		}
	}

	private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (scrollViewer != null)
		{
			double num = (double)e.Delta * 0.5;
			SmoothScrollTo(scrollViewer, scrollViewer.HorizontalOffset - num);
			e.Handled = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdownstyletypepreview.xaml", UriKind.Relative);
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
			((CountdownStyleTypePreview)target).DataContextChanged += UserControl_DataContextChanged;
			((CountdownStyleTypePreview)target).Unloaded += UserControl_Unloaded;
			break;
		case 2:
			StyleOptionsPanel = (Grid)target;
			break;
		case 3:
			scrollViewer = (ScrollViewer)target;
			scrollViewer.PreviewMouseLeftButtonDown += ScrollViewer_PreviewMouseLeftButtonDown;
			scrollViewer.PreviewMouseMove += ScrollViewer_PreviewMouseMove;
			scrollViewer.PreviewMouseLeftButtonUp += ScrollViewer_PreviewMouseLeftButtonUp;
			scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
			break;
		case 4:
			StyleItems = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 5)
		{
			((ContentControl)target).MouseLeftButtonUp += OnCardSelected;
		}
	}
}
