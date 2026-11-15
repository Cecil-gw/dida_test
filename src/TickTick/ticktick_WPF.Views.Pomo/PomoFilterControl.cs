using System;
using System.CodeDom.Compiler;
using System.Collections;
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
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.Pomo;

public class PomoFilterControl : UserControl, IComponentConnector, IStyleConnector
{
	private ProjectOrGroupPopup _popup;

	private ProjectIdentity _projectIdentity;

	public bool ShowComplete;

	private readonly Popup _parentPopup;

	private bool _inFocus;

	private bool _showDetail;

	private HashSet<string> _linkedIds;

	private List<TimerModel> _timers;

	private List<PomoTask> _recent;

	private string _selectedGroupTitle = "Task";

	private bool _loadTimers;

	private bool _loadMoreTask;

	private bool _loadMoreHabit;

	private bool _loadMoreTimer;

	private bool _completingTask;

	private List<HabitModel> _habits;

	private static List<PomoTask> _remoteRecentTasks;

	private int[] _tabIndex = new int[3];

	private List<HabitCheckInModel> _checkIns;

	internal PomoFilterControl Root;

	internal Grid Container;

	internal Grid DetailGrid;

	internal TaskDetailPomoSummaryControl PomoSummary;

	internal Button CompleteButton;

	internal Grid ListGrid;

	internal GroupTitle GroupTitle;

	internal Grid SearchGrid;

	internal EmojiEditor SearchText;

	internal Path SearchIcon;

	internal Border ExitSearchIcon;

	internal Border ClearIcon;

	internal StackPanel ProjectName;

	internal EscPopup ProjectFilterPopup;

	internal Grid SelectTaskPanel;

	internal ListView TaskList;

	internal ContentControl EmptyPanel;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	private FocusViewModel Data => base.DataContext as FocusViewModel;

	public event EventHandler<DisplayItemModel> ItemSelected;

	public PomoFilterControl(Popup popup, bool inFocus = true, bool showDetail = false, List<string> linkedIds = null, bool loadTimers = true)
	{
		_loadTimers = loadTimers;
		_parentPopup = popup;
		_showDetail = showDetail;
		_linkedIds = ((linkedIds == null) ? new HashSet<string>() : new HashSet<string>(linkedIds));
		InitializeComponent();
		base.Loaded += OnLoaded;
		_inFocus = inFocus;
		popup.Child = this;
		SearchText.EditBox.GotFocus += OnSearchFocus;
		InitEmptyView();
	}

	private void InitEmptyView()
	{
		EmptyWithTextView emptyWithTextView = new EmptyWithTextView(168.0);
		emptyWithTextView.SetData(Utils.GetString("NoTasks"), Utils.GetString("FocusLinkEmptyDesc"), new EmptyIconsSourceModel("FocusTaskEmpty", new List<int> { 2, 4, 5 }));
		EmptyPanel.Content = emptyWithTextView;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		PomoTaskDetailModel taskModel;
		if (_showDetail && Data != null && !string.IsNullOrEmpty(Data.FocusId) && TickFocusManager.Working)
		{
			taskModel = await PomoTaskDetailModel.GetDetailModel(Data.ObjId);
			if (taskModel != null)
			{
				PomodoroSummaryModel pomodoroSummaryModel = await PomoService.GetAdjustPomoSummary(Data.ObjId);
				if (!LocalSettings.Settings.EnableFocus || pomodoroSummaryModel == null)
				{
					goto IL_01a5;
				}
				if (pomodoroSummaryModel.count <= 0 && pomodoroSummaryModel.StopwatchDuration + pomodoroSummaryModel.PomoDuration < 30)
				{
					List<object[]> focuses = pomodoroSummaryModel.focuses;
					if ((focuses == null || focuses.Count <= 0) && pomodoroSummaryModel.estimatedPomo <= 0 && pomodoroSummaryModel.EstimatedDuration <= 0)
					{
						goto IL_01a5;
					}
				}
				PomoSummary.SetData(pomodoroSummaryModel);
				PomoSummary.Visibility = Visibility.Visible;
				goto IL_01b1;
			}
		}
		LoadRecentTask();
		TryLoadTaskList();
		return;
		IL_01b1:
		DetailGrid.Visibility = Visibility.Visible;
		ListGrid.Visibility = Visibility.Collapsed;
		DetailGrid.DataContext = taskModel;
		CompleteButton.Content = (TickFocusManager.IsPomo ? Utils.GetString("PublicComplete") : Utils.GetString("CompleteTimingTask"));
		return;
		IL_01a5:
		PomoSummary.Visibility = Visibility.Collapsed;
		goto IL_01b1;
	}

	private async Task LoadRecentTask()
	{
		_remoteRecentTasks = await Communicator.GetRecentFocusTasks();
		if (_remoteRecentTasks != null && _remoteRecentTasks.Count > 0)
		{
			if (!GroupTitle.Titles.StartsWith("Recent"))
			{
				TryLoadTaskList();
			}
			else if (_selectedGroupTitle == "Recent")
			{
				LoadRecent();
			}
		}
	}

