using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Activity;

public class TaskActivityPanel : StackPanel, IComponentConnector
{
	internal TextBlock ActivitiesName;

	internal TextBlock ActivityCountText;

	internal Border ActivityGrid;

	internal TaskActivityControl ActivityControl;

	private bool _contentLoaded;

	public event EventHandler Closed;

	public TaskActivityPanel()
	{
		InitializeComponent();
	}

	private void OnCloseActivityClick(object sender, MouseButtonEventArgs e)
	{
		this.Closed?.Invoke(this, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/activity/taskactivitypanel.xaml", UriKind.Relative);
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
			ActivitiesName = (TextBlock)target;
			break;
		case 2:
			ActivityCountText = (TextBlock)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnCloseActivityClick;
			break;
		case 4:
			ActivityGrid = (Border)target;
			break;
		case 5:
			ActivityControl = (TaskActivityControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
