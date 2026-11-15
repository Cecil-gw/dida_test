using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomPopup;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Views.Calendar.Agenda;

public class AgendaCheckIcon : Grid
{
	private readonly Line _topLine;

	private readonly Line _botLine;

	private readonly Border _border;

	private bool _checkable;

	public AgendaCheckIcon()
	{
		base.Width = 24.0;
		base.VerticalAlignment = VerticalAlignment.Stretch;
		_topLine = new Line
		{
			X1 = 0.0,
			X2 = 0.0,
			Y1 = 0.0,
			Y2 = 1.0,
			Height = 18.0,
			Stretch = Stretch.Uniform,
			StrokeThickness = 1.0,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_topLine.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity5");
		base.Children.Add(_topLine);
		_topLine.SetBinding(UIElement.VisibilityProperty, "TopLineVisible");
		_border = new Border
		{
			Width = 24.0,
			Height = 24.0,
			CornerRadius = new CornerRadius(12.0),
			Margin = new Thickness(0.0, 18.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top,
			Background = Brushes.Transparent
		};
		_border.MouseLeftButtonUp += OnMouseLeftButtonUp;
		_border.MouseRightButtonUp += OnMouseRightButtonUp;
		_border.MouseEnter += OnBorderMouseEnter;
		_border.MouseLeave += OnBorderMouseLeave;
		base.Children.Add(_border);
		Path path = new Path
		{
			Width = 16.0,
			Height = 16.0,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 22.0, 0.0, 0.0),
			Stretch = Stretch.Uniform,
			IsHitTestVisible = false
		};
		path.SetBinding(Path.DataProperty, "Icon");
		path.SetBinding(Shape.FillProperty, "IconColor");
		base.Children.Add(path);
		Image image = new Image
		{
			Width = 16.0,
			Height = 16.0,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 22.0, 0.0, 0.0),
			Stretch = Stretch.Uniform,
			IsHitTestVisible = false
		};
		image.SetBinding(Image.SourceProperty, "IconImage");
		base.Children.Add(image);
		_botLine = new Line
		{
			X1 = 0.0,
			X2 = 0.0,
			Y1 = 0.0,
			Y2 = 1.0,
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Stretch,
			StrokeThickness = 1.0,
			Margin = new Thickness(0.0, 41.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_botLine.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity5");
		base.Children.Add(_botLine);
		_botLine.SetBinding(UIElement.VisibilityProperty, "BotLineVisible");
	}

	private async void OnBorderMouseLeave(object sender, MouseEventArgs e)
	{
		_border.Background = Brushes.Transparent;
	}

	private async void OnBorderMouseEnter(object sender, MouseEventArgs e)
	{
		if (_checkable)
		{
			await Task.Delay(60);
			if (_border.IsMouseOver)
			{
				_border.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity3");
			}
		}
	}

	private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs args)
	{
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		if (calendarControl != null && calendarControl.IsLocked)
		{
			return;
		}
		object dataContext = base.DataContext;
		CalendarAgendaItemViewModel model = dataContext as CalendarAgendaItemViewModel;
		if (model == null || (!model.SourceViewModel.IsTask && !model.SourceViewModel.IsAgenda))
		{
			return;
		}
		if (!model.Checkable)
		{
			string permission = model.SourceViewModel.Permission;
			if (!(permission == "comment"))
			{
				if (permission == "read")
				{
					Utils.Toast(string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("ReadOnly")));
				}
			}
			else
			{
				Utils.Toast(string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("CanComment")));
			}
			return;
		}
		SetClosedStatusPopup instance = SetClosedStatusPopup.GetInstance(sender as FrameworkElement, model.Status == 2, model.Status == -1, -2.0, -5.0, (model.Status == 0) ? new int?(model.SourceViewModel.Progress) : ((int?)null));
		instance.Abandoned += delegate
		{
			UtilLog.Info("TaskList.AbandonTask " + model.Id + " from:CheckIconRightClick");
			TaskService.SetTaskStatus(model.Id, -1, needUndo: false, Utils.GetToastWindow());
		};
		instance.Completed += async delegate
		{
			UtilLog.Info("TaskList.CompleteTask " + model.Id + " from:CheckIconRightClick");
			TaskService.SetTaskStatus(model.Id, 2, needUndo: false, Utils.GetToastWindow());
		};
		instance.ProgressChangeRequested += async delegate(int progress)
		{
			string taskId = model.SourceViewModel.GetTaskId();
			if (model.SourceViewModel.IsTask && model.Status == 0 && model.StartDate.HasValue && model.DisplayStartDate.HasValue && model.StartDate.Value.Date != model.DisplayStartDate.Value.Date)
			{
				IToastShowWindow toastWindow = Utils.FindParent<IToastShowWindow>(this);
				if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(taskId, model.DisplayStartDate, toastWindow)))
				{
					return false;
				}
			}
			TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
			if (taskModel == null)
			{
				return false;
			}
			await TaskService.SyncProgress(taskModel, progress);
			return true;
		};
		instance.Closed += delegate
		{
			Utils.FindParent<CalendarControl>(this)?.SetEditting(editting: false);
		};
		Utils.FindParent<CalendarControl>(this)?.SetEditting(editting: true);
		instance.Show();
	}

	public void SetCheckable(bool checkable)
	{
		_checkable = checkable;
		_border.Cursor = (checkable ? Cursors.Hand : Cursors.Arrow);
		if (_border.IsMouseOver)
		{
			_border.SetResourceReference(Panel.BackgroundProperty, "BaseColorOpacity3");
		}
	}

	private async void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (Utils.FindParent<CalendarControl>(this)?.IsLocked ?? false)
		{
			return;
		}
		object dataContext = base.DataContext;
		if (!(dataContext is CalendarAgendaItemViewModel model))
		{
			return;
		}
		if (!model.Checkable)
		{
			string permission = model.SourceViewModel.Permission;
			if (!(permission == "comment"))
			{
				if (permission == "read")
				{
					Utils.Toast(string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("ReadOnly")));
				}
			}
			else
			{
				Utils.Toast(string.Format(Utils.GetString("NoPermissionToEdit"), Utils.GetString("CanComment")));
			}
			return;
		}
		if (model.SourceViewModel.IsTask || model.SourceViewModel.IsAgenda)
		{
			IToastShowWindow toastWindow = Utils.FindParent<IToastShowWindow>(this);
			if (model.DisplayStartDate.HasValue && model.StartDate != model.DisplayStartDate && model.Status == 0 && !(await ModifyRepeatHandler.CompleteOrSkipRecurrence(model.Id, model.DisplayStartDate, toastWindow)))
			{
				return;
			}
			int closeStatus = ((model.Status == 0) ? 2 : 0);
			await TaskService.SetTaskStatus(model.Id, closeStatus);
		}
		if (model.SourceViewModel.IsCheckItem)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.SourceViewModel.GetTaskId());
			if (!string.IsNullOrEmpty(taskModel.attendId) && !AgendaHelper.CanAccessAgenda(taskModel))
			{
				UtilLog.Info("ListItem.CompleteItem id " + model.Id + " not owner");
				Utils.Toast(Utils.GetString("OnlyOwnerCanCompleteSubtask"));
				return;
			}
			UtilLog.Info($"AgendaItem.CompleteItem checkItemid {model.Id},currentStatus{model.Status}");
			if (model.Status != 1)
			{
				Utils.PlayCompletionSound();
			}
			await TaskDetailItemService.CompleteCheckItem(model.Id);
		}
		if (!model.SourceViewModel.IsHabit)
		{
			return;
		}
		if (model.Status == 0)
		{
			try
			{
				await CheckInHabit(model);
				return;
			}
			catch (Exception)
			{
				return;
			}
		}
		await ResetHabit(model);
	}

	private async Task ResetHabit(CalendarAgendaItemViewModel model)
	{
		if (model.SourceViewModel is HabitBaseViewModel habitBaseViewModel)
		{
			await HabitService.ResetCheckInHabit(model.Id, model.Date, habitBaseViewModel.Habit.Type);
		}
	}

	private async Task CheckInHabit(CalendarAgendaItemViewModel model)
	{
		HabitModel habit = await HabitDao.GetHabitById(model.Id);
		if (habit == null)
		{
			return;
		}
		if (habit.IsBoolHabit())
		{
			await CheckInBooleanHabit(model);
		}
		else if (Math.Abs(habit.Step + 1.0) <= 0.001)
		{
			Utils.FindParent<CalendarAgendaView>(this)?.ManuallyRecordCheckIn(this, habit, delegate(object s, double e)
			{
				CheckInRealHabit(habit, model, e);
			});
		}
		else
		{
			await CheckInRealHabit(habit, model);
		}
	}

	private async Task CheckInRealHabit(HabitModel habit, CalendarAgendaItemViewModel model, double? step = null)
	{
		if (habit != null)
		{
			double actualStep = step ?? ((Math.Abs(habit.Step) < 1E-05) ? habit.Goal : habit.Step);
			HabitAction action = (step.HasValue ? HabitAction.ManualRecord : ((!(Math.Abs(habit.Step) < 1E-05)) ? HabitAction.AutoRecord : HabitAction.CheckIn));
			HabitCheckInModel habitCheckInModel = await HabitService.CheckInHabit(model.Id, DateTime.Today, actualStep, increment: true, checkShowRecord: true, 0, null, null, action);
			if (habitCheckInModel == null)
			{
				Utils.FindParent<CalendarAgendaItem>(this)?.RefreshHabitItem();
				return;
			}
			Utils.PlayCompletionSound();
			model.HabitCheckIn = habitCheckInModel;
			Utils.FindParent<CalendarAgendaView>(this)?.ShowRecordPopup(this, actualStep, habit.Unit);
			Utils.FindParent<CalendarAgendaItem>(this)?.RefreshHabitItem();
		}
	}

	private async Task CheckInBooleanHabit(CalendarAgendaItemViewModel model)
	{
		if (model != null)
		{
			model.SourceViewModel.Status = 2;
			model.SourceViewModel.CompletedTime = DateTime.Now;
			Utils.PlayCompletionSound();
			await Task.Delay(16);
			await HabitService.CheckInHabit(model.Id, DateTime.Today, -1.0, increment: true, checkShowRecord: true, 0, null, null, HabitAction.CheckIn);
		}
	}
}
