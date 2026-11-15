using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Activity;

public class TaskActivityControl : UserControl, IComponentConnector, IStyleConnector
{
	private ScrollViewer _scrollViewer;

	internal ListView Items;

	private bool _contentLoaded;

	public TaskActivityControl()
	{
		InitializeComponent();
	}

	private void OnMouseWheel(object sender, MouseWheelEventArgs e)
	{
		ScrollViewer scrollViewer = GetScrollViewer();
		if (scrollViewer != null)
		{
			scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
			e.Handled = true;
		}
	}

	private ScrollViewer GetScrollViewer()
	{
		if (_scrollViewer == null && Items.Template.FindName("ScrollViewer", Items) is ScrollViewer scrollViewer)
		{
			_scrollViewer = scrollViewer;
		}
		return _scrollViewer;
	}

	public void SetItems(List<TaskActivityViewModel> vms)
	{
		Items.ItemsSource = vms;
		GetScrollViewer()?.ScrollToTop();
	}

	private void Null_Handler(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/activity/taskactivitycontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			Items = (ListView)target;
			Items.PreviewMouseWheel += OnMouseWheel;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			EventSetter eventSetter = new EventSetter();
			eventSetter.Event = FrameworkElement.RequestBringIntoViewEvent;
			eventSetter.Handler = new RequestBringIntoViewEventHandler(Null_Handler);
			((Style)target).Setters.Add(eventSetter);
		}
	}
}
