using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.MainListView.DetailView;

public class BatchDetailView : ContentControl
{
	private TaskDetailBatchControl _batchControl;

	private List<string> _selectedTaskIds;

	private TaskView _parent;

	private SetBatchTaskGridArgs _batchArgs;

	private TaskDetailViewModel _task;

	private TaskType _taskType;

	private ProjectIdentity _projectIdentity => _parent?.ProjectIdentity;

	public event EventHandler<List<string>> TaskBatchDeleted;

	public BatchDetailView(TaskView parent)
	{
		_batchControl = new TaskDetailBatchControl(this)
		{
			Margin = new Thickness(0.0, 34.0, 0.0, 0.0)
		};
		base.Content = _batchControl;
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
		_parent = parent;
	}

	public async void OnBatchSelect(List<string> selected)
	{
		if (selected.Count < 1)
		{
			return;
		}
		TaskBaseViewModel baseModel = new TaskBaseViewModel
		{
			Status = 0,
			StartDate = null,
			DueDate = null,
			IsAllDay = true,
			Progress = 0,
			Kind = "TEXT"
		};
		TaskDetailViewModel model = new TaskDetailViewModel(baseModel)
		{
			IsOwner = true
		};
		List<TaskModel> tasks = await TaskDao.GetThinTasksInBatch(selected);
		int completedCount = 0;
		string projectId = null;
		string columnId = null;
		string assignee = null;
		BatchData batchData = null;
		SetBatchTaskGridArgs batchGridArgs = new SetBatchTaskGridArgs();
		if (tasks.Count > 0)
		{
			batchData = new BatchData();
			TaskModel task = tasks[0];
			List<TaskReminderModel> source = await TaskReminderDao.GetRemindersByTaskId(task.id);
			batchData.StartDate = task.startDate;
			batchData.DueDate = task.dueDate;
			batchData.IsAllDay = task.isAllDay;
			batchData.Reminders = source.ToList();
			batchData.RepeatFlag = task.repeatFlag;
			batchData.RepeatFrom = task.repeatFrom;
			batchData.IsFloating = task.Floating;
			batchData.TimeZone = task.timeZone;
			baseModel.IsAllDay = task.isAllDay;
			baseModel.StartDate = task.startDate;
			baseModel.DueDate = task.dueDate;
			baseModel.RepeatFlag = task.repeatFlag;
			baseModel.RepeatFrom = task.repeatFrom;
			baseModel.ProjectName = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.projectId)?.name;
			baseModel.TimeZoneName = task.timeZone;
			baseModel.Priority = task.priority;
			model.AnnoyingAlert = task.annoyingAlert;
			columnId = task.columnId;
		}
		foreach (TaskModel task2 in tasks)
		{
			batchGridArgs.IsAllPinned = batchGridArgs.IsAllPinned && task2.pinnedTimeStamp > 0;
			batchGridArgs.IsAllCompleted = batchGridArgs.IsAllCompleted && (task2.isCompleted || task2.kind == "NOTE");
			batchGridArgs.HasAbandoned = batchGridArgs.HasAbandoned || task2.isAbandoned;
			if (task2.kind != "NOTE")
			{
				batchGridArgs.HasUncompleted = batchGridArgs.HasUncompleted || task2.status == 0;
				batchGridArgs.HasCompleted = batchGridArgs.HasCompleted || task2.status == 2;
			}
			if (task2.priority != model.Priority)
			{
				baseModel.Priority = 0;
				batchGridArgs.HasSamePriority = false;
			}
			if (task2.columnId != columnId)
			{
				columnId = "-1";
			}
			if (batchGridArgs.HasSameTag && batchGridArgs.TagStr != task2.tag)
			{
				batchGridArgs.HasSameTag = false;
			}
			if (model.StartDate.HasValue && (model.StartDate != task2.startDate || model.DueDate != task2.dueDate || (model.IsAllDay != task2.isAllDay && task2.Floating == model.IsFloating && task2.timeZone == model.TimeZoneName)))
			{
				baseModel.StartDate = null;
				baseModel.DueDate = null;
				batchGridArgs.HasSameDate = false;
			}
			if (task2.startDate.HasValue)
			{
				batchGridArgs.CanPostpone = true;
				if (!(task2.isAllDay ?? true))
				{
					batchGridArgs.CanPostponeTime = true;
				}
			}
			if (task2.repeatFlag != model.RepeatFlag || task2.repeatFrom != model.RepeatFrom)
			{
				baseModel.RepeatFlag = null;
				baseModel.RepeatFrom = null;
			}
			if (model.AnnoyingAlert != task2.annoyingAlert)
			{
				model.AnnoyingAlert = null;
			}
			if (batchData != null)
			{
				if (batchData.IsDateUnified && !DateUtils.IsSameDate(task2.startDate, batchData.StartDate))
				{
					batchData.IsDateUnified = false;
				}
				if (batchData.IsTimeUnified && (((!DateUtils.IsSameTime(task2.startDate, batchData.StartDate) || task2.dueDate.HasValue || batchData.DueDate.HasValue) && (!(batchData.StartDate == task2.startDate) || !(batchData.DueDate == task2.dueDate))) || batchData.IsAllDay != task2.isAllDay || task2.Floating != batchData.IsFloating || !(task2.timeZone == batchData.TimeZone)))
				{
					batchData.IsTimeUnified = false;
				}
				if (batchData.IsReminderUnified && !TaskReminderDao.IsEquals(await TaskReminderDao.GetRemindersByTaskId(task2.id), batchData.Reminders))
				{
					batchData.IsReminderUnified = false;
				}
				if (batchData.IsRepeatUnified && (!(task2.repeatFlag == batchData.RepeatFlag) || !(task2.repeatFrom == batchData.RepeatFrom)))
				{
					batchData.IsRepeatUnified = false;
				}
			}
			if (task2.deleted != 0)
			{
				batchGridArgs.IsDeleted = true;
				batchGridArgs.CanSwitchTaskOrNote = false;
			}
			if (task2.status != 0)
			{
				completedCount++;
				batchGridArgs.CanSwitchTaskOrNote = false;
			}
			if (TaskCache.IsParentTask(task2.id) || TaskCache.IsChildTask(task2.parentId, task2.projectId))
			{
				batchGridArgs.CanSwitchTaskOrNote = false;
			}
			if (task2.kind == "NOTE")
			{
				batchGridArgs.HasNote = true;
			}
			else
			{
				batchGridArgs.HasTask = true;
			}
			if (projectId == null || projectId == task2.projectId)
			{
				projectId = task2.projectId;
			}
			else
			{
				batchGridArgs.HasSameProject = false;
				projectId = "";
			}
			assignee = ((assignee != null && !(assignee == task2.assignee)) ? "" : task2.assignee);
		}
		model.BatchData = batchData;
		baseModel.ProjectId = projectId;
		baseModel.Assignee = assignee;
		baseModel.ColumnId = columnId;
		if (completedCount == tasks.Count)
		{
			baseModel.Status = 2;
		}
		if (batchGridArgs.HasTask && batchGridArgs.HasNote)
		{
			_taskType = TaskType.TaskAndNote;
		}
		else if (batchGridArgs.OnlyNote)
		{
			_taskType = TaskType.Note;
		}
		else
		{
			_taskType = TaskType.Task;
		}
		baseModel.Kind = (batchGridArgs.OnlyNote ? "NOTE" : "TEXT");
		_task = model;
		base.DataContext = model;
		_selectedTaskIds = selected;
		_batchControl.SetAvatar(assignee, projectId);
		string[] array = Utils.GetString("YouHaveChosenTasks").Split('0');
		if (array.Length == 2)
		{
			_batchControl.BatchTaskText.Inlines.Clear();
			_batchControl.BatchTaskText.Inlines.Add(array[0].Substring(0, array[0].Length - 1));
			_batchControl.BatchTaskText.Inlines.Add(new Run(selected.Count.ToString())
			{
				Foreground = ThemeUtil.GetColor("TextAccentColor")
			});
			_batchControl.BatchTaskText.Inlines.Add(array[1].Substring(1, array[1].Length - 1));
		}
		await SetBatchTaskGridVisibility(batchGridArgs);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		base.Content = null;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		if (base.IsVisible)
		{
			List<string> selectedTaskIds = _selectedTaskIds;
			if (selectedTaskIds != null && selectedTaskIds.Count > 0)
			{
				OnBatchSelect(_selectedTaskIds);
			}
		}
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		BlockingSet<string> blockingSet = new BlockingSet<string>();
		blockingSet.AddRange(e.BatchChangedIds);
		blockingSet.AddRange(e.PinChangedIds);
		blockingSet.AddRange(e.StatusChangedIds);
		blockingSet.AddRange(e.DateChangedIds);
		blockingSet.AddRange(e.PriorityChangedIds);
		blockingSet.AddRange(e.ProjectChangedIds);
		blockingSet.AddRange(e.TagChangedIds);
		blockingSet.AddRange(e.KindChangedIds);
		if (blockingSet.Any())
		{
			CheckBatchSelectTasksChanged(blockingSet.Value);
			return;
		}
		if (e.PinChangedIds.Any())
		{
			List<string> selectedTaskIds = _selectedTaskIds;
			if (selectedTaskIds != null && selectedTaskIds.Any(e.PinChangedIds.Contains))
			{
				List<TaskBaseViewModel> tasks = TaskCache.GetTasksByIds(_selectedTaskIds.ToList());
				if (tasks.Any())
				{
					bool batchStarGrid = tasks.All((TaskBaseViewModel t) => t.IsPinned == tasks[0].IsPinned);
					SetBatchStarGrid(batchStarGrid);
				}
			}
		}
		if (e.DeletedChangedIds.Any() && _selectedTaskIds != null && _selectedTaskIds.Any(e.DeletedChangedIds.Contains))
		{
			_parent.TryExtractDetail();
		}
	}

	private void SetBatchStarGrid(bool isPinned)
	{
		_batchControl.BatchTaskStartPinIcon.Visibility = (isPinned ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchTaskStartUnPinIcon.Visibility = ((!isPinned) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void CheckBatchSelectTasksChanged(HashSet<string> ids)
	{
		if (_selectedTaskIds != null && _selectedTaskIds.Any(ids.Contains))
		{
			if (_parent.ProjectIdentity is TrashProjectIdentity)
			{
				_parent.TryExtractDetail();
			}
			else
			{
				OnBatchSelect(_selectedTaskIds.ToList());
			}
		}
	}

	public void SelectDateClick(MouseButtonEventArgs e)
	{
		ShowSelectDateDialog();
	}

	public bool CheckEnable()
	{
		return _task.Enable;
	}

	public void OnMoveProject(EscPopup popup, bool b)
	{
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(popup, new ProjectExtra
		{
			ProjectIds = new List<string> { _task.ProjectId }
		}, new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			CanSearch = true,
			ShowColumn = true,
			ColumnId = _task.ColumnId
		});
		projectOrGroupPopup.ItemSelect += delegate(object o, SelectableItemViewModel e)
		{
			OnMoveProject(e);
		};
		projectOrGroupPopup.Show();
	}

	private async void OnMoveProject(SelectableItemViewModel e)
	{
		if (e != null)
		{
			(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
			string projectId = projectAndColumnId.Item1;
			string item = projectAndColumnId.Item2;
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
			if (projectModel != null)
			{
				UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "move_project");
				UtilLog.Info("TaskDetail.SetProject " + _task?.TaskId + ", value " + projectModel.id + " from:ProjectSelector");
				await BatchMoveProject(projectModel, item);
				NotifyBatchChanged(hideBatchGrid: true);
			}
		}
	}

	public void OnSetAssigneeMouseUp(bool inBatch, EscPopup popup)
	{
		if (Utils.IsNetworkAvailable())
		{
			SetAssigneeDialog setAssigneeDialog = new SetAssigneeDialog(_task.ProjectId, popup, _task.Assignee);
			AddActionEvent("action", "assignee");
			setAssigneeDialog.AssigneeSelect += async delegate(object o, AvatarInfo avatar)
			{
				await SetAssignee(avatar.UserId, inBatch);
			};
			setAssigneeDialog.Show();
		}
		else
		{
			TryToast(Utils.GetString("NoNetwork"));
		}
	}

	private async Task SetAssigneeImage(string assignee, string projectId)
	{
		if (!string.IsNullOrEmpty(assignee) && assignee != "-1")
		{
			string text = await AvatarHelper.GetAvatarUrl(assignee, projectId);
			if (!string.IsNullOrEmpty(text))
			{
				_batchControl?.SetBatchAssignVisible(show: true, text);
			}
		}
		else
		{
			_batchControl?.SetBatchAssignVisible(show: true, null);
		}
	}

	private async Task SetAssignee(string assignee, bool inBatch)
	{
		_task.SourceViewModel.Assignee = assignee;
		await TaskService.BatchSetAssignee(_selectedTaskIds, assignee);
		if (_parent?.GetSelectedProject() is AssignToMeProjectIdentity)
		{
			NotifyBatchChanged(hideBatchGrid: true);
		}
		else
		{
			SetAssigneeImage(assignee, _task.ProjectId);
		}
	}

	private async void OnDelete()
	{
		UtilLog.Info("TaskDetail.DeleteTasks " + _selectedTaskIds?.Join(";"));
		if (await TaskService.BatchDeleteTaskByIds(_selectedTaskIds))
		{
			NotifyBatchDeleted();
			Utils.FindParent<TaskView>(this)?.TryExtractDetail();
		}
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "delete");
	}

	private async void OnSkipRecurrence(object sender, EventArgs e)
	{
		await TaskService.BatchSkipRecurrence(_selectedTaskIds);
	}

	private async Task OnDateSelected(DateTime time)
	{
		await TaskService.BatchSetStartDate(_selectedTaskIds, time);
		TaskChangeNotifier.NotifyBatchDateChanged(_selectedTaskIds);
	}

	public async void OnRestoreProject()
	{
		await TaskService.BatchRestoreProject(_selectedTaskIds);
	}

	public void OnTag(Grid batchSetTagsButton)
	{
		UtilLog.Info("DetailShowSetTag");
		TagSelectData tagSelectData = GetTagSelectData();
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "tag");
		EscPopup popup = new EscPopup
		{
			Placement = PlacementMode.Mouse,
			StaysOpen = false,
			PlacementTarget = batchSetTagsButton,
			HorizontalOffset = 10.0,
			VerticalOffset = -5.0
		};
		popup.Closed += delegate
		{
			_batchControl?.SetPopupShowIndex(-1);
		};
		BatchSetTagControl batchSetTagControl = new BatchSetTagControl();
		batchSetTagControl.Close += delegate
		{
			popup.IsOpen = false;
		};
		batchSetTagControl.TagsSelect += delegate(object s, TagSelectData e)
		{
			OnTagsSelected(e);
			popup.IsOpen = false;
		};
		popup.Child = batchSetTagControl;
		batchSetTagControl.Init(tagSelectData, firstInit: true);
		popup.IsOpen = true;
		_batchControl?.SetPopupShowIndex(5);
	}

	private TagSelectData GetTagSelectData()
	{
		if (_selectedTaskIds != null && _selectedTaskIds.Count > 0)
		{
			return TagDataHelper.GetSelectTagData(TaskCache.GetTasksByIds(_selectedTaskIds)?.Select((TaskBaseViewModel task) => TagSerializer.ToTags(task.Tag)).ToList());
		}
		return new TagSelectData();
	}

	public async void OnTagsSelected(TagSelectData data)
	{
		UtilLog.Info($"TaskDetail.SetTasksTag taskCount {_selectedTaskIds?.Count} from:TagWindow");
		await TaskService.BatchSetTags(_selectedTaskIds, data);
		SetBatchTagsName();
	}

	public async void OnCopy()
	{
		if (_task != null && _selectedTaskIds != null && _selectedTaskIds.Count != 0 && !(await ProChecker.CheckTaskLimit(_task.ProjectId, _selectedTaskIds.Count)))
		{
			UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "duplicate");
			await TaskService.CopyTasks(_selectedTaskIds, this);
		}
	}

	public void OnTasksDeleted(List<string> taskIds)
	{
		Utils.FindParent<ListViewContainer>(this)?.HideDetail();
	}

	private async Task BatchClearDate()
	{
		await TaskService.BatchClearDate(_selectedTaskIds);
		_task.SetTimeData(new TimeData());
		_task.ExDates = null;
		TaskChangeNotifier.NotifyBatchDateChanged(_selectedTaskIds, this);
	}

	private async Task CompleteOrUndoneTask(bool? isComplete = null)
	{
		int num = ((_task.Status == 0) ? 2 : 0);
		isComplete = isComplete ?? (num == 2);
		if (isComplete.Value && _task.StartDate.HasValue && _task.DisplayStartDate.HasValue && _task.StartDate.Value.Date != _task.DisplayStartDate.Value.Date)
		{
			IToastShowWindow toastWindow = (Window.GetWindow(this) as TaskDetailWindow)?.DependentWindow;
			if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(_task.TaskId, _task.DisplayStartDate, toastWindow)))
			{
				return;
			}
		}
		if (_selectedTaskIds != null && _selectedTaskIds.Count != 0)
		{
			UtilLog.Info(string.Format("TaskDetail.BatchCompleteTasks {0}, isComplete {1}", _selectedTaskIds?.Join(";"), isComplete));
			await BatchToggleCompleteTasks(_selectedTaskIds);
		}
	}

	private async Task BatchSetDateAsync(TimeData model)
	{
		await TaskService.BatchSetDate(_selectedTaskIds, model);
		_task.SetTimeData(model);
		_task.ExDates = model.ExDates;
		TaskChangeNotifier.NotifyBatchDateChanged(_selectedTaskIds, this);
	}

	public async void PrioritySelect(bool inBatch, int priority)
	{
		string data = "priority_none";
		switch (priority)
		{
		case 0:
			data = "priority_none";
			break;
		case 1:
			data = "priority_low";
			break;
		case 3:
			data = "priority_medium";
			break;
		case 5:
			data = "priority_high";
			break;
		}
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", data);
		BatchSetPriority(priority);
	}

	private async void NotifyBatchDeleted()
	{
		_selectedTaskIds = null;
		this.TaskBatchDeleted?.Invoke(this, _selectedTaskIds);
	}

	private async void NotifyBatchChanged(bool hideBatchGrid)
	{
		if (hideBatchGrid)
		{
			_selectedTaskIds = null;
			Utils.FindParent<TaskView>(this)?.TryExtractDetail();
		}
	}

	private async Task BatchMoveProject(ProjectModel project, string columnId = null)
	{
		List<TaskBaseViewModel> tasks = TaskCache.GetTaskAndChildrenInBatch(_selectedTaskIds);
		await TaskDao.RemoveTaskParentIdInBatch((from node in TaskNodeUtils.GetTaskNodeTree(tasks)
			where !node.HasParent
			select node.Value.Id).ToList());
		List<string> ids = tasks.Select((TaskBaseViewModel t) => t.Id).ToList();
		await TaskService.BatchMoveProject(ids, new MoveProjectArgs(project)
		{
			ColumnId = columnId
		});
		TaskModel task = await TaskDao.GetThinTaskById(ids.FirstOrDefault());
		TaskService.TryToastMoveControl(_parent?.GetSelectedProject(), task, project.id, hideTitle: true);
	}

	private async Task BatchToggleCompleteTasks(List<string> selectedTaskIds)
	{
		await TaskService.BatchCompleteTasks(selectedTaskIds);
		NotifyBatchChanged(hideBatchGrid: false);
	}

	public async Task OnBatchDeleteTaskForever()
	{
		if (new CustomerDialog(Utils.GetString("DeleteForever"), Utils.GetString("BatchDeleteForeverHint"), Utils.GetString("Delete"), Utils.GetString("Cancel"))
		{
			Owner = Window.GetWindow(this),
			Topmost = false
		}.ShowDialog() == true)
		{
			await TaskService.BatchDeleteTaskByIds(_selectedTaskIds, showUndo: false, 2);
			NotifyBatchDeleted();
			_parent?.TryExtractDetail();
		}
	}

	private bool CheckUiTagEnable(object sender)
	{
		return (sender as FrameworkElement)?.Tag as bool? == true;
	}

	public async void OnBatchCompleteMouseUp(object sender)
	{
		if (CheckUiTagEnable(sender) && CheckEnable())
		{
			await CompleteOrUndoneTask();
		}
	}

	public void OnBatchAbandonMouseUp(object sender)
	{
		if (CheckUiTagEnable(sender) && CheckEnable())
		{
			UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "abandon");
			List<string> taskIds = _selectedTaskIds?.ToList() ?? new List<string>();
			AbandonTasksAsync(taskIds);
		}
	}

	private async Task AbandonTasksAsync(List<string> taskIds)
	{
		try
		{
			await TaskService.BatchAbandonTasks(taskIds);
			NotifyBatchChanged(hideBatchGrid: false);
		}
		catch (Exception ex)
		{
			UtilLog.Error("BatchDetailView.BatchAbandonTasks failed: " + ex);
			Utils.Toast(Utils.GetString("OperateFailedMessage"));
		}
	}

	public async void OnBatchPinClick(object sender, bool isPin)
	{
		if (CheckUiTagEnable(sender))
		{
			UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", isPin ? "pin" : "unpin");
			await TaskService.BatchStarTaskOrNote(_selectedTaskIds, _parent.ProjectIdentity?.CatId, isPin);
		}
	}

	public async Task OnLinkParentMouseUp(object sender)
	{
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "link_parent_task");
		UserActCollectUtils.AddClickEvent("link_parent_task", "link_from", "batch_om");
		App.Window.StartLinkSearch(_selectedTaskIds, "batch_detail");
	}

	public async Task OnMergeMouseUp(object sender)
	{
		if (CheckUiTagEnable(sender))
		{
			List<string> selectedTaskIds = _selectedTaskIds;
			bool flag = selectedTaskIds != null && selectedTaskIds.Count > 0;
			TaskModel taskModel = default(TaskModel);
			if (flag)
			{
				taskModel = await TaskService.TryMergeTask(_selectedTaskIds, _selectedTaskIds[0], _projectIdentity);
				flag = taskModel != null;
			}
			if (flag)
			{
				TaskChangeNotifier.NotifyTasksMerged(new List<string>(_selectedTaskIds) { taskModel.id });
			}
		}
	}

	public async Task BatchSwitchTaskNoteMouseUp(object sender)
	{
		if (!CheckUiTagEnable(sender))
		{
			if (_batchArgs.OnlyTask)
			{
				Utils.Toast(Utils.GetString("CannotConvertMultiLevelToNote"));
				return;
			}
			if (!_batchArgs.OnlyNote || !_batchArgs.OnlyTask)
			{
				Utils.Toast(Utils.GetString("CannotConvertMixedToNote"));
				return;
			}
		}
		await TaskService.BatchSwitchTaskOrNote(_selectedTaskIds);
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "convert_to_note_or_task");
		SyncManager.TryDelaySync();
		OnBatchSelect(_selectedTaskIds.ToList());
	}

	public async void OnBatchCopyTextMouseUp(object sender)
	{
		if (!CheckUiTagEnable(sender))
		{
			return;
		}
		List<TaskModel> obj = await TaskDao.GetThinTasksInBatch(_selectedTaskIds.ToList());
		StringBuilder stringBuilder = new StringBuilder();
		foreach (TaskModel item in obj)
		{
			stringBuilder.Append(string.IsNullOrEmpty(item.title) ? Utils.GetString("NoTitle") : item.title);
			if (!Utils.IsEmptyDate(item.startDate))
			{
				bool flag = item.isAllDay ?? true;
				stringBuilder.Append("(" + DateUtils.FormatDateStringNoWord(item.startDate.Value, item.dueDate?.AddDays(flag ? (-1) : 0), flag) + ")");
			}
			stringBuilder.Append(Environment.NewLine);
		}
		if (stringBuilder.Length - Environment.NewLine.Length >= 0)
		{
			stringBuilder.Remove(stringBuilder.Length - Environment.NewLine.Length, Environment.NewLine.Length);
		}
		try
		{
			Clipboard.SetDataObject(stringBuilder.ToString(), copy: true);
			Utils.Toast(Utils.GetString("CopyTextTips"));
		}
		catch (Exception)
		{
		}
	}

	public void OnBatchDelete(object sender)
	{
		if (CheckUiTagEnable(sender))
		{
			OnDelete();
		}
	}

	public async Task SetBatchTaskGridVisibility(SetBatchTaskGridArgs args)
	{
		_batchArgs = args;
		bool flag = _parent?.GetSelectedProject() is TrashProjectIdentity;
		_batchControl.BatchTaskMoveProjectButton.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchTaskPriorityButton.Visibility = ((flag || args.HasNote) ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchTaskBlocks.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchSetTagsButton.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchSelectDateButton.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchDeleteFromTrashButton.Visibility = Visibility.Collapsed;
		_batchControl.BatchRestoreProjectButton.Visibility = Visibility.Collapsed;
		if (flag)
		{
			_batchControl.BatchAssigneeGrid.Visibility = Visibility.Collapsed;
			_batchControl.BatchCompleteOrUndoneBorder.Visibility = Visibility.Collapsed;
			if ((_projectIdentity as TrashProjectIdentity)?.IsPerson ?? true)
			{
				_batchControl.BatchDeleteFromTrashButton.Visibility = Visibility.Visible;
			}
			_batchControl.BatchRestoreProjectButton.Visibility = Visibility.Visible;
			return;
		}
		_batchControl.DelayGrid.Visibility = ((!args.CanPostpone) ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchCompleteOrUndoneBorder.Tag = true;
		_batchControl.BatchAbandonBorder.Tag = true;
		_batchControl.BatchTaskStarBorder.Tag = args.CanPinOrUnPin;
		_batchControl.BatchTaskMergeBorder.Tag = args.CanMerge;
		_batchControl.BatchSetParentTaskBorder.Tag = args.CanLinkTask;
		_batchControl.BatchTaskCopyBorder.Tag = true;
		_batchControl.BatchSwitchTaskNoteBorder.Tag = args.CanSwitchNote || args.CanSwitchTask;
		_batchControl.BatchCopyTextBorder.Tag = true;
		_batchControl.BatchDeleteBorder.Tag = args.CanDelete;
		SetBatchTagsName();
		SetBatchProjectName(args.HasSameProject ? _task.ProjectName : null);
		SetBatchPriorityName(args.HasSamePriority, _task.Priority);
		SetBatchStarGrid(args.IsAllPinned);
		SetBatchCompleteGrid(args);
		SetBatchSwitchTaskOrNote(args.OnlyTask);
	}

	private async void ShowSelectDateDialog()
	{
		TimeData timeData = new TimeData
		{
			StartDate = _task.DisplayStartDate,
			DueDate = _task.DisplayDueDate,
			IsAllDay = (!_task.DisplayStartDate.HasValue || (_task.IsAllDay ?? true)),
			Reminders = _task.Reminders?.ToList(),
			RepeatFrom = _task.RepeatFrom,
			RepeatFlag = _task.RepeatFlag,
			ExDates = _task.ExDates,
			AnnoyingAlert = _task.AnnoyingAlert
		};
		if (!string.IsNullOrEmpty(_task.TimeZoneName))
		{
			timeData.TimeZone = new TimeZoneViewModel(_task.IsFloating, _task.TimeZoneName);
		}
		if (timeData.IsAllDay.HasValue && timeData.IsAllDay.Value && timeData.StartDate.HasValue && timeData.DueDate.HasValue && timeData.StartDate.Value.Date == timeData.DueDate.Value.Date)
		{
			timeData.DueDate = null;
		}
		if (_task.BatchData != null)
		{
			timeData.BatchData = _task.BatchData.Clone();
			timeData.IsDefault = !_task.BatchData.StartDate.HasValue && _task.BatchData.IsUnified;
		}
		if (timeData.StartDate.HasValue)
		{
			timeData.IsDefault = false;
		}
		bool canSkip = TaskService.IsAllRepeatTask(_selectedTaskIds);
		bool isNote = _taskType == TaskType.Note || (_taskType == TaskType.TaskAndNote && _parent?.GetSelectedProject()?.IsNote == true);
		SetDateDialog dialog = SetDateDialog.GetDialog();
		dialog.ClearEventHandle();
		dialog.Hided += delegate
		{
			_batchControl?.SetPopupShowIndex(-1);
		};
		dialog.Clear += async delegate
		{
			if (_task.IsNewAdd)
			{
				_task.SetTimeData(new TimeData());
			}
			else
			{
				await BatchClearDate();
			}
		};
		dialog.Save += async delegate(object obj, TimeData model)
		{
			TaskService.TryFixRepeatFlag(model);
			UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "date_other");
			await BatchSetDateAsync(model);
		};
		dialog.SkipRecurrence += OnSkipRecurrence;
		dialog.Show(timeData, new SetDateDialogArgs(calendarMode: false, isNote, itemMode: false, _batchControl?.BatchSelectDateButton, 10.0, -5.0, PlacementMode.Mouse, showQuickDate: true, canSkip));
		_batchControl?.SetPopupShowIndex(0);
		UtilLog.Info("DetailShowBatchSetDate");
	}

	private void SetBatchTagsName()
	{
		if (_batchControl != null)
		{
			TagSelectData tagSelectData = GetTagSelectData();
			if (tagSelectData != null && tagSelectData.OmniSelectTags != null && tagSelectData.OmniSelectTags.Any())
			{
				_batchControl.BatchTagsContainer.Visibility = Visibility.Visible;
				_batchControl.BatchTagsText.Visibility = Visibility.Collapsed;
				TagDisplayControl child = new TagDisplayControl(tagSelectData.OmniSelectTags, editable: false)
				{
					CanClickTag = false
				};
				_batchControl.BatchTagsContainer.Child = child;
			}
			else
			{
				_batchControl.BatchTagsContainer.Child = null;
				_batchControl.BatchTagsContainer.Visibility = Visibility.Collapsed;
				_batchControl.BatchTagsText.Visibility = Visibility.Visible;
			}
			if (tagSelectData?.OmniSelectTags != null)
			{
				_batchControl.BatchTagIcon.Data = Utils.GetGeometry((tagSelectData.OmniSelectTags.Count > 1) ? "Ic.Tag.AllTag" : "Ic.ProjectList.Tag");
			}
		}
	}

	private void SetBatchProjectName(string projectName = null)
	{
		if (_batchControl != null)
		{
			if (string.IsNullOrEmpty(projectName))
			{
				_batchControl.BatchTaskMoveProjectButton.Title = Utils.GetString("MoveTo");
				_batchControl.BatchTaskMoveProjectButton.ItemSelected = false;
			}
			else
			{
				_batchControl.BatchTaskMoveProjectButton.Title = projectName;
				_batchControl.BatchTaskMoveProjectButton.ItemSelected = true;
			}
		}
	}

	private void SetBatchPriorityName(bool hasSamePriority, int priority)
	{
		if (!hasSamePriority || priority == 0)
		{
			_batchControl.PriorityText.Text = Utils.GetString("priority");
			_batchControl.PriorityText.Foreground = ThemeUtil.GetColor("BaseColorOpacity80");
			return;
		}
		string text = Utils.GetString("PriorityNull");
		switch (priority)
		{
		case 1:
			text = Utils.GetString("PriorityLow");
			break;
		case 3:
			text = Utils.GetString("PriorityMedium");
			break;
		case 5:
			text = Utils.GetString("PriorityHigh");
			break;
		}
		_batchControl.PriorityText.Text = text;
		_batchControl.PriorityText.Foreground = ThemeUtil.GetColor("TextAccentColor");
	}

	private void SetBatchSwitchTaskOrNote(bool isTaskToNote)
	{
		if (_batchControl != null)
		{
			if (isTaskToNote)
			{
				_batchControl.SwitchTaskToNoteText.Visibility = Visibility.Visible;
				_batchControl.SwitchNoteToTaskText.Visibility = Visibility.Collapsed;
				_batchControl.SwitchPath.Data = Utils.GetGeometry("Ic.BatchEdit.ConvertToNote");
			}
			else
			{
				_batchControl.SwitchTaskToNoteText.Visibility = Visibility.Collapsed;
				_batchControl.SwitchNoteToTaskText.Visibility = Visibility.Visible;
				_batchControl.SwitchPath.Data = Utils.GetGeometry("Ic.BatchEdit.ConvertToTask");
			}
		}
	}

	private void SetBatchCompleteGrid(SetBatchTaskGridArgs args)
	{
		if (_batchControl == null)
		{
			return;
		}
		_batchControl.BatchCompleteOrUndoneBorder.Visibility = ((!args.HasTask) ? Visibility.Collapsed : Visibility.Visible);
		_batchControl.BatchAbandonBorder.Visibility = ((!args.HasTask || !args.HasUncompleted) ? Visibility.Collapsed : Visibility.Visible);
		if (_task != null)
		{
			if (!args.HasUncompleted)
			{
				_batchControl.BatchTaskUndoneIcon.Visibility = Visibility.Visible;
				_batchControl.BatchTaskDoneIcon.Visibility = Visibility.Collapsed;
				_batchControl.HasAbandonCheck.IsChecked = args.HasAbandoned && !args.HasCompleted;
			}
			else
			{
				_batchControl.BatchTaskUndoneIcon.Visibility = Visibility.Collapsed;
				_batchControl.BatchTaskDoneIcon.Visibility = Visibility.Visible;
				_batchControl.HasAbandonCheck.IsChecked = false;
			}
		}
	}

	private async void BatchSetPriority(int priority)
	{
		await TaskService.BatchSetPriority(_selectedTaskIds, priority);
		_batchArgs.HasSamePriority = true;
		_task.SourceViewModel.Priority = priority;
		SetBatchPriorityName(_batchArgs.HasSamePriority, priority);
	}

	public void BatchOpenSticky()
	{
		TaskStickyWindow.ShowTaskSticky(_selectedTaskIds);
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "open_as_sticky_note");
	}

	public void AddActionEvent(string ctype, string label)
	{
		UserActCollectUtils.AddClickEvent("task_detail", ctype, label);
	}

	public void TryToast(string getString)
	{
		Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, getString);
	}

	public void Dispose()
	{
		_parent = null;
	}

	public void OnCancel()
	{
		_parent?.ClearBatchSelect();
	}

	public void SetUseCardStyle(bool useCardStyle, bool showClose)
	{
		_batchControl?.SetUseCardStyle(useCardStyle, showClose);
	}

	public void TryCloseDetail()
	{
		_parent?.TryExtractDetail();
	}

	internal void DelayOrAdvancedTasks(long l)
	{
		TaskService.DelayOrAdvancedTasks(_selectedTaskIds, l);
		Utils.Toast(Utils.GetString((l > 0) ? "TaskPostponed" : "TaskAdvanced"));
	}

	public int GetCurrentPriority()
	{
		if (!_batchArgs.HasSamePriority)
		{
			return -1;
		}
		return _task?.Priority ?? (-1);
	}

	public SetBatchTaskGridArgs GetBatchArgs()
	{
		return _batchArgs;
	}
}
