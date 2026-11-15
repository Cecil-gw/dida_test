using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using KotlinModels;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class UndoToast : UserControl, IComponentConnector
{
	private UndoController _controller;

	private bool _restored;

	internal Border Container;

	internal TextBlock DeletedOrCompleted;

	private bool _contentLoaded;

	public UndoToast()
	{
		InitializeComponent();
	}

	public UndoToast(UndoController controller)
	{
		InitializeComponent();
		_controller = controller;
		_restored = false;
		string content = _controller.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			DeletedOrCompleted.Text = content;
		}
	}

	public void InitEventUndo(string eventId, string eventTitle)
	{
		_controller = new EventUndo(eventId, eventTitle);
		_restored = false;
		string content = _controller.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			DeletedOrCompleted.Text = content;
		}
	}

	public void InitTaskUndo(string taskId, string taskTitle, bool isEmptyDelete = false, List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = null)
	{
		_controller = new TaskDeleteUndo(taskId, taskTitle, isEmptyDelete, deleteRecurrenceUndo);
		_restored = false;
		string content = _controller.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			DeletedOrCompleted.Text = content;
		}
	}

	public void InitBatchTaskUndo(IEnumerable<string> taskIds, IEnumerable<string> deleteForever = null, List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = null)
	{
		_controller = new BatchTaskDeleteUndo(taskIds.ToList(), deleteForever?.ToList(), deleteRecurrenceUndo);
		_restored = false;
		string content = _controller.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			DeletedOrCompleted.Text = content;
		}
	}

	public void InitSubtaskUndo(TaskDetailItemModel subtaskId)
	{
		_controller = new SubTaskUndo(subtaskId);
		_restored = false;
		string content = _controller.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			DeletedOrCompleted.Text = content;
		}
	}

	private async void OnUndoClick(object sender, MouseButtonEventArgs e)
	{
		OnUndo();
	}

	public virtual void OnFinished()
	{
		if (!_restored)
		{
			_controller.Finished();
			SyncManager.TryDelaySync();
		}
	}

	public virtual void OnUndo()
	{
		base.Visibility = Visibility.Collapsed;
		_restored = true;
		_controller.Undo();
	}

	public void InitTaskUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
		_controller = new DeleteRecurrenceUndo(undoModels);
		_restored = false;
		string content = _controller.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			DeletedOrCompleted.Text = content;
		}
	}

	public void SetVisible(bool show)
	{
		Container.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/undotoast.xaml", UriKind.Relative);
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
			Container = (Border)target;
			break;
		case 2:
			((StackPanel)target).MouseLeftButtonUp += OnUndoClick;
			break;
		case 3:
			DeletedOrCompleted = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
