using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomPopup;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Eisenhower;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList.Item;
using ticktick_WPF.Views.Time;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.TaskList;

public class DisplayItemController
{
	protected UIElement Element;

	protected DisplayItemModel Model;

	private IToastShowWindow _parentWindow;

	private bool _canEdit = true;

	private bool _isInAnimation;

	private ITaskList _iTaskList;

	private static double _previewsX;

	private static FrameworkElement _previousWindow;

	public event EventHandler<string> SplashCommand;

	public DisplayItemController(UIElement element, DisplayItemModel model)
	{
		Model = model;
		Element = element;
	}

	protected ITaskList GetTaskList()
	{
		if (_iTaskList == null)
		{
			_iTaskList = Utils.FindParent<ITaskList>(Element);
		}
		return _iTaskList;
	}

	public void TitleGotFocus()
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			GetTaskList()?.TrySetTitleReadonly(model.Id);
		}
	}

	public async void SelectItem(bool focusTitle = false, bool ignoreBatch = false)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			ignoreBatch = ignoreBatch || !model.CanBatchSelect;
			if (model.IsTaskOrNote)
			{
				GetTaskList()?.SelectTask(model.TaskId, TaskSelectType.Click, ignoreBatch);
			}
			else if (model.IsItem)
			{
				GetTaskList()?.SelectSubtask(new IdExtra(model.Id, model.TaskId), TaskSelectType.Click);
			}
			else
			{
				GetTaskList()?.SelectItem(model.Id, model.Type);
			}
		}
	}

	public async Task<TaskDetailPopup> ShowDetailWindow(DisplayItemModel model, bool focusTitle, bool fromUpDown = false)
	{
		if (model.IsTaskOrNote || model.IsItem)
		{
			GetTaskListView()?.SetSelected(new List<string> { model.Id });
			return await ShowTaskDetail(focusTitle, fromUpDown);
		}
		if (model.IsEvent)
		{
			ShowCalDetail(model, fromUpDown);
		}
		if (model.IsCourse)
		{
			ShowCourseDetail(model, fromUpDown);
		}
		if (model.IsHabit)
		{
			ShowHabitDetail(model, fromUpDown);
		}
		return null;
	}

	private async void ShowHabitDetail(DisplayItemModel model, bool fromUpDown)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(model.Habit?.Id);
		if (habitModel == null)
		{
			return;
		}
		Point point = default(Point);
		if (fromUpDown && _previousWindow != null)
		{
			ClosePreviousWindow();
			point = new Point(_previewsX, (Element as FrameworkElement)?.ActualHeight ?? 40.0);
		}
		HabitCheckInWindow window = new HabitCheckInWindow(habitModel, model.Status, model.StartDate, Utils.GetToastWindow());
		if (!fromUpDown)
		{
			_previewsX = Mouse.GetPosition(Element).X;
		}
		_previousWindow = window;
		window.Show(Element, 300.0, 0.0, byMouse: true, point);
		window.OnAction += delegate
		{
			model.Selected = false;
			if (_previousWindow == window)
			{
				_previousWindow = null;
			}
		};
		model.Selected = true;
	}

	private async void ShowCourseDetail(DisplayItemModel model, bool fromUpDown)
	{
		if (model.Course == null)
		{
			return;
		}
		CourseDetailViewModel viewModel = await CourseDetailViewModel.Build(model.Course);
		model.Selected = true;
		Point point = default(Point);
		if (fromUpDown && _previousWindow != null)
		{
			ClosePreviousWindow();
			point = new Point(_previewsX, (Element as FrameworkElement)?.ActualHeight ?? 40.0);
		}
		CourseDetailWindow window = new CourseDetailWindow(viewModel);
		window.Closed += async delegate
		{
			model.Selected = false;
			if (_previousWindow == window)
			{
				_previousWindow = null;
			}
		};
		_previousWindow = window;
		if (!fromUpDown)
		{
			_previewsX = Mouse.GetPosition(Element).X;
		}
		window.Show(Element, 300.0, showByMouse: true, point);
	}

	private async Task ShowCalDetail(DisplayItemModel model, bool fromUpDown)
	{
		string id = model.Id;
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		model.Selected = true;
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(ArchivedDao.GetOriginalId(id));
		if (calendarEventModel != null)
		{
			calendarEventModel.DueStart = model.StartDate;
			calendarEventModel.DueEnd = model.DueDate;
			Point point = default(Point);
			if (fromUpDown && _previousWindow != null)
			{
				ClosePreviousWindow();
				point = new Point(_previewsX, (Element as FrameworkElement)?.ActualHeight ?? 40.0);
			}
			CalendarDetailWindow calendarDetailWindow = new CalendarDetailWindow();
			calendarDetailWindow.Disappear -= OnWindowClosed;
			calendarDetailWindow.Disappear += OnWindowClosed;
			_previousWindow = calendarDetailWindow;
			if (!fromUpDown)
			{
				_previewsX = Mouse.GetPosition(Element).X;
			}
			calendarDetailWindow.Show(Element, 0.0, 0.0, !model.InKanban, new CalendarDetailViewModel(calendarEventModel), point);
		}
	}

	private void ToastUnableMessage(object sender, MouseButtonEventArgs e)
	{
		TryToastUnable();
	}

	private bool TryToastUnable()
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return false;
		}
		if (TeamDao.IsTeamExpired(model.TeamId))
		{
			GetParentWindow()?.TryToastString(null, Utils.GetString("TeamExpiredOperate"));
			return false;
		}
		if (model.Permission != null)
		{
			string permission = model.Permission;
			if (permission == "read")
			{
				string arg = Utils.GetString("ReadOnly");
				GetParentWindow()?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return false;
			}
			if (permission == "comment")
			{
				string arg = Utils.GetString("CanComment");
				GetParentWindow()?.TryToastString(null, string.Format(Utils.GetString("NoPermissionToEdit"), arg));
				return false;
			}
		}
		return true;
	}

	public void OnCheckBoxClick(DisplayItemModel model, bool soundPlayed = false)
	{
		if (model.IsHabit)
		{
			model.IsToggling = false;
			if (!_isInAnimation)
			{
				ToggleHabitCompleted(model);
			}
		}
		else if (model.Enable)
		{
			ToggleItemCompleted(model, soundPlayed);
		}
		else
		{
			TryToastUnable();
			model.IsToggling = false;
		}
	}

	private async void ToggleHabitCompleted(DisplayItemModel model)
	{
		if (model.Status != 0)
		{
			await ResetHabit();
		}
		else
		{
			try
			{
				await CheckInHabit();
			}
			catch (Exception)
			{
			}
		}
		TaskCountCache.ReloadProjectTaskCount(GetProjectIdentify());
	}

	private async Task ResetHabit()
	{
		DisplayItemModel model = Model;
		ITaskList taskList = GetTaskList();
		if (taskList != null && GetTaskList().Editable() && model?.Habit != null)
		{
			await HabitService.ResetCheckInHabit(model.Id, DateTime.Today, model.Habit.Type);
			taskList.ReLoad(model.Id);
		}
	}

	private async Task CheckInHabit()
	{
		DisplayItemModel model = Model;
		_isInAnimation = true;
		if (GetTaskList() == null || !GetTaskList().Editable() || model?.Habit == null)
		{
			return;
		}
		HabitModel habitModel = await HabitDao.GetHabitById(model.Id);
		if (habitModel == null)
		{
			_isInAnimation = false;
			return;
		}
		model.SetHabit(habitModel);
		if (habitModel.IsBoolHabit())
		{
			await CheckInBooleanHabit();
		}
		else if (Math.Abs(habitModel.Step + 1.0) <= 0.001)
		{
			GetTaskListView()?.ManuallyRecordCheckIn(Element, model, OnCheckInPopupSave);
		}
		else
		{
			HabitAction action = ((!(Math.Abs(habitModel.Step) < 1E-05)) ? HabitAction.AutoRecord : HabitAction.CheckIn);
			await CheckInRealHabit(habitModel.Step, action);
		}
		_isInAnimation = false;
	}

	private async void OnCheckInPopupSave(object sender, double e)
	{
		DisplayItemModel model = Model;
		HabitModel habitModel = ((model != null) ? (await HabitDao.GetHabitById(model.Id)) : null);
		HabitModel habitModel2 = habitModel;
		if (habitModel2 != null)
		{
			HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habitModel2.Type, habitModel2.Step, habitModel2.Goal);
			if (habitCheckInType != HabitCheckInType.ManualUnCompleted && habitCheckInType != HabitCheckInType.ManualCompleted && habitCheckInType != HabitCheckInType.AutoUncompleted && habitCheckInType != HabitCheckInType.AutoCompleted)
			{
				GetTaskList()?.ReLoad(model.Id);
				return;
			}
			model.SetHabit(habitModel2);
			await CheckInRealHabit(e, HabitAction.ManualRecord);
		}
	}

	private async Task CheckInRealHabit(double step, HabitAction action)
	{
		DisplayItemModel model = Model;
		if (model == null || !model.IsHabit)
		{
			return;
		}
		if (model.Habit == null)
		{
			DisplayItemModel displayItemModel = model;
			displayItemModel.SetHabit(await HabitDao.GetHabitById(model.Id));
			if (model.Habit == null)
			{
				return;
			}
		}
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
		GetTaskListView()?.ShowRecordPopup(Element, (Math.Abs(step) < 1E-05) ? model.Habit.Goal : step, model.Habit.Unit);
		await HabitService.CheckInHabit(model.Id, DateTime.Today, step, increment: true, checkShowRecord: true, 200, null, null, action);
		await Task.Delay(200);
	}

	private async Task CheckInBooleanHabit()
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			ITaskList taskList = GetTaskList();
			model.SourceViewModel.Status = 2;
			model.SourceViewModel.CompletedTime = DateTime.Now;
			Utils.PlayCompletionSound();
			await Task.Delay(16);
			taskList?.AfterTaskChanged(null);
			await HabitService.CheckInHabit(model.Id, DateTime.Today, -1.0, increment: true, checkShowRecord: true, 0, null, null, HabitAction.CheckIn);
			taskList?.ReLoad(null);
		}
	}

	public async Task<bool> ShowOperationDialogSafely()
	{
		try
		{
			DisplayItemModel model = Model;
			if (model == null)
			{
				return false;
			}
			if (model.IsHabit && model.Status == 0)
			{
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
				operationDialog.SetPlaceTarget(Element);
				operationDialog.Operated += async delegate(object obj, KeyValuePair<string, ActionType> kv)
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
						await HabitService.SkipHabit(model.Id);
						GetParentWindow()?.TryToastString(null, Utils.GetString("Skipped"));
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
				operationDialog.Show();
				return true;
			}
			if (model.IsEvent)
			{
				CalendarOperationDialog calendarOperationDialog = new CalendarOperationDialog(new EventArchiveArgs(model.SourceViewModel));
				calendarOperationDialog.Operated += delegate
				{
					OnEventArchived();
				};
				calendarOperationDialog.SetPlaceTarget(Element);
				calendarOperationDialog.Show();
			}
			else if (model.IsCourse)
			{
				ArchiveOperationDialog archiveOperationDialog = new ArchiveOperationDialog(model.Id, ArchiveKind.Course);
				archiveOperationDialog.SetPlaceTarget(Element);
				archiveOperationDialog.Operated += delegate(object obj, KeyValuePair<string, ActionType> kv)
				{
					GetTaskList()?.RemoveItemById(kv.Key);
				};
				archiveOperationDialog.Show();
			}
			else
			{
				if (!model.Enable)
				{
					if (!TryToastUnable())
					{
						SelectItem();
						return false;
					}
					if (model.Deleted != 0)
					{
						bool flag = TeamService.GetTeamId() == model.TeamId;
						OperationDialog operationDialog2 = new OperationDialog(string.Empty, flag ? new List<OperationItemViewModel>
						{
							new OperationItemViewModel(ActionType.Restore)
						} : new List<OperationItemViewModel>
						{
							new OperationItemViewModel(ActionType.Restore),
							new OperationItemViewModel(ActionType.DeleteForever)
						});
						operationDialog2.SetPlaceTarget(Element);
						operationDialog2.Operated += async delegate(object o, KeyValuePair<string, ActionType> e)
						{
							switch (e.Value)
							{
							case ActionType.Restore:
								await TaskService.BatchRestoreProject(new List<string> { model.Id });
								break;
							case ActionType.DeleteForever:
								if (new CustomerDialog(Utils.GetString("DeleteForever"), Utils.GetString("DeleteForeverHint"), Utils.GetString("Delete"), Utils.GetString("Cancel"))
								{
									Owner = ((Element == null) ? null : Window.GetWindow(Element)),
									Topmost = false
								}.ShowDialog() == true)
								{
									await TaskService.DeleteTask(model.Id, 2);
								}
								break;
							}
						};
						operationDialog2.Show();
					}
					return false;
				}
				await ShowOperationDialog();
			}
			return true;
		}
		catch (Exception)
		{
		}
		return false;
	}

	protected virtual void OnEventArchived()
	{
		GetTaskList()?.OnItemArchived();
	}

	private async Task ShowOperationDialog()
	{
		ITaskList list = GetTaskList();
		if (list == null || !list.Editable())
		{
			return;
		}
		DisplayItemModel model = Model;
		if (model == null || !model.IsTaskOrNote || string.IsNullOrEmpty(model.Id))
		{
			return;
		}
		if (TaskDetailPopup.IsShowing() && !(Utils.GetParentWindow(Element) is TaskDetailPopup))
		{
			TaskDetailPopup.TryCloseWindow();
			return;
		}
		bool? pinned = null;
		bool showAIComplete = false;
		if (model.IsTaskOrNote)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
			pinned = taskModel != null && taskModel.pinnedTimeStamp > 0;
			if (model.IsTask)
			{
				showAIComplete = taskModel.status == 0;
			}
		}
		OperationExtra operationExtra = await TaskOperationHelper.GetTaskAccessInfo(model.Id, list.CanAddSubTask() && model.Enable && (model.InDetail || model.Status == 0));
		if (operationExtra == null)
		{
			return;
		}
		operationExtra.ShowCopy = list.Copyable();
		operationExtra.ShowCopyLink = operationExtra.ShowCopy;
		operationExtra.IsPinned = pinned;
		operationExtra.ShowAIComplete = showAIComplete;
		TaskOperationDialog dialog = new TaskOperationDialog(operationExtra, Element);
		if (Window.GetWindow(Element) is WidgetWindow widgetWindow)
		{
			dialog.Resources = widgetWindow.Resources;
		}
		dialog.CreateSubTask += CreateSubTask;
		dialog.Copied += CopyTask;
		dialog.LinkCopied += CopyTaskLink;
		dialog.Deleted += DeleteTask;
		dialog.AbandonOrReopen += OnAbandonOrReopenTask;
		dialog.PrioritySelect += SetPriority;
		dialog.ProjectSelect += OnProjectSelect;
		dialog.AssigneeSelect += OnAssigneeSelect;
		dialog.TagsSelect += OnTagsSelect;
		dialog.SkipCurrentRecurrence += OnSkipRecurrence;
		dialog.TimeClear += delegate
		{
			dialog.Dismiss();
			OnTimeClear();
			list.SetDetailInOperation(inOperate: false);
		};
		dialog.TimeSelect += delegate(object arg, TimeData data)
		{
			dialog.Dismiss();
			SetReminderAndRepeat(data);
			UtilLog.Info("ListItem.SetDate id " + model.Id + " from:taskOperation");
			list.SetDetailInOperation(inOperate: false);
		};
		dialog.QuickDateSelect += delegate(object arg, DateTime date)
		{
			dialog.Dismiss();
			SetStartDate(date);
			UtilLog.Info($"ListItem.SetDate id {model.Id} value {date} from:taskOperation.QuickSet");
			list.SetDetailInOperation(inOperate: false);
		};
		dialog.CompleteDateChanged += async delegate(object o, DateTime date)
		{
			await TaskService.ChangeCompleteDate(model.TaskId, date);
			GetTaskList()?.ReLoad(model.TaskId);
			SyncManager.TryDelaySync();
		};
		dialog.SwitchTaskOrNote += OnSwitchClick;
		dialog.Toast += OnTaskOperationToast;
		dialog.Starred += OnStarredClick;
		dialog.AIComplete += async delegate
		{
			dialog.Dismiss();
			SelectItem();
			if (!UserDao.IsPro())
			{
				ProChecker.ShowUpgradeDialog(ProType.AICompleteSuggest);
			}
			else
			{
				TaskModel taskModel2 = await TaskDao.GetThinTaskById(model.TaskId);
				if (taskModel2.status == 0)
				{
					if (AICompleteWindow.Window == null)
					{
						AICompleteWindow.Window = new AICompleteWindow(new TaskBaseViewModel(taskModel2))
						{
							Owner = App.Window
						};
						AICompleteWindow.Window.Show();
					}
					else
					{
						GetTaskListView()?.Toast(Utils.GetString("AIAnotherTaskGenerating"));
					}
				}
			}
		};
		dialog.Closed += delegate
		{
			if (!model.InDetail)
			{
				PopupStateManager.OnViewPopupClosed();
			}
			model.InOperation = false;
		};
		dialog.Disappear += delegate(object arg, bool e)
		{
			list.SetDetailInOperation(inOperate: false, e);
			GetTaskListView()?.SetMatrixInOperation(inOperation: false);
		};
		list.SetDetailInOperation(inOperate: true);
		dialog.Show();
		model.InOperation = true;
		if (!model.InDetail)
		{
			PopupStateManager.OnViewPopupOpened();
		}
		GetTaskListView()?.SetMatrixInOperation(inOperation: true);
	}

	private void OnTaskOperationToast(object sender, string e)
	{
		GetTaskListView()?.Toast(e);
	}

	private async void OnAbandonOrReopenTask(object sender, EventArgs e)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			UtilLog.Info("ListItem.AbandonOrReopenTask " + model.Id + ", currentStatus " + model.Id);
			int num = ((!model.IsAbandoned) ? (-1) : 0);
			model.SourceViewModel.Status = num;
			await TaskService.SetTaskStatus(model.TaskId, num, num != 0, GetTaskListView()?.GetToastParent());
		}
	}

	private async void OnStarredClick(object sender, bool isPin)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			string projectId = GetTaskListView()?.GetIdentity()?.CatId;
			string id = model.Id;
			bool inDetail = model.InDetail;
			await TaskService.TogglesStarred(id, projectId, null, null, inDetail);
		}
	}

	public async void OnSwitchClick(object sender, EventArgs e)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			UtilLog.Info("ListItem.SwitchTaskNote " + model.Id + ", currentKind " + model.Kind);
			await TaskService.SwitchTaskOrNote(model.Id);
			GetTaskList()?.AfterTaskChanged(model, tryFocused: true);
			SyncManager.TryDelaySync();
		}
	}

	private async void CreateSubTask(object sender, EventArgs e)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			GetTaskList()?.CreateSubTask(model, addToLastOne: true);
		}
	}

	private async void CopyTaskLink(object sender, EventArgs e)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(Model?.Id);
		if (taskModel != null)
		{
			TaskUtils.CopyTaskLink(taskModel.id, taskModel.projectId, taskModel.title);
		}
	}

	private async void OnSkipRecurrence(object sender, EventArgs e)
	{
		await TaskService.SkipCurrentRecurrence(Model.Id, notify: true, GetParentWindow());
	}

	private async void OnAssigneeSelect(object sender, AvatarInfo assignee)
	{
		OnAssigneeSelect(assignee);
	}

	private async Task OnAssigneeSelect(AvatarInfo assignee)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			model.SourceViewModel.Assignee = assignee.UserId;
			model.AvatarUrl = assignee.AvatarUrl;
			if (GetTaskList() != null)
			{
				await TaskService.SetAssignee(model.Id, assignee.UserId);
			}
		}
	}

	private async void OnTagsSelect(object sender, TagSelectData data)
	{
		List<string> omniSelectTags = data.OmniSelectTags;
		DisplayItemModel model = Model;
		if (model != null)
		{
			await TaskService.SetTags(model.Id, omniSelectTags);
			UtilLog.Info("ListItem.SetTags id " + Model?.Id + "  from:taskOperation");
			TryToastQuadrantChanged(model.Id);
		}
	}

	private async Task OnProjectSelect(ProjectModel project, bool canReload, string columnId = null)
	{
		DisplayItemModel model = Model;
		bool projectChanged = model?.ProjectId != project?.id;
		if (model == null || project == null || (!projectChanged && (model.ColumnId == columnId || string.IsNullOrEmpty(columnId))))
		{
			return;
		}
		ITaskList taskList = GetTaskList();
		List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(model.TaskId, model.ProjectId);
		if (list != null && list.Count > 0)
		{
			List<string> ids = list.Select((TaskModel t) => t.id).ToList();
			ids.Add(model.TaskId);
			await TaskDao.RemoveTaskParentId(model.TaskId);
			UtilLog.Info("SetParent id " + model.TaskId + ",parent null, from:setProject");
			await TaskService.BatchMoveProject(ids, new MoveProjectArgs(project)
			{
				ColumnId = columnId
			});
			UtilLog.Info("SetChildrenProject ids " + ids.Join(";") + ",project " + project.id + ", from:setProject");
			if (canReload)
			{
				taskList.AfterTaskChanged(model);
				if (projectChanged)
				{
					taskList.AfterTaskProjectChanged(model);
				}
			}
			return;
		}
		if (taskList != null)
		{
			await TaskService.MoveProject(model.Id, project.id, null, columnId);
			if (canReload)
			{
				taskList.AfterTaskChanged(model);
				if (projectChanged)
				{
					taskList.AfterTaskProjectChanged(model);
				}
			}
		}
		TryToastQuadrantChanged(model.Id);
	}

	private async void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		DisplayItemModel model = Model;
		var (text, columnId, _) = e.GetProjectAndColumnId();
		UtilLog.Info("ListItem.SetProject id " + model?.Id + ",value " + text + " from:taskOperation");
		ProjectModel projectById = CacheManager.GetProjectById(text);
		if (projectById != null)
		{
			await OnProjectSelect(projectById, canReload: true, columnId);
		}
	}

	private async void CopyTask(object sender, EventArgs e)
	{
		if (Model != null)
		{
			GetTaskList()?.CopyTask(Model.Id);
		}
	}

	private void DeleteTask(object sender, EventArgs e)
	{
		DeleteTask();
	}

	public void DeleteTask()
	{
		if (Model != null)
		{
			GetTaskList()?.DeleteTask(Model.Id, TaskDeleteType.DeleteButton);
		}
	}

	public void DeleteSelectedTasks()
	{
		GetTaskList()?.DeleteSelectedTasks();
	}

	private async void SetReminderAndRepeat(TimeData timeData)
	{
		TaskService.TryFixRepeatFlag(timeData);
		DisplayItemModel model = Model;
		if (model != null)
		{
			TaskListView taskListView = GetTaskListView();
			if (taskListView != null && taskListView.ViewModel.InDetail && !string.IsNullOrEmpty(model.RepeatFlag) && model.Status == 0 && (Utils.FindParent<TaskDetailPopup>(taskListView)?.ShowInCal ?? false))
			{
				ModifyRepeatHandler.TryUpdateDueDate(model.TaskId, model.StartDate, model.DueDate, timeData, 1, 1);
			}
			else if (GetTaskList() != null)
			{
				await TaskService.SetDate(model.Id, timeData);
				TryToastQuadrantChanged(model.Id);
			}
		}
	}

	private async Task SetStartDate(DateTime startDate)
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		if (model.IsTaskOrNote)
		{
			TaskListView taskListView = GetTaskListView();
			if (taskListView != null && taskListView.ViewModel.InDetail && !string.IsNullOrEmpty(model.RepeatFlag) && model.Status == 0 && (Utils.FindParent<TaskDetailPopup>(taskListView)?.ShowInCal ?? false))
			{
				TaskModel task = await TaskDao.GetThinTaskById(model.TaskId);
				if (task != null && model.StartDate.HasValue)
				{
					List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
					TimeData timeData = new TimeData
					{
						StartDate = model.StartDate,
						DueDate = model.DueDate,
						IsAllDay = (!model.StartDate.HasValue || (model.IsAllDay ?? true)),
						Reminders = reminders,
						RepeatFrom = model.RepeatFrom,
						RepeatFlag = model.RepeatFlag,
						ExDates = ((task.exDate != null) ? task.exDate.ToList() : new List<string>())
					};
					ModifyRepeatHandler.TryUpdateDueDateOnlyDate(model.TaskId, model.StartDate, model.DueDate, timeData, startDate, 1, 1);
				}
			}
			else if (!string.IsNullOrEmpty(model.AttendId) && !AgendaHelper.CanAccessAgenda(model))
			{
				GetParentWindow()?.TryToastString(null, Utils.GetString("AttendeeSetDate"));
			}
			else
			{
				TimeData timeData2 = new TimeData
				{
					StartDate = model.StartDate,
					DueDate = model.DueDate,
					IsAllDay = model.IsAllDay,
					RepeatFlag = model.RepeatFlag,
					RepeatFrom = model.RepeatFrom
				};
				TaskService.ChangeStartDate(ref timeData2, startDate);
				await TaskService.SetStartDate(model.Id, startDate);
				TryToastQuadrantChanged(model.Id);
			}
		}
		else if (model.IsItem)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
			if (string.IsNullOrEmpty(taskModel.attendId) || AgendaHelper.CanAccessAgenda(taskModel))
			{
				await TaskService.SetSubtaskDate(model.TaskId, model.Id, startDate);
			}
			else
			{
				GetParentWindow()?.TryToastString(null, Utils.GetString("AttendeeSetDate"));
			}
		}
	}

	private async Task OnTimeClear()
	{
		DisplayItemModel model = Model;
		if (model == null || Element == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(model.AttendId) && !AgendaHelper.CanAccessAgenda(model))
		{
			GetParentWindow()?.TryToastString(null, Utils.GetString("AttendeeSetDate"));
		}
		else if (model.Type == DisplayType.Task || model.Type == DisplayType.Agenda || model.IsNote)
		{
			if (await TaskOperationHelper.CheckIfTaskAllowClearDate(model.TaskId, Element) && GetTaskList() != null)
			{
				await TaskService.ClearDate(model.Id);
				TryToastQuadrantChanged(model.Id);
			}
		}
		else if (model.Type == DisplayType.CheckItem)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
			if (taskModel == null || string.IsNullOrEmpty(taskModel.attendId) || AgendaHelper.CanAccessAgenda(taskModel))
			{
				await TaskService.SetCheckItemDate(model.Id, model.TaskId, new TimeDataModel
				{
					StartDate = null,
					IsAllDay = null
				});
			}
			else
			{
				GetParentWindow()?.TryToastString(null, Utils.GetString("AttendeeSetDate"));
			}
		}
	}

	public async Task SetPriority(int priority, bool checkBatch = false)
	{
		if (checkBatch)
		{
			TaskListView taskListView = GetTaskListView();
			if (taskListView != null && taskListView.GetSelectedTaskIds()?.Count > 0)
			{
				return;
			}
		}
		if (Model != null && Model.Enable && !Model.IsNote)
		{
			UtilLog.Info($"ListItem.SetPriority id {Model?.Id},value {priority} from:shortCut");
			await SetPriority(priority);
		}
	}

	private async Task SetTag(string tag)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			List<string> list = model.Tags?.ToList() ?? new List<string>();
			if (!list.Contains(tag))
			{
				list.Add(tag);
			}
			await TaskService.SetTags(model.TaskId, list);
			TryToastQuadrantChanged(model.Id);
		}
	}

	public async void SelectDate(FrameworkElement target = null, double hOffset = 100.0, double vOffset = 12.0)
	{
		DisplayItemModel model = Model;
		if (model == null || Element == null || !model.Enable)
		{
			return;
		}
		if (!string.IsNullOrEmpty(model.AttendId) && !AgendaHelper.CanAccessAgenda(model))
		{
			GetParentWindow()?.TryToastString(null, Utils.GetString("AttendeeSetDate"));
			return;
		}
		TaskModel originalTask = await TaskDao.GetThinTaskById(model.Id);
		if (originalTask != null)
		{
			List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(model.Id);
			TimeData timeData = new TimeData
			{
				StartDate = originalTask.startDate,
				DueDate = originalTask.dueDate,
				IsAllDay = (!originalTask.startDate.HasValue || (originalTask.isAllDay ?? true)),
				Reminders = list?.ToList(),
				RepeatFrom = originalTask.repeatFrom,
				RepeatFlag = originalTask.repeatFlag,
				AnnoyingAlert = originalTask.annoyingAlert,
				HasTime = originalTask.startDate.HasValue,
				TimeZone = new TimeZoneViewModel(originalTask.Floating, originalTask.timeZone),
				IsDefault = !originalTask.startDate.HasValue
			};
			SetDateDialog dialog = SetDateDialog.GetDialog();
			dialog.ClearEventHandle();
			dialog.Clear += delegate
			{
				OnTimeClear();
			};
			dialog.Save += delegate(object sender, TimeData data)
			{
				SetReminderAndRepeat(data);
			};
			dialog.Hided += delegate
			{
				GetTaskList()?.SetDetailInOperation(inOperate: false);
			};
			dialog.SkipRecurrence += OnSkipRecurrence;
			dialog.Show(timeData, new SetDateDialogArgs(calendarMode: false, originalTask.kind == "NOTE", itemMode: false, target ?? Element, hOffset, vOffset, PlacementMode.Relative, showQuickDate: true, !TaskService.IsNonRepeatTask(originalTask)));
			GetTaskList()?.SetDetailInOperation(inOperate: true);
		}
	}

	public async void SetDate(string key)
	{
		DisplayItemModel model = Model;
		if (model != null && model.Enable)
		{
			DateTime date = DateTime.Today;
			switch (key)
			{
			case "today":
				date = DateTime.Today;
				break;
			case "tomorrow":
				date = DateTime.Today.AddDays(1.0);
				break;
			case "nextweek":
				date = DateTime.Today.AddDays(7.0);
				break;
			}
			await SetStartDate(date);
			UtilLog.Info($"ListItem.SetDate id {model.Id} value {date} from:shortCut");
			TryToastQuadrantChanged(model.Id);
		}
	}

	public virtual async Task<TaskDetailPopup> ShowTaskDetail(bool focusTitle = false, bool fromUpDown = false)
	{
		DisplayItemModel model = Model;
		if (model == null || string.IsNullOrEmpty(model.TaskId))
		{
			return null;
		}
		if (Element == null)
		{
			return null;
		}
		TaskDetailViewModel taskDetailViewModel = await TaskDetailViewModel.Build(model.GetTaskId());
		if (taskDetailViewModel != null)
		{
			if (PopupStateManager.LastTarget == Element)
			{
				return null;
			}
			FrameworkElement frameworkElement = Element as FrameworkElement;
			double num = ((frameworkElement != null && !(!model.InKanban && focusTitle)) ? (frameworkElement.ActualWidth + 10.0) : 262.0);
			double height = 0.0;
			Point point = default(Point);
			if (!focusTitle && model.InMatrix && Element is TaskListItem taskListItem)
			{
				point = taskListItem.GetClickPoint();
			}
			if (!model.InKanban)
			{
				point = new Point(10.0, 10.0);
			}
			if (!model.InKanban && focusTitle)
			{
				point = default(Point);
			}
			TaskDetailPopup taskDetailPopup = null;
			if (fromUpDown && _previousWindow != null)
			{
				if (_previousWindow is TaskDetailPopup taskDetailPopup2)
				{
					taskDetailPopup = taskDetailPopup2;
				}
				else
				{
					ClosePreviousWindow();
				}
				if (!model.InKanban)
				{
					num = 10.0 + _previewsX;
					height = frameworkElement?.ActualHeight ?? 40.0;
					point = default(Point);
				}
			}
			if (taskDetailPopup == null)
			{
				taskDetailPopup = new TaskDetailPopup();
			}
			IToastShowWindow parentWindow = GetParentWindow();
			if (parentWindow != null)
			{
				taskDetailPopup.DependentWindow = parentWindow;
			}
			model.Selected = true;
			taskDetailPopup.Disappear -= OnWindowClosed;
			taskDetailPopup.Disappear += OnWindowClosed;
			taskDetailPopup.Detail.SubTaskChanged += OnChildrenChanged;
			taskDetailPopup.Tag = GetTaskList();
			taskDetailPopup.Detail.ShowUndoOnTaskDeleted += OnTaskDeleted;
			int qLevel = 0;
			if (model.InMatrix)
			{
				UserActCollectUtils.AddClickEvent("matrix", "matrix_action", "task_detail");
				qLevel = Utils.FindParent<QuadrantControl>(Element)?.Level ?? 0;
			}
			TaskListView taskListView = GetTaskListView();
			if (taskListView != null)
			{
				taskListView.SetSelectedIdOnShowDetail(model.TaskId);
				taskListView.SetMatrixInOperation(inOperation: true);
			}
			if (!fromUpDown || _previousWindow == null)
			{
				_previousWindow = taskDetailPopup;
				_previewsX = Mouse.GetPosition(Element).X + 10.0;
				taskDetailPopup.Show(taskDetailViewModel, model.IsItem ? model.Id : string.Empty, new TaskWindowDisplayArgs(Element, num, point, focusTitle, height, qLevel));
			}
			else
			{
				taskDetailPopup.MovePosition(Element, taskDetailViewModel, model.IsItem ? model.Id : string.Empty, point, num, height);
			}
			return taskDetailPopup;
		}
		return null;
	}

	public static void ClosePreviousWindow()
	{
		if (_previousWindow is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.TryClose();
		}
		else if (_previousWindow is CalendarDetailWindow calendarDetailWindow)
		{
			calendarDetailWindow.RestoreFocus = false;
			calendarDetailWindow.IsOpen = false;
		}
		else if (_previousWindow is CourseDetailWindow courseDetailWindow)
		{
			courseDetailWindow.RestoreFocus = false;
			courseDetailWindow.IsOpen = false;
		}
		else if (_previousWindow is HabitCheckInWindow habitCheckInWindow)
		{
			habitCheckInWindow.RestoreFocus = false;
			habitCheckInWindow.IsOpen = false;
		}
		_previousWindow = null;
	}

	private IToastShowWindow GetParentWindow()
	{
		_parentWindow = _parentWindow ?? Utils.FindParent<IToastShowWindow>(Element);
		return _parentWindow;
	}

	private void OnTaskDeleted(object sender, string e)
	{
		if (Model != null)
		{
			GetTaskList()?.RemoveItemById(e);
		}
	}

	private void OnChildrenChanged(object sender, EventArgs e)
	{
		GetTaskList()?.ReLoad(Model?.Id);
	}

	private async void OnWindowClosed(object sender, string e)
	{
		if (sender.Equals(_previousWindow))
		{
			_previousWindow = null;
		}
		bool flag = Utils.IfShiftPressed() || Utils.IfCtrlPressed();
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.Detail.SubTaskChanged -= OnChildrenChanged;
			taskDetailPopup.Detail.ShowUndoOnTaskDeleted -= OnTaskDeleted;
			taskDetailPopup.Disappear -= OnWindowClosed;
			if (taskDetailPopup.Tag is ITaskList taskList)
			{
				if (!flag)
				{
					(taskList as TaskListView)?.RemoveSelected(e);
				}
				(taskList as TaskListView)?.SetMatrixInOperation(inOperation: false);
			}
			taskDetailPopup.Tag = null;
		}
		if (Model != null && !flag)
		{
			Model.Selected = false;
		}
		ReloadOnDetailClosed();
	}

	public virtual void ReloadOnDetailClosed()
	{
	}

	public async void ToggleItemCompleted(DisplayItemModel model, bool soundPlayed = false)
	{
		if (TaskDragHelpModel.DragHelp.IsDragging)
		{
			return;
		}
		if (!model.Enable)
		{
			model.IsToggling = false;
		}
		else if (model.Deleted != 0)
		{
			model.IsToggling = false;
		}
		else if (model.IsTask)
		{
			ToggleTaskCompleted(model, soundPlayed);
		}
		else if (model.IsItem)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
			if (!string.IsNullOrEmpty(taskModel.attendId) && !AgendaHelper.CanAccessAgenda(taskModel))
			{
				UtilLog.Info("ListItem.CompleteItem id " + model.Id + " not owner");
				GetParentWindow()?.TryToastString(null, Utils.GetString("OnlyOwnerCanCompleteSubtask"));
			}
			else
			{
				UtilLog.Info($"ListItem.CompleteItem id {model.Id},currentStatus{model.Status}");
				GetTaskList()?.CompleteCheckitem(model.Id, !soundPlayed);
				model.IsToggling = false;
			}
		}
	}

	private async void ToggleTaskCompleted(DisplayItemModel model, bool soundPlayed = false)
	{
		if (model == null)
		{
			return;
		}
		int status = model.Status;
		UtilLog.Info($"ListItem.CompleteTask id {model.Id},currentStatus{model.Status}");
		if (status == 0)
		{
			if (model.InMatrix)
			{
				UserActCollectUtils.AddClickEvent("matrix", "matrix_action", "complete_task");
			}
			CompleteTask(model, soundPlayed);
		}
		else
		{
			UnCompleteTask(model);
		}
	}

	private async void CompleteTask(DisplayItemModel model, bool soundPlayed = false)
	{
		ITaskList taskList = GetTaskList();
		if (taskList == null || !taskList.Editable())
		{
			model.IsToggling = false;
			return;
		}
		model.SourceViewModel.Status = 2;
		model.SourceViewModel.CompletedTime = DateTime.Now;
		model.SourceViewModel.CompletedUser = LocalSettings.Settings.LoginUserId;
		await Task.Delay(150);
		await TaskService.SetTaskStatus(model.Id, 2, needUndo: true, taskList.GetToastParent(), inDetail: false, !soundPlayed);
		await Task.Delay(10);
		model.IsToggling = false;
		if (taskList is TaskListView dependencyObject && (Window.GetWindow(dependencyObject)?.Equals(App.Window) ?? false))
		{
			App.Window.TryFocus();
		}
	}

	private async void UnCompleteTask(DisplayItemModel model)
	{
		ITaskList taskList = GetTaskList();
		if (taskList == null || !taskList.Editable())
		{
			model.IsToggling = false;
			return;
		}
		model.SourceViewModel.Status = 0;
		model.SourceViewModel.CompletedTime = null;
		await Task.Delay(100);
		await TaskService.SetTaskStatus(model.Id, 0);
		model.IsToggling = false;
		taskList.AfterTaskChanged(null);
	}

	private async void SetPriority(object sender, int priority)
	{
		UtilLog.Info($"ListItem.SetPriority id {Model?.Id},value {priority} from:taskOperation");
		SetPriority(priority);
	}

	private async Task SetPriority(int priority)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			ITaskList taskList = GetTaskList();
			model.SourceViewModel.Priority = priority;
			if (taskList != null)
			{
				await TaskService.SetPriority(model.TaskId, priority);
				TryToastQuadrantChanged(model.Id);
			}
		}
	}

	public async void ClearDate()
	{
		DisplayItemModel model = Model;
		if (model != null && model.Enable)
		{
			ITaskList list = GetTaskList();
			await OnTimeClear();
			await Task.Delay(300);
			list?.FocusItem(model.Id);
		}
	}

	public async void OnTagDelete(string tag)
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			List<string> list = model.Tags.ToList();
			if (list.Contains(tag))
			{
				list.Remove(tag);
			}
			await TaskService.SetTags(model.TaskId, list);
			UtilLog.Info("ListItem.DeleteTag id " + Model?.Id + " tag " + tag);
			SyncManager.TryDelaySync();
			TryToastQuadrantChanged(model.Id);
		}
	}

	public void MoveUp(bool fromDetail = false)
	{
		if (Utils.IfShiftPressed() && !Model.InDetail)
		{
			GetTaskList()?.BatchSelectOnMove(isUp: true);
		}
		else
		{
			GetTaskList()?.MoveUp(Model.TaskId, (Model.Type == DisplayType.CheckItem) ? Model.Id : "", fromDetail);
		}
	}

	public void MoveDown(bool fromDetail = false)
	{
		if (Utils.IfShiftPressed() && !Model.InDetail)
		{
			GetTaskList()?.BatchSelectOnMove(isUp: false);
		}
		else
		{
			GetTaskList()?.MoveDown(Model.TaskId, (Model.Type == DisplayType.CheckItem) ? Model.Id : "", fromDetail);
		}
	}

	public void MoveUp(string taskId, string itemId = "")
	{
		if (Utils.IfShiftPressed() && !Model.InDetail)
		{
			GetTaskList()?.BatchSelectOnMove(isUp: true);
		}
		else
		{
			GetTaskList()?.MoveUp(taskId, itemId);
		}
	}

	public void MoveDown(string taskId, string itemId = "")
	{
		if (Utils.IfShiftPressed() && !Model.InDetail)
		{
			GetTaskList()?.BatchSelectOnMove(isUp: false);
		}
		else
		{
			GetTaskList()?.MoveDown(taskId, itemId);
		}
	}

	public void OnMergeText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			GetTaskList()?.DeleteTask(Model.Id, TaskDeleteType.RemoveText);
		}
	}

	public void OnDragMouseDown(object sender, MouseEventArgs e)
	{
		if (Model.Enable && !TaskDragHelpModel.DragHelp.IsDragging)
		{
			GetTaskList()?.StartDrag(Model, e.GetPosition(Element));
		}
	}

	public void TitleSelectionChanged(int caretIndex)
	{
		GetTaskList()?.SetTitleCaretIndex(caretIndex);
	}

	public void OnMultipleTextPaste(string e)
	{
		GetTaskList()?.MultipleTextPaste(e);
	}

	public void SplitNewTask(SplitData data)
	{
		DisplayItemModel model = Model;
		if (model != null && model.Enable && model.IsTaskOrNote)
		{
			GetTaskList()?.SplitDisplayItem(model.Id);
		}
	}

	public void TitleTextChanged(string text)
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		ITaskList taskList = GetTaskList();
		if (taskList != null)
		{
			if (model.IsTaskOrNote)
			{
				taskList.TaskTitleChanged(model.Id, text);
			}
			else if (model.IsItem)
			{
				taskList.SubtaskTitleChanged(model.TaskId, model.Id, text);
			}
			else if (model.IsEvent)
			{
				taskList.EventTitleChanged(model.Id, text);
			}
		}
	}

	public void ShowSelectAssignPopup()
	{
		DisplayItemModel model = Model;
		if (model != null)
		{
			if (!model.Enable)
			{
				TryToastUnable();
				return;
			}
			SetAssigneeDialog setAssigneeDialog = new SetAssigneeDialog(Model?.ProjectId, null, Model?.Assignee);
			setAssigneeDialog.AssigneeSelect += OnAssigneeSelect;
			setAssigneeDialog.Show();
		}
	}

	public async Task OnInputSelected(QuickSetModel setModel, string text, bool canReload)
	{
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		string id = model.Id;
		await GetTaskList().TaskTitleChanged(id, text);
		switch (setModel.Type)
		{
		case QuickSetType.Project:
			UtilLog.Info("ListItem.SetProject id " + model.Id + ",value " + setModel.Project?.id + " from:quickSelect");
			await OnProjectSelect(setModel.Project, canReload, setModel.ColumnId);
			break;
		case QuickSetType.Priority:
			if (!model.IsNote)
			{
				await SetPriority(setModel.Priority);
			}
			UtilLog.Info($"ListItem.SetPriority id {model.Id},value {setModel.Priority} from:quickSelect");
			break;
		case QuickSetType.Tag:
			await SetTag(setModel.Tag.ToLower());
			UtilLog.Info("ListItem.SetTag id " + model.Id + ",value " + setModel.Tag + " from:quickSelect");
			break;
		case QuickSetType.Date:
			if (setModel.Date.HasValue)
			{
				await SetStartDate(setModel.Date.Value);
			}
			UtilLog.Info($"ListItem.SetDate id {model.Id},value {setModel.Date} from:quickSelect");
			break;
		case QuickSetType.Assign:
			await OnAssigneeSelect(new AvatarInfo(setModel.Avatar.UserId, setModel.Avatar.AvatarUrl));
			break;
		case QuickSetType.Splash:
			this.SplashCommand?.Invoke(this, setModel.SplashCommand);
			break;
		case QuickSetType.CalId:
		case QuickSetType.TaskLink:
			break;
		}
	}

	public void OnOpenPathClick(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<ISectionList>(Element)?.OnTaskOpenClick(Model);
	}

	public void OnCheckBoxRightMouseUp(UIElement ui)
	{
		ui = ui ?? Element;
		DisplayItemModel model = Model;
		if (model == null)
		{
			return;
		}
		ui = ui ?? Element;
		ITaskList list = GetTaskList();
		if (!model.Enable)
		{
			TryToastUnable();
		}
		else
		{
			if (!model.IsTask)
			{
				return;
			}
			SetClosedStatusPopup instance = SetClosedStatusPopup.GetInstance(ui, model.IsCompleted, model.IsAbandoned, 3.0, -5.0, (model.Status == 0) ? new int?(model.Progress) : ((int?)null));
			instance.Abandoned += delegate
			{
				UtilLog.Info("TaskList.AbandonTask " + model.Id + " from:CheckIconRightClick");
				TaskService.SetTaskStatus(model.Id, -1, needUndo: true, list?.GetToastParent());
			};
			instance.Completed += async delegate
			{
				UtilLog.Info("TaskList.CompleteTask " + model.Id + " from:CheckIconRightClick");
				bool playSound = true;
				TaskListItem taskListItem = Utils.FindParent<TaskListItem>(ui);
				if (taskListItem != null)
				{
					playSound = !(await taskListItem.TryPlayCompleteStory(model));
				}
				TaskService.SetTaskStatus(model.Id, 2, needUndo: true, list?.GetToastParent(), inDetail: false, playSound);
			};
			instance.ProgressChangeRequested += async delegate(int progress)
			{
				if (model.Status == 0 && model.SourceViewModel.StartDate.HasValue && model.StartDate.HasValue && model.SourceViewModel.StartDate.Value.Date != model.StartDate.Value.Date && !(await ModifyRepeatHandler.CompleteOrSkipRecurrence(model.TaskId, model.StartDate, list?.GetToastParent())))
				{
					return false;
				}
				TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
				if (taskModel == null)
				{
					return false;
				}
				await TaskService.SyncProgress(taskModel, progress);
				return true;
			};
			instance.Closed += delegate
			{
				list?.SetDetailInOperation(inOperate: false);
			};
			list?.SetDetailInOperation(inOperate: true);
			instance.Show();
		}
	}

	public void NavigateTaskClick()
	{
		GetTaskList()?.OnNavigateTask(Model);
	}

	public void OnLineVisibleChanged(DisplayItemModel model, bool visible)
	{
		GetTaskList()?.OnLineVisibleChanged(model, visible);
	}

	public TaskListView GetTaskListView()
	{
		return GetTaskList() as TaskListView;
	}

	public void SetParentInOperation(bool inOperate)
	{
		GetTaskList()?.SetDetailInOperation(inOperate);
	}

	public void Reset(UIElement element, DisplayItemModel model)
	{
		Element = element;
		Model = model;
	}

	public void Clear()
	{
		Element = null;
		Model = null;
		_iTaskList = null;
		_parentWindow = null;
	}

	public void TitleLostFocus(bool reload)
	{
		GetTaskList()?.OnLostFocus(reload);
	}

	public void SetQuickSetPopup(Popup popup)
	{
		GetTaskListView()?.SetQuickSetPopup(popup);
	}

	public void SetParsedDate(TimeData timeData)
	{
		Model?.OnTimeParsed(timeData);
	}

	public async void SetDateAndTitle(string text, IPaserDueDate parsedData)
	{
		DisplayItemModel model = Model;
		if (model != null && GetTaskList() != null)
		{
			TimeData timeData = parsedData?.ToTimeData(addDefaultReminder: true);
			if (timeData != null)
			{
				await TaskService.SetDateAndTitle(model.Id, text, timeData);
			}
			else
			{
				TitleTextChanged(text);
			}
		}
	}

	public TimeData GetDefaultDate()
	{
		return GetTaskList()?.GetDefaultTimeData() ?? TimeData.BuildDefaultStartAndEnd();
	}

	public bool InProjectList()
	{
		TaskListView taskListView = GetTaskListView();
		if (taskListView != null && !taskListView.ViewModel.InKanban)
		{
			return !taskListView.ViewModel.InDetail;
		}
		return false;
	}

	public ProjectIdentity GetProjectIdentify()
	{
		return GetTaskListView()?.ViewModel.ProjectIdentity;
	}

	private async Task TryToastQuadrantChanged(string taskId)
	{
		int num = GetTaskList()?.QuadrantLevel ?? 0;
		if (num != 0)
		{
			string item = (await MatrixManager.GetTaskQuadrantChangeString(num, taskId)).Item2;
			if (!string.IsNullOrEmpty(item))
			{
				GetParentWindow()?.TryToastString(null, item);
			}
		}
	}

	public async Task<string> GetCompleteStoryText(DisplayItemModel model)
	{
		if (LocalSettings.Settings.ExtraSettings.LastCompleteTime < ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today))
		{
			LocalSettings.Settings.ExtraSettings.LastCompleteTime = ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today);
			if (TaskCache.GetAllTask().All((TaskBaseViewModel t) => t.Status != 2 || t.CompletedTime < DateTime.Today))
			{
				return Utils.GetString("FirstTaskDone");
			}
		}
		TaskListView taskListView = GetTaskListView();
		if (taskListView != null)
		{
			return await taskListView.GetCompleteText(model);
		}
		return null;
	}
}
