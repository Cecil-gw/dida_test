using System;
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
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Event;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Drag;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Timeline;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class ProjectListView : Grid
{
	private double _dragStartOffset;

	private ResourceDictionary _popupRd = new ResourceDictionary
	{
		Source = new Uri("Resource\\Themes\\ProjectMenuPopupLightColors.xaml", UriKind.Relative)
	};

	private DragStatus _dragStatus;

	private string _duplicateProjectId;

	private ListViewContainer _listViewContainer;

	private bool _canReload = true;

	private readonly ContentControl _pinViewContent = new ContentControl
	{
		MinHeight = 4.0
	};

	private ObservableCollection<ProjectItemViewModel> _items;

	private PtfType _ptfType = PtfType.Project;

	private bool _showInCal;

	private bool _showWithCal;

	private readonly ListView _projectList = new ListView();

	private readonly Border _shadowBorder = new Border();

	private readonly Border _backBorder = new Border
	{
		IsHitTestVisible = false,
		BorderThickness = new Thickness(0.0, 0.0, 0.0, 0.0)
	};

	private readonly Border _darkMaskBorder = new Border();

	private readonly MiniCalendar _miniCalendar = new MiniCalendar();

	public ProjectItemViewModel SelectedItem;

	private static List<SyncSortOrderModel> _cache;

	private string _uid;

	private Dictionary<string, DateTime> _subscribeLastPullTimeDict;

	private bool _canBringIntoView;

	private ProjectPinView _pinView;

	private int _dragStartIndex = -1;

	private int _currentIndex = -1;

	private DragDirection _direction = DragDirection.Idle;

	private long _dragStartY = -1L;

	private ProjectItemViewModel _dragStartTarget;

	private ProjectItemViewModel _draggingModel;

	private int _hoverIndex = -1;

	private ListBoxItem _dragHoverItem;

	private ProjectItemViewModel _hoverModel;

	private EmptySubViewModel _lastHoverLine;

	private ScrollViewer _listScrollViewer;

	private Popup _projectDragPopup = new EscPopup
	{
		HorizontalOffset = 10.0,
		StaysOpen = true,
		Placement = PlacementMode.Relative
	};

	public bool TabSelected;

	private bool _dragOver;

	private int _lastDropIndex = -1;

	private ProjectItemViewModel _previousOverGroup;

	private DragAutoScrollHelper _autoScroller;

	private bool _autoScrolling;

	private double _autoScrollOffset;

	public DateTime? InitTime { get; set; }

	private bool _isProjectDragging { get; set; }

	private ObservableCollection<ProjectItemViewModel> ProjectItems => (_projectList.ItemsSource as ObservableCollection<ProjectItemViewModel>) ?? new ObservableCollection<ProjectItemViewModel>();

	public bool InPopup { get; set; }

	public bool ProjectDragging { get; set; }

	public event EventHandler ProjectSelected;

	public ProjectListView(ListViewContainer listViewContainer)
	{
		_listViewContainer = listViewContainer;
		_uid = Utils.GetGuid();
		base.Loaded += OnLoaded;
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition());
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		InitBackground();
		base.Children.Add(_pinViewContent);
		InitProjectList();
		base.MouseLeave += OnMouseLeave;
		InitShadowBorder();
		base.Unloaded += delegate
		{
			_items = ProjectItems;
			_projectList.ItemsSource = null;
			UnbindEvents();
		};
		base.Loaded += delegate
		{
			if (_items != null && _projectList.ItemsSource == null)
			{
				_projectList.ItemsSource = _items;
			}
			BindEvents();
			ScrollToSelected();
		};
		base.Children.Add(_projectDragPopup);
		InitMiniCalendar();
		SetPopupResource();
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoaded;
		if (_projectList.Template.FindName("ScrollViewer", _projectList) is ScrollViewer scrollViewer)
		{
			_listScrollViewer = scrollViewer;
			scrollViewer.ScrollChanged -= OnScrollChanged;
			scrollViewer.ScrollChanged += OnScrollChanged;
		}
		if (_projectList.Template.FindName("ItemsPresenter", _projectList) is ItemsPresenter itemsPresenter)
		{
			itemsPresenter.RequestBringIntoView += OnItemBringIntoView;
		}
	}

	private void InitBackground()
	{
		_backBorder.SetValue(Grid.RowSpanProperty, 3);
		_backBorder.SetResourceReference(Panel.BackgroundProperty, "ProjectMenuBackGround");
		base.Children.Add(_backBorder);
		_darkMaskBorder.SetValue(Grid.RowSpanProperty, 3);
		_darkMaskBorder.SetResourceReference(Panel.BackgroundProperty, "ProjectMenuMaskBackGround");
		base.Children.Add(_darkMaskBorder);
	}

	private void InitProjectList()
	{
		_projectList.SetValue(Grid.RowProperty, 1);
		_projectList.Margin = new Thickness(0.0, 22.0, 0.0, 10.0);
		_projectList.SetResourceReference(FrameworkElement.StyleProperty, "ListViewStyle");
		_projectList.SetResourceReference(ItemsControl.ItemContainerStyleProperty, "ListViewItemContainerStyle");
		_projectList.ItemTemplateSelector = new ProjectItemTemplateSelector();
		base.Children.Add(_projectList);
	}

	private void InitShadowBorder()
	{
		_shadowBorder.SetValue(Grid.RowProperty, 1);
		_shadowBorder.Height = 80.0;
		_shadowBorder.IsHitTestVisible = false;
		_shadowBorder.Margin = new Thickness(0.0, -1.0, 0.0, 0.0);
		_shadowBorder.Visibility = Visibility.Collapsed;
		_shadowBorder.VerticalAlignment = VerticalAlignment.Top;
		_shadowBorder.SetResourceReference(UIElement.OpacityProperty, "ProjectShadowOpacity");
		_shadowBorder.Background = (RadialGradientBrush)FindResource("ProjectTopShadowBrush");
		_shadowBorder.RenderTransform = new TranslateTransform
		{
			Y = -6.0
		};
		base.Children.Add(_shadowBorder);
	}

	private void InitMiniCalendar()
	{
		_miniCalendar.SetValue(Grid.RowProperty, 2);
		base.Children.Add(_miniCalendar);
		DataChangedNotifier.ThemeModeChanged -= OnThemeModeChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeModeChanged;
	}

	private void OnThemeModeChanged(object sender, EventArgs e)
	{
		SetPopupResource();
	}

	private ProjectPinView GetPinView(bool create = false)
	{
		if (_pinViewContent.Content == null && create)
		{
			if (_pinView == null)
			{
				_pinView = new ProjectPinView
				{
					Visibility = Visibility.Collapsed
				};
			}
			_pinViewContent.Content = _pinView;
		}
		if (_pinView != null)
		{
			_pinView.Margin = new Thickness(12.0, InPopup ? 4 : 22, 0.0, 0.0);
		}
		return _pinView;
	}

	public void SetParallelShow(bool showWithCal, bool inCal)
	{
		_showInCal = inCal;
		_showWithCal = showWithCal;
		_backBorder.Visibility = (showWithCal ? Visibility.Collapsed : Visibility.Visible);
		SetPopupResource();
	}

	private void SetPopupResource()
	{
		switch (LocalSettings.Settings.ExtraSettings.InterfaceStyle)
		{
		case InterfaceStyle.Flat:
			if (!ThemeUtil.IsDark() && (_showWithCal || InPopup))
			{
				base.Resources.MergedDictionaries.Remove(_popupRd);
				base.Resources.MergedDictionaries.Add(_popupRd);
			}
			else
			{
				base.Resources.MergedDictionaries.Remove(_popupRd);
			}
			break;
		case InterfaceStyle.Card:
			if (!ThemeUtil.IsDark())
			{
				if (!base.Resources.MergedDictionaries.Contains(_popupRd))
				{
					base.Resources.MergedDictionaries.Add(_popupRd);
				}
			}
			else
			{
				base.Resources.MergedDictionaries.Remove(_popupRd);
			}
			break;
		}
		_miniCalendar.DayPicker.Reload();
	}

	public ListViewContainer GetListViewContainer()
	{
		if (_listViewContainer == null)
		{
			_listViewContainer = Utils.FindParent<ListViewContainer>(this);
		}
		return _listViewContainer;
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.HabitCheckInChanged -= OnHabitCheckInChanged;
		DataChangedNotifier.TagChanged -= OnTagChanged;
		DataChangedNotifier.TagTypeChanged -= OnTagTypeChanged;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
		DataChangedNotifier.ProjectPinRemoteChanged -= PinChanged;
		DataChangedNotifier.InterfaceStyleChanged -= OnInterfaceStyleThemeChanged;
		TickDragHelper.DragDrop -= OnDragDrop;
		TickDragHelper.DragMoving -= OnDragOver;
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnNumDisplayChanged, "ProjectNum");
	}

	private void BindEvents()
	{
		DataChangedNotifier.HabitCheckInChanged += OnHabitCheckInChanged;
		DataChangedNotifier.TagChanged += OnTagChanged;
		DataChangedNotifier.TagTypeChanged += OnTagTypeChanged;
		DataChangedNotifier.FilterChanged += OnFilterChanged;
		DataChangedNotifier.ProjectPinRemoteChanged += PinChanged;
		DataChangedNotifier.InterfaceStyleChanged += OnInterfaceStyleThemeChanged;
		TickDragHelper.DragDrop += OnDragDrop;
		TickDragHelper.DragMoving += OnDragOver;
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnNumDisplayChanged, "ProjectNum");
	}

	private void OnInterfaceStyleThemeChanged(object sender, EventArgs e)
	{
		SetPopupResource();
	}

	private void OnNumDisplayChanged(object sender, PropertyChangedEventArgs e)
	{
		LoadData(refreshCount: false);
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		ProjectMenuItemHoverModel.Model.HoverType = PtfType.Null;
		ProjectMenuItemHoverModel.Model.TeamId = string.Empty;
	}

	private void OnFilterChanged(object sender, FilterChangeArgs e)
	{
		LoadData(refreshCount: false);
		LoadPinModels();
	}

	private void OnTagTypeChanged(object sender, EventArgs e)
	{
		if (LocalSettings.Settings.AllTagOpened)
		{
			LoadData(refreshCount: false);
		}
	}

	private void OnTagChanged(object sender, TagModel e)
	{
		UpdateTagItems(ProjectItems, e);
		LoadPinModels();
	}

	internal static void UpdateTagItems(IEnumerable<ProjectItemViewModel> items, TagModel tag)
	{
		if (tag == null)
		{
			return;
		}
		Stack<ProjectItemViewModel> stack = new Stack<ProjectItemViewModel>(items);
		HashSet<ProjectItemViewModel> hashSet = new HashSet<ProjectItemViewModel>();
		while (stack.Count > 0)
		{
			ProjectItemViewModel projectItemViewModel = stack.Pop();
			if (projectItemViewModel == null || !hashSet.Add(projectItemViewModel))
			{
				continue;
			}
			if (projectItemViewModel is TagProjectViewModel tagProjectViewModel && tagProjectViewModel.TagModel.id == tag.id)
			{
				tagProjectViewModel.UpdateTagModel(tag);
			}
			if (projectItemViewModel.Children == null)
			{
				continue;
			}
			foreach (ProjectItemViewModel child in projectItemViewModel.Children)
			{
				stack.Push(child);
			}
		}
	}

	private void OnHabitCheckInChanged(object sender, HabitCheckInModel e)
	{
		if (e.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", App.Ci))
		{
			ReloadTodayOrWeekCount();
		}
	}

	private void PinChanged(object sender, EventArgs e)
	{
		base.Dispatcher.Invoke((Func<Task>)LoadPinModels);
	}

	private void ReloadTodayOrWeekCount()
	{
		LoadTodayOrWeekCount();
	}

	public SmartListType? GetSelectedSmartListType()
	{
		if (SelectedItem is ticktick_WPF.ViewModels.SmartProjectViewModel smartProjectViewModel)
		{
			string text = smartProjectViewModel.Project.GetType().Name.Replace("Project", "");
			if (text == "Inbox")
			{
				return null;
			}
			return (SmartListType)Enum.Parse(typeof(SmartListType), text);
		}
		return null;
	}

	public async void SelectProject(ProjectIdentity projectId, bool invokeEvent = true, bool autoExpand = true)
	{
		SelectedItem = ProjectItemViewModel.BuildProject(projectId);
		SetSelectedItemHighlighted();
		if (invokeEvent)
		{
			_listViewContainer?.OnProjectSelected();
		}
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == projectId.QueryId);
		if (projectItemViewModel != null)
		{
			_canBringIntoView = true;
			_projectList.ScrollIntoView(projectItemViewModel);
		}
		else
		{
			if (!autoExpand)
			{
				return;
			}
			ProjectIdentity projectIdentity = projectId;
			NormalProjectIdentity normalProjectIdentity = projectIdentity as NormalProjectIdentity;
			if (normalProjectIdentity == null)
			{
				GroupProjectIdentity groupProjectIdentity = projectIdentity as GroupProjectIdentity;
				if (groupProjectIdentity == null)
				{
					if (!(projectIdentity is TagProjectIdentity))
					{
						if (!(projectIdentity is FilterProjectIdentity))
						{
							if (projectIdentity is SubscribeCalendarProjectIdentity || projectIdentity is BindAccountCalendarProjectIdentity)
							{
								ProjectItemViewModel projectItemViewModel2 = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.IsPtfAll && p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.IsSubscribe);
								if (projectItemViewModel2 != null && !projectItemViewModel2.Open)
								{
									await OnPtfAllClick(projectItemViewModel2);
								}
							}
						}
						else
						{
							ProjectItemViewModel projectItemViewModel3 = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.IsPtfAll && p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.IsFilter);
							if (projectItemViewModel3 != null && !projectItemViewModel3.Open)
							{
								await OnPtfAllClick(projectItemViewModel3);
							}
						}
					}
					else
					{
						ProjectItemViewModel projectItemViewModel4 = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.IsPtfAll && p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.IsTag);
						if (projectItemViewModel4 != null && !projectItemViewModel4.Open)
						{
							await OnPtfAllClick(projectItemViewModel4);
						}
					}
				}
				else
				{
					ProjectItemViewModel projectItemViewModel5 = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.IsPtfAll && p is PtfAllViewModel { IsProject: not false } ptfAllViewModel && groupProjectIdentity.Group.teamId == ptfAllViewModel.TeamId);
					if (projectItemViewModel5 != null && !projectItemViewModel5.Open)
					{
						await OnPtfAllClick(projectItemViewModel5);
					}
				}
			}
			else if (normalProjectIdentity.Project != null && !normalProjectIdentity.Project.Isinbox)
			{
				ProjectItemViewModel projectItemViewModel6 = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.IsPtfAll && p is PtfAllViewModel { IsProject: not false } ptfAllViewModel && normalProjectIdentity.Project.teamId == ptfAllViewModel.TeamId);
				if (projectItemViewModel6 != null && !projectItemViewModel6.Open)
				{
					await OnPtfAllClick(projectItemViewModel6);
				}
			}
			ProjectItemViewModel projectItemViewModel7 = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Children != null && p.Children.Any((ProjectItemViewModel c) => c.Id == projectId.QueryId));
			if (projectItemViewModel7 != null && !projectItemViewModel7.Open)
			{
				CollapseItem(projectItemViewModel7);
			}
			SetSelectedItemHighlighted();
			projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == projectId.QueryId);
			if (projectItemViewModel != null)
			{
				_canBringIntoView = true;
				_projectList.ScrollIntoView(projectItemViewModel);
			}
		}
	}

	public async void LoadTaskCount()
	{
		LoadData(refreshCount: false);
	}

	private async void LoadTodayOrWeekCount()
	{
		bool loaded = false;
		Task.Run(async delegate
		{
			await TaskCountCache.LoadTodayOrWeekCount();
			loaded = true;
		});
		int retry = 0;
		while (!loaded && retry < 10)
		{
			await Task.Delay(50);
			retry++;
		}
	}

	public async Task LoadData(bool refreshCount = true)
	{
		if (InitTime.HasValue)
		{
			while ((DateTime.Now - InitTime.Value).TotalSeconds < 8.0)
			{
				await Task.Delay(200);
			}
		}
		if (_projectList.ItemsSource == null)
		{
			InitTime = DateTime.Now;
			await Task.Delay(300);
		}
		DelayActionHandlerCenter.TryDoAction(_uid + "ProjectMenuLoadData", delegate
		{
			Load(refreshCount);
		});
	}

	private void Load(bool refreshCount)
	{
		ThreadUtil.DetachedRunOnUiThread(Action);
		async void Action()
		{
			if (!_isProjectDragging && _canReload && !_projectDragPopup.IsOpen)
			{
				List<ProjectItemViewModel> items = await ProjectDataProvider.GetProjectData();
				ItemsSourceHelper.SetItemsSource(_projectList, items);
				InitSelected();
				if (refreshCount)
				{
					ForceLoadCount();
				}
				LoadPinModels();
				if (LocalSettings.Settings.ExtraSettings.MiniCalendarEnabled)
				{
					_miniCalendar.LoadIndicator();
				}
			}
		}
	}

	public async Task LoadPinModels()
	{
		if (_pinViewContent.Content == null)
		{
			List<SyncSortOrderModel> cache = _cache;
			if (cache != null && cache.Count > 0)
			{
				List<ProjectPinItemViewModel> data = ProjectPinItemViewModel.BuildModels(_cache);
				GetPinView(create: true).SetData(data);
			}
		}
		List<SyncSortOrderModel> list = (_cache = await ProjectPinSortOrderService.GetAsync());
		if (list.Count > 0)
		{
			list.Sort((SyncSortOrderModel a, SyncSortOrderModel b) => a.SortOrder.CompareTo(b.SortOrder));
			List<ProjectPinItemViewModel> list2 = ProjectPinItemViewModel.BuildModels(list);
			if (list2.Count > 0)
			{
				ProjectPinView pinView = GetPinView(create: true);
				pinView.SetData(list2);
				if (pinView.IsEmpty())
				{
					pinView.Visibility = Visibility.Collapsed;
				}
				else
				{
					pinView.Visibility = Visibility.Visible;
				}
			}
			else if (_pinView != null)
			{
				_pinView.Visibility = Visibility.Collapsed;
			}
		}
		else if (_pinView != null)
		{
			_pinView.Visibility = Visibility.Collapsed;
		}
		_projectList.Margin = new Thickness(0.0, (_pinView == null || _pinView.Visibility != Visibility.Visible) ? 22 : 0, 0.0, 10.0);
	}

	private void LoadProjectList(bool refreshCount)
	{
		DelayActionHandlerCenter.TryDoAction("ProjectMenuLoadData", delegate
		{
			ThreadUtil.DetachedRunOnUiThread(async delegate
			{
				if (_canReload && !ProjectDragging)
				{
					base.Dispatcher.Invoke((Func<Task>)async delegate
					{
						Reload(refreshCount);
					});
				}
			});
		});
	}

	private async Task Reload(bool refreshCount)
	{
		LoadPinModels();
		List<ProjectItemViewModel> items = await ProjectDataProvider.GetProjectData();
		ItemsSourceHelper.SetItemsSource(_projectList, items);
		InitSelected();
		if (refreshCount)
		{
			ForceLoadCount();
		}
	}

	public async void ForceLoadCount()
	{
		if (TryRemoveAutoList() || TryRemoveAutoTags())
		{
			LoadData(refreshCount: false);
		}
	}

	private bool TryRemoveAutoTags()
	{
		if (LocalSettings.Settings.SmartListTag == 2)
		{
			foreach (TagModel tag in CacheManager.GetTags())
			{
				if (AutoCountChanged(ProjectItems, tag.name))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool TryRemoveAutoList()
	{
		if (LocalSettings.Settings.SmartListToday == 2 && AutoCountChanged(ProjectItems, "_special_id_today"))
		{
			return true;
		}
		if (LocalSettings.Settings.SmartListTomorrow == 2 && AutoCountChanged(ProjectItems, "_special_id_tomorrow"))
		{
			return true;
		}
		if (LocalSettings.Settings.SmartList7Day == 2 && AutoCountChanged(ProjectItems, "_special_id_week"))
		{
			return true;
		}
		if (LocalSettings.Settings.SmartListForMe == 2 && AutoCountChanged(ProjectItems, "_special_id_assigned"))
		{
			return true;
		}
		return false;
	}

	private static bool AutoCountChanged(IEnumerable<ProjectItemViewModel> projects, string key)
	{
		if (TaskCountCache.CountData.ContainsKey(key))
		{
			int num = TaskCountCache.CountData[key];
			ProjectItemViewModel projectItemViewModel = projects.FirstOrDefault((ProjectItemViewModel project) => project.Id == key);
			if (projectItemViewModel == null && num > 0)
			{
				return true;
			}
			if (num == 0 && projectItemViewModel != null)
			{
				return true;
			}
		}
		return false;
	}

	public async Task OnSyncFinished(SyncResult syncResult)
	{
		if (SelectedItem == null)
		{
			return;
		}
		ListViewContainer listViewContainer = GetListViewContainer();
		if (listViewContainer != null && listViewContainer.Mode == ListMode.Search)
		{
			return;
		}
		if (syncResult.RemoteProjectsChanged || syncResult.RemoteFiltersChanged || syncResult.TeamChanged || syncResult.RemoteGroupChanged || syncResult.RemoteTagChanged)
		{
			LoadData();
		}
		if (syncResult.RemoteProjectsChanged || syncResult.RemoteFiltersChanged || syncResult.RemoteTagChanged)
		{
			if (IsSelectedProjectChanged(syncResult.UpdatedProjects))
			{
				ProjectItemViewModel selectedItem = SelectedItem;
				if (selectedItem is NormalProjectViewModel projectVm)
				{
					ProjectModel projectModel = await ProjectDao.GetProjectById(projectVm.Id);
					if (projectModel != null && !projectModel.IsEquals(projectVm.Project))
					{
						SelectedItem = new NormalProjectViewModel(projectModel);
					}
				}
			}
			else if (IsSelectedFilterChanged(syncResult.UpdatedFilters))
			{
				if (SelectedItem is FilterProjectViewModel filterProjectViewModel)
				{
					FilterModel filterById = CacheManager.GetFilterById(filterProjectViewModel.Id);
					if (filterById != null)
					{
						SelectedItem = new FilterProjectViewModel(filterById);
					}
				}
			}
			else if (IsSelectedTagChanged(syncResult.UpdatedTags))
			{
				if (SelectedItem is TagProjectViewModel tagProjectViewModel)
				{
					TagModel tagByName = CacheManager.GetTagByName(tagProjectViewModel.Id.ToLower());
					if (tagByName != null)
					{
						SelectedItem = new TagProjectViewModel(tagByName);
						_listViewContainer?.OnProjectSelected();
					}
				}
			}
			else if (IsSelectedTagDeleted(syncResult.DeletedTags))
			{
				SetAndSelectDefaultProject();
			}
		}
		if (syncResult.RemoteDataChanged)
		{
			GetListViewContainer()?.ReloadView();
		}
	}

	private bool IsSelectedTagDeleted(List<TagModel> deletedTags)
	{
		if (!string.IsNullOrEmpty(SelectedItem?.Id))
		{
			return deletedTags?.Any((TagModel tag) => tag.name == SelectedItem.Id.ToLower()) ?? false;
		}
		return false;
	}

	private bool IsSelectedTagChanged(List<TagModel> tags)
	{
		if (!string.IsNullOrEmpty(SelectedItem?.Id))
		{
			return tags?.Any((TagModel tag) => tag.name == SelectedItem.Id.ToLower()) ?? false;
		}
		return false;
	}

	private bool IsSelectedProjectChanged(IReadOnlyCollection<ProjectModel> updatedProjects)
	{
		if (!string.IsNullOrEmpty(SelectedItem?.Id))
		{
			if (updatedProjects != null && updatedProjects.Count > 0)
			{
				return updatedProjects.FirstOrDefault((ProjectModel p) => p.id == SelectedItem.Id) != null;
			}
			return false;
		}
		return false;
	}

	private bool IsSelectedFilterChanged(IReadOnlyCollection<FilterModel> updatedFilters)
	{
		if (SelectedItem != null)
		{
			ProjectItemViewModel selectedItem = SelectedItem;
			FilterProjectViewModel filterProject = selectedItem as FilterProjectViewModel;
			if (filterProject != null)
			{
				if (updatedFilters != null && updatedFilters.Count > 0)
				{
					return updatedFilters.FirstOrDefault((FilterModel f) => f.id == filterProject.Id) != null;
				}
				return false;
			}
		}
		return false;
	}

	private void InitSelected()
	{
		if (SelectedItem == null)
		{
			ListViewContainer listViewContainer = GetListViewContainer();
			if (listViewContainer == null || listViewContainer.Mode != ListMode.Search)
			{
				LoadSavedProject();
			}
		}
		else
		{
			SetSelectedItemHighlighted();
		}
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (sender is ScrollViewer scrollViewer && _pinView != null && _pinView.IsVisible)
		{
			if (scrollViewer.VerticalOffset > 0.0 && scrollViewer.ScrollableHeight > 0.0)
			{
				_shadowBorder.Visibility = Visibility.Visible;
			}
			else
			{
				_shadowBorder.Visibility = Visibility.Collapsed;
			}
		}
	}

	public async void LoadSavedProject()
	{
		string text = Utils.FindParent<IListViewParent>(GetListViewContainer())?.GetSelectedProject() ?? (_showInCal ? LocalSettings.Settings.ExtraSettings.ProjectParallelSelectProject : LocalSettings.Settings.SelectProjectId);
		ProjectItemViewModel savedModel = ProjectItemViewModel.BuildProject(text);
		UtilLog.Info("LoadSavedProject : " + text);
		if (savedModel != null)
		{
			SelectedItem = savedModel;
			ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == savedModel.Id);
			if (projectItemViewModel != null)
			{
				SelectedItem = projectItemViewModel;
				_canBringIntoView = true;
				_projectList.ScrollIntoView(projectItemViewModel);
			}
			SetSelectedItemHighlighted();
			_listViewContainer?.OnProjectSelected();
		}
		else
		{
			SetAndSelectDefaultProject();
		}
	}

	public void SelectGroupProject(string gId)
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == gId);
		if (projectItemViewModel != null)
		{
			_canBringIntoView = true;
			_projectList.ScrollIntoView(projectItemViewModel);
		}
		else
		{
			ProjectGroupModel groupById = CacheManager.GetGroupById(gId);
			projectItemViewModel = ((groupById != null) ? new ticktick_WPF.ViewModels.ProjectGroupViewModel(groupById) : null);
		}
		if (projectItemViewModel != null)
		{
			SelectedItem = projectItemViewModel;
			_listViewContainer?.OnProjectSelected();
			SetSelectedItemHighlighted();
		}
	}

	public void SelectTagProject(string tag, bool scroll = true)
	{
		TagModel tag2 = CacheManager.GetTagByName(tag) ?? new TagModel
		{
			name = tag
		};
		SelectProject(new TagProjectIdentity(tag2), invokeEvent: true, scroll);
	}

	public void SelectFilter(string id, bool ignoreEvent = false)
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == id);
		if (projectItemViewModel == null)
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == id);
			projectItemViewModel = ((filterModel != null) ? new FilterProjectViewModel(filterModel) : null);
		}
		if (projectItemViewModel != null)
		{
			SelectedItem = projectItemViewModel;
			LogRecentProject("filter", id);
		}
		if (!ignoreEvent)
		{
			_listViewContainer?.OnProjectSelected();
		}
		SetSelectedItemHighlighted();
	}

	public void SelectBindCalender(string calId)
	{
		BindCalendarModel bindCalendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calId);
		if (bindCalendar == null)
		{
			return;
		}
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == calId);
		if (projectItemViewModel != null)
		{
			_canBringIntoView = true;
			_projectList.ScrollIntoView(projectItemViewModel);
		}
		else
		{
			BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel acc) => acc.Id == bindCalendar.AccountId);
			if (bindCalendarAccountModel != null)
			{
				projectItemViewModel = new BindCalendarAccountProjectViewModel(bindCalendarAccountModel);
			}
		}
		if (projectItemViewModel != null)
		{
			SelectedItem = projectItemViewModel;
			_listViewContainer?.OnProjectSelected();
		}
		SetSelectedItemHighlighted();
	}

	public void SelectSubscribeCalender(string calId)
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == calId);
		if (projectItemViewModel != null)
		{
			_canBringIntoView = true;
			_projectList.ScrollIntoView(projectItemViewModel);
		}
		else
		{
			CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel cal) => cal.Id == calId);
			if (calendarSubscribeProfileModel != null)
			{
				projectItemViewModel = new SubscribeCalendarProjectViewModel(calendarSubscribeProfileModel);
			}
		}
		if (projectItemViewModel != null)
		{
			SelectedItem = projectItemViewModel;
			_listViewContainer?.OnProjectSelected();
		}
		SetSelectedItemHighlighted();
	}

	public void SelectNormalProject(string projectId, string taskId = null)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null)
		{
			ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == projectId);
			if (projectItemViewModel != null)
			{
				_canBringIntoView = true;
				_projectList.ScrollIntoView(projectItemViewModel);
			}
			else
			{
				projectItemViewModel = new NormalProjectViewModel(projectModel);
			}
			SelectedItem = projectItemViewModel;
			_listViewContainer?.OnProjectSelected();
			SetSelectedItemHighlighted();
		}
	}

	public async Task OnProjectClick(ProjectItemViewModel model, bool manual)
	{
		if (model is ClosedProjectGroupViewModel || model.Id == SelectedItem?.Id)
		{
			if (!(model is SubscribeCalendarProjectViewModel model2))
			{
				if (model is BindCalendarAccountProjectViewModel model3)
				{
					OnBindCalendarProjectClick(model3);
				}
			}
			else
			{
				OnSubscribeCalendarClick(model2);
			}
			return;
		}
		if (_projectList.ItemsSource is ObservableCollection<ProjectItemViewModel> observableCollection)
		{
			foreach (ProjectItemViewModel item in observableCollection)
			{
				if (item.Id == model.Id)
				{
					item.Selected = true;
				}
				else if (item.Selected)
				{
					item.Selected = false;
				}
			}
		}
		SelectedItem = model;
		_pinView?.SetSelectedProject(model);
		Utils.FindParent<IListViewParent>(GetListViewContainer())?.SaveSelectedProject(model.GetSaveIdentity());
		_listViewContainer?.OnProjectSelected();
		string listType = "list";
		if (!(model is ticktick_WPF.ViewModels.SmartProjectViewModel smartProjectViewModel))
		{
			if (!(model is NormalProjectViewModel))
			{
				if (!(model is ticktick_WPF.ViewModels.ProjectGroupViewModel))
				{
					if (!(model is TagProjectViewModel))
					{
						if (!(model is FilterProjectViewModel))
						{
							if (!(model is SubscribeCalendarProjectViewModel model4))
							{
								if (model is BindCalendarAccountProjectViewModel model5)
								{
									OnBindCalendarProjectClick(model5, forcePull: true);
									listType = "events";
								}
							}
							else
							{
								OnSubscribeCalendarClick(model4);
								listType = "events";
							}
						}
						else
						{
							listType = "filter";
							LogRecentProject("filter", model.Id);
						}
					}
					else
					{
						listType = "tag";
						LogRecentProject("tag", model.Id);
					}
				}
				else
				{
					ProjectGroupDao.CheckGroupGroupBy(model.Id);
					listType = "folder";
				}
			}
			else
			{
				if (CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == model.Id)?.IsShareList() ?? false)
				{
					AvatarHelper.ResetProjectAvatars(model.Id);
				}
				LogRecentProject("project", model.Id);
			}
		}
		else
		{
			if (manual)
			{
				UserActCollectUtils.AddClickEvent("project_list_ui", "select", (smartProjectViewModel is ticktick_WPF.ViewModels.InboxProjectViewModel) ? "inbox" : smartProjectViewModel.Project.UserEventId);
			}
			if ((smartProjectViewModel.Project is TodayProject || smartProjectViewModel.Project is WeekProject) && LocalSettings.Settings.HabitInToday)
			{
				Task.Run(delegate
				{
					HabitSyncService.PullHabits(8);
				});
			}
		}
		if (manual)
		{
			UtilLog.Info("Select " + model.GetType().Name + " " + model.Id);
			CollectSelectEvent(model.ViewMode, listType, model.GetIdentity()?.SortOption);
			TaskCountCache.ReloadProjectTaskCount(model.GetIdentity());
		}
	}

	private void CollectSelectEvent(string viewMode, string listType, SortOption sortOption)
	{
		UserActCollectUtils.AddClickEvent("project_list_ui", "select", listType);
		if (sortOption != null)
		{
			UserActCollectUtils.AddSortOptionEvent("list_group_order", "sort_view", sortOption.groupBy, sortOption.orderBy);
		}
		if (!string.IsNullOrEmpty(viewMode))
		{
			UserActCollectUtils.AddClickEvent("list_mode", "show", viewMode);
			if (viewMode == "timeline")
			{
				UserActCollectUtils.AddClickEvent("timeline", "view_list_type", listType);
			}
		}
	}

	private static void LogRecentProject(string type, string id)
	{
		string item = type + ":" + id;
		List<string> list = LocalSettings.Settings.RecentProjects.Split(',').ToList();
		if (!list.Contains(item))
		{
			list.Insert(0, item);
		}
		else
		{
			list.Remove(item);
			list.Insert(0, item);
		}
		LocalSettings.Settings.RecentProjects = string.Join(",", list);
		JumpHelper.InitJumpList();
	}

	public void CollapseItem(ProjectItemViewModel model)
	{
		if (model is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel)
		{
			projectGroupViewModel.Open = !projectGroupViewModel.Open;
			if (projectGroupViewModel is ClosedProjectGroupViewModel)
			{
				LocalSettings.Settings.ClosedSectionStatus = projectGroupViewModel.Open.ToString();
			}
			else
			{
				projectGroupViewModel.Icon = Utils.GetGeometry(projectGroupViewModel.Open ? "Ic.ProjectList.ExpandFolder" : "Ic.ProjectList.Folder");
				if (projectGroupViewModel.ProjectGroup != null)
				{
					projectGroupViewModel.ProjectGroup.open = projectGroupViewModel.Open;
					ProjectGroupDao.TrySaveProjectGroup(projectGroupViewModel.ProjectGroup, isNeedUpdateOpen: true);
				}
			}
			OnOpenGroupClick(projectGroupViewModel);
		}
		if (model is TagProjectViewModel tagProjectViewModel)
		{
			tagProjectViewModel.Open = !tagProjectViewModel.Open;
			TagModel tagByName = CacheManager.GetTagByName(tagProjectViewModel.Id.ToLower());
			if (tagByName != null)
			{
				tagByName.collapsed = !tagProjectViewModel.Open;
				tagProjectViewModel.TagModel = tagByName;
				TagDao.UpdateTag(tagByName);
			}
			OnOpenGroupClick(tagProjectViewModel);
		}
	}

	private void OnOpenGroupClick(ProjectItemViewModel vm)
	{
		if (!(_projectList.ItemsSource is ObservableCollection<ProjectItemViewModel> observableCollection))
		{
			return;
		}
		ProjectItemViewModel projectItemViewModel = observableCollection.FirstOrDefault((ProjectItemViewModel p) => p.Id == vm.Id);
		if (projectItemViewModel == null || (!(projectItemViewModel is ticktick_WPF.ViewModels.ProjectGroupViewModel) && !(projectItemViewModel is TagProjectViewModel { IsParent: not false })))
		{
			return;
		}
		if (projectItemViewModel.Open)
		{
			int num = observableCollection.IndexOf(projectItemViewModel);
			if (num <= 0 || projectItemViewModel.Children.Count <= 0)
			{
				return;
			}
			{
				foreach (ProjectItemViewModel child in projectItemViewModel.Children)
				{
					child.Selected = child.Id == SelectedItem?.Id;
					observableCollection.Insert(++num, child);
				}
				return;
			}
		}
		if (projectItemViewModel.Children.Count <= 0)
		{
			return;
		}
		foreach (ProjectItemViewModel child2 in projectItemViewModel.Children)
		{
			observableCollection.Remove(child2);
		}
	}

	public async void OnAddProjectClick(string groupId, string teamId)
	{
		if (!(await CheckProjectOverLimit(CheckListLimiteType.Add)))
		{
			EditProjectDialog obj = new EditProjectDialog(groupId, teamId)
			{
				Owner = Window.GetWindow(this)
			};
			UserActCollectUtils.AddClickEvent("project_list_ui", "list", "add");
			obj.OnProjectSaved += OnProjectSaved;
			obj.ShowDialog();
			obj.OnProjectSaved -= OnProjectSaved;
		}
	}

	private async Task<bool> CheckProjectOverLimit(CheckListLimiteType type, bool isOpen = false)
	{
		long projectLimitNumber = Utils.GetUserLimit(Constants.LimitKind.ProjectNumber);
		List<string> teamIds = (from t in CacheManager.GetTeams()
			select t.id).ToList();
		List<ProjectModel> source = (UserDao.IsPro() ? (await ProjectDao.GetAllProjects(withInbox: false, withClosed: false)).Where((ProjectModel p) => !TeamDao.IsTeamExpired(p.teamId)).ToList() : (await ProjectDao.GetAllProjects(withInbox: false)).ToList());
		int num = source.Where((ProjectModel p) => p.teamId == null || teamIds.Contains(p.teamId)).ToList().Count;
		if (!UserDao.IsPro() && isOpen)
		{
			num--;
		}
		if (num < projectLimitNumber)
		{
			UtilLog.Info($"AddProjectLimit : {projectLimitNumber}, current {num}, isOpen {isOpen}");
			return false;
		}
		if (UserDao.IsPro())
		{
			string content = "";
			switch (type)
			{
			case CheckListLimiteType.Add:
				content = string.Format(Utils.GetString("ListsLimitAdd"), num);
				break;
			case CheckListLimiteType.Copy:
				content = string.Format(Utils.GetString("ListsLimitCopy"), num);
				break;
			case CheckListLimiteType.Open:
				content = string.Format(Utils.GetString("ListsLimitAdd"), num);
				break;
			}
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("ProjectLimitTips"), content, MessageBoxButton.OK);
			customerDialog.Owner = Window.GetWindow(this);
			customerDialog.ShowDialog();
			return true;
		}
		ProChecker.ShowUpgradeDialog(ProType.MoreLists);
		return true;
	}

	public void OnAddTagClick()
	{
		AddOrEditTagWindow obj = new AddOrEditTagWindow
		{
			Owner = Window.GetWindow(this)
		};
		UserActCollectUtils.AddClickEvent("project_list_ui", "tag", "add");
		obj.TagSaved += async delegate(object arg, TagModel tag)
		{
			if (LocalSettings.Settings.SmartListTag == 2)
			{
				ShowTagCreateHint();
			}
			else if (tag != null)
			{
				UtilLog.Info("ProjectMenu.AddTag : " + tag.name);
				SelectTagProject(tag.name);
			}
			ListViewContainer.ReloadProjectData();
		};
		obj.ShowDialog();
	}

	private async void ShowTagCreateHint()
	{
		if (!LocalSettings.Settings.AutoTagUserIds.Contains(Utils.GetCurrentUserIdInt().ToString()))
		{
			await Task.Delay(100);
			LocalSettings.Settings.AutoTagUserIds = LocalSettings.Settings.AutoTagUserIds + ";" + Utils.GetCurrentUserIdInt();
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("ShowTags"), Utils.GetString("AutoShowTagsMessage"), Utils.GetString("GotIt"), string.Empty);
			customerDialog.Owner = Window.GetWindow(this);
			customerDialog.ShowDialog();
		}
		else
		{
			Utils.Toast(Utils.GetString("AutoTagAddHint"));
		}
	}

	private void NotifyGroupOpenChanged(ProjectItemViewModel projectModel)
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p.Id == projectModel.Id);
		if (projectItemViewModel == null || (!(projectItemViewModel is ticktick_WPF.ViewModels.ProjectGroupViewModel) && !(projectItemViewModel is TagProjectViewModel { IsParent: not false })))
		{
			return;
		}
		if (projectItemViewModel.Open)
		{
			int num = ProjectItems.IndexOf(projectItemViewModel);
			if (num <= 0 || projectItemViewModel.Children.Count <= 0)
			{
				return;
			}
			{
				foreach (ProjectItemViewModel child in projectItemViewModel.Children)
				{
					ProjectItems.Insert(++num, child);
				}
				return;
			}
		}
		if (projectItemViewModel.Children.Count <= 0)
		{
			return;
		}
		foreach (ProjectItemViewModel child2 in projectItemViewModel.Children)
		{
			ProjectItems.Remove(child2);
		}
	}

	public async void DeleteFilter(FilterModel filter)
	{
		if (filter != null)
		{
			bool? flag = new CustomerDialog(string.Format(Utils.GetString("DeleteFilterTitle"), filter.name), Utils.GetString("DeleteFilterShortHint"), MessageBoxButton.OKCancel)
			{
				Owner = Window.GetWindow(this)
			}.ShowDialog();
			if (flag.HasValue && flag.Value)
			{
				filter.syncStatus = 1;
				filter.deleted = 1;
				UtilLog.Info("ProjectMenu.DeleteFilter : " + filter.id);
				await FilterDao.UpdateFilter(filter);
				SyncSortOrderDao.OnFilterDeleted(filter.id);
				OnFilterDeleted(this, filter.id);
			}
		}
	}

	public async void UnsubscribeCalendar(CalendarSubscribeProfileModel calendar)
	{
		if (calendar != null)
		{
			bool? flag = new CustomerDialog(Utils.GetString("Unsubscribe"), Utils.GetString("CancelSubscribeMessage"), MessageBoxButton.OKCancel)
			{
				Owner = Window.GetWindow(this)
			}.ShowDialog();
			if (flag.HasValue && flag.Value)
			{
				UtilLog.Info("ProjectMenu.UnsubCal : id" + calendar.Id + ", name " + calendar.Name);
				await SubscribeCalendarHelper.UnsubscribeCalendar(calendar.Id);
				ListViewContainer.ReloadProjectData();
				SetAndSelectDefaultProject();
				App.Window.TryReloadCalendar();
			}
		}
	}

	public async void EditBindCalendar(string accountId)
	{
		if (!string.IsNullOrEmpty(accountId))
		{
			EditBindAccountWindow obj = await EditBindAccountWindow.Build(accountId);
			obj.Owner = Window.GetWindow(this);
			obj.ShowDialog();
			TrySelectDefaultProject(accountId);
		}
	}

	private void TrySelectDefaultProject(string accountId)
	{
		if (!CacheManager.GetBindCalendars().Any((BindCalendarModel cal) => cal != null && cal.AccountId == accountId && cal.Show != "hidden"))
		{
			SetAndSelectDefaultProject();
		}
	}

	public async void UnbindCalendar(string accountId)
	{
		if (!string.IsNullOrEmpty(accountId))
		{
			bool? flag = new CustomerDialog(Utils.GetString("Unsubscribe"), Utils.GetString("CancelSubscribeMessage"), MessageBoxButton.OKCancel, Window.GetWindow(this)).ShowDialog();
			if (flag.HasValue && flag.Value)
			{
				await SubscribeCalendarHelper.UnbindCalendar(accountId);
				ListViewContainer.ReloadProjectData();
				SetAndSelectDefaultProject();
			}
		}
	}

	public async void EditSubscribeCalendar(CalendarSubscribeProfileModel calendar)
	{
		if (calendar != null)
		{
			CalendarSubscribeProfileModel calendarSubscribeProfileModel = await CalendarSubscribeProfileDao.GetProfileById(calendar.Id);
			if (calendarSubscribeProfileModel.Kind != 2)
			{
				EditUrlWindow editUrlWindow = new EditUrlWindow(new ticktick_WPF.Views.Config.SubscribeCalendarViewModel(calendarSubscribeProfileModel));
				editUrlWindow.Owner = Window.GetWindow(this);
				editUrlWindow.ShowDialog();
			}
			else
			{
				EditHolidaySubsWindow editHolidaySubsWindow = new EditHolidaySubsWindow(new ticktick_WPF.Views.Config.SubscribeCalendarViewModel(calendarSubscribeProfileModel));
				editHolidaySubsWindow.Owner = Window.GetWindow(this);
				editHolidaySubsWindow.ShowDialog();
			}
			ListViewContainer.ReloadProjectData();
		}
	}

	public void EditFilter(FilterModel filter)
	{
		if (!ProChecker.CheckPro(ProType.Filter) || filter == null)
		{
			return;
		}
		string inboxId = Utils.GetInboxId();
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == inboxId);
		if (projectModel != null)
		{
			projectModel.sortOrder = long.MinValue;
		}
		FilterEditDialog filterEditDialog = new FilterEditDialog(filter);
		filterEditDialog.Owner = App.Window;
		filterEditDialog.Owner = Window.GetWindow(this);
		filterEditDialog.FilterSaved += delegate(object o, FilterModel f)
		{
			if (f != null)
			{
				ListViewContainer.ReloadProjectData();
				ProjectWidgetsHelper.OnProjectChanged(new FilterProjectIdentity(f));
				SyncManager.Sync();
			}
		};
		filterEditDialog.ShowDialog();
	}

	private async void OnFilterDeleted(object sender, string filterId)
	{
		ListViewContainer.ReloadProjectData();
		if (SelectedItem?.Id == filterId)
		{
			ProjectWidgetsHelper.OnProjectChanged(SelectedItem?.GetIdentity());
			SetAndSelectDefaultProject();
		}
		SyncManager.Sync();
	}

	private int GetGroupIndex(string groupId)
	{
		for (int i = 0; i < ProjectItems.Count; i++)
		{
			if (ProjectItems[i] is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel && projectGroupViewModel.ProjectGroup.id == groupId)
			{
				return i;
			}
		}
		return -1;
	}

	public void OnRenameGroup(ProjectGroupModel group)
	{
		int groupIndex = GetGroupIndex(group.id);
		if (groupIndex < 0)
		{
			return;
		}
		ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel = (ticktick_WPF.ViewModels.ProjectGroupViewModel)ProjectItems[groupIndex];
		if (projectGroupViewModel != null)
		{
			AddProjectGroupDialog addProjectGroupDialog = new AddProjectGroupDialog(projectGroupViewModel.ProjectGroup);
			addProjectGroupDialog.Owner = MainWindowManager.Window;
			addProjectGroupDialog.ProjectGroupEdit += delegate
			{
				ListViewContainer.ReloadProjectData();
			};
			addProjectGroupDialog.ShowDialog();
		}
	}

	public void OnDeleteProjectGroup(ProjectGroupModel projectGroup)
	{
		if (new CustomerDialog(Utils.GetString("LeftMenuProjectDeleteGroupTitle"), string.Format(Utils.GetString("LeftMenuProjectDeleteGroupMessage"), projectGroup.name), MessageBoxButton.OKCancel)
		{
			Owner = Window.GetWindow(this)
		}.ShowDialog() == true)
		{
			DeleteProjectGroup(projectGroup.id);
		}
	}

	private async void DeleteProjectGroup(string groupId)
	{
		ObservableCollection<ProjectModel> projectInGroupList = await ProjectDao.GetProjectsInGroup(groupId);
		foreach (ProjectModel item in projectInGroupList)
		{
			item.groupId = "NONE";
			await ProjectDao.TryUpdateProject(item);
		}
		await ProjectGroupDao.DeleteProjectGroupById(groupId);
		ListViewContainer.ReloadProjectData();
		foreach (ProjectModel item2 in projectInGroupList)
		{
			item2.groupId = "NONE";
			await Communicator.UpdatePutProject(item2);
		}
		UtilLog.Info("ProjectMenu.DeleteProjectGroup : " + groupId);
		await Communicator.DeleteProjectGroup(groupId);
		ProjectWidgetsHelper.OnProjectChanged(new GroupProjectIdentity(new ProjectGroupModel
		{
			id = groupId
		}, null));
		if (SelectedItem?.Id == groupId)
		{
			SetAndSelectDefaultProject();
		}
	}

	public void EditInboxProject()
	{
		EditInboxDialog editInboxDialog = new EditInboxDialog();
		editInboxDialog.Owner = Window.GetWindow(this);
		editInboxDialog.ShowDialog();
		_listViewContainer?.OnProjectSelected();
		ProjectWidgetsHelper.OnProjectChanged(SelectedItem.GetIdentity());
		ListViewContainer.ReloadProjectData();
	}

	public async Task EditProject(ProjectModel project)
	{
		if (project != null)
		{
			await EditProject(project.id);
		}
	}

	public async Task EditProject(string projectId)
	{
		ProjectModel projectModel = await ProjectDao.GetProjectById(projectId);
		if (projectModel != null)
		{
			EditProjectDialog editProjectDialog = new EditProjectDialog(new ticktick_WPF.ViewModels.ProjectViewModel(projectModel)
			{
				IsShowReminderNew = true
			});
			editProjectDialog.Owner = Window.GetWindow(this);
			editProjectDialog.OnProjectSaved += OnProjectSaved;
			editProjectDialog.ShowDialog();
			editProjectDialog.OnProjectSaved -= OnProjectSaved;
		}
	}

	public async void ShareProject(ProjectModel project)
	{
		ShareProjectDialog.TryShowShareDialog(project.id, Window.GetWindow(this));
	}

	public async void CloseOrOpenProject(ProjectModel project)
	{
		if (project == null || (project.closed.HasValue && project.closed.Value && await CheckProjectOverLimit(CheckListLimiteType.Open, isOpen: true)))
		{
			return;
		}
		bool needSync = true;
		if (!project.closed.HasValue || !project.closed.Value)
		{
			if (TaskDefaultDao.GetDefaultSafely().ProjectId == project.id)
			{
				ModifyDefaultProjectDialog modifyDefaultProjectDialog = new ModifyDefaultProjectDialog(isDelete: false, isArchive: true);
				modifyDefaultProjectDialog.Owner = Window.GetWindow(this);
				modifyDefaultProjectDialog.ShowDialog();
				return;
			}
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("ArchiveList"), string.Format(Utils.GetString("ArchiveConfirmMessage"), project.name), MessageBoxButton.OKCancel)
			{
				Owner = Window.GetWindow(this)
			};
			CheckBox checkBox = new CheckBox
			{
				Content = Utils.GetString("ArchiveTogether"),
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
			};
			if (project.IsShareList() && project.IsProjectPermit())
			{
				checkBox.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity60");
				checkBox.SetResourceReference(FrameworkElement.StyleProperty, "SimpleCheckBoxStyle");
				customerDialog.SetExtraContent(checkBox);
			}
			if (customerDialog.ShowDialog() != true)
			{
				return;
			}
			if (checkBox.IsChecked == true)
			{
				needSync = false;
				if (await Communicator.CloseOrOpenProject(project.id, close: true) != null)
				{
					Utils.Toast(Utils.GetString("OptionNotSupport"));
					return;
				}
			}
		}
		else if (project.IsShareList() && project.IsProjectPermit())
		{
			CustomerDialog obj = new CustomerDialog(Utils.GetString("UnArchiveList"), string.Format(Utils.GetString("UnarchivedTogetherDesc"), project.name), MessageBoxButton.OKCancel)
			{
				Owner = Window.GetWindow(this)
			};
			CheckBox checkBox2 = new CheckBox
			{
				Content = Utils.GetString("UnarchiveTogether"),
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
			};
			checkBox2.SetResourceReference(Control.ForegroundProperty, "BaseColorOpacity60");
			checkBox2.SetResourceReference(FrameworkElement.StyleProperty, "SimpleCheckBoxStyle");
			obj.SetExtraContent(checkBox2);
			if (obj.ShowDialog() != true)
			{
				return;
			}
			if (checkBox2.IsChecked == true)
			{
				needSync = false;
				if (await Communicator.CloseOrOpenProject(project.id, close: false) != null)
				{
					Utils.Toast(Utils.GetString("OptionNotSupport"));
					return;
				}
			}
		}
		string id = project.id;
		CompletionLoadStatusDao.DeleteStatusByEntityId(id);
		ProjectModel originalProject = await ProjectDao.GetProjectById(id);
		if (originalProject != null)
		{
			if (!originalProject.closed.HasValue)
			{
				originalProject.closed = false;
			}
			originalProject.closed = !originalProject.closed;
			originalProject.sortOrder = ProjectDragHelper.GetNewSortOrderOnCloseChanged(originalProject);
			if (needSync && originalProject.sync_status == Constants.SyncStatus.SYNC_DONE.ToString())
			{
				originalProject.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			}
			if (TaskStickyWindow.TryCloseInProject(project.id))
			{
				Utils.Toast(Utils.GetString("StickyClosed"));
			}
			await ProjectDao.TryUpdateProject(originalProject);
			SelectedItem = null;
			ListViewContainer.ReloadProjectData();
			SyncManager.TryDelaySync();
			if (!originalProject.closed.Value)
			{
				SyncManager.PullProjectTasks(originalProject.id);
			}
			else
			{
				SyncSortOrderDao.OnProjectDeletedOrClosed(project.id);
			}
		}
		UtilLog.Info($"ProjectMenu.CloseOrOpenProject : {project.id},{project.closed}");
		DataChangedNotifier.NotifyProjectChanged();
	}

	public async Task DuplicateProject(ProjectModel project)
	{
		if (await CheckProjectOverLimit(CheckListLimiteType.Copy) || project == null)
		{
			return;
		}
		DuplicateProjectDialog duplicateProjectDialog = new DuplicateProjectDialog
		{
			Owner = Window.GetWindow(this)
		};
		duplicateProjectDialog.ShowDialog();
		ProjectIdentityModel identity;
		long sortOrder;
		if (duplicateProjectDialog.Option != DuplicateProjectEnum.None)
		{
			string originalName = project.name;
			int originalUserCount = project.userCount;
			project.name = project.name + " " + Utils.GetString("ProjectCopy");
			project.userCount = 1;
			identity = await Communicator.DuplicateProject(project.id, project, (int)duplicateProjectDialog.Option);
			project.name = originalName;
			project.userCount = originalUserCount;
			sortOrder = ProjectDragHelper.GetNewSortOrder(upDown: false, project.groupId, project.id, project.teamId);
			if (identity != null)
			{
				SyncManager.Sync();
				DataChangedNotifier.SyncDone += NavigateProjectOnAutoSyncDone;
			}
			UtilLog.Info("ProjectMenu.DuplicateProject : origin " + project.id + ",new " + identity?.id);
		}
		async void NavigateProjectOnAutoSyncDone(object sender, SyncResult changed)
		{
			DataChangedNotifier.SyncDone -= NavigateProjectOnAutoSyncDone;
			ProjectModel newProject = CacheManager.GetProjectById(identity.id);
			if (newProject != null)
			{
				newProject.sortOrder = sortOrder;
				newProject.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				await ColumnBatchHandler.MergeWithServer(newProject.id);
				await ProjectDao.TryUpdateProject(newProject);
				ListViewContainer.ReloadProjectData();
				App.Window.NavigateNormalProject(newProject.id);
			}
		}
	}

	public async Task DeleteOrExitProject(ProjectModel project, bool isDelete = true)
	{
		if (project == null)
		{
			return;
		}
		if (TaskDefaultDao.GetDefaultSafely().ProjectId == project.id)
		{
			ModifyDefaultProjectDialog modifyDefaultProjectDialog = new ModifyDefaultProjectDialog(isDelete: true);
			modifyDefaultProjectDialog.Owner = Window.GetWindow(this);
			modifyDefaultProjectDialog.ShowDialog();
			return;
		}
		(List<string>, List<string>) obj = await CalendarConnectService.GetConnectedProjectIds();
		var (list, _) = obj;
		if (obj.Item2.Contains(project.id))
		{
			CalendarConnectInfo calendarConnectInfo = await CalendarConnectService.GetConnectInfoByDefaultId(project.id);
			if (calendarConnectInfo == null)
			{
				return;
			}
			if (!Utils.IsNetworkAvailable())
			{
				Utils.Toast(Utils.GetString("NetworkException"));
				return;
			}
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("CannotDeleteDefault"), string.Format(Utils.GetString("DeleteConnectDefaultProjectMessage"), calendarConnectInfo?.account), Utils.GetString("Modify"), Utils.GetString("Cancel"), Window.GetWindow(this));
			customerDialog.ShowDialog();
			if (customerDialog.DialogResult == true)
			{
				CalendarContentProject calendarContentProject = calendarConnectInfo.relProjects?.FirstOrDefault((CalendarContentProject f) => f.syncProjectId == project.id);
				if (calendarContentProject != null)
				{
					LinkGoogleCalendarWindow linkGoogleCalendarWindow = new LinkGoogleCalendarWindow(calendarConnectInfo, calendarContentProject.Copy());
					linkGoogleCalendarWindow.Owner = Window.GetWindow(this);
					linkGoogleCalendarWindow.ShowDialog();
				}
			}
			return;
		}
		bool disConnectShow = false;
		List<CalendarConnectInfo> connectInfos = null;
		if (list.Contains(project.id))
		{
			if (!Utils.IsNetworkAvailable())
			{
				Utils.Toast(Utils.GetString("NetworkException"));
				return;
			}
			connectInfos = await CalendarConnectService.GetConnectInfosByProjectId(project.id);
			List<CalendarConnectInfo> list2 = connectInfos;
			if (list2 != null && list2.Count > 0)
			{
				CustomerDialog customerDialog2 = new CustomerDialog(string.Format(Utils.GetString(isDelete ? "DeleteList_Title" : "ExitProjectTitle"), project.name), string.Format(Utils.GetString((!isDelete) ? "ExitConnectProjectMessage" : (project.IsShareList() ? "DeleteConnectedShareProjectMessage" : "DeleteConnectProjectMessage")), connectInfos[0].account), Utils.GetString("OK"), Utils.GetString("Cancel"), Window.GetWindow(this));
				customerDialog2.ShowDialog();
				if (!customerDialog2.DialogResult.HasValue || !customerDialog2.DialogResult.Value)
				{
					return;
				}
				disConnectShow = true;
			}
		}
		if ((!disConnectShow && new CustomerDialog(string.Format(Utils.GetString(isDelete ? "DeleteList_Title" : "ExitProjectTitle"), project.name), isDelete ? Utils.GetString(project.IsShareList() ? "DeleteShareList_Content" : "DeleteList_Content") : string.Format(Utils.GetString("ExitProjectSummary"), project.name), Utils.GetString("Delete"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() != true) || (!project.IsShareList() && !TaskService.ConfirmBatchDeleteTasks((await TaskDao.GetTasksInProjectAsync(project.id)).Count)))
		{
			return;
		}
		List<CalendarConnectInfo> list3 = connectInfos;
		if (list3 != null && list3.Count > 0)
		{
			foreach (CalendarConnectInfo item in connectInfos)
			{
				CalendarContentProject calendarContentProject2 = item.relProjects.FirstOrDefault((CalendarContentProject r) => r.syncProjectScope == project.id);
				bool flag = calendarContentProject2 != null;
				if (flag)
				{
					flag = !(await Communicator.UnbindProjectCalendar(item.id, calendarContentProject2.id));
				}
				if (flag)
				{
					Utils.Toast("NetworkException");
					return;
				}
			}
			CalendarService.PullCalendarConnects();
		}
		if (await ProjectDao.DeleteProjectById(project.id, showTaskDeleteConfirm: false))
		{
			ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p is NormalProjectViewModel normalProjectViewModel && normalProjectViewModel.ProjectId == project.id);
			if (projectItemViewModel != null)
			{
				ProjectItems.Remove(projectItemViewModel);
			}
			if (TaskStickyWindow.TryCloseInProject(project.id))
			{
				Utils.Toast(Utils.GetString("StickyClosed"));
			}
			UtilLog.Info("ProjectMenu.DeleteOrExitProject : " + project.id);
			ListViewContainer.ReloadProjectData();
			SyncManager.TryDelaySync();
			ProjectWidgetsHelper.OnProjectChanged(SelectedItem.GetIdentity());
			SyncSortOrderDao.OnProjectDeletedOrClosed(project.id);
			if (SelectedItem?.Id == project.id)
			{
				SetAndSelectDefaultProject();
			}
		}
	}

	public async void EditTag(TagModel tag, bool canEditTitle)
	{
		if (tag == null)
		{
			return;
		}
		AddOrEditTagWindow obj = new AddOrEditTagWindow(tag, canEditTitle)
		{
			Owner = Window.GetWindow(this)
		};
		string originName = tag.name;
		string originParent = tag.parent;
		bool wasParent = tag.IsParent();
		obj.TagSaved += delegate(object sender, TagModel newTag)
		{
			UtilLog.Info($"ProjectMenu.EditTag : origin {originName},saved {newTag}");
			if (newTag != null)
			{
				bool flag = SelectedItem is TagProjectViewModel tagProjectViewModel && tagProjectViewModel.TagModel.id == newTag.id;
				UpdateTagItems(ProjectItems, newTag);
				if (originName != newTag.name)
				{
					DataChangedNotifier.NotifyProjectPinRemoteChanged();
				}
				if (SelectedItem == null || (flag && originName != newTag.name))
				{
					SelectedItem = new TagProjectViewModel(newTag);
					SetSelectedItemHighlighted();
					_listViewContainer?.OnProjectSelected();
				}
				else if (SelectedItem.GetIdentity().SortOption.ContainsSortType(Constants.SortType.tag))
				{
					_listViewContainer?.OnProjectSelected();
				}
			}
			if (newTag == null || !string.Equals(originParent ?? string.Empty, newTag.parent ?? string.Empty, StringComparison.Ordinal) || (wasParent && originName != newTag.name))
			{
				ListViewContainer.ReloadProjectData();
			}
		};
		obj.ShowDialog();
	}

	public void AddSubTag(TagModel targetTag)
	{
		AddOrEditTagWindow addOrEditTagWindow = new AddOrEditTagWindow();
		addOrEditTagWindow.Owner = Window.GetWindow(this);
		addOrEditTagWindow.SetAddSub(targetTag);
		addOrEditTagWindow.TagSaved += async delegate(object arg, TagModel tag)
		{
			if (LocalSettings.Settings.SmartListTag == 2)
			{
				ShowTagCreateHint();
			}
			else if (tag != null)
			{
				UtilLog.Info("ProjectMenu.AddTag : " + tag.name);
				SelectTagProject(tag.name);
			}
			ListViewContainer.ReloadProjectData();
		};
		addOrEditTagWindow.ShowDialog();
	}

	public void MergeTag(TagModel tag)
	{
		MergeTagWindow mergeTagWindow = new MergeTagWindow(tag)
		{
			Owner = Window.GetWindow(this)
		};
		mergeTagWindow.ShowDialog();
		if (!string.IsNullOrEmpty(mergeTagWindow.MergeTag))
		{
			UtilLog.Info("ProjectMenu.MergeTag : from " + tag.name + ",to " + mergeTagWindow.MergeTag);
			SelectTagProject(mergeTagWindow.MergeTag);
			ListViewContainer.ReloadProjectData();
		}
	}

	public async void DeleteTag(TagModel tag)
	{
		bool? flag = new CustomerDialog(string.Format(Utils.GetString("DeleteTagTitle"), tag.GetDisplayName()), Utils.GetString("DeleteTagShortHint"), MessageBoxButton.OKCancel)
		{
			Owner = Window.GetWindow(this)
		}.ShowDialog();
		if (flag.HasValue && flag.Value)
		{
			await TagService.DeleteTag(tag.name);
			await Communicator.DeleteTag2(tag.name);
			UtilLog.Info("ProjectMenu.DeleteTag : " + tag.name);
			SyncSortOrderDao.OnTagDeleted(tag.name);
			ListViewContainer.ReloadProjectData();
			if (SelectedItem?.Id == tag.name)
			{
				SetAndSelectDefaultProject();
			}
		}
	}

	private async Task OnBindCalendarProjectClick(BindCalendarAccountProjectViewModel model, bool forcePull = false)
	{
		if (_subscribeLastPullTimeDict == null)
		{
			_subscribeLastPullTimeDict = new Dictionary<string, DateTime>();
		}
		if (!forcePull && (model?.Id == null || (_subscribeLastPullTimeDict.ContainsKey(model.Id) && (DateTime.Now - _subscribeLastPullTimeDict[model.Id]).TotalSeconds < 30.0)))
		{
			return;
		}
		_subscribeLastPullTimeDict[model.Id] = DateTime.Now;
		if (model.Account != null)
		{
			Task.Run(async delegate
			{
				EventArchiveSyncService.PullArchivedModelsAsync();
				PullRemoteCalendarEvents(model.Account.Id, model.Account.Kind, model.Account.Site);
			});
		}
	}

	private async Task PullRemoteCalendarEvents(string accountId, string kind, string site)
	{
		await CalendarService.PullAccountEvents(accountId, kind, site);
		LoadData();
	}

	private async Task OnSubscribeCalendarClick(SubscribeCalendarProjectViewModel model)
	{
		if (_subscribeLastPullTimeDict == null)
		{
			_subscribeLastPullTimeDict = new Dictionary<string, DateTime>();
		}
		if (model?.Id == null || (_subscribeLastPullTimeDict.ContainsKey(model.Id) && (DateTime.Now - _subscribeLastPullTimeDict[model.Id]).TotalSeconds < 30.0))
		{
			return;
		}
		_subscribeLastPullTimeDict[model.Id] = DateTime.Now;
		if (model.Profile == null)
		{
			return;
		}
		Task.Run(async delegate
		{
			string text = model.Profile.Id;
			if (string.IsNullOrEmpty(text))
			{
				text = Utils.Base64Encode(model.Profile.Url);
			}
			await SubscribeCalendarHelper.ParseUrlCalendar(text, model.Profile.Url);
			EventArchiveSyncService.PullArchivedModelsAsync();
			base.Dispatcher.Invoke(delegate
			{
				ListViewContainer.ReloadProjectData();
				_listViewContainer?.OnProjectSelected();
				if (ABTestManager.IsNewRemindCalculate())
				{
					EventReminderCalculator.InitAllEventsReminderTimes();
				}
				else
				{
					ReminderCalculator.AssembleReminders();
				}
			});
		});
	}

	public async void ShowOrHideProject(SmartProject project, ContextActionKey action)
	{
		string name = string.Empty;
		string text = string.Empty;
		int num = action switch
		{
			ContextActionKey.ShowIfNotEmpty => 2, 
			ContextActionKey.Show => 0, 
			_ => 1, 
		};
		if (!(project is AllProject))
		{
			if (!(project is TodayProject))
			{
				if (!(project is TomorrowProject))
				{
					if (!(project is WeekProject))
					{
						if (!(project is AssignProject))
						{
							if (!(project is CompletedProject))
							{
								if (!(project is AbandonedProject))
								{
									if (!(project is TrashProject))
									{
										if (!(project is SummaryProject))
										{
											if (project is InboxProject)
											{
												if (action == ContextActionKey.Hide && LocalSettings.Settings.SmartListInbox != 1 && TaskDefaultDao.GetDefaultSafely().ProjectId == LocalSettings.Settings.InServerBoxId)
												{
													ModifyDefaultProjectDialog modifyDefaultProjectDialog = new ModifyDefaultProjectDialog();
													modifyDefaultProjectDialog.Owner = Window.GetWindow(this);
													modifyDefaultProjectDialog.ShowDialog();
													return;
												}
												text = "SmartListInbox";
												name = Utils.GetString("Inbox");
											}
										}
										else
										{
											text = "SmartListSummary";
											name = Utils.GetString("Summary");
										}
									}
									else
									{
										text = "SmartListTrash";
										name = Utils.GetString("Trash");
									}
								}
								else
								{
									text = "SmartListAbandoned";
									name = Utils.GetString("Abandoned");
								}
							}
							else
							{
								text = "SmartListComplete";
								name = Utils.GetString("Completed");
							}
						}
						else
						{
							text = "SmartListForMe";
							name = Utils.GetString("AssignToMe");
						}
					}
					else
					{
						text = "SmartList7Day";
						name = Utils.GetString("Next7Day");
					}
				}
				else
				{
					text = "SmartListTomorrow";
					name = Utils.GetString("Tomorrow");
				}
			}
			else
			{
				text = "SmartListToday";
				name = Utils.GetString("Today");
			}
		}
		else
		{
			text = "SmartListAll";
			name = Utils.GetString("All");
		}
		if (!string.IsNullOrEmpty(text))
		{
			LocalSettings.Settings[text] = num;
			ListViewContainer.ReloadProjectData();
		}
		switch (action)
		{
		case ContextActionKey.ShowIfNotEmpty:
		{
			bool flag = false;
			if (!(project is InboxProject))
			{
				if (!(project is TodayProject))
				{
					if (!(project is TomorrowProject))
					{
						if (!(project is WeekProject))
						{
							if (!(project is AssignProject))
							{
								if (project is AbandonedProject)
								{
									flag = TaskCache.GetAllTask().Any((TaskBaseViewModel t) => t.Status == -1 && t.Deleted == 0);
								}
							}
							else
							{
								flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_assigned")) > 0;
							}
						}
						else
						{
							flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_week")) > 0;
						}
					}
					else
					{
						flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_tomorrow")) > 0;
					}
				}
				else
				{
					flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_today")) > 0;
				}
			}
			else
			{
				flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateInboxProject()) > 0;
			}
			if (!flag)
			{
				Utils.Toast(string.Format(Utils.GetString("AutoSmartProjectTip"), name));
			}
			break;
		}
		case ContextActionKey.Hide:
			Utils.Toast(string.Format(Utils.GetString("HideHint"), name));
			SetAndSelectDefaultProject();
			break;
		}
	}

	public async void OnAddFilterClick()
	{
		if (ProChecker.CheckPro(ProType.Filter))
		{
			UserActCollectUtils.AddClickEvent("project_list_ui", "filter", "add");
			await AddFilter();
		}
	}

	private async Task AddFilter()
	{
		ObservableCollection<ProjectModel> source = await ProjectDao.GetProjectsWithoutClosed();
		string inboxId = Utils.GetInboxId();
		ProjectModel projectModel = source.FirstOrDefault((ProjectModel p) => p.id == inboxId);
		if (projectModel != null)
		{
			projectModel.sortOrder = long.MinValue;
		}
		FilterEditDialog filterEditDialog = new FilterEditDialog();
		filterEditDialog.Owner = Window.GetWindow(this);
		filterEditDialog.FilterSaved += OnFilterAdded;
		filterEditDialog.ShowDialog();
	}

	private void OnFilterAdded(object sender, FilterModel filter)
	{
		if (filter != null)
		{
			ListViewContainer.ReloadProjectData();
			FilterProjectIdentity projectId = new FilterProjectIdentity(filter)
			{
				SortOption = filter.GetSortOption()
			};
			ListViewContainer listViewContainer = GetListViewContainer();
			if (listViewContainer == null || listViewContainer.Mode != ListMode.Search)
			{
				SelectProject(projectId);
			}
			SyncManager.Sync();
		}
	}

	public async Task OnPtfAllClick(ProjectItemViewModel model)
	{
		if (!(_projectList.ItemsSource is ObservableCollection<ProjectItemViewModel> observableCollection))
		{
			return;
		}
		ProjectItemViewModel section = observableCollection.FirstOrDefault((ProjectItemViewModel p) => p.Id == model.Id);
		int num = observableCollection.IndexOf(section);
		if (section == null || num < 0 || ProjectDragging)
		{
			return;
		}
		section.Open = !section.Open;
		if (section.Open)
		{
			if (!(section is TeamGroupViewModel))
			{
				if (!(section is PtfAllViewModel ptfAllViewModel))
				{
					return;
				}
				switch (ptfAllViewModel.Type)
				{
				case PtfType.Project:
					ProjectDataProvider.AddNormalProjects(observableCollection, num + 1);
					LocalSettings.Settings.AllProjectOpened = true;
					break;
				case PtfType.Tag:
					if (ptfAllViewModel.InSubSection)
					{
						LocalSettings.Settings.AllShareTagOpened = true;
						await ProjectDataProvider.AddShareTagProject(observableCollection, num + 1);
					}
					else
					{
						await ProjectDataProvider.AddTagProject(observableCollection, num + 1);
						LocalSettings.Settings.AllTagOpened = true;
					}
					break;
				case PtfType.Filter:
					ProjectDataProvider.AddFilterProject(observableCollection, num + 1);
					LocalSettings.Settings.AllFilterOpened = true;
					break;
				case PtfType.Subscribe:
					ProjectDataProvider.AddSubscribeCalendar(observableCollection, num + 1);
					LocalSettings.Settings.AllSubscribeOpened = true;
					break;
				}
				return;
			}
			{
				foreach (ProjectItemViewModel child in section.Children)
				{
					observableCollection.Insert(++num, child);
					if (child.Children == null || !child.Children.Any() || !child.Open)
					{
						continue;
					}
					foreach (ProjectItemViewModel child2 in child.Children)
					{
						observableCollection.Insert(++num, child2);
					}
				}
				return;
			}
		}
		IEnumerable<ProjectItemViewModel> enumerable = null;
		if (!(section is TeamGroupViewModel))
		{
			if (section is PtfAllViewModel ptfAllViewModel2)
			{
				switch (ptfAllViewModel2.Type)
				{
				case PtfType.Project:
					enumerable = observableCollection.Where(ProjectDataProvider.IsProjectCategory);
					LocalSettings.Settings.AllProjectOpened = false;
					break;
				case PtfType.Tag:
					if (ptfAllViewModel2.InSubSection)
					{
						LocalSettings.Settings.AllShareTagOpened = false;
						enumerable = observableCollection.Where(ProjectDataProvider.IsShareTagCategory);
					}
					else
					{
						enumerable = observableCollection.Where(ProjectDataProvider.IsTagCategory);
						LocalSettings.Settings.AllTagOpened = false;
					}
					break;
				case PtfType.Filter:
					enumerable = observableCollection.Where(ProjectDataProvider.IsFilterCategory);
					LocalSettings.Settings.AllFilterOpened = false;
					break;
				case PtfType.Subscribe:
					enumerable = observableCollection.Where(ProjectDataProvider.IsSubscribeCategory);
					LocalSettings.Settings.AllSubscribeOpened = false;
					break;
				}
			}
		}
		else
		{
			enumerable = observableCollection.Where((ProjectItemViewModel m) => !(m is TeamGroupViewModel) && m.TeamId == section.TeamId);
		}
		if (enumerable == null)
		{
			return;
		}
		foreach (ProjectItemViewModel item in enumerable.ToList())
		{
			observableCollection.Remove(item);
		}
	}

	public ProjectIdentity GetSelectedProject()
	{
		if (SelectedItem != null)
		{
			return SelectedItem.GetIdentity();
		}
		string saveId = Utils.FindParent<IListViewParent>(GetListViewContainer())?.GetSelectedProject();
		SelectedItem = ProjectItemViewModel.BuildProject(saveId);
		return ProjectIdentity.BuildProject(saveId);
	}

	public void SetSelectedItemHighlighted()
	{
		ClearLastSelected();
		ProjectItemViewModel selectedItem = GetSelectedItem();
		if (selectedItem != null)
		{
			selectedItem.Selected = true;
		}
		else if (SelectedItem is BindCalendarAccountProjectViewModel bindCalendarAccountProjectViewModel)
		{
			if (CacheManager.GetAccountCalById(bindCalendarAccountProjectViewModel.Id) == null)
			{
				SetAndSelectDefaultProject();
			}
		}
		else if (SelectedItem is SubscribeCalendarProjectViewModel subscribeCalendarProjectViewModel && !CacheManager.GetSubscribeDict().ContainsKey(subscribeCalendarProjectViewModel.Id))
		{
			SetAndSelectDefaultProject();
		}
	}

	public void SetAndSelectDefaultProject()
	{
		ProjectModel projectById = CacheManager.GetProjectById(TaskDefaultDao.GetDefaultSafely().ProjectId);
		if (projectById != null && !projectById.Isinbox)
		{
			SelectedItem = new NormalProjectViewModel(projectById);
		}
		else
		{
			SelectedItem = new ticktick_WPF.ViewModels.InboxProjectViewModel(new InboxProject());
		}
		Utils.FindParent<IListViewParent>(GetListViewContainer())?.SaveSelectedProject(SelectedItem?.GetSaveIdentity());
		SetSelectedItemHighlighted();
		_listViewContainer?.OnProjectSelected();
	}

	private void ClearLastSelected()
	{
		foreach (ProjectItemViewModel projectItem in ProjectItems)
		{
			if (projectItem.Selected)
			{
				projectItem.Selected = false;
			}
		}
	}

	private ProjectItemViewModel GetSelectedItem()
	{
		if (ProjectItems != null && ProjectItems.Count > 0 && SelectedItem != null)
		{
			ProjectItemViewModel selectedItem = SelectedItem;
			ticktick_WPF.ViewModels.ProjectGroupViewModel group = selectedItem as ticktick_WPF.ViewModels.ProjectGroupViewModel;
			if (group != null)
			{
				return ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p is ticktick_WPF.ViewModels.ProjectGroupViewModel && !string.IsNullOrEmpty(p.Id) && p.Id == group.Id);
			}
			return ProjectItems.FirstOrDefault((ProjectItemViewModel p) => !string.IsNullOrEmpty(p.Id) && p.Id == SelectedItem.Id);
		}
		return null;
	}

	private async void OnProjectSaved(object sender, ticktick_WPF.ViewModels.ProjectViewModel project)
	{
		ListViewContainer.ReloadProjectData();
		if (project != null)
		{
			ProjectModel project2 = await ProjectDao.GetProjectById(project.id);
			if (SelectedItem?.Id == project.id || project.IsNew)
			{
				SelectedItem = new NormalProjectViewModel(project2);
				SetSelectedItemHighlighted();
			}
			_listViewContainer?.OnProjectSelected();
			ProjectWidgetsHelper.OnProjectChanged(new NormalProjectIdentity(project2));
			SyncManager.Sync();
		}
	}

	public void ClearDropSelected()
	{
		if (ProjectItems != null && ProjectItems.Any())
		{
			ProjectItems.ToList().ForEach(delegate(ProjectItemViewModel model)
			{
				model.DropSelected = false;
			});
		}
	}

	private void ClearLastSelected(ProjectItemViewModel selectedItem)
	{
		foreach (ProjectItemViewModel projectItem in ProjectItems)
		{
			if (projectItem != selectedItem && projectItem.Selected)
			{
				projectItem.Selected = false;
			}
		}
	}

	public void SelectInbox()
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel item) => SpecialListUtils.IsInboxProject(item.Id));
		if (projectItemViewModel != null)
		{
			projectItemViewModel.Selected = true;
		}
		OnProjectClick(new ticktick_WPF.ViewModels.InboxProjectViewModel(new InboxProject()), manual: false);
	}

	public void SelectToday()
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel item) => SpecialListUtils.IsTodayProject(item.Id));
		if (projectItemViewModel != null)
		{
			projectItemViewModel.Selected = true;
		}
		OnProjectClick(new ticktick_WPF.ViewModels.SmartProjectViewModel(new TodayProject()), manual: false);
	}

	public bool SelectSmartList(SmartListType smartType)
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel item) => SpecialListUtils.IsTargetSmartList(smartType, item.Id));
		OnProjectClick(ticktick_WPF.ViewModels.SmartProjectViewModel.BuildModel(smartType), manual: false);
		if (projectItemViewModel != null)
		{
			projectItemViewModel.Selected = true;
			_canBringIntoView = true;
			_projectList.ScrollIntoView(projectItemViewModel);
			return true;
		}
		return false;
	}

	public void SelectTrash()
	{
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel item) => SpecialListUtils.IsTrashProject(item.Id));
		if (projectItemViewModel != null)
		{
			OnProjectClick(new ticktick_WPF.ViewModels.SmartProjectViewModel(new TrashProject()), manual: false);
			projectItemViewModel.Selected = true;
		}
	}

	public async void OnPinClick(string id, int type, bool isPinned)
	{
		if (isPinned)
		{
			await ProjectPinSortOrderService.Insert(id, type);
			LoadPinModels();
		}
		else
		{
			await ProjectPinSortOrderService.Delete(id, type);
			LoadPinModels();
		}
	}

	public void OnLogOut()
	{
		SelectedItem = null;
	}

	public bool TrySelectSmartProject(SmartListType smartListType)
	{
		SelectedItem = null;
		return SelectSmartList(smartListType);
	}

	public bool IsNoteProjectSelected()
	{
		return SelectedItem?.GetIdentity()?.IsNote == true;
	}

	private void OnItemBringIntoView(object sender, RequestBringIntoViewEventArgs e)
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

	public void ReSelectCurrent()
	{
		if (!(SelectedItem is NormalProjectViewModel normalProjectViewModel))
		{
			return;
		}
		ProjectModel projectById = CacheManager.GetProjectById(normalProjectViewModel.Id);
		if (projectById != null && !projectById.delete_status)
		{
			_listViewContainer?.OnProjectSelected();
			return;
		}
		if (normalProjectViewModel.Project.userid == LocalSettings.Settings.LoginUserId)
		{
			Utils.Toast(Utils.GetString("ProjectNotFoundTips"));
		}
		SelectInbox();
	}

	public void SetItemCount(ProjectIdentity identity, int count)
	{
		string id = ((identity is SmartProjectIdentity smartProjectIdentity) ? smartProjectIdentity.QueryId : identity.Id);
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel i) => i.Id == id);
		if (projectItemViewModel != null)
		{
			projectItemViewModel.Count = count;
		}
	}

	public void NavigateProject(string type, string id, bool scrollToItem = true)
	{
		ProjectIdentity projectIdentity = null;
		switch (type)
		{
		case "project":
			projectIdentity = ProjectIdHelper.GetProjectIdentity(id);
			break;
		case "group":
			projectIdentity = ProjectIdHelper.GetGroupIdentity(id);
			break;
		case "tag":
			SelectTagProject(id.ToLower(), scrollToItem);
			return;
		case "filter":
			projectIdentity = ProjectIdHelper.GetFilterIdentity(id);
			break;
		case "calendar":
			projectIdentity = ProjectIdHelper.GetCalendarIdentity(id);
			break;
		case "smart":
			projectIdentity = SmartProjectIdentity.BuildSmartProject(id);
			break;
		case "date":
			projectIdentity = new DateProjectIdentity(id);
			break;
		}
		if (projectIdentity != null)
		{
			SelectProject(projectIdentity, invokeEvent: true, scrollToItem);
		}
	}

	public async Task DuplicateFilter(FilterModel filter)
	{
		filter = CacheManager.GetFilterById(filter?.id);
		if (filter != null)
		{
			FilterModel filterModel = filter.Copy();
			filterModel.id = Utils.GetGuid();
			filterModel.name = filterModel.name + " " + Utils.GetString("ProjectCopy");
			filterModel.syncStatus = 0;
			filterModel.sortOrder = ProjectDragHelper.CalculateInsertFilterSortOrder(upDown: false, filter.id);
			await FilterDao.AddFilter(filterModel);
			SyncManager.Sync();
		}
	}

	public void ShowOrHidePtf(PtfAllViewModel model, int index)
	{
		if (model.IsTag)
		{
			LocalSettings.Settings.SmartListTag = index;
		}
		if (model.IsFilter)
		{
			LocalSettings.Settings.ShowCustomSmartList = index;
		}
		ListViewContainer.ReloadProjectData();
	}

	public void SetInPopup(bool inPopup)
	{
		InPopup = inPopup;
		_projectList.Margin = new Thickness(0.0, (_pinViewContent.Content == null && !inPopup) ? 22 : 0, 0.0, 10.0);
		SetPopupResource();
	}

	public async Task ScrollToSelected(bool wait = false)
	{
		if (wait)
		{
			_listScrollViewer?.ScrollToVerticalOffset(0.0);
			await Task.Yield();
		}
		ProjectItemViewModel selectedItem = GetSelectedItem();
		if (selectedItem != null)
		{
			_canBringIntoView = true;
			_projectList?.ScrollIntoView(selectedItem);
		}
		else
		{
			_listScrollViewer?.ScrollToVerticalOffset(0.0);
		}
	}

	private void TryMoveItem(ProjectItemViewModel model, MouseEventArgs e)
	{
		TryShowLine();
		ListViewItem mousePointItem = Utils.GetMousePointItem<ListViewItem>(e.GetPosition(_projectList), _projectList);
		if (mousePointItem == null)
		{
			return;
		}
		if (mousePointItem.DataContext is ProjectItemViewModel projectItemViewModel)
		{
			if (!CheckHoverModelEnable(projectItemViewModel))
			{
				return;
			}
			TryShowChildLine(projectItemViewModel);
			_hoverIndex = ProjectItems.IndexOf(projectItemViewModel);
			if (!object.Equals(_dragHoverItem, mousePointItem))
			{
				_dragHoverItem = mousePointItem;
			}
			if (_hoverIndex == _currentIndex)
			{
				return;
			}
			if (_hoverModel != projectItemViewModel)
			{
				if (_hoverModel != null)
				{
					_hoverModel.DropSelected = false;
					_hoverModel.Hover = false;
				}
				_hoverModel = projectItemViewModel;
			}
			Point position = e.GetPosition(_dragHoverItem);
			if (!(_hoverModel is EmptySubViewModel) && position.Y > _dragHoverItem.ActualHeight / 3.0 && position.Y < _dragHoverItem.ActualHeight * 2.0 / 3.0)
			{
				if (CanDropCreateGroup(model, _hoverModel))
				{
					_hoverModel.DropSelected = true;
				}
			}
			else if (_hoverIndex < _currentIndex && ((position.Y > 0.0 && position.Y < _dragHoverItem.ActualHeight / 3.0) || (_hoverModel is EmptySubViewModel && position.Y > 0.0 && position.Y < _dragHoverItem.ActualHeight * 3.0 / 4.0)) && ProjectDragHelper.CanSwapItems(model, _hoverModel, isUpDowm: false))
			{
				_previousOverGroup = null;
				SwapItems(_hoverIndex, model);
				_currentIndex = _hoverIndex;
				_hoverModel.DropSelected = false;
			}
			else if (_hoverIndex > _currentIndex && ((position.Y > _dragHoverItem.ActualHeight * 2.0 / 3.0 && position.Y < _dragHoverItem.ActualHeight) || (_hoverModel is EmptySubViewModel && position.Y > _dragHoverItem.ActualHeight / 4.0 && position.Y < _dragHoverItem.ActualHeight)) && ProjectDragHelper.CanSwapItems(model, _hoverModel, isUpDowm: true))
			{
				_previousOverGroup = null;
				SwapItems(_hoverIndex, model);
				_currentIndex = _hoverIndex;
				_hoverModel.DropSelected = false;
			}
			else
			{
				_previousOverGroup = null;
				_hoverModel.DropSelected = false;
			}
		}
		else
		{
			ClearDropSelected();
		}
	}

	private bool CheckHoverModelEnable(ProjectItemViewModel hoverModel)
	{
		ProjectItemViewModel draggingModel = _draggingModel;
		if (!(draggingModel is ticktick_WPF.ViewModels.ProjectGroupViewModel) && !(draggingModel is NormalProjectViewModel))
		{
			if (draggingModel is ticktick_WPF.ViewModels.SmartProjectViewModel)
			{
				return hoverModel is ticktick_WPF.ViewModels.SmartProjectViewModel;
			}
			if (draggingModel is TagProjectViewModel)
			{
				if (!(hoverModel is TagProjectViewModel))
				{
					if (hoverModel is EmptySubViewModel emptySubViewModel)
					{
						return emptySubViewModel.IsEmptyTag;
					}
					return false;
				}
				return true;
			}
			if (draggingModel is FilterProjectViewModel)
			{
				return hoverModel is FilterProjectViewModel;
			}
			if (draggingModel is PtfAllViewModel)
			{
				return hoverModel is PtfAllViewModel;
			}
			if (draggingModel is SubscribeCalendarProjectViewModel || draggingModel is BindCalendarAccountProjectViewModel)
			{
				if (!(hoverModel is BindCalendarAccountProjectViewModel))
				{
					return hoverModel is SubscribeCalendarProjectViewModel;
				}
				return true;
			}
		}
		else
		{
			if ((_draggingModel.TeamId ?? "") != (hoverModel.TeamId ?? ""))
			{
				return false;
			}
			if (_draggingModel is NormalProjectViewModel normalProjectViewModel && !normalProjectViewModel.Project.IsValid())
			{
				if (hoverModel is NormalProjectViewModel normalProjectViewModel2)
				{
					return !normalProjectViewModel2.Project.IsValid();
				}
				return false;
			}
			if (hoverModel is ClosedProjectGroupViewModel)
			{
				return false;
			}
			if (hoverModel is NormalProjectViewModel normalProjectViewModel3)
			{
				return normalProjectViewModel3.Project.IsValid();
			}
			if (hoverModel is ticktick_WPF.ViewModels.ProjectGroupViewModel)
			{
				return true;
			}
			if (hoverModel is EmptySubViewModel emptySubViewModel2)
			{
				return !emptySubViewModel2.IsEmptyTag;
			}
		}
		return false;
	}

	private void TryShowLine()
	{
		try
		{
			if (!(_draggingModel is NormalProjectViewModel) && !(_draggingModel is ticktick_WPF.ViewModels.ProjectGroupViewModel) && !(_draggingModel is TagProjectViewModel))
			{
				return;
			}
			if (ProjectItems.Count > _currentIndex - 1 && _currentIndex > 0 && ProjectItems[_currentIndex - 1] is EmptySubViewModel emptySubViewModel)
			{
				if (_lastHoverLine == null)
				{
					_lastHoverLine = emptySubViewModel;
				}
				else if (!_lastHoverLine.Equals(emptySubViewModel))
				{
					_lastHoverLine.Hover = false;
					_lastHoverLine = emptySubViewModel;
				}
				_lastHoverLine.Hover = true;
			}
			else if (ProjectItems.Count > _currentIndex + 1 && ProjectItems[_currentIndex + 1] is EmptySubViewModel emptySubViewModel2)
			{
				if (_lastHoverLine == null)
				{
					_lastHoverLine = emptySubViewModel2;
				}
				else if (!_lastHoverLine.Equals(emptySubViewModel2))
				{
					_lastHoverLine.Hover = false;
					_lastHoverLine = emptySubViewModel2;
				}
				_lastHoverLine.Hover = true;
			}
			else if (_lastHoverLine != null)
			{
				_lastHoverLine.Hover = false;
			}
		}
		catch (Exception)
		{
		}
	}

	private void TryShowChildLine(ProjectItemViewModel hoverModel)
	{
		if (!(_draggingModel is ticktick_WPF.ViewModels.ProjectGroupViewModel) && !(_draggingModel is TagProjectViewModel { IsParent: not false }))
		{
			return;
		}
		if (_draggingModel?.InSubSection != hoverModel?.InSubSection)
		{
			return;
		}
		string lineParent = "";
		if (!(hoverModel is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel))
		{
			if (!(hoverModel is ticktick_WPF.ViewModels.SubProjectViewModel subProjectViewModel))
			{
				if (hoverModel is TagProjectViewModel tagProjectViewModel2 && (tagProjectViewModel2.IsParent || tagProjectViewModel2.IsSubItem))
				{
					lineParent = (tagProjectViewModel2.IsParent ? tagProjectViewModel2.Title : tagProjectViewModel2.TagModel.parent);
				}
			}
			else
			{
				lineParent = subProjectViewModel.Project.groupId;
			}
		}
		else
		{
			lineParent = projectGroupViewModel.Id;
		}
		if (string.IsNullOrEmpty(lineParent))
		{
			return;
		}
		if (_lastHoverLine != null)
		{
			if (_lastHoverLine.ParentId != lineParent)
			{
				_lastHoverLine.Hover = false;
			}
			else if (_lastHoverLine.Hover)
			{
				return;
			}
		}
		if (ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p is EmptySubViewModel emptySubViewModel && emptySubViewModel.ParentId == lineParent) is EmptySubViewModel lastHoverLine)
		{
			_lastHoverLine = lastHoverLine;
			_lastHoverLine.Hover = true;
		}
	}

	private bool CanDropCreateGroup(ProjectItemViewModel model, ProjectItemViewModel target)
	{
		if (!(model is NormalProjectViewModel))
		{
			if (model is TagProjectViewModel { IsParent: false } tagProjectViewModel && _hoverIndex >= 0 && _hoverIndex < ProjectItems.Count && target is TagProjectViewModel { IsSubItem: false } tagProjectViewModel2)
			{
				if (tagProjectViewModel.InSubSection != tagProjectViewModel2.InSubSection)
				{
					return false;
				}
				if (!tagProjectViewModel2.IsParent)
				{
					return true;
				}
				if (tagProjectViewModel2.IsParent && !tagProjectViewModel2.Open)
				{
					OnGroupDragOver(tagProjectViewModel2);
					return false;
				}
			}
		}
		else if (_hoverIndex >= 0 && _hoverIndex < ProjectItems.Count)
		{
			if (!(target is NormalProjectViewModel))
			{
				if (target is ticktick_WPF.ViewModels.ProjectGroupViewModel { Open: false } projectGroupViewModel)
				{
					OnGroupDragOver(projectGroupViewModel);
					return false;
				}
			}
			else if (!(target is ticktick_WPF.ViewModels.SubProjectViewModel))
			{
				return true;
			}
		}
		return false;
	}

	public async void OnItemStartDrag(ProjectItemViewModel model, Point dragPoint)
	{
		Mouse.Capture(null);
		if (model != null && model.CanDrag)
		{
			_draggingModel = model;
			_draggingModel.DragSelected = true;
			if (_draggingModel is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel && projectGroupViewModel.ProjectGroup.open)
			{
				projectGroupViewModel.Icon = Utils.GetGeometry("Ic.ProjectList.Folder");
				projectGroupViewModel.ProjectGroup.open = false;
				ProjectGroupDao.TrySaveProjectGroup(projectGroupViewModel.ProjectGroup, isNeedUpdateOpen: true);
				model.Open = false;
				NotifyGroupOpenChanged(projectGroupViewModel);
			}
			if (_draggingModel is TagProjectViewModel { Open: not false } tagProjectViewModel)
			{
				tagProjectViewModel.Open = false;
				tagProjectViewModel.TagModel.collapsed = true;
				NotifyGroupOpenChanged(tagProjectViewModel);
			}
			if (_draggingModel is PtfAllViewModel ptf)
			{
				FoldAllPtfSection(ptf);
			}
			_isProjectDragging = true;
			_dragStartIndex = ProjectItems.IndexOf(_draggingModel);
			await TickDragHelper.StartDragItem(_projectList, _draggingModel, null, base.ActualWidth - 20.0, dragPoint);
			_isProjectDragging = false;
			await Task.Delay(120);
			if (_canReload)
			{
				Reload(refreshCount: false);
			}
		}
	}

	private void FoldAllPtfSection(PtfAllViewModel ptf)
	{
		List<ProjectItemViewModel> list = ProjectItems.ToList();
		list.RemoveAll(ProjectDataProvider.IsProjectCategory);
		list.RemoveAll(ProjectDataProvider.IsTagCategory);
		list.RemoveAll(ProjectDataProvider.IsFilterCategory);
		list.RemoveAll(ProjectDataProvider.IsSubscribeCategory);
		ptf.Open = false;
		switch (ptf.Type)
		{
		case PtfType.Project:
			LocalSettings.Settings.AllProjectOpened = false;
			break;
		case PtfType.Tag:
			LocalSettings.Settings.AllTagOpened = false;
			break;
		case PtfType.Filter:
			LocalSettings.Settings.AllFilterOpened = false;
			break;
		case PtfType.Subscribe:
			LocalSettings.Settings.AllSubscribeOpened = false;
			break;
		}
		ItemsSourceHelper.SetItemsSource(_projectList, list);
	}

	private void SwapItems(int index, ProjectItemViewModel model)
	{
		if (ProjectItems.Count > index)
		{
			ProjectItems.Remove(model);
			ProjectItems.Insert(index, model);
		}
	}

	private async void OnDrop()
	{
		_dragStatus = DragStatus.Waiting;
		bool needReload = false;
		PtfAllViewModel draggingPtf = null;
		try
		{
			if (_lastHoverLine != null)
			{
				_lastHoverLine.Hover = false;
				_lastHoverLine = null;
			}
			if (_draggingModel == null || _draggingModel.Id == _hoverModel?.Id)
			{
				ResetDrag();
				return;
			}
			if (_hoverModel != null && _hoverModel.DropSelected && _draggingModel != null && !(_draggingModel is PtfAllViewModel))
			{
				_draggingModel.DragSelected = false;
				if (_draggingModel is TagProjectViewModel dragTagProject && _hoverModel is TagProjectViewModel dropTagProject)
				{
					await TryCreatTagGroup(dragTagProject, dropTagProject);
					UtilLog.Info("ProjectMenu.DragDrop : newTag");
				}
				else if (_hoverModel is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel && _draggingModel is NormalProjectViewModel normalProjectViewModel)
				{
					if (_hoverModel is ClosedProjectGroupViewModel)
					{
						ResetDrag();
						return;
					}
					ProjectModel project = normalProjectViewModel.Project;
					if (project != null)
					{
						string text = $"originGid {project.groupId},originOrder {project.sortOrder}";
						project.groupId = projectGroupViewModel.Id;
						project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
						project.sortOrder = ProjectDragHelper.GetNewSortOrder(projectGroupViewModel.Id, project.teamId);
						UtilLog.Info($"ProjectMenu.DragDrop : project {project.id},group {projectGroupViewModel.Id},order {project.sortOrder},{text}");
						await ProjectDao.TryUpdateProject(project);
					}
					ProjectItems.Remove(_draggingModel);
					needReload = true;
				}
				else
				{
					needReload = !(await CreateNewProjectGroupOnDrop(_hoverModel));
				}
			}
			else if (_hoverModel != null)
			{
				if (_dragStartIndex == _currentIndex && !(_draggingModel is PtfAllViewModel))
				{
					_draggingModel.DragSelected = false;
					return;
				}
				ProjectItemViewModel draggingModel = _draggingModel;
				if (!(draggingModel is ticktick_WPF.ViewModels.SmartProjectViewModel))
				{
					if (draggingModel is FilterProjectViewModel filterProjectViewModel)
					{
						await SaveFilterSortOrder(_hoverModel, filterProjectViewModel);
					}
					else if (!(draggingModel is TagProjectViewModel tagProjectViewModel))
					{
						if (!(draggingModel is NormalProjectViewModel normal))
						{
							if (!(draggingModel is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel2))
							{
								if (!(draggingModel is PtfAllViewModel ptfAllViewModel))
								{
									if (draggingModel is SubscribeCalendarProjectViewModel || draggingModel is BindCalendarAccountProjectViewModel)
									{
										await SaveSubscribeSortOrder(_hoverModel, _draggingModel);
									}
								}
								else
								{
									draggingPtf = ptfAllViewModel;
									bool isTeam = draggingPtf.IsTeam;
									await SaveProjectSectionSortOrder(isTeam);
									needReload = true;
								}
							}
							else
							{
								int hoverIndex = ProjectItems.IndexOf(_hoverModel);
								await SaveProjectGroupSortOrder(_hoverModel, projectGroupViewModel2, hoverIndex);
								needReload = true;
							}
						}
						else
						{
							int num = ProjectItems.IndexOf(_hoverModel);
							ProjectItemViewModel hoverModel = _hoverModel;
							await SaveProjectSortOrder(num < _currentIndex, normal.Project, hoverModel);
							normal.IsSubItem = normal.Project?.InGroup() ?? false;
							needReload = true;
						}
					}
					else
					{
						if (_hoverModel.InSubSection == tagProjectViewModel.InSubSection)
						{
							if (tagProjectViewModel.IsParent)
							{
								await SortTagParent(tagProjectViewModel);
							}
							else
							{
								await SaveTagSortOrder(tagProjectViewModel, _hoverModel);
							}
						}
						needReload = true;
					}
				}
				else
				{
					SaveSmartProjectSortOrder(_hoverModel);
					needReload = true;
				}
			}
			_draggingModel.DragSelected = false;
			SyncManager.TryDelaySync(500);
		}
		catch (Exception)
		{
			needReload = true;
		}
		ProjectIdentity projectIdentity = SelectedItem?.GetIdentity();
		if (projectIdentity?.SortOption != null && ((_draggingModel is NormalProjectViewModel && projectIdentity.SortOption.ContainsSortType(Constants.SortType.project)) || (_draggingModel is TagProjectViewModel && projectIdentity.SortOption.ContainsSortType(Constants.SortType.tag))))
		{
			_listViewContainer?.OnProjectSelected();
		}
		ResetDrag();
		if (needReload)
		{
			await Task.Delay(10);
			ListViewContainer.ReloadProjectData();
		}
		if (draggingPtf != null)
		{
			ScrollToPtf(draggingPtf);
		}
	}

	private async void ScrollToPtf(PtfAllViewModel draggingPtf)
	{
		await Task.Delay(100);
		ProjectItemViewModel projectItemViewModel = ProjectItems.FirstOrDefault((ProjectItemViewModel p) => p is PtfAllViewModel ptfAllViewModel && ptfAllViewModel.Type == draggingPtf.Type);
		if (projectItemViewModel != null)
		{
			_projectList.ScrollIntoView(projectItemViewModel);
		}
	}

	private async Task SaveProjectSectionSortOrder(bool dragTeam)
	{
		List<PtfAllViewModel> list = ProjectItems.OfType<PtfAllViewModel>().ToList();
		string text = string.Empty;
		foreach (PtfAllViewModel item in list)
		{
			text += item.Title;
		}
		UtilLog.Info($"ProjectMenu.DragDrop : projectSection {text} , {dragTeam}");
		if (dragTeam)
		{
			list.RemoveAll((PtfAllViewModel p) => p.IsProject && !p.IsTeam);
		}
		else
		{
			list.RemoveAll((PtfAllViewModel p) => p.IsProject && p.IsTeam);
		}
		ProjectDataProvider.SetNewSortOrders(list);
	}

	private void ResetDrag()
	{
		_draggingModel = null;
		_dragStartY = -1L;
		_currentIndex = -1;
		_dragStartIndex = -1;
		_direction = DragDirection.Idle;
		_projectDragPopup.Child = null;
	}

	private async Task TryCreatTagGroup(TagProjectViewModel dragTagProject, TagProjectViewModel dropTagProject)
	{
		if (dropTagProject.IsSubItem)
		{
			return;
		}
		if (dropTagProject.IsParent)
		{
			if (!dropTagProject.Open)
			{
				dropTagProject.DropSelected = false;
				dragTagProject.TagModel.parent = dropTagProject.TagModel.name;
				dragTagProject.TagModel.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(upDown: true, dropTagProject.TagModel.name);
				dragTagProject.IsSubItem = true;
				ProjectItems.Remove(dragTagProject);
				dropTagProject.Children.Insert(0, dragTagProject);
				TagModel tagByName = CacheManager.GetTagByName(dragTagProject.TagModel.name);
				if (tagByName != null)
				{
					tagByName.parent = dragTagProject.TagModel.parent;
					tagByName.sortOrder = dragTagProject.TagModel.sortOrder;
					tagByName.status = ((tagByName.status != 0) ? 1 : 0);
					await TagDao.UpdateTag(tagByName);
				}
			}
			return;
		}
		TagModel tagParent = await TagDao.CreateTag("");
		if (tagParent != null)
		{
			tagParent.name = Utils.GetString("NewTag");
			int val = ProjectItems.IndexOf(dropTagProject);
			ProjectItems.Insert(Math.Min(val, _currentIndex), new TagProjectViewModel(tagParent)
			{
				IsParent = true,
				IsNew = true,
				InSubSection = dragTagProject.InSubSection
			});
			dropTagProject.IsSubItem = true;
			dropTagProject.DropSelected = false;
			dragTagProject.IsSubItem = true;
			tagParent.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(upDown: false, dropTagProject.TagModel.name);
			tagParent.sortType = Constants.SortType.tag.ToString();
			tagParent.SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.tag, inKanban: false);
			_canReload = false;
			tagParent.type = ((!dropTagProject.InSubSection) ? 1 : 2);
			base.Dispatcher.Invoke((Func<Task>)async delegate
			{
				await Task.Delay(10);
				tagParent.name = "";
				TryAddTagParent(tagParent, dropTagProject.TagModel, dragTagProject.TagModel);
			});
		}
	}

	private async Task SortTagParent(TagProjectViewModel tag)
	{
		List<TagModel> list = (from t in CacheManager.GetTags()?.Where((TagModel t) => t.name == tag.Id || t.parent == tag.Id)
			orderby t.sortOrder
			select t).ToList();
		if (list == null)
		{
			return;
		}
		long num = 268435456L / (long)list.Count;
		if (_hoverModel == null)
		{
			return;
		}
		ProjectItemViewModel projectItemViewModel = ProjectItems[_currentIndex - 1];
		if (projectItemViewModel is TagProjectViewModel || projectItemViewModel is EmptySubViewModel)
		{
			TagProjectViewModel tagProjectViewModel = ((projectItemViewModel is TagProjectViewModel tagProjectViewModel2) ? tagProjectViewModel2 : (ProjectItems[_currentIndex - 2] as TagProjectViewModel));
			if (tagProjectViewModel != null)
			{
				if (ProjectItems.Count > _currentIndex + 1 && ProjectItems[_currentIndex + 1] is TagProjectViewModel tagProjectViewModel3)
				{
					num = (tagProjectViewModel3.TagModel.sortOrder - tagProjectViewModel.TagModel.sortOrder) / (list.Count + 1);
				}
				for (int num2 = 0; num2 < list.Count; num2++)
				{
					string arg = $"originOrder {list[num2].sortOrder}";
					list[num2].sortOrder = tagProjectViewModel.TagModel.sortOrder + (num2 + 1) * num;
					if (list[num2].status != 0)
					{
						list[num2].status = 1;
					}
					if (string.IsNullOrEmpty(list[num2].parent) && list.Count > 1)
					{
						list[num2].collapsed = true;
					}
					UtilLog.Info($"ProjectMenu.DragDrop : tag {list[num2].name},order {list[num2].sortOrder},{arg}");
					TagDao.UpdateTag(list[num2]);
				}
				DataChangedNotifier.NotifyTagChanged(tag.TagModel);
				return;
			}
		}
		long tagMinSort = ProjectDragHelper.GetTagMinSort();
		for (int num3 = list.Count - 1; num3 >= 0; num3--)
		{
			string arg2 = $"originOrder {list[num3].sortOrder}";
			list[num3].sortOrder = tagMinSort - (list.Count - num3) * num;
			if (list[num3].status != 0)
			{
				list[num3].status = 1;
			}
			if (string.IsNullOrEmpty(list[num3].parent))
			{
				list[num3].collapsed = true;
			}
			UtilLog.Info($"ProjectMenu.DragDrop : tag {list[num3].name},order {list[num3].sortOrder},{arg2}");
			TagDao.UpdateTag(list[num3]);
		}
		DataChangedNotifier.NotifyTagChanged(tag.TagModel);
	}

	private void TryAddTagParent(TagModel tagParent, TagModel dropModel, TagModel dragModel)
	{
		AddOrEditTagWindow addOrEditTagWindow = new AddOrEditTagWindow(tagParent, canEditTitle: true, isCreat: true);
		addOrEditTagWindow.Owner = Window.GetWindow(this);
		addOrEditTagWindow.TagSaved += async delegate(object sender, TagModel tag)
		{
			TagModel tagByName = CacheManager.GetTagByName(dropModel.name);
			if (tagByName != null)
			{
				tagByName.parent = tag.name;
				tagByName.status = ((tagByName.status != 0) ? 1 : 0);
				await TagDao.UpdateTag(tagByName);
			}
			TagModel tagByName2 = CacheManager.GetTagByName(dragModel.name);
			if (tagByName2 != null)
			{
				tagByName2.parent = tag.name;
				tagByName2.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(upDown: true, dropModel.name);
				tagByName2.status = ((tagByName2.status != 0) ? 1 : 0);
				await TagDao.UpdateTag(tagByName2);
			}
			_canReload = true;
			ListViewContainer.ReloadProjectData();
			SyncManager.TryDelaySync();
		};
		addOrEditTagWindow.Cancel += async delegate
		{
			_canReload = true;
			LoadData();
		};
		addOrEditTagWindow.ShowDialog();
	}

	private async Task SaveProjectGroupSortOrder(ProjectItemViewModel dropItem, ticktick_WPF.ViewModels.ProjectGroupViewModel group, int hoverIndex)
	{
		ProjectGroupModel projectGroupModel = await ProjectGroupDao.GetProjectGroupById(group.ProjectGroup.id);
		if (projectGroupModel == null)
		{
			return;
		}
		string text = string.Empty;
		if (!(dropItem is NormalProjectViewModel normalProjectViewModel))
		{
			if (!(dropItem is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel))
			{
				if (dropItem is EmptySubViewModel emptySubViewModel)
				{
					text = emptySubViewModel.ParentId;
				}
			}
			else
			{
				text = projectGroupViewModel.ProjectGroup.id;
			}
		}
		else
		{
			text = normalProjectViewModel.Project.id;
		}
		if (!string.IsNullOrEmpty(text))
		{
			bool upDown = hoverIndex < _currentIndex;
			projectGroupModel.sortOrder = ProjectDragHelper.GetNewSortOrder(upDown, text, group.TeamId);
			if (projectGroupModel.sync_status != Constants.SyncStatus.SYNC_NEW.ToString())
			{
				projectGroupModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			}
			await ProjectGroupDao.TrySaveProjectGroup(projectGroupModel);
		}
	}

	private async Task SaveProjectSortOrder(bool upDown, ProjectModel project, ProjectItemViewModel target)
	{
		project = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == project.id) ?? project;
		string origin = $"originGid {project.groupId},originOrder {project.sortOrder}";
		if (!(target is ticktick_WPF.ViewModels.SubProjectViewModel subProjectViewModel))
		{
			if (!(target is NormalProjectViewModel normalProjectViewModel))
			{
				if (!(target is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel))
				{
					if (target is EmptySubViewModel emptySubViewModel)
					{
						project.groupId = (upDown ? "NONE" : emptySubViewModel.ProjectGroup.id);
						project.sortOrder = ProjectDragHelper.GetNewSortOrder(upDown, emptySubViewModel.ProjectGroup.id, isGroupAll: true, emptySubViewModel.TeamId);
						project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
						await ProjectDao.TryUpdateProject(project);
					}
				}
				else
				{
					string oldGroupId = project.groupId;
					if (projectGroupViewModel.Open)
					{
						if (upDown)
						{
							project.groupId = projectGroupViewModel.ProjectGroup.id;
							project.sortOrder = ProjectDragHelper.GetNewSortOrder(upDown: true, projectGroupViewModel.ProjectGroup.id, isGroupAll: false, projectGroupViewModel.TeamId);
							project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
						}
						else
						{
							project.groupId = "NONE";
							project.sortOrder = ProjectDragHelper.GetNewSortOrder(upDown: false, projectGroupViewModel.ProjectGroup.id, isGroupAll: false, projectGroupViewModel.TeamId);
							project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
						}
					}
					else
					{
						project.groupId = "NONE";
						project.sortOrder = ProjectDragHelper.GetNewSortOrder(upDown, projectGroupViewModel.Id, projectGroupViewModel.TeamId);
						project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
					}
					await ProjectDao.TryUpdateProject(project);
					if (oldGroupId != project.groupId && project.IsShareList())
					{
						ProjectGroupDao.CheckGroupGroupBy(oldGroupId);
					}
				}
			}
			else
			{
				project.groupId = "NONE";
				project.sortOrder = ProjectDragHelper.GetNewSortOrder(upDown, normalProjectViewModel.Id, normalProjectViewModel.TeamId);
				project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				await ProjectDao.TryUpdateProject(project);
			}
		}
		else
		{
			long sortOrder = project.sortOrder;
			string oldGroupId = project.groupId;
			if (subProjectViewModel.Project.closed == true)
			{
				project.sortOrder = ProjectDragHelper.GetNewSortOrderInClosed(upDown, subProjectViewModel.Project.id, subProjectViewModel.TeamId) ?? project.sortOrder;
			}
			else
			{
				project.groupId = subProjectViewModel.Project.groupId;
				project.sortOrder = ProjectDragHelper.GetNewSortOrder(upDown, subProjectViewModel.Project.groupId, subProjectViewModel.Project.id, subProjectViewModel.TeamId);
			}
			if (sortOrder != project.sortOrder || oldGroupId != project.groupId)
			{
				project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				await ProjectDao.TryUpdateProject(project);
			}
			if (oldGroupId != project.groupId && project.IsShareList())
			{
				ProjectGroupDao.CheckGroupGroupBy(oldGroupId);
			}
		}
		UtilLog.Info($"ProjectMenu.DragDrop : projectSort {project.id},group {project.groupId},order {project.sortOrder},{origin}");
	}

	private async Task SaveTagSortOrder(TagProjectViewModel tag, ProjectItemViewModel target)
	{
		if (tag.TagModel.status != 0)
		{
			tag.TagModel.status = 1;
		}
		string originParent = tag.TagModel.parent;
		int num = ProjectItems.IndexOf(target);
		string text = $"originParent {tag.TagModel.parent},originOrder {tag.TagModel.sortOrder}";
		if (!(target is EmptySubViewModel emptySubViewModel))
		{
			if (target is TagProjectViewModel tagProjectViewModel)
			{
				if (tagProjectViewModel.IsParent)
				{
					tag.TagModel.parent = ((num > _currentIndex) ? "" : tagProjectViewModel.TagModel.name);
					tag.IsSubItem = num < _currentIndex;
					tag.TagModel.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(num < _currentIndex, tagProjectViewModel.TagModel.name);
				}
				else
				{
					tag.IsSubItem = tagProjectViewModel.IsSubItem;
					tag.TagModel.parent = (string.IsNullOrEmpty(tagProjectViewModel.TagModel.parent) ? "" : tagProjectViewModel.TagModel.parent);
					tag.TagModel.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(num < _currentIndex, tagProjectViewModel.TagModel.name);
				}
			}
		}
		else if (num > _currentIndex)
		{
			if (ProjectItems[_currentIndex - 1] is TagProjectViewModel tagProjectViewModel2)
			{
				tag.IsSubItem = true;
				tag.TagModel.parent = emptySubViewModel.ParentId;
				tag.TagModel.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(upDown: true, tagProjectViewModel2.TagModel.name);
			}
		}
		else if (num < _currentIndex && ProjectItems[_currentIndex - 2] is TagProjectViewModel tagProjectViewModel3)
		{
			tag.IsSubItem = false;
			tag.TagModel.parent = "";
			tag.TagModel.sortOrder = ProjectDragHelper.CalculateInsertTagSortOrder(upDown: true, tagProjectViewModel3.TagModel.name);
		}
		UtilLog.Info($"ProjectMenu.DragDrop : tag {tag.Id},parent{tag.TagModel.parent},order {tag.TagModel.sortOrder},{text}");
		await TagDao.UpdateTag(tag.TagModel);
		if (!string.IsNullOrEmpty(originParent) && originParent != tag.TagModel.parent)
		{
			TagDao.CheckParent(originParent);
		}
		DataChangedNotifier.NotifyTagChanged(tag.TagModel);
	}

	private async Task SaveFilterSortOrder(ProjectItemViewModel dropItem, FilterProjectViewModel filterProjectViewModel)
	{
		if (dropItem is FilterProjectViewModel filterProjectViewModel2)
		{
			int num = ProjectItems.IndexOf(filterProjectViewModel2);
			FilterModel filter = filterProjectViewModel.Filter;
			long sortOrder = filter.sortOrder;
			filter.sortOrder = ProjectDragHelper.CalculateInsertFilterSortOrder(num < _currentIndex, filterProjectViewModel2.Filter.id);
			UtilLog.Info($"ProjectMenu.DragDrop : filter {filter.id},order {filter.sortOrder},originOrder {sortOrder}");
			if (filter.syncStatus != 0)
			{
				filter.syncStatus = 1;
			}
			await FilterDao.UpdateFilter(filter);
		}
	}

	private async Task SaveSubscribeSortOrder(ProjectItemViewModel dropItem, ProjectItemViewModel dragItem)
	{
		List<CalendarSubscribeProfileModel> subscribeCalendars = CacheManager.GetSubscribeCalendars();
		List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
		if (subscribeCalendars.All((CalendarSubscribeProfileModel x) => !x.Order.HasValue || x.Order == 0) && bindCalendarAccounts.All((BindCalendarAccountModel x) => !x.Order.HasValue || x.Order == 0))
		{
			List<ProjectItemViewModel> list = ProjectItems.Where((ProjectItemViewModel x) => x is SubscribeCalendarProjectViewModel || x is BindCalendarAccountProjectViewModel).ToList();
			long num = 0L;
			Dictionary<string, long> dict = new Dictionary<string, long>();
			for (int num2 = list.Count - 1; num2 >= 0; num2--)
			{
				num -= 268435456;
				list[num2].SortOrder = num;
				dict[list[num2].Id] = num;
			}
			await CalendarSubscribeProfileDao.SaveOrders(dict);
			await BindCalendarAccountDao.SaveOrders(dict);
			return;
		}
		int num3 = -1;
		string text = string.Empty;
		if (!(dropItem is SubscribeCalendarProjectViewModel subscribeCalendarProjectViewModel))
		{
			if (dropItem is BindCalendarAccountProjectViewModel bindCalendarAccountProjectViewModel)
			{
				num3 = ProjectItems.IndexOf(bindCalendarAccountProjectViewModel);
				text = bindCalendarAccountProjectViewModel.Account.Id;
			}
		}
		else
		{
			num3 = ProjectItems.IndexOf(subscribeCalendarProjectViewModel);
			text = subscribeCalendarProjectViewModel.Profile.Id;
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (!(dragItem is SubscribeCalendarProjectViewModel { Profile: var profile }))
		{
			if (dragItem is BindCalendarAccountProjectViewModel { Account: var account })
			{
				long? order = account.Order;
				account.Order = ProjectDragHelper.CalculateInsertSubscribeSortOrder(num3 < _currentIndex, text);
				UtilLog.Info($"ProjectMenu.DragDrop : bindaccount {account.Id},order {account.Order},originOrder {order}");
				BindCalendarAccountDao.SaveOrder(account.Id, account.Order.Value);
			}
		}
		else
		{
			long? order2 = profile.Order;
			profile.Order = ProjectDragHelper.CalculateInsertSubscribeSortOrder(num3 < _currentIndex, text);
			UtilLog.Info($"ProjectMenu.DragDrop : subscribe {profile.Id},order {profile.Order},originOrder {order2}");
			CalendarSubscribeProfileDao.SaveOrder(profile.Id, profile.Order.Value);
		}
	}

	private void SaveSmartProjectSortOrder(ProjectItemViewModel dropItem)
	{
		if (!(dropItem is ticktick_WPF.ViewModels.SmartProjectViewModel))
		{
			return;
		}
		for (int i = 0; i < ProjectItems.Count; i++)
		{
			if (ProjectItems[i] is ticktick_WPF.ViewModels.SmartProjectViewModel smartProjectViewModel)
			{
				smartProjectViewModel.Project.SortOrder = i * 1000;
			}
		}
	}

	private async Task<bool> CreateNewProjectGroupOnDrop(ProjectItemViewModel targetModel)
	{
		ProjectGroupModel projectGroup = await CreateNewProjectGroup(targetModel);
		if (projectGroup != null)
		{
			int val = Math.Min(_hoverIndex, _currentIndex);
			val = Math.Min(ProjectItems.Count, val);
			ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel = new ticktick_WPF.ViewModels.ProjectGroupViewModel(projectGroup)
			{
				IsNew = true
			};
			ProjectItems.Insert(val, projectGroupViewModel);
			bool result = await DowngradeProject(val, projectGroup, projectGroupViewModel);
			UserActCollectUtils.AddClickEvent("project_list_ui", "folder", "add");
			OnRenameGroup(projectGroup);
			return result;
		}
		return true;
	}

	private async Task<bool> DowngradeProject(int index, ProjectGroupModel projectGroup, ticktick_WPF.ViewModels.ProjectGroupViewModel group)
	{
		if (ProjectItems.Count > index + 2)
		{
			ProjectItemViewModel projectItemViewModel = ProjectItems[index + 1];
			ProjectItemViewModel projectItemViewModel2 = ProjectItems[index + 2];
			if (projectItemViewModel is NormalProjectViewModel normalProjectViewModel && projectItemViewModel2 is NormalProjectViewModel normalProjectViewModel2)
			{
				ProjectModel project = normalProjectViewModel.Project;
				ProjectModel nextProject = normalProjectViewModel2.Project;
				ProjectItems[index + 1] = new ticktick_WPF.ViewModels.SubProjectViewModel(project);
				ProjectItems[index + 2] = new ticktick_WPF.ViewModels.SubProjectViewModel(nextProject);
				group.Children.Add(ProjectItems[index + 1]);
				group.Children.Add(ProjectItems[index + 2]);
				if (project != null)
				{
					project.groupId = projectGroup.id;
					project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
					await ProjectDao.TryUpdateProject(project);
				}
				if (nextProject != null)
				{
					nextProject.groupId = projectGroup.id;
					nextProject.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
					await ProjectDao.TryUpdateProject(nextProject);
				}
				return true;
			}
		}
		return false;
	}

	private async Task<ProjectGroupModel> CreateNewProjectGroup(ProjectItemViewModel targetModel)
	{
		if (targetModel == null)
		{
			return null;
		}
		string identity = string.Empty;
		string teamId = string.Empty;
		if (!(targetModel is ticktick_WPF.ViewModels.ProjectGroupViewModel projectGroupViewModel))
		{
			if (targetModel is NormalProjectViewModel normalProjectViewModel)
			{
				identity = normalProjectViewModel.Project.id;
				teamId = normalProjectViewModel.Project.teamId;
			}
		}
		else
		{
			identity = projectGroupViewModel.ProjectGroup.id;
			teamId = projectGroupViewModel.ProjectGroup.teamId;
		}
		long value = ProjectDragHelper.CalculateInsertSortOrder(upDown: false, identity, teamId);
		ProjectGroupModel projectGroup = new ProjectGroupModel
		{
			id = Utils.GetGuid(),
			name = Utils.GetString("NewFolder"),
			open = true,
			teamId = teamId,
			sync_status = Constants.SyncStatus.SYNC_NEW.ToString(),
			userId = int.Parse(LocalSettings.Settings.LoginUserId),
			sortOrder = value,
			sortType = "project",
			SortOption = new SortOption
			{
				groupBy = "project",
				orderBy = "dueDate"
			}
		};
		await ProjectGroupDao.TrySaveProjectGroup(projectGroup);
		return projectGroup;
	}

	private void OnDragDrop(object sender, MouseEventArgs e)
	{
		if (_dragOver)
		{
			OnMouseDrop();
			_dragOver = false;
		}
		_miniCalendar.OnDrop(e);
	}

	private void OnDragOver(object sender, MouseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		Window window = Window.GetWindow(this);
		if (window == null || !window.IsMouseCaptured || TaskDetailPopup.IsDragOver(e))
		{
			return;
		}
		Point position = e.GetPosition(this);
		bool flag = !(position.X < 0.0) && !(position.X > base.ActualWidth) && !(position.Y < 0.0) && !(position.Y > base.ActualHeight);
		if (_miniCalendar.IsVisible)
		{
			Point position2 = e.GetPosition(_miniCalendar);
			bool flag2 = !(position2.X < 0.0) && !(position2.X > _miniCalendar.ActualWidth) && !(position2.Y < 0.0) && !(position2.Y > _miniCalendar.ActualHeight);
			if (flag2)
			{
				flag = false;
			}
			_miniCalendar.OnDragOver(flag2, e);
		}
		if (_dragOver != flag)
		{
			if (flag)
			{
				OnMouseDragEnter();
			}
			else
			{
				OnMouseDragLeave();
			}
			_dragOver = flag;
		}
		if (flag)
		{
			OnMouseDragOver(e);
		}
	}

	public void OnTaskDragging(DragMouseEvent position)
	{
	}

	private async Task OnGroupDragOver(ProjectItemViewModel groupVm)
	{
		if (groupVm == _previousOverGroup)
		{
			return;
		}
		_previousOverGroup = groupVm;
		await Task.Delay(240);
		if (groupVm == _previousOverGroup)
		{
			_previousOverGroup = null;
			if (!groupVm.Open)
			{
				groupVm.Open = true;
				OnOpenGroupClick(groupVm);
			}
		}
	}

	private IDroppable GetDroppable()
	{
		if (_lastDropIndex >= 0 && _lastDropIndex < ProjectItems.Count && ProjectItems[_lastDropIndex].DropSelected)
		{
			ProjectItems[_lastDropIndex].DropSelected = false;
			if (ProjectItems[_lastDropIndex] is IDroppable { CanDrop: not false } droppable)
			{
				return droppable;
			}
		}
		return null;
	}

	private int GetDropProjectIndex(Point point)
	{
		if (Utils.GetMousePointItem<ListViewItem>(point, _projectList)?.DataContext is ProjectItemViewModel item)
		{
			return ProjectItems.IndexOf(item);
		}
		return -1;
	}

	public async Task<bool> OnBatchTaskDropped(List<string> topTaskIds, List<string> selectedTaskIds)
	{
		IDroppable droppable = GetDroppable();
		if (droppable != null)
		{
			Utils.FindParent<ListViewContainer>(this)?.HideDetail();
			if (!string.IsNullOrEmpty(droppable.ProjectId))
			{
				ProjectModel project = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == droppable.ProjectId);
				if (project != null)
				{
					List<TaskBaseViewModel> tasks = TaskCache.GetTaskAndChildrenInBatch(topTaskIds);
					await TaskDao.RemoveTaskParentIdInBatch((from node in TaskNodeUtils.GetTaskNodeTree(tasks)
						where !node.HasParent
						select node.Value.Id).ToList());
					List<string> ids = tasks.Select((TaskBaseViewModel t) => t.Id).ToList();
					List<ColumnModel> list = await ColumnDao.GetColumnsByProjectId(project.id);
					if (list == null || !list.Any())
					{
						await ColumnDao.CheckProjectColumns(project.id);
						list = await ColumnDao.GetColumnsByProjectId(project.id);
					}
					string columnId = list?.OrderBy((ColumnModel c) => c.sortOrder).FirstOrDefault()?.id ?? string.Empty;
					await TaskService.BatchMoveProject(ids, new MoveProjectArgs(project)
					{
						ColumnId = columnId
					});
					TaskModel task = await TaskDao.GetThinTaskById(ids.FirstOrDefault());
					TaskService.TryToastMoveControl(SelectedItem.GetIdentity(), task, project.id, hideTitle: true);
				}
			}
			if (droppable.Priority != 0)
			{
				await TaskService.BatchSetPriority(selectedTaskIds, droppable.Priority);
			}
			if (droppable.DefaultDate.HasValue)
			{
				foreach (string taskId in selectedTaskIds)
				{
					bool hasStartDate = TaskCache.GetTaskById(taskId)?.StartDate.HasValue ?? false;
					await TaskService.SetDate(taskId, droppable.DefaultDate.Value, handleReminder: false);
					if (!hasStartDate)
					{
						await TaskService.SaveTaskReminders(new TaskModel
						{
							id = taskId,
							reminders = TimeData.GetDefaultAllDayReminders().ToArray()
						});
					}
				}
				if (selectedTaskIds != null && selectedTaskIds.Count > 0 && droppable is ticktick_WPF.ViewModels.SmartProjectViewModel smart && (smart.Id == "_special_id_tomorrow" || smart.Id == "_special_id_today"))
				{
					TaskModel task2 = await TaskDao.GetThinTaskById(selectedTaskIds[0]);
					App.Window.TryToastMoveControl(task2, smart.Id == "_special_id_today", selectedTaskIds.Count > 1);
				}
			}
			if (droppable.Tags != null && droppable.Tags.Any())
			{
				await TaskService.BatchAddTag(selectedTaskIds, droppable.Tags[0]);
			}
			if (droppable.IsCompleted)
			{
				await TaskService.BatchCompleteTasks(topTaskIds);
			}
			if (droppable.IsAbandoned)
			{
				await TaskService.BatchAbandonTasks(topTaskIds);
			}
			if (droppable.IsDeleted)
			{
				await TaskService.BatchDeleteTaskByIds(topTaskIds, showUndo: false);
			}
			return true;
		}
		return false;
	}

	public IDroppable GetTaskDropTarget()
	{
		return GetDroppable();
	}

	public async Task<bool> OnDragTaskDropped(string taskId)
	{
		if (base.Visibility != Visibility.Visible)
		{
			return false;
		}
		IDroppable droppable = GetDroppable();
		if (droppable != null && droppable.CanDrop)
		{
			Utils.FindParent<ListViewContainer>(this)?.HideDetail();
			TaskModel task = await TaskDao.GetThinTaskById(taskId);
			if (task != null)
			{
				if (droppable.Multiple)
				{
					await SetTaskMultiProperty(task, droppable);
				}
				else
				{
					if (droppable.Tags != null && droppable.Tags.Any())
					{
						Utils.Toast(string.Format(Utils.GetString("TaskAddedTag"), "\"" + TagDataHelper.GetTagDisplayName(droppable.Tags[0]) + "\""));
					}
					else if (!string.IsNullOrEmpty(droppable.ProjectId))
					{
						ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == droppable.ProjectId);
						if (projectModel != null && task.projectId == projectModel.id && (droppable is NormalProjectViewModel || droppable is ticktick_WPF.ViewModels.InboxProjectViewModel))
						{
							return false;
						}
					}
					await TaskDragHelper.SetTaskProperty(task, droppable, Utils.FindParent<ListViewContainer>(this));
				}
				return !string.IsNullOrEmpty(droppable.ProjectId) && droppable.ProjectId != task.projectId;
			}
		}
		return false;
	}

	private static async Task SetTaskMultiProperty(TaskModel task, IDroppable droppable)
	{
		string projectId = task.projectId;
		task.projectId = droppable.ProjectId;
		task.priority = droppable.Priority;
		task.tags = droppable.Tags.ToArray();
		if (droppable.DefaultDate.HasValue)
		{
			task.startDate = droppable.DefaultDate.Value;
			task.reminders = TimeData.GetDefaultAllDayReminders().ToArray();
			task.isAllDay = true;
			task.dueDate = null;
		}
		await TaskService.SetTaskProperties(task, projectId);
	}

	private async void OnCheckItemDrop(string checkItemId)
	{
		IDroppable droppable = GetDroppable();
		if (droppable == null || !droppable.CanDrop)
		{
			return;
		}
		TaskDetailItemModel subTask = await TaskDetailItemDao.GetChecklistItemById(checkItemId);
		if (subTask == null)
		{
			return;
		}
		if (droppable.IsCompleted)
		{
			await TaskDetailItemService.CompleteCheckItem(checkItemId);
			return;
		}
		if (droppable.IsDeleted)
		{
			await TaskService.DeleteCheckItem(checkItemId);
			return;
		}
		ProjectIdentity identity = ProjectItems[_lastDropIndex].GetIdentity();
		TaskPrimaryProperty dropProperty = SubtaskToTaskHelper.GetProjectDefaultProperty(identity);
		TaskModel primaryTask = await TaskDao.GetTaskById(subTask.TaskServerId);
		TimeData timeData = new TimeData();
		if (!Utils.IsEmptyDate(subTask.startDate))
		{
			timeData.StartDate = subTask.startDate;
			timeData.IsAllDay = subTask.isAllDay;
			timeData.RepeatFlag = primaryTask.repeatFlag;
			timeData.RepeatFrom = primaryTask.repeatFrom;
			if (subTask.isAllDay.HasValue && subTask.isAllDay.Value)
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
		TaskPrimaryProperty taskProperty = new TaskPrimaryProperty
		{
			Priority = primaryTask.priority,
			ProjectId = primaryTask.projectId,
			Tags = TagSerializer.ToTags(primaryTask.tag),
			TimeData = timeData
		};
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		TaskPrimaryProperty defaultProperty = new TaskPrimaryProperty
		{
			Priority = defaultSafely.Priority,
			ProjectId = Utils.GetInboxId(),
			TimeData = TimeData.BuildFromDefault(defaultSafely)
		};
		TaskPrimaryProperty property = await SubtaskToTaskHelper.MergeTaskProperty(taskProperty, defaultProperty, dropProperty);
		if (!(await ProChecker.CheckTaskLimit(property.ProjectId)))
		{
			await TaskService.SubtaskToTask(checkItemId, property);
		}
	}

	public async Task<bool> OnDragSectionDropped(string dragSectionId, string projectId)
	{
		if (base.Visibility != Visibility.Visible)
		{
			return false;
		}
		IDroppable droppable = GetDroppable();
		if (droppable != null && droppable.CanDrop && droppable is NormalProjectViewModel normalProjectViewModel)
		{
			if (projectId == normalProjectViewModel.Id)
			{
				return false;
			}
			return await TaskService.MoveColumnAsync(dragSectionId, projectId, normalProjectViewModel.Id, null, isSection: true);
		}
		return false;
	}

	private void OnMouseDragOver(MouseEventArgs e)
	{
		object dragData = TickDragHelper.GetDragData();
		if (dragData is BaseViewModel)
		{
			if (_autoScroller == null)
			{
				_autoScroller = new DragAutoScrollHelper(_projectList);
			}
			if (_autoScroller.TryAutoScroll(e))
			{
				return;
			}
		}
		object obj = dragData;
		if (!(obj is DisplayItemModel displayItemModel))
		{
			if (!(obj is CalendarDisplayViewModel calendarDisplayViewModel))
			{
				if (!(obj is TimelineCellViewModel timelineCellViewModel))
				{
					if (!(obj is CheckItemViewModel viewModel))
					{
						if (obj is ProjectItemViewModel model && _isProjectDragging)
						{
							TryMoveItem(model, e);
						}
					}
					else
					{
						SetTaskDragTarget(viewModel, e);
					}
				}
				else if (timelineCellViewModel.DisplayModel.IsTaskOrNote && timelineCellViewModel.DisplayModel.Editable)
				{
					SetTaskDragTarget(timelineCellViewModel, e);
				}
			}
			else if (calendarDisplayViewModel.IsTaskOrNote && calendarDisplayViewModel.SourceViewModel.Editable)
			{
				SetTaskDragTarget(calendarDisplayViewModel, e);
			}
		}
		else if ((displayItemModel.IsTaskOrNote || displayItemModel.IsItem) && displayItemModel.Enable)
		{
			SetTaskDragTarget(displayItemModel, e);
		}
	}

	private void SetTaskDragTarget(object viewModel, MouseEventArgs e)
	{
		Point position = e.GetPosition(_projectList);
		ProjectItemViewModel projectItemViewModel = null;
		if (viewModel != null)
		{
			int dropProjectIndex = GetDropProjectIndex(position);
			if (dropProjectIndex >= 0 && dropProjectIndex < ProjectItems.Count)
			{
				projectItemViewModel = ProjectItems[dropProjectIndex];
			}
			if (dropProjectIndex != _lastDropIndex)
			{
				if (_lastDropIndex >= 0 && _lastDropIndex < ProjectItems.Count)
				{
					ProjectItems[_lastDropIndex].DropSelected = false;
				}
				if (projectItemViewModel != null && projectItemViewModel.CanDrop)
				{
					if (viewModel is DisplayItemModel { IsSection: not false } && !(projectItemViewModel is NormalProjectViewModel))
					{
						_lastDropIndex = -1;
						return;
					}
					projectItemViewModel.DropSelected = true;
				}
				_lastDropIndex = dropProjectIndex;
			}
		}
		else if (_lastDropIndex >= 0 && _lastDropIndex < ProjectItems.Count)
		{
			ProjectItems[_lastDropIndex].DropSelected = false;
			_lastDropIndex = -1;
		}
		if (projectItemViewModel is ticktick_WPF.ViewModels.ProjectGroupViewModel { Open: false } projectGroupViewModel)
		{
			OnGroupDragOver(projectGroupViewModel);
		}
		else
		{
			_previousOverGroup = null;
		}
	}

	public void OnMouseDragLeave()
	{
		ClearDropSelected();
		_lastDropIndex = -1;
		_autoScroller?.SetAutoScrolling(autoScroll: false);
	}

	private void OnMouseDrop()
	{
		_autoScroller?.SetAutoScrolling(autoScroll: false);
		object dragData = TickDragHelper.GetDragData();
		if (!(dragData is DisplayItemModel displayItemModel))
		{
			if (!(dragData is CheckItemViewModel checkItemViewModel))
			{
				if (!(dragData is CalendarDisplayViewModel calendarDisplayViewModel))
				{
					if (!(dragData is TimelineCellViewModel timelineCellViewModel))
					{
						if (dragData is ProjectItemViewModel && _isProjectDragging)
						{
							OnDrop();
						}
						return;
					}
					if (timelineCellViewModel.BatchSelected)
					{
						HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
						if (draggingTaskIds != null && draggingTaskIds.Count > 1)
						{
							List<string> list = TickDragHelper.DraggingTaskIds.ToList();
							List<string> treeTopIds = TaskDao.GetTreeTopIds(list, null);
							OnBatchTaskDropped(treeTopIds, list);
							return;
						}
					}
					OnDragTaskDropped(timelineCellViewModel.Id);
				}
				else if (calendarDisplayViewModel.BatchSelected)
				{
					List<string> list2 = TickDragHelper.DraggingTaskIds.ToList();
					List<string> treeTopIds2 = TaskDao.GetTreeTopIds(list2, null);
					OnBatchTaskDropped(treeTopIds2, list2);
				}
				else
				{
					OnDragTaskDropped(calendarDisplayViewModel.TaskId);
				}
			}
			else
			{
				OnCheckItemDrop(checkItemViewModel.Id);
			}
		}
		else if (displayItemModel.BatchSelectedIds != null && displayItemModel.BatchSelectedIds.Any())
		{
			OnBatchTaskDropped(displayItemModel.BatchModels?.Select((DisplayItemModel b) => b.Id).ToList() ?? new List<string>(), displayItemModel.BatchSelectedIds);
		}
		else
		{
			OnDragTaskDropped(displayItemModel.Id);
		}
	}

	public void OnMouseDragEnter()
	{
		ListViewContainer listViewContainer = GetListViewContainer();
		if (listViewContainer == null)
		{
			return;
		}
		double itemWidth = listViewContainer.GetItemWidth();
		object dragData = TickDragHelper.GetDragData();
		if (!(dragData is DisplayItemModel))
		{
			if (!(dragData is TimelineCellViewModel timelineCellViewModel))
			{
				if (dragData is CalendarDisplayViewModel)
				{
					string viewMode = listViewContainer.GetTaskView().GetViewMode();
					if (viewMode == "list")
					{
						TickDragHelper.ChangeDragContent(0, itemWidth);
					}
					if (viewMode == "kanban")
					{
						TickDragHelper.ChangeDragContent(1, itemWidth);
					}
				}
			}
			else
			{
				timelineCellViewModel.SetWidth();
				TickDragHelper.ChangeDragContent(timelineCellViewModel, timelineCellViewModel.Width);
			}
		}
		else
		{
			TickDragHelper.ChangeDragContent(dragData, itemWidth);
		}
	}
}
