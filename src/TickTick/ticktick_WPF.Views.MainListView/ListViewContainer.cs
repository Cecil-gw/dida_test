using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.ProjectList;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.MainListView;

public class ListViewContainer : Grid, INavigateProject
{
	public ListMode Mode = ListMode.Normal;

	private readonly ContentControl _projectContent = new ContentControl
	{
		RenderTransform = new TranslateTransform(),
		MinWidth = 230.0
	};

	private TaskView _taskView = new TaskView();

	private Border _parentBorder;

	public bool ParallelShow;

	public bool ShowInCal;

	private static int ThreeFrameWidth = 896;

	private const int TwoFrameWidth = 532;

	private const int ProjectMenuMiniWidth = 230;

	private ColumnDefinition _firstColumn;

	private ColumnDefinition _secondColumn;

	private MainProjectStatus _menuStatus = MainProjectStatus.Show;

	private GridSplitter _leftSplit;

	private bool _needHideDetail;

	private ScaleTransform _projectScale;

	private ProjectListView _projectList;

	private EscPopup _projectListPopup;

	private static ConcurrentDictionary<string, ListViewContainer> _listViews = new ConcurrentDictionary<string, ListViewContainer>();

	public ProjectIdentity ProjectIdentity => _projectList?.GetSelectedProject();

	private bool MenuShow => (_menuStatus & MainProjectStatus.Show) == MainProjectStatus.Show;

	public ListViewColumnStyle ColumnStyle
	{
		get
		{
			return LocalSettings.Settings.ExtraSettings.ListViewColumnStyle;
		}
		set
		{
			LocalSettings.Settings.ExtraSettings.ListViewColumnStyle = value;
		}
	}

	public ProjectListView ProjectList => _projectList;

