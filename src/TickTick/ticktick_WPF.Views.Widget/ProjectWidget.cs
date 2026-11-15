using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using Emoji.Wpf;
using KotlinModels;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomPopup;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Views.Widget;

public class ProjectWidget : WidgetBaseUserControl, IToastShowWindow, INavigateProject, IComponentConnector
{
	private ProjectOrGroupPopup _popup;

	private QuickAddView _quickAddView;

	private readonly DelayActionHandler _delayReloadHandler = new DelayActionHandler(200);

	private List<TaskBaseViewModel> _displayModels;

	internal ProjectWidget ProjectWindow;

	internal Border WidgetBackground;

	internal Grid WidgetGrid;

	internal Grid TitleGrid;

	internal StackPanel TitlePanel;

	internal EmjTextBlock ProjectTitle;

	internal Grid ChoseProjectGrid;

	internal Grid LockedOptionGrid;

	internal Button UnlockWidgetButton;

	internal Button SyncButton;

	internal StackPanel OptionsGrid;

	internal Button AddButton;

	internal Path SyncPath;

	internal Button MorePath;

	internal ContentControl TaskAddGrid;

	internal TextBlock EmptyText;

	internal Grid TaskListGrid;

	internal ListView TaskList;

	internal Grid UndoToastGrid;

	private bool _contentLoaded;

	public string ThemeId => base.Model.ThemeId;

	public string ViewThemeId => base.Model.ViewThemeId;

	public ProjectIdentity ProjectIdentity { get; private set; }

	private ObservableCollection<DisplayItemModel> DisplayItemModels => (TaskList.ItemsSource as ObservableCollection<DisplayItemModel>) ?? new ObservableCollection<DisplayItemModel>();

	public bool IsEditting { private get; set; }

	public ProjectWidget(WidgetViewModel model)
		: base(model)
	{
		InitSelector();
		InitializeComponent();
		base.DataContext = model;
		base.Loaded += OnWidgetLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		if (model.DisplayOption == "top")
		{
			SyncButton.SetResourceReference(FrameworkElement.ToolTipProperty, "Sync");
			UnlockWidgetButton.SetResourceReference(FrameworkElement.ToolTipProperty, "Unlock");
			MorePath.SetResourceReference(FrameworkElement.ToolTipProperty, "More");
		}
		else
		{
			SyncButton.ToolTip = null;
			UnlockWidgetButton.ToolTip = null;
			MorePath.ToolTip = null;
		}
	}

	public void TryToastString(object obj, string toastString)
	{
		WindowToastHelper.ToastString(UndoToastGrid, toastString);
	}

	public async Task<bool> TryToastMoveControl(TaskModel task, ProjectModel project)
	{
		if (task != null && project != null && !MoveToastHelper.CheckTaskMatched(ProjectIdentity, task))
		{
			WindowToastHelper.ShowAndHideToast(UndoToastGrid, new MoveToastControl(project.id, this, task.title, MoveToastType.Move));
			return true;
		}
		return false;
	}

