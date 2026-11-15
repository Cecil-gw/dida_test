using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Activity;

public class ProjectActivityItem : UserControl, IComponentConnector
{
	internal Run TaskTitle;

	internal EscPopup UnknownPopup;

	private bool _contentLoaded;

	public ProjectActivityItem()
	{
		InitializeComponent();
	}

	private void OnTaskClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is ProjectActivityViewModel projectActivityViewModel)
		{
			if (projectActivityViewModel.TaskTitle == Utils.GetString("Unknown"))
			{
				Mouse.Capture(null);
				UnknownPopup.IsOpen = true;
			}
			else
			{
				App.ShowMainWindow(projectActivityViewModel.TaskId);
				CloseProjectActivityWindow();
			}
		}
	}

	private void CloseProjectActivityWindow()
	{
		Utils.FindParent<ProjectActivityWindow>(this)?.Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/activity/projectactivityitem.xaml", UriKind.Relative);
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
			TaskTitle = (Run)target;
			TaskTitle.MouseLeftButtonUp += OnTaskClick;
			break;
		case 2:
			UnknownPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
