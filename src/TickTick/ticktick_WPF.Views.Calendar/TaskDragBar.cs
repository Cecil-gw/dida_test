using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Calendar;

public class TaskDragBar : UserControl, IComponentConnector
{
	private bool _contentLoaded;

	public event EventHandler<MouseButtonEventArgs> TaskDrop;

	public event EventHandler<MouseEventArgs> TaskMove;

	public TaskDragBar()
	{
		InitializeComponent();
	}

	public TaskDragBar(CalendarDisplayViewModel model)
	{
		InitializeComponent();
		base.DataContext = model;
	}

	private void OnDrop(object sender, MouseButtonEventArgs e)
	{
		this.TaskDrop?.Invoke(this, e);
	}

	private void OnMove(object sender, MouseEventArgs e)
	{
		this.TaskMove?.Invoke(this, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/taskdragbar.xaml", UriKind.Relative);
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
			((TaskDragBar)target).MouseLeftButtonUp += OnDrop;
			((TaskDragBar)target).MouseMove += OnMove;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
