using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Views.Calendar;

public class ArrangeTaskPanel : UserControl, IDragBarEvent, IComponentConnector
{
	public const int ByList = 0;

	public const int ByTag = 1;

	public const int ByPriority = 2;

	public const int ShowNoDate = 0;

	public const int ShowOverDue = 1;

	private static ConcurrentDictionary<string, (DateTime?, TimeData)> _undoArrangeDict = new ConcurrentDictionary<string, (DateTime?, TimeData)>();

	public bool InDisplay;

	private List<CalendarDisplayViewModel> _viewModels = new List<CalendarDisplayViewModel>();

	private bool _inAssemble;

	private static ProjectExtra _projectFilter;

	private DateTime _startDragTime;

	private bool _dragOver;

	internal Grid TopGrid;

	internal StackPanel DateFilterPanel;

	internal GroupTitle2 SwitchTitle;

	internal Grid ListGrid;

	internal Grid ListFilter;

	internal StackPanel FilterTextBorder;

	internal Path AllPath;

	internal EmjTextBlock FilterText;

	internal EscPopup ProjectFilterPopup;

	internal EscPopup TagFilterPopup;

	internal Grid ItemsGrid;

	internal ListView TaskItems;

	internal Grid EmptyTaskGrid;

	internal Border MaskBorder;

	private bool _contentLoaded;

	private CalArrangeDateFilterPopup filterPopup { get; set; }

	private ProjectOrGroupPopup projectOrGroupPopup { get; set; }

	public ArrangeTaskPanel()
	{
		InitializeComponent();
		ListFilter.Visibility = ((LocalSettings.Settings.ArrangeDisplayType == 2) ? Visibility.Collapsed : Visibility.Visible);
		switch (LocalSettings.Settings.ArrangeDisplayType)
		{
		case 0:
			AllPath.SetResourceReference(Path.DataProperty, "Ic.ProjectList.All");
			break;
		case 1:
			AllPath.SetResourceReference(Path.DataProperty, "Ic.Tag.AllTag");
			break;
		}
		ArrangeSectionStatusDao.InitClosedSectionNames();
		_projectFilter = ProjectExtra.Deserialize(LocalSettings.Settings.ArrangeTaskFilterData);
		SwitchTitle.SetSelectedIndex(LocalSettings.Settings.ArrangeDisplayType);
		SetFilterText();
		base.Loaded += delegate
		{
			BindEvents();
		};
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		EmptyView emptyView = new EmptyView(132.0);
		EmptyTaskGrid.Children.Add(emptyView);
		emptyView.SetIconsSource(new EmptyIconsSourceModel("CalendarArrangeEmpty"));
		base.SizeChanged += delegate(object sender, SizeChangedEventArgs e)
		{
			EmptyTaskGrid.Margin = new Thickness(0.0, 30.0 + (e.NewSize.Height - 30.0) * 0.28, 0.0, 0.0);
		};
	}

	private void BindEvents()
	{
		TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.FilterChanged += ReloadFilters;
		DataChangedNotifier.ProjectChanged += ReloadProjects;
		GlobalEventManager.ArrangeFilterTypeChanged += OnArrangeFilterChanged;
		GlobalEventManager.ArrangeTaskSortTypeChanged += OnSortTypeChanged;
		TickDragHelper.DragDrop += OnDrop;
		TickDragHelper.DragMoving += OnDragOver;
		ReloadProjects(this, EventArgs.Empty);
	}