	public ListViewContainer(Border parentBorder)
	{
		_parentBorder = parentBorder;
		base.MinWidth = 300.0;
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
		base.SizeChanged += OnListViewSizeChanged;
		InitGridColumns();
		_projectList = new ProjectListView(this);
		base.ClipToBounds = true;
		_projectContent.SetValue(Grid.ColumnProperty, 0);
		base.Children.Add(_projectContent);
		HideDetail();
		_projectContent.Content = _projectList;
		_projectContent.RenderTransformOrigin = new Point(0.0, 0.5);
		_taskView.SetValue(Grid.ColumnProperty, 1);
		base.Children.Add(_taskView);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		CalendarEventChangeNotifier.Deleted += OnEventDeleted;
		DataChangedNotifier.CalendarChanged += OnCalendarChanged;
		DataChangedNotifier.ViewModeChanged += OnProjectViewModeChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
		DataChangedNotifier.InterfaceStyleChanged += OnThemeChanged;
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnSmartProjectsChanged, "SmartProjects");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnProjectNumChanged, "ProjectNum");
		InitColumnSize();
	}

	public void Clear()
	{
		try
		{
			if (_parentBorder != null)
			{
				if (object.Equals(_parentBorder.Child, this))
				{
					_parentBorder.Child = null;
				}
				_parentBorder = null;
			}
			if (_projectList != null)
			{
				_projectList.Children.Clear();
				_projectList = null;
			}
			if (_taskView != null)
			{
				_taskView?.Clear();
				_taskView = null;
			}
			base.Children.Clear();
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		DataChangedNotifier.CalendarChanged -= OnCalendarChanged;
		CalendarEventChangeNotifier.Deleted -= OnEventDeleted;
		DataChangedNotifier.ViewModeChanged -= OnProjectViewModeChanged;
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
		DataChangedNotifier.InterfaceStyleChanged -= OnThemeChanged;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnSmartProjectsChanged, "SmartProjects");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnProjectNumChanged, "ProjectNum");
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		_projectList?.LoadData(refreshCount: false);
	}

	private void OnProjectNumChanged(object sender, PropertyChangedEventArgs e)
	{
		TaskCountCache.Clear();
		_projectList?.LoadData();
	}

	private void OnSmartProjectsChanged(object sender, PropertyChangedEventArgs e)
	{
		_projectList?.LoadData();
	}

	public void OnProjectSelected()
	{
		base.Dispatcher.Invoke(delegate
		{
			Mode = ListMode.Normal;
			ThemeUtil.TryClearImageCached(force: true);
			ProjectIdentity selectedProject = _projectList.GetSelectedProject();
			if (selectedProject != null)
			{
				if (_projectListPopup != null)
				{
					_projectListPopup.IsOpen = false;
				}
				OnProjectSelect(selectedProject);
			}
		});
	}

	private void OnProjectSelect(ProjectIdentity identity, string taskId = null)
	{
		_taskView?.OnProjectSelect(identity, taskId, showDetailAnim: true);
		_taskView?.SetFoldMenuIcon(!MenuShow, ColumnStyle == ListViewColumnStyle.OneColumn);
	}

	private void OnEventDeleted(object sender, string eventId)
	{
		HideDetail();
	}

	private void OnCalendarChanged(object sender, EventArgs e)
	{
		_projectList?.LoadData();
	}

	public IToastShowWindow GetToastWindow()
	{
		return Utils.FindParent<IToastShowWindow>(this);
	}

	public void TryToast(string getString)
	{
		GetToastWindow()?.TryToastString(null, getString);
	}

	public void EditProject(string projectId)
	{
		_projectList?.EditProject(projectId);
	}

	public void SelectProject(ProjectIdentity identity)
	{
		_projectList?.SelectProject(identity);
	}

	public void SelectTaskGroup(string projectId, string columnId)
	{
		ProjectIdentity projectIdentity = ProjectIdHelper.GetProjectIdentity(projectId);
		if (projectIdentity != null && !string.IsNullOrEmpty(columnId))
		{
			Mode = ListMode.Normal;
			ThemeUtil.TryClearImageCached(force: true);
			_projectList?.SelectProject(projectIdentity, invokeEvent: false);
			_taskView?.OnProjectSelect(projectIdentity, null, showDetailAnim: false, columnId);
			_taskView?.SetFoldMenuIcon(!MenuShow, ColumnStyle == ListViewColumnStyle.OneColumn);
		}
	}

	public async void LoadSavedProject(bool delay = false)
	{
		ProjectListView project = _projectList;
		if (project != null)
		{
			if (delay)
			{
				project.LoadSavedProject();
				await Task.Delay(300);
				await project.LoadData();
			}
			else
			{
				await project.LoadData();
				project.LoadSavedProject();
			}
		}
	}

	public ProjectIdentity GetSelectedProject()
	{
		if (Mode == ListMode.Search)
		{
			return new SearchProjectIdentity();
		}
		return _projectList?.SelectedItem?.GetIdentity();
	}

	public async void NavigateTask(TaskModel task)
	{
		if (task.deleted == 1)
		{
			_projectList?.SelectTrash();
			return;
		}
		ProjectModel projectById = CacheManager.GetProjectById(task.projectId);
		if (projectById != null)
		{
			_projectList?.SelectProject(new NormalProjectIdentity(projectById), invokeEvent: false);
			OnProjectSelect(new NormalProjectIdentity(projectById), task.id);
		}
	}

	public void ReloadView(bool hideDetail = false)
	{
		base.Dispatcher.Invoke(delegate
		{
			_projectList?.LoadData();
			_taskView?.ReloadView(hideDetail);
		});
	}

	public void TryExtractDetail()
	{
		_taskView?.TryExtractDetail();
	}

	public void HideDetail()
	{
		_taskView?.HideDetail();
	}

	public async Task StartSearch(SearchExtra searchExtra, bool restore)
	{
		SetMode(ListMode.Search);
		SearchFilterModel searchFilterModel = searchExtra.Filter ?? new SearchFilterModel();
		searchFilterModel.Key = searchExtra.SearchKey;
		searchFilterModel.Category = searchExtra.Category;
		SearchHelper.SearchFilter.ApplySearchFilter(searchFilterModel);
		if (!restore && searchExtra.Filter == null)
		{
			SearchHelper.SearchFilter.ResetAllFilterConditions();
		}
		SearchHelper.SearchCategory = searchExtra.Category;
		SearchHelper.SearchKey = searchExtra.SearchKey;
		SearchHelper.Tags = searchExtra.Tags;
		SearchHelper.SearchFilter.SearchKey = searchExtra.SearchKey;
		SearchHelper.SearchFilter.SearchTags = searchExtra.Tags;
		SearchHelper.SearchFilter.Category = searchExtra.Category;
		SearchHelper.SearchFilter.SyncSearchTypeFromCategory();
		SearchHelper.SearchFilter.RefreshCategoryState();
		SearchHelper.SearchFilter.Searched = false;
		SearchHelper.ClearTaskSearchModels();
		_taskView?.ResetSearchFilterControl();
		if (MenuShow && ColumnStyle != ListViewColumnStyle.OneColumn)
		{
			_firstColumn.MaxWidth = 0.0;
			_firstColumn.MinWidth = 0.0;
		}
		if (_taskView != null)
		{
			await _taskView.SwitchListAndLoad(new SearchProjectIdentity(), string.IsNullOrEmpty(searchExtra.SearchId));
			await Task.Delay(100);
			_taskView.SelectId(searchExtra.SearchId);
		}
	}

	public async void StopSearch()
	{
		SetMode(ListMode.Normal);
		HideDetail();
		if (MenuShow)
		{
			_firstColumn.MaxWidth = GetFirstColumnMaxWidth();
		}
	}

	public async void OnWindowKeyUp(KeyEventArgs e)
	{
		_taskView?.OnKeyUp(e);
	}

	public void TryPrint(bool isDetail)
	{
		_taskView?.TryPrint(isDetail);
	}

	public void TryBatchSetPriority(int priority)
	{
		_taskView?.TryBatchSetPriority(priority);
	}

	public void BatchOpenSticky()
	{
		_taskView?.BatchOpenSticky();
	}

	public void TryBatchSetDate(DateTime? date)
	{
		_taskView?.TryBatchSetDate(date);
	}

	public void BatchPinTask()
	{
		_taskView?.BatchPinTask();
	}

	public void BatchDeleteTask()
	{
		_taskView?.BatchDeleteTask();
	}

	public bool TabListAndDetail(IInputElement focusItem)
	{
		return _taskView?.TabListAndDetail(focusItem) ?? false;
	}

	public async Task ExpandOrFoldAllTask(bool isOpen)
	{
		_taskView?.ExpandOrFoldAllTask(isOpen);
	}

	public async Task ExpandOrFoldAllSection()
	{
		_taskView?.ExpandOrFoldAllSection();
	}

	public void TabSelectQuickAddItem()
	{
		_taskView?.TabSelectItem();
	}

	public void TabKanbanSelect()
	{
		_taskView?.TabKanbanSelect();
	}

	public void OnScroll(int offset)
	{
		_taskView?.OnTouchScroll(offset);
	}

	public async Task TryToastMoveControl(TaskModel task, bool isToday, bool hideTitle = false)
	{
		if (task != null && (string.IsNullOrEmpty(task.parentId) || !(_taskView?.CheckTaskInList(task.parentId) ?? false)) && !MoveToastHelper.CheckTaskMatched(GetSelectedProject(), task))
		{
			GetToastWindow()?.Toast(new MoveToastControl(isToday, this, hideTitle ? string.Empty : task.title));
		}
	}

	public void NavigateProjectById(string projectId)
	{
		SelectProject(ProjectIdHelper.GetProjectIdentity(projectId));
	}

	public void NavigateTodayProject()
	{
		_projectList?.TrySelectSmartProject(SmartListType.Today);
	}

	public void NavigateTomorrowProject()
	{
		_projectList?.TrySelectSmartProject(SmartListType.Tomorrow);
	}

	public void ShowAddTask()
	{
		Utils.FindParent<MainWindow>(this)?.InputCommand();
	}

	public void SetMode(ListMode mode)
	{
		Mode = mode;
	}

	public void ReloadTaskListAndSelect(bool forceReload = false, string taskId = "", bool forceFocus = true)
	{
		_taskView?.ReloadTaskListAndSelect(forceReload, taskId, forceFocus);
	}

	public void ReSearch()
	{
		_taskView?.ReSearch();
	}

	public void HideDetailOnTasksDelete(List<string> taskIds)
	{
		_taskView?.HideDetailOnTasksDelete(taskIds);
	}

	public TaskView GetTaskView()
	{
		return _taskView;
	}

	public bool CanShowDetailPage()
	{
		if (ColumnStyle != ListViewColumnStyle.ThreeColumn)
		{
			if (!ParallelShow)
			{
				return LocalSettings.Settings.ExtraSettings.TaskDetailLayout != "Popup";
			}
			return false;
		}
		return true;
	}

	public void AddItemClickEvent()
	{
		if (ParallelShow)
		{
			UserActCollectUtils.AddClickEvent("split_view", ShowInCal ? "calendar+task" : "task+calendar", "task_detail");
		}
	}

	public void ExitBatchSelect()
	{
		_taskView?.ExitBatchSelect();
	}

	private void InitGridColumns()
	{
		_firstColumn = new ColumnDefinition
		{
			MaxWidth = 305.0,
			Width = new GridLength(230.0)
		};
		_secondColumn = new ColumnDefinition();
		base.ColumnDefinitions.Add(_firstColumn);
		base.ColumnDefinitions.Add(_secondColumn);
		_leftSplit = new GridSplitter
		{
			Width = 3.0,
			IsTabStop = false,
			Background = Brushes.Transparent,
			FocusVisualStyle = null,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Stretch,
			BorderThickness = new Thickness(0.0, 0.0, 1.0, 0.0)
		};
		_projectScale = new ScaleTransform();
		_projectContent.RenderTransform = _projectScale;
		_leftSplit.SetResourceReference(Control.BorderBrushProperty, "ProjectSplitColor");
		_leftSplit.SetValue(Grid.ColumnProperty, 0);
		_leftSplit.SetValue(Panel.ZIndexProperty, 1000);
		base.Children.Add(_leftSplit);
		_leftSplit.DragCompleted += OnSplitDragCompleted;
		_leftSplit.DragDelta += OnSplitDragging;
	}

	private void InitColumnSize()
	{
		double firstColumnMaxWidth = GetFirstColumnMaxWidth();
		double num = Utils.FindParent<IListViewParent>(this)?.GetProjectWidth() ?? 0.0;
		num = ((num < 230.0) ? 230.0 : ((num > firstColumnMaxWidth) ? firstColumnMaxWidth : num));
		double pixels = ((num > 0.0) ? num : 230.0);
		_firstColumn.Width = new GridLength(pixels);
		_secondColumn.Width = new GridLength(1.0, GridUnitType.Star);
		SetListViewSize();
		if ((ShowInCal ? LocalSettings.Settings.ExtraSettings.ProjectParallelMenuFold : LocalSettings.Settings.ExtraSettings.MenuFold) != !MenuShow)
		{
			if (ColumnStyle != ListViewColumnStyle.OneColumn)
			{
				ShowProjectMenu(manual: false);
				return;
			}
			_menuStatus |= MainProjectStatus.Fold;
			_menuStatus &= ~MainProjectStatus.Show;
			_taskView?.SetFoldMenuIcon(!MenuShow, ColumnStyle == ListViewColumnStyle.OneColumn);
		}
	}

	private void OnListViewSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 1.0)
		{
			SetListViewSize();
		}
	}

	private void SetListViewSize()
	{
		double num = Utils.FindParent<IListViewParent>(this)?.GetListContainerWidth() ?? 0.0;
		if (num <= 0.0)
		{
			num = 1140.0;
		}
		_taskView?.ResetColumnWidth(num - _firstColumn.ActualWidth);
		SetColumns(num);
	}

	private void SetColumns(double width)
	{
		if (width < 532.0)
		{
			SetOneColumn();
		}
		else if (width < (double)ThreeFrameWidth)
		{
			SetTwoColumns();
		}
		else
		{
			SetThreeColumns();
		}
		_taskView?.SetFoldMenuIcon(!MenuShow, ColumnStyle == ListViewColumnStyle.OneColumn);
	}

	public void SetParallelShow(bool showWithCal, bool inCal)
	{
		ParallelShow = showWithCal;
		ShowInCal = inCal;
		SetListViewSize();
		_taskView?.SetParallelShow(showWithCal, inCal);
		_projectList?.SetParallelShow(showWithCal, inCal);
		SetProjectMenuShow();
		if (showWithCal)
		{
			_leftSplit.SetResourceReference(Control.BorderBrushProperty, "BaseColorOpacity5");
		}
		else
		{
			_leftSplit.SetResourceReference(Control.BorderBrushProperty, "ProjectSplitColor");
		}
	}

	private void FoldThirdColumn()
	{
		_taskView?.FoldDetail();
	}

	private void ExpandThirdColumn()
	{
		_taskView?.ExpandDetail();
	}

	private void SetOneColumn()
	{
		if (ColumnStyle != ListViewColumnStyle.OneColumn)
		{
			FoldThirdColumn();
			_taskView?.SetListColumnSpan(2, 0.0, 0.0);
			FoldMenu(anim: false);
			ColumnStyle = ListViewColumnStyle.OneColumn;
		}
	}

	private void SetTwoColumns()
	{
		if (ColumnStyle != ListViewColumnStyle.TwoColumn || (_taskView != null && !_taskView.IsFoldColumn))
		{
			FoldThirdColumn();
			ColumnStyle = ListViewColumnStyle.TwoColumn;
			SetProjectMenuShow();
			if (MenuShow && Mode == ListMode.Normal)
			{
				_firstColumn.BeginAnimation(ColumnDefinition.MinWidthProperty, null);
				_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
				_firstColumn.MaxWidth = 300.0;
			}
		}
	}

	private void SetThreeColumns()
	{
		if (MenuShow && Mode == ListMode.Normal)
		{
			double firstColumnMaxWidth = GetFirstColumnMaxWidth();
			if (Math.Abs(_firstColumn.MaxWidth - firstColumnMaxWidth) > 4.0)
			{
				_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
				_firstColumn.MaxWidth = firstColumnMaxWidth;
			}
		}
		if (ColumnStyle != ListViewColumnStyle.ThreeColumn || (_taskView != null && _taskView.IsFoldColumn))
		{
			ExpandThirdColumn();
			_taskView?.SetListColumnSpan(1, 320.0, 312.0);
			ColumnStyle = ListViewColumnStyle.ThreeColumn;
			SetProjectMenuShow();
			if (MenuShow && Mode == ListMode.Normal)
			{
				_firstColumn.MaxWidth = GetFirstColumnMaxWidth();
			}
		}
	}

	private double GetFirstColumnMaxWidth()
	{
		if (!base.IsVisible)
		{
			return Math.Max(LocalSettings.Settings.ProjectPanelWidth, _firstColumn.ActualWidth);
		}
		return Math.Max(Math.Max(Math.Min(600.0, Math.Min(base.ActualWidth - 750.0, (int)base.ActualWidth / 3)), 230.0), _firstColumn.MaxWidth);
	}

	private async void FoldMenu(bool anim = true)
	{
		_menuStatus |= MainProjectStatus.Fold;
		_menuStatus &= ~MainProjectStatus.Show;
		_leftSplit.Visibility = Visibility.Collapsed;
		if (anim)
		{
			BeginProjectContentAnim(hide: true);
		}
		else
		{
			ShowOrHideProject(hide: true);
		}
		_taskView?.SetFoldMenuIcon(!MenuShow, ColumnStyle == ListViewColumnStyle.OneColumn);
	}

	private async void ShowMenu(bool anim = true)
	{
		if (_projectContent.Content == null)
		{
			ContentControl contentControl = Utils.FindParent<ContentControl>(_projectList);
			if (contentControl != null)
			{
				contentControl.Content = null;
			}
			_projectContent.Content = _projectList;
			_projectList.SetInPopup(inPopup: false);
		}
		_menuStatus &= ~MainProjectStatus.Fold;
		_menuStatus |= MainProjectStatus.Show;
		_leftSplit.Visibility = Visibility.Visible;
		if (anim)
		{
			BeginProjectContentAnim(hide: false);
		}
		else
		{
			ShowOrHideProject(hide: true);
		}
		SetProjectMenuStyle();
		_taskView?.SetFoldMenuIcon(!MenuShow, ColumnStyle == ListViewColumnStyle.OneColumn);
	}

	private void ShowOrHideProject(bool hide)
	{
		if (hide)
		{
			_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
			_projectContent.Visibility = Visibility.Collapsed;
			_projectContent.RenderTransform = new TranslateTransform
			{
				X = -230.0
			};
			_firstColumn.MaxWidth = 0.0;
		}
		else
		{
			_projectContent.Visibility = Visibility.Visible;
			_projectContent.RenderTransform = new TranslateTransform
			{
				X = 0.0
			};
			_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
			_firstColumn.MaxWidth = GetFirstColumnMaxWidth();
		}
	}

	private void BeginProjectContentAnim(bool hide)
	{
		if (hide)
		{
			_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
			_firstColumn.MaxWidth = _firstColumn.ActualWidth;
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(_firstColumn.MaxWidth, 0.0, 240);
			doubleAnimation.Completed += delegate
			{
				_projectContent.Visibility = Visibility.Collapsed;
				_projectContent.RenderTransform = new TranslateTransform
				{
					X = -230.0
				};
			};
			_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, doubleAnimation);
		}
		else
		{
			_projectContent.Visibility = Visibility.Visible;
			_projectContent.RenderTransform = new TranslateTransform
			{
				X = 0.0
			};
			DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(_firstColumn.MaxWidth, 600.0, 240);
			doubleAnimation2.Completed += delegate
			{
				_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
				_firstColumn.MaxWidth = ((ColumnStyle == ListViewColumnStyle.ThreeColumn) ? GetFirstColumnMaxWidth() : 300.0);
			};
			_firstColumn.BeginAnimation(ColumnDefinition.MaxWidthProperty, doubleAnimation2);
		}
	}

	private void SetProjectMenuStyle()
	{
		if (MenuShow && !LocalSettings.Settings.InSearch)
		{
			_projectContent.SetValue(Grid.ColumnSpanProperty, 1);
			_projectContent.Visibility = Visibility.Visible;
			TranslateTransform obj = (TranslateTransform)_projectContent.RenderTransform;
			obj.BeginAnimation(TranslateTransform.XProperty, null);
			obj.X = 0.0;
			_projectContent.MinWidth = 230.0;
			_projectContent.MaxWidth = 10000.0;
			_projectContent.HorizontalAlignment = HorizontalAlignment.Stretch;
			SetHideMenuIcon(isHide: false);
		}
	}

	private void SetHideMenuIcon(bool isHide)
	{
		_taskView?.SetFoldMenuIcon(isHide, ColumnStyle == ListViewColumnStyle.OneColumn);
	}

	private void OnSplitDragging(object sender, DragDeltaEventArgs e)
	{
		double actualWidth = _firstColumn.ActualWidth;
		_projectContent.Opacity = ((actualWidth < 180.0) ? (actualWidth / 300.0) : 1.0);
		_projectContent.RenderTransform = _projectScale;
		_projectScale.ScaleX = ((actualWidth < 180.0) ? (1.0 - (1.0 - actualWidth / 180.0) / 18.0) : 1.0);
		_projectScale.ScaleY = ((actualWidth < 180.0) ? (1.0 - (1.0 - actualWidth / 180.0) / 18.0) : 1.0);
	}

	private void OnSplitDragCompleted(object sender, DragCompletedEventArgs e)
	{
		double num = _firstColumn.ActualWidth;
		if (num < 180.0)
		{
			FoldMenu(anim: false);
			if (ShowInCal)
			{
				LocalSettings.Settings.ExtraSettings.ProjectParallelMenuFold = true;
			}
			else
			{
				LocalSettings.Settings.ExtraSettings.MenuFold = true;
			}
			_firstColumn.Width = new GridLength(230.0);
			Utils.FindParent<IListViewParent>(this)?.OnProjectWidthChanged(230.0);
		}
		else
		{
			if (num < 230.0)
			{
				num = 230.0;
				_firstColumn.Width = new GridLength(num);
			}
			Utils.FindParent<IListViewParent>(this)?.OnProjectWidthChanged(num);
		}
		_projectContent.Opacity = 1.0;
		_projectScale.ScaleX = 1.0;
		_projectScale.ScaleY = 1.0;
	}

	public void TryShowThreeColumns()
	{
		if (base.ActualWidth >= (double)ThreeFrameWidth)
		{
			SetThreeColumns();
		}
	}

	public void ShowProjectMenu(bool manual = true)
	{
		if (ColumnStyle != ListViewColumnStyle.OneColumn)
		{
			if (!MenuShow)
			{
				if (Mode == ListMode.Search && !manual)
				{
					return;
				}
				ShowMenu();
			}
			else
			{
				FoldMenu();
			}
			if (manual)
			{
				if (ShowInCal)
				{
					LocalSettings.Settings.ExtraSettings.ProjectParallelMenuFold = !MenuShow;
				}
				else
				{
					LocalSettings.Settings.ExtraSettings.MenuFold = !MenuShow;
				}
			}
			return;
		}
		_projectContent.Content = null;
		if (_projectListPopup == null)
		{
			_projectListPopup = new EscPopup
			{
				StaysOpen = false,
				PopupAnimation = PopupAnimation.Fade,
				Placement = PlacementMode.RelativePoint,
				HorizontalOffset = 14.0,
				PlacementTarget = this,
				VerticalOffset = 60.0
			};
		}
		ContentControl contentControl = new ContentControl
		{
			Width = 244.0,
			MaxHeight = 660.0
		};
		contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
		_projectList.SetInPopup(inPopup: true);
		contentControl.Content = _projectList;
		_projectListPopup.Child = contentControl;
		_projectListPopup.IsOpen = true;
		_projectList.ScrollToSelected();
	}

	public void SetProjectMenuShow()
	{
		if (ColumnStyle == ListViewColumnStyle.OneColumn)
		{
			return;
		}
		bool flag = !LocalSettings.Settings.InSearch && (ShowInCal ? (!LocalSettings.Settings.ExtraSettings.ProjectParallelMenuFold) : (!LocalSettings.Settings.ExtraSettings.MenuFold));
		if (MenuShow != flag)
		{
			if (!MenuShow)
			{
				ShowMenu(anim: false);
			}
			else
			{
				FoldMenu(anim: false);
			}
		}
	}

	public void OnWindowMouseDown(MouseButtonEventArgs e)
	{
		_taskView?.TryFoldDetailOnMouseDown(e);
	}

	public double GetItemWidth()
	{
		return _taskView?.GetItemWidth() ?? 200.0;
	}

	public void OnSyncFinished(SyncResult syncResult)
	{
		_projectList?.OnSyncFinished(syncResult);
	}

	public async Task<bool> OnDragTaskDroppedOnProject(string taskId)
	{
		return await _projectList.OnDragTaskDropped(taskId);
	}

	public void SelectFilter(string filterId)
	{
		_projectList?.SelectFilter(filterId);
	}

	public void SelectGroup(string groupId)
	{
		_projectList?.SelectGroupProject(groupId);
	}

	public async Task NavigateProject(string type, string id)
	{
		if (_projectList == null)
		{
			await Task.Delay(150);
		}
		_projectList?.NavigateProject(type, id);
	}

	public void SelectTagProject(string tag, bool scroll = false)
	{
		_projectList?.SelectTagProject(tag.ToLower(), scroll);
	}

	private void OnProjectViewModeChanged(object sender, ProjectIdentity e)
	{
		if (e.Id == _projectList?.GetSelectedProject()?.Id)
		{
			OnProjectSelected();
		}
	}

	private void SetIdentityCount(ProjectIdentity identity, int count)
	{
		base.Dispatcher.Invoke(delegate
		{
			_projectList?.SetItemCount(identity, count);
		});
	}

	public void LoadVirtualData(bool showVirtualContent)
	{
		FoldMenu(anim: false);
		_taskView.OnProjectSelect(new NormalProjectIdentity(new ProjectModel
		{
			id = Utils.GetInboxId(),
			name = Utils.GetString("Inbox"),
			viewMode = "list",
			SortOption = new SortOption
			{
				groupBy = "none",
				orderBy = "sortOrder"
			}
		})
		{
			IsVirtual = true,
			ShowVirtualContent = showVirtualContent
		});
	}

	public static ListViewContainer GetListView(string key, Border border)
	{
		if (!_listViews.ContainsKey(key))
		{
			_listViews[key] = new ListViewContainer(border);
		}
		return _listViews[key];
	}

	public static void RemoveListView(string key)
	{
		if (_listViews.TryRemove(key, out var value))
		{
			value.Clear();
		}
	}

	public static async void ReloadCount()
	{
		TaskCountCache.SetNeedLoad();
		foreach (ListViewContainer item in _listViews.Values.ToList())
		{
			item._projectList?.LoadTaskCount();
		}
		ProjectWidgetsHelper.ReloadCount();
	}

	public static void ReloadProjectData(bool refreshCount = true)
	{
		foreach (ListViewContainer item in _listViews.Values.ToList())
		{
			item._projectList?.LoadData(refreshCount);
		}
	}

	public static void ReloadTasks()
	{
		foreach (ListViewContainer item in _listViews.Values.ToList())
		{
			item.ReloadView();
		}
	}

	public static void OnProjectHide(PtfType ptfType)
	{
		foreach (ListViewContainer item in _listViews.Values.ToList())
		{
			ProjectItemViewModel projectItemViewModel = item._projectList?.SelectedItem;
			if (projectItemViewModel != null && projectItemViewModel.GetPtfType() == ptfType)
			{
				item._projectList.SetAndSelectDefaultProject();
			}
			item._projectList?.LoadData(refreshCount: false);
		}
	}

	public static void OnProjectHide(SmartListType smartType)
	{
		foreach (ListViewContainer item in _listViews.Values.ToList())
		{
			SmartListType? smartListType = item._projectList?.GetSelectedSmartListType();
			if (smartListType.HasValue && smartListType == smartType)
			{
				item._projectList.SetAndSelectDefaultProject();
			}
		}
	}

	public static async Task AcceptShareList(string projectid)
	{
		ObservableCollection<TaskModel> tasks = await Communicator.PullServerTasksByProjectId(projectid);
		if (tasks != null)
		{
			await TaskDao.BatchCreateTaskFromRemote(tasks.ToList(), needHandleItem: true);
			await TagService.CheckTaskTags(tasks.ToList(), checkShare: true);
			await Task.Delay(100);
		}
		ReloadProjectData();
	}

	public static void ResetIdentityCount(ProjectIdentity identity, int count)
	{
		foreach (ListViewContainer item in _listViews.Values.ToList())
		{
			item.SetIdentityCount(identity, count);
		}
	}
}
