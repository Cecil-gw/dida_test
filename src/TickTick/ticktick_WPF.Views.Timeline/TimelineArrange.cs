using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Timeline;

public class TimelineArrange : UserControl, IComponentConnector, IStyleConnector
{
	private TimelineContainer _container;

	private TimelineCellViewModel _cellModel;

	internal TimelineArrange Root;

	internal StackPanel DateFilterPanel;

	internal Grid EmptyGrid;

	private bool _contentLoaded;

	public TimelineArrange()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		EmptyView emptyView = new EmptyView(132.0);
		EmptyGrid.Children.Add(emptyView);
		emptyView.SetIconsSource(new EmptyIconsSourceModel("CalendarArrangeEmpty"));
		base.SizeChanged += delegate(object sender, SizeChangedEventArgs e)
		{
			EmptyGrid.Margin = new Thickness(0.0, e.NewSize.Height * 0.28, 0.0, 0.0);
		};
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_container = Utils.FindParent<TimelineContainer>(this);
	}

	private void OnOpenSetMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TimelineViewModel model)
		{
			new TimelineArrangeFilterPopup(model, DateFilterPanel).Show();
		}
	}

	private void OnDateSelected(object sender, object e)
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.IsOverDue = e as string == "overdue";
		}
	}

	private async void OnColumnOpenCloseMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: TimelineGroupViewModel dataContext })
		{
			dataContext.UpdateIsArrangeOpen(!dataContext.IsArrangeOpen);
		}
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TimelineViewModel timelineViewModel)
		{
			timelineViewModel.IsArranging = false;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinearrange.xaml", UriKind.Relative);
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
			Root = (TimelineArrange)target;
			break;
		case 3:
			DateFilterPanel = (StackPanel)target;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += OnOpenSetMouseUp;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += OnCloseClick;
			break;
		case 6:
			EmptyGrid = (Grid)target;
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
		if (connectionId == 2)
		{
			((Border)target).MouseLeftButtonUp += OnColumnOpenCloseMouseUp;
		}
	}
}