	private async Task TryLoadTaskList()
	{
		ListGrid.Visibility = Visibility.Visible;
		DetailGrid.Visibility = Visibility.Collapsed;
		List<TabBarModel> source = (LocalSettings.Settings.UserPreference?.desktopTabBars?.bars ?? TabBarModel.InitTabBars()).ToList();
		source = source.Where((TabBarModel t) => t.name.ToUpper() == "TASK" || t.name.ToUpper() == "HABIT" || t.name.ToUpper() == "POMO").ToList();
		source.Sort((TabBarModel a, TabBarModel b) => a.sortOrder.CompareTo(b.sortOrder));
		_tabIndex = new int[3];
		for (int num = 0; num < source.Count; num++)
		{
			if (source[num].name.ToUpper() == "TASK")
			{
				_tabIndex[num] = 1;
			}
			if (source[num].name.ToUpper() == "HABIT")
			{
				_tabIndex[num] = 2;
			}
			if (source[num].name.ToUpper() == "POMO")
			{
				_tabIndex[num] = 3;
			}
		}
		_recent = await PomoTaskDao.GetRecentPomoTasks(6);
		string titles = ((_remoteRecentTasks?.Any((PomoTask r) => !string.IsNullOrEmpty(r.GetId())) ?? _recent?.Any((PomoTask r) => !string.IsNullOrEmpty(r.GetId())) ?? false) ? "Recent" : "");
		List<HabitModel> habits = ((!LocalSettings.Settings.ShowHabit) ? null : (await HabitService.GetHabitsbyStatus(0)));
		_habits = habits;
		_habits = _habits?.Where((HabitModel h) => !h.IsSkipToday()).ToList();
		List<HabitModel> habits2 = _habits;
		if (habits2 != null && habits2.Count > 0)
		{
			_checkIns = await HabitCheckInDao.GetCheckInsInSpan(DateTime.Today, DateTime.Today.AddDays(1.0));
		}
		List<TimerModel> timers = ((!LocalSettings.Settings.EnableFocus || !_loadTimers) ? null : (from a in await TimerDao.GetDisplayTimersAsync()
			where a.Status == 0
			orderby a.SortOrder
			select a).ToList());
		_timers = timers;
		for (int num2 = 0; num2 < 3; num2++)
		{
			switch (_tabIndex[num2])
			{
			case 1:
				titles = titles + ((!string.IsNullOrEmpty(titles)) ? "|" : "") + "Task";
				break;
			case 2:
			{
				List<HabitModel> habits3 = _habits;
				if (habits3 != null && habits3.Count > 0)
				{
					titles = titles + ((!string.IsNullOrEmpty(titles)) ? "|" : "") + "Habit";
				}
				break;
			}
			case 3:
			{
				List<TimerModel> timers2 = _timers;
				if (timers2 != null && timers2.Count > 0)
				{
					titles = titles + ((!string.IsNullOrEmpty(titles)) ? "|" : "") + "CommonTimer";
				}
				break;
			}
			}
		}
		GroupTitle.Titles = titles;
		GroupTitle.SelectedTitle = _selectedGroupTitle;
		_selectedGroupTitle = GroupTitle.SelectedTitle;
		DetailGrid.DataContext = null;
		if (Data != null)
		{
			_projectIdentity = Data.Identity;
			ReloadProjectIdentity(_projectIdentity);
		}
		if (string.IsNullOrEmpty(SearchText.Text))
		{
			GroupTitle.Visibility = ((!GroupTitle.Titles.Contains("|")) ? Visibility.Collapsed : Visibility.Visible);
			SearchIcon.Visibility = Visibility.Visible;
			ProjectName.Visibility = Visibility.Visible;
			ExitSearchIcon.Visibility = Visibility.Collapsed;
			LoadDisplayItems();
		}
		else
		{
			GroupTitle.Visibility = Visibility.Collapsed;
			SearchIcon.Visibility = Visibility.Collapsed;
			ProjectName.Visibility = Visibility.Collapsed;
			ExitSearchIcon.Visibility = Visibility.Visible;
			await Task.Delay(150);
			SearchText.Focus();
			OnSearchTextChanged(null, null);
		}
	}

	public void SetTheme(string theme)
	{
		theme = ((theme == "dark") ? "dark" : "light");
		ThemeUtil.SetTheme(theme, this);
	}

	private void OnFilterClick(object sender, MouseButtonEventArgs e)
	{
		ProjectExtra projectExtra = new ProjectExtra();
		if (_projectIdentity is NormalProjectIdentity)
		{
			projectExtra.ProjectIds.Add(_projectIdentity.Id);
		}
		if (_projectIdentity is TagProjectIdentity)
		{
			projectExtra.Tags.Add(_projectIdentity.Id);
		}
		if (_projectIdentity is FilterProjectIdentity)
		{
			projectExtra.FilterIds.Add(_projectIdentity.Id);
		}
		if (_projectIdentity is SmartProjectIdentity)
		{
			projectExtra.SmartIds.Add(_projectIdentity.Id);
		}
		if (_popup == null)
		{
			_popup = new ProjectOrGroupPopup(ProjectFilterPopup, projectExtra, new ProjectSelectorExtra
			{
				showAll = false,
				showFilters = true,
				showTags = true,
				showSmartProjects = true,
				onlyShowPermission = true,
				batchMode = false,
				canSelectGroup = false,
				showSmartAll = false,
				showFilterGroup = false,
				showListGroup = true
			});
			_popup.ItemSelect += OnFilterItemSelect;
			_popup.Show();
		}
		else
		{
			ProjectFilterPopup.IsOpen = true;
			_popup.SetData(projectExtra);
		}
	}

	private async void OnFilterItemSelect(object sender, SelectableItemViewModel project)
	{
		if (!(project is ticktick_WPF.Views.Misc.ProjectGroupViewModel))
		{
			ProjectFilterPopup.IsOpen = false;
			await LoadProjectIdentity(project, project.Type);
			LoadTasks();
		}
	}

	private async Task LoadProjectIdentity(SelectableItemViewModel project, string type)
	{
		switch (type)
		{
		default:
			if (type == null)
			{
				_projectIdentity = ProjectIdentity.CreateInboxProject();
			}
			else
			{
				_projectIdentity = GetDefaultProject();
			}
			break;
		case "normal":
			_projectIdentity = ProjectIdHelper.GetProjectIdentity(project.Id);
			break;
		case "smart":
			_projectIdentity = ProjectIdentity.CreateSmartIdentity(project.Id);
			break;
		case "filter":
			_projectIdentity = ProjectIdHelper.GetFilterIdentity(project.Id);
			break;
		case "tag":
			_projectIdentity = ProjectIdHelper.GetTagIdentity(project.Id);
			break;
		}
		if (_projectIdentity == null)
		{
			_projectIdentity = GetDefaultProject();
		}
		Data.Identity = _projectIdentity;
	}

	private async Task ReloadProjectIdentity(ProjectIdentity identity)
	{
		if (!(identity is NormalProjectIdentity))
		{
			if (!(identity is SmartProjectIdentity))
			{
				if (!(identity is FilterProjectIdentity))
				{
					if (identity is TagProjectIdentity)
					{
						_projectIdentity = ProjectIdHelper.GetTagIdentity(identity.Id);
					}
					else
					{
						_projectIdentity = GetDefaultProject();
					}
				}
				else
				{
					_projectIdentity = ProjectIdHelper.GetFilterIdentity(identity.Id);
				}
			}
			else
			{
				_projectIdentity = ProjectIdentity.CreateSmartIdentity(identity.Id);
			}
		}
		else
		{
			_projectIdentity = ProjectIdHelper.GetProjectIdentity(identity.Id);
		}
		if (_projectIdentity == null)
		{
			_projectIdentity = GetDefaultProject();
		}
		Data.Identity = _projectIdentity;
	}

