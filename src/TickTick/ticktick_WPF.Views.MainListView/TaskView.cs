using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Coaches;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Summary;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Views.MainListView;

public class TaskView : Grid
{
	private TaskDetailView _taskDetailView;

	private BatchDetailView _batchDetail;

	private DetailHintPanel _hintPanel = new DetailHintPanel(isTask: true);

	private Task _taskDetailNavigationTask;

	private string _pendingTaskDetailId;

	private string _pendingTaskDetailItemId;

	private HabitDetailControl _habitDetailControl;

	private CourseDetailControl _courseDetail;

	private CalendarDetailControl _calendarDetail;

	private ProjectActivityPanel _projectActivityPanel;

	private bool _showParallelWidthCal;

	private bool _showInCal;

	private KanbanContainer _kanban;

	private SummaryControl _summary;

	private ProjectTaskListView _taskListView;

	private TimelineContainer _timeline;

	private ColumnDefinition _firstColumn;

	private ColumnDefinition _secondColumn;

	private bool _hideSecondColumn;

	private readonly Border _listContent = new Border();

	private readonly Border _detailContent = new Border
	{
		RenderTransform = new TranslateTransform()
	};

	private GridSplitter _rightSplit;

	public ProjectIdentity ProjectIdentity;

	private bool _needHideDetail;

	private bool _hideMenu;

	private bool _oneColumn;

	private bool _showProjectMenu;

	public bool ShowInCal => _showInCal;

	public bool ShowInParallel => _showParallelWidthCal;

	public bool IsFoldColumn => _hideSecondColumn;

	private void SetTaskDetailView()
	{
		if (_taskDetailView == null || !object.Equals(_detailContent.Child, _taskDetailView))
		{
			RemoveDetailEvent();
			_taskDetailView = new TaskDetailView(Constants.DetailMode.Page);
			_detailContent.Child = _taskDetailView;
			_taskDetailView.ShowUndoOnTaskDeleted += OnTaskDeletedInDetail;
			_taskDetailView.CheckItemsDeleted += OnCheckItemsDeleted;
			_taskDetailView.NavigateTask += OnNavigateTask;
			_taskDetailView.TaskCopied += OnTaskCopied;
			_taskDetailView.FocusList += TryFocusSelectedItem;
			_taskDetailView.TaskNavigated += OnDetailTaskNavigated;
			_taskDetailView.EnterImmersive += OnEnterImmersive;
		}
		if (_hideSecondColumn)
		{
			_taskDetailView.ShowBackMenu();
		}
	}

	public async void ShowTaskDetail(string taskId, string itemId = null, bool showWithAnim = true)
	{
		if (_hideSecondColumn && _showParallelWidthCal)
		{
			ClearDetail();
			return;
		}
		SetTaskDetailView();
		_pendingTaskDetailId = taskId;
		_pendingTaskDetailItemId = itemId ?? string.Empty;
		_taskDetailNavigationTask = _taskDetailView.TaskSelect(taskId, itemId);
		if (!base.IsLoaded)
		{
			await Task.Delay(200);
		}
		if (showWithAnim)
		{
			SetDetailVisibleWithAnimation(show: true);
		}
	}

	private bool IsDuplicatePendingTaskDetailSelection(string taskId, string itemId)
	{
		if (_taskDetailView == null || !object.Equals(_detailContent.Child, _taskDetailView))
		{
			return false;
		}
		itemId = itemId ?? string.Empty;
		if (_taskDetailNavigationTask != null && !_taskDetailNavigationTask.IsCompleted && _pendingTaskDetailId == taskId)
		{
			return _pendingTaskDetailItemId == itemId;
		}
		return false;
	}

	private void ShowBatchSelectView(List<string> selected, bool forceShow = true)
	{
		if (_batchDetail == null || !object.Equals(_detailContent.Child, _batchDetail))
		{
			RemoveDetailEvent();
			_batchDetail = new BatchDetailView(this);
			_detailContent.Child = _batchDetail;
		}
		if (_hideSecondColumn && forceShow && LocalSettings.Settings.ExtraSettings.TaskDetailLayout != "Popup")
		{
			SetDetailVisibleWithAnimation(show: true);
		}
		else if (_hideSecondColumn)
		{
			_taskListView?.SetBatchSelectedControl(selected);
			return;
		}
		_taskListView?.SetBatchSelectedControl(null);
		_batchDetail.OnBatchSelect(selected);
		_batchDetail.SetUseCardStyle(!_showParallelWidthCal && !_hideSecondColumn, _hideSecondColumn);
	}

	private void RemoveDetailEvent()
	{
		_taskDetailNavigationTask = null;
		_pendingTaskDetailId = null;
		_pendingTaskDetailItemId = null;
		_taskDetailView?.Dispose();
		_taskDetailView?.RemoveKeyBinding();
		_taskDetailView = null;
		_habitDetailControl?.ClearEvent();
		_habitDetailControl = null;
		_courseDetail?.ClearEvent();
		_courseDetail = null;
		_calendarDetail?.ClearEvent();
		_calendarDetail = null;
		_projectActivityPanel?.ClearEvent();
		_projectActivityPanel = null;
		_batchDetail?.Dispose();
		_batchDetail = null;
		_taskListView?.SetBatchSelectedControl(null);
		_detailContent.Child = null;
	}

	private async void OnNavigateTask(object sender, ProjectTask task)
	{
		await TaskDetailWindow.NavigateProjectTask(task, GetToastWindow());
	}

	private void OnCheckItemsDeleted(object sender, TaskDetailItemModel model)
	{
		UndoToast undoToast = new UndoToast();
		undoToast.InitSubtaskUndo(model);
		GetToastWindow()?.Toast(undoToast);
	}

