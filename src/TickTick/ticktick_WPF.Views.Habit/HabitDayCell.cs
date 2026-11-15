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
using System.Windows.Shapes;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Habit;

public class HabitDayCell : UserControl, IComponentConnector
{
	internal HabitDayCell Root;

	internal Grid DayCellGrid;

	internal CycleProgressBar Progress;

	private bool _contentLoaded;

	public HabitDayCell()
	{
		InitializeComponent();
		base.DataContextChanged += OnDataContextChanged;
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		string text = "";
		DateTime dateTime = default(DateTime);
		if (e.OldValue is HabitDayViewModel habitDayViewModel)
		{
			text = habitDayViewModel.HabitId;
			dateTime = habitDayViewModel.Date;
		}
		if (e.NewValue is HabitDayViewModel habitDayViewModel2)
		{
			if (habitDayViewModel2.HabitId != text || habitDayViewModel2.Date != dateTime)
			{
				Progress.BeginAnimation(CycleProgressBar.PercentProperty, null);
				Progress.Percent = habitDayViewModel2.Percent;
			}
			else
			{
				Progress.TargetPercent = habitDayViewModel2.Percent;
			}
		}
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is HabitDayViewModel habitDayViewModel)
		{
			habitDayViewModel.Hover = true;
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (base.DataContext != null && base.DataContext is HabitDayViewModel habitDayViewModel)
		{
			habitDayViewModel.Hover = false;
		}
	}

	private async void OnClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is HabitDayViewModel model) || model.Date > DateTime.Today)
		{
			return;
		}
		if (model.Date < DateTime.Today.AddDays(-90.0))
		{
			Utils.Toast(Utils.GetString("CheckInExpiredDateHint"));
			return;
		}
		HabitModel habitModel = await HabitDao.GetHabitById(model.HabitId);
		if (habitModel == null)
		{
			return;
		}
		model.Habit = habitModel;
		if (!habitModel.IsBoolHabit())
		{
			Utils.FindParent<HabitMonthGrid>(this)?.ManuallyRecord(model.Date, placeMouse: true);
			return;
		}
		HabitUtils.BuildHabitRepeatInfo(habitModel.RepeatRule, habitModel.ExDates);
		if (model.Completed || model.UnCompleted)
		{
			model.Completed = false;
			model.UnCompleted = false;
			await HabitService.ResetCheckInHabit(model.HabitId, model.Date, habitModel.Type);
		}
		else
		{
			model.Completed = true;
			await HabitService.CheckInHabit(model.HabitId, model.Date, -1.0, increment: true, checkShowRecord: true, 0, null, null, HabitAction.CheckIn);
		}
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext == null)
		{
			return;
		}
		object dataContext = base.DataContext;
		HabitDayViewModel model = dataContext as HabitDayViewModel;
		if (model == null)
		{
			return;
		}
		if (model.Date < DateTime.Today.AddDays(-90.0))
		{
			Utils.Toast(Utils.GetString("CheckInExpiredDateHint"));
		}
		else
		{
			if (model.Date > DateTime.Today)
			{
				return;
			}
			if (model.Completed || model.UnCompleted)
			{
				model.Hover = true;
				List<OperationItemViewModel> types = new List<OperationItemViewModel>
				{
					new OperationItemViewModel(ActionType.EditHabitLog),
					new OperationItemViewModel(ActionType.Reset)
				};
				OperationDialog operationDialog = new OperationDialog(model.HabitId, types);
				operationDialog.SetPlaceTarget(this);
				operationDialog.Operated += async delegate(object obj, KeyValuePair<string, ActionType> arg)
				{
					switch (arg.Value)
					{
					case ActionType.EditHabitLog:
						HabitUtils.ShowHabitRecordWindow(model.HabitId, model.Habit.Name, model.Date, isEdit: true, model.UnCompleted);
						break;
					case ActionType.Reset:
						await HabitService.ResetCheckInHabit(model.HabitId, model.Date, model.Habit.Type, this);
						break;
					}
					model.Hover = false;
				};
				operationDialog.Closed += delegate
				{
					model.Hover = false;
				};
				operationDialog.Show();
				return;
			}
			model.Hover = true;
			bool num = !model.Habit.ExDates.Contains(model.Date.ToString("yyyyMMdd")) && (!model.HasPercent || model.Percent == 0.0);
			List<OperationItemViewModel> list = new List<OperationItemViewModel>
			{
				new OperationItemViewModel(ActionType.EditHabitLog),
				new OperationItemViewModel(ActionType.UnComplete)
			};
			if (num)
			{
				list.Insert(1, new OperationItemViewModel(ActionType.Skip));
			}
			if (model.Completed || (model.HasPercent && model.Percent > 0.0))
			{
				list.Add(new OperationItemViewModel(ActionType.Reset));
			}
			OperationDialog operationDialog2 = new OperationDialog(model.HabitId, list);
			operationDialog2.SetPlaceTarget(this);
			operationDialog2.Operated += async delegate(object obj, KeyValuePair<string, ActionType> arg)
			{
				switch (arg.Value)
				{
				case ActionType.EditHabitLog:
					HabitUtils.ShowHabitRecordWindow(model.HabitId, model.Habit.Name, model.Date, isEdit: true, !model.Completed);
					break;
				case ActionType.Skip:
					await HabitService.SkipHabit(model.HabitId, model.Date);
					Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("Skipped"));
					break;
				case ActionType.UnComplete:
					UncompleteHabit(model);
					break;
				case ActionType.Reset:
					await HabitService.ResetCheckInHabit(model.HabitId, model.Date, model.Habit.Type, this);
					break;
				}
				model.Hover = false;
			};
			operationDialog2.Closed += delegate
			{
				model.Hover = false;
			};
			operationDialog2.Show();
		}
	}

	private async void UncompleteHabit(HabitDayViewModel model)
	{
		model.UnCompleted = true;
		await HabitService.UncompleteHabit(model.HabitId, model.Date);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/habitdaycell.xaml", UriKind.Relative);
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
			Root = (HabitDayCell)target;
			Root.MouseEnter += OnMouseEnter;
			Root.MouseLeave += OnMouseLeave;
			break;
		case 2:
			DayCellGrid = (Grid)target;
			break;
		case 3:
			Progress = (CycleProgressBar)target;
			break;
		case 4:
			((Ellipse)target).MouseLeftButtonUp += OnClick;
			((Ellipse)target).MouseRightButtonUp += OnRightClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
