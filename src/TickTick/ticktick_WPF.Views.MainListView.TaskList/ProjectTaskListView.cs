using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Completed;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Print;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.TaskList.Item;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.MainListView.TaskList;

public class ProjectTaskListView : Grid, IBatchEditable, IBatchTaskSelector
{
	private Border _trashTitle = new Border
	{
		Margin = new Thickness(28.0, 0.0, 0.0, 0.0),
		VerticalAlignment = VerticalAlignment.Top,
		HorizontalAlignment = HorizontalAlignment.Stretch
	};

	private bool _inOneColumn;

	private bool _canHoverShow = true;

	private SearchFilterControl _searchFilterControl;

	private CompletedFilterControl _completeFilterControl;

	private CompletedFilterControl _abandonFilterControl;

	private readonly DelayActionHandler _delayLoadHandler = new DelayActionHandler(200);

	private BatchTaskEditHelper _batchHelper;

	private TaskView _parent;

	private BatchTaskControl _batchTaskControl;

	private Border _menuFoldBorder = new Border
	{
		Cursor = Cursors.Hand,
		Margin = new Thickness(16.0, 32.0, 0.0, 12.0),
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private Border _topBorder = new Border
	{
		Margin = new Thickness(14.0, 26.0, 0.0, 12.0),
		VerticalAlignment = VerticalAlignment.Top,
		HorizontalAlignment = HorizontalAlignment.Left
	};

	private Border _titleBorder = new Border
	{
		VerticalAlignment = VerticalAlignment.Top,
		HorizontalAlignment = HorizontalAlignment.Left
	};

	private StackPanel _titlePanel = new StackPanel
	{
		Margin = new Thickness(0.0, 0.0, 0.0, 0.0),
		Orientation = Orientation.Horizontal
	};

	private EmojiTitleEditor _title = new EmojiTitleEditor
	{
		Margin = new Thickness(24.0, 0.0, 0.0, 0.0)
	};

	private ContentControl _addTaskContent = new ContentControl
	{
		Margin = new Thickness(0.0, 4.0, 0.0, 8.0)
	};

	private ContentControl _listContent = new ContentControl();

	private StackPanel _listEmpty = new StackPanel
	{
		VerticalAlignment = VerticalAlignment.Top,
		Margin = new Thickness(0.0, 200.0, 0.0, 0.0)
	};

	private EmptyView _emptyView;

	private TaskListView _taskList = new TaskListView();

	private HoverIconButton _shareButton;

	private HoverIconButton _aiRecommendButton = new HoverIconButton
	{
		HorizontalAlignment = HorizontalAlignment.Right,
		VerticalAlignment = VerticalAlignment.Top,
		Margin = new Thickness(0.0, 31.0, 84.0, 12.0),
		ToolTip = Utils.GetString("AISuggestedTasks")
	};

	private HoverIconButton _orderButton = new HoverIconButton
	{
		HorizontalAlignment = HorizontalAlignment.Right,
		VerticalAlignment = VerticalAlignment.Top,
		Margin = new Thickness(0.0, 31.0, 52.0, 12.0),
		ToolTip = Utils.GetString("GroupAndSort"),
		ImageWidth = 20.0
	};

	private HoverIconButton _moreButton = new HoverIconButton
	{
		HorizontalAlignment = HorizontalAlignment.Right,
		VerticalAlignment = VerticalAlignment.Top,
		Margin = new Thickness(0.0, 31.0, 20.0, 12.0),
		ImageWidth = 20.0
	};

	private string _emptyShow;

	private bool _hideFoldMenu;

	private bool _titleBorderMouseDown;

	private bool _isPersonTrash => (ViewModel.ProjectIdentity as TrashProjectIdentity)?.IsPerson ?? true;

	public Border MenuPathBorder => _menuFoldBorder;

	public TaskListViewModel ViewModel => (base.DataContext as TaskListViewModel) ?? new TaskListViewModel();

	private EscPopup OrderPopup { get; set; }

	private EscPopup MorePopup { get; set; }

	public event TaskSelectDelegate TaskSelect;

	public event EventHandler<List<string>> BatchTaskChanged;

	public event EventHandler<TaskModel> TaskAdded;

	public event EventHandler<string> EventAdded;

	public event EventHandler<(List<string>, bool)> BatchTaskSelected;

	public event EventHandler OnSearchFilterChanged;

	public event EventHandler<string> OnProjectActivityClick;

	public void SwitchTitle(bool isTrash)
	{
		if (isTrash)
		{
			_titleBorder.Child = _trashTitle;
			if (_trashTitle.Child == null || (_trashTitle.Child is StackPanel stackPanel && stackPanel.Children.Count == 0))
			{
				InitTrashTitle();
			}
			SetTrashMode();
		}
		else
		{
			_titleBorder.Child = _title;
		}
		SetModeColumn(_inOneColumn);
	}

	private void SetTrashMode()
	{
		bool flag = UserManager.IsTeamUser();
		_trashTitle.IsHitTestVisible = flag || !_inOneColumn;
		if (!(_trashTitle.Child is StackPanel stackPanel))
		{
			return;
		}
		foreach (object child in stackPanel.Children)
		{
			if (!(child is Path path))
			{
				if (child is TextBlock textBlock)
				{
					textBlock.Text = Utils.GetString((!flag) ? "Trash" : (_isPersonTrash ? "PersonalTrash" : "TeamTrash"));
				}
			}
			else
			{
				path.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			}
		}
	}

	private void InitTrashTitle()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(6.0)
		};
		_trashTitle.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		_trashTitle.Child = stackPanel;
		_trashTitle.MouseLeftButtonUp += OnTrashTitleClick;
		TextBlock textBlock = new TextBlock
		{
			IsHitTestVisible = false,
			FontSize = 20.0,
			FontWeight = FontWeights.Bold,
			MaxWidth = Math.Max(100.0, base.ActualWidth - 150.0),
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		Path arrow = UiUtils.GetArrow(16.0, 0.0, "BaseColorOpacity60");
		arrow.IsHitTestVisible = false;
		arrow.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
		stackPanel.Children.Add(textBlock);
		stackPanel.Children.Add(arrow);
	}

