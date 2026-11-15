using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Editing;
using Newtonsoft.Json;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Eisenhower;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.TaskList.Item;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Views.MainListView.TaskList;

public class TaskListView : Grid, IBatchAddable, ITaskList, ISectionList
{
	public delegate void SelectDelegate(ListItemSelectModel model);

	private int _caretIndex = -1;

	private bool _splitting;

	private TaskView _taskViewParent;

	private ListView _taskList = new ListView
	{
		Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
	};

	private LoadMoreItemControl _loadMoreItem;

	private bool _isExpandOrFolding;

	private Popup _quickSetPopup;

	private bool _canReload = true;

	private string _uid;

	private bool _canBringIntoView;

	private double _dragStartX;

	private int _dragStartIndex;

	private int _dragCurrentIndex;

	private Popup _taskDragPopup;

	private Point _dragPoint;

	private Point _calDragPoint;

	private DisplayItemModel _dragExtraItem;

	private DisplayItemModel _dragFromOtherItem;

	private int _parentLevel;

	private DisplayItemModel _frontItem;

	private DateTime _autoScrollTime;

	private DragAutoScrollHelper _autoScroller;

	private bool _reloadAfterDrag;

	private bool _canDragSwitch;

	private bool _dragOver;

	private bool? _dragToLeft;

	private int _dragStartLevel;

	private Popup _recordPopup;

	private Popup _checkInPopup;

	internal Func<bool, Task> MatrixLoadRequested { get; set; }

	public TaskListViewModel ViewModel => base.DataContext as TaskListViewModel;

	public ObservableCollection<DisplayItemModel> DisplayModels => ViewModel.Items;

	public int QuadrantLevel { get; set; }

	public string SelectedId
	{
		get
		{
			List<string> list = ViewModel.BatchEditor?.GetSelectedTaskIds();
			if (list == null || list.Count != 1)
			{
				return null;
			}
			return list[0];
		}
		set
		{
			if (!string.IsNullOrEmpty(value))
			{
				ViewModel.SetSelectedTaskIds(new List<string> { value });
			}
		}
	}

	public bool IsLocked { get; set; }

	public bool SectionEditing { get; set; }

	public event EventHandler ItemsCountChanged;

	public event EventHandler MoveUpCaret;

	public event EventHandler NeedReload;

	public event EventHandler<(List<string>, bool)> BatchSelect;

	public event EventHandler<string> NavigateTask;

	public event SelectDelegate ItemSelect;

