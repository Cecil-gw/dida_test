using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.TaskList;

public class BatchTaskEditHelper
{
	private string _projectId;

	public ProjectIdentity ProjectIdentity;

	public bool UseInList = true;

	private int _quadrantLevel;

	private IBatchEditable _editor;

	private bool _canBatchUndo;

	private OperationExtra _args;

	public List<string> SelectedTaskIds { get; set; } = new List<string>();

	public event EventHandler<string> TaskMerged;

	public event EventHandler<bool> ShowOrHideOperation;

	public event EventHandler<bool> CanUndo;

	public BatchTaskEditHelper(IBatchEditable editor, int quadrantLevel = 0)
	{
		_quadrantLevel = quadrantLevel;
		_editor = editor;
	}

	public async void ShowOperationDialog()
	{
		_args = await GetBatchArgs(SelectedTaskIds, ProjectIdentity?.IsNote ?? false, UseInList);
		if (_args != null)
		{
			_projectId = _args.ProjectId;
			TaskOperationDialog dialog = new TaskOperationDialog(_args, _editor?.BatchOperaPlacementTarget() ?? (_editor as UIElement));
			dialog.TimeClear += delegate
			{
				_canBatchUndo = true;
				BatchClearDate();
				dialog.Dismiss();
			};
			dialog.TimeSelect += delegate(object arg, TimeData data)
			{
				_canBatchUndo = true;
				BatchSetDate(data);
				dialog.Dismiss();
			};
			dialog.QuickDateSelect += delegate(object arg, DateTime date)
			{
				_canBatchUndo = true;
				BatchSetStartDate(date);
				dialog.Dismiss();
			};
			dialog.PrioritySelect += BatchSetPriority;
			dialog.AbandonOrReopen += OnBatchCompleted;
			dialog.BatchAbandoned += OnBatchAbandoned;
			dialog.Deleted += BatchDeleteTask;
			dialog.ProjectSelect += BatchSetProject;
			dialog.AssigneeSelect += BatchSetAssignee;
			dialog.Merge += MergeTasks;
			dialog.TagsSelect += BatchSetTags;
			dialog.CompleteDateChanged += BatchSetCompleteDate;
			dialog.SkipCurrentRecurrence += BatchSkipRecurrence;
			dialog.SwitchTaskOrNote += OnSwitchTaskOrNoteClick;
			dialog.OpenSticky += OnOpenSticky;
			dialog.Toast += OnTaskOperationToast;
			dialog.Starred += OnBatchStarred;
			dialog.Copied += OnBatchCopied;
			dialog.TextCopied += OnBatchTextCopied;
			dialog.Closed += delegate
			{
				this.CanUndo?.Invoke(this, _canBatchUndo);
				this.ShowOrHideOperation?.Invoke(this, e: false);
			};
			dialog.Show();
			this.ShowOrHideOperation?.Invoke(this, e: true);
		}
	}

	private void OnBatchCompleted(object sender, EventArgs e)
	{
		TaskService.BatchCompleteTasks(SelectedTaskIds);
	}

	private void OnBatchAbandoned(object sender, EventArgs e)
	{
		AbandonTasksAsync(SelectedTaskIds?.ToList() ?? new List<string>());
	}

	private static async Task AbandonTasksAsync(List<string> taskIds)
	{
		try
		{
			await TaskService.BatchAbandonTasks(taskIds);
		}
		catch (Exception ex)
		{
			UtilLog.Error("BatchTaskEditHelper.BatchAbandonTasks failed: " + ex);
			Utils.Toast(Utils.GetString("OperateFailedMessage"));
		}
	}