	public void TryHideToast()
	{
		if (UndoToastGrid.Children.Count > 0)
		{
			if (UndoToastGrid.Children[0] is UndoToast undoToast)
			{
				undoToast.OnFinished();
			}
			UndoToastGrid.Children[0].Visibility = Visibility.Collapsed;
		}
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		return await TaskService.BatchDeleteTasks(tasks, showUndo: true, 1, UndoToastGrid);
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, undo);
	}

	public async void TaskDeleted(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.deleted = 1;
			task.modifiedTime = DateTime.Now;
			await TaskService.UpdateTaskOnDeletedChanged(task);
			UndoToast undoToast = new UndoToast();
			undoToast.InitTaskUndo(taskId, task.title);
			WindowToastHelper.ShowAndHideToast(UndoToastGrid, undoToast);
		}
		await Task.Delay(100);
		RemoveItem(taskId);
	}

	public void ToastMoveProjectControl(string projectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
	}

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(UndoToastGrid, uiElement);
	}

	internal override bool IsEditing()
	{
		return IsEditting;
	}

	private void UnbindEvents()
	{
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.TaskDefaultChanged -= OnDefaultChanged;
		DataChangedNotifier.ScheduleChanged -= OnScheduleChanged;
		CalendarEventChangeNotifier.TitleChanged -= OnEventTitleChanged;
		CalendarEventChangeNotifier.Changed -= Reload;
		CalendarEventChangeNotifier.Deleted -= Reload;
		CalendarEventChangeNotifier.Restored -= Reload;
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		DataChangedNotifier.HabitsChanged -= OnHabitsChanged;
		DataChangedNotifier.SyncDone -= OnSyncDone;
		DataChangedNotifier.ProjectChanged -= OnProjectChanged;
		DataChangedNotifier.ThemeModeChanged -= OnAppThemeChanged;
		DataChangedNotifier.CountdownsChanged -= OnCOuntdownsChanged;
		DataChangedNotifier.SortOptionChanged -= OnSortOptionChanged;
		_delayReloadHandler.StopAndClear();
	}

	private void BindEvents()
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.TaskDefaultChanged += OnDefaultChanged;
		DataChangedNotifier.ScheduleChanged += OnScheduleChanged;
		CalendarEventChangeNotifier.TitleChanged += OnEventTitleChanged;
		CalendarEventChangeNotifier.Changed += Reload;
		CalendarEventChangeNotifier.Deleted += Reload;
		CalendarEventChangeNotifier.Restored += Reload;
		TimeChangeNotifier.DayChanged += OnDayChanged;
		DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
		DataChangedNotifier.HabitsChanged += OnHabitsChanged;
		DataChangedNotifier.SyncDone += OnSyncDone;
		DataChangedNotifier.CountdownsChanged += OnCOuntdownsChanged;
		DataChangedNotifier.SortOptionChanged += OnSortOptionChanged;
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		_delayReloadHandler.SetAction(TryReload);
		DataChangedNotifier.ThemeModeChanged += OnAppThemeChanged;
	}

	private void OnSortOptionChanged(object sender, string catId)
	{
		if (ProjectIdentity.CatId == catId)
		{
			Reload();
		}
	}

	private void OnCOuntdownsChanged(object sender, EventArgs e)
	{
		Reload();
	}

	private void OnAppThemeChanged(object sender, EventArgs e)
	{
		if (base.Model.ViewThemeId == "light")
		{
			Reload();
		}
	}

	private void OnProjectChanged(object sender, EventArgs e)
	{
		Reload();
	}

	private async void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			if (e.BatchChangedIds.Any() && CheckChangeIds(e.BatchChangedIds.Value))
			{
				LoadTasks(force: true);
			}
			else if ((e.DeletedChangedIds.Any() || e.UndoDeletedIds.Any()) && (CheckTaskDeletedIds(e.DeletedChangedIds.Value) || CheckTaskDeletedIds(e.UndoDeletedIds.Value)))
			{
				LoadTasks(force: true);
			}
			else if (e.PinChangedIds.Any() && (DisplayItemModels == null || DisplayItemModels.Count == 0 || DisplayItemModels.Any((DisplayItemModel model) => e.PinChangedIds.Contains(model.Id))))
			{
				LoadTasks(force: true);
			}
			else
			{
				if (e.AddIds.Any() && (DisplayItemModels == null || DisplayItemModels.Count == 0 || DisplayItemModels.Any((DisplayItemModel model) => !e.AddIds.Contains(model.Id))))
				{
					List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, e.AddIds.ToList());
					if (matchedTasks != null && matchedTasks.Count > 0)
					{
						LoadTasks(force: true);
						return;
					}
				}
				if (e.StatusChangedIds.Any())
				{
					List<TaskBaseViewModel> matchedTasks2 = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, e.StatusChangedIds.ToList());
					if (matchedTasks2 != null && matchedTasks2.Count > 0)
					{
						LoadTasks(force: true);
						return;
					}
				}
				if (e.KindChangedIds.Any() && CheckKindChangedIds(e.KindChangedIds.Value))
				{
					LoadTasks(force: true);
				}
				else if (e.ProjectChangedIds.Any() && CheckProjectChangedIds(e.ProjectChangedIds.Value))
				{
					LoadTasks(force: true);
				}
				else if (e.PriorityChangedIds.Any() && CheckPriorityChangedIds(e.PriorityChangedIds.Value))
				{
					LoadTasks(force: true);
				}
				else if (e.TagChangedIds.Any() && CheckTagChangedIds(e.TagChangedIds.Value))
				{
					LoadTasks(force: true);
				}
				else
				{
					if (e.SortOrderChangedIds.Any())
					{
						List<DisplayItemModel> obj = DisplayItemModels?.Where((DisplayItemModel m) => e.SortOrderChangedIds.Contains(m.TaskId)).ToList();
						if (obj != null && obj.Count > 0)
						{
							LoadTasks(force: true);
							return;
						}
					}
					if (e.DateChangedIds.Any() && CheckDateChangedIds(e.DateChangedIds.Value))
					{
						LoadTasks(force: true);
					}
					else if (e.AssignChangedIds.Any() && CheckAssignChangedIds(e.AssignChangedIds.Value))
					{
						LoadTasks(force: true);
					}
					else if (e.CheckItemChangedIds.Any() && CheckCheckItemsChanged(e.CheckItemChangedIds.Value))
					{
						LoadTasks(force: true);
					}
					else if (e.TasksOpenChangedIds.Any())
					{
						bool useTaskOpen = ProjectIdentity is NormalProjectIdentity || ProjectIdentity is GroupProjectIdentity || ProjectIdentity is ParentTaskIdentity || ProjectIdentity is MatrixQuadrantIdentity;
						CheckOpenChangedIds(e.TasksOpenChangedIds.Value, useTaskOpen);
					}
				}
			}
		});
	}

	private bool CheckChangeIds(HashSet<string> ids, bool checkCount = true)
	{
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (checkCount && list != null && list.Count > 0)
		{
			return true;
		}
		List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList());
		return list?.Count != matchedTasks?.Count;
	}

	private void CheckOpenChangedIds(HashSet<string> ids, bool useTaskOpen)
	{
		List<DisplayItemModel> models = DisplayItemModels?.ToList() ?? new List<DisplayItemModel>();
		List<DisplayItemModel> list = models.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (list.Count == 0)
		{
			return;
		}
		HashSet<string> hashSet = null;
		if (!useTaskOpen)
		{
			hashSet = SmartListTaskFoldHelper.GetOpenedTaskIds(ProjectIdentity?.CatId);
		}
		bool flag = false;
		foreach (DisplayItemModel item in list)
		{
			bool flag2 = (useTaskOpen ? item.SourceViewModel.IsOpen : (hashSet?.Contains(item.Id) ?? false));
			if (item.IsOpen == flag2)
			{
				continue;
			}
			if (item.IsOpen)
			{
				item.GetChildrenModels(getAll: true).ForEach(delegate(DisplayItemModel m)
				{
					models.Remove(m);
				});
			}
			else
			{
				List<DisplayItemModel> childrenModels = item.GetChildrenModels(getAll: false, hashSet);
				int num = models.IndexOf(item);
				if (num >= 0)
				{
					for (int num2 = childrenModels.Count - 1; num2 >= 0; num2--)
					{
						models.Insert(num + 1, childrenModels[num2]);
					}
				}
			}
			flag = true;
			item.IsOpen = flag2;
		}
		if (flag)
		{
			ItemsSourceHelper.SetItemsSource(TaskList, models);
		}
	}

	private bool CheckCheckItemsChanged(HashSet<string> ids)
	{
		if (!LocalSettings.Settings.ShowSubtasks)
		{
			return false;
		}
		ProjectIdentity projectIdentity = ProjectIdentity;
		if (!(projectIdentity is TodayProjectIdentity) && !(projectIdentity is TomorrowProjectIdentity))
		{
			if (!(projectIdentity is FilterProjectIdentity filterProjectIdentity))
			{
				if (projectIdentity is WeekProjectIdentity)
				{
					goto IL_0053;
				}
			}
			else if (filterProjectIdentity.Filter.ContainsDate())
			{
				goto IL_0053;
			}
			return false;
		}
		goto IL_0053;
		IL_0053:
		List<DisplayItemModel> obj = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.Id)).ToList();
		if (obj != null && obj.Count > 0)
		{
			return true;
		}
		List<TaskBaseViewModel> matchedItems = TaskViewModelHelper.GetMatchedItems(ProjectIdentity, ids.ToList());
		if (matchedItems == null)
		{
			return false;
		}
		return matchedItems.Count > 0;
	}

	private bool CheckTaskDeletedIds(HashSet<string> ids)
	{
		if (ProjectIdentity is TrashProjectIdentity)
		{
			return true;
		}
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList());
		if (list?.Count != matchedTasks?.Count)
		{
			return true;
		}
		return false;
	}

	private bool CheckAssignChangedIds(HashSet<string> ids)
	{
		if (ProjectIdentity == null)
		{
			return false;
		}
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (list != null && list.Count > 0 && ProjectIdentity.SortOption.ContainsSortType(Constants.SortType.assignee))
		{
			return true;
		}
		if (ProjectIdentity is FilterProjectIdentity || ProjectIdentity is AssignToMeProjectIdentity || ProjectIdentity is MatrixQuadrantIdentity)
		{
			return TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList())?.Count != list?.Count;
		}
		return false;
	}

	private bool CheckDateChangedIds(HashSet<string> ids)
	{
		if (ProjectIdentity == null)
		{
			return false;
		}
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (list != null && list.Count > 0 && ProjectIdentity.SortOption.ContainsSortType(Constants.SortType.dueDate))
		{
			return true;
		}
		if (ProjectIdentity is FilterProjectIdentity || ProjectIdentity is TodayProjectIdentity || ProjectIdentity is TomorrowProjectIdentity || ProjectIdentity is WeekProjectIdentity || ProjectIdentity is MatrixQuadrantIdentity)
		{
			return TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList())?.Count != list?.Count;
		}
		return false;
	}

	private bool CheckTagChangedIds(HashSet<string> ids)
	{
		if (ProjectIdentity == null)
		{
			return false;
		}
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (list != null && list.Count > 0 && ProjectIdentity.SortOption.ContainsSortType(Constants.SortType.tag))
		{
			return true;
		}
		if (!(ProjectIdentity is TagProjectIdentity))
		{
			if (ProjectIdentity is FilterProjectIdentity filterProjectIdentity)
			{
				List<string> tags = filterProjectIdentity.GetTags();
				if (tags != null && tags.Count > 0)
				{
					goto IL_00b8;
				}
			}
			if (ProjectIdentity is MatrixQuadrantIdentity matrixQuadrantIdentity)
			{
				List<string> tags2 = matrixQuadrantIdentity.GetTags();
				if (tags2 != null && tags2.Count > 0)
				{
					goto IL_00b8;
				}
			}
			return false;
		}
		goto IL_00b8;
		IL_00b8:
		return TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList())?.Count != list?.Count;
	}

	private bool CheckPriorityChangedIds(HashSet<string> ids)
	{
		if (ProjectIdentity == null)
		{
			return false;
		}
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (list != null && list.Count > 0 && ProjectIdentity.SortOption.ContainsSortType(Constants.SortType.priority))
		{
			return true;
		}
		if ((ProjectIdentity is FilterProjectIdentity filterProjectIdentity && filterProjectIdentity.GetDefaultPriority().HasValue) || (ProjectIdentity is MatrixQuadrantIdentity matrixQuadrantIdentity && matrixQuadrantIdentity.GetDefaultPriority().HasValue))
		{
			return TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList())?.Count != list?.Count;
		}
		return false;
	}

	private bool CheckProjectChangedIds(HashSet<string> ids)
	{
		if (ProjectIdentity == null)
		{
			return false;
		}
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if (list != null && list.Count > 0)
		{
			return true;
		}
		return TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList())?.Count != list?.Count;
	}

	private bool CheckKindChangedIds(HashSet<string> ids)
	{
		if (ProjectIdentity == null)
		{
			return false;
		}
		List<DisplayItemModel> list = DisplayItemModels?.Where((DisplayItemModel m) => ids.Contains(m.TaskId)).ToList();
		if ((ProjectIdentity.SortOption.groupByType == Constants.SortType.dueDate || ProjectIdentity.SortOption.groupByType == Constants.SortType.priority || ProjectIdentity.SortOption.groupByType == Constants.SortType.createdTime || ProjectIdentity.SortOption.groupByType == Constants.SortType.modifiedTime) && list != null && list.Any((DisplayItemModel m) => (m.Type == DisplayType.Task && m.Section is NoteSection) || (m.Type == DisplayType.Note && m.Section is TaskSection)))
		{
			return true;
		}
		if (ProjectIdentity is FilterProjectIdentity || ProjectIdentity is TodayProjectIdentity || ProjectIdentity is TomorrowProjectIdentity || ProjectIdentity is WeekProjectIdentity || ProjectIdentity is MatrixQuadrantIdentity)
		{
			return TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids.ToList())?.Count != list?.Count;
		}
		return false;
	}

	private void OnScheduleChanged(object sender, EventArgs e)
	{
		if (ProjectIdentity is TodayProjectIdentity || ProjectIdentity is TomorrowProjectIdentity || ProjectIdentity is WeekProjectIdentity)
		{
			Reload();
		}
	}

	private void TryReload(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			Reload();
		});
	}

	private void ChangeModelOpenStatus(DisplayItemModel model, bool useTaskOpen)
	{
		if (model.IsOpen)
		{
			model.GetChildrenModels(getAll: true)?.ForEach(delegate(DisplayItemModel child)
			{
				DisplayItemModels.Remove(child);
			});
		}
		else
		{
			HashSet<string> openedTaskIds = null;
			if (!useTaskOpen)
			{
				openedTaskIds = SmartListTaskFoldHelper.GetOpenedTaskIds(ProjectIdentity?.CatId);
			}
			List<DisplayItemModel> childrenModels = model.GetChildrenModels(getAll: false, openedTaskIds);
			int index = DisplayItemModels.IndexOf(model);
			if (index >= 0)
			{
				childrenModels?.ForEach(delegate(DisplayItemModel child)
				{
					DisplayItemModels.Insert(++index, child);
				});
			}
		}
		model.IsOpen = !model.IsOpen;
	}

	private void OnSyncDone(object sender, SyncResult result)
	{
		if (result.RemoteDataChanged)
		{
			Reload();
		}
	}

	private void OnHabitsChanged(object sender, EventArgs e)
	{
		if (ProjectIdentity is TodayProjectIdentity || ProjectIdentity is WeekProjectIdentity)
		{
			Reload();
		}
	}

	private void OnCheckInChanged(object sender, HabitCheckInModel e)
	{
		if (e.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) && (ProjectIdentity is TodayProjectIdentity || ProjectIdentity is WeekProjectIdentity))
		{
			Reload();
		}
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		Reload(refreshQuickAddDate: true);
	}

	private void OnEventTitleChanged(object sender, TextExtra extra)
	{
		NotifyTitleChanged(extra.Id, extra.Text);
	}

	private void Reload(object sender, CalendarEventModel model)
	{
		Reload();
	}

	private void Reload(object sender, List<string> taskIds)
	{
		Reload();
	}

	private void Reload(object sender, TaskModel task)
	{
		Reload();
	}

	private void Reload(object sender, string itemId)
	{
		Reload();
	}

	private void InitSelector()
	{
		base.Resources["WidgetListItemSelector"] = new WidgetListItemSelector(this);
	}

	private async void OnWidgetLoaded(object sender, EventArgs e)
	{
		BindEvents();
		await LoadProjectIdentity();
		SetShowAdd(withAnim: false);
		LoadTasks();
		LockWidget(base.Model.IsLocked);
	}

	internal override void LockWidget(bool isLocked)
	{
		if (base.IsLocked == isLocked)
		{
			return;
		}
		foreach (UIElement child in WidgetGrid.Children)
		{
			if (child.Equals(TitleGrid))
			{
				for (int i = 0; i < TitleGrid.Children.Count; i++)
				{
					TitleGrid.Children[i].IsHitTestVisible = !isLocked;
				}
				LockedOptionGrid.IsHitTestVisible = isLocked;
			}
			else if (!child.Equals(TaskListGrid))
			{
				child.IsHitTestVisible = !isLocked;
			}
		}
		OptionsGrid.Visibility = (isLocked ? Visibility.Collapsed : Visibility.Visible);
		base.LockWidget(isLocked);
		if (isLocked)
		{
			HideAddView();
		}
		else
		{
			SetShowAdd(withAnim: false);
		}
		LockedOptionGrid.Visibility = ((!isLocked) ? Visibility.Collapsed : Visibility.Visible);
		ChoseProjectGrid.Visibility = (isLocked ? Visibility.Collapsed : Visibility.Visible);
		foreach (DisplayItemModel displayItemModel in DisplayItemModels)
		{
			displayItemModel.HitVisible = !isLocked;
		}
	}

	private async void OnAddTaskClick(object sender, RoutedEventArgs e)
	{
		await Task.Delay(50);
		InitQuickAddView(focus: true);
	}

	public void SetShowAdd(bool withAnim = true)
	{
		if (LocalSettings.Settings.ExtraSettings.PwShowAdd)
		{
			if (ProjectIdentity != null && ProjectIdentity.CanAddTask())
			{
				InitQuickAddView(focus: false, resetIdentity: false, withAnim);
			}
			else
			{
				HideAddView();
			}
		}
		else if (_quickAddView != null && _quickAddView.IsLostFocus())
		{
			HideAddView();
		}
		SetAddButton();
	}

	private void HideAddView()
	{
		QuickAddView quickAddView = _quickAddView;
		_quickAddView = null;
		if (quickAddView == null)
		{
			return;
		}
		quickAddView.TaskAdded -= OnTaskAdded;
		quickAddView.TitleText.LostFocus -= OnQuickAddLostFocus;
		DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(108.0, 0.0, 120);
		doubleAnimation.Completed += delegate
		{
			if (AddButton != null)
			{
				AddButton.IsHitTestVisible = true;
			}
			TaskAddGrid.Content = null;
		};
		quickAddView.BeginAnimation(FrameworkElement.MaxHeightProperty, doubleAnimation);
	}

	private async void OnQuickAddLostFocus(object sender, RoutedEventArgs e)
	{
		await Task.Delay(100);
		if (_quickAddView != null && !_quickAddView.TitleText.Focused && !_quickAddView.ContentText.EditBox.TextArea.IsKeyboardFocused && !_quickAddView.IsInOperation && string.IsNullOrEmpty(_quickAddView.ContentText.Text) && string.IsNullOrEmpty(_quickAddView.TitleText.EditBox.Text))
		{
			await (_quickAddView?.TryAddTaskOnLostFocus());
			HideAddView();
		}
	}

	private void SetAddButton()
	{
		AddButton.Visibility = ((LocalSettings.Settings.ExtraSettings.PwShowAdd || ProjectIdentity == null || !ProjectIdentity.CanAddTask()) ? Visibility.Collapsed : Visibility.Visible);
	}

	public void ResetQuickAddView(bool keepText = true)
	{
		SetAddButton();
		InitQuickAddView();
		QuickAddView quickAddView = _quickAddView;
		if (quickAddView != null)
		{
			ProjectIdentity projectIdentity = ProjectIdentity;
			if (projectIdentity != null)
			{
				SortOption sortOption = projectIdentity.SortOption;
				ProjectIdentity = projectIdentity.Copy(projectIdentity);
				projectIdentity = ProjectIdentity;
				projectIdentity.SortOption = sortOption;
				quickAddView.ResetView(projectIdentity, focus: false, keepText);
			}
		}
	}

	private async Task InitQuickAddView(bool focus = false, bool resetIdentity = false, bool anim = true)
	{
		ProjectIdentity projectIdentity = ProjectIdentity;
		if (projectIdentity == null)
		{
			return;
		}
		if (!projectIdentity.CanAddTask() || base.IsLocked)
		{
			HideAddView();
			return;
		}
		if (resetIdentity)
		{
			SortOption sortOption = projectIdentity.SortOption;
			ProjectIdentity = projectIdentity.Copy(projectIdentity);
			projectIdentity = ProjectIdentity;
			projectIdentity.SortOption = sortOption;
		}
		if (_quickAddView == null)
		{
			if (!focus && !LocalSettings.Settings.ExtraSettings.PwShowAdd)
			{
				return;
			}
			_quickAddView = new QuickAddView(projectIdentity, QuickAddView.Scenario.TaskList, base.Model.ThemeId, focus)
			{
				Margin = new Thickness(0.0, 8.0, 0.0, 8.0)
			};
			_quickAddView.AddTaskGrid.Margin = new Thickness(0.0);
			_quickAddView.InputBorderContent.Background = Brushes.Transparent;
			_quickAddView.VerticalAlignment = VerticalAlignment.Bottom;
			_quickAddView.TaskAdded += OnTaskAdded;
			TaskAddGrid.Content = _quickAddView;
			if (!anim)
			{
				_quickAddView.BeginAnimation(FrameworkElement.MaxHeightProperty, null);
			}
			_quickAddView.TitleText.EditBox.LostFocus += OnQuickAddLostFocus;
			if (focus)
			{
				_quickAddView.FocusEnd();
			}
		}
		if (LocalSettings.Settings.ExtraSettings.PwShowAdd)
		{
			_quickAddView.TitleText.EditBox.LostFocus -= OnQuickAddLostFocus;
			await Task.Delay(120);
			_quickAddView?.BeginAnimation(FrameworkElement.MaxHeightProperty, null);
		}
	}

	private void OnDefaultChanged(object sender, EventArgs e)
	{
		ResetQuickAddView();
	}

	private void OnTaskAdded(object sender, TaskModel task)
	{
		LoadTasks();
	}

	public void Reload(bool refreshQuickAddDate = false, bool force = false)
	{
		if (refreshQuickAddDate)
		{
			ResetQuickAddView();
		}
		LoadTasks(force);
	}

	private void LoadTasks(bool force = false)
	{
		ThreadUtil.DetachedRunOnUiThread(async delegate
		{
			if ((!IsEditting || force) && ProjectIdentity != null)
			{
				if (ProjectIdentity is DeletedProjectIdentity || ProjectIdentity is ClosedProjectIdentity)
				{
					EmptyText.Visibility = Visibility.Visible;
				}
				else
				{
					WidgetViewModel widgetModel = base.Model;
					List<TaskBaseViewModel> models = await ProjectTaskDataProvider.GetDisplayModels(ProjectIdentity, withChild: true, !widgetModel.HideComplete);
					_displayModels = models;
					int num = await TaskCountCache.TryGetCount(ProjectIdentity);
					widgetModel.CollapseTitle = ProjectTitle.Text + ((num > 0) ? (" (" + num + ")") : string.Empty);
					ObservableCollection<DisplayItemModel> displayModels = BuildDisplayItemModels(models);
					bool isParentTag = ProjectIdentity is TagProjectIdentity tagProjectIdentity && TagDao.IsParentTag(tagProjectIdentity.Tag);
					TryAdjustSortType(isParentTag);
					List<ColumnModel> columns = null;
					if (ProjectIdentity is NormalProjectIdentity normalProjectIdentity && normalProjectIdentity.SortOption.groupByType == Constants.SortType.sortOrder)
					{
						columns = await ColumnDao.GetColumnsByProjectId(normalProjectIdentity.Project.id);
					}
					ObservableCollection<DisplayItemModel> observableCollection = await SortHelper.Sort(displayModels, ProjectIdentity, showFold: true, showLoadMore: false, columns, null, !widgetModel.HideComplete);
					foreach (DisplayItemModel item in observableCollection)
					{
						item.CurrentProjectIdentity = ProjectIdentity;
					}
					ItemsSourceHelper.SetItemsSource(TaskList, observableCollection.ToList());
					EmptyText.Visibility = ((displayModels.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
					await Task.Delay(1000);
					int num2 = await TaskCountCache.TryGetCount(ProjectIdentity);
					widgetModel.CollapseTitle = ProjectTitle.Text + ((num2 > 0) ? (" (" + num2 + ")") : string.Empty);
				}
			}
		});
	}

	public void RemoveItem(DisplayItemModel model)
	{
		if (DisplayItemModels != null && model != null)
		{
			DisplayItemModels.Remove(model);
			_displayModels?.RemoveAll((TaskBaseViewModel t) => t.Id == model.Id);
		}
	}

	private void RemoveItem(string edittingTaskId)
	{
		if (DisplayItemModels != null && DisplayItemModels.Any())
		{
			DisplayItemModel displayItemModel = DisplayItemModels.FirstOrDefault((DisplayItemModel i) => i.IsTaskOrNote && i.Id == edittingTaskId);
			if (displayItemModel != null)
			{
				DisplayItemModels.Remove(displayItemModel);
			}
		}
		_displayModels?.RemoveAll((TaskBaseViewModel t) => t.Id == edittingTaskId);
	}

	private void TryAdjustSortType(bool isParentTag)
	{
		if (ProjectIdentity == null)
		{
			return;
		}
		if (ProjectIdentity is NormalProjectIdentity normalProjectIdentity)
		{
			if (ProjectIdentity.SortOption.groupByType == Constants.SortType.project || (ProjectIdentity.SortOption.groupByType == Constants.SortType.assignee && !normalProjectIdentity.Project.IsShareList()))
			{
				ProjectIdentity.SortOption.groupBy = Constants.SortType.sortOrder.ToString();
				ProjectIdentity.SortOption.orderBy = Constants.SortType.sortOrder.ToString();
			}
			if (normalProjectIdentity.Project.IsNote)
			{
				if (ProjectIdentity.SortOption.groupByType == Constants.SortType.priority || ProjectIdentity.SortOption.groupByType == Constants.SortType.dueDate)
				{
					ProjectIdentity.SortOption.groupBy = Constants.SortType.sortOrder.ToString();
				}
				if (ProjectIdentity.SortOption.orderByType == Constants.SortType.priority || ProjectIdentity.SortOption.orderByType == Constants.SortType.dueDate)
				{
					ProjectIdentity.SortOption.orderBy = Constants.SortType.sortOrder.ToString();
				}
			}
			_ = normalProjectIdentity.Project.IsNote;
		}
		if (ProjectIdentity is GroupProjectIdentity groupProjectIdentity)
		{
			if (groupProjectIdentity.DisplayKind == 1)
			{
				if (ProjectIdentity.SortOption.groupByType == Constants.SortType.priority || ProjectIdentity.SortOption.groupByType == Constants.SortType.dueDate)
				{
					ProjectIdentity.SortOption.groupBy = Constants.SortType.project.ToString();
				}
				if (ProjectIdentity.SortOption.orderByType == Constants.SortType.priority || ProjectIdentity.SortOption.orderByType == Constants.SortType.dueDate || ProjectIdentity.SortOption.orderByType == Constants.SortType.sortOrder)
				{
					ProjectIdentity.SortOption.orderBy = Constants.SortType.createdTime.ToString();
				}
			}
			_ = groupProjectIdentity.DisplayKind;
		}
		if (ProjectIdentity is TagProjectIdentity && ProjectIdentity.SortOption.groupByType == Constants.SortType.tag && !isParentTag)
		{
			ProjectIdentity.SortOption.groupBy = Constants.SortType.project.ToString();
		}
		if (!(ProjectIdentity is NormalProjectIdentity))
		{
			if (ProjectIdentity.SortOption.groupByType == Constants.SortType.sortOrder)
			{
				ProjectIdentity.SortOption.groupBy = Constants.SortType.project.ToString();
			}
			if (ProjectIdentity.SortOption.orderByType == Constants.SortType.sortOrder)
			{
				ProjectIdentity.SortOption.orderBy = Constants.SortType.dueDate.ToString();
			}
		}
	}

	private void NotifyTitleChanged(string id, string title)
	{
		DisplayItemModel displayItemModel = DisplayItemModels?.FirstOrDefault((DisplayItemModel model) => model.Id == id);
		if (displayItemModel != null)
		{
			displayItemModel.SourceViewModel.Title = title;
		}
	}

	private async Task LoadProjectIdentity(bool onSelect = false)
	{
		WidgetViewModel model = base.Model;
		if (model == null)
		{
			return;
		}
		switch (model.Type)
		{
		case "normal":
		case "smart":
			await LoadProject();
			break;
		case "group":
			await LoadProjectGroup();
			break;
		case "filter":
			await LoadFilterProject();
			break;
		case "tag":
			await LoadTagProject();
			break;
		default:
			InitDefaultProject();
			break;
		}
		ProjectIdentity projectIdentity = ProjectIdentity;
		if (projectIdentity == null)
		{
			return;
		}
		ProjectTitle.Text = projectIdentity.GetDisplayTitle();
		if (!(projectIdentity is DeletedProjectIdentity) && !(projectIdentity is ClosedProjectIdentity))
		{
			if (onSelect)
			{
				model.GroupType = projectIdentity.SortOption?.groupBy ?? model.GroupType;
				model.SortType = projectIdentity.SortOption?.orderBy ?? model.SortType;
				model.Order = projectIdentity.SortOption?.order ?? model.Order;
			}
			else
			{
				projectIdentity.SortOption = new SortOption
				{
					groupBy = model.GroupType,
					orderBy = model.SortType,
					order = model.Order
				};
			}
		}
	}

	private void InitDefaultProject()
	{
		ProjectIdentity = ProjectIdentity.CreateInboxProject();
	}

	private async Task LoadFilterProject()
	{
		FilterModel filterModel = await FilterDao.GetFilterById(base.Model.Identity);
		if (filterModel != null && filterModel.deleted != 1)
		{
			ProjectIdentity = new FilterProjectIdentity(filterModel);
		}
		else
		{
			LoadInvalidProject();
		}
	}

	private async Task LoadTagProject()
	{
		TagModel tagByName = CacheManager.GetTagByName(base.Model.Identity.ToLower());
		if (tagByName != null)
		{
			ProjectIdentity = new TagProjectIdentity(tagByName);
		}
	}

	private async Task LoadProjectGroup()
	{
		string groupId = base.Model.Identity;
		ProjectGroupModel projectGroupModel = await ProjectGroupDao.GetProjectGroupById(groupId);
		if (projectGroupModel != null && projectGroupModel.deleted != 1)
		{
			List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(groupId);
			ProjectIdentity = new GroupProjectIdentity(projectGroupModel, projectsInGroup);
		}
		else
		{
			LoadInvalidProject();
		}
	}

	private async Task LoadProject()
	{
		string identity = base.Model.Identity;
		if (SpecialListUtils.IsSmartProject(identity))
		{
			ProjectIdentity = ProjectIdentity.CreateSmartIdentity(identity);
			return;
		}
		ProjectModel projectById = CacheManager.GetProjectById(identity);
		if (projectById == null)
		{
			if (identity.Contains("inbox"))
			{
				ProjectIdentity = new NormalProjectIdentity(new ProjectModel
				{
					id = Utils.GetInboxId(),
					name = Utils.GetString("Inbox")
				});
			}
			else
			{
				LoadInvalidProject();
			}
		}
		else if (projectById.closed.HasValue && projectById.closed.Value)
		{
			ProjectIdentity = new ClosedProjectIdentity(projectById.id);
		}
		else if (!projectById.delete_status)
		{
			ProjectIdentity = new NormalProjectIdentity(projectById);
		}
		else
		{
			LoadInvalidProject();
		}
	}

	private void LoadInvalidProject()
	{
		ProjectIdentity = new DeletedProjectIdentity();
	}

	private static ObservableCollection<DisplayItemModel> BuildDisplayItemModels(IEnumerable<TaskBaseViewModel> models)
	{
		ObservableCollection<DisplayItemModel> observableCollection = new ObservableCollection<DisplayItemModel>();
		foreach (TaskBaseViewModel model in models)
		{
			if (model is HabitBaseViewModel habitBaseViewModel)
			{
				DisplayItemModel displayItemModel = new DisplayItemModel(model);
				displayItemModel.ShowReminder = HabitUtils.IsReminderValid(habitBaseViewModel.Habit.Reminder);
				observableCollection.Add(displayItemModel);
			}
			else if (model.Type == DisplayType.Event)
			{
				observableCollection.Add(new DisplayItemModel(model));
			}
			else
			{
				observableCollection.Add(new DisplayItemModel(model, showProject: false, isKanbanMode: true));
			}
		}
		return observableCollection;
	}

	private void OnProjectClick(object sender, MouseButtonEventArgs e)
	{
		e.GetPosition(TitleGrid);
		if (_popup == null)
		{
			_popup = new ProjectOrGroupPopup(new EscPopup
			{
				StaysOpen = false,
				PopupAnimation = PopupAnimation.Fade,
				Placement = PlacementMode.Bottom,
				HorizontalOffset = -5.0,
				PlacementTarget = TitlePanel,
				VerticalOffset = -5.0
			}, new ProjectExtra(), new ProjectSelectorExtra
			{
				showAll = false,
				showFilters = true,
				showTags = true,
				showSmartProjects = true,
				showSmartAll = true,
				batchMode = false,
				showListGroup = true
			});
			_popup.Resources = GetParentWindow().Resources;
			_popup.ItemSelect += OnItemSelect;
			_popup.Show();
			_popup.Closed += OnProjectClosed;
		}
		else
		{
			_popup.Show();
			_popup.SetData(new ProjectExtra());
		}
		IsEditting = true;
	}

	private void OnProjectClosed(object sender, EventArgs e)
	{
		IsEditting = false;
	}

	private async void OnItemSelect(object sender, SelectableItemViewModel project)
	{
		ProjectTitle.Text = project.Title;
		base.Model.Type = project.Type;
		base.Model.Identity = project.Id;
		await LoadProjectIdentity(onSelect: true);
		await base.Model.Save();
		TryAdjustSortType(isParentTag: true);
		WidgetConfigHelper.TryGetSettings(base.Model)?.InitSortType();
		ResetQuickAddView(keepText: false);
		LoadTasks(force: true);
	}

	private void OnMoreClick(object sender, RoutedEventArgs e)
	{
		OnMoreClick(MorePath);
	}

	internal override void OnMoreAction(object sender, WidgetMoreAction e)
	{
		if (e == WidgetMoreAction.Sync)
		{
			SyncTask();
		}
		else
		{
			base.OnMoreAction(sender, e);
		}
	}

	internal override void CloseWidget()
	{
		ProjectWidgetsHelper.CloseProject(base.Model.Id);
		GetParentWindow()?.CloseWidget();
	}

	internal override void AddSettingEvents()
	{
		base.AddSettingEvents();
		base._currentWidgetSettings.HideCompleteChanged -= OnSettingsHideCompleteChanged;
		base._currentWidgetSettings.HideCompleteChanged += OnSettingsHideCompleteChanged;
		base._currentWidgetSettings.SortTypeChanged -= OnSortTypeChanged;
		base._currentWidgetSettings.SortTypeChanged += OnSortTypeChanged;
		base._currentWidgetSettings.GroupTypeChanged -= OnGroupTypeChanged;
		base._currentWidgetSettings.GroupTypeChanged += OnGroupTypeChanged;
		base._currentWidgetSettings.OrderDirectionChanged -= OnOrderDirectionChanged;
		base._currentWidgetSettings.OrderDirectionChanged += OnOrderDirectionChanged;
	}

	private async void OnGroupTypeChanged(object sender, string e)
	{
		base.Model.GroupType = e;
		await LoadProjectIdentity();
		LoadTasks();
	}

	private async void OnSortTypeChanged(object sender, string e)
	{
		base.Model.SortType = e;
		await LoadProjectIdentity();
		LoadTasks();
	}

	private async void OnOrderDirectionChanged(object sender, string e)
	{
		base.Model.Order = e;
		await LoadProjectIdentity();
		LoadTasks();
	}

	internal override void OnThemeChange(string themeKey)
	{
		TaskList.ItemsSource = null;
		if (_quickAddView != null)
		{
			ThemeUtil.SetTheme(themeKey, _quickAddView);
		}
		LoadTasks();
	}

	private void OnSettingsHideCompleteChanged(object sender, EventArgs e)
	{
		LoadTasks();
	}

	internal override void RemoveSettingEvent()
	{
		WidgetSettings widgetSettings = WidgetConfigHelper.TryGetSettings(base.Model);
		if (widgetSettings != null)
		{
			widgetSettings.HideCompleteChanged -= OnSettingsHideCompleteChanged;
			widgetSettings.SortTypeChanged -= OnSortTypeChanged;
			widgetSettings.GroupTypeChanged -= OnGroupTypeChanged;
		}
		base.RemoveSettingEvent();
	}

	public async Task SyncTask()
	{
		if (!Utils.IsNetworkAvailable())
		{
			TryToastString(null, Utils.GetString("NoNetwork"));
		}
		MorePath.Visibility = Visibility.Hidden;
		SyncButton.Visibility = Visibility.Hidden;
		((Storyboard)FindResource("SyncStory")).Begin();
		SyncManager.Sync(1);
	}

	private void OnSyncStoryCompleted(object sender, EventArgs e)
	{
		MorePath.Visibility = Visibility.Visible;
		SyncButton.Visibility = Visibility.Visible;
	}

	public async void OnItemOpenClick(DisplayItemModel item)
	{
		bool flag = ProjectIdentity is NormalProjectIdentity || ProjectIdentity is GroupProjectIdentity || ProjectIdentity is ParentTaskIdentity || ProjectIdentity is MatrixQuadrantIdentity;
		ChangeModelOpenStatus(item, flag);
		if (flag)
		{
			await TaskService.FoldTask(item.Id, item.IsOpen);
		}
		else
		{
			SmartListTaskFoldHelper.UpdateStatus(item.Id, ProjectIdentity.CatId, !item.IsOpen);
		}
		TaskChangeNotifier.NotifyTaskOpenChanged(item.Id);
	}

	public void SetShowCountDown(bool showCountDown)
	{
	}

	public async void NavigateProjectById(string projectId)
	{
		ProjectModel projectById = ProjectAndTaskIdsCache.GetProjectById(projectId);
		if (projectById != null)
		{
			ProjectTitle.Text = projectById.name;
			base.Model.Type = "normal";
			base.Model.Identity = projectId;
			await base.Model.Save();
			await LoadProjectIdentity();
			ResetQuickAddView();
			LoadTasks();
		}
	}

	public void NavigateTodayProject()
	{
	}

	public void NavigateTomorrowProject()
	{
	}

	public async void OnTaskProjectChanged(string taskId, ProjectModel project)
	{
		TryToastMoveControl(await TaskDao.GetThinTaskById(taskId), project);
	}

	private void OnSyncButtonClick(object sender, RoutedEventArgs e)
	{
		SyncTask();
	}

	public bool CanAddSubTask()
	{
		return !(ProjectIdentity is AssignToMeProjectIdentity);
	}

	public async void CreateSubtask(DisplayItemModel model)
	{
		if (ProjectIdentity is AssignToMeProjectIdentity)
		{
			return;
		}
		if (!model.IsOpen)
		{
			OnItemOpenClick(model);
		}
		if (await ProChecker.CheckTaskLimit(model.ProjectId))
		{
			return;
		}
		GetCurrentIndex(model.TaskId);
		TaskModel newTask = new TaskModel
		{
			id = Utils.GetGuid(),
			isAllDay = model.IsAllDay,
			kind = "TEXT",
			title = string.Empty,
			parentId = model.TaskId,
			projectId = model.ProjectId,
			sortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(model.ProjectId, model.Id, false),
			status = model.Status,
			columnId = model.ColumnId,
			pinnedTimeStamp = (model.IsPinned ? Utils.GetNowTimeStamp() : 0)
		};
		Section section = model.Section;
		DateTime? startDate = (section?.GetStartDate() ?? ProjectIdentity.GetTimeData()?.StartDate) ?? TaskDefaultDao.GetDefaultSafely().GetDefaultDateTime();
		newTask.startDate = startDate;
		newTask.isAllDay = true;
		newTask.dueDate = null;
		if (ProjectIdentity is FilterProjectIdentity filterProjectIdentity)
		{
			newTask.tag = TagSerializer.ToJsonContent(filterProjectIdentity.GetTags());
			newTask.priority = filterProjectIdentity.GetPriority();
		}
		else
		{
			newTask.tag = section?.GetTag();
		}
		newTask.priority = section?.GetPriority() ?? TaskDefaultDao.GetDefaultSafely().Priority;
		if (string.IsNullOrEmpty(newTask.tag) && ProjectIdentity is TagProjectIdentity tagProjectIdentity)
		{
			newTask.tag = "[\"" + tagProjectIdentity.Tag + "\"]";
		}
		newTask.assignee = section?.GetAssignee();
		newTask.status = model.Status;
		if (newTask.status != 0)
		{
			newTask.completedTime = DateTime.Now;
		}
		DisplayItemModel displayModel = new DisplayItemModel(new TaskBaseViewModel(newTask))
		{
			Section = section,
			Level = model.Level + 1
		};
		model.AddChild(displayModel, null);
		TaskModel taskModel = displayModel.GetTaskModel();
		taskModel.createdTime = DateTime.Now;
		await TaskService.AddTask(taskModel, setSortOrder: true, notify: true, this);
		await Task.Delay(10);
		if (taskModel.startDate.HasValue)
		{
			List<TaskReminderModel> defaultAllDayReminders = TimeData.GetDefaultAllDayReminders();
			foreach (TaskReminderModel item in defaultAllDayReminders)
			{
				item.taskserverid = taskModel.id;
				await TaskReminderDao.SaveReminders(item);
			}
		}
		DisplayItemModel displayItemModel = displayModel;
		displayItemModel.AvatarUrl = await AvatarHelper.GetAvatarUrl(section?.GetAssignee(), newTask.projectId);
		OnSubTaskAdded(taskModel.id);
	}

	public async void OnSubTaskAdded(string taskModelId)
	{
		await Task.Delay(50);
		DisplayItemModel displayItemModel = DisplayItemModels.FirstOrDefault((DisplayItemModel i) => i.Id == taskModelId);
		if (displayItemModel != null)
		{
			WidgetTaskItem widgetTaskItem = Utils.FindSingleVisualChildren<WidgetTaskItem>((ListViewItem)TaskList.ItemContainerGenerator.ContainerFromItem(displayItemModel));
			if (widgetTaskItem != null)
			{
				TaskList.ScrollIntoView(displayItemModel);
				widgetTaskItem.TryShowDisplayWindow(displayItemModel, byMouse: false, focusTitle: true);
			}
		}
	}

	private int GetCurrentIndex(string taskId)
	{
		if (DisplayItemModels != null && DisplayItemModels.Count > 0)
		{
			for (int i = 0; i < DisplayItemModels.Count; i++)
			{
				if (DisplayItemModels[i].Id == taskId)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public void PopupCloseTask(DisplayItemModel model, WidgetTaskItem ele)
	{
		if (model.IsTask && model.Enable)
		{
			Point position = Mouse.GetPosition(ele);
			SetClosedStatusPopup instance = SetClosedStatusPopup.GetInstance(null, model.IsCompleted, model.IsAbandoned, 15.0 - position.Y, 15.0 - position.X);
			instance.Resources = GetParentWindow().Resources;
			instance.Abandoned += async delegate
			{
				await TaskService.SetTaskStatus(model.TaskId, -1, needUndo: true, this);
			};
			instance.Completed += async delegate
			{
				bool playSound = await ele.TryPlayCompleteStory(model);
				await TaskService.SetTaskStatus(model.TaskId, 2, needUndo: true, this, inDetail: false, playSound);
			};
			instance.Show();
		}
	}

	public void PostPoneAll()
	{
		if (new CustomerDialog(Utils.GetString("PostponeToTodayTitle"), Utils.GetString("PostponeToTodayMsg"), Utils.GetString("Postpone"), Utils.GetString("Cancel"), GetParentWindow()).ShowDialog() == true)
		{
			PostponeUndoModel postponeUndoModel = new PostponeUndoModel();
			List<TaskBaseViewModel> allOutDateTasks = TaskCache.GetAllOutDateTasks();
			if (LocalSettings.Settings.ShowSubtasks)
			{
				allOutDateTasks.AddRange(TaskDetailItemCache.GetAllOutDateItems());
			}
			if (postponeUndoModel.InitData(allOutDateTasks))
			{
				postponeUndoModel.Do();
				UndoToast uiElement = new UndoToast(postponeUndoModel);
				WindowToastHelper.ShowAndHideToast(UndoToastGrid, uiElement);
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
		return await GetCompleteText(model);
	}

	private async Task<string> GetCompleteText(DisplayItemModel model)
	{
		if (ProjectIdentity is TodayProjectIdentity)
		{
			List<TaskBaseViewModel> source = _displayModels.ToList();
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

	public void ResetProject()
	{
		ResetQuickAddView();
		ProjectTitle.Text = ProjectIdentity.GetDisplayTitle();
	}

	public void OpenOrCloseSection(SectionStatus status)
	{
		List<DisplayItemModel> models = DisplayItemModels.ToList();
		DisplayItemModel displayItemModel = models.FirstOrDefault((DisplayItemModel item) => item.Type == DisplayType.Section && item.Section.SectionId == status.SectionId);
		if (displayItemModel != null)
		{
			List<DisplayItemModel> list = new List<DisplayItemModel>();
			foreach (DisplayItemModel child in displayItemModel.Section.Children)
			{
				list.Add(child);
				if (child.IsOpen && child.IsTask)
				{
					List<DisplayItemModel> childrenModels = child.GetChildrenModels(getAll: false);
					list.AddRange(childrenModels);
				}
			}
			string name = displayItemModel.Title;
			if (displayItemModel.Section is AssigneeSection assigneeSection)
			{
				name = assigneeSection.Assingee;
			}
			if (status.IsOpen)
			{
				SectionStatusDao.OpenProjectSection(ProjectIdentity.Id, name);
				int num = models.IndexOf(displayItemModel);
				if (num >= 0 && models[num] != null)
				{
					for (int num2 = 0; num2 < list.Count; num2++)
					{
						models.Insert(num + 1 + num2, list[num2]);
					}
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(ProjectIdentity.Id))
				{
					SectionStatusDao.CloseProjectSection(ProjectIdentity.Id, name);
				}
				list.ForEach(delegate(DisplayItemModel child)
				{
					models.Remove(child);
				});
				if (status.SectionId == new CompletedSection().SectionId && models[models.Count - 1].Type == DisplayType.LoadMore)
				{
					models.RemoveAt(models.Count - 1);
				}
			}
			ItemsSourceHelper.SetItemsSource(TaskList, models);
		}
		DataChangedNotifier.NotifyListSectionOpenChanged(this, (ProjectIdentity.CatId, ProjectIdentity.SortOption));
	}

	private void OnAddPreviewDown(object sender, MouseButtonEventArgs e)
	{
		if (_quickAddView != null)
		{
			TryHideAddView();
			e.Handled = true;
			Mouse.Capture(null);
		}
	}

	private void OnMouseClick(object sender, MouseButtonEventArgs e)
	{
		TryHideAddView();
	}

	private async Task TryHideAddView()
	{
		if (_quickAddView == null || _quickAddView.IsMouseOver || AddButton.IsMouseOver)
		{
			return;
		}
		if (!LocalSettings.Settings.ExtraSettings.PwShowAdd)
		{
			if (string.IsNullOrEmpty(_quickAddView.ContentText.Text) && string.IsNullOrEmpty(_quickAddView.TitleText.EditBox.Text))
			{
				await _quickAddView.TryAddTaskOnLostFocus();
				HideAddView();
			}
			return;
		}
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			FocusManager.SetFocusedElement(window, window);
			Keyboard.Focus(window);
		}
	}

	public async void ResetCount()
	{
		int num = await TaskCountCache.TryGetCount(ProjectIdentity);
		base.Model.CollapseTitle = ProjectTitle.Text + ((num > 0) ? (" (" + num + ")") : string.Empty);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/projectwidget.xaml", UriKind.Relative);
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
			ProjectWindow = (ProjectWidget)target;
			break;
		case 2:
			((Storyboard)target).Completed += OnSyncStoryCompleted;
			break;
		case 3:
			WidgetBackground = (Border)target;
			break;
		case 4:
			WidgetGrid = (Grid)target;
			break;
		case 5:
			((Grid)target).PreviewMouseLeftButtonDown += base.OnDragBarMouseDown;
			((Grid)target).MouseMove += base.OnDragMove;
			break;
		case 6:
			TitleGrid = (Grid)target;
			break;
		case 7:
			TitlePanel = (StackPanel)target;
			TitlePanel.MouseLeftButtonUp += OnProjectClick;
			break;
		case 8:
			ProjectTitle = (EmjTextBlock)target;
			break;
		case 9:
			ChoseProjectGrid = (Grid)target;
			break;
		case 10:
			LockedOptionGrid = (Grid)target;
			break;
		case 11:
			UnlockWidgetButton = (Button)target;
			UnlockWidgetButton.Click += base.OnUnlockWidgetClick;
			break;
		case 12:
			SyncButton = (Button)target;
			SyncButton.Click += OnSyncButtonClick;
			break;
		case 13:
			OptionsGrid = (StackPanel)target;
			break;
		case 14:
			AddButton = (Button)target;
			AddButton.PreviewMouseLeftButtonDown += OnAddPreviewDown;
			AddButton.Click += OnAddTaskClick;
			break;
		case 15:
			SyncPath = (Path)target;
			break;
		case 16:
			MorePath = (Button)target;
			MorePath.Click += OnMoreClick;
			break;
		case 17:
			TaskAddGrid = (ContentControl)target;
			break;
		case 18:
			EmptyText = (TextBlock)target;
			break;
		case 19:
			TaskListGrid = (Grid)target;
			break;
		case 20:
			TaskList = (ListView)target;
			break;
		case 21:
			UndoToastGrid = (Grid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