	private void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		if (e.AddIds.Any() || e.StatusChangedIds.Any() || e.TagChangedIds.Any() || e.DeletedChangedIds.Any() || e.DateChangedIds.Any() || e.ProjectChangedIds.Any() || e.KindChangedIds.Any() || e.PriorityChangedIds.Any() || e.UndoDeletedIds.Any() || e.SortOrderChangedIds.Any())
		{
			TryReloadTasks();
			return;
		}
		CalTaskColorMode taskColorMode = CalendarUtils.GetTaskColorMode();
		if (e.PriorityChangedIds.Any() && (taskColorMode == CalTaskColorMode.Priority || LocalSettings.Settings.ArrangeDisplayType == 2))
		{
			TryReloadTasks();
		}
	}

	private void UnbindEvents()
	{
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		DataChangedNotifier.FilterChanged -= ReloadFilters;
		DataChangedNotifier.ProjectChanged -= ReloadProjects;
		GlobalEventManager.ArrangeFilterTypeChanged -= OnArrangeFilterChanged;
		GlobalEventManager.ArrangeTaskSortTypeChanged -= OnSortTypeChanged;
		TickDragHelper.DragDrop -= OnDrop;
		TickDragHelper.DragMoving -= OnDragOver;
	}

	private void OnArrangeFilterChanged(object sender, EventArgs e)
	{
		TryReloadTasks();
	}

	private void ReloadProjects(object sender, EventArgs eventArgs)
	{
		bool num = RemoveInvalidProjectFilters();
		SetFilterText();
		if (num && base.IsVisible && LocalSettings.Settings.ArrangeDisplayType == 0)
		{
			TryReloadTasks();
		}
	}

	private bool RemoveInvalidProjectFilters()
	{
		List<string> projectIds = _projectFilter.ProjectIds;
		if (projectIds != null && projectIds.Count > 0)
		{
			List<string> list = (from p in (from p in CacheManager.GetProjects()
					where _projectFilter.ProjectIds.Contains(p.id)
					select p).ToList()
				where p.IsEnable()
				select p.id).ToList();
			if (list.Count < _projectFilter.ProjectIds.Count)
			{
				_projectFilter.ProjectIds = list;
				SaveArrangeFilter();
				return true;
			}
		}
		return false;
	}

	private void ReloadFilters(object sender, FilterChangeArgs e)
	{
		List<string> filterIds = _projectFilter.FilterIds;
		if (filterIds == null || filterIds.Count <= 0)
		{
			return;
		}
		string filterId = _projectFilter.FilterIds[0];
		if (filterId == e.deleteId)
		{
			if (LocalSettings.Settings.ArrangeDisplayType == 0)
			{
				FilterText.Text = Utils.GetString("AllList");
			}
			_projectFilter.FilterIds = new List<string>();
			SaveArrangeFilter();
			if (LocalSettings.Settings.ArrangeDisplayType == 0)
			{
				TryReloadTasks();
			}
		}
		else if (filterId == e.Filter?.id)
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == filterId && f.deleted != 1);
			if (filterModel != null && LocalSettings.Settings.ArrangeDisplayType == 0)
			{
				FilterText.Text = filterModel.name;
				TryReloadTasks();
			}
		}
	}

	private void SaveArrangeFilter()
	{
		LocalSettings.Settings.ArrangeTaskFilterData = ProjectExtra.Serialize(_projectFilter);
	}

	public void TryReloadTasks()
	{
		LoadTask();
	}

	private void SetFilterText()
	{
		if (LocalSettings.Settings.ArrangeDisplayType == 0)
		{
			FilterText.Text = GetProjectFilterDisplayText();
		}
		else if (LocalSettings.Settings.ArrangeDisplayType == 1)
		{
			FilterText.Text = TagCardViewModel.ToNormalDisplayText(_projectFilter.Tags, 1);
		}
	}

	private static string GetProjectFilterDisplayText()
	{
		return ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(_projectFilter), onlyShowPermission: true);
	}

	private void OnFilterProject(object sender, MouseButtonEventArgs e)
	{
		if (LocalSettings.Settings.ArrangeDisplayType == 0)
		{
			if (projectOrGroupPopup == null)
			{
				projectOrGroupPopup = new ProjectOrGroupPopup(ProjectFilterPopup, _projectFilter, new ProjectSelectorExtra
				{
					showFilters = true,
					onlyShowPermission = true
				});
				projectOrGroupPopup.Save += OnProjectSelect;
				projectOrGroupPopup.Closed += ProjectOrGroupPopup_Closed;
				projectOrGroupPopup.Show();
			}
		}
		else if (LocalSettings.Settings.ArrangeDisplayType == 1)
		{
			TagSearchFilterControl tagSearchFilterControl = new TagSearchFilterControl(_projectFilter.Tags);
			tagSearchFilterControl.Cancel -= OnTagFilterCancel;
			tagSearchFilterControl.Cancel += OnTagFilterCancel;
			tagSearchFilterControl.Save -= OnTagFilterSelected;
			tagSearchFilterControl.Save += OnTagFilterSelected;
			TagFilterPopup.Child = tagSearchFilterControl;
			TagFilterPopup.IsOpen = true;
		}
	}

	private async void ProjectOrGroupPopup_Closed(object sender, EventArgs e)
	{
		projectOrGroupPopup.Save -= OnProjectSelect;
		projectOrGroupPopup.Closed -= ProjectOrGroupPopup_Closed;
		await Task.Delay(100);
		projectOrGroupPopup = null;
	}

	private void OnTagFilterSelected(object sender, List<string> tags)
	{
		TagFilterPopup.IsOpen = false;
		_projectFilter.Tags = tags ?? new List<string>();
		SaveArrangeFilter();
		LoadTask();
		SetFilterText();
	}

	private void OnTagFilterCancel(object sender, EventArgs e)
	{
		TagFilterPopup.IsOpen = false;
	}

	private void OnProjectSelect(object sender, ProjectExtra data)
	{
		data.Tags = _projectFilter.Tags;
		_projectFilter = data;
		SaveArrangeFilter();
		LoadTask();
		SetFilterText();
	}

	public async void NotifyModelChanged()
	{
		if (base.IsVisible)
		{
			await Task.Delay(100);
			TryReloadTasks();
		}
	}

	private async Task LoadTask()
	{
		if (_inAssemble || !InDisplay)
		{
			return;
		}
		_inAssemble = true;
		try
		{
			_viewModels.Clear();
			ProjectExtra projectFilter = new ProjectExtra();
			if (LocalSettings.Settings.ArrangeDisplayType == 0)
			{
				projectFilter.ProjectIds = _projectFilter.ProjectIds;
				projectFilter.GroupIds = _projectFilter.GroupIds;
				projectFilter.FilterIds = _projectFilter.FilterIds;
			}
			if (LocalSettings.Settings.ArrangeDisplayType == 1)
			{
				projectFilter.Tags = _projectFilter.Tags;
			}
			List<TaskBaseViewModel> list = await TaskDisplayService.GetCalendarArrangeTasks(projectFilter);
			if (list != null && list.Count > 0)
			{
				if (LocalSettings.Settings.ArrangeDisplayType == 1)
				{
					GetViewModelsByTag(list, projectFilter.Tags);
				}
				else
				{
					Dictionary<string, List<TaskBaseViewModel>> dictionary = ((LocalSettings.Settings.ArrangeDisplayType == 2) ? GetTaskDictByPriority(list) : GetTaskDictByProject(list));
					foreach (KeyValuePair<string, List<TaskBaseViewModel>> item in dictionary)
					{
						CalendarDisplayViewModel section = new CalendarDisplayViewModel
						{
							SourceViewModel = new TaskBaseViewModel
							{
								Type = DisplayType.Section,
								Title = item.Key
							}
						};
						if (LocalSettings.Settings.ArrangeDisplayType == 0 && item.Key != Utils.GetString("Pinned"))
						{
							section.SectionStatusName = item.Key;
							section.SourceViewModel.Title = CacheManager.GetProjectById(item.Key)?.name;
						}
						bool folded = ArrangeSectionStatusDao.CheckSectionClosed(LocalSettings.Settings.ArrangeDisplayType, section.SectionStatusName ?? section.Title, section.Title);
						section.IsOpened = !folded;
						_viewModels.Add(section);
						List<TaskBaseViewModel> models = item.Value;
						if (item.Key == Utils.GetString("Pinned"))
						{
							List<SyncSortOrderModel> obj = await TaskSortOrderService.GetAsyncList("taskPinned", "all");
							Dictionary<string, long> pinOrderDict = new Dictionary<string, long>();
							foreach (SyncSortOrderModel item2 in obj)
							{
								pinOrderDict[item2.EntityId] = item2.SortOrder;
							}
							models.Sort(delegate(TaskBaseViewModel left, TaskBaseViewModel right)
							{
								long num = (pinOrderDict.ContainsKey(left.Id) ? pinOrderDict[left.Id] : 0);
								long value2 = (pinOrderDict.ContainsKey(right.Id) ? pinOrderDict[right.Id] : 0);
								return num.CompareTo(value2);
							});
						}
						else if (LocalSettings.Settings.ArrangeTaskDateType == 1)
						{
							models.Sort((TaskBaseViewModel a, TaskBaseViewModel b) => (a.StartDate.HasValue && b.StartDate.HasValue) ? DateSortHelper.CompareTaskByDate(a, b, outDateFirst: false) : 0);
						}
						if (LocalSettings.Settings.ArrangeDisplayType == 0)
						{
							List<CalendarDisplayViewModel> list2 = models.Select(CalendarDisplayViewModel.Build).ToList();
							if (LocalSettings.Settings.ExtraSettings.ShowParentInCalArrange)
							{
								Dictionary<string, CalendarDisplayViewModel> dictionary2 = new Dictionary<string, CalendarDisplayViewModel>();
								foreach (CalendarDisplayViewModel item3 in list2)
								{
									dictionary2[item3.TaskId] = item3;
									item3.IsOpened = !ArrangeSectionStatusDao.CheckSectionClosed(LocalSettings.Settings.ArrangeDisplayType, item3.TaskId);
								}
								foreach (CalendarDisplayViewModel item4 in list2)
								{
									string parentId = item4.SourceViewModel.ParentId;
									if (!string.IsNullOrEmpty(parentId) && dictionary2.TryGetValue(parentId, out var value))
									{
										item4.Parent = value;
										value.Children.Add(item4);
									}
								}
								foreach (CalendarDisplayViewModel item5 in list2)
								{
									if (item5.Parent == null)
									{
										section.Children.Add(item5);
										if (!folded)
										{
											List<CalendarDisplayViewModel> children = item5.GetChildren();
											_viewModels.Add(item5);
											_viewModels.AddRange(children);
										}
									}
								}
								continue;
							}
							foreach (CalendarDisplayViewModel item6 in list2)
							{
								section.Children.Add(item6);
								if (!folded)
								{
									_viewModels.Add(item6);
								}
							}
							continue;
						}
						foreach (CalendarDisplayViewModel item7 in models.Select(CalendarDisplayViewModel.Build))
						{
							section.Children.Add(item7);
							if (!folded)
							{
								_viewModels.Add(item7);
							}
						}
					}
				}
			}
			EmptyTaskGrid.Visibility = (_viewModels.Any() ? Visibility.Collapsed : Visibility.Visible);
			SetViewModels();
		}
		finally
		{
			_inAssemble = false;
		}
	}

	private Dictionary<string, List<TaskBaseViewModel>> GetTaskDictByPriority(IEnumerable<TaskBaseViewModel> tasks)
	{
		Dictionary<int, List<TaskBaseViewModel>> dictionary = new Dictionary<int, List<TaskBaseViewModel>>();
		foreach (TaskBaseViewModel task in tasks)
		{
			int key = ((task.Kind == "NOTE") ? (-1) : task.Priority);
			if (dictionary.ContainsKey(key))
			{
				dictionary[key].Add(task);
				continue;
			}
			dictionary[key] = new List<TaskBaseViewModel> { task };
		}
		IOrderedEnumerable<int> orderedEnumerable = dictionary.Keys.OrderByDescending((int k) => k);
		Dictionary<string, List<TaskBaseViewModel>> dictionary2 = new Dictionary<string, List<TaskBaseViewModel>>();
		foreach (int item in orderedEnumerable)
		{
			string priorityName = GetPriorityName(item);
			IOrderedEnumerable<TaskBaseViewModel> source = from t in dictionary[item]
				orderby t.ProjectOrder, t.SortOrder
				select t;
			dictionary2[priorityName] = source.ToList();
		}
		return dictionary2;
	}

	private void GetViewModelsByTag(List<TaskBaseViewModel> tasks, List<string> tagFilters)
	{
		List<TagModel> tags = CacheManager.GetTags();
		tags.Sort((TagModel a, TagModel b) => a.sortOrder.CompareTo(b.sortOrder));
		string text = Utils.GetString("NoTagsTitle");
		List<string> list = (from tag in tagFilters?.Where((string tag) => !string.IsNullOrEmpty(tag))
			select tag.ToLower()).Distinct().ToList() ?? new List<string>();
		bool flag = list.Count == 0;
		bool flag2 = list.Contains("*withtags");
		bool flag3 = flag || list.Contains("!tag");
		HashSet<string> specificTagFilters = new HashSet<string>(list.Where((string tag) => tag != "*withtags" && tag != "!tag" && tag != "*tag"));
		Dictionary<string, List<TaskBaseViewModel>> dictionary = tags.ToDictionary((TagModel tag) => tag.name, (TagModel tag) => new List<TaskBaseViewModel>());
		if (flag3)
		{
			dictionary.Add(text, new List<TaskBaseViewModel>());
		}
		foreach (TaskBaseViewModel task in tasks)
		{
			List<string> list2 = TagSerializer.ToTags(task.Tag);
			if (list2 == null || list2.Count == 0)
			{
				if (flag3)
				{
					dictionary[text].Add(task);
				}
				continue;
			}
			string arrangeDisplayTag = GetArrangeDisplayTag(dictionary.Keys, list2, flag || flag2, specificTagFilters);
			if (!string.IsNullOrEmpty(arrangeDisplayTag))
			{
				if (!dictionary.ContainsKey(arrangeDisplayTag))
				{
					dictionary.Add(arrangeDisplayTag, new List<TaskBaseViewModel>());
				}
				dictionary[arrangeDisplayTag].Add(task);
			}
		}
		foreach (KeyValuePair<string, List<TaskBaseViewModel>> item in dictionary)
		{
			List<TaskBaseViewModel> value = item.Value;
			if (value == null || value.Count <= 0)
			{
				continue;
			}
			CalendarDisplayViewModel calendarDisplayViewModel = new CalendarDisplayViewModel
			{
				SectionStatusName = ((item.Key == text) ? "!tag" : item.Key),
				SourceViewModel = new TaskBaseViewModel
				{
					Type = DisplayType.Section,
					Title = TagDataHelper.GetTagDisplayName(item.Key)
				}
			};
			_viewModels.Add(calendarDisplayViewModel);
			List<TaskBaseViewModel> value2 = item.Value;
			value2 = (from task in value2
				orderby task.ProjectOrder, task.SortOrder
				select task).ToList();
			if (LocalSettings.Settings.ArrangeTaskDateType == 1)
			{
				value2.Sort((TaskBaseViewModel a, TaskBaseViewModel b) => (a.StartDate.HasValue && b.StartDate.HasValue) ? DateSortHelper.CompareTaskByDate(a, b, outDateFirst: false) : 0);
			}
			bool flag4 = ArrangeSectionStatusDao.CheckSectionClosed(LocalSettings.Settings.ArrangeDisplayType, calendarDisplayViewModel.SectionStatusName, calendarDisplayViewModel.Title);
			foreach (CalendarDisplayViewModel item2 in value2.Select(CalendarDisplayViewModel.Build))
			{
				calendarDisplayViewModel.Children.Add(item2);
				if (!flag4)
				{
					_viewModels.Add(item2);
				}
				else
				{
					calendarDisplayViewModel.IsOpened = false;
				}
			}
		}
	}

	private static string GetArrangeDisplayTag(IEnumerable<string> displayTags, List<string> taskTags, bool showAnyTaggedTask, HashSet<string> specificTagFilters)
	{
		if (showAnyTaggedTask)
		{
			return displayTags.FirstOrDefault((string tag) => taskTags.Contains(tag)) ?? taskTags[0];
		}
		if (specificTagFilters == null || specificTagFilters.Count == 0)
		{
			return string.Empty;
		}
		return displayTags.FirstOrDefault((string tag) => specificTagFilters.Contains(tag) && taskTags.Contains(tag)) ?? taskTags.FirstOrDefault((string tag) => specificTagFilters.Contains(tag));
	}

	private void SetViewModels()
	{
		ItemsSourceHelper.SetItemsSource(TaskItems, _viewModels);
	}

	private string GetPriorityName(int taskPriority)
	{
		return taskPriority switch
		{
			-1 => Utils.GetString("Notes"), 
			0 => Utils.GetString("PriorityNull"), 
			1 => Utils.GetString("PriorityLow"), 
			3 => Utils.GetString("PriorityMedium"), 
			5 => Utils.GetString("PriorityHigh"), 
			_ => Utils.GetString("PriorityNull"), 
		};
	}

	private Dictionary<string, List<TaskBaseViewModel>> GetTaskDictByProject(List<TaskBaseViewModel> tasks)
	{
		Dictionary<string, List<TaskBaseViewModel>> dictionary = new Dictionary<string, List<TaskBaseViewModel>>();
		string pinned = Utils.GetString("Pinned");
		HashSet<string> hashSet = new HashSet<string>();
		foreach (TaskBaseViewModel task in tasks)
		{
			string key = (task.IsPinned ? pinned : task.ProjectId);
			if (task.IsPinned)
			{
				hashSet.Add(task.Id);
			}
			if (dictionary.ContainsKey(key))
			{
				dictionary[key].Add(task);
				continue;
			}
			dictionary[key] = new List<TaskBaseViewModel> { task };
		}
		List<string> list = dictionary.Keys.ToList();
		list.Sort(delegate(string a, string b)
		{
			if (a == pinned)
			{
				return -1;
			}
			if (b == pinned)
			{
				return 1;
			}
			ProjectModel projectById = CacheManager.GetProjectById(a);
			ProjectModel projectById2 = CacheManager.GetProjectById(b);
			return projectById?.CompareTo(projectById2) ?? 1;
		});
		Dictionary<string, List<TaskBaseViewModel>> dictionary2 = new Dictionary<string, List<TaskBaseViewModel>>();
		foreach (string item in list)
		{
			List<TaskBaseViewModel> list2 = dictionary[item];
			if (item != pinned && LocalSettings.Settings.ExtraSettings.ShowParentInCalArrange)
			{
				Dictionary<string, TaskBaseViewModel> dictionary3 = new Dictionary<string, TaskBaseViewModel>();
				foreach (TaskBaseViewModel item2 in list2)
				{
					dictionary3[item2.Id] = item2;
				}
				foreach (TaskBaseViewModel item3 in list2)
				{
					string parentId = item3.ParentId;
					while (!string.IsNullOrEmpty(parentId) && !dictionary3.ContainsKey(parentId) && !hashSet.Contains(parentId))
					{
						TaskBaseViewModel taskById = TaskCache.GetTaskById(parentId);
						if (taskById == null)
						{
							break;
						}
						dictionary3[taskById.Id] = taskById;
						parentId = taskById.ParentId;
					}
				}
				list2 = dictionary3.Values.ToList();
			}
			list2 = list2.OrderBy((TaskBaseViewModel t) => t.SortOrder).ToList();
			dictionary2[item] = list2.ToList();
		}
		return dictionary2;
	}

	public bool OnSelection()
	{
		return false;
	}

	public DateTime? DragSideOver(MouseEventArgs e, CalendarDisplayViewModel model, bool dragLeft)
	{
		return null;
	}

	public void SetBarDragging(bool b)
	{
	}

	public bool GetBarDragging()
	{
		return false;
	}

	public async void OnDragStart(CalendarDisplayViewModel model, MouseEventArgs e, bool fromArrange)
	{
	}

	public void NotifyModelDrop()
	{
		_viewModels?.Where((CalendarDisplayViewModel item) => item.Dragging).ToList().ForEach(delegate(CalendarDisplayViewModel item)
		{
			item.Dragging = false;
		});
	}

	public async Task OnSectionStatusChanged(bool isOpened, CalendarDisplayViewModel model)
	{
		if (_viewModels == null)
		{
			return;
		}
		List<CalendarDisplayViewModel> models = new List<CalendarDisplayViewModel>(_viewModels);
		int index = models.IndexOf(model);
		if (index >= 0)
		{
			List<CalendarDisplayViewModel> children = model.Children;
			if (children != null && children.Count > 0)
			{
				if (isOpened)
				{
					model.Children?.ForEach(delegate(CalendarDisplayViewModel child)
					{
						models.Insert(++index, child);
						child.GetChildren()?.ForEach(delegate(CalendarDisplayViewModel c)
						{
							models.Insert(++index, c);
						});
					});
				}
				else
				{
					model.Children?.ForEach(delegate(CalendarDisplayViewModel child)
					{
						models.Remove(child);
						child.GetChildren()?.ForEach(delegate(CalendarDisplayViewModel c)
						{
							models.Remove(c);
						});
					});
				}
			}
		}
		_viewModels = models;
		ItemsSourceHelper.SetItemsSource(TaskItems, _viewModels);
	}

	public void TryResetTagFilterText()
	{
		if (base.IsVisible && LocalSettings.Settings.ArrangeDisplayType == 1)
		{
			FilterText.Text = TagCardViewModel.ToNormalDisplayText(_projectFilter.Tags, 1);
		}
	}

	public static void ResetArrangeFilter(ProjectExtra filter)
	{
		_projectFilter = filter;
	}

	public bool IsFilterSelected()
	{
		return _projectFilter.FilterIds?.Any() ?? false;
	}

	private void OnFilterDateClick(object sender, MouseButtonEventArgs e)
	{
		if (filterPopup == null)
		{
			filterPopup = new CalArrangeDateFilterPopup(DateFilterPanel);
			CalArrangeDateFilterPopup calArrangeDateFilterPopup = filterPopup;
			calArrangeDateFilterPopup.OnClosed = (Action)Delegate.Combine(calArrangeDateFilterPopup.OnClosed, new Action(OnFilterPopupClosed));
			filterPopup.Show();
		}
	}

	private void OnFilterPopupClosed()
	{
		filterPopup.UnRegistEvent();
		CalArrangeDateFilterPopup calArrangeDateFilterPopup = filterPopup;
		calArrangeDateFilterPopup.OnClosed = (Action)Delegate.Remove(calArrangeDateFilterPopup.OnClosed, new Action(OnFilterPopupClosed));
		filterPopup = null;
	}

	private void OnSortTypeChanged(object sender, GroupTitleViewModel e)
	{
		if (LocalSettings.Settings.ArrangeDisplayType != e.Index)
		{
			LocalSettings.Settings.ArrangeDisplayType = e.Index;
			UserActCollectUtils.AddClickEvent("calendar", "arrangement_category", (e.Index == 0) ? "list" : ((e.Index == 1) ? "tag" : "priority"));
			SetFilterText();
			ListFilter.Visibility = ((LocalSettings.Settings.ArrangeDisplayType == 2) ? Visibility.Collapsed : Visibility.Visible);
			switch (LocalSettings.Settings.ArrangeDisplayType)
			{
			case 0:
				AllPath.SetResourceReference(Path.DataProperty, "Ic.ProjectList.All");
				break;
			case 1:
				AllPath.SetResourceReference(Path.DataProperty, "Ic.Tag.AllTag");
				break;
			}
			LoadTask();
		}
	}

	private void OnSortTypeChanged(object sender, EventArgs e)
	{
		if (SwitchTitle.GetSelectedIndex() != LocalSettings.Settings.ArrangeDisplayType)
		{
			SwitchTitle.SetSelectedIndex(LocalSettings.Settings.ArrangeDisplayType);
			SetFilterText();
			ListFilter.Visibility = ((LocalSettings.Settings.ArrangeDisplayType == 2) ? Visibility.Collapsed : Visibility.Visible);
			LoadTask();
		}
	}

	public void ClearItems()
	{
	}

	public void SetInDisplay(bool show)
	{
		InDisplay = show;
		if (show)
		{
			RemoveInvalidProjectFilters();
			SetFilterText();
			LoadTask();
		}
		else
		{
			ClearItems();
		}
	}

	public void RemoveItem(CalendarDisplayViewModel model)
	{
	}

	public async Task OnBatchDragDrop(List<string> selectedIds)
	{
		if (!(base.Opacity > 0.0) || !MaskBorder.IsVisible)
		{
			return;
		}
		foreach (string selectedId in selectedIds)
		{
			if (LocalSettings.Settings.ArrangeTaskDateType == 0)
			{
				await TaskService.ClearDate(selectedId);
				continue;
			}
			TaskBaseViewModel taskById = TaskCache.GetTaskById(selectedId);
			if (taskById != null && taskById.StartDate >= DateTime.Today)
			{
				await TaskService.SetDateAndClearRepeat(selectedId, DateTime.Today.AddDays(-1.0), handleReminder: false);
			}
		}
		MaskBorder.Visibility = Visibility.Collapsed;
	}

	public async Task OnDragDrop(TimeDataModel origin)
	{
		if (!(base.Opacity > 0.0) || !MaskBorder.IsVisible)
		{
			return;
		}
		if (LocalSettings.Settings.ArrangeTaskDateType == 0 || origin.StartDate >= DateTime.Today)
		{
			string id = origin.TaskId;
			if (origin.RepeatFrom != "1" && !string.IsNullOrEmpty(origin.RepeatFlag) && !origin.RepeatFlag.Contains("COUNT=1"))
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(id);
				if (taskModel != null && origin.StartDate.HasValue && taskModel.startDate.HasValue)
				{
					if (!(taskModel.startDate.Value.Date != origin.StartDate.Value.Date))
					{
						await TaskService.SkipCurrentRecurrence(id, notify: true, null, toast: false, handleCount: true);
					}
					else
					{
						TimeData timeData = new TimeData(taskModel, null);
						if (!timeData.ExDates.Contains(origin.StartDate.Value.Date.ToString("yyyyMMdd", App.Ci)))
						{
							timeData.ExDates.Add(origin.StartDate.Value.Date.ToString("yyyyMMdd", App.Ci));
							timeData.RepeatFlag = RRuleUtils.GetNextCount(timeData.RepeatFlag, handleEbbingCount: false);
							await TaskService.SetDate(id, timeData, handleReminder: false);
						}
					}
					id = (await TaskService.CopyTask(id, this, notify: true, delayNotify: true))?.id;
				}
			}
			if (LocalSettings.Settings.ArrangeTaskDateType != 0)
			{
				await TaskService.SetDateAndClearRepeat(id, DateTime.Today.AddDays(-1.0), handleReminder: false, origin.RepeatFrom != "1");
			}
			else
			{
				await TaskService.ClearDate(id);
			}
		}
		MaskBorder.Visibility = Visibility.Collapsed;
	}

	private IToastShowWindow GetToastWindow()
	{
		return Utils.FindParent<IToastShowWindow>(this);
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		Utils.FindParent<CalendarControl>(this)?.ShowOrHideArrange();
	}

	private void OnDragEnter()
	{
		if (!Equals(TickDragHelper.DragFrom))
		{
			bool flag = false;
			object dragData = TickDragHelper.DragData;
			if (!(dragData is DisplayItemModel displayItemModel))
			{
				if (!(dragData is CalendarDisplayViewModel calendarDisplayViewModel))
				{
					if (dragData is TimelineCellViewModel timelineCellViewModel && timelineCellViewModel.DisplayModel.IsTaskOrNote && timelineCellViewModel.DisplayModel.Editable)
					{
						flag = true;
					}
				}
				else if (calendarDisplayViewModel.IsTaskOrNote)
				{
					flag = true;
				}
			}
			else if (displayItemModel.IsTaskOrNote && displayItemModel.Enable)
			{
				flag = true;
			}
			if (flag)
			{
				MaskBorder.Visibility = Visibility.Visible;
			}
		}
		TickDragHelper.ChangeDragContent(TickDragHelper.DragData, TickDragHelper.DragWidth);
	}

	private void OnDragLeave()
	{
		MaskBorder.Visibility = Visibility.Collapsed;
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
		if (_dragOver != flag)
		{
			if (!flag)
			{
				OnDragLeave();
			}
			else
			{
				OnDragEnter();
			}
			_dragOver = flag;
		}
	}

	private void OnDrop(object sender, MouseEventArgs e)
	{
		if (!Equals(TickDragHelper.DragFrom))
		{
			TaskBaseViewModel taskBaseViewModel = null;
			double value = 0.0;
			object dragData = TickDragHelper.DragData;
			if (!(dragData is DisplayItemModel displayItemModel))
			{
				if (!(dragData is CalendarDisplayViewModel calendarDisplayViewModel))
				{
					if (dragData is TimelineCellViewModel timelineCellViewModel && timelineCellViewModel.DisplayModel.IsTaskOrNote && timelineCellViewModel.DisplayModel.Editable)
					{
						taskBaseViewModel = timelineCellViewModel.DisplayModel;
					}
				}
				else if (calendarDisplayViewModel.IsTaskOrNote)
				{
					taskBaseViewModel = calendarDisplayViewModel.SourceViewModel;
					value = calendarDisplayViewModel.RepeatDiff;
				}
			}
			else if (displayItemModel.IsTaskOrNote && displayItemModel.Enable)
			{
				taskBaseViewModel = displayItemModel.SourceViewModel;
			}
			HashSet<string> draggingTaskIds = TickDragHelper.DraggingTaskIds;
			if (draggingTaskIds != null && draggingTaskIds.Count > 1)
			{
				OnBatchDragDrop(TickDragHelper.DraggingTaskIds.ToList());
			}
			else if (taskBaseViewModel != null)
			{
				TimeDataModel origin = new TimeDataModel
				{
					TaskId = taskBaseViewModel.Id,
					StartDate = taskBaseViewModel.StartDate?.AddDays(value),
					DueDate = taskBaseViewModel.DueDate?.AddDays(value),
					RepeatFlag = taskBaseViewModel.RepeatFlag,
					RepeatFrom = taskBaseViewModel.RepeatFrom,
					IsAllDay = taskBaseViewModel.IsAllDay
				};
				OnDragDrop(origin);
			}
		}
		MaskBorder.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/arrangetaskpanel.xaml", UriKind.Relative);
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
			TopGrid = (Grid)target;
			break;
		case 2:
			DateFilterPanel = (StackPanel)target;
			break;
		case 3:
			((Border)target).MouseLeftButtonUp += OnFilterDateClick;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += OnCloseClick;
			break;
		case 5:
			SwitchTitle = (GroupTitle2)target;
			break;
		case 6:
			ListGrid = (Grid)target;
			break;
		case 7:
			ListFilter = (Grid)target;
			break;
		case 8:
			FilterTextBorder = (StackPanel)target;
			FilterTextBorder.MouseLeftButtonUp += OnFilterProject;
			break;
		case 9:
			AllPath = (Path)target;
			break;
		case 10:
			FilterText = (EmjTextBlock)target;
			break;
		case 11:
			ProjectFilterPopup = (EscPopup)target;
			break;
		case 12:
			TagFilterPopup = (EscPopup)target;
			break;
		case 13:
			ItemsGrid = (Grid)target;
			break;
		case 14:
			TaskItems = (ListView)target;
			break;
		case 15:
			EmptyTaskGrid = (Grid)target;
			break;
		case 16:
			MaskBorder = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
