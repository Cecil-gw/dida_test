using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Entity.Binding;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Framework;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.AI;

public class AICompleteWindow : Window, IComponentConnector, IStyleConnector
{
	internal AICompleteWindow Root;

	internal Border NormalGrid;

	internal SmoothScrollViewer MainScrollViewer;

	internal ColumnDefinition SummaryLineColumn;

	internal TextBlock SummaryLineTextBlock;

	internal MarkDownEditor SuggestMdEditor;

	internal ItemsControl SubTaskItemsControl;

	internal System.Windows.Controls.TextBox SupplementTextBox;

	internal EscPopup ReBuildPopup;

	internal System.Windows.Controls.TextBox PopupTextBox;

	internal System.Windows.Controls.Button SaveButton;

	internal Grid EditGrid;

	internal System.Windows.Controls.TextBox ReEditTextBox;

	internal Border MiniGrid;

	private bool _contentLoaded;

	private bool isMiniCurrent { get; set; }

	private System.Windows.Size normalWindowSize { get; set; }

	private double normalMinHeight { get; set; }

	private bool isScrolling { get; set; }

	private bool loaded { get; set; }

	private AICompleteViewModel viewModel { get; set; }

	public static AICompleteWindow Window { get; set; }

	public AICompleteWindow(TaskBaseViewModel task)
	{
		InitializeComponent();
		base.DataContext = (viewModel = new AICompleteViewModel(task));
		AICompleteViewModel aICompleteViewModel2 = viewModel;
		aICompleteViewModel2.OnShowRebuildPopup = (Action)Delegate.Combine(aICompleteViewModel2.OnShowRebuildPopup, new Action(OnShowRebuildPopup));
		AICompleteViewModel aICompleteViewModel3 = viewModel;
		aICompleteViewModel3.OnTextboxGotFocus = (Action)Delegate.Combine(aICompleteViewModel3.OnTextboxGotFocus, new Action(OnTextboxGotFocus));
		AICompleteViewModel aICompleteViewModel4 = viewModel;
		aICompleteViewModel4.OnUpdateSummaryLineButton = (Action)Delegate.Combine(aICompleteViewModel4.OnUpdateSummaryLineButton, new Action(OnUpdateSummaryLineButton));
		AICompleteViewModel aICompleteViewModel5 = viewModel;
		aICompleteViewModel5.OnCloseWindow = (Action)Delegate.Combine(aICompleteViewModel5.OnCloseWindow, new Action(OnCloseWindow));
		AICompleteViewModel aICompleteViewModel6 = viewModel;
		aICompleteViewModel6.OnScrollToBottom = (Action)Delegate.Combine(aICompleteViewModel6.OnScrollToBottom, new Action(OnScrollToBottom));
		AICompleteViewModel aICompleteViewModel7 = viewModel;
		aICompleteViewModel7.OnSuggestionMdFocus = (Action)Delegate.Combine(aICompleteViewModel7.OnSuggestionMdFocus, new Action(OnSuggestionMdFocus));
		MainWindow window = App.Window;
		window.OnModuleSwitch = (Action<DisplayModule>)Delegate.Combine(window.OnModuleSwitch, new Action<DisplayModule>(OnModuleSwitch));
		WindowNotifier.OnSubWindowOpen += WindowNotifier_OnSubWindowOpen;
		SuggestMdEditor.EditorEnabled = false;
		SuggestMdEditor.StyleEnabled = false;
	}

