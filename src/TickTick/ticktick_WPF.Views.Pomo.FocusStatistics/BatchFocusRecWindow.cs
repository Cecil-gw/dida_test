using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using ticktick_WPF.Notifier;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class BatchFocusRecWindow : Window, IComponentConnector, IStyleConnector
{
	private Point scrollStartPoint;

	private bool isDragging;

	internal CustomComboBox DateCombo;

	internal CustomComboBox DurationLenCombo;

	internal CustomComboBox AssoicateCombo;

	internal TextBox CustomStartDuraTextBox;

	internal TextBox CustomEndDuraTextBox;

	internal Border CloseButton;

	private bool _contentLoaded;

	private BatchFocusRecViewModel viewModel { get; set; }

	public BatchFocusRecWindow()
	{
		InitializeComponent();
		base.DataContext = (viewModel = new BatchFocusRecViewModel());
		PomoNotifier.Changed += OnPomoChanged;
		viewModel.Init();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		viewModel.IsShowWindow = false;
		PomoNotifier.Changed -= OnPomoChanged;
		PomoNotifier.NotifyChanged(string.Empty, PomoChangeType.Updated);
	}

	private void OnPomoChanged(object sender, PomoChangeArgs e)
	{
		viewModel.OnNotifyItemChanged(e);
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Close();
	}

	private void ListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		ListView listView = sender as ListView;
		if (listView.Template.FindName("ScrollViewer", listView) is ScrollViewer scrollViewer && viewModel.FilterItems?.LastOrDefault() is FocusRecordLoadMoreViewModel { NeedClick: false } && e.VerticalOffset >= scrollViewer.ScrollableHeight - 10.0)
		{
			viewModel.LoadMore();
		}
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		if (e.Source == this)
		{
			base.OnMouseLeftButtonDown(e);
			DragMove();
		}
	}

	private async void DeleteButton_Click(object sender, RoutedEventArgs e)
	{
		await viewModel.DeleteSelected();
	}

	private void CustomComboBox_ItemSelected(object sender, ComboBoxViewModel e)
	{
		if (sender is CustomComboBox customComboBox)
		{
			BatchFocusFilterType? batchFocusFilterType = null;
			switch (customComboBox.Name)
			{
			case "DateCombo":
				batchFocusFilterType = BatchFocusFilterType.Date;
				break;
			case "DurationLenCombo":
				batchFocusFilterType = BatchFocusFilterType.Duration;
				break;
			case "AssoicateCombo":
				batchFocusFilterType = BatchFocusFilterType.Assoicate;
				break;
			}
			if (batchFocusFilterType.HasValue)
			{
				viewModel.OnComboSelectChange(batchFocusFilterType.Value);
			}
		}
	}

	private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
	{
		if (viewModel.IsLoading)
		{
			return;
		}
		viewModel.IsLoading = true;
		try
		{
			DateCombo.SetSelected(DateFilterType.All);
			DurationLenCombo.SetSelected(DurationLenFilterType.All);
			AssoicateCombo.SetSelected(AssociationStatusType.All);
		}
		catch (Exception)
		{
		}
		finally
		{
			viewModel.IsLoading = false;
		}
		viewModel.ClearFilter();
	}

	public void LoadMore()
	{
		viewModel.LoadMore();
	}

	private void SelectClearButton_Click(object sender, RoutedEventArgs e)
	{
		viewModel.SelectedAllOrClear();
	}

	private void SaveCustomDuration_Click(object sender, RoutedEventArgs e)
	{
		if (viewModel.CheckCustomDurationValid())
		{
			viewModel.IsConfirmCustomDuration = true;
			viewModel.IsShowCustomDurationPopup = false;
		}
	}

	private async void CancelCustomDuration_Click(object sender, RoutedEventArgs e)
	{
		viewModel.IsShowCustomDurationPopup = false;
	}

	private void CustomDurationPopup_Closed(object sender, EventArgs e)
	{
		viewModel.SetCustomDuration();
		DurationLenCombo.SetSelected(viewModel.CurrentDurationLenFilter);
	}

	private void OnlyNumberInput(object sender, KeyEventArgs e)
	{
		if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
		{
			e.Handled = true;
		}
		else if ((e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9) && e.Key != Key.Back && e.Key != Key.Delete && e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Up && e.Key != Key.Down)
		{
			e.Handled = true;
		}
	}

	private void SaveCustomDate_Click(object sender, RoutedEventArgs e)
	{
		if (viewModel.CheckCustomDateValid())
		{
			viewModel.IsConfirmCustomDate = true;
			viewModel.IsShowCustomDatePopup = false;
		}
	}

	private async void CancelCustomDate_Click(object sender, RoutedEventArgs e)
	{
		viewModel.IsShowCustomDatePopup = false;
	}

	private void CustomDatePopup_Closed(object sender, EventArgs e)
	{
		viewModel.SetCustomDate();
		DateCombo.SetSelected(viewModel.CurrentDateFilter);
	}

	private async void CustomDurationPopup_Opened(object sender, EventArgs e)
	{
		await Task.Delay(100);
		bool flag = string.IsNullOrEmpty(viewModel.CustomStartDuraText);
		bool flag2 = string.IsNullOrEmpty(viewModel.CustomEndDuraText);
		if (flag && flag2)
		{
			CustomStartDuraTextBox.Focus();
		}
		else if (!flag && !flag2)
		{
			CustomEndDuraTextBox.Focus();
		}
		else if (!flag)
		{
			CustomStartDuraTextBox.Focus();
		}
		else
		{
			CustomEndDuraTextBox.Focus();
		}
	}

	private void ListViewItem_RequestBringIntoView(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
	}

	private void ComboScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		if (sender is ScrollViewer relativeTo)
		{
			scrollStartPoint = e.GetPosition(relativeTo);
			isDragging = true;
		}
	}

	private void ComboScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		if (isDragging && scrollViewer != null)
		{
			Point position = e.GetPosition(scrollViewer);
			double num = scrollStartPoint.X - position.X;
			scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + num);
			scrollStartPoint = position;
		}
	}

	private void ComboScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		if (isDragging && scrollViewer != null)
		{
			isDragging = false;
		}
	}

	private void ComboScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		if (scrollViewer != null)
		{
			double num = (double)e.Delta * 0.5;
			SmoothScrollTo(scrollViewer, scrollViewer.HorizontalOffset - num);
			e.Handled = true;
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
				double num6 = 1.0 - Math.Pow(1.0 - animationProgress, 3.0);
				double a = currentOffset + (targetOffset - currentOffset) * num6;
				a = Math.Round(a);
				scrollViewer.ScrollToHorizontalOffset(a);
			}
		};
		scrollTimer.Start();
	}

	private void FilterComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		isDragging = false;
		(sender as CustomComboBox)?.BringIntoView();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/focusstatistics/batchfocusrecwindow.xaml", UriKind.Relative);
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
			((BatchFocusRecWindow)target).Closing += Window_Closing;
			break;
		case 2:
			((ScrollViewer)target).PreviewMouseWheel += ComboScrollViewer_PreviewMouseWheel;
			((ScrollViewer)target).PreviewMouseLeftButtonDown += ComboScrollViewer_PreviewMouseLeftButtonDown;
			((ScrollViewer)target).PreviewMouseMove += ComboScrollViewer_PreviewMouseMove;
			((ScrollViewer)target).PreviewMouseLeftButtonUp += ComboScrollViewer_PreviewMouseLeftButtonUp;
			break;
		case 3:
			DateCombo = (CustomComboBox)target;
			break;
		case 4:
			DurationLenCombo = (CustomComboBox)target;
			break;
		case 5:
			AssoicateCombo = (CustomComboBox)target;
			break;
		case 6:
			((Button)target).Click += SaveCustomDate_Click;
			break;
		case 7:
			((Button)target).Click += CancelCustomDate_Click;
			break;
		case 9:
			CustomStartDuraTextBox = (TextBox)target;
			break;
		case 10:
			CustomEndDuraTextBox = (TextBox)target;
			break;
		case 11:
			((Button)target).Click += SaveCustomDuration_Click;
			break;
		case 12:
			((Button)target).Click += CancelCustomDuration_Click;
			break;
		case 13:
			((Button)target).Click += ClearFilterButton_Click;
			break;
		case 14:
			((ListView)target).AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ListView_ScrollChanged));
			break;
		case 16:
			((Button)target).Click += SelectClearButton_Click;
			break;
		case 17:
			((Button)target).Click += DeleteButton_Click;
			break;
		case 18:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
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
		switch (connectionId)
		{
		case 8:
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = UIElement.PreviewKeyDownEvent;
			eventSetter.Handler = new KeyEventHandler(OnlyNumberInput);
			((Style)target).Setters.Add(eventSetter);
			break;
		}
		case 15:
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = FrameworkElement.RequestBringIntoViewEvent;
			eventSetter.Handler = new RequestBringIntoViewEventHandler(ListViewItem_RequestBringIntoView);
			((Style)target).Setters.Add(eventSetter);
			break;
		}
		}
	}
}