	public static async Task<OperationExtra> GetBatchArgs(List<string> selectedTaskIds, bool isNote, bool useInList)
	{
		ObservableCollection<TaskModel> tasks = await TaskDao.GetTasksInTaskIds(selectedTaskIds);
		if (tasks == null || tasks.Count == 0)
		{
			return null;
		}
		DateTime? completeDate = null;
		bool? showCompeteDate = null;
		bool existTask = false;
		bool existNote = false;
		string projectId = null;
		string columnId = null;
		TimeData timeData = new TimeData();
		BatchData batchData = null;
		int priority = -1;
		bool isPinned = true;
		bool canPostPone = false;
		bool canPostPoneTime = false;
		if (tasks.Count > 0)
		{
			batchData = new BatchData();
			TaskModel task = tasks[0];
			if (task != null)
			{
				List<TaskReminderModel> source = await TaskReminderDao.GetRemindersByTaskId(task.id);
				batchData.StartDate = task.startDate;
				batchData.DueDate = task.dueDate;
				batchData.IsAllDay = task.isAllDay;
				batchData.Reminders = source.ToList();
				batchData.RepeatFlag = task.repeatFlag;
				batchData.RepeatFrom = task.repeatFrom;
				batchData.IsFloating = task.Floating;
				batchData.TimeZone = task.timeZone;
				timeData.StartDate = task.startDate;
				timeData.DueDate = task.dueDate;
				timeData.IsAllDay = task.isAllDay;
				timeData.TimeZone = new TimeZoneViewModel(task.Floating, task.timeZone);
				batchData.Assign = task.assignee;
				priority = task.priority;
				columnId = task.columnId;
				showCompeteDate = ((task.kind == "NOTE") ? ((bool?)null) : new bool?(task.status == 2));
				completeDate = ((showCompeteDate == true) ? task.completedTime : ((DateTime?)null));
			}
		}
		foreach (TaskModel task in tasks)
		{
			isPinned = isPinned && task.pinnedTimeStamp > 0;
			if (priority != -1 && task.priority != priority)
			{
				priority = -1;
			}
			if (task.columnId != columnId)
			{
				columnId = "-1";
			}
			if (batchData.Assign != task.assignee)
			{
				batchData.Assign = null;
			}
			if (timeData.StartDate.HasValue && (timeData.StartDate != task.startDate || timeData.DueDate != task.dueDate || (timeData.IsAllDay != task.isAllDay && task.Floating == timeData.TimeZone.IsFloat && task.timeZone == timeData.TimeZone.TimeZoneName)))
			{
				timeData.StartDate = null;
				timeData.DueDate = null;
				timeData.IsAllDay = true;
				timeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
			}
			if (task.startDate.HasValue)
			{
				canPostPone = true;
				if (!(task.isAllDay ?? true))
				{
					canPostPoneTime = true;
				}
			}
			if (batchData != null)
			{
				if (batchData.IsDateUnified && !DateUtils.IsSameDate(task.startDate, batchData.StartDate))
				{
					batchData.IsDateUnified = false;
				}
				if (batchData.IsTimeUnified && (((!DateUtils.IsSameTime(task.startDate, batchData.StartDate) || task.dueDate.HasValue || batchData.DueDate.HasValue) && (!(batchData.StartDate == task.startDate) || !(batchData.DueDate == task.dueDate))) || batchData.IsAllDay != task.isAllDay || task.Floating != batchData.IsFloating || !(task.timeZone == batchData.TimeZone)))
				{
					batchData.IsTimeUnified = false;
				}
				if (batchData.IsReminderUnified && !TaskReminderDao.IsEquals(await TaskReminderDao.GetRemindersByTaskId(task.id), batchData.Reminders))
				{
					batchData.IsReminderUnified = false;
				}
				if (batchData.IsRepeatUnified && (!(task.repeatFlag == batchData.RepeatFlag) || !(task.repeatFrom == batchData.RepeatFrom)))
				{
					batchData.IsRepeatUnified = false;
				}
			}
			if (projectId == null || projectId == task.projectId)
			{
				projectId = task.projectId;
			}
			else
			{
				projectId = "";
			}
			showCompeteDate = ((!showCompeteDate.HasValue && task.kind == "NOTE") ? ((bool?)null) : new bool?((!showCompeteDate.HasValue || showCompeteDate.Value) && task.status == 2));
			if (showCompeteDate.HasValue && showCompeteDate.Value && (!completeDate.HasValue || (task.completedTime.HasValue && task.completedTime > completeDate)))
			{
				completeDate = task.completedTime;
			}
			else if (!showCompeteDate.HasValue || (!showCompeteDate.Value && task.kind != "NOTE"))
			{
				completeDate = null;
			}
			if (!existTask && task.kind != "NOTE")
			{
				existTask = true;
			}
			if (!existNote && task.kind == "NOTE")
			{
				existNote = true;
			}
		}
		timeData.BatchData = batchData;
		timeData.IsDefault = batchData != null && !batchData.StartDate.HasValue && batchData.IsUnified;
		TaskType taskType = (existNote ? ((!existTask) ? TaskType.Note : TaskType.TaskAndNote) : TaskType.Task);
		List<TaskModel> source2 = tasks.Where((TaskModel taskModel) => taskModel.kind != "NOTE").ToList();
		bool showSkip = TaskService.IsAllRepeatTask(selectedTaskIds);
		bool canSwitch = taskType == TaskType.Note || (taskType == TaskType.Task && !TaskCache.ExistParentTask(selectedTaskIds) && tasks.All((TaskModel taskModel) => taskModel.status == 0 && string.IsNullOrEmpty(taskModel.parentId)));
		bool showAssignTo = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId)?.IsShareList() ?? false;
		return new OperationExtra
		{
			InBatch = true,
			HasUncompletedTask = source2.Any((TaskModel taskModel) => taskModel.status == 0),
			HasCompletedTask = source2.Any((TaskModel taskModel) => taskModel.status == 2),
			HasAbandonedTask = source2.Any((TaskModel taskModel) => taskModel.status == -1),
			BatchTaskIds = selectedTaskIds,
			Priority = priority,
			ProjectId = projectId,
			ColumnId = columnId,
			TimeModel = timeData,
			Tags = GetTagSelectData(tasks),
			ShowCopy = true,
			ShowCopyLink = false,
			ShowMerge = useInList,
			ShowAssignTo = showAssignTo,
			ShowSkip = showSkip,
			CompleteTime = completeDate,
			TaskType = taskType,
			CanSwitch = canSwitch,
			ShowSwitch = true,
			ShowLinkParent = (taskType == TaskType.Task),
			Assignee = batchData.Assign,
			FailedSwitchTips = ((taskType == TaskType.TaskAndNote) ? Utils.GetString("CannotConvertMixedToNote") : Utils.GetString("CannotConvertMultiLevelToNote")),
			IsPinned = (useInList ? new bool?(isPinned) : ((bool?)null)),
			ShowCopyText = true,
			CanPostpone = canPostPone,
			CanPostponeTime = canPostPoneTime,
			InNoteProject = isNote
		};
	}

	private void OnOpenSticky(object sender, EventArgs e)
	{
		TaskStickyWindow.ShowTaskSticky(SelectedTaskIds.ToList());
	}

	private void OnTaskOperationToast(object sender, string e)
	{
		Utils.Toast(e);
	}

	private void OnBatchStarred(object sender, bool e)
	{
		TaskService.BatchStarTaskOrNote(new List<string>(SelectedTaskIds), ProjectIdentity?.CatId, e);
	}

	private async void OnBatchTextCopied(object sender, EventArgs e)
	{
		List<TaskModel> list = await TaskDao.GetThinTasksInBatch(SelectedTaskIds);
		if (list == null || list.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (TaskModel item in list)
		{
			stringBuilder.Append(string.IsNullOrEmpty(item.title) ? Utils.GetString("NoTitle") : item.title);
			if (!Utils.IsEmptyDate(item.startDate))
			{
				bool flag = item.isAllDay ?? true;
				stringBuilder.Append("(" + DateUtils.FormatDateStringNoWord(item.startDate.Value, item.dueDate?.AddDays(flag ? (-1) : 0), flag) + ")");
			}
			stringBuilder.Append(Environment.NewLine);
		}
		stringBuilder.Remove(stringBuilder.Length - Environment.NewLine.Length, Environment.NewLine.Length);
		try
		{
			Clipboard.SetDataObject(stringBuilder.ToString(), copy: true);
			Utils.Toast(Utils.GetString("CopyTextTips"));
		}
		catch (Exception)
		{
		}
	}

	private async void OnBatchCopied(object sender, EventArgs e)
	{
		List<string> ids = new List<string>(SelectedTaskIds);
		if (!(await ProChecker.CheckTaskLimit(_projectId, ids.Count)))
		{
			await TaskService.CopyTasks(ids, App.Window);
		}
	}

	private async void OnSwitchTaskOrNoteClick(object sender, EventArgs e)
	{
		List<string> ids = new List<string>(SelectedTaskIds);
		_canBatchUndo = true;
		UtilLog.Info("BatchSwitchTaskNote " + SelectedTaskIds.Join(";"));
		await TaskService.BatchSwitchTaskOrNote(ids);
		SyncManager.TryDelaySync();
		_canBatchUndo = true;
	}

	private async void BatchSetCompleteDate(object sender, DateTime date)
	{
		await TaskService.BatchSetCompleteDate(SelectedTaskIds.ToList(), date);
		_editor?.ReloadList();
		SyncManager.TryDelaySync();
	}

	private async void BatchSkipRecurrence(object sender, EventArgs e)
	{
		await TaskService.BatchSkipRecurrence(SelectedTaskIds.ToList());
	}

	private async void BatchSetTags(object sender, TagSelectData data)
	{
		_canBatchUndo = true;
		await TaskService.BatchSetTags(SelectedTaskIds.ToList(), data);
	}

	private async void MergeTasks(object sender, EventArgs e)
	{
		string e2 = await MergeTasks();
		this.TaskMerged?.Invoke(this, e2);
	}

	private async Task<string> MergeTasks()
	{
		List<string> selectIds = SelectedTaskIds.ToList();
		bool flag = selectIds.Count > 0;
		TaskModel taskModel = default(TaskModel);
		if (flag)
		{
			taskModel = await TaskService.TryMergeTask(selectIds, selectIds[0], ProjectIdentity);
			flag = taskModel != null;
		}
		if (flag)
		{
			List<string> list = new List<string>(selectIds);
			UtilLog.Info("MergeTasks newTask " + taskModel.id);
			list.Add(taskModel.id);
			TaskChangeNotifier.NotifyTasksMerged(list);
			return taskModel.id;
		}
		return null;
	}

	private async void BatchSetAssignee(object sender, AvatarInfo assignee)
	{
		if (!string.IsNullOrEmpty(assignee.UserId) && assignee.UserId != "-1")
		{
			List<string> list = TaskCache.GetTasksByIds(SelectedTaskIds.ToList())?.Select((TaskBaseViewModel m) => m.ProjectId).Distinct().ToList();
			if (list != null && list.Count > 0)
			{
				foreach (string item in list)
				{
					List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(item);
					if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != assignee.UserId))
					{
						Utils.Toast(Utils.GetString("ChangeAssigneeError"));
						return;
					}
				}
			}
		}
		_canBatchUndo = true;
		await TaskService.BatchSetAssignee(SelectedTaskIds.ToList(), assignee.UserId);
	}

	private async void BatchSetProject(object sender, SelectableItemViewModel e)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string item = projectAndColumnId.Item1;
		string columnId = projectAndColumnId.Item2;
		ProjectModel project = CacheManager.GetProjectById(item);
		if (project == null)
		{
			return;
		}
		List<TaskBaseViewModel> taskAndChildrenInBatch = TaskCache.GetTaskAndChildrenInBatch(SelectedTaskIds.ToList());
		List<string> ids = taskAndChildrenInBatch.Select((TaskBaseViewModel t) => t.Id).ToList();
		if (_args?.ProjectId != item || (!string.IsNullOrEmpty(_args?.ColumnId) && !(_args?.ColumnId == columnId)))
		{
			await TaskDao.RemoveTaskParentIdInBatch((from node in TaskNodeUtils.GetTaskNodeTree(taskAndChildrenInBatch)
				where !node.HasParent
				select node.Value.Id).ToList());
			await TaskService.BatchMoveProject(ids, new MoveProjectArgs(project)
			{
				ColumnId = columnId
			});
			TaskModel taskModel = await TaskDao.GetThinTaskById(ids.FirstOrDefault());
			if (_quadrantLevel == 0)
			{
				TaskService.TryToastMoveControl(ProjectIdentity, taskModel, project.id, hideTitle: true);
			}
			else
			{
				string item2 = (await MatrixManager.GetTaskQuadrantChangeString(_quadrantLevel, taskModel.id)).Item2;
				if (!string.IsNullOrEmpty(item2))
				{
					Utils.Toast(item2);
				}
			}
		}
		if (!string.IsNullOrEmpty(columnId) && _args?.ColumnId != columnId)
		{
			await TaskService.BatchSetColumn(ids, columnId);
			DataChangedNotifier.NotifyColumnChanged(project.id);
		}
	}

	private async void BatchSetStartDate(DateTime date)
	{
		await TaskService.BatchSetStartDate(SelectedTaskIds.ToList(), date);
	}

	private async void BatchSetDate(TimeData timeData)
	{
		await TaskService.BatchSetDate(SelectedTaskIds.ToList(), timeData);
	}

	private async void BatchDeleteTask(object sender, EventArgs e)
	{
		await TaskService.BatchDeleteTaskByIds(SelectedTaskIds.ToList());
	}

	private static TagSelectData GetTagSelectData(ObservableCollection<TaskModel> tasks)
	{
		List<List<string>> list = new List<List<string>>();
		if (tasks != null && tasks.Any())
		{
			list.AddRange(from TaskModel task in tasks
				select (!string.IsNullOrEmpty(task.tag)) ? TagSerializer.ToTags(task.tag).ToList() : new List<string>());
		}
		return TagDataHelper.GetSelectTagData(list);
	}

	private async void BatchClearDate()
	{
		await TaskService.BatchClearDate(SelectedTaskIds.ToList());
	}

	private async void BatchSetPriority(object sender, int priority)
	{
		_canBatchUndo = true;
		await TaskService.BatchSetPriority(SelectedTaskIds.ToList(), priority);
	}

	public void ClearSelectedTaskIds()
	{
		SelectedTaskIds = new List<string>();
	}

	public void Dispose()
	{
		this.ShowOrHideOperation = null;
		this.CanUndo = null;
	}
}