	private void OnSuggestionMdFocus()
	{
		SuggestMdEditor.FocusEnd();
		MainScrollViewer.Focus();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		WindowNotifier.NotifySubWindowOpen(SubWindowType.AIComplete);
		loaded = true;
		UpdateWindowPosition();
		if (base.Owner != null)
		{
			base.Owner.LocationChanged += Owner_LocationChanged;
			base.Owner.SizeChanged += Owner_SizeChanged;
			base.Owner.IsVisibleChanged += Owner_IsVisibleChanged;
			base.Owner.StateChanged += Owner_StateChanged;
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (base.Owner != null)
		{
			base.Owner.LocationChanged -= Owner_LocationChanged;
			base.Owner.SizeChanged -= Owner_SizeChanged;
			base.Owner.IsVisibleChanged -= Owner_IsVisibleChanged;
			base.Owner.StateChanged -= Owner_StateChanged;
			base.Owner.Activate();
		}
		AICompleteViewModel aICompleteViewModel = viewModel;
		aICompleteViewModel.OnShowRebuildPopup = (Action)Delegate.Remove(aICompleteViewModel.OnShowRebuildPopup, new Action(OnShowRebuildPopup));
		AICompleteViewModel aICompleteViewModel2 = viewModel;
		aICompleteViewModel2.OnTextboxGotFocus = (Action)Delegate.Remove(aICompleteViewModel2.OnTextboxGotFocus, new Action(OnTextboxGotFocus));
		AICompleteViewModel aICompleteViewModel3 = viewModel;
		aICompleteViewModel3.OnUpdateSummaryLineButton = (Action)Delegate.Remove(aICompleteViewModel3.OnUpdateSummaryLineButton, new Action(OnUpdateSummaryLineButton));
		AICompleteViewModel aICompleteViewModel4 = viewModel;
		aICompleteViewModel4.OnCloseWindow = (Action)Delegate.Remove(aICompleteViewModel4.OnCloseWindow, new Action(OnCloseWindow));
		AICompleteViewModel aICompleteViewModel5 = viewModel;
		aICompleteViewModel5.OnScrollToBottom = (Action)Delegate.Remove(aICompleteViewModel5.OnScrollToBottom, new Action(OnScrollToBottom));
		AICompleteViewModel aICompleteViewModel6 = viewModel;
		aICompleteViewModel6.OnSuggestionMdFocus = (Action)Delegate.Remove(aICompleteViewModel6.OnSuggestionMdFocus, new Action(OnSuggestionMdFocus));
		MainWindow window = App.Window;
		window.OnModuleSwitch = (Action<DisplayModule>)Delegate.Remove(window.OnModuleSwitch, new Action<DisplayModule>(OnModuleSwitch));
		WindowNotifier.OnSubWindowOpen -= WindowNotifier_OnSubWindowOpen;
		Window = null;
	}

	private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateWindowPosition();
	}

