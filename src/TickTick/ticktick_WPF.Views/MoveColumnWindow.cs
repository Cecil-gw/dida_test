using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public class MoveColumnWindow : Window, IOkCancelWindow, IComponentConnector
{
	internal MoveColumnWindow Root;

	internal RadioButton MoveUnCompleteRadio;

	internal RadioButton MoveAllRadio;

	private bool _contentLoaded;

	public bool? IsMoveAll { get; set; }

	public MoveColumnWindow(List<TaskBaseViewModel> tasks)
	{
		InitializeComponent();
		LoadData(tasks);
	}

	private async void LoadData(List<TaskBaseViewModel> tasks)
	{
		bool flag = tasks.Any((TaskBaseViewModel x) => x.IsNote);
		bool flag2 = tasks.Any((TaskBaseViewModel x) => x.IsTask);
		bool flag3 = tasks.Any((TaskBaseViewModel x) => x.IsTask && x.Status == 0);
		string arg = string.Empty;
		if (flag && flag2)
		{
			arg = ((!flag3) ? Utils.GetString("task_type_note") : Utils.GetString("TaskTypeUncompletedTasksNotes"));
		}
		else if (flag)
		{
			arg = Utils.GetString("task_type_note");
		}
		else if (flag2)
		{
			arg = Utils.GetString("task_type_uncompleted_task");
		}
		string arg2 = Utils.GetString("TaskTypeAllTasks");
		if (flag)
		{
			arg2 = Utils.GetString("TaskTypeAllTasksNotes");
		}
		MoveUnCompleteRadio.Content = string.Format(Utils.GetString("MoveSectionAnd"), arg);
		MoveAllRadio.Content = string.Format(Utils.GetString("MoveSectionAnd"), arg2);
		MoveUnCompleteRadio.IsChecked = true;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		DoMove();
		Close();
	}

	public void Ok()
	{
		DoMove();
		Close();
	}

	private void DoMove()
	{
		if (MoveUnCompleteRadio.IsChecked == true)
		{
			IsMoveAll = false;
		}
		else if (MoveAllRadio.IsChecked == true)
		{
			IsMoveAll = true;
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public void OnCancel()
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/movecolumnwindow.xaml", UriKind.Relative);
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
			Root = (MoveColumnWindow)target;
			break;
		case 2:
			MoveUnCompleteRadio = (RadioButton)target;
			break;
		case 3:
			MoveAllRadio = (RadioButton)target;
			break;
		case 4:
			((Button)target).Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