	private async void OnTaskDeletedInDetail(object sender, string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(taskId, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				GetToastWindow()?.BatchDeleteTask(list);
			}
			else
			{
				GetToastWindow()?.TaskDeleted(taskId);
			}
		}
	}

	private void OnTaskCopied(object sender, string taskId)
	{
		_taskListView?.SetSelectedId(taskId);
	}

	private void OnDetailTaskNavigated(object sender, string taskId)
	{
		_taskListView?.OnDetailTaskNavigated(taskId);
	}

	private void OnEnterImmersive(string taskId, int caretIndex)
	{
		Utils.FindParent<IListViewParent>(this)?.EnterImmersiveMode(taskId, caretIndex);
	}

	private void TryFocusSelectedItem(object sender, EventArgs e)
	{
		_taskListView?.TryFocusSelectedItem();
	}

	private async void NavigateHabit(string habitId, bool pullRemote = true, bool force = true)
	{
		if (!_showParallelWidthCal || !_hideSecondColumn)
		{
			SetHabitDetailView();
			UserActCollectUtils.AddClickEvent("habit", "habit_detail_location", "show_in_task");
			_habitDetailControl.Load(habitId, DateTime.Today, pullRemote, force, useCardStyle: false);
			_habitDetailControl.ScrollToTop();
			SetDetailVisibleWithAnimation(show: true);
		}
	}

	private void SetHabitDetailView()
	{
		if (_habitDetailControl == null || !object.Equals(_detailContent.Child, _habitDetailControl))
		{
			RemoveDetailEvent();
			_habitDetailControl = new HabitDetailControl
			{
				Margin = new Thickness(0.0, 25.0, 0.0, 0.0)
			};
			_habitDetailControl.HideDetail += OnHideDetail;
			_detailContent.Child = _habitDetailControl;
			if (_hideSecondColumn)
			{
				_habitDetailControl.ShowBackMenu();
			}
		}
	}

	private void OnHideDetail(object sender, EventArgs e)
	{
		TryExtractDetail();
	}

	public async Task ReloadHabit()
	{
		if (_habitDetailControl == null)
		{
			SetHabitDetailView();
		}
		_habitDetailControl.Reload();
	}

	private async void NavigateCourse(string courseId)
	{
		if ((!_showParallelWidthCal || !_hideSecondColumn) && !string.IsNullOrEmpty(courseId))
		{
			RemoveDetailEvent();
			SetCourseDetail();
			_courseDetail.NavigateCourse(courseId);
			SetDetailVisibleWithAnimation(show: true);
		}
	}

	private void SetCourseDetail()
	{
		if (_courseDetail == null || !object.Equals(_detailContent.Child, _courseDetail))
		{
			RemoveDetailEvent();
			_courseDetail = new CourseDetailControl
			{
				Margin = new Thickness(0.0, 25.0, 0.0, 0.0)
			};
			_courseDetail.HideDetail += OnHideDetail;
			_detailContent.Child = _courseDetail;
			if (_hideSecondColumn)
			{
				_courseDetail.ShowBackMenu();
			}
		}
	}

	private async void OnScheduleChanged(object sender, EventArgs e)
	{
		if (_courseDetail != null)
		{
			if (await ScheduleDao.GetCoursedByIdAsync(_courseDetail.CourseId) != null)
			{
				_courseDetail.Reload();
			}
			else
			{
				TryExtractDetail();
			}
		}
		base.Dispatcher.Invoke(delegate
		{
			if (ProjectIdentity is TodayProjectIdentity || ProjectIdentity is TomorrowProjectIdentity || ProjectIdentity is WeekProjectIdentity)
			{
				Reload();
			}
		});
	}

	private void SetCalendarDetail()
	{
		if (_calendarDetail == null || !object.Equals(_detailContent.Child, _calendarDetail))
		{
			RemoveDetailEvent();
			_calendarDetail = new CalendarDetailControl
			{
				Margin = new Thickness(0.0, 25.0, 0.0, 0.0)
			};
			_calendarDetail.HideDetail += OnHideDetail;
			_detailContent.Child = _calendarDetail;
			if (_hideSecondColumn)
			{
				_calendarDetail.ShowBackMenu();
			}
		}
	}

	private void ShowEventDetail(string eventId)
	{
		if (!_showParallelWidthCal || !_hideSecondColumn)
		{
			NavigateEvent(eventId);
		}
	}

	public async Task NavigateEvent(string eventId)
	{
		if (string.IsNullOrEmpty(eventId))
		{
			return;
		}
		SetCalendarDetail();
		string displayEventId = eventId;
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
			return;
		}
		SwitchTaskList();
		_taskListView?.SetSelectedId(displayEventId);
		IEnumerable<DisplayItemModel> enumerable = _taskListView?.GetItems();
		if (enumerable != null)
		{
			List<DisplayItemModel> source = enumerable.ToList();
			if (source.Any())
			{
				DisplayItemModel displayItemModel = source.FirstOrDefault((DisplayItemModel model) => model.Id == displayEventId);
				if (displayItemModel != null)
				{
					calendarEventModel2.DueStart = displayItemModel.StartDate;
					calendarEventModel2.DueEnd = displayItemModel.DueDate;
				}
			}
		}
		if (_calendarDetail != null)
		{
			_calendarDetail.DataContext = new CalendarDetailViewModel(calendarEventModel2);
		}
		SetDetailVisibleWithAnimation(show: true);
	}

	private void SetProjectActivity()
	{
		if (_projectActivityPanel == null || !object.Equals(_detailContent.Child, _projectActivityPanel))
		{
			RemoveDetailEvent();
			_projectActivityPanel = new ProjectActivityPanel(_hideSecondColumn);
			_projectActivityPanel.HideDetail += OnHideDetail;
			_detailContent.Child = _projectActivityPanel;
			if (_hideSecondColumn)
			{
				_projectActivityPanel.SetBachPathVisible(show: true);
			}
		}
	}

	private async void ToggleProjectActivityPanel(string projectId)
	{
		if (ProChecker.CheckPro(ProType.ListActivities))
		{
			if (!Utils.IsNetworkAvailable())
			{
				TryToast(Utils.GetString("NoNetwork"));
			}
			else if (_showParallelWidthCal && _hideSecondColumn)
			{
				ProjectActivityWindow projectActivityWindow = new ProjectActivityWindow(projectId);
				projectActivityWindow.Owner = Window.GetWindow(this);
				projectActivityWindow.Show();
			}
			else
			{
				SetProjectActivity();
				_projectActivityPanel.Init(projectId);
				SetDetailVisibleWithAnimation(show: true);
			}
		}
	}

	public void HideDetailOnTasksDelete(List<string> taskIds)
	{
		if (taskIds.Contains(_taskDetailView?.TaskId))
		{
			HideDetail();
		}
		else
		{
			_taskDetailView?.OnTasksDeleted(taskIds);
		}
	}

	public bool DetailMouseOver()
	{
		if (_taskDetailView != null && _taskDetailView.IsVisible)
		{
			return _taskDetailView.CheckMouseOver();
		}
		return false;
	}

	public bool DetailMouseMouseOver(MouseEventArgs e)
	{
		if (_taskDetailView != null && _taskDetailView.IsVisible)
		{
			Point position = e.GetPosition(_taskDetailView);
			if (position.X > 0.0)
			{
				return position.X < _taskDetailView.ActualWidth;
			}
			return false;
		}
		return false;
	}

	private void OnHabitArchived(object sender, string e)
	{
		if (_habitDetailControl != null && _habitDetailControl.GetHabitId() == e)
		{
			TryExtractDetail();
		}
	}

	public void ClearDetailParseDate()
	{
		_taskDetailView?.ClearParseDate();
	}

	public bool DetailTitleFocus()
	{
		return _taskDetailView?.TitleFocused ?? false;
	}

	public TaskDetailView GetTaskDetail()
	{
		return _taskDetailView;
	}

	public void SetEmptyImageVisible(bool showHint, bool isTask, bool isNote)
	{
		_hintPanel.Opacity = (showHint ? 1 : 0);
	}

	public void ShowDetailBackMenu()
	{
		_taskDetailView?.ShowBackMenu();
		_calendarDetail?.ShowBackMenu();
		_habitDetailControl?.ShowBackMenu();
		_courseDetail?.ShowBackMenu();
		_projectActivityPanel?.SetBachPathVisible(show: true);
	}

	public void HideDetailBackMenu()
	{
		_taskDetailView?.HideBackMenu();
		_calendarDetail?.HideBackMenu();
		_habitDetailControl?.HideBackMenu();
		_courseDetail?.HideBackMenu();
		_projectActivityPanel?.SetBachPathVisible(show: false);
	}

	public bool TaskDetailFocus()
	{
		return _taskDetailView?.TextFocus() ?? false;
	}

	private void ClearDetail()
	{
		RemoveDetailEvent();
		SetDetailHintVisible(showHint: true);
		_detailContent.Child = _hintPanel;
	}

	private string GetDetailId()
	{
		if (_detailContent.Child == _hintPanel)
		{
			return null;
		}
		if (_taskDetailView != null)
		{
			return _taskDetailView.TaskId;
		}
		if (_calendarDetail != null)
		{
			return _calendarDetail.EventId;
		}
		if (_habitDetailControl != null)
		{
			return _habitDetailControl.GetHabitId();
		}
		if (_courseDetail != null)
		{
			return _courseDetail.CourseId;
		}
		return null;
	}

	public void InValidDetailMargin()
	{
		_taskDetailView?.InvalidContentMargin();
	}

	public async Task SwitchKanbanAndLoad(ProjectIdentity identity, bool cancelOperation = true, bool projectSelect = false, string taskId = null, string columnId = null)
	{
		Utils.FindParent<MainWindow>(this)?.SetMainWindowsMinSize(isList: false);
		if (_kanban == null)
		{
			InitKanban(identity);
		}
		if (cancelOperation)
		{
			_kanban.CancelOperation();
		}
		await _kanban.Load(identity, projectSelect, needPull: true, taskId);
		if (identity is NormalProjectIdentity { Project: not null } normalProjectIdentity && await TaskService.AdjustKanbanData(normalProjectIdentity.Project.id))
		{
			await _kanban.Load(identity, forceReload: false, needPull: true);
		}
		SetDisplayMode("kanban");
		if (!base.Children.Contains(_kanban))
		{
			base.Children.Add(_kanban);
		}
		if (!string.IsNullOrEmpty(columnId))
		{
			_kanban.ScrollToColumn(columnId);
		}
	}

	private void InitKanban(ProjectIdentity project)
	{
		if (_kanban == null)
		{
			_kanban = new KanbanContainer(project);
			_kanban.SetValue(Grid.ColumnProperty, 0);
			_kanban.SetValue(Grid.ColumnSpanProperty, 2);
			_kanban.SetValue(Panel.ZIndexProperty, 10);
		}
		if (_showParallelWidthCal)
		{
			_kanban.SetBackGround(transparent: true);
		}
		_kanban.SetFoldMenuIcon(_hideMenu, _oneColumn);
		if (!_showProjectMenu)
		{
			_kanban.HideFoldMenuIcon();
		}
	}

	private void ClearKanban()
	{
		base.Children.Remove(_kanban);
	}

	public void SwitchSummary()
	{
		SetDisplayMode("Summary");
		if (_summary == null)
		{
			InitSummary();
		}
		else
		{
			_summary.LoadData();
		}
	}

	private void InitSummary()
	{
		_summary = new SummaryControl();
		_summary.SetResourceReference(Panel.BackgroundProperty, "ShowAreaBottomColor");
		_summary.SetFoldMenuIcon(_hideMenu, _oneColumn, _showInCal);
		base.Children.Add(_summary);
		_summary.SetValue(Grid.ColumnProperty, 0);
		_summary.SetValue(Grid.ColumnSpanProperty, 2);
		_summary.SetValue(Panel.ZIndexProperty, 8);
	}

	private void ClearSummary()
	{
		base.Children.Remove(_summary);
		_summary = null;
	}

	public void SwitchTaskList()
	{
		SetDisplayMode("list");
	}

	private void OpenProjectActivity(object sender, string projectId)
	{
		ToggleProjectActivityPanel(projectId);
	}

	private void OnTaskTaskListBatchChanged(object sender, List<string> taskIds)
	{
		HideDetail();
	}

	private void OnSearchFilterChanged(object sender, EventArgs e)
	{
		HideDetail();
	}

	private void OnTaskListTaskAdded(object sender, TaskModel task)
	{
		ShowTaskDetail(task.id);
		UtilLog.Info("OnTaskListTaskAdded " + _hideSecondColumn + "  " + _showParallelWidthCal + "  " + (_taskDetailView == null));
	}

	private void OnBatchSelect(object sender, (List<string>, bool) selected)
	{
		if (selected.Item1 == null || selected.Item1.Count == 0)
		{
			HideDetail();
			_taskListView?.SetBatchSelectedControl(null);
		}
		else
		{
			ShowBatchSelectView(selected.Item1, selected.Item2);
		}
	}

	private void EventAdded(object sender, string e)
	{
		ShowEventDetail(e);
	}

	public void TaskSelect(ListItemSelectModel model)
	{
		if (model.Type != TaskSelectType.Click && _hideSecondColumn && (!_hideSecondColumn || _detailContent.Effect == null))
		{
			return;
		}
		switch (model.ItemType)
		{
		case DisplayType.Task:
		case DisplayType.CheckItem:
		case DisplayType.Agenda:
		case DisplayType.Note:
			if (model.Type != TaskSelectType.Click || !IsDuplicatePendingTaskDetailSelection(model.Id, model.ChildId))
			{
				ShowTaskDetail(model.Id, model.ChildId);
			}
			else
			{
				UtilLog.Info("TaskDetail.Navigate skip duplicate click TaskId: " + model.Id + ", ItemId: " + model.ChildId);
			}
			break;
		case DisplayType.Event:
			ShowEventDetail(model.Id);
			break;
		case DisplayType.Habit:
			NavigateHabit(model.Id);
			break;
		case DisplayType.Course:
			NavigateCourse(model.Id);
			break;
		case DisplayType.Derivative:
		case DisplayType.LoadMore:
		case DisplayType.Section:
		case DisplayType.Pomo:
			break;
		}
	}

	private void CheckIdentityColumns(ProjectIdentity identity)
	{
		if (identity is NormalProjectIdentity normalProjectIdentity)
		{
			ProjectModel projectById = CacheManager.GetProjectById(normalProjectIdentity.Id);
			if (projectById != null && identity.SortOption.groupByType == Constants.SortType.sortOrder)
			{
				ColumnBatchHandler.MergeWithServer(projectById.id);
			}
		}
	}

	public async Task SwitchListAndLoad(ProjectIdentity identity, bool hideDetail = false)
	{
		CheckIdentityColumns(identity);
		SwitchTaskList();
		await _taskListView.LoadProject(identity, forceRefresh: false, forceLoad: true);
		if (hideDetail)
		{
			HideDetail();
		}
	}

	public void FocusQuickAdd()
	{
		_taskListView?.FocusQuickAdd();
	}

	public bool CheckTaskInList(string taskId)
	{
		return _taskListView?.ExistTask(taskId) ?? true;
	}

	public void ReloadTaskListAndSelect(bool forceReload = false, string taskId = "", bool forceFocus = true)
	{
		_taskListView?.ReloadTask(forceReload, taskId, forceFocus);
		ShowTaskDetail(taskId);
	}

	public void ReSearch()
	{
		base.Dispatcher.Invoke(delegate
		{
			_taskListView?.ReSearch();
		});
	}

	public void SelectId(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		DisplayItemModel displayItemModel = _taskListView?.SetSelectedId(id);
		if (displayItemModel != null)
		{
			if (displayItemModel.Type == DisplayType.Event)
			{
				NavigateEvent(displayItemModel.Id);
			}
			else
			{
				ShowTaskDetail(displayItemModel.Id);
			}
		}
	}

	public ProjectTaskListView GetTaskListView()
	{
		return _taskListView;
	}

	public void ClearBatchSelect()
	{
		_taskListView?.ViewModel?.ClearBatchSelect();
		TryExtractDetail();
	}

	public void ExitBatchSelect()
	{
		_taskListView?.ViewModel.ExitBatchSelect();
		_taskListView?.HideBatchSelectControl();
		_kanban?.CancelBatchSelect();
		_timeline?.CancelBatchSelect();
	}

	private void InitTimeline()
	{
		App.Instance.LoadTimelineStyle();
		_timeline = new TimelineContainer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_timeline.SetInitWidth(base.ActualWidth - 90.0 - _firstColumn.ActualWidth);
		base.Children.Add(_timeline);
		if (!_showProjectMenu)
		{
			_timeline.HideFoldMenuIcon();
		}
		if (_showParallelWidthCal)
		{
			_timeline.SetBackground(transparent: true);
		}
		_timeline.SetFoldMenuIcon(_hideMenu, _oneColumn);
		_timeline.SetValue(Grid.ColumnProperty, 0);
		_timeline.SetValue(Grid.ColumnSpanProperty, 2);
		_timeline.SetValue(Panel.ZIndexProperty, 10);
	}

	public void SwitchTimelineAndLoad(ProjectIdentity project)
	{
		if (!UserDao.IsPro())
		{
			SwitchListAndLoad(project, hideDetail: true);
		}
		else
		{
			SwitchTimeline(project);
		}
	}

	public async Task SwitchTimeline(ProjectIdentity projectProject, string taskId = null)
	{
		SetDisplayMode("timeline");
		if (_timeline == null)
		{
			InitTimeline();
		}
		else if (!base.Children.Contains(_timeline))
		{
			base.Children.Add(_timeline);
		}
		await _timeline.LoadProject(projectProject, taskId);
		ShowTimelineGuide(projectProject.CatId);
	}

	private async Task ShowTimelineGuide(string pId)
	{
		if (LocalSettings.Settings.UserPreference.Timeline.ShowGuide)
		{
			LocalSettings.Settings.SetTimelineShowGuide();
			SettingsHelper.PushLocalPreference();
			LocalSettings.Settings.Save();
			if (!LocalSettings.Settings.ExtraSettings.TLUsed)
			{
				await Task.Delay(750);
				LocalSettings.Settings.ExtraSettings.TLUsed = true;
				LocalSettings.Settings.Save();
				UserActCollectUtils.AddClickEvent("timeline", "user_guide", "feature_introduction");
				await FeatureInfoWindow.Show(Utils.GetString("TimeLine"), Utils.GetString("TimeLineProSummary"), Utils.GetString("TimeLineExtraProSummary"), "TimeLine" + (Utils.IsCn() ? "_cn" : "_en"), Utils.GetString("EnjoyNow"), null, isGif: true);
			}
			if (!CacheManager.GetProjects().Any((ProjectModel p) => pId != p.id && p.viewMode == "timeline") && !CacheManager.GetProjectGroups().Any((ProjectGroupModel g) => pId != g.id && g.viewMode == "timeline") && !CacheManager.GetFilters().Any((FilterModel f) => pId != f.id && f.viewMode == "timeline"))
			{
				MainWindow window = App.Window;
				window.BeginCoach(new CoachControl(await CoachModel.GetTimeLineCoach()));
			}
		}
	}

	private void ClearTimeline()
	{
		base.Children.Remove(_timeline);
	}

	public void ClearCopiedTaskSelectionOnWindowMinimized()
	{
		_timeline?.ClearCopiedTaskSelection();
	}

	public TaskView(bool showProjectMenu = true)
	{
		_showProjectMenu = showProjectMenu;
		_firstColumn = new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star),
			MinWidth = 320.0
		};
		_secondColumn = new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star),
			MinWidth = 312.0
		};
		base.ColumnDefinitions.Add(_firstColumn);
		base.ColumnDefinitions.Add(_secondColumn);
		_listContent.SetValue(Grid.ColumnProperty, 0);
		base.Children.Add(_listContent);
		_detailContent.SetValue(Grid.ColumnProperty, 1);
		base.Children.Add(_detailContent);
		SetResourceReference(Panel.BackgroundProperty, "ShowAreaBottomColor");
		_listContent.Background = Brushes.Transparent;
		_detailContent.Background = Brushes.Transparent;
		Line line = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			Stretch = Stretch.Fill,
			StrokeThickness = 1.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		line.SetValue(Grid.ColumnProperty, 1);
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity5");
		line.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = _detailContent
		});
		base.Children.Add(line);
		_rightSplit = new GridSplitter
		{
			Width = 3.0,
			IsTabStop = false,
			Background = Brushes.Transparent,
			FocusVisualStyle = null,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_rightSplit.SetValue(Grid.ColumnProperty, 1);
		_rightSplit.SetValue(Panel.ZIndexProperty, 1000);
		base.Children.Add(_rightSplit);
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
		_rightSplit.DragCompleted += OnSplitDragCompleted;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ScheduleChanged -= OnScheduleChanged;
		DataChangedNotifier.HabitArchived -= OnHabitArchived;
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		DataChangedNotifier.EventArchivedChanged -= OnEventArchivedChanged;
		DataChangedNotifier.SortOptionChanged -= OnSortOptionChanged;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.ScheduleChanged += OnScheduleChanged;
		DataChangedNotifier.HabitArchived += OnHabitArchived;
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		TimeChangeNotifier.DayChanged += OnDayChanged;
		DataChangedNotifier.EventArchivedChanged += OnEventArchivedChanged;
		DataChangedNotifier.SortOptionChanged += OnSortOptionChanged;
		if (!_hideSecondColumn)
		{
			double num = Utils.FindParent<IListViewParent>(this)?.GetDetailWidth() ?? 1.0;
			num = ((num < 0.0) ? 1.0 : num);
			_secondColumn.Width = new GridLength(num, GridUnitType.Star);
		}
		Reload();
	}

	private void OnSortOptionChanged(object sender, string e)
	{
		if (ProjectIdentity?.CatId == e)
		{
			_kanban?.ReloadIdentity();
			_taskListView?.ReloadIdentity();
			_timeline?.ReloadSortOption();
		}
	}

	private async void OnEventArchivedChanged(object sender, EventArgs e)
	{
		Reload();
		bool flag = _calendarDetail != null;
		if (flag)
		{
			flag = await _calendarDetail.IsArchived();
		}
		if (flag)
		{
			TryExtractDetail();
		}
	}

	public void OnSplitDragCompleted(object sender, DragCompletedEventArgs e)
	{
		if (!_hideSecondColumn)
		{
			double num = _secondColumn.ActualWidth / _firstColumn.ActualWidth;
			Utils.FindParent<IListViewParent>(this)?.OnDetailWidthChanged(num);
			_secondColumn.Width = new GridLength((double.IsNaN(num) || double.IsInfinity(num)) ? 1.0 : num, GridUnitType.Star);
			_firstColumn.Width = new GridLength(1.0, GridUnitType.Star);
		}
	}

	private async void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		try
		{
			if ((e.DeletedChangedIds.Any() || e.UndoDeletedIds.Any()) && _taskDetailView != null)
			{
				await Task.Delay(30);
				if (e.DeletedChangedIds.Contains(_taskDetailView?.TaskId) || e.UndoDeletedIds.Contains(_taskDetailView?.TaskId))
				{
					HideDetail();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public void HideDetail(bool checkColumn = false)
	{
		if (!checkColumn || _hideSecondColumn)
		{
			_taskListView?.SetSelectedId(string.Empty);
			_taskListView?.SetBatchSelectedControl(null);
			ClearDetail();
		}
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		_taskListView?.ReloadDataAndQuickAddView();
		_kanban?.ReloadDataAndColumnQuickAddView();
		_timeline?.Reload();
		_taskDetailView?.Reload();
	}

	public new async void OnKeyUp(KeyEventArgs e)
	{
		Key key = e.Key;
		if (key == Key.Up || key == Key.Down)
		{
			await Task.Delay(50);
			if (e.Handled)
			{
				return;
			}
			if (Utils.IfShiftPressed())
			{
				_taskListView?.BatchSelectOnMove(e.Key == Key.Up);
			}
			else
			{
				_taskListView?.TryFocusSelectedItem();
			}
		}
		_timeline?.KeyDown(e.Key);
	}

	public void SetListColumnSpan(int span, double firstMinWidth, double secondMinWidth)
	{
		_listContent.SetValue(Grid.ColumnSpanProperty, span);
		_firstColumn.MinWidth = firstMinWidth;
		_secondColumn.MinWidth = secondMinWidth;
	}

	private void SetDisplayMode(string mode)
	{
		IListViewParent listViewParent = Utils.FindParent<IListViewParent>(this);
		switch (mode)
		{
		case "list":
			_listContent.Visibility = Visibility.Visible;
			_detailContent.Visibility = Visibility.Visible;
			_rightSplit.SetValue(Panel.ZIndexProperty, 1000);
			ClearKanban();
			ClearTimeline();
			ClearSummary();
			listViewParent?.SetMinSize(_showParallelWidthCal ? 680 : 390, _showParallelWidthCal ? 680 : 400);
			if (_taskListView == null)
			{
				_taskListView = new ProjectTaskListView(this);
				if (!_showProjectMenu)
				{
					_taskListView.HideFoldMenuIcon();
				}
				_listContent.Child = _taskListView;
				_taskListView.SetFoldMenuIcon(_hideMenu, _oneColumn);
				_taskListView.TaskSelect += TaskSelect;
				_taskListView.EventAdded += EventAdded;
				_taskListView.BatchTaskSelected += OnBatchSelect;
				_taskListView.TaskAdded += OnTaskListTaskAdded;
				_taskListView.OnSearchFilterChanged += OnSearchFilterChanged;
				_taskListView.BatchTaskChanged += OnTaskTaskListBatchChanged;
				_taskListView.OnProjectActivityClick += OpenProjectActivity;
			}
			else
			{
				_listContent.Child = _taskListView;
			}
			break;
		case "kanban":
			_listContent.Visibility = Visibility.Collapsed;
			_detailContent.Visibility = Visibility.Collapsed;
			_rightSplit.SetValue(Panel.ZIndexProperty, 0);
			ClearTimeline();
			ClearList();
			ClearSummary();
			listViewParent?.SetMinSize(680, _showParallelWidthCal ? 680 : 462);
			break;
		case "timeline":
			_listContent.Visibility = Visibility.Collapsed;
			_detailContent.Visibility = Visibility.Collapsed;
			ClearKanban();
			ClearList();
			ClearSummary();
			listViewParent?.SetMinSize(680, _showParallelWidthCal ? 680 : 462);
			break;
		case "Summary":
			_listContent.Visibility = Visibility.Collapsed;
			_detailContent.Visibility = Visibility.Collapsed;
			_rightSplit.SetValue(Panel.ZIndexProperty, 0);
			ClearTimeline();
			ClearList();
			ClearKanban();
			listViewParent?.SetMinSize(680, _showParallelWidthCal ? 680 : 462);
			break;
		}
	}

	private void ClearList()
	{
		HideDetail();
		_listContent.Child = null;
	}

	public void SetFoldMenuIcon(bool hideMenu, bool oneColumn = false)
	{
		_hideMenu = hideMenu;
		_oneColumn = oneColumn;
		_taskListView?.SetFoldMenuIcon(hideMenu, oneColumn);
		_kanban?.SetFoldMenuIcon(hideMenu, oneColumn);
		_timeline?.SetFoldMenuIcon(hideMenu, oneColumn);
		_summary?.SetFoldMenuIcon(hideMenu, oneColumn, _showInCal);
	}

	public void ReloadView(bool hideDetail = false)
	{
		if (ProjectIdentity != null)
		{
			string taskId = ((_taskListView == null || !_taskListView.IsVisible) ? null : (hideDetail ? null : (_taskListView?.GetSelectedId() ?? GetDetailId())));
			OnProjectSelect(ProjectIdentity.Copy(ProjectIdentity), taskId);
		}
	}

	public bool IsMenuPathMouseOver()
	{
		if (_taskListView != null)
		{
			return _taskListView.MenuPathBorder.IsMouseOver;
		}
		return false;
	}

	public void Clear()
	{
		_kanban?.ClearEvent();
		_taskListView?.Dispose();
		ClearDetail();
		if (_detailContent != null)
		{
			_detailContent.Child = null;
		}
		_batchDetail?.Dispose();
		_batchDetail = null;
		_timeline?.Dispose();
		_timeline = null;
		_kanban?.Dispose();
		_kanban = null;
	}

	public async void OnProjectSelect(ProjectIdentity identity, string taskId = null, bool showDetailAnim = false, string columnId = null)
	{
		if (identity == null)
		{
			return;
		}
		base.IsHitTestVisible = !identity.IsVirtual;
		bool projectChanged = identity.Id != ProjectIdentity?.Id || identity.CatId != ProjectIdentity?.CatId;
		ProjectIdentity = identity;
		if (!projectChanged && taskId == null)
		{
			taskId = ((_taskListView == null || !_taskListView.IsVisible) ? null : (_hideSecondColumn ? null : (_taskListView?.GetSelectedId() ?? GetDetailId())));
		}
		if (identity is GroupProjectIdentity groupProjectIdentity)
		{
			groupProjectIdentity.CheckSortOption();
		}
		string text = (_showInCal ? identity.ParallelViewMode : identity.ViewMode);
		if (!string.IsNullOrEmpty(columnId) && text != "kanban")
		{
			text = "list";
			identity.SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.sortOrder, inKanban: false);
		}
		if (identity is NormalProjectIdentity normalProjectIdentity)
		{
			if (normalProjectIdentity.IsNote && text == "timeline")
			{
				text = "list";
				if (_showInCal)
				{
					identity.ParallelViewMode = "list";
				}
				else
				{
					identity.ViewMode = "list";
				}
			}
			if (normalProjectIdentity.Project.userCount >= 2 || normalProjectIdentity.Project.needPullRemote)
			{
				TryPullRemoteTasks(normalProjectIdentity.Id);
			}
		}
		if (text == "kanban")
		{
			SwitchKanbanAndLoad(identity, cancelOperation: false, projectSelect: true, taskId, columnId);
			return;
		}
		if (text == "timeline" && UserDao.IsPro())
		{
			SwitchTimelineAndLoad(identity);
			if (!string.IsNullOrEmpty(taskId))
			{
				TaskDetailWindows.ShowTaskWindows(taskId);
			}
			return;
		}
		if (identity is SummaryProjectIdentity)
		{
			SwitchSummary();
			return;
		}
		CheckIdentityColumns(identity);
		SwitchTaskList();
		ProjectTaskListView taskListView = _taskListView;
		if (taskListView == null)
		{
			return;
		}
		bool inBatch = taskListView.ViewModel?.ProjectIdentity?.Id == identity.Id && _batchDetail != null;
		bool currentSelected = !string.IsNullOrEmpty(taskId) && _taskDetailView?.TaskId == taskId;
		if (!string.IsNullOrEmpty(taskId))
		{
			if (!currentSelected)
			{
				taskListView.SetSelectedId(taskId);
			}
		}
		else if (!inBatch || projectChanged)
		{
			taskListView.SetSelectedId(null);
		}
		await taskListView.LoadProject(identity, forceRefresh: false, forceLoad: true, columnId);
		if (projectChanged)
		{
			HideDetail();
		}
		if (!inBatch)
		{
			SelectAndShowDetail(taskId, showDetailAnim, !currentSelected);
		}
	}

	private async void SelectAndShowDetail(string taskId, bool showDetailAnim, bool scroll = true)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return;
		}
		await Task.Delay(100);
		_taskListView?.SetSelectedId(taskId, scroll);
		if (_calendarDetail == null)
		{
			if (_habitDetailControl != null)
			{
				_habitDetailControl.Reload();
			}
			else if (TaskCache.ExistTask(taskId))
			{
				ShowTaskDetail(taskId, null, showDetailAnim);
			}
		}
	}

	private async void TryPullRemoteTasks(string projectId)
	{
		if (await TaskService.PullTasksOfProject(projectId))
		{
			ReloadView();
		}
	}

	public string GetSelectedHabitId()
	{
		HabitDetailControl habitDetailControl = _habitDetailControl;
		if (habitDetailControl == null || habitDetailControl.Visibility != Visibility.Visible)
		{
			return string.Empty;
		}
		return _habitDetailControl.GetHabitId();
	}

	public bool IsEditable()
	{
		if (_taskDetailView != null)
		{
			return _taskDetailView.Enable();
		}
		if (_calendarDetail != null)
		{
			if (_calendarDetail != null)
			{
				return _calendarDetail.Editable;
			}
			return false;
		}
		return false;
	}

	public IToastShowWindow GetToastWindow()
	{
		return Utils.FindParent<IToastShowWindow>(this);
	}

	public void TryToast(string getString)
	{
		GetToastWindow()?.TryToastString(null, getString);
	}

	public void TryFoldDetail()
	{
		if (_hideSecondColumn)
		{
			_taskListView?.ForbidShowMenu();
			SetDetailVisibleWithAnimation(show: false);
		}
	}

	public void TryExtractDetail()
	{
		if (_hideSecondColumn)
		{
			_taskListView?.ForbidShowMenu();
			SetDetailVisibleWithAnimation(show: false);
			_taskListView?.SetSelectedId(string.Empty);
			_taskListView?.SetBatchSelectedControl(null);
			ClearDetail();
		}
		else
		{
			HideDetail();
		}
	}

	public void ResetSearchFilterControl()
	{
		_taskListView?.ResetSearchFilterControl();
	}

	public bool OnEsc()
	{
		if (_detailContent.Visibility == Visibility.Visible)
		{
			SetDetailVisibleWithAnimation(show: false);
			return true;
		}
		return false;
	}

	public void TryPrint(bool isDetail)
	{
		if (_taskListView != null && _taskListView.CanPrint())
		{
			_taskListView?.ShowPrintPreview(isDetail);
		}
	}

	public void TryBatchSetPriority(int priority)
	{
		_taskListView?.TryBatchSetPriority(priority).ContinueWith(UtilRun.LogTask);
		_kanban?.TryBatchSetPriority(priority).ContinueWith(UtilRun.LogTask);
	}

	public void BatchOpenSticky()
	{
		_taskListView?.BatchOpenSticky();
		_kanban?.BatchOpenSticky();
	}

	public void TryBatchSetDate(DateTime? date)
	{
		_taskListView?.TryBatchSetDate(date).ContinueWith(UtilRun.LogTask);
		_kanban?.TryBatchSetDate(date).ContinueWith(UtilRun.LogTask);
	}

	public void BatchPinTask()
	{
		_taskListView?.BatchPinTask();
		_kanban?.BatchPinTask();
	}

	public void BatchDeleteTask()
	{
		_taskListView?.DeleteSelectedTasks();
		_kanban?.BatchDeleteTask();
	}

	public bool TabListAndDetail(IInputElement focusItem)
	{
		if (focusItem is FrameworkElement child)
		{
			TaskDetailView taskDetailView = Utils.FindParent<TaskDetailView>(child);
			TaskListView taskListView = Utils.FindParent<TaskListView>(child);
			if (taskDetailView != null)
			{
				taskDetailView.TryTabInputBox();
				return true;
			}
			if (taskListView != null)
			{
				_taskDetailView?.TryFocusTitle();
				return true;
			}
		}
		return false;
	}

	public async Task ExpandOrFoldAllTask(bool isOpen)
	{
		if (_kanban != null && _kanban.IsVisible)
		{
			_kanban.ExpandOrFoldAllTask();
		}
		if (_taskListView != null && _taskListView.IsVisible)
		{
			_taskListView.ExpandOrFoldAllTask(isOpen);
		}
	}

	public async Task ExpandOrFoldAllSection()
	{
		if (_timeline != null && _timeline.IsVisible)
		{
			_timeline.ToggleGroupsOpen();
		}
		if (_kanban != null && _kanban.IsVisible)
		{
			_kanban.ExpandOrFoldAllSection();
		}
		if (_taskListView != null && _taskListView.IsVisible)
		{
			_taskListView.ExpandOrFoldAllSection();
		}
	}

	public void TabSelectItem()
	{
		_taskListView?.TabSelectItem();
	}

	public void TabKanbanSelect()
	{
		if (_kanban != null && _kanban.IsVisible)
		{
			_kanban.TabSelect();
		}
	}

	public void OnTouchScroll(int offset)
	{
		if (_kanban != null && _kanban.IsVisible)
		{
			_kanban.OnScroll(offset);
		}
		if (_timeline != null && _timeline.Visibility == Visibility.Visible)
		{
			_timeline.OnTouchPadHorizontalScroll(offset);
		}
	}

	public void Reload()
	{
		_kanban?.Reload(forceReload: true);
		_timeline?.Reload();
		_taskListView?.ReloadData();
	}

	public void FoldDetail()
	{
		_hideSecondColumn = true;
		if (_detailContent.Tag != "Float")
		{
			ShowDetailBackMenu();
			_detailContent.Tag = "Float";
			_detailContent.SetValue(Panel.ZIndexProperty, 100);
			_secondColumn.MaxWidth = 0.0;
			_secondColumn.MinWidth = 0.0;
			SetFoldStyle(fold: true);
			SetDetailVisibleWithAnimation(show: false);
			_rightSplit.Visibility = Visibility.Collapsed;
		}
		_taskListView?.SetForceReadOnly(!CanShowDetail());
		_batchDetail?.SetUseCardStyle(!_showParallelWidthCal && !_hideSecondColumn, _hideSecondColumn);
		ProjectTaskListView taskListView = _taskListView;
		if (taskListView != null && taskListView.ViewModel.InBatchSelect)
		{
			ShowBatchSelectView(_taskListView.ViewModel.SelectedTaskIds);
		}
	}

	public void ExpandDetail()
	{
		_hideSecondColumn = false;
		if (_detailContent.Tag == "Float")
		{
			HideDetailBackMenu();
			_detailContent.Tag = "Normal";
			SetFoldStyle(fold: false);
			_secondColumn.MinWidth = 0.0;
			_secondColumn.MaxWidth = double.PositiveInfinity;
			SetColumnWidth();
			_rightSplit.Visibility = Visibility.Visible;
		}
		_taskListView?.SetForceReadOnly(!CanShowDetail());
		_batchDetail?.SetUseCardStyle(!_showParallelWidthCal && !_hideSecondColumn, _hideSecondColumn);
		ProjectTaskListView taskListView = _taskListView;
		if (taskListView != null && taskListView.ViewModel.InBatchSelect)
		{
			ShowBatchSelectView(_taskListView.ViewModel.SelectedTaskIds);
		}
		else if (_batchDetail != null)
		{
			HideDetail();
		}
	}

	private void SetColumnWidth()
	{
		double num = _firstColumn.ActualWidth + _secondColumn.ActualWidth;
		double num2 = Utils.FindParent<IListViewParent>(this)?.GetDetailWidth() ?? 1.0;
		double num3 = num / (1.0 + num2);
		double num4 = num / (1.0 + num2) * num2;
		num3 = Math.Max((int)num3, 320);
		num2 = (double)Math.Max((int)num4, 312) / num3;
		_secondColumn.Width = new GridLength(num2, GridUnitType.Star);
		_firstColumn.Width = new GridLength(1.0, GridUnitType.Star);
	}

	private void SetFoldStyle(bool fold)
	{
		if (fold)
		{
			_detailContent.SetValue(Grid.ColumnProperty, 0);
			_detailContent.SetValue(Grid.ColumnSpanProperty, 2);
			_detailContent.MaxWidth = 310.0;
			_detailContent.MinWidth = 310.0;
			_detailContent.HorizontalAlignment = HorizontalAlignment.Right;
			_detailContent.RenderTransform = new TranslateTransform
			{
				X = 310.0
			};
			_detailContent.SetResourceReference(Panel.BackgroundProperty, "FoldAreaBackground");
		}
		else
		{
			_detailContent.MinWidth = 0.0;
			_detailContent.MaxWidth = double.PositiveInfinity;
			_detailContent.SetValue(Grid.ColumnProperty, 1);
			_detailContent.SetValue(Grid.ColumnSpanProperty, 1);
			_detailContent.SetValue(Panel.ZIndexProperty, 0);
			_detailContent.BeginAnimation(TranslateTransform.XProperty, null);
			_detailContent.RenderTransform = new TranslateTransform
			{
				X = 0.0
			};
			_detailContent.HorizontalAlignment = HorizontalAlignment.Stretch;
			_detailContent.Background = Brushes.Transparent;
			_detailContent.Effect = null;
		}
	}

	public void TryFoldDetailOnMouseDown(MouseButtonEventArgs e)
	{
		if (!_detailContent.IsMouseOver && _hideSecondColumn)
		{
			TaskDetailView taskDetailView = _taskDetailView;
			if ((taskDetailView == null || !taskDetailView.PopupShowing) && !(e.GetPosition(this).X > base.ActualWidth - _detailContent.ActualWidth) && _detailContent.Visibility == Visibility.Visible)
			{
				_needHideDetail = true;
				DelayActionHandlerCenter.TryDoAction("MainWindowDelayHideDetail", TryHideDetail, 200);
			}
		}
	}

	private void TryHideDetail(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			if (_needHideDetail)
			{
				SetDetailVisibleWithAnimation(show: false);
			}
		});
	}

	public void SetDetailVisibleWithAnimation(bool show)
	{
		if (!_hideSecondColumn)
		{
			return;
		}
		if (show)
		{
			_needHideDetail = false;
			_detailContent.SetResourceReference(Panel.BackgroundProperty, "FoldAreaBackground");
			if (_detailContent.Effect == null)
			{
				_detailContent.Effect = new DropShadowEffect
				{
					Opacity = 0.08,
					BlurRadius = 12.0,
					ShadowDepth = 4.0,
					Direction = 180.0
				};
			}
			if (_detailContent?.RenderTransform is TranslateTransform translateTransform)
			{
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(translateTransform.X, 0.0, 240);
				translateTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
			}
		}
		else
		{
			_detailContent.Effect = null;
			if (_detailContent?.RenderTransform is TranslateTransform translateTransform2)
			{
				DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(translateTransform2.X, 310.0, 240);
				translateTransform2.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
			}
		}
	}

	private void SetDetailHintVisible(bool showHint)
	{
		_hintPanel.Visibility = ((!showHint) ? Visibility.Collapsed : Visibility.Visible);
	}

	public ProjectIdentity GetSelectedProject()
	{
		ProjectTaskListView taskListView = _taskListView;
		if (taskListView != null && taskListView.InSearch())
		{
			return new SearchProjectIdentity();
		}
		return ProjectIdentity;
	}

	public void ResetColumnWidth(double width)
	{
		if (_hideSecondColumn || !base.IsLoaded || (!(_secondColumn.ActualWidth - 8.0 <= _secondColumn.MinWidth) && !(_firstColumn.ActualWidth - 8.0 <= _firstColumn.MinWidth)))
		{
			return;
		}
		double num = ((_firstColumn.ActualWidth - 8.0 <= _firstColumn.MinWidth) ? (_firstColumn.MinWidth + 12.0) : _firstColumn.ActualWidth);
		double num2 = ((_secondColumn.ActualWidth - 8.0 <= _secondColumn.MinWidth) ? (_secondColumn.MinWidth + 12.0) : _secondColumn.ActualWidth);
		if (num + num2 > width)
		{
			if (num > num2)
			{
				num = width - num2;
			}
			else
			{
				num2 = width - num;
			}
		}
		double num3 = num2 / num;
		if (num3 > 0.0)
		{
			_secondColumn.Width = new GridLength((double.IsNaN(num3) || double.IsInfinity(num3)) ? 1.0 : num3, GridUnitType.Star);
			_firstColumn.Width = new GridLength(1.0, GridUnitType.Star);
		}
	}

	public void SetColumns()
	{
		_firstColumn.Width = new GridLength(1.0, GridUnitType.Star);
		_secondColumn.Width = new GridLength(1.0, GridUnitType.Star);
	}

	public void SetParallelShow(bool withCal, bool inCal)
	{
		_showParallelWidthCal = withCal;
		_showInCal = inCal;
		_kanban?.SetBackGround(withCal);
		if (withCal)
		{
			_listContent.Background = Brushes.Transparent;
			_detailContent.Background = Brushes.Transparent;
			base.Background = Brushes.Transparent;
		}
		else
		{
			SetResourceReference(Panel.BackgroundProperty, "ShowAreaBottomColor");
		}
		_timeline?.SetBackground(withCal);
		_batchDetail?.SetUseCardStyle(!_showParallelWidthCal && !_hideSecondColumn, _hideSecondColumn);
	}

	public bool CanShowDetail()
	{
		if (_showParallelWidthCal)
		{
			return !_hideSecondColumn;
		}
		return true;
	}

	public double GetItemWidth()
	{
		if (_listContent.IsVisible)
		{
			return Math.Max(120.0, _listContent.ActualWidth - 40.0);
		}
		if (_kanban != null && _kanban.IsVisible)
		{
			return Math.Max(120.0, (double)FindResource("KanbanColumnWidth") - 20.0);
		}
		if (_timeline != null && _timeline.IsVisible)
		{
			return _timeline.GetViewModel().OneDayWidth;
		}
		return 200.0;
	}

	public void ExitVirtual()
	{
		base.IsHitTestVisible = true;
		_taskListView?.ExitVirtual();
	}

	public string GetDisplayMode()
	{
		if (_taskListView != null && _taskListView.IsVisible)
		{
			return "list";
		}
		if (_kanban != null && _kanban.IsVisible)
		{
			return "kanban";
		}
		if (_timeline != null && _timeline.IsVisible)
		{
			return "timeline";
		}
		return null;
	}

	public string GetViewMode()
	{
		if (_kanban != null && _kanban.IsVisible)
		{
			return "kanban";
		}
		if (_timeline != null && _timeline.IsVisible)
		{
			return "timeline";
		}
		if (_taskListView != null && _taskListView.IsVisible)
		{
			return "list";
		}
		return "";
	}
}