	private void Owner_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!AppLockWindowManager.IsLocked)
		{
			base.Visibility = ((e.NewValue as bool? != true) ? Visibility.Hidden : Visibility.Visible);
		}
	}

	private void Owner_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateWindowPosition();
	}

	private void Owner_LocationChanged(object sender, EventArgs e)
	{
		UpdateWindowPosition();
	}

	private async void Owner_StateChanged(object sender, EventArgs e)
	{
		UpdateWindowPosition();
	}

	private void UpdateWindowPosition()
	{
		if (base.Owner != null)
		{
			Rect windowRectangle = GetWindowRectangle(base.Owner);
			base.MaxHeight = windowRectangle.Height;
			if (!isMiniCurrent)
			{
				base.MinHeight = Math.Min(base.MaxHeight, base.ActualHeight);
			}
			base.Left = windowRectangle.Left + windowRectangle.Width - base.ActualWidth;
			base.Top = windowRectangle.Top + windowRectangle.Height - base.ActualHeight;
		}
	}

	public static Rect GetWindowRectangle(Window w)
	{
		if (w.WindowState == WindowState.Maximized)
		{
			Rectangle workingArea = Screen.FromHandle(new WindowInteropHelper(w).Handle).WorkingArea;
			double pixelsPerDip = VisualTreeHelper.GetDpi(w).PixelsPerDip;
			return new Rect((double)workingArea.Left / pixelsPerDip, (double)workingArea.Top / pixelsPerDip, (double)workingArea.Width / pixelsPerDip, (double)workingArea.Height / pixelsPerDip);
		}
		return new Rect((int)w.Left, (int)w.Top, (int)w.ActualWidth, (int)w.ActualHeight);
	}

	private void MiniModeButton_Click(object sender, RoutedEventArgs e)
	{
		SwitchMini(toMini: true);
	}

	private void NormalMode_PreviewMouseUp(object sender, MouseButtonEventArgs e)
	{
		SwitchMini(toMini: false);
	}

	private async Task SwitchMini(bool toMini)
	{
		if (isMiniCurrent != toMini)
		{
			isMiniCurrent = toMini;
			if (toMini)
			{
				viewModel.HasMinied = true;
				normalWindowSize = new System.Windows.Size(base.ActualWidth, base.ActualHeight);
				normalMinHeight = base.MinHeight;
				base.Opacity = 0.0;
				await Task.Delay(60);
				base.SizeToContent = SizeToContent.Manual;
				base.MinHeight = 0.0;
				base.Width = 100.0;
				base.Height = 120.0;
				NormalGrid.Visibility = Visibility.Collapsed;
				MiniGrid.Visibility = Visibility.Visible;
				base.Opacity = 1.0;
			}
			else
			{
				base.Opacity = 0.0;
				await Task.Delay(20);
				base.MinHeight = normalMinHeight;
				base.Width = normalWindowSize.Width;
				base.Height = normalWindowSize.Height;
				base.SizeToContent = SizeToContent.Height;
				MiniGrid.Visibility = Visibility.Collapsed;
				NormalGrid.Visibility = Visibility.Visible;
				base.Opacity = 1.0;
			}
		}
	}

	public void OnModuleSwitch(DisplayModule module)
	{
		if (module != DisplayModule.Task)
		{
			SwitchMini(toMini: true);
		}
	}

	private void WindowNotifier_OnSubWindowOpen(object sender, SubWindowType e)
	{
		if (e != SubWindowType.Search && e != SubWindowType.AIComplete)
		{
			SwitchMini(toMini: true);
		}
	}

	private void MarkDownEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
		MouseWheelEventArgs e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
		{
			RoutedEvent = UIElement.MouseWheelEvent,
			Source = sender
		};
		(((System.Windows.Controls.Control)sender).Parent as UIElement)?.RaiseEvent(e2);
	}

	private void MainScrollViewer_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			viewModel.ConfirmEdit();
		}
	}

	private void SubTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Return:
			if ((sender as System.Windows.Controls.Control).DataContext is AISubTaskEditItem item2)
			{
				int num2 = viewModel.SubTasks.IndexOf(item2);
				viewModel.SubTasks.Insert(num2 + 1, new AISubTaskEditItem
				{
					NeedFocusInLoad = true
				});
			}
			break;
		case Key.Back:
		case Key.Delete:
		{
			if (!((sender as System.Windows.Controls.Control).DataContext is AISubTaskEditItem aISubTaskEditItem) || !string.IsNullOrEmpty(aISubTaskEditItem.Name))
			{
				break;
			}
			int num = viewModel.SubTasks.IndexOf(aISubTaskEditItem);
			viewModel.SubTasks.RemoveAt(num);
			if (viewModel.SubTasks.Any())
			{
				int index = Math.Max(0, num - 1);
				AISubTaskEditItem item = viewModel.SubTasks[index];
				System.Windows.Controls.TextBox textBox = FindVisualChild<System.Windows.Controls.TextBox>(SubTaskItemsControl.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter);
				if (textBox != null)
				{
					textBox.SelectionStart = textBox.Text.Length;
					textBox.Focus();
				}
			}
			break;
		}
		}
	}

	public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
	{
		if (depObj != null)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
				if (child != null && child is T)
				{
					return (T)child;
				}
				T val = FindVisualChild<T>(child);
				if (val != null)
				{
					return val;
				}
			}
		}
		return null;
	}

	private void OnTextboxGotFocus()
	{
		if (viewModel.IsShowFeedbackPopup)
		{
			PopupTextBox.Focus();
		}
		else if (viewModel.IsShowSupplementTextbox)
		{
			SupplementTextBox.Focus();
		}
		else
		{
			ReEditTextBox.Focus();
		}
	}

	private void SubTaskTextBox_Loaded(object sender, RoutedEventArgs e)
	{
		System.Windows.Controls.Control control = sender as System.Windows.Controls.Control;
		if (control.DataContext is AISubTaskEditItem { NeedFocusInLoad: not false } aISubTaskEditItem)
		{
			control.Focus();
			aISubTaskEditItem.NeedFocusInLoad = false;
		}
	}

	private void OnUpdateSummaryLineButton()
	{
		if (viewModel.CurrentPage.Page != 1)
		{
			System.Windows.Application.Current.Dispatcher.Invoke((Func<Task>)async delegate
			{
				await Task.Delay(100);
				viewModel.IsShowExpandSummaryButton = SummaryLineTextBlock.ActualWidth > SummaryLineColumn.ActualWidth;
			});
		}
	}

	private void OnShowRebuildPopup()
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel("RebuildAll", Utils.GetString("AIRegenerate"), Utils.GetGeometry("Ic.AI.Refresh"))
			{
				ShowSelected = false
			},
			new CustomMenuItemViewModel("RebuildSubTask", Utils.GetString("AIGenerateSubTaskAgain"), Utils.GetIcon("IcRebuildSubTask"))
			{
				ShowSelected = false
			}
		}, ReBuildPopup);
		customMenuList.Operated += OnReBuildItemSelected;
		customMenuList.Show();
	}

	private void OnReBuildItemSelected(object sender, object e)
	{
		string text = e.ToString();
		if (!(text == "RebuildAll"))
		{
			if (text == "RebuildSubTask")
			{
				viewModel.RebuildMenuSelect(onlySubTask: true);
			}
		}
		else
		{
			viewModel.RebuildMenuSelect(onlySubTask: false);
		}
	}

	private void FeedBackPopup_Closed(object sender, EventArgs e)
	{
		viewModel.IsShowFeedbackPopup = false;
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnCloseWindow()
	{
		Close();
	}

	private async void OnScrollToBottom()
	{
		await Task.Delay(80);
		MainScrollViewer.SmoothScrollToBottom();
	}

	private void ReEditButton_Click(object sender, RoutedEventArgs e)
	{
		if (MainScrollViewer.VerticalOffset + MainScrollViewer.ViewportHeight == MainScrollViewer.ExtentHeight)
		{
			MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - 0.5);
		}
	}

	private async void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (!isScrolling && !(MainScrollViewer.ScrollableHeight <= 0.0) && viewModel.State == AICompleteState.Building)
		{
			isScrolling = true;
			await Task.Delay(100);
			MainScrollViewer.SmoothScrollToBottom();
			isScrolling = false;
		}
	}

	private void SubDateButton_Click(object sender, RoutedEventArgs e)
	{
		if (sender is System.Windows.Controls.Button button)
		{
			AISubTaskEditItem item = button.DataContext as AISubTaskEditItem;
			SelectDate(item, button, -128.0, 0.0);
		}
	}

	private void SelectDate(AISubTaskEditItem item, FrameworkElement target = null, double hOffset = 100.0, double vOffset = 12.0)
	{
		SetDateDialog dialog = SetDateDialog.GetDialog();
		dialog.ClearEventHandle();
		dialog.Clear += delegate
		{
			OnSubTaskDateTimeUpdate(item, null);
		};
		dialog.Save += delegate(object sender, TimeData data)
		{
			OnSubTaskDateTimeUpdate(item, data);
		};
		dialog.Show(item.TimeData, new SetDateDialogArgs(calendarMode: false, isNote: false, itemMode: false, target, hOffset, vOffset, PlacementMode.Bottom, showQuickDate: true, canSkip: false));
	}

	private void OnSubTaskDateTimeUpdate(AISubTaskEditItem item, TimeData timeData)
	{
		if (timeData != null)
		{
			TaskService.TryFixRepeatFlag(timeData);
		}
		item.TimeData = timeData;
		if (timeData != null && timeData.StartDate.HasValue && !Utils.IsEmptyDate(timeData.StartDate.Value))
		{
			item.DateText = DateUtils.FormatListDateString(timeData.StartDate.Value, timeData.DueDate, timeData.IsAllDay);
		}
		else
		{
			item.DateText = string.Empty;
		}
		if (timeData != null && DateUtils.IsOutDated(timeData.StartDate, timeData.DueDate, timeData.IsAllDay))
		{
			item.DateTextColor = ThemeUtil.GetColor("OutDateColor");
		}
		else
		{
			item.DateTextColor = ThemeUtil.GetColor("DateColorPrimary", ThemeUtil.GetColor("ThemeBlue"));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aicompletewindow.xaml", UriKind.Relative);
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
			Root = (AICompleteWindow)target;
			Root.Loaded += Window_Loaded;
			Root.Closing += Window_Closing;
			Root.SizeChanged += Window_SizeChanged;
			break;
		case 2:
			NormalGrid = (Border)target;
			break;
		case 3:
			((System.Windows.Controls.Button)target).Click += MiniModeButton_Click;
			break;
		case 4:
			((System.Windows.Controls.Button)target).Click += OnCloseClick;
			break;
		case 5:
			MainScrollViewer = (SmoothScrollViewer)target;
			break;
		case 6:
			SummaryLineColumn = (ColumnDefinition)target;
			break;
		case 7:
			SummaryLineTextBlock = (TextBlock)target;
			break;
		case 8:
			SuggestMdEditor = (MarkDownEditor)target;
			break;
		case 9:
			SubTaskItemsControl = (ItemsControl)target;
			break;
		case 13:
			SupplementTextBox = (System.Windows.Controls.TextBox)target;
			SupplementTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			break;
		case 14:
			ReBuildPopup = (EscPopup)target;
			break;
		case 15:
			((System.Windows.Controls.Button)target).Click += ReEditButton_Click;
			break;
		case 16:
			((Popup)target).Closed += FeedBackPopup_Closed;
			break;
		case 17:
			PopupTextBox = (System.Windows.Controls.TextBox)target;
			break;
		case 18:
			SaveButton = (System.Windows.Controls.Button)target;
			break;
		case 19:
			EditGrid = (Grid)target;
			break;
		case 20:
			ReEditTextBox = (System.Windows.Controls.TextBox)target;
			ReEditTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			break;
		case 21:
			MiniGrid = (Border)target;
			MiniGrid.PreviewMouseUp += NormalMode_PreviewMouseUp;
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
		case 10:
			((System.Windows.Controls.TextBox)target).KeyUp += SubTextBox_KeyUp;
			((System.Windows.Controls.TextBox)target).Loaded += SubTaskTextBox_Loaded;
			break;
		case 11:
			((System.Windows.Controls.Button)target).Click += SubDateButton_Click;
			break;
		case 12:
			((System.Windows.Controls.Button)target).Click += SubDateButton_Click;
			break;
		}
	}
}
