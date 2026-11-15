using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Timeline;

public class TimelineCellArrange : TimelineCellBase, IComponentConnector
{
	private Point? _point;

	internal Grid InfoTile;

	private bool _contentLoaded;

	public TimelineCellArrange()
	{
		InitializeComponent();
		base.MouseRightButtonUp += OnMouseRightUp;
	}

	private void OnCellMouseLeftDown(object sender, MouseButtonEventArgs e)
	{
		_point = e.GetPosition(this);
	}

	private void OnMouseRightUp(object sender, MouseButtonEventArgs e)
	{
		TimelineCellViewModel timelineCellViewModel = cellModel;
		if (timelineCellViewModel != null)
		{
			if (timelineCellViewModel.DisplayModel.IsTaskOrNote && !timelineCellViewModel.Editable)
			{
				base.Container?.TryToastString(Utils.GetString("NoEditingPermission"));
			}
			else if (timelineCellViewModel.DisplayModel.Type == DisplayType.CheckItem)
			{
				base.Container?.TryToastString(Utils.GetString("OperationNotSupport"));
			}
			else
			{
				base.Container?.ShowMenuPopup(timelineCellViewModel);
			}
		}
	}

	private async void OnCellMouseMove(object sender, MouseEventArgs e)
	{
		if (TickDragHelper.Dragging)
		{
			return;
		}
		Point? point = _point;
		if (!point.HasValue)
		{
			return;
		}
		Point position = e.GetPosition(this);
		if (!(Math.Abs(position.X - _point.Value.X) < 4.0) || !(Math.Abs(position.Y - _point.Value.Y) < 4.0))
		{
			_point = null;
			if ((!(base.DataContext is TimelineViewModel timelineViewModel) || (timelineViewModel.ProjectEnable && timelineViewModel.TimelineSortOption.groupByType != Constants.SortType.tag)) && sender is FrameworkElement { DataContext: var dataContext } && dataContext is TimelineCellViewModel model && model.DisplayModel.Editable)
			{
				model.Operation = TimelineCellOperation.Full;
				model.StartDragLine = model.Line;
				model.DragStatus = 1;
				model.DragFromArrange = true;
				await TickDragHelper.StartDragItem(this, model, new List<string> { model.Id }, base.ActualWidth, position);
				model.DragFromArrange = false;
				model.Operation = model.Operation.RemovePos() & model.Operation.Remove(TimelineCellOperation.Hover);
				model.StartDragLine = null;
				model.DragStatus = 0;
				base.timelineModel.UpdateTimelineSortType();
			}
		}
	}

	private void OnCellMouseLeftUp(object sender, MouseButtonEventArgs e)
	{
		Point? point = _point;
		if (point.HasValue)
		{
			Point valueOrDefault = point.GetValueOrDefault();
			if (valueOrDefault == e.GetPosition(this) && sender is FrameworkElement)
			{
				OpenDetailWindow(inArrange: true);
			}
		}
		_point = null;
	}

	private void OnFoldClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TimelineCellViewModel timelineCellViewModel)
		{
			_point = null;
			timelineCellViewModel.ToggleArrangeItemOpen();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinecellarrange.xaml", UriKind.Relative);
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
			InfoTile = (Grid)target;
			InfoTile.MouseLeftButtonDown += OnCellMouseLeftDown;
			InfoTile.MouseMove += OnCellMouseMove;
			InfoTile.MouseLeftButtonUp += OnCellMouseLeftUp;
			break;
		case 2:
			((Border)target).PreviewMouseLeftButtonUp += OnFoldClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