	private ProjectIdentity GetDefaultProject()
	{
		return ProjectIdentity.CreateSmartIdentity("_special_id_today");
	}

	private async void LoadTasks()
	{
		List<TaskBaseViewModel> models = (await ProjectTaskDataProvider.GetDisplayModels(_projectIdentity)).Where((TaskBaseViewModel m) => m.Editable).ToList();
		ObservableCollection<DisplayItemModel> displayModels = BuildDisplayItemModels(models);
		List<ColumnModel> list = ((_projectIdentity.SortOption.groupByType != Constants.SortType.sortOrder) ? new List<ColumnModel>() : (await ColumnDao.GetColumnsByProjectId(_projectIdentity.GetProjectId())));
		List<ColumnModel> columns = list;
		displayModels = await SortHelper.Sort(displayModels, _projectIdentity, showFold: true, showLoadMore: false, columns);
		DisplayItemModel displayItemModel = null;
		foreach (DisplayItemModel item in displayModels)
		{
			if (item.Selected)
			{
				displayItemModel = item;
			}
		}
		ReloadTask(displayModels.ToList());
		if (displayItemModel != null)
		{
			TaskList.ScrollIntoView(displayItemModel);
		}
	}

	private ObservableCollection<DisplayItemModel> BuildDisplayItemModels(IEnumerable<TaskBaseViewModel> models)
	{
		ObservableCollection<DisplayItemModel> observableCollection = new ObservableCollection<DisplayItemModel>();
		foreach (TaskBaseViewModel model in models)
		{
			if (model.Editable)
			{
				switch (model.Type)
				{
				case DisplayType.Habit:
					model.StartDate = null;
					model.DueDate = null;
					break;
				case DisplayType.CheckItem:
				case DisplayType.Event:
				case DisplayType.Course:
					continue;
				}
				DisplayItemModel displayItemModel = new DisplayItemModel(model);
				displayItemModel.Selected = (displayItemModel.IsTask && displayItemModel.TaskId == Data.FocusId) || (displayItemModel.IsNote && displayItemModel.TaskId == Data.FocusId) || (displayItemModel.IsHabit && displayItemModel.Habit.Id == Data.FocusId);
				displayItemModel.DisplayColor = GetPriorityColor(displayItemModel.Priority).ToString();
				displayItemModel.Linked = _linkedIds.Contains(displayItemModel.Id);
				observableCollection.Add(displayItemModel);
			}
		}
		return observableCollection;
	}

	private SolidColorBrush GetPriorityColor(int modelPriority)
	{
		string text = modelPriority switch
		{
			0 => "BaseColorOpacity40", 
			1 => "PriorityLowColor", 
			3 => "PriorityMiddleColor", 
			5 => "PriorityHighColor", 
			_ => "BaseColorOpacity40", 
		};
		if (FindResource(text) is SolidColorBrush result)
		{
			return result;
		}
		return ThemeUtil.GetColor(text);
	}

