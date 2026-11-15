using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Widget;

public class WidgetTaskItem : UserControl, IShowTaskDetailWindow, IComponentConnector
{
	public static readonly DependencyProperty ShowCountDownProperty = DependencyProperty.Register("ShowCountDown", typeof(bool), typeof(WidgetTaskItem), new PropertyMetadata(false));

	private bool _customDateSelected;

	private string _edittingTaskId = string.Empty;

	private ProjectWidget _parent;

	private bool _mouseDown;

	internal WidgetTaskItem RootView;

	internal Grid RootGrid;

	internal Grid ItemContainer;

	internal Path OpenIndicator;

	internal Popup ManuallyCheckInPopup;

	internal ManualRecordCheckinControl CheckInControl;

	internal Grid CheckBox;

	internal Grid CountdownIcon;

	internal Image CountDownImage;

	private bool _contentLoaded;

	public bool ShowCountDown
	{
		get
		{
			return (bool)GetValue(ShowCountDownProperty);
		}
		set
		{
			SetValue(ShowCountDownProperty, value);
		}
	}

	private DisplayItemModel Model => base.DataContext as DisplayItemModel;

	protected WidgetTaskItem(string themeId)
	{
		ThemeUtil.SetTheme(themeId, this);
		InitializeComponent();
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		model.SetIcon();
		model.SetPropertyChangedEvent();
		if (!model.ShowReminder.HasValue)
		{
			TaskItemLoadHelper.LoadShowReminder(model);
		}
		if (model.IsCountdown)
		{
			CountdownModel countdown = (model.SourceViewModel as CountDownBaseViewModel)?.Countdown;
			if (countdown != null)
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					CountDownImage.Source = CountdownService.GetIcon(countdown.IconRes, countdown.Color);
				}, DispatcherPriority.Render);
				CountDownImage.Visibility = Visibility.Visible;
			}
		}
		else
		{
			CountDownImage.Visibility = Visibility.Collapsed;
		}
	}

	private async void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		e.Handled = true;
		if ((GetParent()?.IsLocked ?? false) || model.IsToggling)
		{
			return;
		}
		if (!model.Enable)
		{
			if (string.IsNullOrEmpty(model.TeamId) || model.Permission == null)
			{
				return;
			}
			string permission = model.Permission;
			if (!(permission == "read"))
			{
				if (permission == "comment")
				{
					string arg = Utils.GetString("CanComment");
					GetParent()?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				}
			}
			else
			{
				string arg = Utils.GetString("ReadOnly");
				GetParent()?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
			}
			return;
		}
		model.IsToggling = true;
		bool soundPlayed = await TryPlayCompleteStory(model);
		if (model.IsTask)
		{
			model.SourceViewModel.Status = ((model.SourceViewModel.Status == 0) ? 2 : 0);
			await CompleteTask(model, soundPlayed);
		}
		else if (model.IsHabit)
		{
			ToggleHabitCompleted();
		}
		else if (model.IsItem)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
			if (!string.IsNullOrEmpty(taskModel.attendId) && !AgendaHelper.CanAccessAgenda(taskModel))
			{
				GetParent()?.TryToastString(null, Utils.GetString("OnlyOwnerCanCompleteSubtask"));
				return;
			}
			await CompleteCheckItem(model);
		}
		model.IsToggling = false;
	}

	public async Task<bool> TryPlayCompleteStory(DisplayItemModel model)
	{
		ProjectWidget parent = GetParent();
		if ((!model.IsTask && !model.IsItem) || parent == null)
		{
			return false;
		}
		bool flag = model.Status == 0;
		string text = default(string);
		if (flag)
		{
			text = await parent.GetCompleteStoryText(model);
			flag = text != null;
		}
		if (flag)
		{
			await PlayCompleteStory(text);
			return true;
		}
		return false;
	}

	private async Task PlayCompleteStory(string text)
	{
		bool flag = true;
		if (text == Utils.GetString("FirstTaskDone"))
		{
			flag = LocalSettings.Settings.ExtraSettings.CpltStoryTimes < 2;
			LocalSettings.Settings.ExtraSettings.CpltStoryTimes++;
		}
		int num = ((base.DataContext as DisplayItemModel)?.Level ?? 0) * 16;
		double num2 = ((double)FindResource("Height32") - 32.0) / 2.0;
		CompleteStory completeStoryView = new CompleteStory
		{
			HorizontalAlignment = HorizontalAlignment.Left
		};
		RootGrid.Children.Add(completeStoryView);
		completeStoryView.Margin = new Thickness(num + 10, num2 - 4.0, 0.0, 0.0);
		DoubleAnimation animation = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(300.0)),
			From = -40.0,
			To = 0.0
		};
		TextBlock textBlock = new TextBlock
		{
			Text = text,
			FontSize = 14.0
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		TranslateTransform translateTransform = (TranslateTransform)(textBlock.RenderTransform = new TranslateTransform
		{
			Y = -40.0
		});
		textBlock.HorizontalAlignment = HorizontalAlignment.Left;
		textBlock.Margin = new Thickness(num + 48, num2 + 7.0, 0.0, 0.0);
		RootGrid.Children.Add(textBlock);
		DoubleAnimation animation2 = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(300.0)),
			From = 0.0,
			To = Math.Max(0.0, base.ActualWidth - (double)num)
		};
		Border backBorder = new Border
		{
			Width = 0.0,
			Height = base.ActualHeight,
			Opacity = 0.2
		};
		backBorder.HorizontalAlignment = HorizontalAlignment.Left;
		backBorder.SetResourceReference(Control.BackgroundProperty, "TickYellow");
		backBorder.Margin = new Thickness(num, 0.0, 0.0, 0.0);
		RootGrid.Children.Add(backBorder);
		TranslateTransform translateTransform2 = new TranslateTransform();
		ItemContainer.RenderTransform = translateTransform2;
		DoubleAnimation animation3 = new DoubleAnimation
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(300.0)),
			From = 0.0,
			To = ItemContainer.ActualHeight
		};
		Utils.PlayCompletionSound();
		translateTransform2.BeginAnimation(TranslateTransform.YProperty, animation3);
		translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);
		backBorder.BeginAnimation(FrameworkElement.WidthProperty, animation2);
		completeStoryView.PlayStory();
		await Task.Delay(flag ? 900 : 500);
		RootGrid.Children.Remove(completeStoryView);
		RootGrid.Children.Remove(textBlock);
		RootGrid.Children.Remove(backBorder);
		ItemContainer.RenderTransform = null;
	}

	private void ToggleHabitCompleted()
	{
		if (Model.Status == 0)
		{
			CheckInHabit();
		}
		else
		{
			ResetHabit();
		}
	}

	private async void ResetHabit()
	{
		await HabitService.ResetCheckInHabit(Model.Id, DateTime.Today, Model.Habit.Type);
		GetParent()?.Reload();
	}

	private async void CheckInHabit()
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		HabitModel habitModel = await HabitDao.GetHabitById(model.Id);
		if (habitModel != null)
		{
			model.SetHabit(habitModel);
			if (habitModel.IsBooleanType())
			{
				await CheckInBooleanHabit();
				return;
			}
			if (Math.Abs(habitModel.Step + 1.0) <= 0.001)
			{
				ManuallyRecordCheckIn();
				return;
			}
			HabitAction action = ((!(Math.Abs(habitModel.Step) < 1E-05)) ? HabitAction.AutoRecord : HabitAction.CheckIn);
			await CheckInRealHabit(habitModel.Step, action);
		}
	}

	private void ManuallyRecordCheckIn()
	{
		ManuallyCheckInPopup.IsOpen = true;
		CheckInControl.Init(Model.Habit.Unit);
		CheckInControl.Cancel -= OnCheckInPopupCancel;
		CheckInControl.Cancel += OnCheckInPopupCancel;
		CheckInControl.Save -= OnCheckInPopupSave;
		CheckInControl.Save += OnCheckInPopupSave;
	}

	private async void OnCheckInPopupSave(object sender, double amount)
	{
		ManuallyCheckInPopup.IsOpen = false;
		DisplayItemModel model = Model;
		HabitModel habitModel = ((model != null) ? (await HabitDao.GetHabitById(model.Id)) : null);
		HabitModel habitModel2 = habitModel;
		if (habitModel2 != null)
		{
			HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habitModel2.Type, habitModel2.Step, habitModel2.Goal);
			if (habitCheckInType != HabitCheckInType.ManualUnCompleted && habitCheckInType != HabitCheckInType.ManualCompleted && habitCheckInType != HabitCheckInType.AutoUncompleted && habitCheckInType != HabitCheckInType.AutoCompleted)
			{
				GetParent()?.Reload();
				return;
			}
			model.SetHabit(habitModel2);
			await CheckInRealHabit(amount, HabitAction.ManualRecord);
		}
	}

	private void OnCheckInPopupCancel(object sender, EventArgs e)
	{
		ManuallyCheckInPopup.IsOpen = false;
	}

	private async Task CheckInBooleanHabit()
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			Utils.PlayCompletionSound();
			await Task.Delay(20);
			await HabitService.CheckInHabit(model.Id, DateTime.Today, -1.0, increment: true, checkShowRecord: true, 0, GetParent(), null, HabitAction.CheckIn);
			GetParent()?.Reload();
		}
	}

	private async Task CheckInRealHabit(double step, HabitAction action)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			model.SourceViewModel.Status = 2;
			Utils.PlayCompletionSound();
			if (model.HabitCheckIn == null)
			{
				HabitCheckInModel habitCheckIn = new HabitCheckInModel
				{
					Goal = model.Habit.Goal,
					Value = step
				};
				model.SetHabitCheckIn(habitCheckIn);
			}
			else
			{
				model.HabitCheckIn.Value = model.HabitCheckIn.GetDisplayValue() + step;
				model.SetHabitCheckIn(model.HabitCheckIn);
			}
			await HabitService.CheckInHabit(model.Id, DateTime.Today, step, increment: true, checkShowRecord: true, 0, GetParent(), null, action);
			if (model.HabitCheckIn.Value >= model.Habit.Goal)
			{
				GetParent()?.Reload();
			}
			else if (step > 0.0)
			{
				await Task.Delay(1500);
				GetParent()?.Reload();
			}
			else
			{
				GetParent()?.Reload();
			}
		}
	}

	private async Task CompleteCheckItem(DisplayItemModel item)
	{
		await TaskDetailItemService.CompleteCheckItem(item.Id, handleTask: true, completeOnly: false, needUndo: true, GetParent());
		await Task.Delay(120);
		GetParent()?.Reload();
	}

	private async Task CompleteTask(DisplayItemModel item, bool soundPlayed)
	{
		int status = item.Status;
		await Task.Delay(250);
		await TaskService.SetTaskStatus(item.Id, status, status != 0, GetParent(), inDetail: false, !soundPlayed);
		GetParent()?.Reload();
	}

	private ProjectWidget GetParent()
	{
		return _parent ?? (_parent = FindParent(this));
	}

	private static ProjectWidget FindParent(DependencyObject child)
	{
		if (child == null)
		{
			return null;
		}
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		if (parent is ProjectWidget result)
		{
			return result;
		}
		return FindParent(parent);
	}

	private void OnRightClick(object sender, MouseButtonEventArgs e)
	{
		if (Model != null && (Model.Enable || Model.IsEvent))
		{
			ProjectWidget parent = GetParent();
			if (parent == null || !parent.IsLocked)
			{
				SetEditting(editting: true);
				ShowOperationDialog();
			}
		}
	}

	private async void ShowOperationDialog()
	{
		object dataContext = base.DataContext;
		DisplayItemModel model = dataContext as DisplayItemModel;
		if (model == null || model.IsItem)
		{
			return;
		}
		model.Selected = true;
		SetEditting(editting: true);
		_edittingTaskId = model.TaskId;
		if (model.IsCourse || model.IsEvent)
		{
			OperationDialog obj = (model.IsCourse ? ((OperationDialog)new ArchiveOperationDialog(model.Id, ArchiveKind.Course)) : ((OperationDialog)new CalendarOperationDialog(new EventArchiveArgs(model.SourceViewModel))));
			obj.SetPlaceTarget(this);
			obj.Operated += delegate
			{
				GetParent()?.RemoveItem(model);
				if (model.IsCourse)
				{
					DataChangedNotifier.OnScheduleChanged();
				}
			};
			obj.Closed += delegate
			{
				model.Selected = false;
				SetEditting(editting: false);
			};
			obj.Show();
			return;
		}
		if (model.Type == DisplayType.Habit)
		{
			if (model.Status != 0)
			{
				return;
			}
			List<OperationItemViewModel> list = new List<OperationItemViewModel>
			{
				new OperationItemViewModel(ActionType.EditHabitLog),
				new OperationItemViewModel(ActionType.Skip),
				new OperationItemViewModel(ActionType.UnComplete)
			};
			if (model.HabitCheckIn != null && model.HabitCheckIn.Value > 0.0)
			{
				list.Add(new OperationItemViewModel(ActionType.Reset));
			}
			if (LocalSettings.Settings.EnableFocus)
			{
				list.Insert(0, new OperationItemViewModel(ActionType.StartFocus)
				{
					Selectable = false,
					SubActions = new List<OperationItemViewModel>
					{
						new OperationItemViewModel(ActionType.StartPomo)
						{
							Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Focus)
						},
						new OperationItemViewModel(ActionType.StartTiming)
						{
							Enable = (!TickFocusManager.Working || LocalSettings.Settings.PomoType == FocusConstance.Timing)
						}
					}
				});
			}
			OperationDialog operationDialog = new OperationDialog(model.Id, list);
			operationDialog.SetPlaceTarget(this);
			operationDialog.Operated += async delegate(object obj2, KeyValuePair<string, ActionType> kv)
			{
				switch (kv.Value)
				{
				case ActionType.StartPomo:
					TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: true);
					break;
				case ActionType.StartTiming:
					TickFocusManager.TryStartFocusHabit(kv.Key, isPomo: false);
					break;
				case ActionType.Skip:
					model.Selected = false;
					SetEditting(editting: false);
					await HabitService.SkipHabit(kv.Key);
					GetParent()?.RemoveItem(model);
					GetParent()?.TryToastString(null, Utils.GetString("Skipped"));
					break;
				case ActionType.EditHabitLog:
					HabitUtils.ShowHabitRecordWindow(model.Id, model.Habit.Name, DateTime.Today, isEdit: true, !model.IsCompleted);
					break;
				case ActionType.UnComplete:
					await HabitService.UncompleteHabit(model.Id, DateTime.Today);
					break;
				case ActionType.Reset:
					await HabitService.ResetCheckInHabit(model.Id, DateTime.Today, model.Habit.Type, this);
					break;
				}
			};
			operationDialog.Closed += OnOperationClosed;
			operationDialog.Show();
			return;
		}
		bool canShowAddSubtask = (GetParent()?.CanAddSubTask() ?? false) && model.Enable && (model.InDetail || model.Status == 0);
		OperationExtra operationExtra = await TaskOperationHelper.GetTaskAccessInfo(model.TaskId, canShowAddSubtask);
		if (operationExtra == null)
		{
			return;
		}
		if (model.IsTaskOrNote)
		{
			operationExtra.IsPinned = model.IsPinned;
		}
		TaskOperationDialog dialog = new TaskOperationDialog(operationExtra, this);
		WidgetWindow widgetWindow = GetParent()?.GetParentWindow();
		if (widgetWindow != null)
		{
			dialog.Resources = widgetWindow.Resources;
		}
		dialog.CreateSubTask += CreateSubTask;
		dialog.Copied += CopyTask;
		dialog.LinkCopied += CopyTaskLink;
		dialog.Deleted += DeleteTask;
		dialog.PrioritySelect += SetPriority;
		dialog.ProjectSelect += OnProjectSelect;
		dialog.AbandonOrReopen += OnAbandonOrReopen;
		dialog.AssigneeSelect += OnAssigneeSelect;
		dialog.Closed += OnOperationClosed;
		dialog.CustomDateSelect += OnCustomSelect;
		dialog.TagsSelect += OnTagsSelected;
		dialog.SkipCurrentRecurrence += OnSkipRecurrence;
		dialog.SwitchTaskOrNote += OnSwitchTaskOrNote;
		dialog.Toast += OnTaskOperationToast;
		dialog.Starred += OnStarClick;
		dialog.CompleteDateChanged += async delegate(object o, DateTime date)
		{
			await TaskService.ChangeCompleteDate(model.TaskId, date);
			SyncManager.TryDelaySync();
		};
		dialog.TimeClear += async delegate
		{
			dialog.Dismiss();
			await ClearDate();
		};
		dialog.TimeSelect += async delegate(object arg, TimeData data)
		{
			_customDateSelected = false;
			if (Model != null)
			{
				model.Selected = false;
			}
			SetEditting(editting: false);
			await TaskService.SetDate(_edittingTaskId, data);
			dialog.Dismiss();
		};
		dialog.QuickDateSelect += async delegate(object arg, DateTime date)
		{
			SetEditting(editting: false);
			await TaskService.SetStartDate(_edittingTaskId, date);
			dialog.Dismiss();
		};
		dialog.Show();
	}

	private void OnTaskOperationToast(object sender, string e)
	{
		GetParent()?.TryToastString(null, e);
	}

	private async void OnAbandonOrReopen(object sender, EventArgs e)
	{
		if (Model != null)
		{
			int num = ((!Model.IsAbandoned) ? (-1) : 0);
			await TaskService.SetTaskStatus(Model.TaskId, num, num != 0, GetParent());
		}
	}

	private async void OnStarClick(object sender, bool isPin)
	{
		if (Model != null)
		{
			string projectId = Model.CurrentProjectIdentity?.CatId ?? Model.ProjectId;
			await TaskService.TogglesStarred(Model.TaskId, projectId);
		}
	}

	private void CreateSubTask(object sender, EventArgs e)
	{
		if (base.DataContext is DisplayItemModel model)
		{
			GetParent()?.CreateSubtask(model);
		}
	}

	private async void OnSwitchTaskOrNote(object sender, EventArgs e)
	{
		await TaskService.SwitchTaskOrNote(Model.Id);
		SyncManager.TryDelaySync();
	}

	private async void CopyTaskLink(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_edittingTaskId);
		if (taskModel != null)
		{
			TaskUtils.CopyTaskLink(taskModel.id, taskModel.projectId, taskModel.title);
		}
	}

	private async Task ClearDate()
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(_edittingTaskId);
		if (taskModel != null)
		{
			if (string.IsNullOrEmpty(taskModel.attendId))
			{
				await TaskService.ClearDate(_edittingTaskId);
			}
			else if (await TaskOperationHelper.CheckIfTaskAllowClearDate(_edittingTaskId, this))
			{
				await TaskService.ClearAgendaDate(_edittingTaskId);
			}
		}
	}

	private async void OnSkipRecurrence(object sender, EventArgs e)
	{
		await TaskService.SkipCurrentRecurrence(_edittingTaskId, notify: true, GetParent());
		SyncManager.TryDelaySync();
	}

	private async void OnTagsSelected(object sender, TagSelectData tags)
	{
		await TaskService.SetTags(_edittingTaskId, tags.OmniSelectTags);
	}

	private void OnCustomSelect(object sender, EventArgs e)
	{
		_customDateSelected = true;
	}

	private void NotifyTaskChange()
	{
		_ = Model;
	}

	private void OnOperationClosed(object sender, EventArgs e)
	{
		SetEditting(editting: false);
		if (base.DataContext != null && base.DataContext is DisplayItemModel displayItemModel && !_customDateSelected)
		{
			displayItemModel.Selected = false;
		}
	}

	private async void OnAssigneeSelect(object sender, AvatarInfo assignee)
	{
		await TaskService.SetAssignee(_edittingTaskId, assignee.UserId);
	}

	private async void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string item = projectAndColumnId.Item1;
		string item2 = projectAndColumnId.Item2;
		ProjectModel project = CacheManager.GetProjectById(item);
		if (project != null)
		{
			await TaskService.TryMoveProject(new MoveProjectArgs(_edittingTaskId, project)
			{
				ColumnId = item2
			});
			GetParent()?.OnTaskProjectChanged(_edittingTaskId, project);
		}
	}

	private async void SetPriority(object sender, int priority)
	{
		await TaskService.SetPriority(_edittingTaskId, priority);
	}

	private async void DeleteTask(object sender, EventArgs e)
	{
		TaskModel task = await TaskDao.GetThinTaskById(_edittingTaskId);
		if (task == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(task.attendId))
		{
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(_edittingTaskId, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				FindParent(this)?.BatchDeleteTask(list);
			}
			else
			{
				FindParent(this)?.TaskDeleted(_edittingTaskId);
			}
		}
		else if (await TaskOperationHelper.CheckIfAllowDeleteAgenda(task, this))
		{
			await TaskService.DeleteAgenda(_edittingTaskId, task.projectId, task.attendId);
		}
	}

	private async void CopyTask(object sender, EventArgs e)
	{
		await TaskService.CopyTask(_edittingTaskId, this);
	}

	private void OnLeftClick(object sender, MouseButtonEventArgs e)
	{
		if (_mouseDown)
		{
			_mouseDown = false;
			e.Handled = true;
			ProjectWidget parent = GetParent();
			if (parent == null || !parent.IsLocked)
			{
				TryShowDisplayWindow(Model, byMouse: true);
			}
		}
	}

	public async void TryShowDisplayWindow(DisplayItemModel model, bool byMouse, bool focusTitle = false)
	{
		if (model != null && !model.IsCountdown && PopupStateManager.CanShowDetailPopup())
		{
			model.Selected = true;
			if (model.IsTaskOrNote || model.IsItem)
			{
				ShowTaskOrItemWindow(model, byMouse, focusTitle);
			}
			else if (model.IsEvent)
			{
				await ShowCalendarEventWindow(model);
			}
			else if (model.IsHabit)
			{
				await ShowHabitWindow(model);
			}
			else if (model.IsCourse)
			{
				await ShowCourseWindow(model);
			}
		}
	}

	private async Task ShowCourseWindow(DisplayItemModel model)
	{
		CourseDetailViewModel courseDetailViewModel = await CourseDetailViewModel.GetDetailViewModelById(model.Id);
		if (courseDetailViewModel != null)
		{
			model.Selected = true;
			CourseDetailWindow courseDetailWindow = new CourseDetailWindow(courseDetailViewModel);
			Window window = Window.GetWindow(this);
			if (window != null)
			{
				courseDetailWindow.Resources = window.Resources;
			}
			courseDetailWindow.Closed += async delegate
			{
				await Task.Delay(200);
				model.Selected = false;
				SetEditting(editting: false);
			};
			courseDetailWindow.Show(this, base.ActualWidth, base.ActualWidth > 300.0);
			SetEditting(editting: true);
		}
	}

	private async Task ShowHabitWindow(DisplayItemModel model)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(model.Id);
		if (habitModel != null)
		{
			model.Selected = true;
			HabitCheckInWindow habitCheckInWindow = new HabitCheckInWindow(habitModel, model.Status, model.StartDate, GetParent());
			Window window = Window.GetWindow(this);
			if (window != null)
			{
				habitCheckInWindow.Resources = window.Resources;
			}
			habitCheckInWindow.Show(this, base.ActualWidth, 0.0, base.ActualWidth >= 350.0);
			habitCheckInWindow.OnAction -= OnCheckInAction;
			habitCheckInWindow.OnAction += OnCheckInAction;
		}
	}

	private async void OnCheckInAction(object sender, EventArgs extra)
	{
		SetEditting(editting: false);
		if (base.DataContext is DisplayItemModel { Selected: not false } displayItemModel)
		{
			displayItemModel.Selected = false;
		}
	}

	private async Task ShowCalendarEventWindow(DisplayItemModel model)
	{
		string eventId = model.Id;
		if (string.IsNullOrEmpty(eventId))
		{
			return;
		}
		SetEditting(editting: true);
		if (eventId.Contains("_time_"))
		{
			eventId = eventId.Remove(eventId.IndexOf("_time_", StringComparison.Ordinal));
		}
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(eventId);
		if (calendarEventModel == null)
		{
			calendarEventModel = await CalendarEventDao.GetEventByEventId(eventId);
		}
		CalendarEventModel calendarEventModel2 = calendarEventModel;
		if (calendarEventModel2 == null)
		{
			model.Selected = false;
			SetEditting(editting: false);
			return;
		}
		ProjectWidget projectWidget = Utils.FindParent<ProjectWidget>(this);
		if (projectWidget != null && projectWidget.TaskList.ItemsSource is ObservableCollection<DisplayItemModel> source && source.Any())
		{
			DisplayItemModel displayItemModel = source.FirstOrDefault((DisplayItemModel item) => item.Id == model.Id);
			if (displayItemModel != null)
			{
				calendarEventModel2.DueStart = displayItemModel.StartDate;
				calendarEventModel2.DueEnd = displayItemModel.DueDate;
			}
		}
		CalendarDetailWindow calendarDetailWindow = new CalendarDetailWindow();
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			calendarDetailWindow.Resources = window.Resources;
		}
		calendarDetailWindow.Disappear += OnDetailClosed;
		calendarDetailWindow.EventArchiveChanged += OnEventArchiveChanged;
		calendarDetailWindow.Show(this, base.ActualWidth, 0.0, base.ActualWidth > 350.0, new CalendarDetailViewModel(calendarEventModel2));
	}

	private void OnEventArchiveChanged(object sender, bool e)
	{
		GetParent()?.Reload(refreshQuickAddDate: false, force: true);
	}

	private void ShowTaskOrItemWindow(DisplayItemModel model, bool byMouse, bool focusTitle)
	{
		SetEditting(editting: true);
		TaskDetailPopup taskDetailPopup = new TaskDetailPopup();
		ProjectWidget parent = GetParent();
		if (parent != null)
		{
			if (PopupStateManager.LastTarget == this)
			{
				model.Selected = false;
				SetEditting(editting: false);
				return;
			}
			taskDetailPopup.DependentWindow = parent;
		}
		taskDetailPopup.Disappear -= OnDetailClosed;
		taskDetailPopup.Disappear += OnDetailClosed;
		taskDetailPopup.Show(model.TaskId, model.IsItem ? model.Id : string.Empty, new TaskWindowDisplayArgs(this, (!byMouse) ? 256 : 0, PopupLocationCalculator.GetMousePoint(!byMouse), focusTitle, 0.0, 0, useInWidget: true));
	}

	private void SetEditting(bool editting)
	{
		ProjectWidget parent = GetParent();
		if (parent != null)
		{
			parent.IsEditting = editting;
		}
	}

	private void OnDetailClosed(object sender, string e)
	{
		SetEditting(editting: false);
		if (base.DataContext is DisplayItemModel { Selected: not false } displayItemModel)
		{
			displayItemModel.Selected = false;
		}
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Disappear -= OnDetailClosed;
		}
	}

	private void OnOpenPathClick(object sender, MouseButtonEventArgs e)
	{
		ProjectWidget parent = GetParent();
		if ((parent == null || !parent.IsLocked) && base.DataContext is DisplayItemModel item)
		{
			GetParent()?.OnItemOpenClick(item);
			e.Handled = true;
		}
	}

	private void OnCheckRightMouseUp(object sender, MouseButtonEventArgs e)
	{
		ProjectWidget parent = GetParent();
		if ((parent == null || !parent.IsLocked) && base.DataContext is DisplayItemModel model)
		{
			GetParent()?.PopupCloseTask(model, this);
			e.Handled = true;
		}
	}

	public void OnDetailKeyUp(Key key)
	{
	}

	private void OnLeftMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/widgettaskitem.xaml", UriKind.Relative);
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
			RootView = (WidgetTaskItem)target;
			RootView.MouseLeftButtonUp += OnLeftClick;
			RootView.MouseLeftButtonDown += OnLeftMouseDown;
			RootView.MouseRightButtonUp += OnRightClick;
			break;
		case 2:
			RootGrid = (Grid)target;
			break;
		case 3:
			ItemContainer = (Grid)target;
			break;
		case 4:
			((Grid)target).PreviewMouseLeftButtonUp += OnOpenPathClick;
			break;
		case 5:
			OpenIndicator = (Path)target;
			break;
		case 6:
			ManuallyCheckInPopup = (Popup)target;
			break;
		case 7:
			CheckInControl = (ManualRecordCheckinControl)target;
			break;
		case 8:
			CheckBox = (Grid)target;
			CheckBox.MouseLeftButtonUp += OnCheckBoxClick;
			CheckBox.MouseRightButtonUp += OnCheckRightMouseUp;
			break;
		case 9:
			CountdownIcon = (Grid)target;
			break;
		case 10:
			CountDownImage = (Image)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
