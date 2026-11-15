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
using System.Windows.Shapes;
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
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Kanban;

public class KanbanContainer : UserControl, IKanban, IBatchEditable, IBatchTaskSelector, IComponentConnector
{
	private BatchTaskEditHelper _batchHelper;

	private string _switchedColumnId;

	private string _currentColumnId;

	private long _current;

	private bool _inOperation;

	private Point _startPoint;

	private readonly DelayActionHandler _delayLoadHandler = new DelayActionHandler(200);

	private readonly BlockingList<ColumnViewModel> _columnViewModels = new BlockingList<ColumnViewModel>();

	private bool _autoScroll;

	private double _offset;

	private string _startDragColumnId;

	private bool _dropping;

	private bool _titleBorderMouseDown;

	private bool _dragOver;

	internal KanbanContainer Root;

	internal Grid KanbanContainerGrid;

	internal Grid TemplateGuidePopup;

	internal Border MenuPathGrid;

	internal Path FoldImage;

	internal StackPanel OptionPanel;

	internal Border AIRecommendBorder;

	internal Popup AIRecommendHintPopup;

	internal EscPopup ChooseSortTypePopup;

	internal Grid MoreGrid;

	internal EscPopup MorePopup;

	internal Grid TitleGrid;

	internal Border TitleBorder;

	internal EmojiTitleEditor TitleEditor;

	internal Grid ShareGrid;

	internal Path SharePath;

	internal ScrollViewer KanbanScroller;

	internal Grid KanbanGrid;

	internal KanbanColumnCanvas ColumnContainer;

	internal Grid AddColumnControlGrid;

	internal Border AddColumnControl;

	internal Border CloseAddButton;

	internal StackPanel EmptyGrid;

	internal EmptyView EmptyImage;

	internal TextBlock EmptyText;

	internal TextBlock EmptyAddText;

	internal BatchTaskControl BatchControl;

	private bool _contentLoaded;

	public SectionAddTaskViewModel AddingModel { get; private set; }

	public KanbanViewModel ViewModel => base.DataContext as KanbanViewModel;

	public bool ColumnDragging => ColumnContainer.IsDragging;

	public bool HasNewAddColumnEditing => _columnViewModels.Exists((ColumnViewModel model) => model.Editing);

	public event EventHandler<DisplayItemModel> TaskRemoved;

	public event EventHandler<DisplayItemModel> TaskAdded;

	public event EventHandler<List<string>> SetSelected;

	public KanbanContainer(ProjectIdentity project)
	{
		InitializeComponent();
		UpdateColumnColorLayout();
		base.DataContext = new KanbanViewModel(project, this);
		InitBatchHelper();
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
	}

	public void ShowBatchOperationDialog()
	{
		_batchHelper.ShowOperationDialog();
	}

	public void RemoveSelectedId(string id)
	{
		List<string> selectedTaskIds = _batchHelper.SelectedTaskIds;
		if (selectedTaskIds != null && selectedTaskIds.Count > 1)
		{
			return;
		}
		_batchHelper.SelectedTaskIds?.Remove(id);
		foreach (ColumnViewModel item in _columnViewModels.ToList())
		{
			item.RemoveSelectedId(id);
		}
		OnBatchSelectChanged();
	}

	private async void OnBatchSelectChanged()
	{
		List<string> selectedIds = _batchHelper.SelectedTaskIds?.ToList();
		if (selectedIds != null && selectedIds.Count > 0)
		{
			if (selectedIds.Count == 1)
			{
				await Task.Delay(50);
			}
			if (PopupStateManager.IsViewPopOpened())
			{
				if (selectedIds == null || selectedIds.Count <= 1)
				{
					return;
				}
			}
			BatchControl.SetTaskIds(selectedIds, ViewModel.Identity.IsNote, useInList: true, ViewModel.Identity);
			BatchControl.Visibility = Visibility.Visible;
			if (Utils.FindParent<TaskView>(this)?.ShowInParallel ?? false)
			{
				DataChangedNotifier.NotifyCalOrListBatchSelected(calBatchSelected: false);
			}
		}
		else
		{
			BatchControl.Visibility = Visibility.Collapsed;
		}
	}

	public void SetSelectedTaskIds(List<string> taskIds)
	{
		List<string> list = new List<string>();
		foreach (ColumnViewModel item in _columnViewModels.ToList())
		{
			list.AddRange(item.GetSelectedTask() ?? new List<string>());
		}
		_batchHelper.SelectedTaskIds = list.Distinct().ToList();
		OnBatchSelectChanged();
	}

	public List<string> GetSelectedTaskIds()
	{
		return _batchHelper.SelectedTaskIds;
	}

	public void ReloadList()
	{
		Reload();
	}

	public UIElement BatchOperaPlacementTarget()
	{
		return TitleEditor;
	}

	public void HideBatchSelectControl()
	{
		ClearSelected();
		OnBatchSelectChanged();
	}

	public void SetInOperation()
	{
		_inOperation = true;
	}

	public void CancelOperation()
	{
		_inOperation = false;
	}

	public bool IsInDragging()
	{
		return TickDragHelper.Dragging;
	}

	public string GetProjectId()
	{
		return ViewModel.Identity?.GetProjectId();
	}

	public void ClearSelected()
	{
		this.SetSelected?.Invoke(this, null);
		_batchHelper.ClearSelectedTaskIds();
	}

	public KanbanViewModel GetViewModel()
	{
		return ViewModel;
	}