	private void ReloadTask(List<DisplayItemModel> tasks)
	{
		if (tasks == null || tasks.Count <= 0)
		{
			EmptyPanel.Visibility = Visibility.Visible;
			SelectTaskPanel.Visibility = Visibility.Collapsed;
		}
		else
		{
			EmptyPanel.Visibility = Visibility.Collapsed;
			SelectTaskPanel.Visibility = Visibility.Visible;
			TaskList.ItemsSource = new ObservableCollection<DisplayItemModel>(tasks);
		}
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: DisplayItemModel dataContext }))
		{
			return;
		}
		if (!dataContext.Linked)
		{
			_parentPopup.IsOpen = false;
			if (_inFocus)
			{
				string actCType = TickFocusManager.GetActCType();
				UserActCollectUtils.AddClickEvent("focus", actCType, string.IsNullOrEmpty(TickFocusManager.Config.FocusVModel.FocusId) ? "select_task" : "switch_task");
				TickFocusManager.OnFocusIdSelected(dataContext.Id, (dataContext.Type == DisplayType.Timer) ? 2 : (dataContext.IsHabit ? 1 : 0));
			}
			else
			{
				Data.SetFocusId(dataContext.Id);
				this.ItemSelected?.Invoke(null, dataContext);
			}
		}
		else
		{
			Toast(Utils.GetString(dataContext.IsHabit ? "HabitLinked" : "TaskLinked"));
		}
	}

	private void CheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is CheckBox checkBox)
		{
			checkBox.Tag = "1";
		}
	}

	private async void OnItemCheckChanged(object sender, RoutedEventArgs e)
	{
		if (sender is CheckBox { Tag: string tag } checkBox && tag == "1" && checkBox.DataContext is PomoSubtaskDetailModel pomoSubtaskDetailModel)
		{
			checkBox.Tag = "0";
			pomoSubtaskDetailModel.Status = ((pomoSubtaskDetailModel.Status == 0) ? 1 : 0);
			await NotifyItemStatusChanged(pomoSubtaskDetailModel);
		}
	}

	private async Task NotifyItemStatusChanged(PomoSubtaskDetailModel model)
	{
		if (model.Status == 1)
		{
			model.CompleteDate = DateTime.Now;
		}
		await TaskDetailItemService.CompleteCheckItem(model.Id, handleTask: false);
		if (DetailGrid.DataContext is PomoTaskDetailModel pomoTaskDetailModel)
		{
			ObservableCollection<PomoSubtaskDetailModel> items = pomoTaskDetailModel.Items;
			if (items != null && items.Count > 0)
			{
				pomoTaskDetailModel.SortItems();
			}
		}
	}

	private async void OnCompleteClick(object sender, RoutedEventArgs e)
	{
		if (_completingTask)
		{
			return;
		}
		object dataContext = DetailGrid.DataContext;
		if (!(dataContext is PomoTaskDetailModel task))
		{
			return;
		}
		_completingTask = true;
		CompleteButton.IsEnabled = false;
		try
		{
			bool flag = !TickFocusManager.IsPomo && FocusTimer.IsWorking;
			if (flag)
			{
				flag = !(await FocusTimer.TryStopTiming(waitSync: false));
			}
			if (!flag)
			{
				await TaskService.SetTaskStatus(task.TaskId, 2);
				SyncManager.TryDelaySync();
				_parentPopup.IsOpen = false;
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
			Toast(Utils.GetString("OperateFailed"));
		}
		finally
		{
			_completingTask = false;
			CompleteButton.IsEnabled = true;
		}
	}

	private void OnSelectTaskClick(object sender, RoutedEventArgs e)
	{
		if (!_completingTask)
		{
			TryLoadTaskList();
		}
	}

	private void OnOpenPathClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (!(sender is Grid { DataContext: DisplayItemModel dataContext }))
		{
			return;
		}
		List<DisplayItemModel> childrenModels = dataContext.GetChildrenModels(getAll: false);
		IEnumerable itemsSource = TaskList.ItemsSource;
		ObservableCollection<DisplayItemModel> models = itemsSource as ObservableCollection<DisplayItemModel>;
		if (models == null || childrenModels == null)
		{
			return;
		}
		if (dataContext.IsOpen)
		{
			foreach (DisplayItemModel item in childrenModels)
			{
				models.Remove(item);
			}
			dataContext.IsOpen = false;
			return;
		}
		int i = 1;
		int index = models.IndexOf(dataContext);
		childrenModels.ForEach(delegate(DisplayItemModel child)
		{
			models.Insert(index + i, child);
			i++;
		});
		dataContext.IsOpen = true;
	}

	public void ClearItemSelectedEvent()
	{
		this.ItemSelected = null;
	}

	private void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	private void OnClearClick(object sender, MouseButtonEventArgs e)
	{
		SearchText.Text = "";
	}

	private void OnSearchTextChanged(object sender, EventArgs e)
	{
		ClearIcon.Visibility = (string.IsNullOrEmpty(SearchText.Text) ? Visibility.Collapsed : Visibility.Visible);
		List<DisplayItemModel> displayModels = new List<DisplayItemModel>();
		string keyword;
		List<string> keys;
		if (!string.IsNullOrEmpty(SearchText.Text))
		{
			keyword = SearchText.Text.ToLower().Trim();
			keys = keyword.Split(' ').ToList();
			keys.Remove("");
			for (int i = 0; i < 3; i++)
			{
				switch (_tabIndex[i])
				{
				case 1:
					AddTasks();
					break;
				case 2:
					AddHabits();
					break;
				case 3:
					AddTimers();
					break;
				}
			}
		}
		if (displayModels.Count > 0)
		{
			displayModels.RemoveAt(displayModels.Count - 1);
		}
		EmptyPanel.Visibility = Visibility.Collapsed;
		SelectTaskPanel.Visibility = Visibility.Visible;
		TaskList.ItemsSource = new ObservableCollection<DisplayItemModel>(displayModels);
		void AddHabits()
		{
			List<HabitModel> list = _habits?.Where((HabitModel habitModel) => habitModel.SyncStatus != -1 && SearchHelper.KeyWordMatched(habitModel.Name, keyword, keys)).ToList();
			if (list != null && list.Any())
			{
				list.Sort((HabitModel a, HabitModel b) => a.SortOrder.CompareTo(b.SortOrder));
				Section section = new Section
				{
					SectionId = "Habit",
					Name = Utils.GetString("Habit")
				};
				DisplayItemModel displayItemModel = new DisplayItemModel(new TaskBaseViewModel(section))
				{
					Section = section,
					IsOpen = true
				};
				displayModels.Add(displayItemModel);
				List<DisplayItemModel> list2 = new List<DisplayItemModel>();
				foreach (HabitModel habit in list)
				{
					HabitCheckInModel habitCheckInModel = _checkIns?.FirstOrDefault((HabitCheckInModel c) => c.HabitId == habit.Id);
					DisplayItemModel displayItemModel2 = new DisplayItemModel(new HabitBaseViewModel(habit)
					{
						StartDate = null
					});
					if (habitCheckInModel != null && (habitCheckInModel.Value >= habitCheckInModel.Goal || habitCheckInModel.CheckStatus == 1))
					{
						displayItemModel2.SourceViewModel.Status = 2;
						displayItemModel2.SourceViewModel.CompletedTime = habitCheckInModel.CheckinTime;
					}
					displayItemModel2.Selected = displayItemModel2.Id == Data.FocusId;
					displayItemModel2.DisplayColor = GetPriorityColor(displayItemModel2.Priority).ToString();
					displayItemModel.Section.Children.Add(displayItemModel2);
					list2.Add(displayItemModel2);
				}
				list2.Sort((DisplayItemModel a, DisplayItemModel b) => (a.Status == b.Status && a.CompletedTime.HasValue && b.CompletedTime.HasValue) ? b.CompletedTime.Value.CompareTo(a.CompletedTime.Value) : a.Status.CompareTo(b.Status));
				for (int num = 0; num < list2.Count; num++)
				{
					if (num < 5 || _loadMoreHabit)
					{
						displayModels.Add(list2[num]);
					}
				}
				if (displayItemModel.Section.Children.Count > 5 && !_loadMoreHabit)
				{
					DisplayItemModel item = new DisplayItemModel
					{
						SourceViewModel = new TaskBaseViewModel
						{
							Type = DisplayType.LoadMore
						},
						Parent = displayItemModel
					};
					displayModels.Add(item);
				}
				displayModels.Add(DisplayItemModel.BuildSplit());
			}
		}
		void AddTasks()
		{
			List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
			List<TaskBaseViewModel> list2 = new List<TaskBaseViewModel>();
			List<TaskBaseViewModel> list3 = new List<TaskBaseViewModel>();
			List<TaskBaseViewModel> list4 = new List<TaskBaseViewModel>();
			foreach (TaskBaseViewModel item3 in TaskCache.GetAllTask())
			{
				if (item3.Deleted == 0 && item3.Editable)
				{
					int num = SearchHelper.KeyWordMatch(item3.Title.ToLower(), keyword, keys);
					if (num == 1)
					{
						if (item3.Kind != "NOTE")
						{
							list.Add(item3);
						}
						else
						{
							list3.Add(item3);
						}
					}
					if (num == 2)
					{
						if (item3.Kind != "NOTE")
						{
							list2.Add(item3);
						}
						else
						{
							list4.Add(item3);
						}
					}
				}
			}
			List<TaskBaseViewModel> searchModels = TaskDisplayService.GetSearchModels(list, list2);
			if (searchModels.Any())
			{
				Section section = new Section
				{
					SectionId = "Task",
					Name = Utils.GetString("Task")
				};
				DisplayItemModel displayItemModel = new DisplayItemModel(new TaskBaseViewModel(section))
				{
					Section = section,
					IsOpen = true
				};
				displayModels.Add(displayItemModel);
				foreach (TaskBaseViewModel item4 in searchModels)
				{
					DisplayItemModel displayItemModel2 = new DisplayItemModel(item4);
					displayItemModel2.Selected = displayItemModel2.Id == Data.FocusId;
					displayItemModel2.DisplayColor = GetPriorityColor(displayItemModel2.Priority).ToString();
					displayItemModel.Section.Children.Add(displayItemModel2);
					if (displayItemModel.Section.Children.Count <= 5 || _loadMoreTask)
					{
						displayModels.Add(displayItemModel2);
					}
				}
				if (displayItemModel.Section.Children.Count > 5 && !_loadMoreTask)
				{
					DisplayItemModel item = new DisplayItemModel
					{
						SourceViewModel = new TaskBaseViewModel
						{
							Type = DisplayType.LoadMore
						},
						Parent = displayItemModel
					};
					displayModels.Add(item);
				}
				displayModels.Add(DisplayItemModel.BuildSplit());
			}
			List<TaskBaseViewModel> searchModels2 = TaskDisplayService.GetSearchModels(list3, list4);
			if (searchModels2.Any())
			{
				Section section2 = new Section
				{
					SectionId = "Note",
					Name = Utils.GetString("Notes")
				};
				DisplayItemModel displayItemModel3 = new DisplayItemModel(new TaskBaseViewModel(section2))
				{
					Section = section2,
					IsOpen = true
				};
				displayModels.Add(displayItemModel3);
				foreach (TaskBaseViewModel item5 in searchModels2)
				{
					DisplayItemModel displayItemModel4 = new DisplayItemModel(item5);
					displayItemModel4.Selected = displayItemModel4.Id == Data.FocusId;
					displayItemModel4.DisplayColor = GetPriorityColor(displayItemModel4.Priority).ToString();
					displayItemModel3.Section.Children.Add(displayItemModel4);
					if (displayItemModel3.Section.Children.Count <= 5 || _loadMoreTask)
					{
						displayModels.Add(displayItemModel4);
					}
				}
				if (displayItemModel3.Section.Children.Count > 5 && !_loadMoreTask)
				{
					DisplayItemModel item2 = new DisplayItemModel
					{
						SourceViewModel = new TaskBaseViewModel
						{
							Type = DisplayType.LoadMore
						},
						Parent = displayItemModel3
					};
					displayModels.Add(item2);
				}
				displayModels.Add(DisplayItemModel.BuildSplit());
			}
		}
		void AddTimers()
		{
			List<TimerModel> list = _timers?.Where((TimerModel timer) => !timer.Deleted && timer.Status == 0 && SearchHelper.KeyWordMatched(timer.Name, keyword, keys)).ToList();
			if (list != null && list.Any())
			{
				Section section = new Section
				{
					SectionId = "CommonTimer",
					Name = Utils.GetString("CommonTimer")
				};
				DisplayItemModel displayItemModel = new DisplayItemModel(new TaskBaseViewModel(section))
				{
					Section = section,
					IsOpen = true
				};
				displayModels.Add(displayItemModel);
				foreach (TimerModel item6 in list)
				{
					DisplayItemModel displayItemModel2 = new DisplayItemModel(new TaskBaseViewModel
					{
						Id = item6.Id,
						Title = item6.Name,
						Type = DisplayType.Timer,
						Priority = 0,
						Status = 0
					});
					displayItemModel2.Selected = displayItemModel2.Id == Data.FocusId;
					displayItemModel2.DisplayColor = GetPriorityColor(displayItemModel2.Priority).ToString();
					displayItemModel.Section.Children.Add(displayItemModel2);
					if (displayItemModel.Section.Children.Count <= 5 || _loadMoreTimer)
					{
						displayModels.Add(displayItemModel2);
					}
				}
				if (displayItemModel.Section.Children.Count > 5 && !_loadMoreTimer)
				{
					DisplayItemModel item = new DisplayItemModel
					{
						SourceViewModel = new TaskBaseViewModel
						{
							Type = DisplayType.LoadMore
						},
						Parent = displayItemModel
					};
					displayModels.Add(item);
				}
				displayModels.Add(DisplayItemModel.BuildSplit());
			}
		}
	}

	private void OnSearchFocus(object sender, RoutedEventArgs e)
	{
		if (SearchIcon.IsVisible)
		{
			GroupTitle.Visibility = Visibility.Collapsed;
			SearchIcon.Visibility = Visibility.Collapsed;
			ProjectName.Visibility = Visibility.Collapsed;
			ExitSearchIcon.Visibility = Visibility.Visible;
			EmptyPanel.Visibility = Visibility.Collapsed;
			TaskList.ItemsSource = new ObservableCollection<DisplayItemModel>();
			SelectTaskPanel.Visibility = Visibility.Visible;
		}
	}

	private void ExitSearchClick(object sender, MouseButtonEventArgs e)
	{
		SearchText.Text = string.Empty;
		TaskList.Focus();
		GroupTitle.Visibility = ((!GroupTitle.Titles.Contains("|")) ? Visibility.Collapsed : Visibility.Visible);
		SearchIcon.Visibility = Visibility.Visible;
		ProjectName.Visibility = Visibility.Visible;
		ExitSearchIcon.Visibility = Visibility.Collapsed;
		LoadDisplayItems();
	}

	private void OnGroupTitleSelected(object sender, GroupTitleViewModel e)
	{
		LoadDisplayItems();
		_selectedGroupTitle = GroupTitle.SelectedTitle;
	}

	private void LoadDisplayItems()
	{
		switch (GroupTitle.SelectedTitle)
		{
		case "Recent":
			ProjectName.Visibility = Visibility.Collapsed;
			LoadRecent();
			break;
		case "Habit":
			ProjectName.Visibility = Visibility.Collapsed;
			LoadHabits();
			break;
		case "CommonTimer":
			ProjectName.Visibility = Visibility.Collapsed;
			LoadTimers();
			break;
		default:
			ProjectName.Visibility = Visibility.Visible;
			LoadTasks();
			break;
		}
	}

	private async Task LoadRecent()
	{
		List<PomoTask> list = _recent;
		if (list == null)
		{
			list = await PomoTaskDao.GetRecentPomoTasks(6);
		}
		List<PomoTask> list2 = list;
		List<PomoTask> remoteRecentTasks = _remoteRecentTasks;
		if (remoteRecentTasks != null)
		{
			list2.AddRange(remoteRecentTasks.Where((PomoTask p) => p.EndTime > DateTime.Today.AddDays(-6.0)));
		}
		list2.Sort((PomoTask a, PomoTask b) => b.EndTime.CompareTo(a.EndTime));
		List<string> taskIds = new List<string>();
		List<string> habitIds = new List<string>();
		List<string> timerIds = new List<string>();
		int[] array = new int[3];
		HashSet<string> hashSet = new HashSet<string>();
		foreach (PomoTask r in list2)
		{
			if (taskIds.Count + habitIds.Count + timerIds.Count >= 20)
			{
				break;
			}
			if (hashSet.Contains(r.GetId()))
			{
				continue;
			}
			hashSet.Add(r.GetId());
			if (_loadTimers && !string.IsNullOrEmpty(r.TimerSid) && !timerIds.Contains(r.TimerSid) && _timers?.FirstOrDefault((TimerModel t) => t.Id == r.TimerSid) != null)
			{
				timerIds.Add(r.TimerSid);
				if (array[2] == 0)
				{
					array[2] = 1 + ((array[0] > 0) ? 1 : 0) + ((array[1] > 0) ? 1 : 0);
				}
			}
			else if (!string.IsNullOrEmpty(r.TaskId) && !taskIds.Contains(r.TaskId))
			{
				if (_loadTimers && _timers?.FirstOrDefault((TimerModel t) => t.ObjId == r.TaskId) != null)
				{
					timerIds.Add(r.TimerSid);
					if (array[2] == 0)
					{
						array[2] = 1 + ((array[0] > 0) ? 1 : 0) + ((array[1] > 0) ? 1 : 0);
					}
					continue;
				}
				taskIds.Add(r.TaskId);
				if (!string.IsNullOrEmpty(r.TimerSid))
				{
					hashSet.Add(r.TimerSid);
				}
				if (array[0] == 0)
				{
					array[0] = 1 + ((array[1] > 0) ? 1 : 0) + ((array[2] > 0) ? 1 : 0);
				}
			}
			else
			{
				if (!LocalSettings.Settings.ShowHabit || string.IsNullOrEmpty(r.HabitId) || habitIds.Contains(r.HabitId))
				{
					continue;
				}
				if (_loadTimers && _timers?.FirstOrDefault((TimerModel t) => t.ObjId == r.HabitId) != null)
				{
					timerIds.Add(r.TimerSid);
					if (array[2] == 0)
					{
						array[2] = 1 + ((array[0] > 0) ? 1 : 0) + ((array[1] > 0) ? 1 : 0);
					}
					continue;
				}
				habitIds.Add(r.HabitId);
				if (!string.IsNullOrEmpty(r.TimerSid))
				{
					hashSet.Add(r.TimerSid);
				}
				if (array[1] == 0)
				{
					array[1] = 1 + ((array[0] > 0) ? 1 : 0) + ((array[2] > 0) ? 1 : 0);
				}
			}
		}
		List<DisplayItemModel> displayModels = new List<DisplayItemModel>();
		for (int num = 1; num < 4; num++)
		{
			for (int num2 = 0; num2 < 3; num2++)
			{
				if (array[num2] == num)
				{
					switch (num2)
					{
					case 0:
						AddTasks();
						break;
					case 1:
						AddHabits();
						break;
					case 2:
						AddTimers();
						break;
					}
				}
			}
		}
		ReloadTask(displayModels);
		void AddHabits()
		{
			if (habitIds.Any())
			{
				bool flag = false;
				Section section = new Section
				{
					SectionId = "Habit",
					Name = Utils.GetString("Habit")
				};
				DisplayItemModel displayItemModel = new DisplayItemModel(new TaskBaseViewModel(section))
				{
					Section = section,
					IsOpen = true
				};
				foreach (string t in habitIds)
				{
					HabitModel habit = _habits?.FirstOrDefault((HabitModel h) => h.Id == t);
					if (habit != null && habit.Status == 0 && habit.SyncStatus != -1)
					{
						DisplayItemModel displayItemModel2 = new DisplayItemModel(new HabitBaseViewModel(habit)
						{
							StartDate = null
						});
						displayItemModel2.Selected = displayItemModel2.Id == Data.FocusId;
						displayItemModel2.Linked = _linkedIds.Contains(displayItemModel2.Id);
						displayItemModel2.DisplayColor = GetPriorityColor(displayItemModel2.Priority).ToString();
						if (!flag)
						{
							displayModels.Add(displayItemModel);
							flag = true;
						}
						HabitCheckInModel habitCheckInModel = _checkIns?.FirstOrDefault((HabitCheckInModel c) => c.HabitId == habit.Id);
						if (habitCheckInModel != null && (habitCheckInModel.Value >= habitCheckInModel.Goal || habitCheckInModel.CheckStatus == 1))
						{
							displayItemModel2.SourceViewModel.Status = 2;
							displayItemModel2.SourceViewModel.CompletedTime = habitCheckInModel.CheckinTime;
						}
						displayItemModel.Section.Children.Add(displayItemModel2);
						displayModels.Add(displayItemModel2);
					}
				}
			}
		}
		void AddTasks()
		{
			if (taskIds.Any())
			{
				bool flag = false;
				Section section = new Section
				{
					SectionId = "Task",
					Name = Utils.GetString("Task")
				};
				DisplayItemModel displayItemModel = new DisplayItemModel(new TaskBaseViewModel(section))
				{
					Section = section,
					IsOpen = true
				};
				foreach (string item in taskIds)
				{
					TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
					if (taskById != null && taskById.Deleted == 0 && taskById.Status == 0)
					{
						DisplayItemModel displayItemModel2 = new DisplayItemModel(taskById);
						displayItemModel2.Selected = displayItemModel2.Id == Data.FocusId;
						displayItemModel2.DisplayColor = GetPriorityColor(displayItemModel2.Priority).ToString();
						displayItemModel2.Linked = _linkedIds.Contains(displayItemModel2.Id);
						displayItemModel.Section.Children.Add(displayItemModel2);
						if (!flag)
						{
							displayModels.Add(displayItemModel);
							flag = true;
						}
						displayModels.Add(displayItemModel2);
					}
				}
			}
		}
		void AddTimers()
		{
			if (timerIds.Any())
			{
				Section section = new Section
				{
					SectionId = "CommonTimer",
					Name = Utils.GetString("CommonTimer")
				};
				bool flag = false;
				DisplayItemModel displayItemModel = new DisplayItemModel(new TaskBaseViewModel(section))
				{
					Section = section,
					IsOpen = true
				};
				foreach (string id in timerIds)
				{
					TimerModel timerModel = _timers.FirstOrDefault((TimerModel t) => t.Id == id && t.Status == 0 && !t.Deleted);
					if (timerModel != null)
					{
						DisplayItemModel displayItemModel2 = new DisplayItemModel(new TaskBaseViewModel
						{
							Id = timerModel.Id,
							Title = timerModel.Name,
							Type = DisplayType.Timer,
							Priority = 0,
							Status = 0,
							IsOpen = true
						})
						{
							Parent = displayItemModel
						};
						displayItemModel2.Selected = displayItemModel2.Id == Data.FocusId;
						displayItemModel2.DisplayColor = GetPriorityColor(displayItemModel2.Priority).ToString();
						if (!flag)
						{
							displayModels.Add(displayItemModel);
							flag = true;
						}
						displayItemModel.Section.Children.Add(displayItemModel2);
						displayModels.Add(displayItemModel2);
					}
				}
			}
		}
	}

	private async Task LoadTimers()
	{
		List<TimerModel> list = _timers;
		if (list == null)
		{
			list = (from a in await TimerDao.GetDisplayTimersAsync()
				where a.Status == 0
				orderby a.SortOrder
				select a).ToList();
		}
		List<TimerModel> list2 = list;
		List<DisplayItemModel> list3 = new List<DisplayItemModel>();
		foreach (TimerModel item2 in list2)
		{
			DisplayItemModel item = new DisplayItemModel(new TaskBaseViewModel
			{
				Id = item2.Id,
				Title = item2.Name,
				Type = DisplayType.Timer,
				Priority = 0,
				Status = 0,
				SortOrder = item2.SortOrder
			});
			list3.Add(item);
		}
		foreach (DisplayItemModel item3 in list3)
		{
			item3.Selected = item3.Id == Data.FocusId;
			item3.DisplayColor = GetPriorityColor(item3.Priority).ToString();
		}
		ReloadTask(list3.ToList());
	}

	private async Task LoadHabits()
	{
		List<HabitSectionModel> source = (await HabitService.GetHabitSections()).OrderBy((HabitSectionModel s) => s.SortOrder).ToList();
		Dictionary<string, long> sectionIdDict = source.ToDictionaryEx((HabitSectionModel s) => s.Id, (HabitSectionModel s) => s.SortOrder);
		List<HabitModel> list = _habits;
		if (list == null)
		{
			list = await HabitService.GetHabitsbyStatus(0);
		}
		List<HabitModel> list2 = list;
		list2.Sort(delegate(HabitModel a, HabitModel b)
		{
			long num = ((!string.IsNullOrEmpty(a.SectionId) && sectionIdDict.ContainsKey(a.SectionId)) ? sectionIdDict[a.SectionId] : long.MaxValue);
			long num2 = ((!string.IsNullOrEmpty(b.SectionId) && sectionIdDict.ContainsKey(b.SectionId)) ? sectionIdDict[b.SectionId] : long.MaxValue);
			return (num != num2) ? num.CompareTo(num2) : a.SortOrder.CompareTo(b.SortOrder);
		});
		List<DisplayItemModel> list3 = new List<DisplayItemModel>();
		foreach (HabitModel habit in list2)
		{
			HabitBaseViewModel habitBaseViewModel = new HabitBaseViewModel(habit)
			{
				StartDate = null
			};
			HabitCheckInModel habitCheckInModel = _checkIns?.FirstOrDefault((HabitCheckInModel c) => c.HabitId == habit.Id);
			if (habitCheckInModel != null && (habitCheckInModel.Value >= habitCheckInModel.Goal || habitCheckInModel.CheckStatus == 1))
			{
				habitBaseViewModel.Status = 2;
				habitBaseViewModel.CompletedTime = habitCheckInModel.CheckinTime;
			}
			DisplayItemModel item = new DisplayItemModel(habitBaseViewModel);
			list3.Add(item);
		}
		list3.Sort((DisplayItemModel a, DisplayItemModel b) => (a.Status == b.Status && a.CompletedTime.HasValue && b.CompletedTime.HasValue) ? b.CompletedTime.Value.CompareTo(a.CompletedTime.Value) : a.Status.CompareTo(b.Status));
		foreach (DisplayItemModel item2 in list3)
		{
			item2.Selected = item2.Habit.Id == Data.FocusId;
			item2.DisplayColor = GetPriorityColor(item2.Priority).ToString();
			item2.Linked = _linkedIds.Contains(item2.Id);
		}
		ReloadTask(list3);
	}

	private void OnLoadMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: DisplayItemModel { Parent: not null } dataContext }))
		{
			return;
		}
		DisplayItemModel parent = dataContext.Parent;
		IEnumerable itemsSource = TaskList.ItemsSource;
		ObservableCollection<DisplayItemModel> models = itemsSource as ObservableCollection<DisplayItemModel>;
		if (models == null || parent.Section.Children == null)
		{
			return;
		}
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel child in parent.Section.Children)
		{
			list.Add(child);
			if (child.IsOpen && child.IsTask)
			{
				List<DisplayItemModel> childrenModels = child.GetChildrenModels(getAll: false);
				list.AddRange(childrenModels);
			}
		}
		list.ForEach(delegate(DisplayItemModel child)
		{
			models.Remove(child);
		});
		models.Remove(dataContext);
		int num = models.IndexOf(parent);
		if (num >= 0 && models[num] != null)
		{
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				models.Insert(num + 1 + num2, list[num2]);
			}
		}
	}

	private void OnSectionClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: var dataContext }))
		{
			return;
		}
		DisplayItemModel section = dataContext as DisplayItemModel;
		if (section == null || !section.IsSection)
		{
			return;
		}
		section.IsOpen = !section.IsOpen;
		IEnumerable itemsSource = TaskList.ItemsSource;
		ObservableCollection<DisplayItemModel> models = itemsSource as ObservableCollection<DisplayItemModel>;
		if (models == null || section.Section.Children == null)
		{
			return;
		}
		List<DisplayItemModel> list = new List<DisplayItemModel>();
		foreach (DisplayItemModel child in section.Section.Children)
		{
			list.Add(child);
			if (child.IsOpen && child.IsTask)
			{
				List<DisplayItemModel> childrenModels = child.GetChildrenModels(getAll: false);
				list.AddRange(childrenModels);
			}
		}
		if (section.IsOpen)
		{
			int num = models.IndexOf(section);
			if (num >= 0 && models[num] != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					models.Insert(num + 1 + i, list[i]);
				}
				if (NeedLoadMore(section))
				{
					DisplayItemModel displayItemModel = DisplayItemModel.BuildLoadMore();
					displayItemModel.Parent = section;
					models.Insert(num + 1 + list.Count, displayItemModel);
				}
			}
		}
		else
		{
			list.ForEach(delegate(DisplayItemModel child)
			{
				models.Remove(child);
			});
			DisplayItemModel displayItemModel2 = models.FirstOrDefault((DisplayItemModel m) => m.IsLoadMore && m.Parent == section);
			if (displayItemModel2 != null)
			{
				models.Remove(displayItemModel2);
			}
		}
		if (GroupTitle.SelectedTitle == "Task" && _projectIdentity != null)
		{
			string name = section.Title;
			if (section.Section is AssigneeSection assigneeSection)
			{
				name = assigneeSection.Assingee;
			}
			if (section.IsOpen)
			{
				SectionStatusDao.OpenProjectSection(_projectIdentity.Id, name);
			}
			else
			{
				SectionStatusDao.CloseProjectSection(_projectIdentity.Id, name);
			}
		}
	}

	private bool NeedLoadMore(DisplayItemModel section)
	{
		if (!string.IsNullOrEmpty(SearchText.Text))
		{
			if (section.Section.Children.Count <= 5)
			{
				return false;
			}
			switch (section.Section.SectionId)
			{
			case "Task":
				return _loadMoreTask;
			case "Habit":
				return _loadMoreHabit;
			case "CommonTimer":
				return _loadMoreTimer;
			}
		}
		return false;
	}

	public void Open()
	{
		_parentPopup.IsOpen = true;
	}

	public Popup GetParentPopup()
	{
		return _parentPopup;
	}

	public void ResetSelectTitle()
	{
		SearchText.Text = string.Empty;
		_selectedGroupTitle = "Task";
		_projectIdentity = new TodayProjectIdentity();
		Data.Identity = _projectIdentity;
	}

	public void Show()
	{
		_parentPopup.IsOpen = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/pomofiltercontrol.xaml", UriKind.Relative);
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
			Root = (PomoFilterControl)target;
			break;
		case 2:
			((Storyboard)target).Completed += OnToasted;
			break;
		case 3:
			Container = (Grid)target;
			break;
		case 4:
			DetailGrid = (Grid)target;
			break;
		case 5:
			PomoSummary = (TaskDetailPomoSummaryControl)target;
			break;
		case 7:
			CompleteButton = (Button)target;
			CompleteButton.Click += OnCompleteClick;
			break;
		case 8:
			((Button)target).Click += OnSelectTaskClick;
			break;
		case 9:
			ListGrid = (Grid)target;
			break;
		case 10:
			GroupTitle = (GroupTitle)target;
			break;
		case 11:
			SearchGrid = (Grid)target;
			break;
		case 12:
			SearchText = (EmojiEditor)target;
			break;
		case 13:
			SearchIcon = (Path)target;
			break;
		case 14:
			ExitSearchIcon = (Border)target;
			ExitSearchIcon.MouseLeftButtonUp += ExitSearchClick;
			break;
		case 15:
			ClearIcon = (Border)target;
			ClearIcon.MouseLeftButtonUp += OnClearClick;
			break;
		case 16:
			ProjectName = (StackPanel)target;
			break;
		case 17:
			((EmjTextBlock)target).MouseLeftButtonUp += OnFilterClick;
			break;
		case 18:
			((Grid)target).MouseLeftButtonUp += OnFilterClick;
			break;
		case 19:
			ProjectFilterPopup = (EscPopup)target;
			break;
		case 20:
			SelectTaskPanel = (Grid)target;
			break;
		case 21:
			TaskList = (ListView)target;
			break;
		case 26:
			EmptyPanel = (ContentControl)target;
			break;
		case 27:
			ToastBorder = (Border)target;
			break;
		case 28:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 6:
			((CheckBox)target).PreviewMouseLeftButtonUp += CheckBoxClick;
			((CheckBox)target).Checked += OnItemCheckChanged;
			((CheckBox)target).Unchecked += OnItemCheckChanged;
			break;
		case 22:
			((ContentControl)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 23:
			((Grid)target).PreviewMouseLeftButtonUp += OnOpenPathClick;
			break;
		case 24:
			((Grid)target).MouseLeftButtonUp += OnSectionClick;
			break;
		case 25:
			((TextBlock)target).MouseLeftButtonUp += OnLoadMoreClick;
			break;
		}
	}
}
