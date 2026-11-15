using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Misc;

public class BatchTaskControl : UserControl, IComponentConnector
{
	private OperationExtra _args;

	private List<string> _taskIds;

	private int _quadrantLevel;

	private ProjectIdentity _projectIdentity;

	private bool _isNote;

	private bool _useInList;

	private object _mouseDownItem;

	internal HoverIconButton SetDateButton;

	internal HoverIconButton DelayButton;

	internal HoverIconButton MovetoButton;

	internal HoverIconButton SetPriorityButton;

	internal HoverIconButton SetTagButton;

	internal HoverIconButton MoreButton;

	private bool _contentLoaded;

	public bool PopupShowing { get; set; }

	public event EventHandler BatchOperated;

	public BatchTaskControl()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		};
		base.Unloaded += delegate
		{
			TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
			this.BatchOperated = null;
		};
	}

	private async void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (e.BatchChangedIds.Any() || e.DeletedChangedIds.Any() || e.KindChangedIds.Any() || e.DateChangedIds.Any() || e.ProjectChangedIds.Any() || e.TagChangedIds.Any() || e.PriorityChangedIds.Any())
		{
			SetTaskIds(_taskIds, _isNote, _useInList, _projectIdentity);
		}
	}

	public async void SetTaskIds(List<string> taskIds, bool isNote, bool useInList, ProjectIdentity projectIdentity = null)
	{
		base.Dispatcher.Invoke((Func<Task>)async delegate
		{
			_projectIdentity = projectIdentity;
			_taskIds = taskIds?.ToList();
			_isNote = isNote;
			_useInList = useInList;
			OperationExtra operationExtra = await BatchTaskEditHelper.GetBatchArgs(taskIds, isNote, useInList);
			if (operationExtra == null)
			{
				ExitBatchSelect();
			}
			else
			{
				SetArgs(operationExtra);
				SetHighlight();
			}
		});
	}

	private void SetArgs(OperationExtra args)
	{
		_args = args;
		DelayButton.Visibility = ((!args.CanPostpone) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void ExitBatchSelect()
	{
		Utils.FindParent<IBatchTaskSelector>(this)?.CancelBatchSelect();
	}

	private void SetHighlight()
	{
		SetPriorityButton.Visibility = ((_args.TaskType != TaskType.Task) ? Visibility.Collapsed : Visibility.Visible);
		SetIconHighLight(SetDateButton, _args.TimeModel.StartDate.HasValue);
		SetIconHighLight(MovetoButton, !string.IsNullOrEmpty(_args.ProjectId));
		SetIconHighLight(SetTagButton, _args.Tags.OmniSelectTags.Any());
		SetPriorityImage(_args.Priority);
		SetTagIconImage(_args.Tags);
	}

	private void SetTagIconImage(TagSelectData argsTags)
	{
		SetTagButton.IconData = Utils.GetGeometry((argsTags.OmniSelectTags.Count > 1) ? "Ic.Tag.AllTag" : "Ic.ProjectList.Tag");
	}

	private void SetPriorityImage(int priority)
	{
		string text = "BaseColorOpacity100";
		SetPriorityButton.SetIconColor(priority switch
		{
			1 => "PriorityLowColor", 
			3 => "PriorityMiddleColor", 
			5 => "PriorityHighColor", 
			_ => "BaseColorOpacity100", 
		});
	}

	private void SetIconHighLight(HoverIconButton button, bool highLight)
	{
		if (highLight)
		{
			button.SetIconColor("TextAccentColor");
			button.ImageOpacity = 1.0;
		}
		else
		{
			button.SetIconColor("BaseColorOpacity100");
		}
	}

	private async void OnSetDateClick(object sender, MouseButtonEventArgs e)
	{
		if (!object.Equals(_mouseDownItem, sender))
		{
			return;
		}
		_mouseDownItem = null;
		OperationExtra operationExtra = await BatchTaskEditHelper.GetBatchArgs(_taskIds, _isNote, _useInList);
		if (operationExtra == null)
		{
			ExitBatchSelect();
			return;
		}
		SetArgs(operationExtra);
		bool isNote = operationExtra.TaskType == TaskType.Note || (operationExtra.TaskType == TaskType.TaskAndNote && operationExtra.InNoteProject);
		SetDateDialog dia = SetDateDialog.GetDialog();
		dia.ClearEventHandle();
		dia.Clear += delegate
		{
			dia.TryClose();
			OnTimeClear();
			PopupShowing = false;
		};
		dia.Save += delegate(object o, TimeData data)
		{
			dia.TryClose();
			OnTimeSelected(data);
			PopupShowing = false;
		};
		dia.Hided += delegate
		{
			PopupShowing = false;
		};
		dia.Show(operationExtra.TimeModel, new SetDateDialogArgs(calendarMode: false, isNote, itemMode: false, SetDateButton, -126.0, -4.0, PlacementMode.Top, showQuickDate: false));
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "date_other");
		PopupShowing = true;
	}

	private async void OnTimeClear()
	{
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		if (calendarControl != null)
		{
			calendarControl.BatchClearDate();
		}
		else
		{
			await TaskService.BatchClearDate(_taskIds);
		}
	}

	private async void OnTimeSelected(TimeData data)
	{
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		if (calendarControl != null)
		{
			calendarControl.BatchSetTimeData(data);
		}
		else
		{
			await TaskService.BatchSetDate(_taskIds, data);
		}
	}

	private async void OnDelayClick(object sender, MouseButtonEventArgs e)
	{
		if (!object.Equals(_mouseDownItem, sender))
		{
			return;
		}
		_mouseDownItem = null;
		OperationExtra operationExtra = await BatchTaskEditHelper.GetBatchArgs(_taskIds, _isNote, _useInList);
		if (operationExtra != null)
		{
			SetArgs(operationExtra);
		}
		EscPopup escPopup = new EscPopup
		{
			PlacementTarget = DelayButton,
			Placement = PlacementMode.Top,
			StaysOpen = false,
			HorizontalOffset = -76.0,
			VerticalOffset = 0.0
		};
		SetPopupEvent(escPopup);
		DelayOrAdvanceDialog delayOrAdvanceDialog = new DelayOrAdvanceDialog();
		delayOrAdvanceDialog.DelayOrAdvanced += async delegate(object o, long l)
		{
			CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
			if (calendarControl != null)
			{
				calendarControl.OnBatchDelay(l);
			}
			else
			{
				await TaskService.DelayOrAdvancedTasks(_taskIds, l);
				Utils.Toast(Utils.GetString((l > 0) ? "TaskPostponed" : "TaskAdvanced"));
			}
		};
		delayOrAdvanceDialog.ShowDialog(escPopup, placeTop: true, _args.CanPostponeTime);
	}

	private async void OnMoveToClick(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(_mouseDownItem, sender))
		{
			_mouseDownItem = null;
			EscPopup escPopup = new EscPopup
			{
				PlacementTarget = MovetoButton,
				Placement = PlacementMode.Top,
				StaysOpen = false,
				HorizontalOffset = -94.0
			};
			SetPopupEvent(escPopup);
			ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(escPopup, new ProjectExtra
			{
				ProjectIds = new List<string> { _args.ProjectId }
			}, new ProjectSelectorExtra
			{
				showAll = false,
				batchMode = false,
				canSelectGroup = false,
				onlyShowPermission = true,
				CanSearch = true,
				ShowColumn = true,
				ColumnId = _args.ColumnId
			});
			projectOrGroupPopup.ItemSelect += delegate(object o, SelectableItemViewModel arg)
			{
				OnMoveProject(arg);
			};
			projectOrGroupPopup.Show();
		}
	}

	private void OnMoveProject(SelectableItemViewModel model)
	{
		if (model != null)
		{
			OnMoveProjectSelect(model);
		}
	}

	private async void OnMoveProjectSelect(SelectableItemViewModel model)
	{
		(string, string, string) projectAndColumnId = model.GetProjectAndColumnId();
		string item = projectAndColumnId.Item1;
		string columnId = projectAndColumnId.Item2;
		ProjectModel project = CacheManager.GetProjectById(item);
		if (project == null)
		{
			return;
		}
		List<TaskBaseViewModel> taskAndChildrenInBatch = TaskCache.GetTaskAndChildrenInBatch(_taskIds);
		List<string> ids = taskAndChildrenInBatch.Select((TaskBaseViewModel t) => t.Id).ToList();
		if (_args?.ProjectId != item || (!string.IsNullOrEmpty(_args?.ColumnId) && !(_args?.ColumnId == columnId)))
		{
			await TaskDao.RemoveTaskParentIdInBatch((from node in TaskNodeUtils.GetTaskNodeTree(taskAndChildrenInBatch)
				where !node.HasParent
				select node.Value.Id).ToList(), disableNotify: true);
			await TaskService.BatchMoveProject(ids, new MoveProjectArgs(project)
			{
				ColumnId = columnId
			});
			this.BatchOperated?.Invoke(this, null);
			UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "move_project");
			TaskModel taskModel = await TaskDao.GetThinTaskById(ids.FirstOrDefault());
			if (_quadrantLevel == 0)
			{
				if (_projectIdentity != null)
				{
					TaskService.TryToastMoveControl(_projectIdentity, taskModel, project.id, hideTitle: true);
				}
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

	private void OnSetPriorityClick(object sender, MouseButtonEventArgs e)
	{
		EscPopup popup;
		if (object.Equals(_mouseDownItem, sender))
		{
			_mouseDownItem = null;
			popup = new EscPopup
			{
				PlacementTarget = SetPriorityButton,
				Placement = PlacementMode.Top,
				StaysOpen = false,
				HorizontalOffset = -80.0
			};
			SetPopupEvent(popup);
			SetPriorityDialog setPriorityDialog = new SetPriorityDialog(_args.Priority, lineStyle: true);
			popup.Child = setPriorityDialog;
			popup.IsOpen = true;
			setPriorityDialog.PrioritySelect += OnPrioritySelected;
		}
		void OnPrioritySelected(object o, int i)
		{
			popup.IsOpen = false;
			OnTasksPriorityChanged(i);
		}
	}

	private async void OnTasksPriorityChanged(int priority)
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
		await TaskService.BatchSetPriority(_taskIds, priority);
		this.BatchOperated?.Invoke(this, null);
	}

	private void OnSetTagClick(object sender, MouseButtonEventArgs e)
	{
		if (object.Equals(_mouseDownItem, sender))
		{
			_mouseDownItem = null;
			TagSelectData tags = _args.Tags;
			UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "tag");
			EscPopup popup = new EscPopup
			{
				PlacementTarget = SetTagButton,
				Placement = PlacementMode.Top,
				StaysOpen = false,
				HorizontalOffset = -94.0,
				VerticalOffset = 4.0
			};
			SetPopupEvent(popup);
			BatchSetTagControl batchSetTagControl = new BatchSetTagControl();
			batchSetTagControl.Close += delegate
			{
				popup.IsOpen = false;
			};
			batchSetTagControl.TagsSelect += delegate(object s, TagSelectData a)
			{
				OnTagSelected(a);
				popup.IsOpen = false;
			};
			popup.Child = batchSetTagControl;
			batchSetTagControl.Init(tags, firstInit: true);
			popup.IsOpen = true;
		}
	}

	private void SetPopupEvent(Popup popup)
	{
		popup.Opened += delegate
		{
			PopupShowing = true;
		};
		popup.Closed += delegate
		{
			PopupShowing = false;
		};
	}

	private async void OnTagSelected(TagSelectData data)
	{
		await TaskService.BatchSetTags(_taskIds, data);
		this.BatchOperated?.Invoke(this, null);
	}

	private async void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "delete");
		CalendarControl calendarControl = Utils.FindParent<CalendarControl>(this);
		if (calendarControl != null)
		{
			calendarControl.OnBatchDelete();
			return;
		}
		List<TaskModel> tasks = await TaskDao.GetTasksByIds(_taskIds);
		Utils.GetToastWindow()?.BatchDeleteTask(tasks);
	}

	private async void OnMoreClick(object sender, MouseButtonEventArgs ea)
	{
		ea.Handled = true;
		if (!object.Equals(_mouseDownItem, sender))
		{
			return;
		}
		_mouseDownItem = null;
		OperationExtra operationExtra = await BatchTaskEditHelper.GetBatchArgs(_taskIds, _isNote, _useInList);
		if (operationExtra == null)
		{
			ExitBatchSelect();
			return;
		}
		SetArgs(operationExtra);
		if (_projectIdentity != null && _projectIdentity.ViewMode == "timeline")
		{
			operationExtra.ShowMerge = false;
			operationExtra.IsPinned = null;
		}
		EscPopup popup = new EscPopup
		{
			PlacementTarget = MoreButton,
			Placement = PlacementMode.Top,
			StaysOpen = false,
			HorizontalOffset = -94.0
		};
		operationExtra.UsedInBatchControl = true;
		operationExtra.ShowSwitch = false;
		TaskOperationDialog dialog = new TaskOperationDialog(operationExtra, null, popup);
		dialog.AssigneeSelect += delegate(object arg, AvatarInfo e)
		{
			OnAssigneeSelected(e);
			dialog.Dismiss();
		};
		dialog.Merge += async delegate
		{
			TaskModel taskModel = await TaskService.TryMergeTask(_taskIds, _taskIds[0], _projectIdentity);
			if (taskModel != null)
			{
				TaskChangeNotifier.NotifyTasksMerged(new List<string>(_taskIds) { taskModel.id });
			}
			dialog.Dismiss();
		};
		dialog.CompleteDateChanged += async delegate(object arg, DateTime e)
		{
			await TaskService.BatchSetCompleteDate(_taskIds, e);
			this.BatchOperated?.Invoke(this, null);
			dialog.Dismiss();
		};
		dialog.SwitchTaskOrNote += async delegate
		{
			await TaskService.BatchSwitchTaskOrNote(_taskIds);
			this.BatchOperated?.Invoke(this, null);
			dialog.Dismiss();
		};
		dialog.OpenSticky += delegate
		{
			TaskStickyWindow.ShowTaskSticky(_taskIds);
			dialog.Dismiss();
		};
		dialog.Toast += delegate(object arg, string e)
		{
			Utils.Toast(e);
			dialog.Dismiss();
		};
		dialog.Starred += delegate(object arg, bool e)
		{
			TaskService.BatchStarTaskOrNote(_taskIds, _projectIdentity?.CatId, e);
			dialog.Dismiss();
		};
		dialog.AbandonOrReopen += delegate
		{
			TaskService.BatchCompleteTasks(_taskIds);
			dialog.Dismiss();
		};
		dialog.BatchAbandoned += delegate
		{
			List<string> taskIds = _taskIds?.ToList() ?? new List<string>();
			AbandonTasksAsync(taskIds, dialog);
		};
		dialog.Copied += async delegate
		{
			await TaskService.CopyTasks(new List<string>(_taskIds), Window.GetWindow(this));
			dialog.Dismiss();
		};
		dialog.TextCopied += async delegate
		{
			List<TaskModel> list = await TaskDao.GetThinTasksInBatch(_taskIds);
			if (list != null && list.Count != 0)
			{
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
				dialog.Dismiss();
			}
		};
		await Task.Delay(50);
		dialog.Show();
	}

	private async Task AbandonTasksAsync(List<string> taskIds, TaskOperationDialog dialog)
	{
		try
		{
			await TaskService.BatchAbandonTasks(taskIds);
			this.BatchOperated?.Invoke(this, null);
		}
		catch (Exception ex)
		{
			UtilLog.Error("BatchTaskControl.BatchAbandonTasks failed: " + ex);
			Utils.Toast(Utils.GetString("OperateFailedMessage"));
		}
		finally
		{
			dialog.Dismiss();
		}
	}

	private async void OnAssigneeSelected(AvatarInfo assignee)
	{
		if (!string.IsNullOrEmpty(assignee.UserId) && assignee.UserId != "-1")
		{
			List<string> list = TaskCache.GetTasksByIds(_taskIds)?.Select((TaskBaseViewModel m) => m.ProjectId).Distinct().ToList();
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
		await TaskService.BatchSetAssignee(_taskIds, assignee.UserId);
		this.BatchOperated?.Invoke(this, null);
	}

	private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDownItem = sender;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/batchtaskcontrol.xaml", UriKind.Relative);
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
			SetDateButton = (HoverIconButton)target;
			break;
		case 2:
			DelayButton = (HoverIconButton)target;
			break;
		case 3:
			MovetoButton = (HoverIconButton)target;
			break;
		case 4:
			SetPriorityButton = (HoverIconButton)target;
			break;
		case 5:
			SetTagButton = (HoverIconButton)target;
			break;
		case 6:
			MoreButton = (HoverIconButton)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