	public void Toast(string text)
	{
		Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, text);
	}

	public async Task ReloadColumn(string columnId, bool force = false)
	{
		Reload();
	}

	public int GetColumnCount()
	{
		return _columnViewModels?.GetCount((ColumnViewModel m) => !m.IsPinned) ?? 0;
	}

	public async void AddColumn(string columnId, HorizontalDirection direction)
	{
		if (_columnViewModels.GetCount((ColumnViewModel c) => !c.IsPinned) >= LimitCache.GetLimitByKey(Constants.LimitKind.KanbanNumber))
		{
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("ExceedMaxSectionCountMessage"), MessageBoxButton.OK);
			customerDialog.Owner = Window.GetWindow(this);
			customerDialog.ShowDialog();
			return;
		}
		AddColumnControl.Visibility = Visibility.Collapsed;
		ColumnViewModel columnViewModel = _columnViewModels.FirstOrDefault((ColumnViewModel m) => m.ColumnId == columnId);
		if (columnViewModel == null)
		{
			return;
		}
		ColumnModel columnModel = new ColumnModel
		{
			id = Utils.GetGuid(),
			userId = Utils.GetCurrentUserIdInt().ToString(),
			projectId = ViewModel.Identity.Id,
			createdTime = DateTime.Now,
			modifiedTime = DateTime.Now,
			name = string.Empty,
			syncStatus = "new"
		};
		int num = _columnViewModels.IndexOf(columnViewModel);
		if (direction == HorizontalDirection.Left)
		{
			if (num == 0 || (num > 0 && _columnViewModels[num - 1].IsPinned))
			{
				columnModel.sortOrder = columnViewModel.SortOrder - 268435456;
			}
			else
			{
				columnModel.sortOrder = columnViewModel.SortOrder / 2 + _columnViewModels[num - 1].SortOrder / 2;
			}
		}
		else if (num == _columnViewModels.Count - 1)
		{
			columnModel.sortOrder = columnViewModel.SortOrder + 268435456;
		}
		else
		{
			columnModel.sortOrder = columnViewModel.SortOrder / 2 + _columnViewModels[num + 1].SortOrder / 2;
		}
		int index = ((direction == HorizontalDirection.Right) ? (num + 1) : num);
		InsertColumn(index, new ColumnViewModel(columnModel)
		{
			NewAdd = true,
			Identity = new ColumnProjectIdentity(ViewModel.Identity, columnModel.id)
		});
	}

	public async void DeleteColumn(string columnId, bool prompt)
	{
		string targetColumnId = null;
		if (prompt)
		{
			string text = await ColumnDao.GetProjectDefaultColumnId(ViewModel.Identity.Id);
			if ((await TaskDao.GetTasksInProjectOrColumn(ViewModel.Identity.Id, columnId, text == columnId)).Count > 0)
			{
				DeleteColumnWindow deleteColumnWindow = new DeleteColumnWindow(ViewModel.Identity.Id, columnId)
				{
					Owner = Window.GetWindow(this)
				};
				bool? flag = deleteColumnWindow.ShowDialog();
				App.Window.Activate();
				if (flag != true)
				{
					return;
				}
				targetColumnId = deleteColumnWindow.TargetColumnId;
			}
		}
		await PerformDeleteColumn(columnId, targetColumnId);
	}

	private async Task PerformDeleteColumn(string columnId, string targetColumnId = null)
	{
		List<ColumnViewModel> list = _columnViewModels.ToList();
		ColumnViewModel model = list.FirstOrDefault((ColumnViewModel m) => m.ColumnId == columnId);
		if (model != null)
		{
			ColumnViewModel primaryModel = list[0];
			list.Remove(model);
			SetupColumn(list);
			UtilLog.Info("kanbanContainer.DeleteColumn : " + columnId);
			await ColumnDao.DeleteColumn(columnId);
			if (targetColumnId != null)
			{
				await TaskService.MoveColumnAsync(columnId, model.ProjectId, model.ProjectId, targetColumnId);
			}
			else
			{
				await TaskService.BatchDeleteTaskInColumn(model.ProjectId, columnId, primaryModel.ColumnId);
			}
			SyncManager.TryDelaySync();
		}
		ShowAddColumn();
	}

	public async Task<List<string>> GetColumnNames()
	{
		List<ColumnModel> list = await ColumnDao.GetColumnsByProjectId(ViewModel.Identity.Id);
		if (list != null && list.Any())
		{
			return list.Select((ColumnModel column) => column.name).ToList();
		}
		return new List<string>();
	}

	public async void Reload(bool forceReload = false, bool restoreSelect = true)
	{
		while (_dropping)
		{
			await Task.Delay(30);
		}
		if ((!_inOperation || forceReload) && base.IsVisible)
		{
			await Load(ViewModel.Identity.Copy(ViewModel.Identity), forceReload, needPull: false, null, restoreSelect);
			ShowAddColumn();
		}
	}

	public bool IsEnable()
	{
		if (ViewModel.Identity is NormalProjectIdentity)
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == ViewModel.Identity.Id);
			if (projectModel != null)
			{
				return projectModel.IsEnable();
			}
		}
		return true;
	}

	private bool IsDefaultColumn(string columnId)
	{
		if (_columnViewModels.Count > 0)
		{
			return _columnViewModels[0].ColumnId == columnId;
		}
		return false;
	}

	private void TryLoad(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			Reload(forceReload: true);
		});
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.SyncDone -= OnSyncChanged;
		DataChangedNotifier.ThemeModeChanged -= OnThemeModeChanged;
		DataChangedNotifier.ProjectChanged -= OnProjectChanged;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
		DataChangedNotifier.ProjectGroupChanged -= OnProjectGroupChanged;
		DataChangedNotifier.TagChanged -= OnTagChanged;
		DataChangedNotifier.PomoChanged -= OnPomoChanged;
		DataChangedNotifier.TaskDefaultChanged -= OnDefaultChanged;
		DataChangedNotifier.ProjectColumnChanged -= OnColumnChanged;
		DataChangedNotifier.HabitsChanged -= HabitsChanged;
		DataChangedNotifier.HabitSkip -= HabitsChanged;
		DataChangedNotifier.HabitCheckInChanged -= HabitsChanged;
		DataChangedNotifier.EventArchivedChanged -= OnEventChanged;
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		CalendarEventChangeNotifier.TitleChanged -= OnCalendarTitleChanged;
		CalendarEventChangeNotifier.SummaryChanged -= OnCalendarSummaryChanged;
		CalendarEventChangeNotifier.Deleted -= OnEventChanged;
		CalendarEventChangeNotifier.Restored -= OnEventChanged;
		CalendarEventChangeNotifier.Changed -= OnEventChanged;
		CalendarEventChangeNotifier.RemoteChanged -= OnEventChanged;
		TickDragHelper.DragDrop -= OnDragDrop;
		TickDragHelper.DragMoving -= OnDragOver;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "HideComplete");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "ShowSubtasks");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "ShowDetails");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnShowAddChanged, "KbShowAdd");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnColumnColorChanged, "KbShowColumnColor");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnColumnWidthChanged, "KbSize");
		_delayLoadHandler.StopAndClear();
	}

	private void BindEvents()
	{
		DataChangedNotifier.SyncDone += OnSyncChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeModeChanged;
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		DataChangedNotifier.FilterChanged += OnFilterChanged;
		DataChangedNotifier.ProjectGroupChanged += OnProjectGroupChanged;
		DataChangedNotifier.TagChanged += OnTagChanged;
		DataChangedNotifier.PomoChanged += OnPomoChanged;
		DataChangedNotifier.TaskDefaultChanged += OnDefaultChanged;
		DataChangedNotifier.ProjectColumnChanged += OnColumnChanged;
		DataChangedNotifier.HabitsChanged += HabitsChanged;
		DataChangedNotifier.HabitSkip += HabitsChanged;
		DataChangedNotifier.HabitCheckInChanged += HabitsChanged;
		DataChangedNotifier.EventArchivedChanged += OnEventChanged;
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		CalendarEventChangeNotifier.TitleChanged += OnCalendarTitleChanged;
		CalendarEventChangeNotifier.SummaryChanged += OnCalendarSummaryChanged;
		CalendarEventChangeNotifier.Deleted += OnEventChanged;
		CalendarEventChangeNotifier.Restored += OnEventChanged;
		CalendarEventChangeNotifier.Changed += OnEventChanged;
		CalendarEventChangeNotifier.RemoteChanged += OnEventChanged;
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "HideComplete");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "ShowSubtasks");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "ShowDetails");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnShowAddChanged, "KbShowAdd");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnColumnColorChanged, "KbShowColumnColor");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnColumnWidthChanged, "KbSize");
		TickDragHelper.DragDrop += OnDragDrop;
		TickDragHelper.DragMoving += OnDragOver;
		_delayLoadHandler.SetAction(TryLoad);
		TitleEditor.SetMaxWidth(base.ActualWidth - 150.0);
		TitleEditor.SetCheckFunc(CheckTitleValid);
	}

	private void OnColumnWidthChanged(object sender, PropertyChangedEventArgs e)
	{
		ColumnContainer.ColumnWidthChanged();
		if (KanbanScroller.HorizontalOffset + base.ActualWidth > ColumnContainer.Width)
		{
			KanbanScroller.ScrollToHorizontalOffset(ColumnContainer.Width - base.ActualWidth - 10.0);
		}
	}

	private void OnShowAddChanged(object sender, PropertyChangedEventArgs e)
	{
		ColumnContainer.SetShowAddInColumn();
	}

	private void OnColumnColorChanged(object sender, PropertyChangedEventArgs e)
	{
		UpdateColumnColorLayout();
		foreach (KanbanColumnView item in ColumnContainer.ChildrenList.ToList())
		{
			item.RefreshColumnColor();
		}
		Reload(forceReload: true);
	}

	private void UpdateColumnColorLayout()
	{
		bool kbShowColumnColor = LocalSettings.Settings.ExtraSettings.KbShowColumnColor;
		KanbanScroller.Margin = new Thickness(0.0, kbShowColumnColor ? 4 : 0, 0.0, 0.0);
		AddColumnControl.Margin = new Thickness(0.0, 18.0, kbShowColumnColor ? 12 : 10, 25.0);
		ColumnContainer.ColumnWidthChanged();
	}

	private void OnDisplayChanged(object sender, PropertyChangedEventArgs e)
	{
		Reload(forceReload: true);
	}

	private string CheckTitleValid(string text)
	{
		return ViewModel.Identity?.CheckTitleValid(text);
	}

	private void OnTitleTextChanged(object sender, string e)
	{
		ViewModel.Identity?.SaveTitle(e);
	}

	private void OnTagChanged(object sender, TagModel e)
	{
		if (base.Visibility != Visibility.Visible || !(ViewModel.Identity is TagProjectIdentity tagProjectIdentity) || !(tagProjectIdentity.Tag == e?.name))
		{
			return;
		}
		TagModel tagByName = CacheManager.GetTagByName(tagProjectIdentity.Id);
		if (tagByName != null)
		{
			TagProjectIdentity identity = new TagProjectIdentity(tagByName);
			if (tagByName.viewMode != "kanban")
			{
				Utils.FindParent<ListViewContainer>(this)?.SelectProject(identity);
				return;
			}
			ViewModel.SetIdentity(identity);
			Reload(forceReload: true);
		}
	}

	private void OnProjectGroupChanged(object sender, ProjectGroupModel e)
	{
		if (base.Visibility != Visibility.Visible || !(ViewModel.Identity is GroupProjectIdentity groupProjectIdentity) || !(groupProjectIdentity.Id == e?.id))
		{
			return;
		}
		ProjectGroupModel groupById = CacheManager.GetGroupById(groupProjectIdentity.Id);
		if (groupById != null)
		{
			List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(groupById.id);
			GroupProjectIdentity identity = new GroupProjectIdentity(groupById, projectsInGroup);
			if (groupById.viewMode != "kanban")
			{
				Utils.FindParent<ListViewContainer>(this)?.SelectProject(identity);
				return;
			}
			ViewModel.SetIdentity(identity);
			Reload(forceReload: true);
		}
	}

	private void OnFilterChanged(object sender, FilterChangeArgs e)
	{
		if (base.Visibility != Visibility.Visible || !(ViewModel.Identity is FilterProjectIdentity filterProjectIdentity) || !(filterProjectIdentity.Id == e?.Filter?.id))
		{
			return;
		}
		FilterModel filterById = CacheManager.GetFilterById(filterProjectIdentity.Id);
		if (filterById != null)
		{
			FilterProjectIdentity identity = new FilterProjectIdentity(filterById);
			if (filterById.viewMode != "kanban")
			{
				Utils.FindParent<ListViewContainer>(this)?.SelectProject(identity);
				return;
			}
			ViewModel.SetIdentity(identity);
			Reload(forceReload: true);
		}
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		BlockingSet<string> changedIds = new BlockingSet<string>();
		changedIds.AddRange(e.SortOrderChangedIds.Value);
		changedIds.AddRange(e.DateChangedIds.Value);
		changedIds.AddRange(e.DeletedChangedIds.Value);
		changedIds.AddRange(e.BatchChangedIds.Value);
		changedIds.AddRange(e.PinChangedIds.Value);
		changedIds.AddRange(e.StatusChangedIds.Value);
		changedIds.AddRange(e.KindChangedIds.Value);
		changedIds.AddRange(e.ProjectChangedIds.Value);
		changedIds.AddRange(e.PriorityChangedIds.Value);
		changedIds.AddRange(e.TagChangedIds.Value);
		changedIds.AddRange(e.AssignChangedIds.Value);
		changedIds.AddRange(e.CheckItemChangedIds.Value);
		e.AddIds.AddRange(e.UndoDeletedIds);
		if (e.AddIds.Any())
		{
			List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ViewModel.Identity, e.AddIds.ToList());
			if (matchedTasks != null && matchedTasks.Count > 0)
			{
				goto IL_0235;
			}
		}
		if (e.TagChangedIds.Any())
		{
			List<TaskBaseViewModel> matchedTasks2 = TaskViewModelHelper.GetMatchedTasks(ViewModel.Identity, e.TagChangedIds.ToList());
			if (matchedTasks2 != null && matchedTasks2.Count > 0)
			{
				goto IL_0235;
			}
		}
		List<TaskBaseViewModel> sourceModels = ViewModel.SourceModels;
		if (sourceModels == null || !sourceModels.Any((TaskBaseViewModel m) => changedIds.Contains(m.Id)))
		{
			_columnViewModels.Do(delegate
			{
				_columnViewModels.Value.ForEach(delegate(ColumnViewModel c)
				{
					c.GetListViewModel()?.CheckTaskChanged(e);
				});
			});
			return;
		}
		goto IL_0235;
		IL_0235:
		Reload(forceReload: true);
	}

	private void OnDefaultChanged(object sender, EventArgs e)
	{
		ColumnContainer.ReloadQuickAddView();
	}

	private void OnPomoChanged(object sender, string e)
	{
		Reload(forceReload: true);
	}

	private void OnProjectChanged(object sender, EventArgs e)
	{
		if (base.Visibility != Visibility.Visible)
		{
			return;
		}
		ProjectIdentity identity = ViewModel.Identity;
		NormalProjectIdentity pid = identity as NormalProjectIdentity;
		if (pid == null)
		{
			return;
		}
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == pid.Id);
		if (projectModel != null)
		{
			NormalProjectIdentity identity2 = new NormalProjectIdentity(projectModel);
			if (projectModel.viewMode != "kanban")
			{
				Utils.FindParent<ListViewContainer>(this)?.SelectProject(new NormalProjectIdentity(projectModel));
				return;
			}
			ViewModel.SetIdentity(identity2);
			Reload(forceReload: true);
		}
	}

	private void HabitsChanged(object sender, object e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if ((ViewModel.Identity is TodayProjectIdentity || ViewModel.Identity is WeekProjectIdentity) && LocalSettings.Settings.HabitInToday)
			{
				Reload(forceReload: true);
			}
		});
	}

	private void OnEventChanged(object sender, object e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (ViewModel.Identity is SmartProjectIdentity || ViewModel.Identity is FilterProjectIdentity)
			{
				Reload(forceReload: true);
			}
		});
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

	private void OnColumnChanged(object sender, string e)
	{
		if (e == ViewModel.Identity?.Id)
		{
			Reload(forceReload: true);
		}
	}

	private void OnThemeModeChanged(object sender, EventArgs e)
	{
		if (base.IsVisible)
		{
			Reload(forceReload: true);
		}
	}

	private void OnSyncChanged(object sender, SyncResult changed)
	{
		if ((changed.ColumnChanged || changed.RemoteTasksChanged) && base.Visibility == Visibility.Visible)
		{
			Reload(forceReload: true);
		}
	}

	private void InitBatchHelper()
	{
		_batchHelper = new BatchTaskEditHelper(this);
	}

	private void InitView(ProjectModel project)
	{
		if (project == null)
		{
			ShareGrid.Visibility = Visibility.Collapsed;
			OptionPanel.Visibility = Visibility.Visible;
			AddColumnControl.Visibility = Visibility.Collapsed;
		}
		else
		{
			SetShareImage(project);
			AddColumnControl.Visibility = Visibility.Visible;
		}
	}

	public async Task Load(ProjectIdentity identity, bool forceReload = false, bool needPull = false, string taskId = null, bool restoreSelect = true)
	{
		if (TaskDragHelpModel.DragHelp.IsDragging || ColumnDragging || HasNewAddColumnEditing)
		{
			return;
		}
		if (ViewModel.Identity?.Id != identity.Id)
		{
			KanbanScroller.ScrollToHorizontalOffset(0.0);
			RemoveAddingTaskModel();
		}
		if (ViewModel.Identity?.Id != identity.Id || (ViewModel.Identity is NormalProjectIdentity normalProjectIdentity && identity is NormalProjectIdentity normalProjectIdentity2 && normalProjectIdentity.Project?.permission != normalProjectIdentity2.Project?.permission))
		{
			forceReload = true;
		}
		if (forceReload)
		{
			if (!restoreSelect)
			{
				_batchHelper.ClearSelectedTaskIds();
			}
			InitView((identity as NormalProjectIdentity)?.Project);
			_batchHelper.ProjectIdentity = identity;
			bool flag = identity is TodayProjectIdentity;
			AIRecommendBorder.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			if (flag && !LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.MainAIRecommendHint))
			{
				AIRecommendHintPopup.IsOpen = true;
				LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.MainAIRecommendHint);
				LocalSettings.Settings.Save();
			}
			ViewModel.SetIdentity(identity);
			TitleEditor.SetText(ViewModel.Name);
			TitleEditor.SetEnable((ViewModel.Identity is NormalProjectIdentity { CanEdit: not false, Project: not null } normalProjectIdentity3 && !normalProjectIdentity3.Project.Isinbox) || ViewModel.Identity is GroupProjectIdentity || ViewModel.Identity is FilterProjectIdentity);
			await ViewModel.LoadTasks(force: true, needPull);
			if (!string.IsNullOrEmpty(taskId))
			{
				NavigateTask(taskId);
			}
		}
	}

	private void SetShareImage(ProjectModel project)
	{
		if (project.closed ?? TeamDao.IsTeamExpired(project.teamId))
		{
			ShareGrid.Visibility = Visibility.Collapsed;
			OptionPanel.Visibility = Visibility.Collapsed;
			return;
		}
		ShareGrid.Visibility = ((project.userCount < 2) ? Visibility.Collapsed : Visibility.Visible);
		OptionPanel.Visibility = Visibility.Visible;
		string permission = project.permission;
		if (!(permission == "read"))
		{
			if (permission == "comment")
			{
				ShareGrid.ToolTip = new ToolTip
				{
					Content = Utils.GetString("CanComment")
				};
				SharePath.SetResourceReference(Path.DataProperty, "Ic.ProjectList.SharingCanComment");
			}
			else
			{
				ShareGrid.ToolTip = new ToolTip
				{
					Content = Utils.GetString("Editable")
				};
				SharePath.SetResourceReference(Path.DataProperty, "Ic.ProjectList.SharingEditable");
			}
		}
		else
		{
			ShareGrid.ToolTip = new ToolTip
			{
				Content = Utils.GetString("ReadOnly")
			};
			SharePath.SetResourceReference(Path.DataProperty, "Ic.ProjectList.SharingReadOnly");
		}
	}

	private void ShowAddColumn()
	{
		if (AddColumnControl.Visibility != Visibility.Visible && !HasNewAddColumnEditing)
		{
			AddColumnControl.Visibility = Visibility.Visible;
		}
	}

	private void OnContainerMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Released)
		{
			_autoScroll = false;
		}
		if (ColumnDragging)
		{
			double x = e.GetPosition(this).X;
			x = Math.Min(base.ActualWidth, Math.Max(0.0, x));
			ShiftKanbanOnScrollEdge(x);
		}
	}

	private void ShiftKanbanOnScrollEdge(double offset)
	{
		if (offset <= 36.0)
		{
			offset = -36.0 + offset;
		}
		else
		{
			if (!(offset >= base.ActualWidth - 36.0))
			{
				_offset = 0.0;
				_autoScroll = false;
				return;
			}
			offset = 36.0 + offset - base.ActualWidth;
		}
		_offset = offset;
		AutoScrollContainer();
	}

	private async void AutoScrollContainer()
	{
		if (_autoScroll)
		{
			return;
		}
		_autoScroll = true;
		while (_autoScroll)
		{
			await Task.Delay(30);
			if (_autoScroll)
			{
				KanbanScroller.ScrollToHorizontalOffset(KanbanScroller.HorizontalOffset + _offset);
			}
		}
	}

	private async Task OnBatchTaskDrop(DisplayItemModel model, ColumnViewModel dropColumn)
	{
		if (dropColumn != null && !string.IsNullOrEmpty(dropColumn.ColumnId))
		{
			await BatchDropOnAnotherColumn(model, dropColumn);
		}
	}

	private async Task BatchDropOnAnotherColumn(DisplayItemModel model, ColumnViewModel dropColumn)
	{
		if (dropColumn.ColumnId.StartsWith("tag:"))
		{
			return;
		}
		if (dropColumn.ColumnId.StartsWith("date:"))
		{
			DateTime? date = dropColumn.GetDate();
			await TaskService.BatchSetDate(model.DragTaskIds, date);
		}
		else if (dropColumn.ColumnId.StartsWith("priority:"))
		{
			int priority = dropColumn.GetPriority();
			await TaskService.BatchSetPriority(model.DragTaskIds, priority);
		}
		else if (dropColumn.ColumnId.StartsWith("project:"))
		{
			ProjectModel projectById = CacheManager.GetProjectById(dropColumn.GetProject());
			if (projectById != null)
			{
				await TaskService.BatchMoveParentTaskProject(model.DragTaskIds, projectById);
			}
		}
		else if (!dropColumn.ColumnId.StartsWith("assign:"))
		{
			await TaskService.BatchSetColumn(model.DragTaskIds, dropColumn.ColumnId);
		}
		else
		{
			string assignee = dropColumn.GetAssignee();
			if (!string.IsNullOrEmpty(assignee) && assignee != "-1")
			{
				List<string> list = TaskCache.GetTasksByIds(model.DragTaskIds)?.Select((TaskBaseViewModel m) => m.ProjectId).Distinct().ToList();
				if (list != null && list.Count > 0)
				{
					foreach (string item in list)
					{
						List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(item);
						if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != assignee))
						{
							Toast(Utils.GetString("ChangeAssigneeError"));
							Reload(forceReload: true);
							return;
						}
					}
				}
			}
			await TaskService.BatchSetAssignee(model.DragTaskIds, assignee);
		}
		List<string> treeTopIds = TaskDao.GetTreeTopIds(model.DragTaskIds, model.ProjectId);
		foreach (string item2 in treeTopIds)
		{
			await TaskDao.UpdateParent(item2, string.Empty);
		}
	}

	private async Task OnTaskDrop(DisplayItemModel model, ColumnViewModel dropColumn)
	{
		_ = 8;
		try
		{
			if (model == null || dropColumn == null || dropColumn.ColumnId == "note" || dropColumn.ColumnId == "habit" || dropColumn.ColumnId == "course" || dropColumn.ColumnId == "calendar" || dropColumn.ColumnId.StartsWith("tag:"))
			{
				return;
			}
			if (dropColumn.ColumnId.StartsWith("date:"))
			{
				DateTime? date = dropColumn.GetDate();
				if (model.IsItem)
				{
					if (!date.HasValue)
					{
						await TaskService.SetCheckItemDate(model.TaskId, model.Id, new TimeDataModel
						{
							StartDate = null,
							IsAllDay = null
						});
					}
					else
					{
						await TaskService.SetSubtaskDate(model.TaskId, model.Id, date.Value);
					}
				}
				else if (!date.HasValue)
				{
					await TaskService.ClearDate(model.TaskId);
				}
				else
				{
					await TaskService.SetDate(model.TaskId, date.Value, handleReminder: false);
				}
			}
			else if (dropColumn.ColumnId.StartsWith("project:"))
			{
				string project = dropColumn.GetProject();
				await TaskService.MoveProject(model.TaskId, project);
			}
			else if (dropColumn.ColumnId.StartsWith("priority:"))
			{
				int priority = dropColumn.GetPriority();
				await TaskService.SetPriority(model.TaskId, priority);
			}
			else if (!dropColumn.ColumnId.StartsWith("assign:"))
			{
				await TaskService.SaveTaskColumnId(model.TaskId, dropColumn.ColumnId);
			}
			else
			{
				string assignee = dropColumn.GetAssignee();
				if (!string.IsNullOrEmpty(assignee) && assignee != "-1")
				{
					List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(model.ProjectId);
					if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != assignee))
					{
						Toast(Utils.GetString("ChangeAssigneeError"));
						return;
					}
				}
				await TaskService.SetAssignee(model.TaskId, assignee);
			}
			await TaskDao.UpdateParent(model.TaskId, string.Empty);
		}
		catch (Exception)
		{
		}
	}

	private void OnAddColumn()
	{
		if (AddColumnControl.Visibility != Visibility.Visible)
		{
			KanbanColumnView kanbanColumnView = ColumnContainer.ChildrenList.FirstOrDefault((KanbanColumnView item) => item != null && (item.GetModel()?.NewAdd ?? false));
			if (kanbanColumnView != null)
			{
				KanbanColumnView kanbanColumnView2 = kanbanColumnView;
				kanbanColumnView2.TryFocus();
			}
			return;
		}
		ColumnViewModel columnViewModel = _columnViewModels.Value.LastOrDefault();
		if (columnViewModel != null)
		{
			ProjectModel projectById = CacheManager.GetProjectById(ViewModel.Identity?.GetProjectId());
			if (projectById != null)
			{
				projectById.ShowAddColumn = true;
				ProjectDao.TryUpdateProject(projectById);
			}
			ViewModel.ShowAdd = true;
			AddColumn(columnViewModel.ColumnId, HorizontalDirection.Right);
			KanbanScroller.ScrollToRightEnd();
		}
	}

	private void OnMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (Utils.IfShiftPressed())
		{
			ScrollViewer obj = (ScrollViewer)sender;
			double horizontalOffset = obj.HorizontalOffset;
			horizontalOffset -= (double)e.Delta;
			obj.ScrollToHorizontalOffset(horizontalOffset);
			e.Handled = true;
		}
	}

	private async void MoreGridClick(object sender, MouseButtonEventArgs e)
	{
		List<CustomMenuItemViewModel> menuItems = new List<CustomMenuItemViewModel>();
		bool flag = false;
		menuItems.Add(new CustomMenuItemViewModel("showComplete", Utils.GetString(LocalSettings.Settings.HideComplete ? "ShowCompleted" : "HideCompleted"), Utils.GetGeometry(LocalSettings.Settings.HideComplete ? "Ic.TaskOM.ShowCompleted" : "Ic.TaskOM.HideCompleted")));
		menuItems.Add(new CustomMenuItemViewModel("showDetail", Utils.GetString(LocalSettings.Settings.ShowDetails ? "HideDetails" : "ShowDetails"), Utils.GetGeometry(LocalSettings.Settings.ShowDetails ? "Ic.TaskOM.HideDetail" : "Ic.TaskOM.ShowDetail")));
		menuItems.Add(new CustomMenuItemViewModel("DisplaySetting", Utils.GetString("DisplaySetting"), Utils.GetGeometry("Ic.TaskOM.ViewSetting")));
		if (ViewModel.Enable && ViewModel.SortOption.groupByType == Constants.SortType.sortOrder)
		{
			flag = true;
			menuItems.Add(new CustomMenuItemViewModel("addSection", Utils.GetString("AddSection"), Utils.GetGeometry("Ic.ProjectList.Add")));
		}
		if (ViewModel.Identity is NormalProjectIdentity)
		{
			ProjectModel projectById = CacheManager.GetProjectById(ViewModel.Identity.Id);
			if (projectById != null)
			{
				if (projectById.userCount <= 1 && !projectById.Isinbox)
				{
					flag = true;
					menuItems.Add(new CustomMenuItemViewModel("share", Utils.GetString("Share"), Utils.GetGeometry("Ic.TaskOM.Sharing")));
				}
				if (projectById.IsProjectPermit())
				{
					flag = true;
					menuItems.Add(new CustomMenuItemViewModel("listActivities", Utils.GetString("ListActivitiesPro"), Utils.GetGeometry("Ic.TaskOM.ListActivities")));
				}
			}
		}
		if (flag)
		{
			menuItems.Insert(3, new CustomMenuItemViewModel(null));
		}
		if (ViewModel.Identity is NormalProjectIdentity || ViewModel.Identity is AllProjectIdentity)
		{
			List<string> list = await CalendarConnectService.GetNotionConnectedProjectIds();
			if (list.Contains(ViewModel.Identity.Id) || (ViewModel.Identity is AllProjectIdentity && list.Contains("all")))
			{
				menuItems.Add(new CustomMenuItemViewModel("SyncNotion", Utils.GetString("SyncWithNotion"), Utils.GetGeometry("Ic.Tabbar.Sync"))
				{
					ShowSelected = false
				});
			}
		}
		if (Utils.FindParent<TaskView>(this)?.ShowInCal ?? false)
		{
			menuItems.Add(new CustomMenuItemViewModel("ExitParallel", Utils.GetString("Close"), Utils.GetGeometry("Ic.Common.Close"))
			{
				ImageWidth = 12.0,
				ImageMargin = new Thickness(14.0, 0.0, 0.0, 0.0)
			});
		}
		List<string> switchViewModes = ViewModel.Identity.GetSwitchViewModes();
		SwitchListViewControl switchListViewControl = ((switchViewModes != null) ? new SwitchListViewControl() : null);
		if (switchViewModes != null)
		{
			switchListViewControl.ViewSelected += OnSwitchView;
			switchListViewControl.SetButtonStatus(false, true, switchViewModes.Contains("timeline") ? new bool?(false) : ((bool?)null));
		}
		CustomMenuList customMenuList = new CustomMenuList(menuItems, MorePopup, switchListViewControl);
		customMenuList.Operated += OnMoreItemSelected;
		customMenuList.Show();
	}

	private void OnMoreItemSelected(object sender, object e)
	{
		if (!(e is string text) || text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 14:
			switch (text[0])
			{
			case 'D':
				if (text == "DisplaySetting")
				{
					ShowDisplaySetting();
				}
				break;
			case 'l':
				if (text == "listActivities")
				{
					OnListActivityClick();
				}
				break;
			}
			break;
		case 12:
			switch (text[0])
			{
			case 's':
				if (text == "showComplete")
				{
					OnHideCompleteClick();
				}
				break;
			case 'E':
				if (text == "ExitParallel")
				{
					UserActCollectUtils.AddClickEvent("split_view", "calendar+task", "close_from_om");
					MainWindow.ShowTaskInCalendarChanged();
				}
				break;
			}
			break;
		case 10:
			switch (text[0])
			{
			case 's':
				if (text == "showDetail")
				{
					OnShowDetailClick();
				}
				break;
			case 'a':
				if (text == "addSection")
				{
					OnAddColumn();
				}
				break;
			case 'S':
				if (text == "SyncNotion")
				{
					Communicator.PostNotionSync();
					MainWindowManager.BeginSyncStory();
				}
				break;
			}
			break;
		case 5:
			if (text == "share")
			{
				OnShareClick(null, null);
			}
			break;
		case 13:
			if (text == "showCountDown")
			{
				CountDownOrTaskDateSwitch();
			}
			break;
		}
	}

	private void ShowDisplaySetting()
	{
		KanbanDisplaySettingWindow kanbanDisplaySettingWindow = new KanbanDisplaySettingWindow((UserDao.IsPro() && ViewModel.Identity is TodayProjectIdentity) || ViewModel.Identity is TomorrowProjectIdentity || ViewModel.Identity is WeekProjectIdentity || (ViewModel.Identity is FilterProjectIdentity filterProjectIdentity && filterProjectIdentity.Filter.ContainsDate()));
		kanbanDisplaySettingWindow.Owner = Window.GetWindow(this);
		kanbanDisplaySettingWindow.ShowDialog();
	}

	private async void SelectOrderClick(object sender, MouseButtonEventArgs e)
	{
		if (ViewModel.Identity == null)
		{
			return;
		}
		ProjectIdentity identity = ViewModel.Identity;
		List<SortTypeViewModel> sortTypes;
		if (!(identity is GroupProjectIdentity groupProjectIdentity))
		{
			TagProjectIdentity tagProjectIdentity = identity as TagProjectIdentity;
			sortTypes = ((tagProjectIdentity != null) ? SortOptionHelper.GetTagProjectSortTypeModels(CacheManager.GetTags().Any((TagModel t) => t.parent == tagProjectIdentity.Tag)) : ((identity is NormalProjectIdentity normalProjectIdentity) ? SortOptionHelper.GetNormalProjectSortTypeModels(normalProjectIdentity.Project.IsShareList(), normalProjectIdentity.Project.IsNote) : ((!(identity is FilterProjectIdentity filterProjectIdentity)) ? SortOptionHelper.GetSmartProjectSortTypeModels() : SortOptionHelper.GetSmartProjectSortTypeModels(filterProjectIdentity.OnlyNote))));
		}
		else
		{
			sortTypes = SortOptionHelper.GetGroupProjectSortTypeModels(groupProjectIdentity.DisplayKind, groupProjectIdentity.ContainsShare);
		}
		SortTypeSelector sortTypeSelector = new SortTypeSelector(ViewModel.Identity, sortTypes, ViewModel.Identity.SortOption, inKanban: true, ChooseSortTypePopup);
		sortTypeSelector.SortOptionSelect += OnSortOptionSelect;
		sortTypeSelector.ResetSortOrder += OnResetDateOrderClick;
		sortTypeSelector.Show();
	}

	private async void OnResetDateOrderClick(object sender, int e)
	{
		ChooseSortTypePopup.IsOpen = false;
		await TaskSortOrderService.DeleteAllSortOrderBySortOptionInListId(ViewModel.Identity.SortOption, ViewModel.Identity.GetSortProjectId());
		DataChangedNotifier.NotifySortOptionChanged(ViewModel.Identity.CatId);
		SyncManager.TryDelaySync();
	}

	private async void OnSortOptionSelect(object sender, SortOption sortOption)
	{
		if (ViewModel?.Identity != null)
		{
			UtilLog.Info($"Kanban.Om SetSortType {ViewModel.Identity.Id},type {sortOption}");
			(await ProjectTaskDataProvider.GetProjectData(ViewModel.Identity)).SaveSortOption(sortOption);
			ViewModel.Identity.SortOption = sortOption;
			Load(ViewModel.Identity, forceReload: true);
			SyncManager.TryDelaySync(1000);
		}
	}

	private async void OnHideCompleteClick()
	{
		LocalSettings.Settings.HideComplete = !LocalSettings.Settings.HideComplete;
		LocalSettings.Settings.Save();
		SettingsHelper.PushLocalSettings();
		UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_completed");
	}

	private void OnListActivityClick()
	{
		if (ProChecker.CheckPro(ProType.ListActivities) && ViewModel?.Identity is NormalProjectIdentity normalProjectIdentity)
		{
			ProjectActivityWindow projectActivityWindow = new ProjectActivityWindow(normalProjectIdentity.Id);
			projectActivityWindow.Owner = Window.GetWindow(this);
			projectActivityWindow.Show();
			UserActCollectUtils.AddClickEvent("tasklist", "om", "project_activities");
		}
	}

	private async void OnShowDetailClick()
	{
		LocalSettings.Settings.ShowDetails = !LocalSettings.Settings.ShowDetails;
		LocalSettings.Settings.Save();
		UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_details");
	}

	private void OnShareClick(object sender, RoutedEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("tasklist", "om", "collaboration");
		ShareProject();
	}

	private async void ShareProject()
	{
		ShareProjectDialog.TryShowShareDialog(ViewModel.Identity.Id, Window.GetWindow(this));
	}

	private async void NavigateTask(string taskId)
	{
		await Task.Delay(200);
		List<ColumnViewModel> list = _columnViewModels.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			ColumnViewModel columnModel = list[i];
			if (columnModel?.SourceItems?.FirstOrDefault((TaskBaseViewModel m) => m.Id == taskId) != null)
			{
				double columnLeft = ColumnContainer.GetColumnLeft(i);
				double columnRight = ColumnContainer.GetColumnRight(i);
				double horizontalOffset = KanbanScroller.HorizontalOffset;
				if (horizontalOffset + KanbanScroller.ActualWidth < columnRight)
				{
					KanbanScroller.ScrollToHorizontalOffset(columnRight - KanbanScroller.ActualWidth + 50.0);
				}
				else if (horizontalOffset > columnLeft)
				{
					KanbanScroller.ScrollToHorizontalOffset(columnLeft);
				}
				await Task.Delay(150);
				GetColumnControlById(columnModel.ColumnId)?.TryShowTaskDetail(taskId);
				return;
			}
		}
		TaskDetailWindows.ShowTaskWindows(taskId);
	}

	private async void MenuGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		await Task.Delay(1);
		Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
	}

	private void HideTemplateGuide(object sender, MouseButtonEventArgs e)
	{
		HideTemplateGuide();
	}

	private void HideTemplateGuide()
	{
		TemplateGuidePopup.Visibility = Visibility.Collapsed;
	}

	public void OnScroll(int offset)
	{
		KanbanScroller.ScrollToHorizontalOffset(KanbanScroller.HorizontalOffset + (double)offset);
	}

	private void OnDragMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			if (_startPoint != default(Point) && _startPoint.Y > 25.0)
			{
				Point position = e.GetPosition(KanbanScroller);
				if (Math.Abs(position.X - _startPoint.X) > 0.0)
				{
					KanbanScroller.ScrollToHorizontalOffset(KanbanScroller.HorizontalOffset - position.X + _startPoint.X);
				}
				_startPoint = position;
			}
		}
		else
		{
			_startPoint = default(Point);
		}
	}

	private void CountDownOrTaskDateSwitch()
	{
		LocalSettings.Settings.ShowCountDown = !LocalSettings.Settings.ShowCountDown;
		UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_countdown");
	}

	private void OnContainerMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (CloseQuickAddActionPopups())
		{
			e.Handled = true;
		}
		else if (!BatchControl.IsMouseOver)
		{
			ClearSelected();
		}
	}

	public void SetFoldMenuIcon(bool hideMenu, bool oneColumn)
	{
		FoldImage.SetResourceReference(Path.DataProperty, oneColumn ? "Ic.ProjectList.List" : (hideMenu ? "Ic.ProjectList.ExpandSide" : "Ic.ProjectList.CollapseSide"));
		SetModeColumn(oneColumn);
	}

	private void SetModeColumn(bool isOneColumn)
	{
		if (isOneColumn)
		{
			TitleEditor.IsHitTestVisible = false;
			TitleBorder.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
			TitleBorder.PreviewMouseLeftButtonDown -= OnTitleBorderMouseDown;
			TitleBorder.PreviewMouseLeftButtonDown += OnTitleBorderMouseDown;
			TitleBorder.MouseLeftButtonUp -= OnTitleClick;
			TitleBorder.MouseLeftButtonUp += OnTitleClick;
		}
		else
		{
			TitleEditor.IsHitTestVisible = true;
			TitleBorder.Style = null;
			TitleBorder.PreviewMouseLeftButtonDown -= OnTitleBorderMouseDown;
			TitleBorder.MouseLeftButtonUp -= OnTitleClick;
		}
	}

	private void OnTitleBorderMouseDown(object sender, MouseButtonEventArgs e)
	{
		_titleBorderMouseDown = true;
	}

	private async void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		if (_titleBorderMouseDown)
		{
			_titleBorderMouseDown = false;
			e.Handled = true;
			await Task.Delay(1);
			Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
		}
	}

	public void HideFoldMenuIcon()
	{
		MenuPathGrid.Visibility = Visibility.Collapsed;
		TitleGrid.Margin = new Thickness(2.0, 27.0, 0.0, 0.0);
	}

	public async Task BatchPinTask()
	{
		List<string> selectedTaskIds = GetSelectedTaskIds();
		List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(selectedTaskIds);
		await TaskService.BatchStarTaskOrNote(pin: !(tasksByIds.Count > 0 && tasksByIds.All((TaskBaseViewModel t) => t.IsPinned)), ids: selectedTaskIds, project: ViewModel.Identity.CatId);
	}

	public void BatchOpenSticky()
	{
		TaskStickyWindow.ShowTaskSticky(GetSelectedTaskIds());
	}

	public async Task TryBatchSetDate(DateTime? date)
	{
		await TaskService.BatchSetDate(GetSelectedTaskIds(), date);
	}

	public async Task TryBatchSetPriority(int priority)
	{
		await TaskService.BatchSetPriority(GetSelectedTaskIds(), priority);
	}

	private void OnKanbanScrollerSizeChanged(object sender, SizeChangedEventArgs e)
	{
		ColumnContainer.SetScrollOffset(KanbanScroller.HorizontalOffset, base.ActualWidth);
		ColumnContainer.SetItemMaxHeight();
		TitleEditor.SetMaxWidth(base.ActualWidth - 150.0);
	}

	private void OnStartDrag(object sender, MouseButtonEventArgs e)
	{
		if (CloseQuickAddActionPopups())
		{
			e.Handled = true;
		}
		else if (AddingModel == null && !ColumnMouseOver() && !AddColumnControl.IsMouseOver)
		{
			ClearSelected();
			_startPoint = e.GetPosition(KanbanScroller);
			Mouse.OverrideCursor = Cursors.Hand;
			KanbanGrid.CaptureMouse();
			PopupStateManager.OnViewPopupOpened();
			PopupStateManager.OnAddPopupOpened();
		}
	}

	private void OnStopDrag(object sender, MouseButtonEventArgs e)
	{
		if (_startPoint != default(Point))
		{
			_startPoint = default(Point);
			Mouse.OverrideCursor = null;
			KanbanGrid.ReleaseMouseCapture();
			PopupStateManager.OnViewPopupClosed();
			PopupStateManager.OnAddPopupClosed();
		}
	}

	public void InsertColumn(int index, ColumnViewModel model)
	{
		ColumnContainer.Insert(index, model);
	}

	public void DeleteColumn(KanbanColumnView column)
	{
		ColumnViewModel model = column.DataContext as ColumnViewModel;
		ColumnContainer.RemoveItem(model);
		ShowAddColumn();
	}

	public async Task SetupColumn(IEnumerable<ColumnViewModel> data)
	{
		_columnViewModels.Clear();
		foreach (ColumnViewModel item in data.Select((ColumnViewModel model) => model.Clone()))
		{
			item.TaskCount = item.SourceItems.Count((TaskBaseViewModel s) => s.Status == 0);
			_columnViewModels.Add(item);
		}
		ColumnContainer.SetItemModels(_columnViewModels);
		if (_columnViewModels.Count == 0)
		{
			ShowEmptyImage();
			return;
		}
		KanbanScroller.Visibility = Visibility.Visible;
		EmptyGrid.Visibility = Visibility.Collapsed;
	}

	private async Task ShowEmptyImage()
	{
		KanbanScroller.Visibility = Visibility.Collapsed;
		EmptyGrid.Visibility = Visibility.Visible;
		SortProjectData sortProjectData = await ProjectTaskDataProvider.GetProjectData(ViewModel.Identity);
		bool allCompleted = LocalSettings.Settings.HideComplete && !sortProjectData.IsCompleted && ViewModel.SourceModels.Any();
		EmptyImage.SetIconsSource(sortProjectData.GetEmptyIconsSource(allCompleted));
		string emptyTitle = sortProjectData.GetEmptyTitle(allCompleted);
		EmptyText.Text = emptyTitle;
		EmptyAddText.Text = Utils.GetString(ViewModel.Identity.IsNote ? "AddaNote" : "AddaTask");
	}

	public KanbanColumnView GetColumnControlById(string columnId)
	{
		return ColumnContainer.GetColumnById(columnId);
	}

	public void ScrollToColumn(string columnId)
	{
		GetColumnControlById(columnId)?.BringIntoView();
	}

	public bool ColumnMouseOver()
	{
		foreach (KanbanColumnView child in ColumnContainer.Children)
		{
			if (child.IsMouseOver || child.IsPopupOpen())
			{
				return true;
			}
		}
		return false;
	}

	private bool CloseQuickAddActionPopups()
	{
		return ColumnContainer.Children.OfType<KanbanColumnView>().Any((KanbanColumnView control) => control.CloseQuickAddActionPopups());
	}

	private async void OnAddColumnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (ViewModel.Identity is NormalProjectIdentity normalProjectIdentity)
		{
			if (CloseAddButton.IsMouseOver)
			{
				ProjectModel projectById = CacheManager.GetProjectById(normalProjectIdentity.Id);
				if (projectById != null)
				{
					projectById.ShowAddColumn = false;
					await ProjectDao.TryUpdateProject(projectById);
				}
				ViewModel.ShowAdd = false;
				Utils.Toast(Utils.GetString("KanbanClosedAddColumnTips"));
				return;
			}
			if (AddColumnControl.IsMouseOver)
			{
				OnAddColumn();
			}
		}
		e.Handled = true;
	}

	private void OnSwitchView(object sender, string e)
	{
		Mouse.Capture(null);
		MorePopup.IsOpen = false;
		if (e == "list" || e == "timeline")
		{
			UserActCollectUtils.AddClickEvent("list_mode", "switch", e);
			if (!(e == "timeline") || ProChecker.CheckPro(ProType.TimeLine, App.Window))
			{
				ViewModel.Identity.SwitchViewMode(e, Utils.FindParent<TaskView>(this)?.ShowInCal ?? false);
			}
		}
	}

	public void ReloadDataAndColumnQuickAddView()
	{
		ColumnContainer.ReloadQuickAddView();
		Reload(forceReload: true);
	}

	public void TabSelect()
	{
	}

	public void BatchDeleteTask()
	{
		List<string> selectedTaskIds = _batchHelper.SelectedTaskIds;
		if (selectedTaskIds != null && selectedTaskIds.Count > 1)
		{
			TaskService.BatchDeleteTaskByIds(_batchHelper.SelectedTaskIds);
		}
	}

	private void OnAddTaskClick(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<ListViewContainer>(this)?.ShowAddTask();
	}

	public void Clear()
	{
		ViewModel.SourceModels = new List<TaskBaseViewModel>();
		ViewModel.Name = string.Empty;
		_columnViewModels.Do(delegate
		{
			_columnViewModels.Value.ForEach(delegate(ColumnViewModel v)
			{
				v.Clear();
			});
		});
	}

	public void Dispose()
	{
		TitleEditor.SetText(string.Empty);
		ViewModel.SourceModels = new List<TaskBaseViewModel>();
		_columnViewModels.Do(delegate
		{
			_columnViewModels.Value.ForEach(delegate(ColumnViewModel v)
			{
				v.Dispose();
			});
		});
		ViewModel.Dispose();
		ColumnContainer.Dispose();
	}

	public void StartRemoveAddModel()
	{
		AddingModel?.SetInvalid();
	}

	public void RemoveAddingTaskModel()
	{
		AddingModel = null;
	}

	public SectionAddTaskViewModel GetAddingTaskModel(ProjectIdentity identity, Section section)
	{
		if (identity is ColumnProjectIdentity columnProjectIdentity && section != null)
		{
			AddingModel = new SectionAddTaskViewModel
			{
				ColumnId = columnProjectIdentity.ColumnId,
				SectionId = section.SectionId
			};
		}
		return AddingModel;
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		ColumnContainer.SetScrollOffset(KanbanScroller.HorizontalOffset, base.ActualWidth);
	}

	public void ClearEvent()
	{
		this.TaskRemoved = null;
		this.TaskAdded = null;
		this.SetSelected = null;
	}

	public void ReloadIdentity()
	{
		ViewModel.SetIdentity(ViewModel.Identity.Copy(ViewModel.Identity));
		Reload(forceReload: true);
	}

	public async void ExpandOrFoldAllTask()
	{
		bool allOpen = true;
		List<TaskListViewModel> taskListVms = new List<TaskListViewModel>();
		foreach (ColumnViewModel item in _columnViewModels.Value)
		{
			TaskListViewModel tM = new TaskListViewModel(item.Identity)
			{
				ListDisplayType = TaskListDisplayType.Kanban
			};
			tM.SourceModels = item.SourceItems;
			TaskListViewModel taskListViewModel = tM;
			taskListViewModel.Items = new ObservableCollection<DisplayItemModel>(await tM.BuildItems(loadMore: true, item.SourceItems));
			taskListVms.Add(tM);
			allOpen = allOpen && tM.IsAllTaskOpen();
		}
		foreach (TaskListViewModel item2 in taskListVms)
		{
			await item2.OpenOrCloseAllTasksInKanban(!allOpen);
		}
		Reload(forceReload: true);
	}

	public async void ExpandOrFoldAllSection()
	{
		bool allOpen = true;
		List<TaskListViewModel> taskListVms = new List<TaskListViewModel>();
		foreach (ColumnViewModel item in _columnViewModels.Value)
		{
			TaskListViewModel tM = new TaskListViewModel(item.Identity)
			{
				ListDisplayType = TaskListDisplayType.Kanban
			};
			tM.SourceModels = item.SourceItems;
			TaskListViewModel taskListViewModel = tM;
			taskListViewModel.Items = new ObservableCollection<DisplayItemModel>(await tM.BuildItems(loadMore: true, item.SourceItems));
			taskListVms.Add(tM);
			allOpen = allOpen && tM.IsAllSectionOpen();
		}
		foreach (TaskListViewModel item2 in taskListVms)
		{
			await item2.OpenOrCloseAllSections(item2.Items.Where((DisplayItemModel i) => i.IsSection).ToList(), !allOpen);
		}
		Reload(forceReload: true);
	}

	private void OnKanbanDragEnter()
	{
		double num = (double)FindResource("KanbanColumnWidth");
		TickDragHelper.ChangeDragContent(1, num - 20.0);
	}

	private void OnDragLeave()
	{
		ClearDragging();
		foreach (ColumnViewModel item in _columnViewModels.Value)
		{
			item.CanDrop = false;
			item.MouseOver = false;
		}
		_autoScroll = false;
	}

	private void OnDragOver(object sender, MouseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		Window window = Window.GetWindow(this);
		if (window == null || !window.IsMouseCaptured)
		{
			return;
		}
		if (TaskDetailPopup.IsDragOver(e))
		{
			ClearDragging();
		}
		else
		{
			if (!ViewModel.Identity.CanEdit)
			{
				return;
			}
			Point position = e.GetPosition(this);
			bool flag = !(position.X < 0.0) && !(position.X > base.ActualWidth) && !(position.Y < 0.0) && !(position.Y > base.ActualHeight);
			if (_dragOver != flag)
			{
				if (flag)
				{
					OnKanbanDragEnter();
				}
				else
				{
					OnDragLeave();
				}
				_dragOver = flag;
			}
			if (!flag)
			{
				return;
			}
			object dragData = TickDragHelper.GetDragData();
			if (!(dragData is DisplayItemModel) && !(dragData is CalendarDisplayViewModel { IsCheckItem: false, IsCalendarEvent: false }))
			{
				return;
			}
			if (string.IsNullOrEmpty(_startDragColumnId))
			{
				CalendarDisplayViewModel c = dragData as CalendarDisplayViewModel;
				if (c != null)
				{
					foreach (ColumnViewModel item in _columnViewModels.Value)
					{
						if (item.SourceItems.Any((TaskBaseViewModel t) => t.Id == c.TaskId))
						{
							SetStartDragColumnId(item.ColumnId);
						}
					}
				}
				if (string.IsNullOrEmpty(_startDragColumnId))
				{
					SetStartDragColumnId("fromOther");
				}
			}
			ShiftKanbanOnScrollEdge(e.GetPosition(this).X);
			KanbanColumnView mousePointItem = Utils.GetMousePointItem<KanbanColumnView>(new Point(e.GetPosition(this).X, 120.0), this);
			if (mousePointItem == null)
			{
				return;
			}
			foreach (ColumnViewModel item2 in _columnViewModels.Value)
			{
				item2.CanDrop = false;
				item2.MouseOver = false;
			}
			string startDragColumnId = _startDragColumnId;
			if (mousePointItem.ColumnId != startDragColumnId)
			{
				mousePointItem.OnDragMouseMove(e, startDragColumnId);
				e.Handled = true;
			}
		}
	}

	private async Task OnDrop(MouseEventArgs e, string fromColumnId)
	{
		if (!TickDragHelper.Dragging)
		{
			return;
		}
		TaskDragHelpModel.DragHelp.IsDragging = false;
		_autoScroll = false;
		object dragData = TickDragHelper.GetDragData();
		DisplayItemModel model = dragData as DisplayItemModel;
		if (model == null && dragData is CalendarDisplayViewModel calendarDisplayViewModel)
		{
			model = new DisplayItemModel
			{
				SourceViewModel = calendarDisplayViewModel.SourceViewModel
			};
			if (calendarDisplayViewModel.BatchSelected)
			{
				HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
				if (draggingTaskIds != null && draggingTaskIds.Count > 0)
				{
					model.BatchMode = true;
					model.BatchModels = new List<DisplayItemModel>();
					foreach (TaskBaseViewModel item in from task in TickDragHelper.DraggingTaskIds.ToList().Select(TaskCache.GetTaskById)
						where task != null
						select task)
					{
						model.BatchModels.Add(new DisplayItemModel(item));
					}
				}
			}
		}
		if (model != null)
		{
			KanbanColumnView column = Utils.GetMousePointItem<KanbanColumnView>(new Point(e.GetPosition(this).X, 120.0), this);
			if (column != null && column.ColumnId != fromColumnId && (column.GetModel()?.CanDrop ?? false))
			{
				foreach (ColumnViewModel item2 in _columnViewModels.Value)
				{
					TaskListViewModel listViewModel = item2.GetListViewModel();
					if (listViewModel == null)
					{
						continue;
					}
					if (model.BatchMode)
					{
						foreach (DisplayItemModel batchModel in model.BatchModels)
						{
							listViewModel.Items?.Remove(batchModel);
							listViewModel.SourceModels?.Remove(batchModel.SourceViewModel);
						}
					}
					else
					{
						listViewModel.Items?.Remove(model);
						listViewModel.SourceModels?.Remove(model.SourceViewModel);
					}
				}
				_dropping = true;
				if (fromColumnId == "fromOther")
				{
					if (!model.BatchMode)
					{
						await model.SourceViewModel.SetIdentityProperties(ViewModel.Identity, Utils.FindParent<IToastShowWindow>(this));
					}
					else
					{
						foreach (DisplayItemModel batchModel2 in model.BatchModels)
						{
							await batchModel2.SourceViewModel.SetIdentityProperties(ViewModel.Identity, Utils.FindParent<IToastShowWindow>(this));
						}
					}
				}
				try
				{
					List<DisplayItemModel> batchModels = model.BatchModels;
					if (batchModels == null || batchModels.Count <= 0)
					{
						await OnTaskDrop(model, column.GetModel());
					}
					else
					{
						model.DragTaskIds = model.DragTaskIds ?? TickDragHelper.DraggingTaskIds?.ToList();
						await OnBatchTaskDrop(model, column.GetModel());
					}
				}
				finally
				{
					_dropping = false;
					if (fromColumnId == "fromOther" || model.BatchMode)
					{
						Reload(forceReload: true);
					}
					ClearDragging();
				}
				SyncManager.TryDelaySync();
			}
		}
		e.Handled = true;
	}

	private void ClearDragging()
	{
		foreach (ColumnViewModel item in _columnViewModels.Value)
		{
			item.CanDrop = false;
			item.MouseOver = false;
		}
		_autoScroll = false;
	}

	private async void OnDragDrop(object sender, MouseEventArgs e)
	{
		if (_dragOver)
		{
			_dragOver = false;
			await OnDrop(e, _startDragColumnId);
		}
		_startDragColumnId = "";
		ClearDragging();
	}

	public void SetStartDragColumnId(string columnId)
	{
		_startDragColumnId = columnId;
		if (string.IsNullOrEmpty(columnId))
		{
			_autoScroll = false;
		}
	}

	public void SetBackGround(bool transparent)
	{
		if (LocalSettings.Settings.ExtraSettings.InterfaceStyle == InterfaceStyle.Card)
		{
			KanbanContainerGrid.Background = Brushes.Transparent;
		}
		else if (transparent)
		{
			KanbanContainerGrid.Background = Brushes.Transparent;
		}
		else
		{
			KanbanContainerGrid.SetResourceReference(Control.BackgroundProperty, "KanbanContainerColor");
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		EmptyGrid.Margin = new Thickness(0.0, e.NewSize.Height * 0.28, 0.0, 0.0);
	}

	public void CancelBatchSelect()
	{
		HideBatchSelectControl();
	}

	private void AIRecommendClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("tasklist", "om", "ai_suggested_tasks");
		if (AIRecommendWindow.Window == null)
		{
			AIRecommendWindow.Window = new AIRecommendWindow
			{
				Owner = Window.GetWindow(this)
			};
		}
		AIRecommendWindow.Window.Show();
		AIRecommendWindow.Window.Activate();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/kanban/kanbancontainer.xaml", UriKind.Relative);
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
			Root = (KanbanContainer)target;
			Root.PreviewMouseMove += OnContainerMouseMove;
			Root.SizeChanged += OnSizeChanged;
			break;
		case 2:
			KanbanContainerGrid = (Grid)target;
			KanbanContainerGrid.MouseLeftButtonDown += OnContainerMouseDown;
			break;
		case 3:
			TemplateGuidePopup = (Grid)target;
			TemplateGuidePopup.PreviewMouseLeftButtonUp += HideTemplateGuide;
			break;
		case 4:
			MenuPathGrid = (Border)target;
			MenuPathGrid.MouseLeftButtonUp += MenuGrid_MouseLeftButtonUp;
			break;
		case 5:
			FoldImage = (Path)target;
			break;
		case 6:
			OptionPanel = (StackPanel)target;
			break;
		case 7:
			AIRecommendBorder = (Border)target;
			AIRecommendBorder.MouseLeftButtonUp += AIRecommendClick;
			break;
		case 8:
			AIRecommendHintPopup = (Popup)target;
			break;
		case 9:
			((Grid)target).MouseLeftButtonUp += SelectOrderClick;
			break;
		case 10:
			ChooseSortTypePopup = (EscPopup)target;
			break;
		case 11:
			MoreGrid = (Grid)target;
			MoreGrid.MouseLeftButtonUp += MoreGridClick;
			break;
		case 12:
			MorePopup = (EscPopup)target;
			break;
		case 13:
			TitleGrid = (Grid)target;
			break;
		case 14:
			TitleBorder = (Border)target;
			break;
		case 15:
			TitleEditor = (EmojiTitleEditor)target;
			break;
		case 16:
			ShareGrid = (Grid)target;
			ShareGrid.MouseLeftButtonUp += OnShareClick;
			break;
		case 17:
			SharePath = (Path)target;
			break;
		case 18:
			KanbanScroller = (ScrollViewer)target;
			KanbanScroller.PreviewMouseWheel += OnMouseWheel;
			KanbanScroller.SizeChanged += OnKanbanScrollerSizeChanged;
			KanbanScroller.ScrollChanged += OnScrollChanged;
			break;
		case 19:
			KanbanGrid = (Grid)target;
			KanbanGrid.MouseMove += OnDragMove;
			KanbanGrid.PreviewMouseLeftButtonDown += OnStartDrag;
			KanbanGrid.PreviewMouseLeftButtonUp += OnStopDrag;
			break;
		case 20:
			ColumnContainer = (KanbanColumnCanvas)target;
			break;
		case 21:
			AddColumnControlGrid = (Grid)target;
			AddColumnControlGrid.MouseLeftButtonUp += OnAddColumnMouseUp;
			break;
		case 22:
			AddColumnControl = (Border)target;
			break;
		case 23:
			CloseAddButton = (Border)target;
			break;
		case 24:
			EmptyGrid = (StackPanel)target;
			break;
		case 25:
			EmptyImage = (EmptyView)target;
			break;
		case 26:
			EmptyText = (TextBlock)target;
			break;
		case 27:
			EmptyAddText = (TextBlock)target;
			EmptyAddText.MouseLeftButtonUp += OnAddTaskClick;
			break;
		case 28:
			BatchControl = (BatchTaskControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