	public TaskListView()
	{
		base.DataContext = new TaskListViewModel();
		InitListView();
		base.PreviewMouseLeftButtonUp += TaskListMouseUp;
		base.LostFocus += delegate
		{
			_caretIndex = -1;
		};
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			_taskViewParent = null;
			UnbindEvents();
		};
	}

	public void SetIdentity(ProjectIdentity identity, TaskListDisplayType displayType = TaskListDisplayType.Normal)
	{
		ViewModel.SetIdentity(identity, displayType);
	}

	private TaskView GetTaskViewParent()
	{
		if (ViewModel == null || ViewModel.InKanban || ViewModel.InDetail || ViewModel.InMatrix)
		{
			return null;
		}
		if (_taskViewParent == null)
		{
			_taskViewParent = Utils.FindParent<TaskView>(this);
		}
		return _taskViewParent;
	}

	private void InitListView()
	{
		_taskList.SetBinding(ItemsControl.ItemsSourceProperty, "Items");
		_taskList.SetResourceReference(FrameworkElement.StyleProperty, "ListViewStyle");
		_taskList.ItemTemplateSelector = new TaskItemTemplateSelector();
		_taskList.PreviewMouseRightButtonUp += ShowBatchContextMenu;
		_taskList.PreviewKeyDown += OnKeyDown;
		_taskList.PreviewMouseLeftButtonUp += TaskListMouseUp;
		_taskList.FocusVisualStyle = null;
		base.Children.Add(_taskList);
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		if (_taskList.Template.FindName("ScrollViewer", _taskList) is ScrollViewer scrollViewer)
		{
			scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
		}
		if (_taskList.Template.FindName("ItemsPresenter", _taskList) is ItemsPresenter itemsPresenter)
		{
			itemsPresenter.RequestBringIntoView += OnItemBringIntoView;
		}
	}

	private void OnItemBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		if (ViewModel.InMatrix || ViewModel.InKanban)
		{
			if (_canBringIntoView)
			{
				_canBringIntoView = false;
			}
			else
			{
				e.Handled = true;
			}
		}
	}

	private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (ViewModel.AddingTask)
		{
			e.Handled = true;
		}
		else if (base.IsMouseOver && !e.Handled && sender is SmoothScrollViewer { ComputedVerticalScrollBarVisibility: Visibility.Visible } smoothScrollViewer)
		{
			if (Math.Abs(smoothScrollViewer.VerticalOffset - (double)e.Delta - smoothScrollViewer.ScrollableHeight) < 0.05)
			{
				Utils.FindSingleVisualChildren<LoadMoreItemControl>(this)?.OnLoadMore(force: false);
			}
			smoothScrollViewer.HandleSmoothMouseWheel(e);
		}
	}

	private void TaskListMouseUp(object sender, MouseButtonEventArgs e)
	{
		Point position = e.GetPosition(_taskList);
		if (_taskDragPopup != null && _taskDragPopup.IsOpen)
		{
			e.Handled = true;
		}
		bool drop = position.X >= (double)(ViewModel.InDetail ? (-20) : 0) && position.X <= (ViewModel.InDetail ? (base.ActualWidth + 20.0) : base.ActualWidth) && (!ViewModel.InDetail || position.Y > 0.0);
		StopDragging(drop, e);
		if (_taskDragPopup != null)
		{
			_taskDragPopup.IsOpen = false;
		}
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
	}

	private void ShowBatchContextMenu(object sender, MouseButtonEventArgs e)
	{
		if (ViewModel.ProjectIdentity != null && !ViewModel.ProjectIdentity.CanEdit)
		{
			if (ViewModel.ProjectIdentity is TrashProjectIdentity)
			{
				ShowTaskOperationInTrash(e);
			}
			return;
		}
		IBatchEditable batchEditor = ViewModel.BatchEditor;
		if (batchEditor != null && batchEditor.GetSelectedTaskIds().Any() && batchEditor.GetSelectedTaskIds().Count > 1)
		{
			batchEditor.ShowBatchOperationDialog();
			e.Handled = true;
		}
	}

	private void ShowTaskOperationInTrash(MouseButtonEventArgs e)
	{
		List<string> selectedIds = GetSelectedTaskIds();
		if (selectedIds.Count <= 1)
		{
			return;
		}
		bool flag = TaskService.IsTeamTask(selectedIds[0]);
		OperationDialog operationDialog = new OperationDialog(string.Empty, flag ? new List<OperationItemViewModel>
		{
			new OperationItemViewModel(ActionType.Restore)
		} : new List<OperationItemViewModel>
		{
			new OperationItemViewModel(ActionType.Restore),
			new OperationItemViewModel(ActionType.DeleteForever)
		});
		operationDialog.SetPlaceTarget(this);
		operationDialog.Operated += async delegate(object o, KeyValuePair<string, ActionType> kv)
		{
			switch (kv.Value)
			{
			case ActionType.Restore:
				await TaskService.BatchRestoreProject(selectedIds);
				break;
			case ActionType.DeleteForever:
				if (new CustomerDialog(Utils.GetString("DeleteForever"), Utils.GetString("BatchDeleteForeverHint"), Utils.GetString("Delete"), Utils.GetString("Cancel"))
				{
					Owner = Window.GetWindow(this),
					Topmost = false
				}.ShowDialog() == true)
				{
					await TaskService.BatchDeleteTaskByIds(selectedIds, showUndo: false, 2);
				}
				break;
			}
		};
		operationDialog.Show();
		e.Handled = true;
	}

	public async Task Load(bool forceRefresh = false, bool loadMore = true, bool restoreSelected = true, bool ignoreFocus = false, bool setSelect = true, bool scroll = false, bool refreshData = true)
	{
		LoadAsync(forceRefresh, loadMore, restoreSelected, ignoreFocus, setSelect, scroll, refreshData);
	}

	private void DelayReload()
	{
		if (_uid == null)
		{
			_uid = Utils.GetGuid();
		}
		DelayActionHandlerCenter.TryDoAction(_uid, delegate
		{
			LoadAsync(forceRefresh: false, loadMore: true, restoreSelected: true, ignoreFocus: true);
		}, 200);
	}

	public async Task LoadAsync(bool forceRefresh = false, bool loadMore = true, bool restoreSelected = true, bool ignoreFocus = false, bool setSelect = true, bool scroll = false, bool refreshData = true, bool coordinatedMatrixLoad = false)
	{
		if (coordinatedMatrixLoad || MatrixLoadRequested == null)
		{
			await base.Dispatcher.Invoke((Func<Task>)async delegate
			{
				TaskListViewModel vm = base.DataContext as TaskListViewModel;
				if (_canReload && vm != null && !TickDragHelper.Dragging && TaskViewModelHelper.Loaded)
				{
					if (vm.InKanban)
					{
						vm.LoadSortedItems(loadMore);
					}
					else
					{
						ITaskOperation focusingItem = GetFocusingItem();
						if (focusingItem == null || (!focusingItem.ParsingDate() && !focusingItem.IsNewAdd()))
						{
							if (focusingItem == null)
							{
								_caretIndex = -1;
							}
							else
							{
								scroll = true;
							}
							ignoreFocus = ignoreFocus || vm.InKanban || vm.InMatrix;
							if ((forceRefresh || ignoreFocus || focusingItem == null) && !SectionEditing)
							{
								if (vm.InDetail)
								{
									this.NeedReload?.Invoke(this, null);
								}
								else
								{
									_reloadAfterDrag = false;
									if (!restoreSelected)
									{
										vm.SetSelectedTaskIds(new List<string>());
									}
									bool forceReadOnly = !(GetTaskViewParent()?.CanShowDetail() ?? true);
									await vm.LoadModels(refreshData, loadMore, forceReadOnly);
									this.ItemsCountChanged?.Invoke(this, null);
									RestoreSelected(restoreSelected);
									ResetDrag();
									if (setSelect)
									{
										List<string> selectedTaskIds = vm.SelectedTaskIds;
										if (selectedTaskIds != null && selectedTaskIds.Count == 1 && scroll)
										{
											TryFocusAndSelectModelById(vm.SelectedTaskIds[0]);
										}
									}
									if (!vm.InMatrix)
									{
										UpdateLayout();
									}
									if (!restoreSelected)
									{
										ScrollToTop();
									}
								}
							}
						}
					}
				}
			});
		}
		else
		{
			await MatrixLoadRequested(restoreSelected);
		}
	}

	private void RestoreSelected(bool restore = false)
	{
		if (!(base.DataContext is TaskListViewModel taskListViewModel))
		{
			return;
		}
		if (!restore)
		{
			taskListViewModel.InBatchSelect = false;
			taskListViewModel.SetSelectedTaskIds(new List<string>());
			this.BatchSelect?.Invoke(this, (taskListViewModel.SelectedTaskIds, true));
			return;
		}
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel item in taskListViewModel.Items.ToList())
		{
			if (item.Selected && !item.IsSection)
			{
				list.Add(item);
			}
			if (item.IsOpen)
			{
				continue;
			}
			foreach (DisplayItemModel childrenModel in item.GetChildrenModels(getAll: true))
			{
				if (childrenModel.Selected)
				{
					list.Add(childrenModel);
				}
			}
		}
		if (list.Count == 0)
		{
			List<string> selectedTaskIds = taskListViewModel.SelectedTaskIds;
			if (selectedTaskIds != null && selectedTaskIds.Count > 0)
			{
				NotifyMultipleSelected();
			}
			return;
		}
		if (list.Count > 1 || taskListViewModel.InBatchSelect)
		{
			taskListViewModel.InBatchSelect = true;
			list.ForEach(delegate(DisplayItemModel item)
			{
				item.InBatchSelected = item.IsSection || item.Selected;
			});
		}
		List<string> list2 = list.Select((DisplayItemModel t) => t.Id).ToList();
		bool flag = taskListViewModel.SelectedTaskIds == null || list2.Count != taskListViewModel.SelectedTaskIds.Count || list2.Union(taskListViewModel.SelectedTaskIds).Count() != taskListViewModel.SelectedTaskIds.Count;
		taskListViewModel.SetSelectedTaskIds(list2);
		if (!taskListViewModel.InKanban)
		{
			DisplayItemModel displayItemModel = null;
			for (int num = 0; num < taskListViewModel.Items.Count; num++)
			{
				DisplayItemModel displayItemModel2 = ViewModel.Items[num];
				if (displayItemModel2.Selected)
				{
					if (displayItemModel == null)
					{
						displayItemModel = displayItemModel2;
						displayItemModel2.ShowTopMargin = true;
					}
					else
					{
						displayItemModel2.ShowTopMargin = false;
						displayItemModel.ShowBottomMargin = false;
						displayItemModel = displayItemModel2;
					}
				}
				else if (displayItemModel != null)
				{
					displayItemModel.ShowBottomMargin = true;
					displayItemModel = null;
				}
			}
			if (displayItemModel != null)
			{
				displayItemModel.ShowBottomMargin = true;
			}
		}
		if (flag)
		{
			if (taskListViewModel.InBatchSelect)
			{
				this.BatchSelect?.Invoke(this, (taskListViewModel.SelectedTaskIds, false));
			}
			else if (list2.Count > 0)
			{
				NotifyItemSelect(new ListItemSelectModel(list2[0], null, DisplayType.Task, TaskSelectType.Click));
			}
		}
	}

	private void NotifyMultipleSelected(bool forceShowBatch = true)
	{
		if (base.DataContext is TaskListViewModel taskListViewModel && taskListViewModel.OnBatchSelectChanged())
		{
			this.BatchSelect?.Invoke(this, (taskListViewModel.SelectedTaskIds, forceShowBatch));
		}
	}

	public async void SetItemSelected(List<DisplayItemModel> items, string itemId, bool scroll = true)
	{
		if (items != null)
		{
			DisplayItemModel displayItemModel = null;
			for (int i = 0; i < items.Count; i++)
			{
				DisplayItemModel displayItemModel2 = items[i];
				if (displayItemModel2.Selected)
				{
					displayItemModel2.Selected = false;
				}
				if (displayItemModel2.Id == itemId)
				{
					displayItemModel2.Selected = true;
					if (i > 0)
					{
						items[i - 1].LineVisible = false;
					}
					displayItemModel = displayItemModel2;
				}
			}
			if (displayItemModel != null)
			{
				ViewModel.SetSelectedTaskIds(new List<string> { displayItemModel.Id });
				if (scroll)
				{
					TryFocusAndSelectModelById(displayItemModel.Id);
				}
			}
		}
		SelectedId = itemId;
	}

	public async void ToggleSelectedItemCompleted()
	{
		List<string> selectedTaskIds = GetSelectedTaskIds();
		if (selectedTaskIds.Count > 0)
		{
			await TaskService.BatchCompleteTasks(selectedTaskIds);
			LoadAsync(forceRefresh: true);
			return;
		}
		ListViewItem listViewItem = (ListViewItem)_taskList.ItemContainerGenerator.ContainerFromItem(GetSelectedModel());
		if (listViewItem != null)
		{
			Utils.FindSingleVisualChildren<TaskListItem>(listViewItem)?.ToggleTaskCompleted();
		}
	}

	private ITaskOperation GetFocusingItem()
	{
		ITaskOperation focusedItemInWindow = GetFocusedItemInWindow();
		TaskListView taskListView = focusedItemInWindow?.GetParentList();
		if (taskListView == null || !taskListView.Equals(this))
		{
			return null;
		}
		return focusedItemInWindow;
	}

	private ITaskOperation GetFocusedItemInWindow()
	{
		Window window = Window.GetWindow(this);
		if (window == null || !window.IsActive)
		{
			return null;
		}
		IInputElement focusedElement = FocusManager.GetFocusedElement(window);
		if (!(focusedElement is ITaskOperation result))
		{
			if (focusedElement is TextArea child)
			{
				ITaskOperation taskOperation = Utils.FindParent<ITaskOperation>(child);
				if (taskOperation != null)
				{
					return taskOperation;
				}
			}
			return null;
		}
		return result;
	}

	public void ScrollToTop()
	{
		if (_taskList.Template.FindName("ScrollViewer", _taskList) is ScrollViewer { VerticalOffset: not 0.0 } scrollViewer)
		{
			scrollViewer.ScrollToVerticalOffset(0.0);
		}
	}

	public bool CanBatchAdd()
	{
		if (base.DataContext is TaskListViewModel taskListViewModel)
		{
			if (!(taskListViewModel.ProjectIdentity is AssignToMeProjectIdentity))
			{
				return !(taskListViewModel.ProjectIdentity is BindAccountCalendarProjectIdentity);
			}
			return false;
		}
		return false;
	}

	public bool Editable()
	{
		if (base.DataContext is TaskListViewModel taskListViewModel)
		{
			return taskListViewModel.ProjectIdentity.CanEdit;
		}
		return false;
	}

	public bool Copyable()
	{
		return !(ViewModel.ProjectIdentity is AssignToMeProjectIdentity);
	}

	public void OnDetailTaskNavigated(string taskId)
	{
		SetItemSelected(DisplayModels.ToList(), taskId);
	}

	public void SelectTask(string taskId, TaskSelectType type, bool ignoreBatch = false)
	{
		if (TaskDragHelpModel.DragHelp.IsDragging)
		{
			return;
		}
		if (type == TaskSelectType.Click)
		{
			DisplayItemModel displayItemModel = ViewModel.Items.FirstOrDefault((DisplayItemModel item) => item.Id == taskId);
			if (displayItemModel != null && displayItemModel.IsTaskOrNote && !ignoreBatch)
			{
				if (Utils.IfCtrlPressed())
				{
					BatchCtrlSelect(taskId);
					return;
				}
				if (Utils.IfShiftPressed())
				{
					BatchShiftSelect(taskId);
					return;
				}
			}
			SelectItem(new ListItemSelectModel(taskId, string.Empty, DisplayType.Task, type));
			ViewModel.SetSelectedTaskIds(GetSelectedTaskIds());
		}
		else
		{
			SelectItem(taskId, type);
			ScrollToItemById(taskId);
		}
	}

	public async void TrySetTitleReadonly(string taskId)
	{
		DisplayItemModel selected = ViewModel.Items.FirstOrDefault((DisplayItemModel item) => item.Id == taskId);
		if (selected == null)
		{
			return;
		}
		switch (selected.Type)
		{
		case DisplayType.CheckItem:
		case DisplayType.Agenda:
		{
			AgendaHelper.IAgenda agenda = selected;
			if (selected.Type == DisplayType.CheckItem)
			{
				agenda = await TaskDao.GetThinTaskById(selected.TaskId);
				if (agenda.GetAttendId() == null)
				{
					break;
				}
			}
			if (!AgendaHelper.CanAccessAgenda(agenda))
			{
				TaskTitleBox taskTitleTextBox2 = GetTaskTitleTextBox(selected);
				if (taskTitleTextBox2 != null)
				{
					taskTitleTextBox2.IsReadOnly = true;
					GetToastParent()?.TryToastString(null, Utils.GetString((selected.Type == DisplayType.CheckItem) ? "OnlyOwnerCanChangeSubtask" : "AttendeeModifyContent"));
				}
			}
			break;
		}
		case DisplayType.Countdown:
		{
			TaskTitleBox taskTitleTextBox3 = GetTaskTitleTextBox(selected);
			if (taskTitleTextBox3 != null)
			{
				taskTitleTextBox3.IsReadOnly = true;
			}
			break;
		}
		case DisplayType.Event:
		{
			TaskTitleBox taskTitleTextBox = GetTaskTitleTextBox(selected);
			if (taskTitleTextBox == null)
			{
				break;
			}
			taskTitleTextBox.IsReadOnly = true;
			if (UserDao.IsUserValid())
			{
				string calendarId = selected.CalendarId;
				BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calendarId);
				if (bindCalendarModel != null && bindCalendarModel.AvailableType == CalendarAvailableType.Editable)
				{
					taskTitleTextBox.IsReadOnly = false;
				}
			}
			else
			{
				GetToastParent()?.TryToastString(null, Utils.GetString("CannotEditGoogleEvents"));
			}
			break;
		}
		}
	}

	public async void CopyTask(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel == null)
		{
			return;
		}
		TaskDetailPopup.TrySetDetailEditing(editing: true, 100);
		try
		{
			if (!(await ProChecker.CheckTaskLimit(taskModel.GetProjectId())))
			{
				TaskModel taskModel2 = await TaskService.CopyTask(taskId, this);
				if (taskModel2 != null)
				{
					NotifyItemSelect(new ListItemSelectModel(taskModel2.id, null, DisplayType.Task, TaskSelectType.Navigate));
				}
			}
		}
		finally
		{
			TaskDetailPopup.TrySetDetailEditing(editing: false);
		}
	}

	public void DeleteTask(string taskId, TaskDeleteType type)
	{
		UtilLog.Info($"TaskList.Delete id {taskId} type {type}");
		TryDeleteTask(taskId, type);
	}

	private async Task TryDeleteTask(string taskId, TaskDeleteType type)
	{
		DisplayItemModel item = DisplayModels.FirstOrDefault((DisplayItemModel p) => p.Id == taskId);
		if (item != null)
		{
			List<TaskModel> subTasks = await TaskService.GetAllSubTasksByIdAsync(taskId, item.ProjectId);
			if (type == TaskDeleteType.DeleteButton)
			{
				TaskModel task = await TaskDao.GetThinTaskById(taskId);
				if (task == null || !task.CheckEnable())
				{
					return;
				}
				if (!string.IsNullOrEmpty(task?.attendId))
				{
					if (await TaskOperationHelper.CheckIfAllowDeleteAgenda(task, this))
					{
						if (ViewModel.InDetail && !string.IsNullOrEmpty(task.repeatFlag) && task.status == 0 && (Utils.FindParent<TaskDetailPopup>(this)?.ShowInCal ?? false))
						{
							ModifyRepeatHandler.TryDeleteRecurrence(task.id, task.startDate, task.dueDate, GetToastParent());
							return;
						}
						await DeleteAgenda(task);
					}
				}
				else
				{
					if (ViewModel.InDetail && !string.IsNullOrEmpty(task.repeatFlag) && task.status == 0 && (Utils.FindParent<TaskDetailPopup>(this)?.ShowInCal ?? false))
					{
						ModifyRepeatHandler.TryDeleteRecurrence(task.id, task.startDate, task.dueDate, GetToastParent());
						return;
					}
					if (subTasks != null && subTasks.Count > 0)
					{
						subTasks.Add(task);
						if (!(await GetToastParent().BatchDeleteTask(subTasks)))
						{
							return;
						}
					}
					else
					{
						GetToastParent()?.TaskDeleted(taskId);
					}
				}
				item.SourceViewModel.Deleted = 1;
				RemoveItemById(taskId);
			}
			else if (await TaskService.InEmptyTask(taskId) && (subTasks == null || subTasks.Count == 0))
			{
				bool focusFirst = IsFirstTaskInList(taskId);
				if (type == TaskDeleteType.RemoveText && !focusFirst)
				{
					MoveUp(taskId);
				}
				item.SourceViewModel.Deleted = 1;
				RemoveItemById(taskId);
				await TaskService.DeleteTask(taskId, 2);
				if (focusFirst && type == TaskDeleteType.RemoveText)
				{
					TryFocusFirstItem();
				}
			}
		}
		if (ViewModel.InDetail && DisplayModels.Count == 0)
		{
			await Task.Delay(100);
			this.NeedReload?.Invoke(this, null);
		}
	}

	public void TryFocusItemById(string id)
	{
		DisplayItemModel displayItemModel = ViewModel.Items.FirstOrDefault((DisplayItemModel i) => i.Id == id);
		if (displayItemModel != null)
		{
			TryFocusItem(displayItemModel);
		}
	}

	public async void TryFocusItem(DisplayItemModel displayItem)
	{
		FocusTitle(displayItem, 100);
		SelectTaskOrItem(displayItem);
		displayItem.Selected = true;
	}

	public void TryFocusFirstItem()
	{
		foreach (DisplayItemModel displayModel in DisplayModels)
		{
			if (displayModel.IsTaskOrNote || displayModel.IsItem)
			{
				SelectTaskOrItem(displayModel);
				FocusItemTitle(displayModel);
				break;
			}
		}
	}

	private bool IsFirstTaskInList(string taskId)
	{
		switch (GetCurrentIndex(taskId))
		{
		case 0:
			return true;
		case 1:
			if (DisplayModels[0].IsSection)
			{
				return true;
			}
			break;
		}
		return false;
	}

	private async Task DeleteAgenda(TaskModel task)
	{
		await TaskService.DeleteAgenda(task.id, task.projectId, task.attendId);
	}

	public async void DeleteSelectedTasks()
	{
		List<string> selectedTaskIds = GetSelectedTaskIds();
		UtilLog.Info("TaskList.DeleteTasks from:shortCut");
		if (ViewModel.ProjectIdentity is TrashProjectIdentity)
		{
			if (selectedTaskIds != null && selectedTaskIds.Count > 0)
			{
				await TaskService.BatchDeleteTaskByIds(selectedTaskIds, showUndo: false, 2);
			}
		}
		else if (selectedTaskIds != null && selectedTaskIds.Count > 1)
		{
			await TaskService.BatchDeleteTaskByIds(selectedTaskIds);
			NotifyBatchDeleted();
		}
		else if (selectedTaskIds != null && selectedTaskIds.Count == 1)
		{
			TryDeleteTask(selectedTaskIds[0], TaskDeleteType.DeleteButton);
		}
	}

	private void NotifyBatchDeleted()
	{
		ViewModel.ExitBatchSelect();
		LoadAsync(forceRefresh: true);
	}

	public async void CompleteCheckitem(string itemId, bool playSound = true)
	{
		DisplayItemModel displayItemModel = DisplayModels.FirstOrDefault((DisplayItemModel t) => t.Id == itemId);
		if (displayItemModel != null && playSound && displayItemModel.Status != 1)
		{
			Utils.PlayCompletionSound();
		}
		await TaskDetailItemService.CompleteCheckItem(itemId, handleTask: true, completeOnly: false, needUndo: true, GetToastParent());
	}

	public async Task TaskTitleChanged(string taskId, string text)
	{
		await TaskService.SaveTaskTitle(taskId, text);
		if (ViewModel.ProjectIdentity.SortOption.orderByType == Constants.SortType.modifiedTime)
		{
			DisplayItemModel displayItemModel = DisplayModels.FirstOrDefault((DisplayItemModel t) => t.Id == taskId);
			if (displayItemModel != null)
			{
				CheckSortByModifiedTime(displayItemModel);
			}
		}
	}

	public void EventTitleChanged(string eventId, string text)
	{
		eventId = eventId?.Split('_').FirstOrDefault();
		if (!string.IsNullOrEmpty(eventId))
		{
			CalendarService.SaveEventTitle(eventId, text);
		}
	}

	public async void SubtaskTitleChanged(string taskId, string checkItemId, string text)
	{
		await TaskService.SetItemTitle(taskId, checkItemId, text);
	}

	private void MoveDown(string taskId)
	{
		MoveDown(taskId, string.Empty);
	}

	private void MoveUp(string taskId)
	{
		MoveUp(taskId, string.Empty);
	}

	public async void MoveUp(string taskId, string itemId, bool fromDetail = false)
	{
		DisplayItemModel lastTask = GetLastDisplayModel((!string.IsNullOrEmpty(itemId)) ? itemId : taskId);
		if (lastTask != null)
		{
			_canBringIntoView = true;
			SelectTaskOrItem(lastTask);
			if (!fromDetail)
			{
				FocusItemTitle(lastTask);
				return;
			}
			await Task.Delay(40);
			Utils.FindSingleVisualChildren<TaskListItem>(GetItemByModel(lastTask))?.ShowDetailWindow(focus: false, fromUpDown: true);
		}
		else if (!fromDetail)
		{
			this.MoveUpCaret?.Invoke(this, null);
		}
	}

	private DisplayItemModel GetLastDisplayModel(string id)
	{
		int currentIndex = GetCurrentIndex(id);
		if (currentIndex > 0 && currentIndex < ViewModel.Items.Count)
		{
			for (int num = currentIndex - 1; num >= 0; num--)
			{
				DisplayItemModel displayItemModel = ViewModel.Items[num];
				if (!displayItemModel.IsSection && !displayItemModel.IsLoadMore)
				{
					return displayItemModel;
				}
			}
		}
		return null;
	}

	public async void MoveDown(string taskId, string itemId, bool fromDetail = false)
	{
		DisplayItemModel nextTask = GetNextDisplayModel((!string.IsNullOrEmpty(itemId)) ? itemId : taskId);
		if (nextTask != null)
		{
			_canBringIntoView = true;
			SelectTaskOrItem(nextTask);
			if (!fromDetail)
			{
				FocusItemTitle(nextTask);
				return;
			}
			await Task.Delay(20);
			Utils.FindSingleVisualChildren<TaskListItem>(GetItemByModel(nextTask))?.ShowDetailWindow(focus: false, fromUpDown: true);
		}
	}

	private DisplayItemModel GetNextDisplayModel(string id)
	{
		int currentIndex = GetCurrentIndex(id);
		if (currentIndex >= 0 && currentIndex < ViewModel.Items.Count)
		{
			for (int i = currentIndex + 1; i < ViewModel.Items.Count; i++)
			{
				DisplayItemModel displayItemModel = ViewModel.Items[i];
				if (!displayItemModel.IsSection && !displayItemModel.IsLoadMore)
				{
					return displayItemModel;
				}
			}
		}
		return null;
	}

	private DisplayItemModel GetModelById(string id)
	{
		return DisplayModels.FirstOrDefault((DisplayItemModel item) => item.Id == id);
	}

	public void TryFocusSelectedItem()
	{
		DisplayItemModel selectedModel = GetSelectedModel();
		if (selectedModel != null)
		{
			FocusItemTitle(selectedModel);
		}
	}

	private void TryFocusAndSelectModelById(string id)
	{
		DisplayItemModel displayItemModel = ViewModel.Items.FirstOrDefault((DisplayItemModel t) => t.Id == id);
		if (displayItemModel != null)
		{
			FocusItemTitle(displayItemModel);
		}
	}

	public ListViewItem GetItemByModel(DisplayItemModel model)
	{
		return _taskList.ItemContainerGenerator.ContainerFromItem(model) as ListViewItem;
	}

	private TaskTitleBox GetTaskTitleTextBox(DisplayItemModel model)
	{
		return Utils.GetDescendantByType((ListViewItem)_taskList.ItemContainerGenerator.ContainerFromItem(model), typeof(TaskTitleBox), "EditBox") as TaskTitleBox;
	}

	private async void FocusItemTitle(DisplayItemModel model)
	{
		_taskList.ScrollIntoView(model);
		TaskTitleBox titleTextbox = GetTaskTitleTextBox(model);
		if (titleTextbox != null && !titleTextbox.KeyboardFocused)
		{
			titleTextbox.Select(titleTextbox.Text.Length, 0);
			titleTextbox.Focus();
			await Task.Delay(10);
			titleTextbox.CaretOffset = ((_caretIndex >= titleTextbox.Text.Length || _caretIndex < 0) ? titleTextbox.Text.Length : _caretIndex);
			titleTextbox.ScrollToHorizontalOffset(10000.0);
		}
	}

	public async Task SplitDisplayItem(string id)
	{
		UtilLog.Info("TaskListView.SplitDisplayItem " + id + $", splitting: {_splitting}");
		if (_splitting)
		{
			return;
		}
		_splitting = true;
		int currentIndex = GetCurrentIndex(id);
		DisplayItemModel model = GetModelById(id);
		if (model != null && TaskListItemFocusHelper.FocusingItem != null && TaskListItemFocusHelper.FocusingItem.TitleTextBox.ParsingDate)
		{
			TaskListItemFocusHelper.FocusingItem.RemoveFocus();
			await Task.Delay(300);
			TryFocusAndSelectModelById(model.Id);
			_splitting = false;
			return;
		}
		if (model != null && !model.IsNote)
		{
			if (ViewModel.ProjectIdentity is BindAccountCalendarProjectIdentity)
			{
				await SplitNewEvent(id, model, currentIndex);
			}
			else if (!Utils.IfShiftPressed() || !model.IsTask || string.IsNullOrEmpty(model.Id) || TaskDao.GetTaskLevel(model.Id, model.ProjectId) >= 4)
			{
				await SplitNewTask(id, model, currentIndex);
			}
			else
			{
				await CreateSubTask(model);
			}
		}
		_splitting = false;
	}

	private async Task SplitNewEvent(string eventId, DisplayItemModel model, int index)
	{
		string text = ((model.CalendarType == 5) ? Guid.NewGuid().ToString() : Utils.GetGuid());
		string id = text + "@" + model.CalendarId;
		TaskBaseViewModel taskBaseViewModel = new TaskBaseViewModel
		{
			Type = DisplayType.Event,
			Id = id,
			IsAllDay = true,
			Kind = "TEXT",
			Title = string.Empty,
			ProjectName = string.Empty,
			CreatedTime = DateTime.Now,
			ModifiedTime = DateTime.Now
		};
		Section section = GetSection(eventId);
		if (!section.GetStartDate().HasValue)
		{
			return;
		}
		string calendarId = model.CalendarId;
		BindCalendarModel calendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calendarId);
		if (calendar != null)
		{
			BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel acc) => acc.Id == calendar.AccountId);
			if (bindCalendarAccountModel != null && bindCalendarAccountModel.Calendars.Any())
			{
				calendarId = bindCalendarAccountModel.Calendars[0].Id;
			}
		}
		DateTime? startDate = section.GetStartDate();
		if (startDate.HasValue)
		{
			double num = DateTimeOffset.Now.Offset.TotalMinutes;
			if (num < 0.0)
			{
				num = 0.0;
			}
			taskBaseViewModel.DueDate = (taskBaseViewModel.StartDate = startDate.Value.Date.AddMinutes(num)).Value.AddDays(1.0);
		}
		taskBaseViewModel.CalendarId = calendarId;
		taskBaseViewModel.ProjectName = string.Empty;
		DisplayItemModel displayItemModel = new DisplayItemModel(taskBaseViewModel)
		{
			ShowReminder = true
		};
		ViewModel.Items.Insert(index + 1, displayItemModel);
		ViewModel.SourceModels.Add(model.SourceViewModel);
		FocusTitle(displayItemModel, 100);
		SelectTaskOrItem(displayItemModel);
		displayItemModel.Selected = true;
		CalendarEventModel calendarEventModel = new CalendarEventModel
		{
			Id = id,
			UserId = Utils.GetCurrentUserIdInt().ToString(),
			EventId = text,
			Uid = text,
			Title = string.Empty,
			CalendarId = taskBaseViewModel.CalendarId,
			Type = 0,
			DueStart = taskBaseViewModel.StartDate,
			DueEnd = taskBaseViewModel.DueDate,
			IsAllDay = true
		};
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		if (!string.IsNullOrEmpty(defaultSafely.AllDayReminders))
		{
			calendarEventModel.Reminders = JsonConvert.SerializeObject(defaultSafely.AllDayReminders.Split(',').Select(TriggerUtils.TriggerToReminder));
		}
		await CalendarService.AddEvent(calendarEventModel);
	}

	private async Task SplitNewTask(string taskId, DisplayItemModel task, int index)
	{
		bool flag = string.IsNullOrEmpty(task.Title);
		if (flag)
		{
			flag = await TaskService.InEmptyTask(taskId);
		}
		if (flag)
		{
			return;
		}
		ProjectIdentity projectIdentity = ViewModel.ProjectIdentity;
		if (projectIdentity is CompletedProjectIdentity || projectIdentity is AbandonedProjectIdentity || projectIdentity is TrashProjectIdentity || projectIdentity is AssignToMeProjectIdentity || projectIdentity is SearchProjectIdentity)
		{
			return;
		}
		UserActCollectUtils.AddShortCutEvent("task", "add_task_below");
		TaskBaseViewModel newTask = new TaskBaseViewModel
		{
			Type = DisplayType.Task,
			Id = Utils.GetGuid(),
			IsAllDay = task.IsAllDay,
			Kind = "TEXT",
			Title = string.Empty,
			ColumnId = task.ColumnId,
			PinnedTime = (task.IsPinned ? Utils.GetNowTimeStamp() : 0)
		};
		Section section = task.Section;
		string projectId = section?.GetProjectId();
		if ((!string.IsNullOrEmpty(task.ParentId) && task.Level > 0) || ViewModel.SortOption.groupByType == Constants.SortType.assignee)
		{
			projectId = task.ProjectId;
		}
		else
		{
			projectIdentity = ViewModel.ProjectIdentity;
			if (!(projectIdentity is FilterProjectIdentity))
			{
				if (!(projectIdentity is NormalProjectIdentity))
				{
					if (!(projectIdentity is GroupProjectIdentity groupProjectIdentity))
					{
						if (projectIdentity is ParentTaskIdentity)
						{
							projectId = task.ProjectId;
						}
					}
					else if (string.IsNullOrEmpty(projectId))
					{
						projectId = groupProjectIdentity.GetProjectId();
					}
				}
				else
				{
					projectId = ViewModel.ProjectIdentity.Id;
				}
			}
			else
			{
				projectId = task.ProjectId;
			}
			if (string.IsNullOrEmpty(projectId))
			{
				projectId = TaskDefaultDao.GetDefaultSafely().ProjectId;
			}
		}
		ProjectModel project = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (project == null || await ProChecker.CheckTaskLimit(projectId))
		{
			return;
		}
		int num = 0;
		FilterTaskDefault filterTaskDefault = ((num > 0) ? FilterViewModel.CalculateTaskDefault(MatrixManager.GetQuadrantByLevel(num).rule) : null);
		DateTime? dateTime = section?.GetStartDate();
		if (!dateTime.HasValue)
		{
			if (filterTaskDefault != null && filterTaskDefault.DefaultDate.HasValue)
			{
				dateTime = filterTaskDefault.DefaultDate;
			}
			else
			{
				TimeData timeData = ViewModel.ProjectIdentity.GetTimeData();
				if (timeData != null && (!timeData.IsDefault || !task.IsNote))
				{
					dateTime = timeData.StartDate;
				}
			}
		}
		dateTime = dateTime ?? (task.IsNote ? ((DateTime?)null) : TaskDefaultDao.GetDefaultSafely().GetDefaultDateTime());
		if (Utils.IsEmptyDate(dateTime))
		{
			dateTime = null;
		}
		newTask.StartDate = dateTime;
		newTask.IsAllDay = true;
		newTask.DueDate = null;
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		if (ViewModel.ProjectIdentity is FilterProjectIdentity filterProjectIdentity)
		{
			newTask.SetTags(filterProjectIdentity.GetTags());
			newTask.Priority = filterProjectIdentity.GetPriority();
		}
		else if (ViewModel.ProjectIdentity is MatrixQuadrantIdentity matrixQuadrantIdentity)
		{
			newTask.SetTags(matrixQuadrantIdentity.GetTags());
			newTask.Priority = matrixQuadrantIdentity.GetPriority();
		}
		else if (ViewModel.ProjectIdentity is TagProjectIdentity tagProjectIdentity)
		{
			newTask.SetTags(tagProjectIdentity.GetTags());
		}
		else if (filterTaskDefault != null)
		{
			newTask.SetTags(filterTaskDefault.DefaultTags);
			newTask.Priority = filterTaskDefault.Priority ?? defaultSafely.Priority;
		}
		else
		{
			newTask.Priority = defaultSafely.Priority;
		}
		if (!(section is TagSection tagSection))
		{
			if (section is NoPrioritySection || section is LowPrioritySection || section is MediumPrioritySection || section is HighPrioritySection)
			{
				newTask.Priority = section.GetPriority();
			}
		}
		else
		{
			newTask.SetTags((tagSection.Name == Utils.GetString("NoTags")) ? null : new List<string> { tagSection.Name });
		}
		if (task.IsNote)
		{
			newTask.Kind = "NOTE";
			newTask.Priority = 0;
		}
		newTask.ProjectId = ((!string.IsNullOrEmpty(task.ParentId)) ? task.ProjectId : project.id);
		newTask.ProjectName = project.name;
		newTask.ProjectOrder = project.sortOrder;
		newTask.Assignee = GetDefaultAssignee(section?.GetAssignee(), ViewModel.ProjectIdentity?.GetAssignee());
		newTask.Color = project.color;
		string parentId = ((task.Level == 0 && !ViewModel.InDetail) ? "" : task.ParentId);
		newTask.ParentId = "";
		newTask.Status = section?.GetTaskStatus() ?? 0;
		long specialSortOrder = task.SpecialOrder;
		newTask.SortOrder = ProjectSortOrderDao.GetNextTaskSortOrderInProject(task.ProjectId, task.SortOrder, task.ParentId);
		if (newTask.IsPinned)
		{
			string groupId = newTask.ProjectId;
			if (ViewModel.ProjectIdentity != null)
			{
				groupId = ViewModel.ProjectIdentity.CatId;
			}
			await TaskSortOrderService.InsertAfterAsync("taskPinned", groupId, newTask.Id, 1, task.ColumnId, task.TaskId);
		}
		else if (ViewModel.ProjectIdentity.SortOption.orderByType != Constants.SortType.sortOrder)
		{
			string currentProject = ViewModel.ProjectIdentity.GetSortProjectId();
			string sortTypeKey = ViewModel.ProjectIdentity.SortOption.GetSortKey();
			if (!string.IsNullOrEmpty(sortTypeKey) && !string.IsNullOrEmpty(section?.SectionEntityId))
			{
				sortTypeKey = string.Format(sortTypeKey, section.SectionEntityId);
			}
			if (!string.IsNullOrEmpty(section?.SectionEntityId))
			{
				long specialOrder = task.SpecialOrder;
				if (specialOrder == long.MaxValue || specialOrder == long.MinValue)
				{
					await TaskSortOrderService.BatchResetOrders(GetSiblings(task.Section), sortTypeKey, currentProject);
					specialOrder = task.SpecialOrder;
				}
				if (specialOrder != long.MaxValue && specialOrder != long.MinValue)
				{
					DisplayItemModel displayItemModel = ((DisplayModels.Count > index + 1) ? DisplayModels[index + 1] : null);
					long num2 = ((displayItemModel != null && !displayItemModel.IsSection) ? displayItemModel.SpecialOrder : long.MaxValue);
					specialSortOrder = ((num2 != long.MaxValue && num2 != long.MinValue) ? (specialOrder + (num2 - specialOrder) / 2) : (specialOrder + 268435456));
					await TaskSortOrderService.InsertOrUpdateAsync(sortTypeKey, currentProject, newTask.Id, 1, specialSortOrder);
				}
			}
		}
		if (!string.IsNullOrEmpty(parentId))
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(parentId);
			if (taskModel != null && taskModel.projectId == newTask.ProjectId)
			{
				newTask.ParentId = taskModel.id;
				newTask.Status = taskModel.status;
			}
		}
		if (newTask.Status != 0)
		{
			newTask.CompletedTime = DateTime.Now;
			newTask.CompletedUser = LocalSettings.Settings.LoginUserId;
		}
		TaskCache.AddToDict(newTask);
		DisplayItemModel displayModel = new DisplayItemModel(newTask, !(ViewModel.ProjectIdentity is NormalProjectIdentity) && !(ViewModel.ProjectIdentity is ParentTaskIdentity))
		{
			Level = task.Level,
			InDetail = task.InDetail,
			InSticky = task.InSticky,
			ShowDragBar = task.ShowDragBar,
			Section = section,
			IsNewAdd = true,
			SpecialOrder = specialSortOrder
		};
		TaskModel taskModel2 = displayModel.GetTaskModel();
		taskModel2.createdTime = DateTime.Now;
		taskModel2.modifiedTime = DateTime.Now;
		int num3;
		for (num3 = index + 1; num3 < ViewModel.Items.Count && ViewModel.Items[num3].Level > task.Level; num3++)
		{
		}
		ViewModel.Items.Insert(num3, displayModel);
		ViewModel.SourceModels.Add(displayModel.SourceViewModel);
		this.ItemsCountChanged?.Invoke(this, null);
		task.Parent?.AddChild(displayModel, task);
		displayModel.Parent = task.Parent;
		if (section != null)
		{
			DisplayItemModel lastItem = CollectionUtils.GetLastItem(ViewModel.Items, index, (DisplayItemModel item) => item.IsSection && item.Section.SectionId == section.SectionId);
			if (lastItem != null)
			{
				lastItem.Num++;
			}
		}
		SelectedId = displayModel.Id;
		displayModel.Selected = true;
		task.Selected = false;
		UtilLog.Info("TaskList.AddTask " + taskModel2.id + " from:splitNewTask");
		await TaskService.AddTask(taskModel2, setSortOrder: true, notify: true, this);
		await TryAddTaskOrderBySortType(task, displayModel);
		TaskListItemFocusHelper.ParseDateId = displayModel.Id;
		NotifyItemSelect(new ListItemSelectModel(taskModel2.id, null, DisplayType.Task, TaskSelectType.Click));
		UpdateLayout();
		TryFocusAndSelectModelById(displayModel.Id);
		if (taskModel2.startDate.HasValue && !task.IsNote)
		{
			List<TaskReminderModel> defaultAllDayReminders = TimeData.GetDefaultAllDayReminders();
			foreach (TaskReminderModel item in defaultAllDayReminders)
			{
				item.taskserverid = taskModel2.id;
				await TaskReminderDao.SaveReminders(item);
				TaskItemLoadHelper.LoadShowReminder(displayModel);
			}
		}
		DisplayItemModel displayItemModel2 = displayModel;
		displayItemModel2.AvatarUrl = await AvatarHelper.GetAvatarUrl(section?.GetAssignee(), newTask.ProjectId);
	}

	public async Task CreateSubTask(DisplayItemModel model, bool addToLastOne = false)
	{
		ProjectIdentity identity = ViewModel.ProjectIdentity;
		if (identity is CompletedProjectIdentity || identity is AbandonedProjectIdentity || identity is TrashProjectIdentity || identity is AssignToMeProjectIdentity || identity is SearchProjectIdentity)
		{
			return;
		}
		if (!model.IsOpen)
		{
			await OnTaskOpenClick(model);
		}
		if (await ProChecker.CheckTaskLimit(model.ProjectId))
		{
			return;
		}
		int index = GetCurrentIndex(model.TaskId);
		if (addToLastOne)
		{
			for (index++; index < DisplayModels.Count && DisplayModels[index].Level > model.Level; index++)
			{
			}
		}
		UserActCollectUtils.AddShortCutEvent("task", "add_subtask");
		DisplayItemModel task = GetModelById(model.TaskId);
		TaskBaseViewModel newTask = new TaskBaseViewModel
		{
			Type = DisplayType.Task,
			Id = Utils.GetGuid(),
			IsAllDay = task.IsAllDay,
			Kind = "TEXT",
			Title = string.Empty,
			ParentId = model.TaskId,
			ProjectId = model.ProjectId,
			ProjectName = model.ProjectName,
			ProjectOrder = model.ProjectOrder,
			Color = model.Color,
			SortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(model.ProjectId, task.Id, !addToLastOne),
			Status = model.Status,
			ColumnId = model.ColumnId,
			PinnedTime = (model.IsPinned ? Utils.GetNowTimeStamp() : 0),
			CreatedTime = DateTime.Now,
			ModifiedTime = DateTime.Now
		};
		Section section = model.Section;
		int num = 0;
		FilterTaskDefault filterTaskDefault = ((num > 0) ? FilterViewModel.CalculateTaskDefault(MatrixManager.GetQuadrantByLevel(num).rule) : null);
		DateTime? startDate = (section?.GetStartDate() ?? ((filterTaskDefault != null) ? filterTaskDefault.DefaultDate : identity.GetTimeData()?.StartDate)) ?? TaskDefaultDao.GetDefaultSafely().GetDefaultDateTime();
		newTask.StartDate = startDate;
		newTask.IsAllDay = true;
		newTask.DueDate = null;
		if (identity is FilterProjectIdentity filterProjectIdentity)
		{
			newTask.SetTags(filterProjectIdentity.GetTags());
			newTask.Priority = filterProjectIdentity.GetPriority();
		}
		else if (identity is MatrixQuadrantIdentity matrixQuadrantIdentity)
		{
			newTask.SetTags(matrixQuadrantIdentity.GetTags());
			newTask.Priority = matrixQuadrantIdentity.GetPriority();
		}
		else if (identity is TagProjectIdentity tagProjectIdentity)
		{
			newTask.SetTags(tagProjectIdentity.GetTags());
		}
		else if (filterTaskDefault != null)
		{
			newTask.SetTags(filterTaskDefault.DefaultTags);
			newTask.Priority = filterTaskDefault.Priority ?? TaskDefaultDao.GetDefaultSafely().Priority;
		}
		else
		{
			newTask.Priority = TaskDefaultDao.GetDefaultSafely().Priority;
		}
		if (!(section is TagSection tagSection))
		{
			if (section is NoPrioritySection || section is LowPrioritySection || section is MediumPrioritySection || section is HighPrioritySection)
			{
				newTask.Priority = section.GetPriority();
			}
		}
		else
		{
			newTask.SetTags((tagSection.Name == Utils.GetString("NoTags")) ? null : new List<string> { tagSection.Name });
		}
		if (string.IsNullOrEmpty(newTask.Tag) && identity is TagProjectIdentity tagProjectIdentity2)
		{
			newTask.SetTags(new List<string> { tagProjectIdentity2.Tag });
		}
		newTask.Assignee = GetDefaultAssignee(section?.GetAssignee(), identity?.GetAssignee());
		newTask.Status = model.Status;
		if (newTask.Status != 0)
		{
			newTask.CompletedTime = DateTime.Now;
			newTask.CompletedUser = LocalSettings.Settings.LoginUserId;
		}
		if (ViewModel.InDetail)
		{
			TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
			newTask.StartDate = defaultSafely.GetDefaultDateTime();
			newTask.Priority = defaultSafely.Priority;
		}
		TaskCache.AddToDict(newTask);
		DisplayItemModel displayModel = new DisplayItemModel(newTask, !(identity is NormalProjectIdentity) && !(identity is ParentTaskIdentity))
		{
			Section = section,
			Level = task.Level + 1,
			InDetail = task.InDetail,
			InSticky = task.InSticky,
			ShowDragBar = task.ShowDragBar,
			InMatrix = ViewModel.InMatrix,
			InKanban = ViewModel.InKanban,
			ShowProject = task.ShowProject
		};
		task.AddChild(displayModel, null);
		if (ViewModel.InKanban)
		{
			model.ShowBottomMargin = false;
			displayModel.ShowTopMargin = false;
		}
		TaskModel taskModel = displayModel.GetTaskModel();
		taskModel.createdTime = DateTime.Now;
		taskModel.modifiedTime = DateTime.Now;
		UtilLog.Info("TaskList.AddTask " + taskModel.id + " from:" + (addToLastOne ? "CreatSubTask" : "SplitSubTask"));
		await TaskService.AddTask(taskModel, setSortOrder: true, notify: true, this);
		TaskListItemFocusHelper.ParseDateId = displayModel.Id;
		NotifyItemSelect(new ListItemSelectModel(taskModel.id, null, DisplayType.Task, TaskSelectType.Click));
		if (section != null)
		{
			DisplayItemModel lastItem = CollectionUtils.GetLastItem(DisplayModels, index, (DisplayItemModel item) => item.IsSection && item.Section == section);
			if (lastItem != null)
			{
				lastItem.Num++;
			}
		}
		displayModel.IsNewAdd = !ViewModel.InKanban;
		DisplayModels.Insert(addToLastOne ? index : (index + 1), displayModel);
		ViewModel.SourceModels.Add(displayModel.SourceViewModel);
		displayModel.Selected = true;
		SelectedId = displayModel.Id;
		task.Selected = false;
		displayModel.Parent = task;
		UpdateLayout();
		this.ItemsCountChanged?.Invoke(this, null);
		if (ViewModel.InKanban)
		{
			Utils.FindParent<KanbanColumnView>(this)?.TryShowTaskDetail(taskModel.id, focusTitle: true);
		}
		else if (ViewModel.InMatrix)
		{
			Utils.FindSingleVisualChildren<TaskListItem>((ListViewItem)_taskList.ItemContainerGenerator.ContainerFromItem(displayModel))?.ShowDetailWindow(focus: true);
		}
		else if (!model.TitleHitVisible)
		{
			Utils.FindSingleVisualChildren<TaskListItem>((ListViewItem)_taskList.ItemContainerGenerator.ContainerFromItem(displayModel))?.ShowDetailWindow(focus: true);
		}
		else
		{
			TryFocusAndSelectModelById(displayModel.Id);
		}
		if (taskModel.startDate.HasValue)
		{
			List<TaskReminderModel> defaultAllDayReminders = TimeData.GetDefaultAllDayReminders();
			foreach (TaskReminderModel item in defaultAllDayReminders)
			{
				item.taskserverid = taskModel.id;
				await TaskReminderDao.SaveReminders(item);
				TaskItemLoadHelper.LoadShowReminder(displayModel);
			}
		}
		if (LocalSettings.Settings.ShowDetails)
		{
			model.CompletionRate = TaskCompletionRateDao.GetRateStrByIdInDb(model.TaskId, model.ProjectId);
		}
		DisplayItemModel displayItemModel = displayModel;
		displayItemModel.AvatarUrl = await AvatarHelper.GetAvatarUrl(section?.GetAssignee(), newTask.ProjectId);
	}

	private static string GetDefaultAssignee(string sectionAssignee, string identityAssignee)
	{
		if (!string.IsNullOrEmpty(sectionAssignee))
		{
			return sectionAssignee;
		}
		return identityAssignee;
	}

	private async Task TryAddTaskOrderBySortType(DisplayItemModel frontTask, DisplayItemModel insertTask)
	{
		if (frontTask != null && insertTask != null && ViewModel.ProjectIdentity.SortOption.SpecialSort() && frontTask.SpecialOrder != long.MaxValue && frontTask.SpecialOrder != long.MinValue && insertTask.Level <= 0)
		{
			await TaskSortOrderService.NewTaskAdded(insertTask.Id, ViewModel.ProjectIdentity.CatId, ViewModel.ProjectIdentity.SortOption, frontTask.Id);
		}
	}

	public async void AddNewSection(DisplayItemModel sectionItem, bool below)
	{
		if (sectionItem == null)
		{
			return;
		}
		CustomizedSection customizedSection = new CustomizedSection
		{
			SectionId = Utils.GetGuid(),
			Customized = true,
			Name = "",
			ProjectId = sectionItem.ProjectId
		};
		List<DisplayItemModel> list = DisplayModels?.Where((DisplayItemModel m) => m.IsCustomizedSection).ToList();
		if (list == null || !list.Contains(sectionItem))
		{
			return;
		}
		List<long> list2 = list.Select((DisplayItemModel s) => s.Section.Ordinal).ToList();
		if (list2.Count >= LimitCache.GetLimitByKey(Constants.LimitKind.KanbanNumber))
		{
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("ExceedMaxSectionCountMessage"), MessageBoxButton.OK);
			customerDialog.Owner = Window.GetWindow(this);
			customerDialog.ShowDialog();
			return;
		}
		list2.Sort();
		int num = list2.IndexOf(sectionItem.Section.Ordinal);
		if (below)
		{
			customizedSection.Ordinal = ((num < list2.Count - 1) ? ((list2[num + 1] + sectionItem.Section.Ordinal) / 2) : (sectionItem.Section.Ordinal + 268435456));
		}
		else
		{
			customizedSection.Ordinal = ((num > 0) ? ((list2[num - 1] + sectionItem.Section.Ordinal) / 2) : (sectionItem.Section.Ordinal - 268435456));
		}
		int num2 = DisplayModels.IndexOf(sectionItem);
		DisplayItemModel displayItemModel = DisplayItemModel.BuildSection(customizedSection);
		displayItemModel.NewAdd = true;
		if (below)
		{
			int num3;
			for (num3 = num2 + 1; num3 < DisplayModels.Count && !DisplayModels[num3].IsSection; num3++)
			{
			}
			if (num3 > num2)
			{
				DisplayModels.Insert(Math.Min(num3, DisplayModels.Count), displayItemModel);
			}
		}
		else
		{
			DisplayModels.Insert(num2, displayItemModel);
		}
		_taskList.ScrollIntoView(displayItemModel);
	}

	public async void AddNewSectionAtLast()
	{
		if (!(ViewModel.ProjectIdentity is NormalProjectIdentity { Project: not null } normalProjectIdentity))
		{
			return;
		}
		string projectId = normalProjectIdentity.Project.id;
		List<ColumnModel> list = await ColumnDao.GetColumnsByProjectId(projectId);
		if (list == null || !list.Any())
		{
			ProjectModel projectModel = await ProjectDao.GetProjectById(projectId);
			if (projectModel != null)
			{
				if (projectModel.sync_status != Constants.SyncStatus.SYNC_NEW.ToString())
				{
					projectModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				}
				projectModel.viewMode = "list";
				await ProjectDao.TryUpdateProject(projectModel);
				await ColumnDao.TryInitColumns(projectId);
			}
			list = await ColumnDao.GetColumnsByProjectId(projectId);
		}
		if (list.Count >= LimitCache.GetLimitByKey(Constants.LimitKind.KanbanNumber))
		{
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("ExceedMaxSectionCountMessage"), MessageBoxButton.OK);
			customerDialog.Owner = Window.GetWindow(this);
			customerDialog.ShowDialog();
			return;
		}
		if (DisplayModels.FirstOrDefault((DisplayItemModel m) => m.Section is CustomizedSection) == null && list != null && list.Count > 0)
		{
			CustomizedSection customizedSection = new CustomizedSection(list[0]);
			List<DisplayItemModel> list2 = DisplayModels.Where((DisplayItemModel m) => m.IsTaskOrNote && m.Section == null && m.Level == 0).ToList();
			foreach (DisplayItemModel item2 in list2)
			{
				item2.Section = customizedSection;
			}
			customizedSection.Children = list2;
			DisplayItemModel item = DisplayItemModel.BuildSection(customizedSection);
			DisplayModels.Insert(0, item);
		}
		CustomizedSection obj = new CustomizedSection
		{
			SectionId = Utils.GetGuid(),
			Customized = true,
			Name = "",
			ProjectId = projectId
		};
		long valueOrDefault = (list.Select((ColumnModel c) => c.sortOrder).Max() + 268435456).GetValueOrDefault();
		obj.Ordinal = valueOrDefault;
		DisplayItemModel displayItemModel = DisplayItemModel.BuildSection(obj);
		displayItemModel.NewAdd = true;
		int num;
		for (num = DisplayModels.Count - 1; num >= 0; num--)
		{
			DisplayItemModel displayItemModel2 = DisplayModels[num];
			if (!(displayItemModel2.Section is CompletedSection) && !(displayItemModel2.Section is NoteSection))
			{
				num++;
				break;
			}
		}
		DisplayModels.Insert(Math.Max(0, Math.Min(DisplayModels.Count, num)), displayItemModel);
		try
		{
			_taskList.ScrollIntoView(displayItemModel);
		}
		catch (Exception)
		{
		}
	}

	public async void AddTaskInSection(DisplayItemModel model)
	{
		if (model?.Section == null)
		{
			return;
		}
		TaskBaseViewModel newTask = new TaskBaseViewModel
		{
			Id = Utils.GetGuid(),
			Kind = (ViewModel.ProjectIdentity.IsNote ? "NOTE" : "TEXT"),
			Title = string.Empty,
			CreatedTime = DateTime.Now,
			ModifiedTime = DateTime.Now
		};
		string projectId = model.ProjectId;
		if (await ProChecker.CheckTaskLimit(projectId))
		{
			return;
		}
		DateTime? startDate = (newTask.IsNote ? ((DateTime?)null) : TaskDefaultDao.GetDefaultSafely().GetDefaultDateTime());
		newTask.StartDate = startDate;
		newTask.IsAllDay = true;
		newTask.DueDate = null;
		newTask.Priority = TaskDefaultDao.GetDefaultSafely().Priority;
		if (CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId) == null)
		{
			return;
		}
		newTask.ProjectId = projectId;
		newTask.ParentId = "";
		newTask.Status = 0;
		newTask.ColumnId = model.Section.SectionId;
		bool top = TaskDefaultDao.GetDefaultSafely().AddTo == 0 || ViewModel.SortOption.orderByType == Constants.SortType.createdTime || ViewModel.SortOption.orderByType == Constants.SortType.modifiedTime;
		long baseOrder = ((!top) ? model.Section.Children?.LastOrDefault() : model.Section.Children?.FirstOrDefault())?.SortOrder ?? 0;
		newTask.SortOrder = ProjectSortOrderDao.GetNextTaskSortOrderInProject(projectId, baseOrder, "", top);
		TaskCache.AddToDict(newTask);
		DisplayItemModel displayModel = new DisplayItemModel(newTask, !(ViewModel.ProjectIdentity is NormalProjectIdentity) && !(ViewModel.ProjectIdentity is ParentTaskIdentity));
		if (ViewModel.ProjectIdentity.SortOption.orderByType != Constants.SortType.sortOrder)
		{
			displayModel.SpecialOrder = (top ? long.MinValue : long.MaxValue);
			string sortProjectId = ViewModel.ProjectIdentity.GetSortProjectId();
			string text = ViewModel.ProjectIdentity.SortOption.GetSortKey();
			if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(newTask.ColumnId))
			{
				text = string.Format(text, newTask.ColumnId);
			}
			if (!string.IsNullOrEmpty(newTask.ColumnId))
			{
				List<DisplayItemModel> siblings = GetSiblings(model.Section);
				long num = ((siblings == null || siblings.Count <= 0) ? long.MinValue : (top ? siblings.Min((DisplayItemModel m) => m.SpecialOrder) : siblings.Max((DisplayItemModel m) => m.SpecialOrder)));
				if (num != long.MaxValue && num != long.MinValue)
				{
					displayModel.SpecialOrder = (top ? (num - 268435456) : (num + 268435456));
					await TaskSortOrderService.InsertOrUpdateAsync(text, sortProjectId, newTask.Id, 1, displayModel.SpecialOrder);
				}
			}
		}
		displayModel.Level = 0;
		displayModel.Section = model.Section;
		displayModel.IsNewAdd = true;
		TaskModel taskModel = displayModel.GetTaskModel();
		taskModel.createdTime = DateTime.Now;
		taskModel.modifiedTime = DateTime.Now;
		int num2 = DisplayModels.IndexOf(model);
		ViewModel.SourceModels.Add(newTask);
		displayModel.Parent = model;
		if (!top)
		{
			int num3;
			for (num3 = num2 + 1; num3 < DisplayModels.Count && !DisplayModels[num3].IsSection; num3++)
			{
			}
			if (num3 > num2)
			{
				DisplayModels.Insert(Math.Min(num3, DisplayModels.Count), displayModel);
				if (model.Section.Children != null)
				{
					model.Section.Children.Add(displayModel);
				}
				else
				{
					model.Section.Children = new List<DisplayItemModel> { displayModel };
				}
			}
		}
		else
		{
			DisplayModels.Insert(num2 + 1, displayModel);
			if (model.Section.Children != null)
			{
				model.Section.Children.Insert(0, displayModel);
			}
			else
			{
				model.Section.Children = new List<DisplayItemModel> { displayModel };
			}
		}
		ViewModel.SourceModels.Add(displayModel.SourceViewModel);
		DisableReloadInSecond();
		await TaskService.AddTask(taskModel, setSortOrder: true, notify: true, this);
		model.Num++;
		SelectedId = displayModel.Id;
		displayModel.Selected = true;
		this.ItemsCountChanged?.Invoke(this, null);
		UpdateLayout();
		UtilLog.Info("TaskList.AddTask " + taskModel.id + " from:AddTaskInSection");
		await Task.Delay(20);
		ScrollToItemById(displayModel.Id);
		ViewModel.ExitBatchSelect(displayModel.Id);
		TryFocusAndSelectModelById(displayModel.Id);
		if (taskModel.startDate.HasValue)
		{
			List<TaskReminderModel> defaultAllDayReminders = TimeData.GetDefaultAllDayReminders();
			foreach (TaskReminderModel item in defaultAllDayReminders)
			{
				item.taskserverid = taskModel.id;
				await TaskReminderDao.SaveReminders(item);
				TaskItemLoadHelper.LoadShowReminder(displayModel);
			}
		}
		if (LocalSettings.Settings.ExtraSettings.TaskDetailLayout == "Popup" && LocalSettings.Settings.ExtraSettings.ListViewColumnStyle != ListViewColumnStyle.ThreeColumn && _taskList.ItemContainerGenerator.ContainerFromItem(displayModel) is ListViewItem parentObj)
		{
			TaskListItem taskListItem = Utils.FindSingleVisualChildren<TaskListItem>(parentObj);
			if (taskListItem != null)
			{
				await taskListItem.ShowItemDetailWindow(displayModel, focusTitle: true);
			}
		}
	}

	private async Task DisableReloadInSecond()
	{
		_canReload = false;
		await Task.Delay(300);
		_canReload = true;
	}

	private async void FocusTitle(DisplayItemModel itemModel, int delay = 0, int caretIndex = -1)
	{
		if (delay > 0)
		{
			await Task.Delay(delay);
			UpdateLayout();
		}
		if (Utils.GetDescendantByType((ListViewItem)_taskList.ItemContainerGenerator.ContainerFromItem(itemModel), typeof(TaskTitleBox), "EditBox") is TaskTitleBox taskTitleBox)
		{
			Utils.Focus(taskTitleBox);
			UpdateLayout();
			taskTitleBox.CaretOffset = ((caretIndex == -1) ? taskTitleBox.Text.Length : Math.Min(caretIndex, taskTitleBox.Text.Length));
			taskTitleBox.ScrollToHorizontalOffset(10000.0);
			UpdateLayout();
		}
	}

	private void SelectTaskOrItem(DisplayItemModel model)
	{
		if (model.IsTaskOrNote)
		{
			SelectTask(model.Id, TaskSelectType.Navigate);
		}
		else if (model.IsItem)
		{
			SelectSubtask(new IdExtra(model.Id, model.TaskId), TaskSelectType.Navigate);
			ScrollToItemById(model.Id);
		}
		else
		{
			SelectItem(model.Id, model.Type);
			ScrollToItemById(model.Id);
		}
	}

	public void SetTitleCaretIndex(int caretIndex)
	{
		_caretIndex = caretIndex;
	}

	public async void MultipleTextPaste(string text)
	{
		DisplayItemModel task = GetModelById(SelectedId);
		if (task == null)
		{
			return;
		}
		Section section = GetSection(SelectedId);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		DateTime? startDate = (section.GetStartDate() ?? ViewModel.ProjectIdentity.GetTimeData()?.StartDate) ?? TaskDefaultDao.GetDefaultSafely().GetDefaultDateTime();
		TimeData timeData = null;
		if (startDate.HasValue)
		{
			timeData = new TimeData
			{
				StartDate = startDate,
				IsAllDay = true,
				DueDate = null,
				Reminders = TimeData.GetDefaultAllDayReminders()
			};
		}
		List<string> titles = text.Trim().Split('\n').ToList();
		if (!(await ProChecker.CheckTaskLimit(task.ProjectId, titles.Count)) && titles.Any())
		{
			task.IsNewAdd = false;
			TaskModel taskModel = await TaskDao.GetTaskById(SelectedId);
			if (string.IsNullOrEmpty(task.Title))
			{
				string title = titles.First().Trim();
				taskModel.title = title;
				TaskService.UpdateTask(taskModel);
				titles.RemoveAt(0);
			}
			List<string> titles2 = titles.ToList();
			string projectId = task.ProjectId;
			TimeData time = timeData;
			int priority = ((!task.IsNote) ? section.GetPriority() : 0);
			List<string> tags = TagSerializer.ToTags(section.GetTag());
			int status = task.Status;
			string assignee = section.GetAssignee();
			TaskListViewModel viewModel = ViewModel;
			string columnId = ((viewModel != null && viewModel.SortOption?.groupByType == Constants.SortType.sortOrder) ? task.ColumnId : "");
			string parentId = task.ParentId;
			bool isNote = task.IsNote;
			bool isPinned = task.IsPinned;
			bool? addTop = false;
			long? targetSortOrder = taskModel.sortOrder;
			TaskChangeNotifier.NotifyTaskBatchAdded((await TaskService.BatchAddTasks(titles2, projectId, time, priority, tags, status, assignee, columnId, parentId, isNote, null, isPinned, addTop, targetSortOrder))?.Select((TaskModel model) => model.id).ToList());
		}
		Load(forceRefresh: true, loadMore: true, restoreSelected: true, ignoreFocus: true);
	}

	public async Task RemoveSection(string modelId)
	{
		RemoveItemById(modelId);
		if (DisplayModels.Count((DisplayItemModel m) => m.IsCustomizedSection) <= 1)
		{
			LoadAsync(forceRefresh: true);
		}
	}

	public void AfterTaskChanged(DisplayItemModel model, bool tryFocused = false)
	{
		if (!tryFocused)
		{
			_caretIndex = -1;
		}
		else
		{
			FocusCurrent();
		}
	}

	public void OnItemArchived()
	{
		Load();
	}

	public void OnHabitSkipped(string habitId)
	{
		if ((ViewModel.ProjectIdentity is TodayProjectIdentity || ViewModel.ProjectIdentity is WeekProjectIdentity) && LocalSettings.Settings.HabitInToday)
		{
			ReLoad(null);
		}
	}

	public void ReLoad(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			SelectedId = id;
		}
		Load();
	}

	public bool IsCompletedList()
	{
		return ViewModel.ProjectIdentity is CompletedProjectIdentity;
	}

	public bool CanAddSubTask()
	{
		if (!(ViewModel.ProjectIdentity is CompletedProjectIdentity) && !(ViewModel.ProjectIdentity is AssignToMeProjectIdentity))
		{
			return !(ViewModel.ProjectIdentity is SearchProjectIdentity);
		}
		return false;
	}

	private async void FocusCurrent()
	{
		await Task.Delay(200);
		DisplayItemModel selectedModel = GetSelectedModel();
		if (selectedModel != null && !selectedModel.IsNewAdd)
		{
			FocusItemTitle(selectedModel);
		}
	}

	private DisplayItemModel GetSelectedModel()
	{
		List<DisplayItemModel> list = ViewModel.Items.Where((DisplayItemModel t) => t.Selected).ToList();
		if (list.Count != 1)
		{
			return null;
		}
		return list[0];
	}

	public void BatchSelectOnMove(bool isUp)
	{
		int num = -1;
		int num2 = -1;
		int firstBatchSelectIndex = ViewModel.GetFirstBatchSelectIndex();
		if (firstBatchSelectIndex < 0)
		{
			return;
		}
		for (int i = 0; i < DisplayModels.Count; i++)
		{
			if (DisplayModels[i].Selected)
			{
				if (num < 0)
				{
					num = i;
					num2 = i;
				}
				else
				{
					num2 = i;
				}
			}
		}
		int num3 = ((firstBatchSelectIndex < num2) ? num2 : num);
		if (num3 < 0 || num3 >= DisplayModels.Count)
		{
			return;
		}
		int num4 = num3;
		while (num4 >= 0 && num4 < DisplayModels.Count)
		{
			num4 = ((!isUp) ? (num4 + 1) : (num4 - 1));
			if (num4 >= 0 && num4 < DisplayModels.Count)
			{
				DisplayItemModel displayItemModel = DisplayModels[num4];
				if (displayItemModel.IsTaskOrNote && displayItemModel.CanBatchSelect)
				{
					num3 = num4;
					break;
				}
			}
		}
		if (num3 == firstBatchSelectIndex)
		{
			SelectTask(DisplayModels[firstBatchSelectIndex].Id, TaskSelectType.Navigate);
			return;
		}
		DisplayItemModel displayItemModel2 = DisplayModels[num3];
		if (displayItemModel2.IsTaskOrNote)
		{
			BatchShiftSelect(displayItemModel2.TaskId, firstBatchSelectIndex);
		}
	}

	public async Task OnCheckBoxRightMouseUp(UIElement element, DisplayItemModel model)
	{
	}

	public void OnNavigateTask(DisplayItemModel model)
	{
		this.NavigateTask?.Invoke(this, model?.TaskId);
	}

	public void OnLineVisibleChanged(DisplayItemModel model, bool visible)
	{
		int num = DisplayModels.IndexOf(model);
		if (num > 0)
		{
			DisplayModels[num - 1].LineVisible = visible;
		}
	}

	public async void SetDetailInOperation(bool inOperate, bool active = true)
	{
		if (ViewModel.InDetail)
		{
			TaskDetailView parentDetailControl = Utils.FindParent<TaskDetailView>(this);
			parentDetailControl?.SetPopupStatus(inOperate);
			if (!inOperate && parentDetailControl != null && active)
			{
				await Task.Delay(50);
				Utils.FindParent<TaskDetailWindow>(parentDetailControl)?.Activate();
			}
		}
	}

	public void RemoveItemByIds(List<string> taskIds)
	{
		ViewModel.RemoveItemByIds(taskIds);
		this.ItemsCountChanged?.Invoke(this, null);
	}

	public void RemoveItemById(string id)
	{
		ViewModel.RemoveItemById(id);
		this.ItemsCountChanged?.Invoke(this, null);
	}

	public IToastShowWindow GetToastParent()
	{
		return Utils.FindParent<IToastShowWindow>(this) ?? App.Window;
	}

	public async Task OnLostFocus(bool reload)
	{
		_caretIndex = -1;
		if (reload)
		{
			await Task.Delay(150);
			_canReload = true;
			LoadAsync(forceRefresh: true);
		}
	}

	public async void AfterTaskProjectChanged(DisplayItemModel model)
	{
		if (model != null)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
			if (!ViewModel.InDetail && taskModel != null && !ViewModel.InMatrix)
			{
				TaskService.TryToastMoveControl(ViewModel.ProjectIdentity, taskModel, taskModel.projectId, hideTitle: true);
			}
		}
	}

	public TimeData GetDefaultTimeData()
	{
		TimeData timeData = ViewModel.ProjectIdentity.GetTimeData();
		TimeData timeData2 = TimeData.BuildDefaultStartAndEnd();
		if (timeData == null)
		{
			return timeData2;
		}
		if (timeData2.StartDate.HasValue)
		{
			if (timeData2.DueDate.HasValue)
			{
				double totalMinutes = (timeData2.DueDate - timeData2.StartDate).Value.TotalMinutes;
				timeData2.DueDate = timeData2.StartDate.Value.AddMinutes(totalMinutes);
			}
			timeData2.StartDate = timeData.StartDate;
		}
		else
		{
			timeData2.StartDate = timeData.StartDate;
			timeData2.IsAllDay = timeData.IsAllDay;
		}
		return timeData2;
	}

	public void FocusItem(string modelId)
	{
		DisplayItemModel modelById = GetModelById(modelId);
		if (modelById != null && !modelById.IsNewAdd)
		{
			FocusItemTitle(modelById);
		}
	}

	public void ResetModel(DisplayItemModel model)
	{
		int num = DisplayModels.IndexOf(model);
		if (num >= 0)
		{
			DisplayModels[num] = model.Clone();
		}
	}

	public void OnSectionStatusChanged(SectionStatus status)
	{
		ViewModel.OpenOrCloseSection(status, storeStatus: true);
		DataChangedNotifier.NotifyListSectionOpenChanged(this, (ViewModel.ProjectIdentity.CatId, ViewModel.ProjectIdentity.SortOption));
	}

	public void SelectOrDeselectAll(DisplayItemModel model, bool selectAll)
	{
		if (!ViewModel.InBatchSelect || model == null || !model.IsSection || model.Section.Children == null)
		{
			return;
		}
		foreach (DisplayItemModel child in model.Section.Children)
		{
			if (!child.CanBatchSelect)
			{
				continue;
			}
			child.Selected = selectAll;
			child.InBatchSelected = selectAll;
			foreach (DisplayItemModel childrenModel in child.GetChildrenModels(getAll: true))
			{
				childrenModel.Selected = selectAll;
				childrenModel.InBatchSelected = selectAll;
			}
		}
		NotifyMultipleSelected(forceShowBatch: false);
	}

	public async Task OnTaskOpenClick(DisplayItemModel model)
	{
		ProjectIdentity projectIdentity = ViewModel.ProjectIdentity;
		if (projectIdentity is ColumnProjectIdentity columnProjectIdentity)
		{
			projectIdentity = columnProjectIdentity.Project;
		}
		if (model != null)
		{
			bool flag = projectIdentity is NormalProjectIdentity || projectIdentity is GroupProjectIdentity || projectIdentity is ParentTaskIdentity || projectIdentity is MatrixQuadrantIdentity || projectIdentity is CompletedProjectIdentity || projectIdentity is ClosedProjectIdentity;
			ViewModel.ChangeModelOpenStatus(model, flag);
			if (flag)
			{
				await TaskService.FoldTask(model.Id, model.IsOpen);
			}
			else
			{
				SmartListTaskFoldHelper.UpdateStatus(model.Id, projectIdentity.CatId, !model.IsOpen);
			}
			TaskChangeNotifier.NotifyTaskOpenChanged(model.Id);
		}
	}

	public async Task OnAddTaskInSectionClick(DisplayItemModel model)
	{
		SectionAddTaskViewModel addViewModel = Utils.FindParent<KanbanContainer>(this)?.GetAddingTaskModel(ViewModel.ProjectIdentity, model.Section);
		int num = DisplayModels.IndexOf(model);
		DisplayModels.Insert(num + 1, new DisplayItemModel
		{
			SourceViewModel = new TaskBaseViewModel
			{
				Id = string.Empty
			},
			AddViewModel = addViewModel,
			Section = model.Section
		});
		SetAddingTask(adding: true);
	}

	public void RemoveSelected(string id)
	{
		ViewModel.RemoveSelectedId(id);
	}

	public void SetSelected(List<string> ids)
	{
		ids = ids ?? new List<string>();
		bool inBatchSelected = ids.Count > 1;
		foreach (DisplayItemModel displayModel in DisplayModels)
		{
			if (ViewModel.InMatrix)
			{
				displayModel.ShowTopMargin = true;
				displayModel.ShowBottomMargin = true;
			}
			if (ids.Contains(displayModel.Id))
			{
				displayModel.Selected = true;
				displayModel.InBatchSelected = inBatchSelected;
			}
			else
			{
				displayModel.Selected = false;
				displayModel.InBatchSelected = false;
			}
		}
		ViewModel.SetSelectedTaskIds(ids);
	}

	public void ScrollToItemById(string id)
	{
		if (ViewModel.Items != null && ViewModel.Items.Any())
		{
			DisplayItemModel displayItemModel = ViewModel.Items.FirstOrDefault((DisplayItemModel item) => item.Id == id);
			if (displayItemModel != null)
			{
				_taskList.ScrollIntoView(displayItemModel);
			}
		}
	}

	public void ScrollToItem(DisplayItemModel item)
	{
		_taskList.ScrollIntoView(item);
	}

	public void HideLoadMore()
	{
		if (_loadMoreItem != null)
		{
			_loadMoreItem.Visibility = Visibility.Collapsed;
		}
	}

	public void UnregisterLoadMore(LoadMoreItemControl item)
	{
		if (_loadMoreItem == item)
		{
			_loadMoreItem = null;
		}
		item.LoadMore -= OnLoadMoreClick;
	}

	public void RegisterLoadMore(LoadMoreItemControl item)
	{
		item.LoadMore -= OnLoadMoreClick;
		item.LoadMore += OnLoadMoreClick;
		_loadMoreItem = item;
	}

	private async void OnLoadMoreClick(object sender, string parentId)
	{
		if (ViewModel.InDetail)
		{
			LoadCompleteChildren(parentId);
			return;
		}
		ProjectIdentity projectIdentity = ViewModel.ProjectIdentity;
		if (projectIdentity is TrashProjectIdentity trash)
		{
			if (await ViewModel.TrashTaskLoader.TryLoadMoreTrashTasks(trash.IsPerson))
			{
				await LoadAsync(forceRefresh: true, !TrashSyncService.IsDrainOff(trash.IsPerson));
			}
		}
		else if (ViewModel.ProjectData.IsCompleted)
		{
			Dictionary<string, DateTime> dictionary = new Dictionary<string, DateTime>();
			foreach (DisplayItemModel displayModel in DisplayModels)
			{
				if (displayModel.IsTask && !string.IsNullOrEmpty(displayModel.ProjectId) && (!dictionary.ContainsKey(displayModel.ProjectId) || displayModel.CompletedTime < dictionary[displayModel.ProjectId]))
				{
					dictionary[displayModel.ProjectId] = displayModel.CompletedTime ?? DateTime.Now;
				}
			}
			if (ViewModel.ProjectIdentity is CompletedProjectIdentity)
			{
				ClosedTaskWithFilterLoader.CompletionLoader.TryLoadTasks(DisplayModels.LastOrDefault((DisplayItemModel t) => t.IsTask)?.CompletedTime, loadMore: true);
			}
			if (ViewModel.ProjectIdentity is AbandonedProjectIdentity)
			{
				ClosedTaskWithFilterLoader.AbandonedLoader.TryLoadTasks(DisplayModels.LastOrDefault((DisplayItemModel t) => t.IsTask)?.CompletedTime, loadMore: true);
			}
		}
		else
		{
			DisplayItemModel displayItemModel = DisplayModels.LastOrDefault((DisplayItemModel m) => m.IsTask && m.Status != 0);
			ViewModel.CompletedTaskLoader.AddLimit(ViewModel.ProjectIdentity, ViewModel.InKanban ? 500 : 50);
			await ViewModel.CompletedTaskLoader.TryLoadCompletedTasks(ViewModel.ProjectIdentity, loadMore: true, displayItemModel?.CompletedTime);
			Load();
		}
	}

	private async void LoadCompleteChildren(string taskId)
	{
		await TaskService.TryLoadTaskChildren(taskId);
		await Task.Delay(200);
		this.NeedReload?.Invoke(this, null);
	}

	public bool IsAllTaskOpen()
	{
		return ViewModel.IsAllTaskOpen();
	}

	public bool IsAllSectionOpen()
	{
		return ViewModel.IsAllSectionOpen();
	}

	public async Task ExpandOrFoldAllTask(bool? open = null)
	{
		if (_isExpandOrFolding)
		{
			return;
		}
		_isExpandOrFolding = true;
		bool flag = open == true;
		ProjectIdentity projectIdentity = ViewModel.ProjectIdentity;
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel displayModel in DisplayModels)
		{
			if (displayModel.IsTask && displayModel.Level == 0 && displayModel.HasChildren)
			{
				list.Add(displayModel);
				list.AddRange(from c in displayModel.GetChildrenModels(getAll: true)
					where c.HasChildren
					select c);
			}
			if (!open.HasValue && !flag && !displayModel.IsOpen && displayModel.HasChildren)
			{
				flag = true;
			}
		}
		if (list.Count > 0)
		{
			List<string> list2 = list.Select((DisplayItemModel m) => m.Id).ToList();
			if (projectIdentity is ColumnProjectIdentity columnProjectIdentity)
			{
				projectIdentity = columnProjectIdentity.Project;
			}
			if (projectIdentity is NormalProjectIdentity || projectIdentity is GroupProjectIdentity || projectIdentity is ParentTaskIdentity)
			{
				await TaskDao.FoldOrOpenTasks(list2, flag);
			}
			else
			{
				SmartListTaskFoldHelper.ResetStatus(projectIdentity.CatId, flag ? list2 : null);
			}
		}
		if (!ViewModel.InKanban)
		{
			LoadAsync(forceRefresh: true, loadMore: true, restoreSelected: true, ignoreFocus: true);
		}
		await Task.Delay(300);
		_isExpandOrFolding = false;
	}

	public async Task ExpandOrFoldAllSection(bool? open = null)
	{
		if (_isExpandOrFolding)
		{
			return;
		}
		_isExpandOrFolding = true;
		bool flag = open == true;
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel displayModel in DisplayModels)
		{
			if (displayModel.IsSection)
			{
				list.Add(displayModel);
				if (!open.HasValue && !flag && !displayModel.IsOpen)
				{
					flag = true;
				}
			}
		}
		if (list.Count > 0)
		{
			await ViewModel.OpenOrCloseAllSections(list, flag);
			if (ViewModel.InKanban)
			{
				_isExpandOrFolding = false;
				return;
			}
			DataChangedNotifier.NotifyListSectionOpenChanged(this, (ViewModel.ProjectIdentity.CatId, ViewModel.ProjectIdentity.SortOption));
		}
		await Task.Delay(300);
		_isExpandOrFolding = false;
	}

	public List<string> GetSelectedTaskIds()
	{
		List<string> list = new List<string>();
		list.AddRange(from DisplayItemModel task in DisplayModels
			where task.IsTaskOrNote && task.Selected
			select task.Id);
		return list;
	}

	public void SetQuickSetPopup(Popup popup)
	{
		_quickSetPopup = popup;
	}

	public void Toast(string toastString)
	{
		GetToastParent()?.TryToastString(null, toastString);
	}

	public void SetMatrixInOperation(bool inOperation)
	{
		if (ViewModel.InMatrix)
		{
			Utils.FindParent<QuadrantControl>(this)?.SetMatrixInOperation(inOperation);
		}
	}

	public async Task<string> GetCompleteText(DisplayItemModel model)
	{
		if (ViewModel.ProjectIdentity is TodayProjectIdentity)
		{
			List<TaskBaseViewModel> source = ViewModel.SourceModels.ToList();
			if (source.Count((TaskBaseViewModel m) => m.Type != DisplayType.Habit && m.Status == 2) >= 3)
			{
				bool showText = false;
				if (model.IsTask)
				{
					List<TaskBaseViewModel> list = source.Where((TaskBaseViewModel m) => m.Status == 0 && m.GetTaskId() != model.Id && ((m.IsTaskOrNote && m.IsTask) || m.IsCheckItem)).ToList();
					if (list.Count == 0 || list.All((TaskBaseViewModel m) => (m.IsCheckItem && TaskCache.FindParent(m.ParentId, model.Id)) || (m.IsTaskOrNote && m.IsTask && TaskCache.FindParent(m.Id, model.Id))))
					{
						showText = true;
					}
				}
				if (model.Type == DisplayType.CheckItem)
				{
					List<TaskBaseViewModel> list2 = source.Where((TaskBaseViewModel m) => m.Status == 0 && m.Id != model.Id && ((m.IsTaskOrNote && m.IsTask) || m.IsCheckItem)).ToList();
					TaskBaseViewModel parent = list2.FirstOrDefault((TaskBaseViewModel m) => m.Id == model.SourceViewModel.ParentId);
					if (list2.Count > 0)
					{
						if (list2.Count == 1 && parent != null && (await TaskDetailItemDao.GetCheckItemsByTaskId(parent.Id)).All((TaskDetailItemModel c) => c.id == model.Id || c.status == 2) && TaskCache.CanTaskCompletedByCheckItem(parent.Id))
						{
							showText = true;
						}
					}
					else
					{
						showText = true;
					}
				}
				if (showText)
				{
					return Utils.GetString(model.IsOutDate() ? "OverdueTasksAllDone" : "TodayTasksAllDone");
				}
			}
		}
		return null;
	}

	public ProjectIdentity GetIdentity()
	{
		return ViewModel.ProjectIdentity;
	}

	public async void TryLoadCompletedTasks(ProjectIdentity identity)
	{
		ClosedLoader loader = ViewModel?.CompletedTaskLoader;
		bool flag = loader != null;
		if (flag)
		{
			flag = await loader.NeedPullCompletedTasks(identity);
		}
		bool flag2 = flag;
		if (flag2)
		{
			flag2 = await loader.TryLoadCompletedTasks(identity);
		}
		if (flag2)
		{
			LoadAsync();
		}
	}

	public async Task SortItemsAndLoad(List<TaskBaseViewModel> source, SectionAddTaskViewModel addingModel = null)
	{
		ViewModel.SourceModels = source;
		await ViewModel.LoadSortedItems(loadMore: true, addingModel);
		List<string> selectedTaskIds = ViewModel.SelectedTaskIds;
		if (selectedTaskIds == null || selectedTaskIds.Count <= 0)
		{
			return;
		}
		foreach (DisplayItemModel item in ViewModel.Items)
		{
			if (ViewModel.SelectedTaskIds.Contains(item.Id))
			{
				item.Selected = true;
				item.InBatchSelected = true;
			}
			else
			{
				item.Selected = false;
				item.InBatchSelected = false;
			}
		}
	}

	public void Dispose()
	{
		ViewModel.Dispose();
		_taskList.ItemsSource = null;
		base.Children.Clear();
		this.ItemsCountChanged = null;
		this.MoveUpCaret = null;
		this.NeedReload = null;
		this.BatchSelect = null;
		this.NavigateTask = null;
		this.ItemSelect = null;
	}

	public async void RemoveAddItem(DisplayItemModel model, bool delay = false)
	{
		KanbanContainer kanban = Utils.FindParent<KanbanContainer>(this);
		if (delay)
		{
			kanban?.StartRemoveAddModel();
			await Task.Delay(150);
		}
		SetAddingTask(adding: false);
		kanban?.RemoveAddingTaskModel();
		DisplayModels?.Remove(model);
	}

	public void SetAddingTask(bool adding)
	{
		ViewModel.AddingTask = adding;
	}

	public void RemoveSelectedId(string id)
	{
		DisplayItemModel displayItemModel = ViewModel?.Items?.FirstOrDefault((DisplayItemModel m) => m.Id == id);
		if (displayItemModel != null)
		{
			displayItemModel.Selected = false;
		}
	}

	public bool ExistId(string id)
	{
		return ViewModel?.Items?.FirstOrDefault((DisplayItemModel m) => m.Id == id) != null;
	}

	public void SetSelectedIdOnShowDetail(string taskId)
	{
		SelectedId = taskId;
	}

	private async Task OnDragDrop(MouseEventArgs e)
	{
		_autoScroller?.SetAutoScrolling(autoScroll: false);
		object dragData = TickDragHelper.GetDragData();
		if (!(dragData is DisplayItemModel displayItemModel))
		{
			if (!(dragData is CheckItemViewModel checkItemViewModel))
			{
				if (!(dragData is CalendarDisplayViewModel calendarDisplayViewModel))
				{
					return;
				}
				if ((!ViewModel.InKanban && !ViewModel.InMatrix) || (ViewModel.InKanban && (_dragExtraItem != null || _dragFromOtherItem != null)))
				{
					if (_dragExtraItem != null)
					{
						await OnCalendarDisplayModelDrop(calendarDisplayViewModel);
					}
					else
					{
						DisplayItemModel displayItemModel2 = ((_dragFromOtherItem?.Id == calendarDisplayViewModel.TaskId) ? _dragFromOtherItem : DisplayModels.FirstOrDefault((DisplayItemModel m) => m.Dragging));
						if (displayItemModel2 != null)
						{
							if (calendarDisplayViewModel.BatchSelected)
							{
								HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
								if (draggingTaskIds != null && draggingTaskIds.Count > 0)
								{
									displayItemModel2.BatchModels = new List<DisplayItemModel>();
									foreach (TaskBaseViewModel item in from task in TickDragHelper.DraggingTaskIds.ToList().Select(TaskCache.GetTaskById)
										where task != null
										select task)
									{
										displayItemModel2.BatchModels.Add(new DisplayItemModel(item));
									}
								}
							}
							await OnTaskDrop(displayItemModel2, calendarDisplayViewModel.BatchSelected);
						}
					}
					ReLoad(SelectedId);
				}
				ListViewContainer listViewContainer = Utils.FindParent<ListViewContainer>(this);
				if (listViewContainer != null && listViewContainer.ParallelShow)
				{
					string ctype = ((!listViewContainer.ShowInCal) ? "task+calendar" : "calendar+task");
					UserActCollectUtils.AddClickEvent("split_view", ctype, "drag_to_list");
				}
			}
			else
			{
				OnCheckItemDrop(checkItemViewModel.Id);
			}
		}
		else if (_canDragSwitch)
		{
			if (displayItemModel.IsSection)
			{
				await OnSectionDrop(displayItemModel);
			}
			else if (displayItemModel.BatchModels == null || ViewModel.SelectedTaskIds == null || ViewModel.SelectedTaskIds.Count <= 1)
			{
				await OnTaskDrop(displayItemModel);
			}
			else
			{
				await OnBatchTaskDrop(displayItemModel);
			}
			await Task.Delay(100);
			LoadAsync();
		}
	}

	private async Task OnCalendarDisplayModelDrop(CalendarDisplayViewModel calModel)
	{
		if (calModel.IsTaskOrNote)
		{
			DisplayItemModel dragExtraItem = _dragExtraItem;
			await OnTaskDrop(dragExtraItem, calModel.BatchSelected);
			_dragExtraItem = null;
		}
	}

	private void OnDragLeave()
	{
		_autoScroller?.SetAutoScrolling(autoScroll: false);
		if (_dragExtraItem != null)
		{
			DisplayModels.Remove(_dragExtraItem);
			this.ItemsCountChanged?.Invoke(this, null);
			ResetDrag();
		}
		else
		{
			DisplayItemModel displayItemModel = DisplayModels.FirstOrDefault((DisplayItemModel m) => m.Dragging);
			if (displayItemModel != null)
			{
				RestoreIndex(displayItemModel);
			}
		}
		if (_dragFromOtherItem != null)
		{
			ViewModel.TempFoldModel(_dragFromOtherItem, fold: false);
			_dragFromOtherItem.Dragging = false;
			_dragFromOtherItem = null;
		}
	}

	private void OnDragEnter()
	{
		if (ViewModel.InMatrix)
		{
			return;
		}
		if (!ViewModel.InKanban)
		{
			TickDragHelper.ChangeDragContent(0, base.ActualWidth - 40.0);
		}
		if (!ViewModel.ProjectIdentity.CanDrag || !ViewModel.ProjectIdentity.CanEdit)
		{
			_canDragSwitch = false;
			return;
		}
		object dragData = TickDragHelper.GetDragData();
		if (dragData is DisplayItemModel displayItemModel)
		{
			if (ViewModel.InKanban)
			{
				string id = ViewModel.ProjectIdentity.Id;
				_canDragSwitch = displayItemModel.DraggingFromColumnId == id;
				return;
			}
			foreach (DisplayItemModel item in ViewModel.Items)
			{
				item.Selected = false;
			}
			_canDragSwitch = true;
			return;
		}
		CalendarDisplayViewModel calModel = dragData as CalendarDisplayViewModel;
		if (calModel != null)
		{
			if (ViewModel.ProjectIdentity.CanDrag)
			{
				_canDragSwitch = (!ViewModel.InKanban && !calModel.IsCheckItem && !calModel.IsCalendarEvent) || (ViewModel.InKanban && calModel.IsTaskOrNote && ViewModel.SourceModels.Any((TaskBaseViewModel t) => t.Id == calModel.TaskId));
			}
			else
			{
				_canDragSwitch = false;
			}
		}
		else
		{
			_canDragSwitch = dragData is CheckItemViewModel || ViewModel.InKanban || object.Equals(TickDragHelper.DragFrom, _taskList);
		}
	}

	private async void OnDragOver(object sender, MouseEventArgs e)
	{
		if (!base.IsVisible || !(Window.GetWindow(this)?.IsMouseCaptured ?? false))
		{
			return;
		}
		bool flag = TaskDetailPopup.IsDragOver(e) && Utils.FindParent<TaskDetailPopup>(this) == null;
		if (!flag && !ViewModel.InDetail)
		{
			TaskView taskViewParent = GetTaskViewParent();
			if (taskViewParent != null && taskViewParent.DetailMouseMouseOver(e))
			{
				flag = true;
			}
		}
		Point position = e.GetPosition(this);
		bool flag2 = !flag && !(position.X < 0.0) && !(position.X > base.ActualWidth) && !(position.Y < 0.0) && !(position.Y > base.ActualHeight);
		if (_dragOver != flag2)
		{
			_dragPoint = default(Point);
			if (flag2)
			{
				OnDragEnter();
			}
			else
			{
				OnDragLeave();
			}
			_dragOver = flag2;
		}
		if (!flag2 || !_canDragSwitch || ViewModel.ProjectIdentity is DateProjectIdentity)
		{
			return;
		}
		if (_autoScroller == null)
		{
			_autoScroller = new DragAutoScrollHelper(_taskList);
		}
		if (_autoScroller.TryAutoScroll(e))
		{
			return;
		}
		object dragData = TickDragHelper.GetDragData();
		object obj = dragData;
		if (!(obj is DisplayItemModel displayItemModel))
		{
			if (obj is CheckItemViewModel)
			{
				if (_dragExtraItem == null)
				{
					_dragExtraItem = new DisplayItemModel
					{
						SourceViewModel = new TaskBaseViewModel
						{
							Type = DisplayType.Task,
							Id = "checkItem2task"
						},
						Dragging = true,
						Level = 0,
						InDetail = ViewModel.InDetail
					};
				}
				OnItemMove(e, _dragExtraItem);
				return;
			}
			if (!(obj is CalendarDisplayViewModel))
			{
				return;
			}
		}
		else
		{
			if (!displayItemModel.InDetail || ViewModel.InDetail)
			{
				DragMove(e, displayItemModel);
				return;
			}
			if (!displayItemModel.InDetail || ViewModel.InDetail)
			{
				return;
			}
		}
		CalendarDisplayViewModel cdm = dragData as CalendarDisplayViewModel;
		DisplayItemModel dv = dragData as DisplayItemModel;
		string taskId = ((cdm != null) ? cdm.TaskId : dv.Id);
		if (_dragExtraItem == null && _dragFromOtherItem == null && DisplayModels.Count > 0)
		{
			if ((_calDragPoint - position).Length > 2.0)
			{
				_calDragPoint = position;
				return;
			}
			await Task.Delay(50);
			if ((_calDragPoint - position).Length > 2.0)
			{
				return;
			}
		}
		DisplayItemModel displayItemModel2 = ((_dragFromOtherItem?.Id == taskId) ? _dragFromOtherItem : DisplayModels.FirstOrDefault((DisplayItemModel f) => f.Id == taskId));
		if (displayItemModel2 == null)
		{
			if (_dragExtraItem == null)
			{
				_dragExtraItem = new DisplayItemModel
				{
					SourceViewModel = ((cdm != null) ? cdm.SourceViewModel : dv.SourceViewModel),
					Dragging = true,
					Level = 0,
					InDetail = ViewModel.InDetail
				};
				if (cdm != null && cdm.BatchSelected)
				{
					HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
					if (draggingTaskIds != null && draggingTaskIds.Count > 0)
					{
						_dragExtraItem.BatchMode = true;
						_dragExtraItem.BatchModels = new List<DisplayItemModel>();
						foreach (TaskBaseViewModel item in from task in TickDragHelper.DraggingTaskIds.ToList().Select(TaskCache.GetTaskById)
							where task != null
							select task)
						{
							_dragExtraItem.BatchModels.Add(new DisplayItemModel(item));
						}
					}
				}
			}
			if (DisplayModels.Count == 0)
			{
				DisplayModels.Add(_dragExtraItem);
				this.ItemsCountChanged?.Invoke(this, null);
			}
			else
			{
				OnItemMove(e, _dragExtraItem);
			}
		}
		else
		{
			if (!displayItemModel2.Dragging)
			{
				displayItemModel2.Dragging = true;
				_dragCurrentIndex = (_dragStartIndex = DisplayModels.IndexOf(displayItemModel2));
			}
			if (_dragFromOtherItem != displayItemModel2)
			{
				_dragFromOtherItem = displayItemModel2;
				_dragFromOtherItem.OriginLevel = displayItemModel2.Level;
				_dragFromOtherItem.DraggingLevel = displayItemModel2.Level;
				ViewModel.TempFoldModel(_dragFromOtherItem, fold: true);
			}
			OnItemMove(e, displayItemModel2);
		}
	}

	private void DragMove(MouseEventArgs e, DisplayItemModel model)
	{
		if (model.IsSection)
		{
			OnSectionMove(e, model);
		}
		else
		{
			OnItemMove(e, model);
		}
	}

	private void OnSectionMove(MouseEventArgs e, DisplayItemModel model)
	{
		Point position = e.GetPosition(_taskList);
		if (!(position.X > 0.0) || !(position.X < _taskList.ActualWidth))
		{
			return;
		}
		ListBoxItem mousePointItem = Utils.GetMousePointItem<ListBoxItem>(position, _taskList);
		if (mousePointItem == null || !(mousePointItem.DataContext is DisplayItemModel displayItemModel) || displayItemModel.Section is PinnedSection || displayItemModel.Section is CompletedSection)
		{
			return;
		}
		int num = DisplayModels.IndexOf(displayItemModel);
		_dragCurrentIndex = DisplayModels.IndexOf(model);
		if (_dragCurrentIndex < 0)
		{
			return;
		}
		if (_dragStartIndex < 0)
		{
			_dragStartIndex = _dragCurrentIndex;
		}
		HideFrontLine(_dragCurrentIndex);
		Point position2 = e.GetPosition(mousePointItem);
		if (num == _dragCurrentIndex)
		{
			return;
		}
		int num2 = -1;
		if (_dragCurrentIndex < num && position2.Y > mousePointItem.ActualHeight / 2.0)
		{
			num2 = num;
			DisplayItemModel displayItemModel2 = ((num2 + 1 < DisplayModels.Count) ? DisplayModels[num2 + 1] : null);
			if (displayItemModel2 != null && !displayItemModel2.IsSection)
			{
				return;
			}
		}
		else if (_dragCurrentIndex > num && position2.Y < mousePointItem.ActualHeight / 2.0)
		{
			num2 = num;
			if (!displayItemModel.IsSection)
			{
				return;
			}
		}
		if (num2 != _dragCurrentIndex && num2 >= 0 && num2 < DisplayModels.Count)
		{
			List<DisplayItemModel> list = DisplayModels.ToList();
			list.Remove(model);
			list.Insert(num2, model);
			ViewModel.SetItems(list);
			_dragCurrentIndex = num2;
		}
	}

	private async void OnItemMove(MouseEventArgs e, DisplayItemModel model)
	{
		Point point = e.GetPosition(_taskList);
		ListBoxItem mousePointItem = Utils.GetMousePointItem<ListBoxItem>(new Point(_taskList.ActualWidth - 40.0, point.Y), this);
		DisplayItemModel dragModel = model;
		if (mousePointItem != null && mousePointItem.DataContext is DisplayItemModel displayItemModel)
		{
			int num = DisplayModels.IndexOf(displayItemModel);
			if (!displayItemModel.CanHoverSwitch(num) || dragModel == null || (dragModel.IsCourse && (displayItemModel.IsSection || displayItemModel.Section != dragModel.Section)) || (dragModel.IsItem && ViewModel.ProjectIdentity.SortOption.groupByType != Constants.SortType.dueDate && (displayItemModel.IsSection || displayItemModel.Section != dragModel.Section)))
			{
				return;
			}
			if (!DisplayModels.Contains(dragModel))
			{
				DisplayModels.Insert(num, dragModel);
				_dragStartIndex = num;
			}
			_dragCurrentIndex = DisplayModels.IndexOf(dragModel);
			Point position = e.GetPosition(mousePointItem);
			if (num != _dragCurrentIndex && ((_dragCurrentIndex < num && position.Y < mousePointItem.ActualHeight / 2.0) || (_dragCurrentIndex > num && position.Y > mousePointItem.ActualHeight / 2.0)))
			{
				if (Math.Abs(num - _dragCurrentIndex) == 1)
				{
					num = _dragCurrentIndex;
				}
				else if (_dragCurrentIndex < num && position.Y < mousePointItem.ActualHeight / 2.0)
				{
					num--;
				}
				else if (_dragCurrentIndex > num && position.Y > mousePointItem.ActualHeight / 2.0)
				{
					num++;
				}
			}
			if (dragModel.IsNote && (ViewModel.ProjectIdentity.SortOption.groupByType == Constants.SortType.dueDate || ViewModel.ProjectIdentity.SortOption.groupByType == Constants.SortType.priority || ViewModel.ProjectIdentity.Id == "note"))
			{
				bool flag = displayItemModel.IsSection && num > _dragCurrentIndex && (displayItemModel.Section is NoteSection || ViewModel.ProjectIdentity.Id == "note");
				if (!flag && displayItemModel.IsSection && num < _dragCurrentIndex && num > 0)
				{
					flag = DisplayModels[num - 1].IsPinned;
				}
				if (!(displayItemModel.Section is PinnedSection || flag))
				{
					return;
				}
			}
			if (dragModel.IsTask && displayItemModel.Section is NoteSection && (!displayItemModel.IsSection || num > _dragCurrentIndex))
			{
				RestoreIndex(model);
				return;
			}
			HideFrontLine(_dragCurrentIndex);
			if (num != _dragCurrentIndex)
			{
				if (num < 0 || num >= DisplayModels.Count || _dragCurrentIndex >= DisplayModels.Count)
				{
					return;
				}
				DisplayItemModel displayItemModel2 = ((num > _dragCurrentIndex) ? displayItemModel : ((num > 0) ? DisplayModels[num - 1] : null));
				DisplayItemModel displayItemModel3 = ((num < _dragCurrentIndex) ? displayItemModel : ((num < DisplayModels.Count - 1) ? DisplayModels[num + 1] : null));
				if ((displayItemModel3 != null && !displayItemModel3.Enable && displayItemModel3.Level > 0) || (!dragModel.IsTask && displayItemModel3 != null && displayItemModel3.Level > 0))
				{
					return;
				}
				if (!(((num >= _dragCurrentIndex) ? displayItemModel.Section : displayItemModel2?.Section)?.CanSwitch(dragModel.Type) ?? true))
				{
					RestoreIndex(model);
					return;
				}
				SwitchModel(_dragCurrentIndex, num);
				_dragCurrentIndex = num;
				HideFrontLine(_dragCurrentIndex);
				int num2 = ((displayItemModel3 != null && displayItemModel3.IsTask) ? displayItemModel3.Level : 0);
				int level = ((displayItemModel2 != null && displayItemModel2.IsTask) ? ((displayItemModel2.Level < num2) ? num2 : ((dragModel.DraggingLevel > displayItemModel2.Level) ? (displayItemModel2.Level + 1) : Math.Max(dragModel.DraggingLevel, num2))) : 0);
				dragModel.Level = level;
				_dragStartLevel = dragModel.Level;
				if (ViewModel.InKanban)
				{
					ViewModel.SetKanbanProperty(DisplayModels.ToList());
				}
			}
			else
			{
				SetLevel();
			}
		}
		else if (Utils.GetMousePointItem<ListBoxItem>(new Point(_taskList.ActualWidth - 40.0, point.Y - 30.0), this) != null)
		{
			SetLevel();
		}
		else
		{
			RestoreIndex(model);
		}
		void SetLevel()
		{
			if (dragModel.IsTask)
			{
				Point dragPoint = _dragPoint;
				_dragPoint = point;
				if (dragPoint != default(Point))
				{
					bool? flag2 = ((Math.Abs(dragPoint.X - point.X) < 0.01) ? _dragToLeft : new bool?(dragPoint.X > point.X));
					if (flag2 != _dragToLeft)
					{
						_dragToLeft = flag2;
						_dragStartX = dragPoint.X;
						_dragStartLevel = dragModel.Level;
					}
				}
				else
				{
					_dragStartLevel = dragModel.Level;
					_dragStartX = point.X;
				}
				double num3 = point.X - _dragStartX;
				DisplayItemModel displayItemModel4 = ((_dragCurrentIndex > 0) ? DisplayModels[_dragCurrentIndex - 1] : null);
				DisplayItemModel displayItemModel5 = ((_dragCurrentIndex < DisplayModels.Count - 1) ? DisplayModels[_dragCurrentIndex + 1] : null);
				int num4 = ((displayItemModel4 != null && displayItemModel4.Enable && displayItemModel4.IsTask && (ViewModel.InDetail || displayItemModel4.Status == 0)) ? (displayItemModel4.Level + 1) : 0);
				int val = ((displayItemModel5 != null && displayItemModel5.IsTask) ? displayItemModel5.Level : 0);
				val = Math.Min(num4, val);
				int val2 = ((!(point.X < 20.0)) ? (_dragStartLevel + (int)(num3 / (double)((_dragToLeft == true && _dragStartX < (double)(40 + model.OriginLevel * 20)) ? 20 : 40))) : 0);
				val2 = Math.Max(0, Math.Min(4, val2));
				int level2 = dragModel.Level;
				if (dragModel.Level <= 4)
				{
					val2 = Math.Max(val, Math.Min(val2, num4));
					dragModel.Level = ((val2 + _parentLevel > 4) ? (4 - _parentLevel) : val2);
				}
				if (dragModel.Level != level2)
				{
					dragModel.DraggingLevel = dragModel.Level;
					if (ViewModel.InKanban)
					{
						ViewModel.SetKanbanProperty(DisplayModels.ToList());
					}
				}
			}
		}
	}

	public async Task StartDrag(DisplayItemModel model, Point p, string dragColumnId = null)
	{
		if (model == null)
		{
			return;
		}
		if (model.IsItem && model.Status != 0)
		{
			Utils.Toast(Utils.GetString("CannotDragCompletedSubTask"));
			return;
		}
		if (!ViewModel.InDetail)
		{
			TaskDetailPopup.TryCloseWindow();
		}
		List<DisplayItemModel> models = ViewModel.Items.ToList();
		ViewModel.CheckSelectedIds();
		List<string> selectedTaskIds = ViewModel.SelectedTaskIds;
		DisplayItemModel displayItemModel;
		if (selectedTaskIds != null && selectedTaskIds.Count > 1 && model.Selected)
		{
			if (!ViewModel.SelectedTaskIds.Contains(model.Id))
			{
				ViewModel.SelectedTaskIds.Add(model.Id);
			}
			string projectId = (ViewModel.ProjectIdentity as NormalProjectIdentity)?.Id ?? (ViewModel.ProjectIdentity as ParentTaskIdentity)?.ProjectId;
			List<string> treeTopIds = TaskDao.GetTreeTopIds(ViewModel.SelectedTaskIds, projectId);
			displayItemModel = new DisplayItemModel
			{
				SourceViewModel = new TaskBaseViewModel
				{
					Type = DisplayType.Task,
					Title = string.Format(Utils.GetString("MultiTasks"), ViewModel.SelectedTaskIds.Count),
					Id = model.Id,
					PinnedTime = model.PinnedTime
				},
				InKanban = !string.IsNullOrEmpty(dragColumnId),
				BatchMode = true,
				Dragging = true,
				DraggingFromColumnId = dragColumnId
			};
			List<DisplayItemModel> list = models.Where((DisplayItemModel m) => treeTopIds.Contains(m.Id)).ToList();
			List<DisplayItemModel> list2 = new List<DisplayItemModel>();
			displayItemModel.Section = list[0].Section;
			foreach (DisplayItemModel item in list)
			{
				item.OriginLevel = item.Level;
				item.DraggingLevel = item.Level;
				list2.Add(item);
				list2.AddRange(item.IsOpen ? item.GetChildrenModels(getAll: true) : new List<DisplayItemModel>());
				if (displayItemModel.Section != item.Section)
				{
					displayItemModel.Section = null;
				}
			}
			displayItemModel.BatchModels = list;
			displayItemModel.DragTaskIds = list2.Select((DisplayItemModel m) => m.Id).ToList();
			displayItemModel.BatchSelectedIds = ViewModel.SelectedTaskIds.ToList();
			list2.Remove(model);
			list2.ForEach(delegate(DisplayItemModel m)
			{
				models.Remove(m);
			});
			int num = models.IndexOf(model);
			if (num >= 0)
			{
				models.Remove(model);
				models.Insert(num, displayItemModel);
			}
		}
		else
		{
			model.DraggingFromColumnId = dragColumnId;
			if (model.IsOpen)
			{
				model.GetChildrenModels(getAll: true)?.ForEach(delegate(DisplayItemModel m)
				{
					models.Remove(m);
				});
			}
			displayItemModel = model;
		}
		ItemsSourceHelper.CopyTo(models, ViewModel.Items);
		_taskList.UpdateLayout();
		bool selected = model.Selected;
		model.Dragging = true;
		model.Selected = false;
		model.OriginLevel = model.Level;
		model.DraggingLevel = model.Level;
		_dragStartIndex = DisplayModels.IndexOf(displayItemModel);
		_reloadAfterDrag = ViewModel.InKanban || ViewModel.InDetail;
		_canDragSwitch = true;
		TaskDragHelpModel.DragHelp.IsDragging = true;
		TaskDetailPopup.SetCanClose(value: false);
		await TickDragHelper.StartDragItem(_taskList, displayItemModel, displayItemModel.DragTaskIds ?? new List<string> { displayItemModel.Id }, base.ActualWidth - 10.0 - (double)(model.Level * 20), new Point(p.X - (double)(model.Level * 20) + (double)(ViewModel.InKanban ? 10 : (-10)), p.Y));
		TaskDetailPopup.SetCanClose(value: true);
		TaskDragHelpModel.DragHelp.IsDragging = false;
		model.Dragging = false;
		model.Selected = selected;
		_canDragSwitch = false;
		ResetDrag();
		if (Window.GetWindow(this) is MainWindow mainWindow)
		{
			mainWindow.TryFocus();
		}
		await Task.Delay(100);
		if (_reloadAfterDrag)
		{
			_reloadAfterDrag = false;
			LoadAsync();
		}
	}

	public async void StartDragSection(DisplayItemModel model)
	{
		if (DisplayModels.Contains(model))
		{
			if (model.IsOpen)
			{
				ViewModel.OpenOrCloseSection(new SectionStatus
				{
					SectionId = model.Section.SectionId,
					IsOpen = false
				}, storeStatus: false);
			}
			_dragCurrentIndex = DisplayModels.IndexOf(model);
			model.Dragging = true;
			model.UnderTaskItem = false;
			_dragStartX = 0.0;
			TaskDragHelpModel.DragHelp.IsDragging = true;
			await TickDragHelper.StartDragItem(_taskList, model, null, base.ActualWidth - 10.0, new Point(0.0, 15.0));
			TaskDragHelpModel.DragHelp.IsDragging = false;
			model.Dragging = false;
			ResetDrag();
		}
	}

	private bool CanChangeSection(SortOption option, DisplayType dragType)
	{
		if (option.groupByType != Constants.SortType.tag)
		{
			if (option.groupByType == Constants.SortType.priority)
			{
				return dragType != DisplayType.Note;
			}
			return true;
		}
		return false;
	}

	private bool CanSort(SortOption option, Section dropSection)
	{
		if (option == null)
		{
			return false;
		}
		bool flag = option.orderByType != Constants.SortType.title && option.orderByType != Constants.SortType.assignee && option.orderByType != Constants.SortType.modifiedTime && option.orderByType != Constants.SortType.createdTime;
		if (flag)
		{
			if (ViewModel.InKanban)
			{
				flag = ColumnViewModel.CanTaskSortInColumn(ViewModel.ProjectIdentity.Id);
				if (flag && option.groupByType == Constants.SortType.none)
				{
					flag = dropSection is PinnedSection || ViewModel.ProjectIdentity.Id != "date:-1";
				}
				if (flag && dropSection is OutdatedSection)
				{
					flag = false;
				}
			}
			else
			{
				flag = dropSection?.CanSort(option.orderByType) ?? true;
			}
		}
		return flag;
	}

	private Section GetSection(string taskId)
	{
		int currentIndex = GetCurrentIndex(taskId);
		if (currentIndex >= 1)
		{
			for (int num = currentIndex - 1; num >= 0; num--)
			{
				DisplayItemModel displayItemModel = ViewModel.Items[num];
				if (displayItemModel.IsSection)
				{
					return displayItemModel.Section;
				}
			}
		}
		return InitEmptySection();
	}

	private Section InitEmptySection()
	{
		if (ViewModel.SortOption.groupByType == Constants.SortType.sortOrder)
		{
			return new Section(canSort: true, canSwitch: true);
		}
		return new Section();
	}

	private void HideFrontLine(int index)
	{
		if (index > 0)
		{
			DisplayItemModel displayItemModel = DisplayModels[Math.Min(DisplayModels.Count, index - 1)];
			if (!displayItemModel.IsSection)
			{
				displayItemModel.LineVisible = false;
			}
			if (_frontItem != displayItemModel)
			{
				if (_frontItem != null && !_frontItem.IsSection)
				{
					_frontItem.LineVisible = true;
				}
				_frontItem = displayItemModel;
			}
		}
		else if (_frontItem != null && !_frontItem.IsSection)
		{
			_frontItem.LineVisible = true;
			_frontItem = null;
		}
	}

	public void ResetDrag()
	{
		_dragCurrentIndex = -1;
		_dragStartIndex = -1;
		_dragPoint = default(Point);
		_dragExtraItem = null;
		_dragFromOtherItem = null;
		_dragToLeft = null;
	}

	private void RestoreIndex(DisplayItemModel dragModel)
	{
		if (_dragStartIndex >= 0 && _dragCurrentIndex >= 0)
		{
			SwitchModel(_dragCurrentIndex, _dragStartIndex);
			_dragCurrentIndex = _dragStartIndex;
			dragModel.Level = dragModel.OriginLevel;
			HideFrontLine(_dragCurrentIndex);
			_ = DateTime.Now.Second;
			_ = 30;
			if (ViewModel.InKanban)
			{
				ViewModel.SetKanbanProperty(DisplayModels.ToList());
			}
		}
	}

	private void SwitchModel(int start, int end)
	{
		if (start < 0 || end < 0 || start >= DisplayModels.Count || end >= DisplayModels.Count)
		{
			return;
		}
		DisplayItemModel value = DisplayModels[start];
		if (end < start)
		{
			for (int num = start; num > end; num--)
			{
				DisplayModels[num] = DisplayModels[num - 1];
			}
			DisplayModels[end] = value;
		}
		else
		{
			for (int i = start; i < end; i++)
			{
				DisplayModels[i] = DisplayModels[i + 1];
			}
			DisplayModels[end] = value;
		}
	}

	private async void StopDragging(bool drop, MouseEventArgs e)
	{
		if (_taskDragPopup != null && _taskDragPopup.IsOpen)
		{
			_ = _taskDragPopup.DataContext;
		}
	}

	private async Task OnSectionDrop(DisplayItemModel dragModel)
	{
		DisplayItemModel model = ((_dragCurrentIndex < DisplayModels.Count && _dragCurrentIndex >= 0) ? DisplayModels[_dragCurrentIndex] : null);
		if (model == null || model.Id != dragModel.Id || !model.Dragging)
		{
			model = null;
			for (int i = 0; i < DisplayModels.Count; i++)
			{
				DisplayItemModel displayItemModel = DisplayModels[i];
				if (displayItemModel.Dragging && displayItemModel.IsSection)
				{
					model = DisplayModels[i];
					_dragCurrentIndex = i;
					break;
				}
			}
		}
		if (model == null)
		{
			return;
		}
		if (_dragCurrentIndex < DisplayModels.Count - 1 && _dragCurrentIndex >= 0)
		{
			DisplayItemModel displayItemModel2 = DisplayModels[_dragCurrentIndex + 1];
			if (displayItemModel2.IsCustomizedSection)
			{
				await ColumnDao.SaveSortOrder(dragModel.Id, displayItemModel2.Id, left: true);
				await TryAdjustInvalidTasksAfterSectionReorder();
				DataChangedNotifier.NotifyColumnChanged(ViewModel?.ProjectIdentity?.Id);
				SyncManager.TryDelaySync();
				model.Dragging = false;
				return;
			}
		}
		await ColumnDao.SaveAsLastSortOrder(dragModel.Id);
		await TryAdjustInvalidTasksAfterSectionReorder();
		DataChangedNotifier.NotifyColumnChanged(ViewModel?.ProjectIdentity?.Id);
		SyncManager.TryDelaySync();
		model.Dragging = false;
	}

	public async Task OnBatchTaskDrop(DisplayItemModel dragModel)
	{
		await OnTaskDrop(dragModel, inBatch: true);
	}

	public async Task OnTaskDrop(DisplayItemModel dragModel, bool inBatch = false)
	{
		DisplayItemModel model = null;
		if (_dragExtraItem == null)
		{
			for (int i = 0; i < DisplayModels.Count; i++)
			{
				if (DisplayModels[i].Id == dragModel.Id)
				{
					model = DisplayModels[i];
					_dragCurrentIndex = i;
					break;
				}
			}
		}
		else
		{
			_dragCurrentIndex = DisplayModels.IndexOf(dragModel);
			if (_dragCurrentIndex >= 0)
			{
				model = dragModel;
			}
		}
		if (model == null)
		{
			return;
		}
		model.Dragging = false;
		List<DisplayItemModel> dropModels = (inBatch ? dragModel.BatchModels : new List<DisplayItemModel> { model });
		if (dropModels == null || (_dragExtraItem == null && model.OriginLevel == model.Level && !inBatch && _dragCurrentIndex == _dragStartIndex))
		{
			return;
		}
		int dragCurrentIndex = _dragCurrentIndex;
		DisplayItemModel frontItem = null;
		DisplayItemModel nextItem = null;
		if (dragCurrentIndex >= 0)
		{
			for (int num = dragCurrentIndex - 1; num >= 0; num--)
			{
				DisplayItemModel displayItemModel = DisplayModels[num];
				if ((displayItemModel.Level <= model.Level && displayItemModel.IsTask) || !displayItemModel.IsTask)
				{
					frontItem = displayItemModel;
					break;
				}
			}
			for (int j = dragCurrentIndex + 1; j < DisplayModels.Count; j++)
			{
				DisplayItemModel displayItemModel2 = DisplayModels[j];
				if (displayItemModel2.IsSection || displayItemModel2.Level < model.Level)
				{
					break;
				}
				if (displayItemModel2.Level == model.Level)
				{
					nextItem = displayItemModel2;
					break;
				}
			}
		}
		List<DisplayItemModel> insertModels = new List<DisplayItemModel>();
		dropModels.ForEach(delegate(DisplayItemModel m)
		{
			m.Level = model.Level;
			insertModels.Add(m);
			m.Selected = true;
			m.InBatchSelected = dropModels.Count > 1;
			List<DisplayItemModel> list3 = (m.IsOpen ? m.GetChildrenModels(getAll: false) : null);
			list3?.ForEach(delegate(DisplayItemModel c)
			{
				c.Level += m.Level - m.OriginLevel;
			});
			if (list3 != null)
			{
				insertModels.AddRange(list3);
			}
		});
		List<DisplayItemModel> list = DisplayModels.ToList();
		if (_dragCurrentIndex >= 0 && _dragCurrentIndex < list.Count)
		{
			list.Remove(model);
			list.InsertRange(_dragCurrentIndex, insertModels);
		}
		ViewModel.SetItems(list);
		_taskList.UpdateLayout();
		await Task.Delay(1);
		bool isUndo = model.Level == 0 && !inBatch && TaskDragUndoModel.TryUndo(model.Id);
		TaskDragUndoModel.TryFinishAll();
		Section section = frontItem?.Section ?? nextItem?.Section;
		if (model.Type == DisplayType.CheckItem)
		{
			string text = section?.SectionId ?? string.Empty;
			if (((_taskDragPopup?.Tag as Section) ?? model.Section).SectionId != text)
			{
				if (section is CompletedSection)
				{
					await TaskDetailItemService.CompleteCheckItem(model.Id);
					return;
				}
				if (ViewModel.SortOption.groupByType == Constants.SortType.priority)
				{
					Utils.Toast(Utils.GetString("CannotDragSubTasksToPrioritySection"));
					return;
				}
				if (ViewModel.SortOption.groupByType == Constants.SortType.project)
				{
					Utils.Toast(Utils.GetString("CannotDragSubTasksToProjectSection"));
					return;
				}
				TaskModel taskModel = await TaskDao.GetThinTaskById(model.TaskId);
				if (!string.IsNullOrEmpty(taskModel.attendId) && !AgendaHelper.CanAccessAgenda(taskModel))
				{
					Utils.Toast(Utils.GetString("OnlyOwnerCanChangeSubtask"));
					return;
				}
			}
		}
		NormalProjectIdentity targetNormalProject = ViewModel.ProjectIdentity as NormalProjectIdentity;
		string targetProjectId = targetNormalProject?.Id;
		bool forceTargetColumnForCrossProjectDrop = !string.IsNullOrEmpty(targetProjectId) && dropModels.Any((DisplayItemModel m) => m.ProjectId != targetProjectId) && targetNormalProject != null && ViewModel.SortOption.groupByType == Constants.SortType.sortOrder;
		string text2 = ((!forceTargetColumnForCrossProjectDrop) ? null : (await ResolveTargetColumnIdForProjectDrop(frontItem, nextItem, targetProjectId)));
		string targetColumnIdForCrossProjectDrop = text2;
		foreach (DisplayItemModel item in dropModels.Where((DisplayItemModel m) => m.Level == 0))
		{
			if (!forceTargetColumnForCrossProjectDrop)
			{
				await item.SourceViewModel.SetIdentityProperties(ViewModel.ProjectIdentity, Utils.FindParent<IToastShowWindow>(this));
			}
		}
		List<string> dropIds = dropModels.Select((DisplayItemModel m) => m.Id).ToList();
		if (frontItem?.Section is CompletedSection || nextItem?.Section is CompletedSection)
		{
			UtilLog.Info("TaskList.CompleteTasks " + dropIds.Join(";") + ", from:TaskDragDrop");
			await TaskService.BatchCompleteTasks(dropIds, ignoreRepeat: false, justComplete: true);
			return;
		}
		List<DisplayItemModel> list2 = dropModels.Where((DisplayItemModel m) => m.Status != 0).ToList();
		if (!ViewModel.InDetail && model.Level == 0 && list2.Count > 0 && !(frontItem?.Section is PinnedSection))
		{
			List<string> ids = list2.Select((DisplayItemModel m) => m.Id).ToList();
			await TaskService.BatchCompleteTasks(ids);
			UtilLog.Info($"TaskList.UnCompleteTasks {ids.Count}, from:TaskDragDrop");
		}
		if (!isUndo)
		{
			await SetModelParent(dropModels, frontItem, model.Level);
		}
		if (forceTargetColumnForCrossProjectDrop)
		{
			bool keepAssignee = ViewModel.ProjectIdentity is AssignToMeProjectIdentity;
			await TaskService.BatchMoveParentTaskProject(dropIds, targetNormalProject.Project, keepAssignee, targetColumnIdForCrossProjectDrop);
		}
		bool flag = await ChangeSortOrder(frontItem, nextItem, model, dropModels);
		UtilLog.Info(string.Format("TaskList.ChangeSortOrder {0}, sort {1}, result {2}", dropIds.Join(";"), ViewModel?.SortOption, flag));
		if (flag)
		{
			TaskChangeNotifier.NotifyTaskSortChanged(dropIds);
		}
		ResetDrag();
		SyncManager.TryDelaySync();
	}

	private async Task<bool> ChangeSortOrder(DisplayItemModel frontItem, DisplayItemModel nextItem, DisplayItemModel dragModel, List<DisplayItemModel> models)
	{
		if (models == null || models.Count == 0)
		{
			return false;
		}
		Section dropSection = frontItem?.Section ?? ((nextItem != null && !nextItem.IsSection) ? nextItem.Section : null);
		bool flag = false;
		if (models.Count > 1)
		{
			DisplayItemModel firstModel = models.First();
			flag = models.Any((DisplayItemModel task) => task.IsPinned != firstModel.IsPinned);
		}
		if (dropSection is PinnedSection && dragModel.IsPinned && !flag)
		{
			List<DisplayItemModel> siblings = DisplayModels.Where((DisplayItemModel i) => !i.IsSection && i.IsPinned).ToList();
			await TaskDragHelper.ChangeOrderInType("taskPinned", frontItem, nextItem, models, siblings, ViewModel.ProjectIdentity);
			return true;
		}
		if (dropSection is PinnedSection && (!dragModel.IsPinned || flag))
		{
			foreach (DisplayItemModel model in models)
			{
				await TaskService.TogglesStarred(model.Id, ViewModel.ProjectIdentity.CatId, true, null, isInDetail: false, notify: false);
			}
			List<DisplayItemModel> siblings2 = DisplayModels.Where((DisplayItemModel i) => !i.IsSection && i.IsPinned).ToList();
			await TaskDragHelper.ChangeOrderInType("taskPinned", frontItem, nextItem, models, siblings2, ViewModel.ProjectIdentity);
			return true;
		}
		if ((dragModel.IsPinned || flag) && dropSection != null && !(dropSection is PinnedSection))
		{
			foreach (DisplayItemModel model2 in models)
			{
				await TaskService.TogglesStarred(model2.Id, ViewModel.ProjectIdentity.CatId, false, null, isInDetail: false, notify: false);
			}
		}
		List<DisplayItemModel> siblings3 = GetSiblings(dropSection);
		bool keepAssignee = ViewModel.ProjectIdentity is AssignToMeProjectIdentity;
		siblings3 = siblings3?.Where((DisplayItemModel m) => !models.Contains(m)).ToList();
		switch (ViewModel.SortOption.groupByType)
		{
		case Constants.SortType.sortOrder:
		{
			string text = ((frontItem == null) ? string.Empty : (frontItem.IsSection ? frontItem.Id : frontItem.ColumnId));
			if (!string.IsNullOrEmpty(text) && dragModel.ColumnId != text)
			{
				await TaskService.BatchSetColumn(models.Select((DisplayItemModel m) => m.Id).ToList(), text);
			}
			break;
		}
		case Constants.SortType.project:
		{
			ProjectModel projectById = CacheManager.GetProjectById(dropSection?.GetProjectId());
			if (dragModel.IsTaskOrNote && projectById != null)
			{
				await TaskService.BatchMoveParentTaskProject(models.Select((DisplayItemModel m) => m.Id).ToList(), projectById, keepAssignee);
			}
			break;
		}
		case Constants.SortType.dueDate:
			if (dragModel.Level != 0)
			{
				break;
			}
			foreach (DisplayItemModel model3 in models)
			{
				if (model3.Section?.SectionId != dropSection?.SectionId && !model3.IsNote)
				{
					await TaskDragHelper.ChangeNewDate(dropSection, model3, "");
				}
			}
			break;
		case Constants.SortType.priority:
		{
			if (dragModel.Level != 0)
			{
				break;
			}
			int priority = dropSection?.GetPriority() ?? 0;
			foreach (DisplayItemModel model4 in models)
			{
				if (model4.IsTask && model4.Priority != priority)
				{
					await TaskService.SetPriority(model4.Id, priority, "", notify: false);
				}
			}
			break;
		}
		case Constants.SortType.assignee:
		{
			if (dragModel.Level != 0)
			{
				break;
			}
			string assignee = dropSection?.GetAssignee() ?? "";
			if (!string.IsNullOrEmpty(assignee) && assignee != "-1")
			{
				List<string> list = (from m in TaskCache.GetTasksByIds(models.Select((DisplayItemModel m) => m.Id).ToList())
					select m.ProjectId).Distinct().ToList();
				bool flag2 = true;
				foreach (string item in list)
				{
					List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(item);
					if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != assignee))
					{
						flag2 = false;
						break;
					}
				}
				if (!flag2)
				{
					Toast(Utils.GetString("ChangeAssigneeError"));
					return false;
				}
			}
			foreach (DisplayItemModel model5 in models)
			{
				if (model5.IsTaskOrNote && model5.Assignee != assignee)
				{
					await TaskService.SetAssignee(model5.Id, assignee, notify: false);
				}
			}
			break;
		}
		case Constants.SortType.createdTime:
			if (dragModel.OriginLevel == dragModel.Level)
			{
				if (dragModel.Section?.SectionId != dropSection?.SectionId)
				{
					Toast(Utils.GetString("DraggingNotSupportInGroup"));
					return false;
				}
				if (ViewModel.SortOption.orderByType == Constants.SortType.createdTime || ViewModel.SortOption.orderByType == Constants.SortType.modifiedTime || ViewModel.SortOption.orderByType == Constants.SortType.title)
				{
					Toast(Utils.GetString("DraggingNotSupportInSort"));
					return false;
				}
			}
			break;
		}
		if (frontItem == null && nextItem == null)
		{
			return false;
		}
		if (ViewModel.SortOption.orderByType == Constants.SortType.sortOrder)
		{
			await ChangeTaskSortOrderInProject(frontItem, nextItem, dragModel, models, dropSection, siblings3);
			return true;
		}
		if (dragModel.OriginLevel == dragModel.Level && dragModel.Section?.SectionId == dropSection?.SectionId && (ViewModel.SortOption.orderByType == Constants.SortType.createdTime || ViewModel.SortOption.orderByType == Constants.SortType.modifiedTime))
		{
			Toast(Utils.GetString("DraggingNotSupportInSort"));
			return false;
		}
		if (dropSection != dragModel.Section && !CanChangeSection(ViewModel.SortOption, dragModel.Type))
		{
			if (ViewModel.SortOption.isFromKanbanColumn && ViewModel.SortOption.groupByType == ViewModel.SortOption.orderByType)
			{
				Toast(Utils.GetString("DraggingNotSupportInSort"));
			}
			else
			{
				Toast(Utils.GetString("DraggingNotSupportInGroup"));
			}
			return false;
		}
		if (!CanSort(ViewModel.SortOption, dropSection))
		{
			if (dragModel.Section == dropSection && dragModel.OriginLevel == dragModel.Level)
			{
				Toast(Utils.GetString("DraggingNotSupportInSort"));
			}
			return false;
		}
		string catId = ViewModel.ProjectIdentity.GetSortProjectId();
		string sortKey = ViewModel.SortOption.GetSortKey();
		if (ViewModel.ProjectIdentity is ColumnProjectIdentity columnProjectIdentity)
		{
			sortKey = ColumnViewModel.GetTaskSortKey(columnProjectIdentity.GetRealSortOption(), columnProjectIdentity.ColumnId);
			if (DisplayModels != null && DisplayModels.All((DisplayItemModel m) => m.IsSection || m.Status != 0 || m.IsPinned || m.SpecialOrder == long.MaxValue || m.SpecialOrder == long.MinValue))
			{
				await InitSortModels(sortKey, catId);
			}
		}
		else if (!string.IsNullOrEmpty(sortKey))
		{
			string arg = dropSection?.SectionEntityId;
			if (dropSection == null)
			{
				arg = "none";
			}
			sortKey = string.Format(sortKey, arg);
		}
		if (siblings3 != null && (siblings3.Where((DisplayItemModel m) => m.SpecialOrder == long.MaxValue || m.SpecialOrder == long.MinValue).Any() || (frontItem != null && !frontItem.IsSection && nextItem != null && !nextItem.IsSection && nextItem.SpecialOrder - frontItem.SpecialOrder < models.Count * 16)))
		{
			await TaskSortOrderService.BatchResetOrders(siblings3, sortKey, catId);
		}
		if (ViewModel.SortOption.orderByType == Constants.SortType.title || ViewModel.SortOption.orderByType == Constants.SortType.assignee)
		{
			return false;
		}
		long? frontOrder = ((frontItem != null && !frontItem.IsSection && frontItem.Level == dragModel.Level) ? new long?(frontItem.SpecialOrder) : ((long?)null));
		long? nextOrder = ((nextItem != null && !nextItem.IsSection && nextItem.Level == dragModel.Level) ? new long?(nextItem.SpecialOrder) : ((long?)null));
		await TaskDragHelper.ChangeOrderInType(sortKey, frontOrder, nextOrder, models, catId);
		return true;
	}

	private static async Task<string> ResolveTargetColumnIdForProjectDrop(DisplayItemModel frontItem, DisplayItemModel nextItem, string projectId)
	{
		string text = ((frontItem == null) ? string.Empty : (frontItem.IsSection ? frontItem.Id : frontItem.ColumnId));
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		string text2 = frontItem?.Section?.SectionId ?? nextItem?.Section?.SectionId;
		if (!string.IsNullOrEmpty(text2) && text2 != "none" && text2 != "NotPinned")
		{
			return text2;
		}
		return await ColumnDao.GetProjectDefaultColumnId(projectId);
	}

	private async Task TryAdjustInvalidTasksAfterSectionReorder()
	{
		if (ViewModel?.ProjectIdentity is NormalProjectIdentity normalProjectIdentity && ViewModel.SortOption.groupByType == Constants.SortType.sortOrder)
		{
			await TaskService.AdjustKanbanData(normalProjectIdentity.Id);
		}
	}

	private async Task InitSortModels(string sortKey, string cId)
	{
		List<DisplayItemModel> list = DisplayModels.ToList();
		List<DisplayItemModel> list2 = new List<DisplayItemModel>();
		foreach (DisplayItemModel item in list)
		{
			if (item.IsPinned)
			{
				continue;
			}
			if (item.IsSection && !item.IsOpen && !(item.Section is CompletedSection) && !(item.Section is PinnedSection) && item.Children != null)
			{
				foreach (DisplayItemModel child in item.Children)
				{
					list2.Add(child);
				}
			}
			else if (!item.IsSection && !item.IsPinned)
			{
				if (item.Status != 0)
				{
					break;
				}
				list2.Add(item);
			}
		}
		await TaskDragHelper.InitSortModels(sortKey, cId, list2);
	}

	private async Task ChangeTaskSortOrderInProject(DisplayItemModel front, DisplayItemModel next, DisplayItemModel dragModel, List<DisplayItemModel> models, Section dropSection, List<DisplayItemModel> siblings)
	{
		if (models == null || models.Count == 0)
		{
			return;
		}
		UtilLog.Info("TaskListChangeSortOrder : Front " + front?.Id + " " + front?.SortOrder + " next " + next?.Id + " " + next?.SortOrder + " drag " + dragModel.Id + " " + dragModel.SortOrder);
		long? frontSortOrder = ((front == null || front.Level < dragModel.Level || front.IsSection) ? ((long?)null) : new long?(front.SortOrder));
		long? nextSortOrder = ((next == null || next.IsSection) ? ((long?)null) : next?.SortOrder);
		if (frontSortOrder.HasValue && nextSortOrder.HasValue && nextSortOrder.Value - frontSortOrder.Value < 16 * models.Count)
		{
			await ResetTaskSortOrders(siblings);
			frontSortOrder = front.SortOrder;
			nextSortOrder = next.SortOrder;
		}
		if (!frontSortOrder.HasValue && !nextSortOrder.HasValue)
		{
			frontSortOrder = 0L;
			nextSortOrder = 268435456L * (long)(models.Count + 1);
		}
		if (!frontSortOrder.HasValue)
		{
			frontSortOrder = nextSortOrder - 268435456;
		}
		if (!nextSortOrder.HasValue)
		{
			nextSortOrder = frontSortOrder + 268435456;
		}
		string columnId = ((ViewModel.ProjectIdentity is ColumnProjectIdentity columnProjectIdentity) ? columnProjectIdentity.ColumnId : ((dropSection == null || !dropSection.Customized) ? "" : dropSection.SectionId));
		for (int i = models.Count - 1; i >= 0; i--)
		{
			long sortOrder = frontSortOrder.Value + (nextSortOrder.Value - frontSortOrder.Value) * (i + 1) / (models.Count + 1);
			await TaskService.SetSortOrder(models[i].Id, sortOrder, columnId);
			UtilLog.Info("TaskListChangeSortOrder : Task " + models[i].Id + " order " + sortOrder);
			if (dragModel.Level > dragModel.OriginLevel && dragModel.IsTask)
			{
				await TaskDao.CheckChildrenLevel(dragModel.Id, dragModel.Level + _parentLevel);
			}
		}
	}

	private async Task ResetTaskSortOrders(List<DisplayItemModel> siblings)
	{
		long sortOrder = 0L;
		foreach (DisplayItemModel sibling in siblings)
		{
			await TaskService.SetSortOrder(sibling.Id, sortOrder);
			sortOrder += 268435456;
		}
	}

	private List<DisplayItemModel> GetSiblings(Section section)
	{
		if (section == null)
		{
			return DisplayModels.Where((DisplayItemModel i) => i.Section == null).ToList();
		}
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel item in DisplayModels.Where((DisplayItemModel i) => !i.IsSection && i.Section?.SectionId == section.SectionId).ToList())
		{
			list.Add(item);
			list.AddRange(item.GetChildrenModels(getAll: true));
		}
		return list;
	}

	private async Task SetModelParent(List<DisplayItemModel> models, DisplayItemModel front, int dropLevel)
	{
		if (models == null)
		{
			return;
		}
		string parentId = "";
		if (front == null || !front.IsTask || dropLevel == 0)
		{
			if (ViewModel.ProjectIdentity is ParentTaskIdentity parentTaskIdentity)
			{
				parentId = parentTaskIdentity.Id;
			}
		}
		else
		{
			parentId = ((front.Level == dropLevel) ? front.ParentId : front.Id);
			int parentStatus = 0;
			if (parentId == front.Id)
			{
				if (TaskCache.GetTaskLevel(parentId) >= 4)
				{
					UtilLog.Info("TaskList.SetParent error, parent.level = 4");
					return;
				}
				parentStatus = front.Status;
			}
			else
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(parentId);
				if (taskModel != null)
				{
					parentStatus = taskModel.status;
				}
			}
			if (parentStatus != 0)
			{
				await TaskService.BatchCompleteTasks((from m in models
					where m.Status == 0
					select m.Id).ToList(), ignoreRepeat: true);
			}
			ProjectIdentity projectIdentity = ViewModel.ProjectIdentity;
			if (projectIdentity is ColumnProjectIdentity columnProjectIdentity)
			{
				projectIdentity = columnProjectIdentity.Project;
			}
			if (projectIdentity is NormalProjectIdentity || projectIdentity is GroupProjectIdentity || projectIdentity is ParentTaskIdentity)
			{
				await TaskService.FoldTask(parentId, isOpen: true);
			}
			else
			{
				SmartListTaskFoldHelper.UpdateStatus(parentId, ViewModel.ProjectIdentity.CatId, isFolded: false);
			}
		}
		List<string> parentChangedIds = new List<string>();
		bool canUndo = true;
		models.ForEach(delegate(DisplayItemModel m)
		{
			if (!m.IsNote)
			{
				if (!Utils.IsEqualString(m.ParentId, parentId) && (m.OriginLevel != 0 || m.Level != 0))
				{
					DisplayItemModel parent = m.Parent;
					if (parent != null && parent.IsTask)
					{
						m.Parent?.RemoveItem(m);
					}
					m.SourceViewModel.ParentId = parentId;
					parentChangedIds.Add(m.Id);
				}
				if (m.Section?.SectionId != front?.Section?.SectionId)
				{
					canUndo = false;
				}
			}
		});
		if (!string.IsNullOrEmpty(parentId))
		{
			await TaskDragHelper.CheckChildrenLevelAndSortOrder(parentId);
		}
		if (string.IsNullOrEmpty(parentId))
		{
			parentId = "";
		}
		if (parentChangedIds.Count > 0)
		{
			UtilLog.Info("TaskList.SetParent " + parentChangedIds.Join(";") + ",parent " + parentId + " from:TaskDragDrop");
			await TaskDao.BatchUpdateParent(parentChangedIds, parentId, null, canUndo);
			TaskChangeNotifier.NotifyTaskParentChanged(parentChangedIds);
		}
	}

	private async Task OnCheckItemDrop(string itemId)
	{
		if (_dragExtraItem == null || !DisplayModels.Contains(_dragExtraItem) || ViewModel.ProjectIdentity is AssignToMeProjectIdentity || DisplayModels == null)
		{
			return;
		}
		DisplayItemModel dragCheckItem = _dragExtraItem;
		if (dragCheckItem == null || !DisplayModels.Contains(dragCheckItem))
		{
			return;
		}
		DisplayItemModel frontItem = null;
		DisplayItemModel nextItem = null;
		int index = DisplayModels.IndexOf(dragCheckItem);
		if (index >= 0)
		{
			for (int num = index - 1; num >= 0; num--)
			{
				DisplayItemModel displayItemModel = DisplayModels[num];
				if ((displayItemModel.Level <= dragCheckItem.Level && displayItemModel.IsTask) || !displayItemModel.IsTask)
				{
					frontItem = displayItemModel;
					break;
				}
			}
			for (int i = index + 1; i < DisplayModels.Count; i++)
			{
				DisplayItemModel displayItemModel2 = DisplayModels[i];
				if (displayItemModel2.IsSection || displayItemModel2.Level < dragCheckItem.Level)
				{
					break;
				}
				if (displayItemModel2.Level == dragCheckItem.Level && displayItemModel2.IsTask)
				{
					nextItem = displayItemModel2;
					break;
				}
			}
		}
		Section dropSection = frontItem?.Section ?? nextItem?.Section;
		if (frontItem != null || nextItem != null)
		{
			TaskDetailItemModel checkItem = await TaskDetailItemDao.GetChecklistItemById(itemId);
			if (checkItem == null)
			{
				return;
			}
			dragCheckItem.SourceViewModel.Title = checkItem.title;
			TaskModel primaryTask = await TaskDao.GetTaskById(checkItem.TaskServerId);
			if (primaryTask == null)
			{
				return;
			}
			TimeData timeData = new TimeData();
			if (!Utils.IsEmptyDate(checkItem.startDate))
			{
				timeData.StartDate = checkItem.startDate;
				timeData.IsAllDay = checkItem.isAllDay;
				timeData.RepeatFlag = primaryTask.repeatFlag;
				timeData.RepeatFrom = primaryTask.repeatFrom;
				if (checkItem.isAllDay.HasValue && checkItem.isAllDay.Value)
				{
					timeData.Reminders = new List<TaskReminderModel>
					{
						new TaskReminderModel
						{
							trigger = "TRIGGER:P0DT9H0M0S"
						}
					};
				}
				else
				{
					timeData.Reminders = new List<TaskReminderModel>
					{
						new TaskReminderModel
						{
							trigger = "TRIGGER:PT0S"
						}
					};
				}
			}
			else if (!Utils.IsEmptyDate(primaryTask.startDate))
			{
				timeData.StartDate = primaryTask.startDate;
				timeData.DueDate = primaryTask.dueDate;
				timeData.IsAllDay = primaryTask.isAllDay;
				timeData.RepeatFlag = primaryTask.repeatFlag;
				timeData.RepeatFrom = primaryTask.repeatFrom;
				TimeData timeData2 = timeData;
				timeData2.Reminders = await TaskReminderDao.GetRemindersByTaskId(primaryTask.id);
			}
			if (dropSection?.SectionId == "nodate")
			{
				timeData.TimeZone = TimeZoneData.LocalTimeZoneModel;
			}
			else
			{
				timeData.TimeZone = new TimeZoneViewModel((!timeData.IsAllDay.HasValue || !timeData.IsAllDay.Value) && primaryTask.Floating, primaryTask.timeZone);
			}
			string parentId = "";
			if (frontItem == null || !frontItem.IsTask || dragCheckItem.Level == 0)
			{
				if (ViewModel.ProjectIdentity is ParentTaskIdentity parentTaskIdentity)
				{
					parentId = parentTaskIdentity.Id;
				}
			}
			else
			{
				parentId = ((frontItem.Level < dragCheckItem.Level) ? frontItem.Id : frontItem.ParentId);
			}
			TaskPrimaryProperty primaryTaskProperty = new TaskPrimaryProperty
			{
				Priority = primaryTask.priority,
				ProjectId = primaryTask.projectId,
				Tags = TagSerializer.ToTags(primaryTask.tag),
				TimeData = timeData,
				ParentId = parentId
			};
			bool flag = frontItem != null && frontItem.Level == dragCheckItem.Level;
			TaskPrimaryProperty taskProperty = await SubtaskToTaskHelper.GetTaskPrimaryProperty(primaryTaskProperty, ViewModel.ProjectIdentity, flag ? frontItem : nextItem, flag, dropSection, dragCheckItem.Level == 0);
			if (!(await ProChecker.CheckTaskLimit(taskProperty.ProjectId)))
			{
				string columnId = ((dropSection != null && dropSection.Customized) ? dropSection.SectionId : "");
				TaskModel taskModel = await TaskService.SubtaskToTask(itemId, taskProperty, columnId);
				if (taskModel != null)
				{
					DisplayItemModel displayItemModel3 = new DisplayItemModel(TaskCache.SafeGetTaskViewModel(taskModel));
					displayItemModel3.Level = dragCheckItem.Level;
					displayItemModel3.InDetail = ViewModel.InDetail;
					index = Math.Max(0, Math.Min(index, DisplayModels.Count));
					DisplayModels[index] = displayItemModel3;
					ChangeSortOrder(frontItem, nextItem, displayItemModel3, new List<DisplayItemModel> { displayItemModel3 });
				}
			}
			await TaskService.OnCheckItemChanged(primaryTask.id);
			SyncManager.TryDelaySync();
		}
		_dragExtraItem = null;
	}

	public void SetParentLevel(int level)
	{
		_parentLevel = level;
	}

	private async void OnDragDrop(object sender, MouseEventArgs e)
	{
		if (_dragOver)
		{
			_dragOver = false;
			await OnDragDrop(e);
		}
		else if (ViewModel.Items.FirstOrDefault((DisplayItemModel i) => i.Dragging) != null)
		{
			if (ViewModel.InDetail)
			{
				Load();
				_dragExtraItem = null;
				_dragFromOtherItem = null;
				return;
			}
			ViewModel.LoadSortedItems(loadMore: true, null, force: true);
		}
		SetSelected(ViewModel.SelectedTaskIds);
		_dragExtraItem = null;
		_dragFromOtherItem = null;
	}

	private void BindEvents()
	{
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		CalendarEventChangeNotifier.TitleChanged += OnCalendarTitleChanged;
		CalendarEventChangeNotifier.SummaryChanged += OnCalendarSummaryChanged;
		CalendarEventChangeNotifier.Deleted += OnEventChanged;
		CalendarEventChangeNotifier.Restored += OnEventChanged;
		CalendarEventChangeNotifier.Changed += OnEventChanged;
		CalendarEventChangeNotifier.RemoteChanged += OnEventChanged;
		DataChangedNotifier.HabitSkip += OnHabitSkip;
		TickDragHelper.DragDrop += OnDragDrop;
		TickDragHelper.DragMoving += OnDragOver;
		if (base.DataContext is TaskListViewModel { IsList: not false })
		{
			DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
			DataChangedNotifier.HabitsChanged += OnHabitsChanged;
		}
	}

	private void UnbindEvents()
	{
		TickDragHelper.DragDrop -= OnDragDrop;
		TickDragHelper.DragMoving -= OnDragOver;
		DataChangedNotifier.ProjectChanged -= OnProjectChanged;
		CalendarEventChangeNotifier.TitleChanged -= OnCalendarTitleChanged;
		CalendarEventChangeNotifier.SummaryChanged -= OnCalendarSummaryChanged;
		CalendarEventChangeNotifier.Deleted -= OnEventChanged;
		CalendarEventChangeNotifier.Restored -= OnEventChanged;
		CalendarEventChangeNotifier.Changed -= OnEventChanged;
		CalendarEventChangeNotifier.RemoteChanged -= OnEventChanged;
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		DataChangedNotifier.HabitsChanged -= OnHabitsChanged;
		DataChangedNotifier.HabitSkip -= OnHabitSkip;
	}

	private void OnHabitSkip(object sender, string e)
	{
		OnHabitSkipped(e);
	}

	private void OnProjectChanged(object sender, EventArgs e)
	{
		if (base.DataContext is TaskListViewModel { SortOption: not null } taskListViewModel && taskListViewModel.SortOption.ContainsSortType(Constants.SortType.project))
		{
			LoadAsync();
		}
	}

	private void OnCalendarTitleChanged(object sender, TextExtra e)
	{
		List<TaskBaseViewModel> list = ViewModel.SourceModels?.ToList();
		if (list == null)
		{
			return;
		}
		foreach (TaskBaseViewModel item in list)
		{
			if (item.EntityId == e.Id)
			{
				item.Title = e.Text;
			}
		}
	}

	private void OnCalendarSummaryChanged(object sender, TextExtra e)
	{
		List<TaskBaseViewModel> list = ViewModel.SourceModels?.ToList();
		if (list == null)
		{
			return;
		}
		foreach (TaskBaseViewModel item in list)
		{
			if (item.EntityId == e.Id)
			{
				item.Content = e.Text;
			}
		}
	}

	private void CheckSortByModifiedTime(DisplayItemModel model)
	{
		if (model == null || ViewModel.SortOption.orderByType != Constants.SortType.modifiedTime || model.IsPinned)
		{
			return;
		}
		int num = ViewModel.Items.IndexOf(model);
		if (num > 0)
		{
			int num2 = 0;
			DisplayItemModel displayItemModel = ViewModel.Items.FirstOrDefault((DisplayItemModel m) => m.IsSection && model.Section == m.Section);
			if (displayItemModel != null)
			{
				num2 = ViewModel.Items.IndexOf(displayItemModel) + 1;
			}
			if (num != num2)
			{
				ITaskOperation focusingItem = GetFocusingItem();
				int caretIndex = _caretIndex;
				ViewModel.Items.RemoveAt(num);
				model.CaretIndex = ((focusingItem == null) ? ((int?)null) : new int?(caretIndex));
				ViewModel.Items.Insert(num2, model);
				_taskList.ScrollIntoView(model);
			}
		}
	}

	private void OnEventChanged(object sender, object e)
	{
		LoadAsync(forceRefresh: false, loadMore: true, restoreSelected: true, ignoreFocus: true);
	}

	private void OnCheckInChanged(object sender, HabitCheckInModel checkInModel)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (checkInModel.CheckinStamp == DateTime.Today.ToString("yyyyMMdd") && base.DataContext is TaskListViewModel taskListViewModel && (taskListViewModel.ProjectIdentity is TodayProjectIdentity || taskListViewModel.ProjectIdentity is WeekProjectIdentity))
			{
				LoadAsync(forceRefresh: false, loadMore: true, restoreSelected: true, ignoreFocus: true);
			}
		});
	}

	private void OnHabitsChanged(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (base.DataContext is TaskListViewModel taskListViewModel && (taskListViewModel.ProjectIdentity is TodayProjectIdentity || taskListViewModel.ProjectIdentity is WeekProjectIdentity))
			{
				LoadAsync(forceRefresh: false, loadMore: true, restoreSelected: true, ignoreFocus: true);
			}
		});
	}

	public async void ShowRecordPopup(UIElement element, double step, string unit)
	{
		if (_recordPopup != null)
		{
			_recordPopup.BeginAnimation(Popup.VerticalOffsetProperty, null);
			_recordPopup.IsOpen = false;
			_recordPopup = null;
		}
		if (element != null)
		{
			Popup popup = new Popup
			{
				Placement = PlacementMode.Top,
				VerticalOffset = 0.0,
				StaysOpen = true,
				HorizontalOffset = -5.0,
				AllowsTransparency = true,
				Opacity = 0.0
			};
			popup.PlacementTarget = element;
			StackPanel stackPanel = new StackPanel();
			Border border = new Border
			{
				Height = 30.0,
				CornerRadius = new CornerRadius(4.0),
				Effect = new DropShadowEffect
				{
					BlurRadius = 10.0,
					Direction = 300.0,
					ShadowDepth = 2.0,
					Opacity = 0.2
				}
			};
			border.SetResourceReference(Panel.BackgroundProperty, "PrimaryColor");
			border.Child = new TextBlock
			{
				FontSize = 13.0,
				Foreground = Brushes.White,
				Opacity = 0.8,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(8.0, 0.0, 8.0, 0.0),
				Text = "+" + step + " " + HabitUtils.GetUnitText(unit)
			};
			stackPanel.Children.Add(border);
			Path path = UiUtils.CreatePath("IcHabitAddStep", "PrimaryColor", "");
			path.Stretch = Stretch.None;
			path.HorizontalAlignment = HorizontalAlignment.Center;
			path.Margin = new Thickness(0.0, -2.0, 0.0, 0.0);
			stackPanel.Children.Add(path);
			popup.Child = stackPanel;
			popup.IsOpen = true;
			_recordPopup = popup;
			BeginRecordAnimation(popup);
		}
	}

	private void BeginRecordAnimation(Popup popup)
	{
		popup.BeginAnimation(UIElement.OpacityProperty, AnimationUtils.GetDoubleAnimation(0.0, 1.0, 100));
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(1.0, 0.0, 800);
		doubleAnimation.BeginTime = TimeSpan.FromMilliseconds(800.0);
		popup.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(0.0, -30.0, 1000);
		doubleAnimation2.BeginTime = TimeSpan.FromMilliseconds(800.0);
		doubleAnimation2.Completed += delegate
		{
			popup.IsOpen = false;
			_recordPopup = null;
		};
		popup.BeginAnimation(Popup.VerticalOffsetProperty, doubleAnimation2);
	}

	public void ManuallyRecordCheckIn(UIElement element, DisplayItemModel model, EventHandler<double> onCheckInPopupSave)
	{
		if (_checkInPopup != null)
		{
			_checkInPopup.IsOpen = false;
			return;
		}
		_checkInPopup = new Popup
		{
			PlacementTarget = element,
			Placement = PlacementMode.Top,
			VerticalOffset = 8.0,
			StaysOpen = false,
			AllowsTransparency = true
		};
		_checkInPopup.Closed += delegate
		{
			_checkInPopup = null;
		};
		ManualRecordCheckinControl manualRecordCheckinControl = new ManualRecordCheckinControl();
		_checkInPopup.Child = manualRecordCheckinControl;
		_checkInPopup.IsOpen = true;
		_checkInPopup.Focus();
		manualRecordCheckinControl.Init(model.Habit.Unit);
		manualRecordCheckinControl.Save += delegate(object o, double e)
		{
			onCheckInPopupSave(o, e);
			if (_checkInPopup != null)
			{
				_checkInPopup.IsOpen = false;
			}
		};
		manualRecordCheckinControl.Cancel += delegate
		{
			if (_checkInPopup != null)
			{
				_checkInPopup.IsOpen = false;
			}
		};
	}

	private void UpdateFocus()
	{
		Window window = Window.GetWindow(this);
		if (window != null && window.Equals(App.Window))
		{
			App.Window.TryFocus();
		}
		else
		{
			window?.Focus();
		}
	}

	public void BatchShiftSelected(string taskId)
	{
		BatchShiftSelect(taskId);
	}

	public void BatchCtrlSelected(string taskId)
	{
		BatchCtrlSelect(taskId);
	}

	private void BatchShiftSelect(string taskId, int currentIndex = -1)
	{
		ViewModel.SetFirstBatchSelectId(taskId);
		currentIndex = ((currentIndex < 0) ? ViewModel.GetFirstBatchSelectIndex() : currentIndex);
		int currentIndex2 = GetCurrentIndex(taskId);
		if (currentIndex < 0)
		{
			currentIndex = currentIndex2;
		}
		if (base.DataContext is TaskListViewModel { ProjectIdentity: NormalProjectIdentity { Project: { closed: var closed } } } && closed == true)
		{
			return;
		}
		int num = Math.Min(currentIndex, currentIndex2);
		int num2 = Math.Max(currentIndex, currentIndex2);
		for (int i = 0; i < ViewModel.Items.Count; i++)
		{
			DisplayItemModel displayItemModel = ViewModel.Items[i];
			if (i >= num && i <= num2 && displayItemModel.CanBatchSelect)
			{
				displayItemModel.Selected = true;
				displayItemModel.InBatchSelected = true;
			}
			else
			{
				displayItemModel.Selected = false;
				displayItemModel.InBatchSelected = false;
			}
		}
		NotifyMultipleSelected();
		if (!ViewModel.InDetail)
		{
			TaskDetailPopup.TryCloseWindow(force: true);
		}
		ScrollToItemById(taskId);
		UpdateFocus();
	}

	private void BatchCtrlSelect(string taskId)
	{
		if (ViewModel.ProjectIdentity is NormalProjectIdentity { Project: { closed: var closed } } && closed == true)
		{
			return;
		}
		ViewModel.SetFirstBatchSelectId(taskId);
		DisplayItemModel selectedModel = GetSelectedModel();
		foreach (DisplayItemModel item in ViewModel.Items.ToList())
		{
			if (!(item.Id == taskId))
			{
				continue;
			}
			if (selectedModel != null && selectedModel.Id == item.Id && selectedModel.CanBatchSelect)
			{
				item.Selected = true;
				if (ViewModel.InBatchSelect)
				{
					SelectItem(new ListItemSelectModel(selectedModel.Id, null, selectedModel.Type, TaskSelectType.Click));
					return;
				}
				break;
			}
			if (item.CanBatchSelect)
			{
				item.Selected = !item.Selected;
				if (selectedModel != null && !selectedModel.CanBatchSelect && selectedModel.Id != taskId)
				{
					selectedModel.Selected = false;
				}
			}
			break;
		}
		NotifyMultipleSelected();
		if (!ViewModel.InDetail)
		{
			TaskDetailPopup.TryCloseWindow(force: true);
		}
		UpdateFocus();
	}

	private void SelectItem(string taskId, TaskSelectType type)
	{
		SelectItem(new ListItemSelectModel(taskId, string.Empty, DisplayType.Task, type));
	}

	private async void SelectItem(ListItemSelectModel selectModel)
	{
		DisplayItemModel displayItemModel = ((!string.IsNullOrEmpty(selectModel.ChildId)) ? ViewModel.Items.FirstOrDefault((DisplayItemModel item) => !string.IsNullOrEmpty(item.Id) && item.Id == selectModel.ChildId) : ViewModel.Items.FirstOrDefault((DisplayItemModel item) => !string.IsNullOrEmpty(item.Id) && item.Id == selectModel.Id));
		if (displayItemModel != null)
		{
			displayItemModel.Selected = true;
			SelectedId = displayItemModel.Id;
		}
		NotifyItemSelect(selectModel);
	}

	private void DelaySelectItem(ListItemSelectModel model)
	{
		DelayActionHandlerCenter.TryDoAction("TaskListDelaySelectItem", delegate
		{
			Utils.RunOnUiThread(base.Dispatcher, delegate
			{
				NotifyItemSelect(model);
			});
		});
	}

	private void NotifyItemSelect(ListItemSelectModel model)
	{
		ViewModel.ExitBatchSelect((!string.IsNullOrEmpty(model?.ChildId)) ? model.ChildId : model?.Id);
		ListViewContainer listViewContainer = Utils.FindParent<ListViewContainer>(this);
		bool flag = false;
		if (listViewContainer != null)
		{
			flag = !(listViewContainer?.CanShowDetailPage() ?? true);
		}
		else
		{
			TaskListWindow taskListWindow = Utils.FindParent<TaskListWindow>(this);
			if (taskListWindow != null)
			{
				flag = taskListWindow.CanShowPopup();
			}
		}
		if (!flag)
		{
			this.ItemSelect?.Invoke(model);
		}
	}

	private int GetCurrentIndex(string taskId)
	{
		return ViewModel.GetCurrentIndex(taskId);
	}

	public void SelectSubtask(IdExtra id, TaskSelectType type)
	{
		if (!Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
		{
			SelectItem(new ListItemSelectModel(id.TaskId, id.ItemId, DisplayType.Task, type));
		}
	}

	public void SelectItem(string id, DisplayType itemType)
	{
		SelectItem(new ListItemSelectModel(id, string.Empty, itemType, TaskSelectType.Click));
	}

	public void OnTasksChanged(TasksChangeEventArgs e, Action reloadAction = null)
	{
		if (!(base.DataContext is TaskListViewModel { InKanban: false, InDetail: false } taskListViewModel))
		{
			return;
		}
		if (taskListViewModel.InMatrix && taskListViewModel.SortOption.orderByType == Constants.SortType.modifiedTime)
		{
			DisplayItemModel selectedModel = GetSelectedModel();
			if (selectedModel != null)
			{
				CheckSortByModifiedTime(selectedModel);
			}
		}
		e.DeletedChangedIds.AddRange(e.UndoDeletedIds);
		List<DisplayItemModel> list = taskListViewModel.Items.ToList();
		if (CheckNeedReloadOnTasksChanged(e, taskListViewModel, list))
		{
			ReloadCheckDragging(reloadAction);
		}
		else if (e.SortOrderChangedIds.Any() && list.Where((DisplayItemModel m) => e.SortOrderChangedIds.Contains(m.TaskId)).ToList().Count > 0)
		{
			ReloadCheckDragging(reloadAction);
		}
		else
		{
			taskListViewModel.CheckTaskChanged(e);
		}
	}

	private void ReloadCheckDragging(Action reloadAction = null)
	{
		if (reloadAction != null)
		{
			reloadAction();
		}
		else if (TickDragHelper.Dragging)
		{
			DelayReload();
		}
		else
		{
			LoadAsync(forceRefresh: false, loadMore: true, restoreSelected: true, ignoreFocus: true);
		}
	}

	private bool CheckNeedReloadOnTasksChanged(TasksChangeEventArgs e, TaskListViewModel vm, List<DisplayItemModel> items)
	{
		if ((!e.CheckItemChangedIds.Any() || !CheckChangedCheckItemIds(e.CheckItemChangedIds.Value)) && (!e.BatchChangedIds.Any() || !CheckChangedIds(e.BatchChangedIds.Value)) && (!e.DeletedChangedIds.Any() || (!(vm.ProjectIdentity is TrashProjectIdentity) && !CheckChangedIds(e.DeletedChangedIds.Value, checkExist: false))) && (!e.PinChangedIds.Any() || vm.ProjectData.IsCompleted || vm.ProjectData.IsProjectClosed || vm.ProjectData.IsTrash || !CheckChangedIds(e.PinChangedIds.Value)))
		{
			if (e.AddIds.Any())
			{
				if (vm.ProjectIdentity is SearchProjectIdentity || vm.ProjectIdentity is DateProjectIdentity)
				{
					goto IL_03e2;
				}
				if (items.Count == 0 || e.AddIds.Value.Any((string id) => items.All((DisplayItemModel model) => model.Id != id)))
				{
					List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(vm.ProjectIdentity, e.AddIds.ToList());
					if (matchedTasks != null && matchedTasks.Count > 0)
					{
						goto IL_03e2;
					}
				}
			}
			if ((!e.StatusChangedIds.Any() || (!vm.ProjectData.IsCompleted && !CheckChangedIds(e.StatusChangedIds.Value))) && (!e.KindChangedIds.Any() || !CheckKindChangedIds(e.KindChangedIds.Value, vm.ProjectIdentity, items)) && (!e.ProjectChangedIds.Any() || !CheckChangedIds(e.ProjectChangedIds.Value)) && (!e.PriorityChangedIds.Any() || !CheckChangedIds(e.PriorityChangedIds.Value)) && (!e.TagChangedIds.Any() || !CheckChangedIds(e.TagChangedIds.Value)) && (!e.DateChangedIds.Any() || !CheckChangedIds(e.DateChangedIds.Value)) && (!e.SortOrderChangedIds.Any() || !CheckChangedIds(e.SortOrderChangedIds.Value)) && (!e.AssignChangedIds.Any() || (!(vm.ProjectIdentity is FilterProjectIdentity) && !(vm.ProjectIdentity is AssignToMeProjectIdentity) && !(vm.ProjectIdentity is MatrixQuadrantIdentity) && !vm.ProjectIdentity.SortOption.ContainsSortType(Constants.SortType.assignee)) || !CheckChangedIds(e.AssignChangedIds.Value, vm.ProjectIdentity.SortOption.ContainsSortType(Constants.SortType.assignee))))
			{
				if (e.CheckItemChangedIds.Any() && LocalSettings.Settings.ShowSubtasks && (vm.ProjectIdentity is TodayProjectIdentity || vm.ProjectIdentity is TomorrowProjectIdentity || vm.ProjectIdentity is WeekProjectIdentity || (vm.ProjectIdentity is MatrixQuadrantIdentity matrixQuadrantIdentity && Parser.ContainsDate(matrixQuadrantIdentity.Quadrant.rule, onlyNormal: true)) || (vm.ProjectIdentity is FilterProjectIdentity filterProjectIdentity && Parser.ContainsDate(filterProjectIdentity.Filter.rule))))
				{
					return CheckChangedIds(e.CheckItemChangedIds.Value);
				}
				return false;
			}
		}
		goto IL_03e2;
		IL_03e2:
		return true;
		bool CheckChangedCheckItemIds(HashSet<string> ids)
		{
			List<DisplayItemModel> list = items?.Where((DisplayItemModel m) => m.IsItem && ids.Contains(m.Id)).ToList();
			List<TaskBaseViewModel> matchedItems = TaskViewModelHelper.GetMatchedItems(vm.ProjectIdentity, ids.ToList());
			return list?.Count != matchedItems?.Count;
		}
		bool CheckChangedIds(HashSet<string> ids, bool checkExist = true)
		{
			List<DisplayItemModel> list = items.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
			if (checkExist && list.Count > 0)
			{
				return true;
			}
			List<TaskBaseViewModel> matchedTasks2 = TaskViewModelHelper.GetMatchedTasks(vm.ProjectIdentity, ids.ToList());
			return list?.Count != matchedTasks2?.Count;
		}
	}

	private bool CheckKindChangedIds(HashSet<string> ids, ProjectIdentity identity, List<DisplayItemModel> items)
	{
		List<DisplayItemModel> list = items.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if ((identity.SortOption.ContainsSortType(Constants.SortType.dueDate) || identity.SortOption.ContainsSortType(Constants.SortType.priority) || identity.SortOption.orderByType == Constants.SortType.createdTime || identity.SortOption.orderByType == Constants.SortType.modifiedTime) && list.Any((DisplayItemModel m) => (m.Type == DisplayType.Task && !(m.Section is TaskSection)) || (m.Type == DisplayType.Note && !(m.Section is NoteSection))))
		{
			return true;
		}
		if (identity is FilterProjectIdentity || identity is TodayProjectIdentity || identity is TomorrowProjectIdentity || identity is WeekProjectIdentity || identity is MatrixQuadrantIdentity)
		{
			return TaskViewModelHelper.GetMatchedTasks(identity, ids.ToList())?.Count != list.Count;
		}
		return false;
	}
}