	private void OnTrashTitleClick(object sender, MouseButtonEventArgs e)
	{
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"PersonalTrash", Utils.GetString("PersonalTrash"), (Geometry)null, "")
			{
				Selected = _isPersonTrash
			},
			new CustomMenuItemViewModel((object)"TeamTrash", Utils.GetString("TeamTrash"), (Geometry)null, "")
			{
				Selected = !_isPersonTrash
			}
		}, new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.Bottom,
			PlacementTarget = _trashTitle
		});
		customMenuList.Operated += OnActionSelected;
		customMenuList.Show();
	}

	private async void OnActionSelected(object sender, object e)
	{
		if (!(e is string text))
		{
			return;
		}
		bool usePersonalTrash = _isPersonTrash;
		if (!(text == "PersonalTrash"))
		{
			if (text == "TeamTrash")
			{
				usePersonalTrash = false;
			}
		}
		else
		{
			usePersonalTrash = true;
		}
		if (usePersonalTrash != _isPersonTrash && ViewModel.ProjectIdentity is TrashProjectIdentity trashProjectIdentity)
		{
			trashProjectIdentity.IsPerson = usePersonalTrash;
			LocalSettings.Settings.Save(force: true);
			SetTrashMode();
			Utils.FindParent<TaskView>(this)?.HideDetail();
			_moreButton.Visibility = ((!usePersonalTrash) ? Visibility.Collapsed : Visibility.Visible);
			await ResetTrashDataForRefresh(usePersonalTrash);
			await LoadTaskAsync(forceReload: true);
			LoadTrashDataInBackground(usePersonalTrash);
		}
	}

	private void SetMaxTrashTitleWidth(double width)
	{
		if (!(_trashTitle.Child is StackPanel stackPanel))
		{
			return;
		}
		foreach (object child in stackPanel.Children)
		{
			if (child is TextBlock textBlock)
			{
				textBlock.MaxWidth = width;
			}
		}
	}

	public ProjectTaskListView(TaskView parent)
	{
		_parent = parent;
		InitBatchHelper();
		base.DataContext = new TaskListViewModel();
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		base.RowDefinitions.Add(new RowDefinition());
		InitFoldIcon();
		InitTitle();
		InitTopButtons();
		InitAddContent();
		InitListView();
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		base.SizeChanged += OnSizeChanged;
		base.Background = Brushes.Transparent;
		base.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
	}

	private void UnbindEvents()
	{
		DataChangedNotifier.SyncDone -= OnSyncChanged;
		DataChangedNotifier.ThemeModeChanged -= OnThemeModeChanged;
		DataChangedNotifier.TaskDefaultChanged -= RestartQuickAdd;
		DataChangedNotifier.ProjectColumnChanged -= OnColumnChanged;
		DataChangedNotifier.PomoChanged -= NotifyReload;
		DataChangedNotifier.CalendarChanged -= OnCalendarChanged;
		DataChangedNotifier.CalendarTitleChanged -= OnCalendarTitleChanged;
		DataChangedNotifier.ProjectChanged -= OnProjectChanged;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
		DataChangedNotifier.TagChanged -= OnTagChanged;
		DataChangedNotifier.ProjectGroupChanged -= OnGroupChanged;
		DataChangedNotifier.ListSectionOpenChanged -= OnListSectionOpenChanged;
		DataChangedNotifier.AddTaskToSelectView -= OnTaskAddToSelect;
		ClosedTaskWithFilterLoader.CompletionLoader.Loaded -= LoadedMoreReload;
		ClosedTaskWithFilterLoader.AbandonedLoader.Loaded -= LoadedMoreReload;
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "HideComplete");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "ShowSubtasks");
		PropertyChangedEventManager.RemoveHandler(LocalSettings.Settings, OnDisplayChanged, "ShowDetails");
		_delayLoadHandler.StopAndClear();
	}

	private void BindEvents()
	{
		DataChangedNotifier.SyncDone += OnSyncChanged;
		DataChangedNotifier.CalendarChanged += OnCalendarChanged;
		DataChangedNotifier.CalendarTitleChanged += OnCalendarTitleChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeModeChanged;
		DataChangedNotifier.TaskDefaultChanged += RestartQuickAdd;
		DataChangedNotifier.ProjectColumnChanged += OnColumnChanged;
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		DataChangedNotifier.FilterChanged += OnFilterChanged;
		DataChangedNotifier.TagChanged += OnTagChanged;
		DataChangedNotifier.ProjectGroupChanged += OnGroupChanged;
		DataChangedNotifier.PomoChanged += NotifyReload;
		DataChangedNotifier.ListSectionOpenChanged += OnListSectionOpenChanged;
		DataChangedNotifier.AddTaskToSelectView += OnTaskAddToSelect;
		ClosedTaskWithFilterLoader.CompletionLoader.Loaded += LoadedMoreReload;
		ClosedTaskWithFilterLoader.AbandonedLoader.Loaded += LoadedMoreReload;
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "HideComplete");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "ShowSubtasks");
		PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnDisplayChanged, "ShowDetails");
		_delayLoadHandler.SetAction(DelayLoad);
	}

	private void OnDisplayChanged(object sender, PropertyChangedEventArgs e)
	{
		ReloadData();
	}

	private async void OnTagChanged(object sender, TagModel e)
	{
		if (!base.IsVisible || !(ViewModel?.ProjectIdentity is TagProjectIdentity tagProjectIdentity) || !(tagProjectIdentity.TagModel.id == e.id))
		{
			return;
		}
		TagModel tagModel = CacheManager.GetTagByName(tagProjectIdentity.Tag);
		if (tagModel != null && ((tagModel.viewMode == "timeline" && UserDao.IsPro()) || tagModel.viewMode == "kanban"))
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				DataChangedNotifier.NotifyProjectViewModeChanged(new TagProjectIdentity(tagModel));
			}
		}
		else if (tagModel != null)
		{
			LoadProject(new TagProjectIdentity(tagModel));
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width > 150.0)
		{
			_title.SetMaxWidth(e.NewSize.Width - 150.0);
			SetMaxTrashTitleWidth(e.NewSize.Width - 150.0);
		}
		_listEmpty.Margin = new Thickness(0.0, Math.Max(e.NewSize.Height * 0.28, 140.0), 0.0, 0.0);
	}

	private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (_batchTaskControl != null && _batchTaskControl.IsVisible && !_batchTaskControl.IsMouseOver && !_moreButton.IsMouseOver && !_orderButton.IsMouseOver && (!_batchTaskControl.PopupShowing || _addTaskContent.IsMouseOver) && Utils.GetMousePointElement<TaskListItem>(e, this) == null && Utils.GetMousePointElement<SectionItemControl>(e, this) == null && Utils.GetMousePointElement<EditableSectionControl>(e, this) == null)
		{
			ViewModel.ExitBatchSelect();
			HideBatchSelectControl();
		}
	}

	private void OnListSectionOpenChanged(object sender, (string, SortOption) e)
	{
		ProjectIdentity projectIdentity = ViewModel.ProjectIdentity;
		if (projectIdentity != null && !object.Equals(sender, _taskList) && projectIdentity.CatId == e.Item1 && projectIdentity.SortOption.Equal(e.Item2))
		{
			_taskList?.ReLoad(null);
		}
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		_taskList?.OnTasksChanged(e);
	}

	private void DelayLoad(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			ReloadData(forceReload: false, "", forceFocus: false);
		});
	}

	private void OnCalendarChanged(object sender, EventArgs e)
	{
		if (ViewModel.ProjectIdentity is SubscribeCalendarProjectIdentity || ViewModel.ProjectIdentity is BindAccountCalendarProjectIdentity)
		{
			LoadProject(ViewModel?.ProjectIdentity);
		}
	}

	private void OnCalendarTitleChanged(object sender, string id)
	{
		if (ViewModel.ProjectIdentity is SubscribeCalendarProjectIdentity subscribeCalendarProjectIdentity)
		{
			if (!(subscribeCalendarProjectIdentity.Id != id))
			{
				CalendarSubscribeProfileModel calendarSubscribeProfileModel = CacheManager.GetSubscribeCalendars().FirstOrDefault((CalendarSubscribeProfileModel c) => c.Id == id);
				if (calendarSubscribeProfileModel != null)
				{
					SubscribeCalendarProjectIdentity projectIdentity = new SubscribeCalendarProjectIdentity(calendarSubscribeProfileModel);
					LoadProject(projectIdentity);
				}
			}
		}
		else if (ViewModel.ProjectIdentity is BindAccountCalendarProjectIdentity bindAccountCalendarProjectIdentity && !(bindAccountCalendarProjectIdentity.Id != id))
		{
			BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel account) => account.Id == id);
			if (bindCalendarAccountModel != null)
			{
				BindAccountCalendarProjectIdentity projectIdentity2 = new BindAccountCalendarProjectIdentity(bindCalendarAccountModel);
				LoadProject(projectIdentity2);
			}
		}
	}

	private void OnColumnChanged(object sender, string e)
	{
		if (ViewModel.ProjectIdentity is NormalProjectIdentity normalProjectIdentity && normalProjectIdentity.Project.id == e && ViewModel.ProjectIdentity.SortOption.ContainsSortType(Constants.SortType.sortOrder))
		{
			ReloadData();
		}
	}

	private void NotifyReload(object sender, object e)
	{
		ReloadData();
	}

	private void RestartQuickAdd(object sender, EventArgs e)
	{
		LoadQuickAddView(focus: false, keepText: true);
	}

	private async void OnGroupChanged(object sender, ProjectGroupModel e)
	{
		if (!base.IsVisible || !(e?.id == ViewModel?.ProjectIdentity?.CatId))
		{
			return;
		}
		ProjectIdentity projectIdentity = ViewModel?.ProjectIdentity;
		if (!(projectIdentity is GroupProjectIdentity group))
		{
			return;
		}
		ProjectGroupModel groupModel = CacheManager.GetGroupById(group.GroupId);
		if (groupModel != null && ((groupModel.viewMode == "timeline" && UserDao.IsPro()) || groupModel.viewMode == "kanban"))
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				group.SetGroup(groupModel);
				DataChangedNotifier.NotifyProjectViewModeChanged(group);
			}
		}
		else if (groupModel != null)
		{
			group.SetGroup(groupModel);
			LoadProject(group);
		}
	}

	private async void OnProjectChanged(object sender, EventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		ProjectModel project = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == ViewModel?.ProjectIdentity.Id);
		if (project?.viewMode == "kanban")
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				DataChangedNotifier.NotifyProjectViewModeChanged(new NormalProjectIdentity(project));
			}
		}
		else if (project?.kind != Constants.ProjectKind.NOTE.ToString() && project?.viewMode == "timeline" && UserDao.IsPro())
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				DataChangedNotifier.NotifyProjectViewModeChanged(new NormalProjectIdentity(project));
			}
		}
		else if (ViewModel?.ProjectIdentity is GroupProjectIdentity groupProjectIdentity)
		{
			ProjectGroupModel groupById = CacheManager.GetGroupById(groupProjectIdentity.GroupId);
			if (groupById != null)
			{
				List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(groupProjectIdentity.GroupId);
				ViewModel.ProjectIdentity = new GroupProjectIdentity(groupById, projectsInGroup);
				LoadQuickAddView(focus: false, keepText: true);
			}
		}
		else if (project != null)
		{
			LoadProject(new NormalProjectIdentity(project));
			LoadQuickAddView(focus: false, keepText: true);
		}
	}

	private async void OnFilterChanged(object sender, FilterChangeArgs e)
	{
		if (!base.IsVisible || !(e.Filter?.id == ViewModel?.ProjectIdentity.Id) || !(ViewModel?.ProjectIdentity is FilterProjectIdentity filterProjectIdentity))
		{
			return;
		}
		FilterModel filter = CacheManager.GetFilterById(filterProjectIdentity.Id);
		if (filter != null && ((filter.viewMode == "timeline" && UserDao.IsPro()) || filter.viewMode == "kanban"))
		{
			await Task.Delay(500);
			if (base.IsVisible)
			{
				DataChangedNotifier.NotifyProjectViewModeChanged(new FilterProjectIdentity(filter));
			}
		}
		else if (filter != null)
		{
			FilterProjectIdentity filterProjectIdentity2 = new FilterProjectIdentity(filter);
			LoadProject(filterProjectIdentity2);
			if (e.RuleChanged)
			{
				GetQuickAddView()?.ResetView(filterProjectIdentity2, focus: false, keepText: true);
			}
		}
	}

	private void OnThemeModeChanged(object sender, EventArgs e)
	{
		if (ViewModel?.ProjectData != null && base.IsVisible)
		{
			ReloadData(forceReload: false, "", forceFocus: false);
		}
	}

	private void LoadedMoreReload(object sender, bool e)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (e)
			{
				ReloadData(forceReload: true);
			}
			else
			{
				_taskList.HideLoadMore();
			}
		});
	}

	private void OnSyncChanged(object sender, SyncResult e)
	{
		if (e.RemoteTasksChanged && base.Visibility == Visibility.Visible)
		{
			ReloadData();
		}
	}

	private void InitFoldIcon()
	{
		Path path = new Path
		{
			IsHitTestVisible = false,
			Margin = new Thickness(3.0)
		};
		_menuFoldBorder.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
		path.SetResourceReference(FrameworkElement.StyleProperty, "ModuleHeadIconPath");
		path.SetResourceReference(Path.DataProperty, "Ic.ProjectList.CollapseSide");
		_menuFoldBorder.Child = path;
		_menuFoldBorder.PreviewMouseLeftButtonUp += MenuBorderMouseLeftButtonUp;
		base.Children.Add(_menuFoldBorder);
	}

	private void InitTitle()
	{
		_titlePanel.Children.Add(_titleBorder);
		_title.TextChanged += OnTitleTextChanged;
		_topBorder.Child = _titlePanel;
		_topBorder.SetValue(Panel.ZIndexProperty, 1000);
		_topBorder.SetValue(Grid.RowSpanProperty, 3);
		base.Children.Add(_topBorder);
		_title.SetCheckFunc(CheckTitleValid);
		_titleBorder.Child = _title;
	}

	private string CheckTitleValid(string text)
	{
		return ViewModel?.ProjectIdentity?.CheckTitleValid(text);
	}

	private void OnTitleTextChanged(object sender, string e)
	{
		ViewModel.ProjectIdentity?.SaveTitle(e);
		ViewModel.Title = e;
	}

	private void InitTopButtons()
	{
		_aiRecommendButton.Visibility = Visibility.Collapsed;
		_aiRecommendButton.IsImage = false;
		_aiRecommendButton.ImageOpacity = 1.0;
		_aiRecommendButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.TaskOM.RecommendTask");
		_aiRecommendButton.MouseLeftButtonUp += OnAIRecommendTaskClick;
		base.Children.Add(_aiRecommendButton);
		_orderButton.IsImage = false;
		_orderButton.ImageOpacity = 1.0;
		_orderButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.TaskOM.Sort");
		_orderButton.MouseLeftButtonUp += SelectOrderClick;
		base.Children.Add(_orderButton);
		_moreButton.IsImage = false;
		_moreButton.ImageOpacity = 1.0;
		_moreButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.TaskList.More");
		_moreButton.MouseLeftButtonUp += MoreButtonClick;
		base.Children.Add(_moreButton);
	}

	private void InitAddContent()
	{
		_addTaskContent.SetValue(Grid.RowProperty, 1);
		base.Children.Add(_addTaskContent);
	}

	private void InitListView()
	{
		_listContent.SetValue(Grid.RowProperty, 2);
		_listContent.Content = _taskList;
		_taskList.DataContext = base.DataContext as TaskListViewModel;
		base.Children.Add(_listContent);
		_listEmpty.SetValue(Grid.RowProperty, 0);
		_listEmpty.SetValue(Grid.RowSpanProperty, 3);
		base.Children.Add(_listEmpty);
		_taskList.ItemSelect += OnItemSelect;
		_taskList.ItemsCountChanged += OnTaskCountChanged;
		_taskList.BatchSelect += OnBatchTaskSelected;
		_taskList.MoveUpCaret += OnListCaretMoveUp;
	}

	private void InitBatchHelper()
	{
		_batchHelper = new BatchTaskEditHelper(this);
		_batchHelper.TaskMerged += OnTaskMerged;
	}

	private async void OnTaskMerged(object sender, string e)
	{
		if (!string.IsNullOrEmpty(e))
		{
			await Task.Delay(300);
			_taskList.SelectTask(e, TaskSelectType.Navigate, ignoreBatch: true);
		}
	}

	public async Task LoadProject(ProjectIdentity projectIdentity, bool forceRefresh = false, bool forceLoad = false, string scrollToItemId = null)
	{
		await base.Dispatcher.InvokeAsync((Func<Task>)async delegate
		{
			_titlePanel.Opacity = (projectIdentity.IsVirtual ? 0.4 : 1.0);
			_menuFoldBorder.Opacity = (projectIdentity.IsVirtual ? 0.4 : 1.0);
			_moreButton.Opacity = (projectIdentity.IsVirtual ? 0.4 : 1.0);
			_orderButton.Opacity = (projectIdentity.IsVirtual ? 0.4 : 1.0);
			_listContent.Opacity = ((projectIdentity.IsVirtual && !projectIdentity.ShowVirtualContent) ? 0.4 : 1.0);
			await LoadProjectAsync(projectIdentity, forceRefresh, forceLoad);
			if (!string.IsNullOrEmpty(scrollToItemId))
			{
				_taskList?.ScrollToItemById(scrollToItemId);
			}
			if (base.DataContext is TaskListViewModel taskListViewModel)
			{
				bool flag = taskListViewModel.ProjectIdentity is TodayProjectIdentity;
				_aiRecommendButton.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				if (flag && !LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.MainAIRecommendHint))
				{
					ShowAIRecHintPopup();
					LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.MainAIRecommendHint);
					LocalSettings.Settings.Save();
				}
			}
		});
	}

	private void ShowAIRecHintPopup()
	{
		ArrowHintUserControl child = new ArrowHintUserControl
		{
			Text = Utils.GetString("AITrySuggestedTasks"),
			ArrowDirection = ArrowHintUserControl.ArrowDirectionEnum.Up,
			ArrowHorizontalAlignment = HorizontalAlignment.Stretch,
			ContentPadding = new Thickness(8.0, 6.0, 8.0, 6.0)
		};
		Popup popup = new Popup
		{
			Child = child,
			AllowsTransparency = true,
			StaysOpen = false,
			Placement = PlacementMode.Center,
			PlacementTarget = _aiRecommendButton,
			VerticalOffset = 32.0
		};
		popup.Closed += delegate
		{
			popup.Child = null;
			popup.PlacementTarget = null;
		};
		popup.IsOpen = true;
	}

	private async Task LoadProjectAsync(ProjectIdentity projectIdentity, bool forceRefresh = false, bool forceLoad = false)
	{
		TaskListView taskList = _taskList;
		if (taskList == null)
		{
			await Task.Delay(100);
			taskList = _taskList;
			if (taskList == null)
			{
				return;
			}
		}
		if (projectIdentity == null)
		{
			return;
		}
		ProjectIdentity projectIdentity2 = ViewModel?.ProjectIdentity;
		bool projectChanged = ViewModel.ProjectIdentity.CatId != projectIdentity.CatId || ViewModel.ProjectIdentity.CanEdit != projectIdentity.CanEdit;
		bool enteredTrash = projectIdentity is TrashProjectIdentity && !(projectIdentity2 is TrashProjectIdentity);
		if (projectIdentity is TrashProjectIdentity trashProjectIdentity && ViewModel.ProjectIdentity is TrashProjectIdentity trashProjectIdentity2)
		{
			trashProjectIdentity.IsPerson = trashProjectIdentity2.IsPerson;
		}
		TaskListViewModel taskListViewModel = base.DataContext as TaskListViewModel;
		if (taskListViewModel == null)
		{
			taskListViewModel = (TaskListViewModel)(base.DataContext = new TaskListViewModel(projectIdentity));
		}
		else
		{
			taskListViewModel.SetIdentity(projectIdentity);
		}
		taskListViewModel.SetBatchEditor(this);
		_title.SetText(taskListViewModel.Title);
		_title.SetMaxWidth(base.ActualWidth - 150.0);
		_title.SetEnable((projectIdentity is NormalProjectIdentity { CanEdit: not false, Project: not null } normalProjectIdentity && !normalProjectIdentity.Project.Isinbox) || projectIdentity is GroupProjectIdentity || projectIdentity is FilterProjectIdentity);
		if (forceRefresh)
		{
			taskList.ScrollToTop();
		}
		_batchHelper.ProjectIdentity = projectIdentity;
		TaskListViewModel taskListViewModel3 = taskListViewModel;
		taskListViewModel3.ProjectData = await ProjectTaskDataProvider.GetProjectData(projectIdentity);
		SetShareIcon();
		if (projectChanged || _addTaskContent.Content == null)
		{
			LoadQuickAddView();
		}
		UpdateView();
		if (projectChanged)
		{
			taskList.SelectedId = "";
			taskList.ScrollToTop();
			taskList.SectionEditing = false;
		}
		if (forceLoad)
		{
			taskList.SetTitleCaretIndex(-1);
		}
		SearchHelper.InSearch = ViewModel.ProjectData is SearchListData;
		bool shouldRefreshTrash = ViewModel.ProjectData is TrashListData && (enteredTrash || forceRefresh);
		bool refreshTrashIsPerson = _isPersonTrash;
		SortProjectData projectData = ViewModel.ProjectData;
		if (!(projectData is SearchListData))
		{
			if (!(projectData is CompleteListData))
			{
				if (!(projectData is AbandonedListData))
				{
					if (projectData is TrashListData)
					{
						if (shouldRefreshTrash)
						{
							await ResetTrashDataForRefresh(refreshTrashIsPerson);
						}
						if (projectChanged)
						{
							base.Children.Remove(_completeFilterControl);
							_completeFilterControl = null;
							base.Children.Remove(_abandonFilterControl);
							_abandonFilterControl = null;
							base.Children.Remove(_searchFilterControl);
							_searchFilterControl = null;
						}
					}
					else
					{
						base.Children.Remove(_completeFilterControl);
						_completeFilterControl = null;
						base.Children.Remove(_abandonFilterControl);
						_abandonFilterControl = null;
						base.Children.Remove(_searchFilterControl);
						_searchFilterControl = null;
					}
				}
				else
				{
					InitAbandonedData();
				}
			}
			else
			{
				InitCompletedData();
			}
		}
		else
		{
			InitSearchData();
			SearchHelper.InSearch = true;
			taskList.ScrollToTop();
		}
		UpdateLayout();
		await LoadTaskAsync(forceLoad);
		if (shouldRefreshTrash)
		{
			LoadTrashDataInBackground(refreshTrashIsPerson);
		}
		TryLoadClosedProjectData(ViewModel.ProjectData);
		taskList.TryLoadCompletedTasks(ViewModel.ProjectIdentity);
	}

	private static async Task ResetTrashDataForRefresh(bool isPersonTrash)
	{
		await TrashSyncService.ResetForRefresh(isPersonTrash);
	}

	private void LoadTrashDataInBackground(bool isPersonTrash)
	{
		LoadTrashDataInBackgroundAsync(isPersonTrash);
	}

	private async Task LoadTrashDataInBackgroundAsync(bool isPersonTrash)
	{
		try
		{
			await LoadTrashDataFromRemote(isPersonTrash, reloadAfterLoad: true);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private async Task LoadTrashDataFromRemote(bool isPersonTrash, bool reloadAfterLoad = false)
	{
		if (await ViewModel.TrashTaskLoader.TryLoadTrashTasks(isPersonTrash) && reloadAfterLoad && _taskList != null && ViewModel.ProjectIdentity is TrashProjectIdentity trashProjectIdentity && trashProjectIdentity.IsPerson == isPersonTrash)
		{
			await LoadTaskAsync(forceReload: true, forceFocus: false);
		}
	}

	private void UpdateView()
	{
		SortProjectData projectData = ViewModel.ProjectData;
		if (projectData != null)
		{
			SwitchTitle(projectData.IsTrash);
			_orderButton.Visibility = ((projectData.IsCompleted || projectData.IsProjectClosed || projectData is SearchListData || projectData is PreviewListData || projectData.IsTeamExpired || projectData is BindAccountCalendarListData || projectData is SubscribeCalendarListData || projectData is TrashListData || projectData is DateListData || ViewModel.ProjectIdentity is SummaryProjectIdentity) ? Visibility.Collapsed : Visibility.Visible);
			bool num = !projectData.IsProjectClosed && !projectData.IsCompleted && !(projectData is SearchListData) && !(projectData is PreviewListData) && !projectData.IsTeamExpired && !(projectData is TrashListData) && !(ViewModel.ProjectIdentity is SummaryProjectIdentity);
			bool flag = projectData is TrashListData && _isPersonTrash;
			bool num2 = _parent?.ShowInParallel ?? false;
			bool flag2 = projectData.IsCompleted || projectData is SubscribeCalendarListData || projectData is BindAccountCalendarListData;
			bool flag3 = !num2 && flag2;
			TaskView parent = _parent;
			bool flag4 = parent != null && parent.ShowInCal && (flag2 || (projectData is NormalListData normalListData && !normalListData.Project.IsValid()));
			_moreButton.MouseLeftButtonUp -= MoreButtonClick;
			_moreButton.MouseLeftButtonUp -= ShowPrintPopup;
			_moreButton.MouseLeftButtonUp -= ClearMouseLeftButtonUp;
			_moreButton.MouseLeftButtonUp -= ExitParallel;
			if (num && !flag3)
			{
				_moreButton.Visibility = Visibility.Visible;
				_moreButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.More");
				_moreButton.IsImage = false;
				_moreButton.MouseLeftButtonUp += MoreButtonClick;
				_moreButton.ToolTip = Utils.GetString("More");
			}
			else if (flag)
			{
				_moreButton.Visibility = Visibility.Visible;
				_moreButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.TaskList.Clear");
				_moreButton.IsImage = false;
				_moreButton.MouseLeftButtonUp += ClearMouseLeftButtonUp;
				_moreButton.ToolTip = Utils.GetString("ClearTrash");
			}
			else if (flag3)
			{
				_moreButton.Visibility = Visibility.Visible;
				_moreButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.TaskOM.Print");
				_moreButton.IsImage = false;
				_moreButton.MouseLeftButtonUp += ShowPrintPopup;
				_moreButton.ToolTip = Utils.GetString("Print");
			}
			else if (flag4)
			{
				_moreButton.Visibility = Visibility.Visible;
				_moreButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.Common.Close");
				_moreButton.IsImage = false;
				_moreButton.MouseLeftButtonUp += ExitParallel;
				_moreButton.ToolTip = Utils.GetString("Close");
			}
			else
			{
				_moreButton.Visibility = Visibility.Collapsed;
			}
			_titlePanel.Visibility = ((ViewModel.ProjectData is SearchListData) ? Visibility.Collapsed : Visibility.Visible);
			_menuFoldBorder.Visibility = ((ViewModel.ProjectData is SearchListData || _hideFoldMenu) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void SetShareIcon()
	{
		if (ViewModel.ProjectData.ShowShare && ViewModel.ProjectData.ShowAssignSort && !ViewModel.ProjectData.IsProjectClosed && !ViewModel.ProjectData.IsTeamExpired)
		{
			if (_shareButton == null)
			{
				_shareButton = new HoverIconButton
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
					ImageWidth = 20.0,
					IsImage = false
				};
				_shareButton.MouseLeftButtonUp += ShareMouseLeftButtonUp;
				_titlePanel.Children.Add(_shareButton);
			}
			if (!(ViewModel?.ProjectData is NormalListData normalListData))
			{
				return;
			}
			string permission = normalListData.Project.permission;
			if (!(permission == "read"))
			{
				if (permission == "comment")
				{
					_shareButton.ToolTip = new ToolTip
					{
						Content = Utils.GetString("CanComment")
					};
					_shareButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.SharingCanComment");
				}
				else
				{
					_shareButton.ToolTip = new ToolTip
					{
						Content = Utils.GetString("Editable")
					};
					_shareButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.SharingEditable");
				}
			}
			else
			{
				_shareButton.ToolTip = new ToolTip
				{
					Content = Utils.GetString("ReadOnly")
				};
				_shareButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.ProjectList.SharingReadOnly");
			}
		}
		else
		{
			_titlePanel.Children.Remove(_shareButton);
			_shareButton = null;
		}
	}

	private void ShareMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (!string.IsNullOrEmpty(ViewModel?.ProjectData.DefaultProjectModel.id))
		{
			ShareProject(ViewModel.ProjectData.DefaultProjectModel.id);
		}
	}

	private void LoadQuickAddView(bool focus = false, bool keepText = false)
	{
		ProjectIdentity projectIdentity = ViewModel?.ProjectIdentity;
		if (projectIdentity == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(ViewModel?.ProjectData?.AddTaskHint))
		{
			_addTaskContent.Visibility = Visibility.Collapsed;
			_addTaskContent.Content = null;
		}
		else
		{
			_addTaskContent.Visibility = Visibility.Visible;
		}
		if (projectIdentity is BindAccountCalendarProjectIdentity bindAccountCalendarProjectIdentity)
		{
			if (bindAccountCalendarProjectIdentity.Account != null && !bindAccountCalendarProjectIdentity.Account.Expired && SubscribeCalendarHelper.GetDefaultCalendar(bindAccountCalendarProjectIdentity.Account.Id) != null)
			{
				if (_addTaskContent.Content is CalendarAddView calendarAddView)
				{
					calendarAddView.ResetView(projectIdentity, focus);
					calendarAddView.SetAccountKind(bindAccountCalendarProjectIdentity.Account.Kind);
				}
				else
				{
					CalendarAddView calendarAddView2 = new CalendarAddView(bindAccountCalendarProjectIdentity.Account, focus);
					calendarAddView2.CalendarEventAdded += OnEventAdded;
					_addTaskContent.Content = calendarAddView2;
				}
			}
		}
		else if (_addTaskContent.Content is QuickAddView quickAddView && !(quickAddView is CalendarAddView))
		{
			quickAddView.ResetView(projectIdentity, focus, keepText);
			TryApplyCurrentFirstSectionToQuickAdd(quickAddView);
		}
		else
		{
			QuickAddView quickAddView2 = new QuickAddView(projectIdentity, QuickAddView.Scenario.TaskList, "light", focus);
			quickAddView2.TitleText.MoveDown += OnQuickAddMoveDown;
			quickAddView2.HideGuide += OnHideGuide;
			quickAddView2.TaskAdded += OnTaskQuickAdded;
			TryApplyCurrentFirstSectionToQuickAdd(quickAddView2);
			_addTaskContent.Content = quickAddView2;
		}
	}

	private async Task TryApplyCurrentFirstSectionToQuickAdd(QuickAddView quickAddView)
	{
		if (quickAddView?.Model == null)
		{
			return;
		}
		NormalProjectIdentity projectIdentity = ViewModel?.ProjectIdentity as NormalProjectIdentity;
		if (projectIdentity == null)
		{
			return;
		}
		string text = null;
		if (projectIdentity.SortOption?.groupBy == Constants.SortType.sortOrder.ToString())
		{
			text = ViewModel.Items?.FirstOrDefault(delegate(DisplayItemModel item)
			{
				if (item.IsSection)
				{
					ticktick_WPF.Util.Provider.Section section = item.Section;
					if (section != null && section.Customized && item.Section?.ProjectId == projectIdentity.Id && !string.IsNullOrEmpty(item.Section.SectionId) && item.Section.SectionId != "none")
					{
						return item.Section.SectionId != "NotPinned";
					}
				}
				return false;
			})?.Section?.SectionId;
		}
		if (string.IsNullOrEmpty(text))
		{
			text = (await ColumnDao.GetColumnsByProjectId(projectIdentity.Id))?.OrderBy((ColumnModel column) => column.sortOrder).FirstOrDefault()?.id;
		}
		if (!string.IsNullOrEmpty(text))
		{
			quickAddView.Model.AddToColumnId = text;
			quickAddView.Model.OriginalAddToColumnId = text;
		}
	}

	private bool ShouldAdjustDefaultColumnOnListOpen()
	{
		if (ViewModel?.ProjectIdentity is NormalProjectIdentity normalProjectIdentity && KanbanHelper.IsProjectInKanbanMode(normalProjectIdentity.Id))
		{
			return normalProjectIdentity.SortOption?.groupBy == Constants.SortType.sortOrder.ToString();
		}
		return false;
	}

	private async Task TryAdjustKanbanDataBeforeTaskLoad()
	{
		if (ShouldAdjustDefaultColumnOnListOpen() && ViewModel.ProjectIdentity is NormalProjectIdentity normalProjectIdentity)
		{
			await TaskService.AdjustKanbanData(normalProjectIdentity.Id);
		}
	}

	private void OnTaskQuickAdded(object sender, TaskModel task)
	{
		UtilLog.Info("TaskList.AddTask " + task.id + " from:quickAdd");
		if (ViewModel.ProjectIdentity is FilterProjectIdentity filterProjectIdentity)
		{
			List<TaskBaseViewModel> tasksMatchedFilter = TaskService.GetTasksMatchedFilter(filterProjectIdentity.Filter, new List<TaskBaseViewModel>
			{
				new TaskBaseViewModel(task)
			});
			if (tasksMatchedFilter == null || tasksMatchedFilter.Count == 0)
			{
				Utils.GetToastWindow()?.ToastMoveProjectControl(task.projectId, task.title, MoveToastType.Add);
				return;
			}
		}
		if (sender != null && sender.Equals(GetQuickAddView()))
		{
			_taskList.SelectedId = task.id;
			this.TaskAdded?.Invoke(this, task);
		}
	}

	private void OnTaskAddToSelect(object sender, TaskModel task)
	{
		UtilLog.Info("TaskList.AddTask " + task.id + " from:AI complete");
		_taskList.SelectedId = task.id;
		this.TaskAdded?.Invoke(this, task);
	}

	private void OnQuickAddMoveDown(object sender, EventArgs e)
	{
		if (_taskList.IsVisible)
		{
			_taskList.TryFocusFirstItem();
		}
	}

	private void OnEventAdded(object sender, CalendarEventModel e)
	{
		_taskList.SelectedId = e.Id;
		ReloadData(forceReload: true);
		this.EventAdded?.Invoke(this, e.Id);
	}

	public void NavigateTask(string taskId)
	{
		_taskList.SetItemSelected(ViewModel.Items.ToList(), taskId);
	}

	private async void ClearMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		bool? flag = new CustomerDialog(Utils.GetString("ClearTrash"), Utils.GetString("SureClearTrash"), Utils.GetString("OK"), Utils.GetString("Cancel")).ShowDialog();
		if (flag.HasValue && flag.Value)
		{
			await TaskService.BatchClearTrash(await TaskDao.GetDeletedIds());
			await ReloadData();
		}
	}

	private void ExitParallel(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("split_view", "calendar+task", "close_from_om");
		MainWindow.ShowTaskInCalendarChanged();
	}

	private async void TryLoadClosedProjectData(SortProjectData projectDataModel)
	{
		ProjectModel projectModel = (projectDataModel as NormalListData)?.Project;
		if (projectModel != null && projectModel.closed.HasValue && projectModel.closed.Value)
		{
			await SyncManager.PullProjectTasks(projectModel.id);
			ReloadData();
		}
	}

	public async void TryLoadCompletedTasks(ProjectIdentity identity)
	{
		object dataContext = base.DataContext;
		TaskListViewModel vm = dataContext as TaskListViewModel;
		bool flag = vm != null;
		if (flag)
		{
			flag = await vm.CompletedTaskLoader.NeedPullCompletedTasks(identity);
		}
		if (flag && await vm.CompletedTaskLoader.TryLoadCompletedTasks(identity))
		{
			await ReloadData(forceReload: true);
		}
	}

	public void ReloadTask(bool forceReload = false, string taskId = "", bool forceFocus = true)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			_taskList.SelectedId = taskId;
		}
		LoadTaskAsync(forceReload, forceFocus);
	}

	private async Task LoadTaskAsync(bool forceReload = false, bool forceFocus = true)
	{
		await TryAdjustKanbanDataBeforeTaskLoad();
		await _taskList.LoadAsync(forceReload, loadMore: true, restoreSelected: true, ignoreFocus: false, forceFocus);
		RefreshQuickAddAfterTaskLoad();
	}

	private void OnItemSelect(ListItemSelectModel model)
	{
		this.TaskSelect?.Invoke(model);
	}

	private void OnTaskCountChanged(object sender, EventArgs e)
	{
		int count = (ViewModel.Items.Any() ? 1 : 0);
		RefreshEmptyView(count);
	}

	private void RefreshEmptyView(int count)
	{
		_listEmpty.Visibility = ((count != 0) ? Visibility.Collapsed : Visibility.Visible);
		if (count == 0)
		{
			SetEmptyView((base.DataContext as TaskListViewModel)?.ProjectData);
		}
		else
		{
			_emptyShow = null;
			_emptyView?.Clear();
			_emptyView = null;
			_listEmpty.Children.Clear();
		}
		SetDetailImageVisible(count);
	}

	private void SetDetailImageVisible(int count)
	{
		_parent?.SetEmptyImageVisible(count > 0, isTask: true, ViewModel.ProjectIdentity.IsNote);
	}

	private async void SetEmptyView(SortProjectData projectDataModel)
	{
		if (projectDataModel != null && (!(_emptyShow == projectDataModel.ProjectIdentity?.Id) || !(_emptyShow != "_special_id_date")))
		{
			_emptyShow = projectDataModel.ProjectIdentity?.Id;
			if (_emptyView == null)
			{
				_emptyView = new EmptyView();
				_listEmpty.Children.Insert(0, _emptyView);
			}
			_listEmpty.Children.RemoveRange(1, _listEmpty.Children.Count - 1);
			bool allCompleted = LocalSettings.Settings.HideComplete && !projectDataModel.IsCompleted && ViewModel.SourceModels.Any();
			_emptyView.SetIconsSource(projectDataModel.GetEmptyIconsSource(allCompleted), !(projectDataModel is AssignListData));
			if (projectDataModel.ShowFilterExpired())
			{
				TextBlock textBlock = new TextBlock
				{
					FontSize = 14.0,
					HorizontalAlignment = HorizontalAlignment.Center,
					LineHeight = 20.0,
					Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
				};
				textBlock.Inlines.Add(new Run
				{
					Text = Utils.GetString("FilterExpiredHead") + " "
				});
				Run run = new Run
				{
					Text = Utils.GetString("FilterExpiredCenter"),
					Background = Brushes.Transparent,
					Cursor = Cursors.Hand
				};
				run.SetResourceReference(TextElement.ForegroundProperty, "PrimaryColor");
				run.MouseLeftButtonUp += OnExpiredFilterClick;
				textBlock.Inlines.Add(run);
				textBlock.Inlines.Add(new Run
				{
					Text = " " + Utils.GetString("FilterExpiredTail")
				});
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
				_listEmpty.Children.Add(textBlock);
			}
			else if (projectDataModel.ShowCalendarExpired())
			{
				TextBlock textBlock2 = new TextBlock
				{
					Text = Utils.GetString("Reauthorize"),
					Margin = new Thickness(10.0, 2.0, 10.0, 0.0),
					LineHeight = 20.0,
					FontSize = 14.0,
					HorizontalAlignment = HorizontalAlignment.Center,
					Cursor = Cursors.Hand
				};
				textBlock2.SetResourceReference(TextBlock.ForegroundProperty, "TextAccentColor");
				textBlock2.MouseLeftButtonUp += OnExpiredCalendarClick;
				_listEmpty.Children.Add(textBlock2);
			}
			else
			{
				TextBlock textBlock3 = new TextBlock
				{
					Text = projectDataModel.GetEmptyTitle(allCompleted),
					FontSize = 14.0,
					TextAlignment = TextAlignment.Center,
					LineHeight = 20.0,
					Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
				};
				textBlock3.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
				_listEmpty.Children.Add(textBlock3);
				TextBlock textBlock4 = new TextBlock
				{
					Text = projectDataModel.GetEmptyContent(allCompleted),
					FontSize = 12.0,
					TextAlignment = TextAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(10.0, 4.0, 10.0, 0.0),
					LineHeight = 17.0
				};
				textBlock4.SetResourceReference(TextBlock.ForegroundProperty, "BaseSolidColorOpacity60");
				_listEmpty.Children.Add(textBlock4);
			}
		}
	}

	private void OnExpiredCalendarClick(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is TaskListViewModel taskListViewModel))
		{
			return;
		}
		if (taskListViewModel.ProjectData is BindAccountCalendarListData bindAccountCalendarListData)
		{
			if (bindAccountCalendarListData.Account.IsBindAccountPassword())
			{
				BindAccountWindow bindAccountWindow = new BindAccountWindow(bindAccountCalendarListData.Account);
				bindAccountWindow.Owner = Utils.GetParentWindow(this);
				bindAccountWindow.ShowDialog();
			}
			else if (bindAccountCalendarListData.Account.Site == "outlook")
			{
				BindOutlookWindow bindOutlookWindow = new BindOutlookWindow();
				bindOutlookWindow.SetOriginAccount(bindAccountCalendarListData.Account?.Id);
				bindOutlookWindow.ShowDialog();
			}
			else if (bindAccountCalendarListData.Account.Site == "google")
			{
				BindGoogleAccount instance = BindGoogleAccount.GetInstance();
				instance.Start(bindAccountCalendarListData.Account?.Id);
				instance.ToastText += delegate(object se, string s)
				{
					Utils.Toast(s);
				};
			}
			else if (bindAccountCalendarListData.Account.Kind == "icloud")
			{
				BindICloudWindow bindICloudWindow = new BindICloudWindow(null, bindAccountCalendarListData.Account);
				bindICloudWindow.Owner = Window.GetWindow(this);
				bindICloudWindow.Topmost = false;
				bindICloudWindow.ShowDialog();
			}
		}
		else if (taskListViewModel.ProjectData is SubscribeCalendarListData subscribeCalendarListData)
		{
			if (subscribeCalendarListData.Profile.Kind != 2)
			{
				EditUrlWindow editUrlWindow = new EditUrlWindow(new ticktick_WPF.Views.Config.SubscribeCalendarViewModel(subscribeCalendarListData.Profile));
				editUrlWindow.Owner = Window.GetWindow(this);
				editUrlWindow.ShowDialog();
			}
			else
			{
				EditHolidaySubsWindow editHolidaySubsWindow = new EditHolidaySubsWindow(new ticktick_WPF.Views.Config.SubscribeCalendarViewModel(subscribeCalendarListData.Profile));
				editHolidaySubsWindow.Owner = Window.GetWindow(this);
				editHolidaySubsWindow.ShowDialog();
			}
		}
	}

	private void OnExpiredFilterClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TaskListViewModel { ProjectIdentity: FilterProjectIdentity projectIdentity })
		{
			FilterEditDialog filterEditDialog = new FilterEditDialog(projectIdentity.Filter);
			filterEditDialog.Owner = Window.GetWindow(this);
			filterEditDialog.ShowDialog();
		}
	}

	private void OnBatchTaskSelected(object sender, (List<string>, bool) taskIdsAndShow)
	{
		this.BatchTaskSelected?.Invoke(this, taskIdsAndShow);
	}

	private void OnListCaretMoveUp(object sender, EventArgs e)
	{
		if (_addTaskContent.Visibility == Visibility.Visible && _addTaskContent.Content is QuickAddView quickAddView)
		{
			quickAddView.FocusText();
		}
	}

	public void FocusQuickAdd()
	{
		LoadQuickAddView(focus: true, keepText: true);
	}

	private async void SelectOrderClick(object sender, MouseButtonEventArgs e)
	{
		if (OrderPopup != null || ViewModel?.ProjectIdentity == null || ViewModel?.ProjectData == null)
		{
			return;
		}
		ProjectIdentity projectIdentity = ViewModel?.ProjectIdentity;
		List<SortTypeViewModel> sortTypes;
		if (!(projectIdentity is GroupProjectIdentity groupProjectIdentity))
		{
			TagProjectIdentity tagProjectIdentity = projectIdentity as TagProjectIdentity;
			sortTypes = ((tagProjectIdentity != null) ? SortOptionHelper.GetTagProjectSortTypeModels(CacheManager.GetTags().Any((TagModel t) => t.parent == tagProjectIdentity.Tag)) : ((projectIdentity is NormalProjectIdentity normalProjectIdentity) ? SortOptionHelper.GetNormalProjectSortTypeModels(normalProjectIdentity.Project.IsShareList(), normalProjectIdentity.Project.IsNote) : ((!(projectIdentity is FilterProjectIdentity filterProjectIdentity)) ? SortOptionHelper.GetSmartProjectSortTypeModels() : SortOptionHelper.GetSmartProjectSortTypeModels(filterProjectIdentity.OnlyNote))));
		}
		else
		{
			sortTypes = SortOptionHelper.GetGroupProjectSortTypeModels(groupProjectIdentity.DisplayKind, groupProjectIdentity.ContainsShare);
		}
		OrderPopup = new EscPopup
		{
			PlacementTarget = _orderButton,
			Placement = PlacementMode.Bottom,
			HorizontalOffset = -156.0,
			VerticalOffset = -5.0,
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade
		};
		OrderPopup.Closed += OrderPopup_Closed;
		SortTypeSelector sortTypeSelector = new SortTypeSelector(ViewModel.ProjectIdentity, sortTypes, ViewModel.SortOption, inKanban: false, OrderPopup);
		sortTypeSelector.ResetSortOrder += OnResetSortOrder;
		sortTypeSelector.SortOptionSelect += OnSortOptionSelect;
		sortTypeSelector.Show();
	}

	private void OrderPopup_Closed(object sender, EventArgs e)
	{
		OrderPopup.Closed -= OrderPopup_Closed;
		OrderPopup = null;
	}

	private async void OnSortOptionSelect(object sender, SortOption sortOption)
	{
		if (ViewModel.ProjectData != null)
		{
			ViewModel.ProjectData.SortOption = sortOption;
			ViewModel.ProjectIdentity.SortOption = ViewModel.ProjectData.SortOption;
			ViewModel.ProjectData.SaveSortOption(ViewModel.ProjectData.SortOption);
			UtilLog.Info($"TaskList.Om SetSortType {ViewModel.ProjectIdentity},Id {ViewModel.ProjectIdentity.Id},type {ViewModel.ProjectData.SortOption}");
			await ReloadData();
			SyncManager.TryDelaySync(1000);
		}
	}

	private async void OnResetSortOrder(object sender, int e)
	{
		await TaskSortOrderService.DeleteAllSortOrderBySortOptionInListId(ViewModel.ProjectData.ProjectIdentity.SortOption, ViewModel.ProjectData.ProjectIdentity.GetSortProjectId());
		UserActCollectUtils.AddClickEvent("tasklist", "sort_note", "reset_tag");
		DataChangedNotifier.NotifySortOptionChanged(ViewModel.ProjectData.ProjectIdentity.CatId);
		SyncManager.TryDelaySync();
	}

	private async void MoreButtonClick(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is TaskListViewModel vm) || MorePopup != null)
		{
			return;
		}
		List<CustomMenuItemViewModel> menuItems = new List<CustomMenuItemViewModel>();
		if (!(vm.ProjectIdentity is BindAccountCalendarProjectIdentity))
		{
			menuItems.Add(new CustomMenuItemViewModel("showComplete", Utils.GetString(LocalSettings.Settings.HideComplete ? "ShowCompleted" : "HideCompleted"), Utils.GetGeometry(LocalSettings.Settings.HideComplete ? "Ic.TaskOM.ShowCompleted" : "Ic.TaskOM.HideCompleted"))
			{
				ShowSelected = false
			});
		}
		menuItems.Add(new CustomMenuItemViewModel("showDetail", Utils.GetString(LocalSettings.Settings.ShowDetails ? "HideDetails" : "ShowDetails"), Utils.GetGeometry(LocalSettings.Settings.ShowDetails ? "Ic.TaskOM.HideDetail" : "Ic.TaskOM.ShowDetail"))
		{
			ShowSelected = false
		});
		if ((UserDao.IsPro() && vm.ProjectIdentity is TodayProjectIdentity) || vm.ProjectIdentity is TomorrowProjectIdentity || vm.ProjectIdentity is WeekProjectIdentity || vm.ProjectIdentity is DateProjectIdentity || (vm.ProjectIdentity is FilterProjectIdentity filterProjectIdentity && filterProjectIdentity.Filter.ContainsDate()) || (vm.ProjectData?.IsCompleted ?? false))
		{
			menuItems.Add(new CustomMenuItemViewModel("showCheckItem", Utils.GetString(LocalSettings.Settings.ShowSubtasks ? "HideSubtask" : "ShowSubtask"), Utils.GetGeometry(LocalSettings.Settings.ShowSubtasks ? "Ic.TaskOM.HideCheckItem" : "Ic.TaskOM.ShowCheckItem"))
			{
				ShowSelected = false
			});
		}
		menuItems.Add(new CustomMenuItemViewModel("ViewOptions", Utils.GetString("DisplaySetting"), Utils.GetGeometry("Ic.TaskOM.ViewSetting"))
		{
			ShowSelected = false
		});
		CustomMenuItemViewModel split = new CustomMenuItemViewModel(null);
		menuItems.Add(split);
		int extra = 1;
		if (vm.ProjectIdentity is NormalProjectIdentity normalProjectIdentity && normalProjectIdentity.Project.IsEnable() && normalProjectIdentity.SortOption.groupByType == Constants.SortType.sortOrder)
		{
			extra++;
			menuItems.Add(new CustomMenuItemViewModel("addSection", Utils.GetString("AddSection"), Utils.GetGeometry("Ic.ProjectList.Add"))
			{
				ShowSelected = false
			});
		}
		if (vm.ProjectData != null && vm.ProjectData.ShowShare && !vm.ProjectData.ShowAssignSort)
		{
			extra++;
			menuItems.Add(new CustomMenuItemViewModel("share", Utils.GetString("Share"), Utils.GetGeometry("Ic.TaskOM.Sharing"))
			{
				ShowSelected = false
			});
		}
		if (vm.ProjectIdentity is NormalProjectIdentity normalProjectIdentity2 && (string.IsNullOrEmpty(normalProjectIdentity2.Project.teamId) || (normalProjectIdentity2.Project.permission != "read" && normalProjectIdentity2.Project.permission != "comment")))
		{
			extra++;
			menuItems.Add(new CustomMenuItemViewModel("listActivities", Utils.GetString("ListActivitiesPro"), Utils.GetGeometry("Ic.TaskOM.ListActivities"))
			{
				ShowSelected = false
			});
		}
		menuItems.Add(new CustomMenuItemViewModel("", Utils.GetString("Print"), Utils.GetGeometry("Ic.TaskOM.Print"))
		{
			SubActions = new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel((object)"print", Utils.GetString("PrintTitle"), (Geometry)null, "")
				{
					ImageWidth = 0.0,
					ShowSelected = false
				},
				new CustomMenuItemViewModel((object)"printDetail", Utils.GetString("PrintTitleAndContent"), (Geometry)null, "")
				{
					ImageWidth = 0.0,
					ShowSelected = false
				}
			},
			ShowSelected = false
		});
		if (vm.ProjectIdentity is NormalProjectIdentity || vm.ProjectIdentity is AllProjectIdentity)
		{
			List<string> list = await CalendarConnectService.GetNotionConnectedProjectIds();
			if (list.Contains(vm.ProjectIdentity.Id) || (vm.ProjectIdentity is AllProjectIdentity && list.Contains("all")))
			{
				menuItems.Add(new CustomMenuItemViewModel("SyncNotion", Utils.GetString("SyncWithNotion"), Utils.GetGeometry("Ic.Tabbar.Sync"))
				{
					ShowSelected = false
				});
			}
		}
		if (_parent?.ShowInCal ?? false)
		{
			menuItems.Add(new CustomMenuItemViewModel("ExitParallel", Utils.GetString("Close"), Utils.GetGeometry("Ic.Common.Close")));
		}
		if (extra == 1)
		{
			menuItems.Remove(split);
		}
		MorePopup = new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.Bottom,
			PlacementTarget = _moreButton,
			HorizontalOffset = -118.0,
			VerticalOffset = -5.0,
			PopupAnimation = PopupAnimation.Fade
		};
		List<string> switchViewModes = vm.ProjectIdentity.GetSwitchViewModes();
		SwitchListViewControl switchListViewControl = ((switchViewModes != null) ? new SwitchListViewControl() : null);
		if (switchViewModes != null)
		{
			switchListViewControl.ViewSelected += delegate(object o, string a)
			{
				MorePopup.IsOpen = false;
				OnSwitchView(a);
			};
			switchListViewControl.SetButtonStatus(true, switchViewModes.Contains("kanban") ? new bool?(false) : ((bool?)null), switchViewModes.Contains("timeline") ? new bool?(false) : ((bool?)null));
		}
		MorePopup.Closed += MorePopup_Closed;
		CustomMenuList customMenuList = new CustomMenuList(menuItems, MorePopup, switchListViewControl);
		customMenuList.MinWidth = 172.0;
		customMenuList.Operated += delegate(object o, object operation)
		{
			MorePopup.IsOpen = false;
			OnMoreItemSelected(operation);
		};
		customMenuList.Show();
	}

	private void MorePopup_Closed(object sender, EventArgs e)
	{
		Window.GetWindow(this)?.Activate();
		MorePopup.Closed -= MorePopup_Closed;
		MorePopup = null;
	}

	private void OnMoreItemSelected(object e)
	{
		if (!(e is string text) || text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 5:
			switch (text[0])
			{
			default:
				return;
			case 's':
				break;
			case 'p':
				if (text == "print")
				{
					OnPrintClick(detail: false);
				}
				return;
			}
			if (!(text == "share"))
			{
				break;
			}
			goto IL_017b;
		case 12:
			switch (text[0])
			{
			default:
				return;
			case 's':
				break;
			case 'E':
				if (text == "ExitParallel")
				{
					UserActCollectUtils.AddClickEvent("split_view", "calendar+task", "close_from_om");
					MainWindow.ShowTaskInCalendarChanged();
				}
				return;
			}
			if (!(text == "showComplete"))
			{
				break;
			}
			goto IL_017b;
		case 13:
			switch (text[5])
			{
			default:
				return;
			case 'h':
				if (!(text == "showCheckItem"))
				{
					return;
				}
				break;
			case 'o':
				if (!(text == "showCountDown"))
				{
					return;
				}
				break;
			}
			goto IL_017b;
		case 10:
			switch (text[0])
			{
			default:
				return;
			case 's':
				break;
			case 'a':
				if (text == "addSection")
				{
					AddSectionClick();
				}
				return;
			case 'S':
				if (text == "SyncNotion")
				{
					Communicator.PostNotionSync();
					MainWindowManager.BeginSyncStory();
				}
				return;
			}
			if (!(text == "showDetail"))
			{
				break;
			}
			goto IL_017b;
		case 11:
			switch (text[0])
			{
			case 'p':
				if (text == "printDetail")
				{
					OnPrintClick(detail: true);
				}
				break;
			case 'V':
				if (text == "ViewOptions")
				{
					TaskListViewOptionsDialog taskListViewOptionsDialog = new TaskListViewOptionsDialog();
					taskListViewOptionsDialog.Owner = Window.GetWindow(this);
					taskListViewOptionsDialog.ShowDialog();
				}
				break;
			}
			break;
		case 14:
			if (text == "listActivities")
			{
				ShowProjectActivityClick();
			}
			break;
		case 6:
		case 7:
		case 8:
		case 9:
			break;
			IL_017b:
			ShowOrHideClick(text);
			break;
		}
	}

	private void OnAIRecommendTaskClick(object sender, MouseButtonEventArgs e)
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

	private void OnPrintClick(bool detail)
	{
		ShowPrintPreview(detail);
		UserActCollectUtils.AddClickEvent("tasklist", "om", detail ? "print_title_content" : "print_title");
	}

	public async void ShowPrintPreview(bool isDetail)
	{
		List<DisplayItemModel> models = ViewModel.Items.ToList();
		if (!models.Any())
		{
			return;
		}
		List<TaskModel> tasks = new List<TaskModel>();
		if (isDetail)
		{
			tasks = await TaskDao.GetThinTasksInBatch((from displayItemModel in models
				where displayItemModel.Type == DisplayType.Task || displayItemModel.Type == DisplayType.Agenda || displayItemModel.Type == DisplayType.Note
				select displayItemModel.Id).ToList());
		}
		TaskListPrintViewModel printModel = new TaskListPrintViewModel
		{
			ListName = ViewModel.ProjectData.GetTitle(),
			IsNormal = (ViewModel.ProjectData is NormalListData)
		};
		foreach (DisplayItemModel model in models)
		{
			TaskPrintViewModel viewModel = new TaskPrintViewModel(model);
			if (isDetail && (model.Type == DisplayType.Task || model.Type == DisplayType.Agenda || model.Type == DisplayType.Note))
			{
				TaskModel taskModel = tasks.FirstOrDefault((TaskModel t) => t.id == model.TaskId);
				viewModel.Content = TaskUtils.ReplaceAttachmentTextInString(taskModel?.content);
				viewModel.Desc = taskModel?.desc;
				if (model.Kind == "CHECKLIST" || model.Type == DisplayType.Agenda)
				{
					List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(model.Id);
					if (list != null && list.Count > 0)
					{
						list.Sort(delegate(TaskDetailItemModel a, TaskDetailItemModel b)
						{
							if (a.status != 0 && b.status == 0)
							{
								return 1;
							}
							if (a.status == 0 && b.status != 0)
							{
								return -1;
							}
							return (a.status != 0 && b.status != 0 && a.completedTime.HasValue && b.completedTime.HasValue) ? b.completedTime.Value.CompareTo(a.completedTime.Value) : a.sortOrder.CompareTo(b.sortOrder);
						});
						viewModel.SubtaskPrintViewModels = list.Select((TaskDetailItemModel i) => new SubtaskPrintViewModel(i)).ToList();
					}
				}
			}
			if (isDetail && model.Type == DisplayType.Event)
			{
				viewModel.Content = model.Content;
			}
			printModel.TaskPrintViewModels.Add(viewModel);
		}
		PrintPreviewWindow window = new PrintPreviewWindow(printModel, isDetail);
		await Task.Delay(300);
		window.Show();
		window.Activate();
	}

	private void ShowProjectActivityClick()
	{
		if (base.DataContext is TaskListViewModel { ProjectIdentity: NormalProjectIdentity projectIdentity })
		{
			UserActCollectUtils.AddClickEvent("tasklist", "om", "project_activities");
			this.OnProjectActivityClick?.Invoke(this, projectIdentity.Project.id);
		}
	}

	private void AddSectionClick()
	{
		UserActCollectUtils.AddClickEvent("tasklist", "om", "add_section");
		if (base.DataContext is TaskListViewModel taskListViewModel && taskListViewModel.SortOption.groupByType == Constants.SortType.sortOrder)
		{
			_taskList.AddNewSectionAtLast();
			_listEmpty.Visibility = Visibility.Collapsed;
		}
	}

	public void TryReloadProject(string projectId)
	{
		if (ViewModel?.ProjectIdentity.Id == projectId)
		{
			ReloadData(forceReload: true);
		}
	}

	private void ShowOrHideClick(string tag)
	{
		switch (tag)
		{
		default:
			_ = tag == "showCountDown";
			break;
		case "share":
			if (base.DataContext is TaskListViewModel taskListViewModel && taskListViewModel.ProjectIdentity.GetProjectId() != null)
			{
				ShareProject(taskListViewModel.ProjectIdentity.GetProjectId());
			}
			UserActCollectUtils.AddClickEvent("tasklist", "om", "collaboration");
			break;
		case "showComplete":
			LocalSettings.Settings.HideComplete = !LocalSettings.Settings.HideComplete;
			ProjectWidgetsHelper.Reload();
			LocalSettings.Settings.Save();
			SettingsHelper.PushLocalSettings();
			UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_completed");
			UtilLog.Info($"TaskList.Om ShowComplete {!LocalSettings.Settings.HideComplete}");
			break;
		case "showCheckItem":
			LocalSettings.Settings.ShowSubtasks = !LocalSettings.Settings.ShowSubtasks;
			LocalSettings.Settings.NotifyPropertyChanged("ProjectNum");
			ProjectWidgetsHelper.Reload();
			LocalSettings.Settings.Save();
			SettingsHelper.PushLocalSettings();
			UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_checklist");
			UtilLog.Info($"TaskList.Om ShowCheckItem {LocalSettings.Settings.ShowSubtasks}");
			break;
		case "showDetail":
			LocalSettings.Settings.ShowDetails = !LocalSettings.Settings.ShowDetails;
			UserActCollectUtils.AddClickEvent("tasklist", "om", "show_hide_details");
			break;
		}
	}

	private void ShareProject(string projectId)
	{
		ShareProjectDialog.TryShowShareDialog(projectId, Window.GetWindow(this));
	}

	private async void OnSwitchView(string e)
	{
		if (!(e == "kanban") && !(e == "timeline"))
		{
			return;
		}
		UserActCollectUtils.AddClickEvent("list_mode", "switch", e);
		if (!(e == "timeline") || ProChecker.CheckPro(ProType.TimeLine, App.Window))
		{
			_parent?.HideDetail();
			if (ViewModel.ProjectIdentity != null)
			{
				await ViewModel.ProjectIdentity.SwitchViewMode(e, Utils.FindParent<TaskView>(this)?.ShowInCal ?? false);
			}
		}
	}

	public void SetFoldMenuIcon(bool hideMenu, bool oneColumn)
	{
		if (_menuFoldBorder.Child is Path path)
		{
			path.SetResourceReference(Path.DataProperty, oneColumn ? "Ic.ProjectList.List" : (hideMenu ? "Ic.ProjectList.ExpandSide" : "Ic.ProjectList.CollapseSide"));
		}
		SetModeColumn(oneColumn);
	}

	public void HideFoldMenuIcon()
	{
		_hideFoldMenu = true;
		_menuFoldBorder.Visibility = Visibility.Collapsed;
		_titlePanel.Margin = new Thickness(-20.0, 0.0, 0.0, 12.0);
		_trashTitle.Margin = new Thickness(22.0, 29.0, 0.0, 12.0);
	}

	public DisplayItemModel SetSelectedId(string id, bool scroll = false)
	{
		if (_taskList != null)
		{
			_taskList.SelectedId = id;
			if (scroll)
			{
				_taskList?.ScrollToItemById(id);
			}
		}
		return ViewModel.SetSelectedTaskId(id);
	}

	public void OnDetailTaskNavigated(string taskId)
	{
		_taskList?.OnDetailTaskNavigated(taskId);
	}

	public void BatchSelectOnMove(bool up)
	{
		_taskList?.BatchSelectOnMove(up);
	}

	public void TryFocusSelectedItem()
	{
		_taskList?.TryFocusSelectedItem();
	}

	public IEnumerable<DisplayItemModel> GetItems()
	{
		if (base.DataContext is TaskListViewModel taskListViewModel)
		{
			return taskListViewModel.Items;
		}
		return null;
	}

	public void ResetSearchFilterControl()
	{
		_searchFilterControl?.Reset();
	}

	public void ReSearch()
	{
		if (ViewModel.ProjectIdentity is SearchProjectIdentity)
		{
			ReloadData(forceReload: true);
		}
	}

	private async void SearchFilterChanged(object sender, EventArgs e)
	{
		if (base.DataContext is TaskListViewModel taskListViewModel && taskListViewModel.ProjectIdentity is SearchProjectIdentity)
		{
			await LoadProject(new SearchProjectIdentity());
		}
	}

	public async Task ForbidShowMenu()
	{
		_canHoverShow = false;
		await Task.Delay(300);
		_canHoverShow = true;
	}

	private async void MenuBorderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		await Task.Delay(1);
		Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
	}

	public void ReloadDataAndQuickAddView()
	{
		if (base.IsVisible)
		{
			LoadQuickAddView(focus: true, keepText: true);
			ReloadData();
		}
	}

	public void OnTaskCopied(string copyId)
	{
		_taskList.SelectedId = copyId;
	}

	public void ReloadOnBatchTaskChanged(object sender, List<string> taskIds)
	{
		ReloadData();
	}

	private QuickAddView GetQuickAddView()
	{
		return _addTaskContent.Content as QuickAddView;
	}

	public async Task ReloadData(bool forceReload = false, string taskId = "", bool forceFocus = true)
	{
		TaskListView taskList = _taskList;
		if (taskList == null)
		{
			await Task.Delay(100);
			taskList = _taskList;
			if (taskList == null)
			{
				return;
			}
		}
		if (!string.IsNullOrEmpty(taskId))
		{
			taskList.SelectedId = taskId;
		}
		await TryAdjustKanbanDataBeforeTaskLoad();
		await taskList.LoadAsync(forceReload, loadMore: true, restoreSelected: true, ignoreFocus: true, forceFocus);
		RefreshQuickAddAfterTaskLoad();
	}

	private void RefreshQuickAddAfterTaskLoad()
	{
		TryApplyCurrentFirstSectionToQuickAdd(GetQuickAddView());
	}

	public bool ExistTask(string taskId)
	{
		if (base.DataContext is TaskListViewModel { SourceModels: not null } taskListViewModel)
		{
			return taskListViewModel.SourceModels.Any((TaskBaseViewModel t) => t.Id == taskId);
		}
		return false;
	}

	public bool CanPrint()
	{
		if (base.DataContext is TaskListViewModel { ProjectIdentity: var projectIdentity })
		{
			if (!(projectIdentity is TrashProjectIdentity) && !(projectIdentity is SearchProjectIdentity) && !(projectIdentity is AbandonedProjectIdentity))
			{
				if (projectIdentity is NormalProjectIdentity normalProjectIdentity)
				{
					return normalProjectIdentity.Project.IsValid();
				}
				return true;
			}
			return false;
		}
		return false;
	}

	private void ShowPrintPopup(object sender, MouseButtonEventArgs e)
	{
		List<CustomMenuItemViewModel> types = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"print", Utils.GetString("PrintTitle"), (DrawingImage)null, "")
			{
				ImageWidth = 0.0
			},
			new CustomMenuItemViewModel((object)"printDetail", Utils.GetString("PrintTitleAndContent"), (DrawingImage)null, "")
			{
				ImageWidth = 0.0
			}
		};
		EscPopup popup = new EscPopup
		{
			StaysOpen = false,
			Placement = PlacementMode.Bottom,
			PlacementTarget = _moreButton,
			HorizontalOffset = -5.0,
			VerticalOffset = -5.0
		};
		CustomMenuList customMenuList = new CustomMenuList(types, popup);
		customMenuList.Operated += delegate(object o, object operation)
		{
			popup.IsOpen = false;
			OnMoreItemSelected(operation);
		};
		customMenuList.Show();
	}

	public async Task TryBatchSetPriority(int priority)
	{
		await TaskService.BatchSetPriority(_taskList.GetSelectedTaskIds(), priority);
	}

	public void BatchOpenSticky()
	{
		TaskStickyWindow.ShowTaskSticky(_taskList.GetSelectedTaskIds());
	}

	public async Task TryBatchSetDate(DateTime? date)
	{
		await TaskService.BatchSetDate(_taskList.GetSelectedTaskIds(), date);
	}

	public async Task BatchPinTask()
	{
		List<string> selectedTaskIds = _taskList.GetSelectedTaskIds();
		List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(selectedTaskIds);
		await TaskService.BatchStarTaskOrNote(pin: !(tasksByIds.Count > 0 && tasksByIds.All((TaskBaseViewModel t) => t.IsPinned)), ids: selectedTaskIds, project: ViewModel?.ProjectIdentity?.CatId);
	}

	public void DeleteSelectedTasks()
	{
		_taskList.DeleteSelectedTasks();
	}

	public void ExpandOrFoldAllTask(bool isOpen)
	{
		_taskList.ExpandOrFoldAllTask();
	}

	public void ExpandOrFoldAllSection()
	{
		_taskList.ExpandOrFoldAllSection();
	}

	public void TabSelectItem()
	{
		if (_addTaskContent.Content is QuickAddView quickAddView)
		{
			quickAddView.TabSelectItem();
		}
	}

	public void RemoveTabSelect()
	{
		GetQuickAddView()?.ClearTabSelect();
	}

	private void InitSearchData()
	{
		if (_searchFilterControl == null)
		{
			_searchFilterControl = new SearchFilterControl
			{
				Margin = new Thickness(0.0, 15.0, 20.0, 0.0)
			};
			_searchFilterControl.SetValue(Grid.RowProperty, 0);
			base.Children.Add(_searchFilterControl);
			_searchFilterControl.FilterChanged += SearchFilterChanged;
		}
		base.Children.Remove(_abandonFilterControl);
		_abandonFilterControl = null;
		base.Children.Remove(_completeFilterControl);
		_completeFilterControl = null;
	}

	private void InitCompletedData()
	{
		if (_completeFilterControl == null)
		{
			_completeFilterControl = new CompletedFilterControl
			{
				Margin = new Thickness(10.0, -12.0, 0.0, 5.0)
			};
			_completeFilterControl.SetValue(Grid.RowProperty, 1);
			base.Children.Add(_completeFilterControl);
			_completeFilterControl.FilterChanged += ClosedFilterChanged;
		}
		base.Children.Remove(_abandonFilterControl);
		_abandonFilterControl = null;
		base.Children.Remove(_searchFilterControl);
		_searchFilterControl = null;
	}

	private void InitAbandonedData()
	{
		if (_abandonFilterControl == null)
		{
			_abandonFilterControl = new CompletedFilterControl(isAbandon: true)
			{
				Margin = new Thickness(10.0, -12.0, 0.0, 5.0)
			};
			_abandonFilterControl.SetValue(Grid.RowProperty, 1);
			base.Children.Add(_abandonFilterControl);
			_abandonFilterControl.FilterChanged += ClosedFilterChanged;
		}
		base.Children.Remove(_completeFilterControl);
		_completeFilterControl = null;
		base.Children.Remove(_searchFilterControl);
		_searchFilterControl = null;
	}

	private void ClosedFilterChanged(object sender, ClosedFilterViewModel filter)
	{
		if (ViewModel?.ProjectData is CompleteListData)
		{
			CompletedProjectIdentity.Filter = filter;
			ClosedTaskWithFilterLoader.CompletionLoader.Reset();
			ReloadData();
		}
		if (ViewModel?.ProjectData is AbandonedListData)
		{
			AbandonedProjectIdentity.Filter = filter;
			ClosedTaskWithFilterLoader.AbandonedLoader.Reset();
			ReloadData();
		}
	}

	public bool GetQuickAddPosition()
	{
		QuickAddView quickAddView = GetQuickAddView();
		if (quickAddView.IsVisible)
		{
			quickAddView?.FocusText();
			return true;
		}
		return false;
	}

	private void HideTemplateGuide(object sender, MouseButtonEventArgs e)
	{
		HideTemplateGuide();
	}

	private void OnHideGuide(object sender, EventArgs e)
	{
		HideTemplateGuide();
	}

	private void HideTemplateGuide()
	{
	}

	public void ShowBatchOperationDialog()
	{
		_batchHelper.ShowOperationDialog();
	}

	public void SetSelectedTaskIds(List<string> taskIds)
	{
		_batchHelper.SelectedTaskIds = taskIds;
	}

	public void RemoveSelectedId(string id)
	{
		_batchHelper.SelectedTaskIds?.Remove(id);
		_taskList?.RemoveSelectedId(id);
	}

	public List<string> GetSelectedTaskIds()
	{
		return _batchHelper.SelectedTaskIds;
	}

	public void ReloadList()
	{
		ReloadData();
	}

	public UIElement BatchOperaPlacementTarget()
	{
		return _titlePanel;
	}

	public void HideBatchSelectControl()
	{
		SetBatchSelectedControl(null);
	}

	public DisplayItemModel GetSelectedItem()
	{
		return ViewModel.GetSelectedItem();
	}

	public void Dispose()
	{
		ViewModel.Title = string.Empty;
		_parent = null;
		this.TaskSelect = null;
		this.EventAdded = null;
		this.BatchTaskSelected = null;
		this.TaskAdded = null;
		this.OnSearchFilterChanged = null;
		this.BatchTaskChanged = null;
		this.OnProjectActivityClick = null;
		base.Children.Clear();
		_taskList?.Dispose();
		_taskList = null;
	}

	public string GetSelectedId()
	{
		return _taskList.SelectedId;
	}

	public bool InSearch()
	{
		SearchFilterControl searchFilterControl = _searchFilterControl;
		if (searchFilterControl == null)
		{
			return false;
		}
		return searchFilterControl.Visibility == Visibility.Visible;
	}

	public void ReloadIdentity()
	{
		ViewModel?.SetIdentity(ViewModel.ProjectIdentity.Copy(ViewModel.ProjectIdentity));
		_taskList?.LoadAsync();
	}

	public void ToggleSelectedTaskCompleted()
	{
		_taskList?.ToggleSelectedItemCompleted();
	}

	public void SetModeColumn(bool isOneColumn)
	{
		_inOneColumn = isOneColumn;
		if (_titleBorder.Child == _trashTitle)
		{
			isOneColumn = !UserManager.IsTeamUser();
		}
		if (isOneColumn)
		{
			_title.IsHitTestVisible = false;
			_trashTitle.IsHitTestVisible = false;
			_titleBorder.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
			_titleBorder.PreviewMouseLeftButtonDown -= OnTopBorderMouseDown;
			_titleBorder.PreviewMouseLeftButtonDown += OnTopBorderMouseDown;
			_titleBorder.MouseLeftButtonUp -= OnTopClick;
			_titleBorder.MouseLeftButtonUp += OnTopClick;
		}
		else
		{
			_trashTitle.IsHitTestVisible = true;
			_title.IsHitTestVisible = true;
			_titleBorder.Style = null;
			_titleBorder.MouseLeftButtonUp -= OnTopClick;
			_titleBorder.PreviewMouseLeftButtonDown -= OnTopBorderMouseDown;
		}
	}

	private void OnTopBorderMouseDown(object sender, MouseButtonEventArgs e)
	{
		_titleBorderMouseDown = true;
	}

	private async void OnTopClick(object sender, MouseButtonEventArgs e)
	{
		if (_titleBorderMouseDown)
		{
			_titleBorderMouseDown = false;
			e.Handled = true;
			await Task.Delay(1);
			Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
		}
	}

	public void SetForceReadOnly(bool b)
	{
		ViewModel?.SetForceReadOnly(b);
	}

	public void ExitVirtual()
	{
		_titlePanel.Opacity = 1.0;
		_menuFoldBorder.Opacity = 1.0;
		_moreButton.Opacity = 1.0;
		_orderButton.Opacity = 1.0;
		_listContent.Opacity = 1.0;
	}

	public void SetBatchSelectedControl(List<string> selected)
	{
		if (selected != null && selected.Count > 0 && !(ViewModel.ProjectIdentity is TrashProjectIdentity))
		{
			if (_batchTaskControl == null)
			{
				_batchTaskControl = new BatchTaskControl
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
				};
				_batchTaskControl.SetValue(Grid.RowSpanProperty, 3);
				base.Children.Add(_batchTaskControl);
			}
			_batchTaskControl.SetTaskIds(selected, ViewModel.ProjectIdentity.IsNote, useInList: true, ViewModel.ProjectIdentity);
			_batchTaskControl.Visibility = Visibility.Visible;
			TaskView parent = _parent;
			if (parent != null && parent.ShowInParallel)
			{
				DataChangedNotifier.NotifyCalOrListBatchSelected(calBatchSelected: false);
			}
		}
		else if (_batchTaskControl != null)
		{
			_batchTaskControl.Visibility = Visibility.Collapsed;
		}
	}

	public void CancelBatchSelect()
	{
		HideBatchSelectControl();
	}
}
