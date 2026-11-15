using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class MiniCalendar : UserControl, IComponentConnector
{
	private readonly MiniCalendarViewModel _viewModel;

	private bool _isDragOver;

	internal Grid Root;

	internal Grid DayPickerGrid;

	internal TickDatePicker DayPicker;

	private bool _contentLoaded;

	public MiniCalendar()
	{
		InitializeComponent();
		_viewModel = new MiniCalendarViewModel();
		base.DataContext = _viewModel;
		DayPicker.UseInSlideMenu = true;
		DayPicker.AllowSelectMonth = true;
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		TimeChangeNotifier.DayChanged += OnDayChanged;
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		DayPicker.SetDayCells(reloadIndicator: true, reset: true);
	}

	private void OnDaySelected(object sender, DateTime date)
	{
		Utils.FindParent<ProjectListView>(this)?.NavigateProject("date", date.ToString("yyyyMMdd", CultureInfo.InvariantCulture), scrollToItem: false);
		_viewModel.MiniMode = DayPicker.MiniMode;
		DayPicker.SelectedDate = date;
	}

	private void OnCollapsedClick(object sender, MouseButtonEventArgs e)
	{
		DayPicker.MiniMode = !DayPicker.MiniMode;
		_viewModel.MiniMode = DayPicker.MiniMode;
	}

	public void LoadIndicator()
	{
		DayPicker.LoadTaskIndicator();
	}

	public void Reload()
	{
		DayPicker.MiniMode = _viewModel.MiniMode;
	}

	public async void OnDrop(MouseEventArgs e)
	{
		if (!_isDragOver)
		{
			return;
		}
		_isDragOver = false;
		object data = TickDragHelper.GetDragData();
		object obj = Utils.GetMousePointItem<DayCellControl>(e.GetPosition(DayPicker), DayPicker)?.DataContext;
		if (obj is DayViewModel dayViewModel)
		{
			if (data is DisplayItemModel model)
			{
				if (model.IsTaskOrNote)
				{
					List<string> batchSelectedIds = model.BatchSelectedIds;
					if (batchSelectedIds == null || batchSelectedIds.Count <= 0)
					{
						await TaskService.SetDate(model.Id, dayViewModel.Date, handleReminder: false);
					}
					else
					{
						await TaskService.BatchSetStartDate(model.BatchSelectedIds, dayViewModel.Date);
					}
				}
				if (model.IsItem)
				{
					TimeDataModel newDate = new TimeDataModel
					{
						TaskId = model.GetTaskId(),
						ItemId = model.Id,
						IsAllDay = (Utils.IsEmptyDate(model.StartDate) || (model.IsAllDay ?? true)),
						StartDate = ((!model.StartDate.HasValue) ? dayViewModel.Date : DateUtils.SetDateOnly(model.StartDate.Value, dayViewModel.Date)),
						DueDate = null
					};
					await TaskService.SetCheckItemDate(model.GetTaskId(), model.Id, newDate);
				}
				LoadIndicator();
			}
			if (data is CheckItemViewModel checkItemViewModel)
			{
				TimeDataModel newDate2 = new TimeDataModel
				{
					TaskId = checkItemViewModel.TaskServerId,
					ItemId = checkItemViewModel.Id,
					IsAllDay = (Utils.IsEmptyDate(checkItemViewModel.StartDate) || (checkItemViewModel.IsAllDay ?? true)),
					StartDate = ((!checkItemViewModel.StartDate.HasValue) ? dayViewModel.Date : DateUtils.SetDateOnly(checkItemViewModel.StartDate.Value, dayViewModel.Date)),
					DueDate = null
				};
				await TaskService.SetCheckItemDate(checkItemViewModel.TaskServerId, checkItemViewModel.Id, newDate2);
				LoadIndicator();
			}
		}
		DayPicker.SetCellHover(null);
	}

	public void OnDragOver(bool isOver, MouseEventArgs e)
	{
		if (isOver)
		{
			_isDragOver = true;
			object dragData = TickDragHelper.GetDragData();
			if ((dragData is DisplayItemModel displayItemModel && (displayItemModel.IsTaskOrNote || displayItemModel.IsItem) && displayItemModel.Enable) || dragData is CheckItemViewModel)
			{
				if (Utils.GetMousePointItem<DayCellControl>(e.GetPosition(DayPicker), DayPicker)?.DataContext is DayViewModel cellHover)
				{
					DayPicker.SetCellHover(cellHover);
				}
				TickDragHelper.SetDragOpacity(0.4);
			}
		}
		else if (_isDragOver)
		{
			_isDragOver = false;
			DayPicker.SetCellHover(null);
			TickDragHelper.SetDragOpacity(1.0);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/mainlistview/projectlist/minicalendar.xaml", UriKind.Relative);
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
			Root = (Grid)target;
			break;
		case 2:
			((Border)target).MouseLeftButtonUp += OnCollapsedClick;
			break;
		case 3:
			DayPickerGrid = (Grid)target;
			break;
		case 4:
			DayPicker = (TickDatePicker)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
