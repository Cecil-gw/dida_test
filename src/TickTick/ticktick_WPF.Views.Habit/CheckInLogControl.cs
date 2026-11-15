using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class CheckInLogControl : UserControl, IComponentConnector
{
	public static readonly DependencyProperty ShowEditProperty = DependencyProperty.Register("ShowEdit", typeof(bool), typeof(CheckInLogControl), new PropertyMetadata(true));

	internal CheckInLogControl Root;

	internal TextBox ContentText;

	internal StackPanel ExpandToggle;

	private bool _contentLoaded;

	public bool ShowEdit
	{
		get
		{
			return (bool)GetValue(ShowEditProperty);
		}
		set
		{
			SetValue(ShowEditProperty, value);
		}
	}

	public CheckInLogControl()
	{
		InitializeComponent();
	}

	private void OnExpandClick(object sender, MouseButtonEventArgs e)
	{
		ContentText.MaxLines = ((ContentText.MaxLines <= 3) ? 100 : 3);
	}

	private void ContentText_OnLayoutChanged(object sender, RoutedEventArgs e)
	{
		UpdateExpandToggleVisibility();
	}

	private void ContentText_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		UpdateExpandToggleVisibility();
	}

	private void UpdateExpandToggleVisibility()
	{
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (ContentText != null && ExpandToggle != null)
			{
				ExpandToggle.Visibility = ((ContentText.LineCount <= 3) ? Visibility.Collapsed : Visibility.Visible);
			}
		}, DispatcherPriority.Loaded);
	}

	private void ContentText_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		ScrollViewer scrollViewer = Utils.FindParent<ScrollViewer>(this);
		if (scrollViewer != null)
		{
			e.Handled = true;
			scrollViewer.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
			{
				RoutedEvent = Mouse.MouseWheelEvent,
				Source = scrollViewer
			});
		}
	}

	private void OnEditClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is CheckInLogViewModel model)
		{
			EditHabitLogWindow editHabitLogWindow = new EditHabitLogWindow(model);
			editHabitLogWindow.Owner = App.Window;
			editHabitLogWindow.ShowDialog();
		}
	}

	private async void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is CheckInLogViewModel checkInLogViewModel)
		{
			bool? flag = new CustomerDialog(Utils.GetString("DeleteHabitRecord"), string.Format(Utils.GetString("DeleteHabitRecordHint"), checkInLogViewModel.DisplayDate), Utils.GetString("Delete"), Utils.GetString("Cancel"))
			{
				Owner = Window.GetWindow(this)
			}.ShowDialog();
			if (flag.HasValue && flag.Value)
			{
				checkInLogViewModel.IsHide = true;
				Utils.FindParent<HabitLogControl>(this)?.OnItemDeleted();
				await HabitService.DeleteCheckInRecord(checkInLogViewModel.Id);
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/checkinlogcontrol.xaml", UriKind.Relative);
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
			Root = (CheckInLogControl)target;
			break;
		case 2:
			((Border)target).MouseLeftButtonUp += OnEditClick;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnDeleteClick;
			break;
		case 4:
			ContentText = (TextBox)target;
			ContentText.Loaded += ContentText_OnLayoutChanged;
			ContentText.SizeChanged += ContentText_OnLayoutChanged;
			ContentText.TextChanged += ContentText_OnTextChanged;
			ContentText.PreviewMouseWheel += ContentText_OnPreviewMouseWheel;
			break;
		case 5:
			ExpandToggle = (StackPanel)target;
			ExpandToggle.MouseLeftButtonUp += OnExpandClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
