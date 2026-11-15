using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Search;

public class SearchInputControl : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty SearchMoreVisibilityProperty = DependencyProperty.Register("SearchMoreVisibility", typeof(Visibility), typeof(SearchInputControl), new PropertyMetadata(Visibility.Collapsed));

	private readonly SolidColorBrush _primaryTextColor = ThemeUtil.GetColor("BaseColorOpacity100_80");

	private readonly DelayActionHandler _searchActionHandler = new DelayActionHandler(100);

	private bool _isTagOpen;

	private bool _isSearchMoreMouseOver;

	private bool _keepOldText;

	private SearchExtra _searchExtra;

	private ObservableCollection<SearchHistoryViewModel> _searchHistoryModels = new ObservableCollection<SearchHistoryViewModel>();

	private ObservableCollection<SearchItemBaseModel> _searchItemModels = new ObservableCollection<SearchItemBaseModel>();

	private const int SearchResultPageSize = 20;

	private const double SearchResultAutoLoadThreshold = 120.0;

	private readonly List<SearchTaskResultPagingState> _searchResultPagingStates = new List<SearchTaskResultPagingState>();

	private bool _loadingNextSearchResultPage;

	private int _loadingNextSearchResultPageVersion;

	private int _searchDisplayVersion;

	private int _selectedIndex = -1;

	private List<string> _tags;

	private bool _restore;

	private SearchFilterViewModel _searchFilter;

	private SearchCategory _selectedCategory;

	private bool _ignoreCategoryOptionClickUntilMouseUp;

	private DateTime _ignoreCategoryOptionClickUntil = DateTime.MinValue;

	private static string _inputText;

	private static bool _tagExpand;

	private static bool _groupExpand;

	private static bool _taskGroupExpand;

	private static bool _listExpand;

	private static bool _filterExpand;

	private List<string> _linkTaskIds;

	private static SearchKind _searchKind = SearchKind.Task;

	internal SearchInputControl Root;

	internal Line FocusHighlightLine;

	internal TextBlock HintText;

	internal ScrollViewer Visual;

	internal RichTextBox TitleTextBox;

	internal Border ClearGrid;

	internal Border CloseGrid;

	internal StackPanel CategoryOptionPanel;

	internal Border TagCategoryOption;

	internal Border ListCategoryOption;

	internal Border FilterCategoryOption;

	internal Border CalendarEventCategoryOption;

	internal TextBlock CalendarEventCategoryText;

	internal SearchFilterControl PopupFilterControl;

	internal StackPanel SearchHistoryPage;

	internal ItemsControl SearchHistoryList;

	internal ListView SearchItemList;

	internal StackPanel EmptyGrid;

	internal EmptyView EmptyView;

	internal TextBlock NoTaskGridTitleTextBox;

	internal TextBlock NoTaskGridTitleTextBoxOnSearch;

	internal Run NoResultText;

	internal Run TryCloudSearchText;

	internal TextBlock ChangeKeyWordsText;

	private bool _contentLoaded;

	public Visibility SearchMoreVisibility
	{
		get
		{
			return (Visibility)GetValue(SearchMoreVisibilityProperty);
		}
		set
		{
			SetValue(SearchMoreVisibilityProperty, value);
		}
	}

	private List<ProjectTemplate> projectTemplates { get; set; }

	private SearchFilterViewModel SearchFilter => _searchFilter ?? SearchHelper.SearchFilter;

	public event EventHandler<EventArgs> Close;

	public event EventHandler<SearchExtra> Search;

	public SearchInputControl()
	{
		InitializeComponent();
		AddHandler(Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(OnSearchInputPreviewMouseUp), handledEventsToo: true);
		_searchFilter = new SearchFilterViewModel();
		PopupFilterControl.UseInSearchPopup(_searchFilter);
		PopupFilterControl.FilterChanged += OnPopupFilterChanged;
		SearchItemList.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnSearchItemListScrollChanged));
		_searchActionHandler.SetAction(TryDelaySearch);
		DataObject.AddPastingHandler(TitleTextBox, OnPaste);
		base.Loaded += OnLoaded;
		FocusEnd();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		TryUpdateProjectTemplates();
		base.Loaded -= OnLoaded;
		SetDefaultEmptyIcon();
	}

	private void OnPaste(object sender, DataObjectPastingEventArgs e)
	{
		e.CancelCommand();
		try
		{
			if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, autoConvert: true) && e.SourceDataObject.GetData(DataFormats.UnicodeText) is string text)
			{
				InsertPasteContent(text.Replace("\n", " ").Replace("\r", " "));
			}
		}
		catch (Exception)
		{
		}
	}

	private async Task TryUpdateProjectTemplates()
	{
		ProjectTemplatesModel local = await ProjectTemplatesDao.GetFirst();
		ProjectTemplatesDto projectTemplatesDto = await Communicator.QueryProjectTemplate(local?.Timestamp ?? 0);
		if (projectTemplatesDto != null)
		{
			await ProjectTemplatesDao.AddOrUpdate(projectTemplatesDto);
			projectTemplates = await ProjectTemplatesDao.GetAllTemplates();
		}
		else
		{
			projectTemplates = local?.ProjectTemplates.Where((ProjectTemplate x) => x.Deleted == 0).ToList();
		}
	}

	private void InsertPasteContent(string content)
	{
		TitleTextBox.Selection.Text = string.Empty;
		TitleTextBox.CaretPosition.InsertTextInRun(content);
	}

	private void TryDelaySearch(object sender, EventArgs e)
	{
		if (base.Visibility == Visibility.Visible)
		{
			Utils.RunOnUiThread(base.Dispatcher, StartSearch);
		}
	}

	private void StartSearch()
	{
		if (!_isTagOpen)
		{
			string searchKey = _searchExtra.SearchKey;
			if (searchKey != null && searchKey.Trim().Length > 64)
			{
				_searchExtra.SearchKey = _searchExtra.SearchKey?.Trim().Substring(0, 64);
			}
			if (_linkTaskIds == null)
			{
				UserActCollectUtils.AddClickEvent("search", "search_type", "task");
			}
			this.Search?.Invoke(this, _searchExtra);
			_keepOldText = false;
			if (!string.IsNullOrWhiteSpace(_searchExtra.SearchKey) || (_searchExtra.Tags != null && _searchExtra.Tags.Count > 0))
			{
				SearchHistoryDao.AddHistoryModel(_searchExtra.SearchKey, _searchExtra.Tags);
			}
		}
	}

	private string GetContentText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		InlineCollection textInlines = GetTextInlines();
		if (textInlines != null && textInlines.Count > 0)
		{
			foreach (Inline item in textInlines)
			{
				if (item is Run run)
				{
					stringBuilder.Append(run.Text);
				}
			}
		}
		return stringBuilder.ToString();
	}

	private InlineCollection GetTextInlines()
	{
		BlockCollection blocks = TitleTextBox.Document.Blocks;
		if (blocks.Count == 1 && blocks.FirstOrDefault() is Paragraph paragraph)
		{
			return paragraph.Inlines;
		}
		return null;
	}

	public SearchExtra GetSearchExtra()
	{
		return new SearchExtra
		{
			SearchKey = GetContentText().Trim(),
			Tags = GetInputTags(),
			Category = _selectedCategory,
			Filter = SearchFilter.ToSearchFilter()
		};
	}

	private void TryDelaySearch(string taskId = null)
	{
		_searchExtra = new SearchExtra
		{
			SearchKey = GetContentText().Trim(),
			Tags = GetInputTags(),
			SearchId = taskId,
			LinkTaskIds = _linkTaskIds,
			Category = _selectedCategory,
			Filter = SearchFilter.ToSearchFilter()
		};
		if (!_searchExtra.Empty() || _linkTaskIds != null)
		{
			_searchActionHandler.TryDoAction();
		}
	}

	private void TaskTextChanged(object sender, TextChangedEventArgs e)
	{
		_keepOldText = true;
		OnTextChanged();
	}

	private void OnTextBoxClick(object sender, RoutedEventArgs e)
	{
		OnTextChanged();
	}

	private async void OnTextChanged(bool forceSearch = false)
	{
		_selectedIndex = -1;
		string text = GetContentText()?.Trim();
		SetHintVisibility(text);
		if (await TryShowTagPopup(text))
		{
			return;
		}
		if (_inputText == text)
		{
			if (!forceSearch)
			{
				return;
			}
		}
		else
		{
			_tagExpand = false;
			_groupExpand = false;
			_taskGroupExpand = false;
			_listExpand = false;
			_filterExpand = false;
			_inputText = text;
		}
		SetSearchItem(text);
	}

	private void SetSearchItem(string text)
	{
		int searchDisplayVersion = BeginSearchDisplayUpdate();
		_selectedIndex = -1;
		SetTags();
		text = text?.Trim() ?? string.Empty;
		if (_searchKind == SearchKind.Task)
		{
			SearchMoreVisibility = Visibility.Collapsed;
			ClearGrid.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
		}
		bool flag = !string.IsNullOrEmpty(text) || _tags.Count > 0;
		bool flag2 = SearchFilter.ToSearchFilter().HasFilterConditions();
		if (_searchKind == SearchKind.Normal && _selectedCategory != SearchCategory.None && !flag && !flag2)
		{
			DelayActionHandlerCenter.RemoveAction("OnSearchInput");
			SearchMoreVisibility = Visibility.Collapsed;
			ClearGrid.Visibility = Visibility.Collapsed;
			SearchItemList.IsHitTestVisible = false;
			_searchItemModels = new ObservableCollection<SearchItemBaseModel>();
			SearchItemList.ItemsSource = _searchItemModels;
			SearchHistoryPage.Visibility = Visibility.Collapsed;
			EmptyGrid.Visibility = Visibility.Visible;
			SetCategoryEmptyText();
			return;
		}
		if (_searchKind == SearchKind.Normal && _tags.Count == 0 && string.IsNullOrEmpty(text) && !flag2)
		{
			DelayActionHandlerCenter.RemoveAction("OnSearchInput");
			SearchMoreVisibility = Visibility.Collapsed;
			ClearGrid.Visibility = Visibility.Collapsed;
			SearchItemList.IsHitTestVisible = false;
			_searchItemModels = new ObservableCollection<SearchItemBaseModel>();
			SearchItemList.ItemsSource = _searchItemModels;
			SetDefaultEmptyText();
			ShowSearchHistory();
			return;
		}
		DelayActionHandlerCenter.TryDoAction("OnSearchInput", delegate
		{
			base.Dispatcher.Invoke(delegate
			{
				SearchHistoryPage.Visibility = Visibility.Collapsed;
				ClearGrid.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
				SearchItemList.IsHitTestVisible = true;
				List<string> list = text.ToLower().Split(' ').ToList();
				list.Remove("");
				string text2 = "(" + Utils.HandleReg(text.ToLower()) + ")";
				if (list.Count > 1)
				{
					text2 = list.Aggregate(text2, (string current, string key) => current + "|(" + Utils.HandleReg(key) + ")");
				}
				SearchHelper.PreSearchRegex = new Regex(text2);
				List<string> list2 = new List<string>(_linkTaskIds ?? new List<string>());
				if (list2.Count > 0)
				{
					foreach (string item in _linkTaskIds ?? new List<string>())
					{
						TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
						if (taskById != null)
						{
							List<TaskBaseViewModel> allSubTasksById = TaskCache.GetAllSubTasksById(taskById.Id, taskById.ProjectId);
							list2.AddRange(allSubTasksById.Select((TaskBaseViewModel it) => it.Id));
							if (!string.IsNullOrEmpty(taskById.ParentId))
							{
								list2.Add(taskById.ParentId);
							}
						}
					}
				}
				GetSearchDisplayModels((text: text, kind: _searchKind, exceptTaskIds: list2), searchDisplayVersion);
			});
		}, 150);
	}

	private void SetCategoryEmptyText()
	{
		SetCategoryEmptyIcon(_selectedCategory);
		string selectedCategoryTitle = GetSelectedCategoryTitle();
		NoTaskGridTitleTextBox.Text = string.Format(Utils.GetString("EnterKeywordsToSearchX"), selectedCategoryTitle);
		TryCloudSearchText.Text = "";
		ChangeKeyWordsText.Text = "";
	}

	private void SetDefaultEmptyText()
	{
		SetDefaultEmptyIcon();
		NoTaskGridTitleTextBox.Text = Utils.GetString("EnterKeywordsToSearch");
		TryCloudSearchText.Text = Utils.GetString("TryCloudSearch");
		ChangeKeyWordsText.Text = "";
	}

	private void SetDefaultEmptyIcon()
	{
		EmptyView.Visibility = Visibility.Visible;
		EmptyView.SetIconsSource((_searchKind == SearchKind.Normal) ? new EmptyIconsSourceModel("SearchEmpty") : new EmptyIconsSourceModel("SearchResultEmpty"));
	}

	private void SetSearchResultEmptyIcon()
	{
		EmptyView.Visibility = Visibility.Visible;
		EmptyView.SetIconsSource(new EmptyIconsSourceModel("SearchResultEmpty"));
	}

	private void SetCategoryEmptyIcon(SearchCategory category)
	{
		EmptyView.Visibility = Visibility.Visible;
		switch (category)
		{
		case SearchCategory.Task:
			EmptyView.SetIconsSource(new EmptyIconsSourceModel("NormalEmpty", new List<int> { 1, 2, 3, 4, 6 }));
			break;
		case SearchCategory.Tag:
			EmptyView.SetIconsSource(new EmptyIconsSourceModel("TagEmpty"));
			break;
		case SearchCategory.List:
			EmptyView.SetIconsSource(new EmptyIconsSourceModel("ListEmpty", new List<int> { 1, 2, 3, 4, 6 }, "ListEmpty0"));
			break;
		case SearchCategory.Filter:
			EmptyView.SetIconsSource(new EmptyIconsSourceModel("FilterEmpty"));
			break;
		case SearchCategory.CalendarEvent:
			EmptyView.SetIconsSource(new EmptyIconsSourceModel("CalendarArrangeEmpty"));
			break;
		default:
			SetDefaultEmptyIcon();
			break;
		}
	}

	private string GetSelectedCategoryTitle()
	{
		return SearchFilter.SelectedSearchType switch
		{
			SearchTypeFilter.Task => Utils.GetString("Task"), 
			SearchTypeFilter.Tag => Utils.GetString("Tags"), 
			SearchTypeFilter.List => Utils.GetString("List"), 
			SearchTypeFilter.Filter => Utils.GetString("Filter"), 
			SearchTypeFilter.CalendarEvent => Utils.GetString("Event", Utils.GetString("Calendar")), 
			_ => _selectedCategory switch
			{
				SearchCategory.Tag => Utils.GetString("Tags"), 
				SearchCategory.List => Utils.GetString("List"), 
				SearchCategory.Filter => Utils.GetString("Filter"), 
				SearchCategory.CalendarEvent => Utils.GetString("Event", Utils.GetString("Calendar")), 
				SearchCategory.Task => Utils.GetString("Task"), 
				_ => Utils.GetString("Search"), 
			}, 
		};
	}

	private void SetTags()
	{
		List<string> inputTags = GetInputTags();
		if (inputTags?.Count != _tags?.Count)
		{
			SearchHelper.ClearTaskSearchModels();
			SearchHelper.GetTaskSearchModels(SearchFilter.ToSearchFilter(), inputTags);
		}
		_tags = inputTags;
	}

	private async void ShowSearchHistory()
	{
		List<SearchHistoryViewModel> list = (await SearchHistoryDao.GetHistoryViewModels()).Where((SearchExtra it) => !string.IsNullOrEmpty(it.SearchKey))?.Select((SearchExtra m) => new SearchHistoryViewModel(m)).ToList();
		if (list == null || list.Count == 0)
		{
			_searchHistoryModels.Clear();
		}
		else
		{
			_searchHistoryModels = new ObservableCollection<SearchHistoryViewModel>(list);
		}
		if (_searchHistoryModels.Count == 0 && _searchKind != SearchKind.Task)
		{
			SearchHistoryPage.Visibility = Visibility.Collapsed;
			EmptyGrid.Visibility = Visibility.Visible;
		}
		else
		{
			SearchHistoryPage.Visibility = Visibility.Visible;
			EmptyGrid.Visibility = Visibility.Collapsed;
		}
		SearchHistoryList.ItemsSource = _searchHistoryModels;
	}

	private async Task GetSearchDisplayModels((string text, SearchKind kind, List<string> exceptTaskIds) searchKey, int searchDisplayVersion)
	{
		ObservableCollection<SearchItemBaseModel> items = new ObservableCollection<SearchItemBaseModel>();
		List<SearchTaskResultPagingState> pagingStates = new List<SearchTaskResultPagingState>();
		SearchFilterViewModel filter = SearchFilter;
		filter.Kind = _searchKind;
		filter.Category = _selectedCategory;
		bool flag = !string.IsNullOrWhiteSpace(searchKey.text) || _tags.Count > 0;
		SearchFilterModel searchFilter = filter.ToSearchFilter();
		bool hasFilterConditions = searchFilter.HasFilterConditions();
		bool flag2 = searchFilter.HasCalendarFilterOnly();
		bool canSearchEvents = _tags.Count == 0 && searchFilter.SearchEvent();
		bool isCategorySearch = searchKey.kind == SearchKind.Normal && _selectedCategory != SearchCategory.None && (flag || hasFilterConditions);
		List<TaskBaseViewModel> taskSearchItems = new List<TaskBaseViewModel>();
		List<TaskBaseViewModel> eventSearchItems = new List<TaskBaseViewModel>();
		List<SearchTagAndProjectModel> categoryProjectItems = new List<SearchTagAndProjectModel>();
		SearchHelper.ClearTaskSearchModels();
		if (isCategorySearch)
		{
			switch (_selectedCategory)
			{
			case SearchCategory.Task:
				taskSearchItems = await GetSearchItems(SearchCategory.Task);
				break;
			case SearchCategory.CalendarEvent:
				eventSearchItems = await GetSearchItems(SearchCategory.CalendarEvent);
				break;
			case SearchCategory.Tag:
			case SearchCategory.List:
			case SearchCategory.Filter:
				categoryProjectItems = GetCategoryProjectItems(searchKey.text);
				break;
			}
		}
		else if (flag2 && canSearchEvents && searchKey.kind == SearchKind.Normal)
		{
			eventSearchItems = await GetSearchItems(SearchCategory.CalendarEvent);
		}
		else
		{
			taskSearchItems = await GetSearchItems(SearchCategory.Task);
			if (canSearchEvents && searchKey.kind == SearchKind.Normal)
			{
				eventSearchItems = await GetSearchItems(SearchCategory.CalendarEvent);
			}
		}
		if (!IsCurrentSearchDisplayVersion(searchDisplayVersion))
		{
			return;
		}
		if (string.IsNullOrEmpty(searchKey.text))
		{
			List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
			if (_linkTaskIds != null && _linkTaskIds.Count > 0)
			{
				foreach (string linkTaskId in _linkTaskIds)
				{
					TaskBaseViewModel taskById = TaskCache.GetTaskById(linkTaskId);
					if (taskById != null && !string.IsNullOrEmpty(taskById.ParentId))
					{
						TaskBaseViewModel taskById2 = TaskCache.GetTaskById(taskById.ParentId);
						if (taskById2 != null)
						{
							list.Add(taskById2);
						}
					}
				}
			}
			if (list.Any())
			{
				items.Add(new SearchTitleModel
				{
					Title = Utils.GetString("LinkedParentTask")
				});
				SearchTaskItemViewModel searchTaskItemViewModel = new SearchTaskItemViewModel(list.FirstOrDefault());
				searchTaskItemViewModel.InitData(SearchKind.Parents);
				searchTaskItemViewModel.SetParentsCount(list.Select((TaskBaseViewModel x) => x.Id).Distinct().Count());
				items.Add(searchTaskItemViewModel);
				items.Add(new SearchSplitModel());
				items.Add(new SearchTitleModel
				{
					Title = Utils.GetString("LinkOtherParentTask")
				});
			}
		}
		bool needSplit = false;
		if (!isCategorySearch && !hasFilterConditions && projectTemplates != null && projectTemplates.Any() && !Utils.IsWindows7() && !string.IsNullOrEmpty(searchKey.text))
		{
			string text = searchKey.text.Trim();
			List<SearchProjectTemplateModel> list2 = new List<SearchProjectTemplateModel>();
			foreach (ProjectTemplate projectTemplate in projectTemplates)
			{
				if (!(projectTemplate.Keyword.Trim() != text))
				{
					list2.Add(new SearchProjectTemplateModel(projectTemplate));
				}
			}
			if (list2.Any())
			{
				needSplit = true;
				items.Add(new SearchTitleModel
				{
					Title = Utils.GetString("Template")
				});
				foreach (SearchProjectTemplateModel item in list2)
				{
					items.Add(item);
				}
			}
		}
		int taskGroupResultCount = taskSearchItems.Count((TaskBaseViewModel item) => searchKey.exceptTaskIds == null || !searchKey.exceptTaskIds.Contains(item.Id));
		AddTaskModels(taskSearchItems, SearchCategory.Task, "Task");
		AddTaskModels(eventSearchItems, SearchCategory.CalendarEvent, "Event");
		if (isCategorySearch)
		{
			foreach (SearchTagAndProjectModel item2 in categoryProjectItems)
			{
				item2.Selected = false;
				items.Add(item2);
			}
		}
		if (!isCategorySearch && ShouldShowNarrowedCandidates(searchFilter) && _tags.Count == 0 && searchKey.kind == SearchKind.Normal)
		{
			List<SearchTagAndProjectModel> matchModels = SearchProjectHelper.GetModelsMatched(searchKey.text, onlyProject: false, withInbox: true);
			List<SearchTagAndProjectModel> taskGroupModels = await SearchProjectHelper.GetTaskGroupsMatched(searchKey.text, withInbox: true);
			if (!IsCurrentSearchDisplayVersion(searchDisplayVersion))
			{
				return;
			}
			List<SearchTagAndProjectModel> projectModels = matchModels.Where((SearchTagAndProjectModel p) => p.IsProject).ToList();
			List<SearchTagAndProjectModel> tagModels = matchModels.Where((SearchTagAndProjectModel p) => p.IsTag).ToList();
			List<SearchTagAndProjectModel> groupModels = matchModels.Where((SearchTagAndProjectModel p) => p.IsGroup).ToList();
			List<SearchTagAndProjectModel> filterModels = matchModels.Where((SearchTagAndProjectModel p) => p.IsFilter).ToList();
			NarrowCandidatesByFilter(searchFilter, ref tagModels, ref groupModels, ref taskGroupModels, ref projectModels, ref filterModels);
			AddModels(tagModels, "Tags", _tagExpand, PtfType.Tag);
			AddModels(groupModels, "AddOrEditProjectFolder", _groupExpand, PtfType.Group);
			AddModels(projectModels, "List", _listExpand, PtfType.Project);
			AddModels(taskGroupModels, "Section", _taskGroupExpand, PtfType.TaskGroup);
			AddModels(filterModels, "Filter", _filterExpand, PtfType.Filter);
		}
		bool flag3 = searchKey.kind == SearchKind.Normal && (_selectedCategory == SearchCategory.None || _selectedCategory == SearchCategory.Task);
		bool flag4 = items.Count > 0 && flag3 && (_selectedCategory != SearchCategory.None || taskGroupResultCount < 5);
		SearchMoreVisibility = ((!flag4) ? Visibility.Collapsed : Visibility.Visible);
		EmptyGrid.Visibility = ((items.Count != 0 || (_searchKind != SearchKind.Normal && (_searchKind != SearchKind.Task || searchKey.text.Length <= 0))) ? Visibility.Collapsed : Visibility.Visible);
		if (EmptyGrid.Visibility == Visibility.Visible)
		{
			if (!string.IsNullOrWhiteSpace(searchKey.text))
			{
				SetSearchResultEmptyIcon();
			}
			else
			{
				SetCategoryEmptyIcon(_selectedCategory);
			}
		}
		bool flag5 = EmptyGrid.Visibility == Visibility.Visible && flag3;
		NoResultText.Text = (flag5 ? Utils.GetString("NotFoundYet") : Utils.GetString("SearchA"));
		TryCloudSearchText.Text = (flag5 ? Utils.GetString("TryCloudSearch") : "");
		if (EmptyGrid.Visibility == Visibility.Visible && _selectedCategory == SearchCategory.CalendarEvent)
		{
			TryCloudSearchText.Text = "";
			ChangeKeyWordsText.Text = "";
		}
		if (searchKey.kind == SearchKind.Task && string.IsNullOrEmpty(searchKey.text))
		{
			if (taskSearchItems.Count == 0)
			{
				EmptyGrid.Visibility = Visibility.Visible;
				NoTaskGridTitleTextBox.Text = Utils.GetString("no_tasks_to_link");
				ChangeKeyWordsText.Text = Utils.GetString("add_tasks_first");
			}
			else
			{
				NoTaskGridTitleTextBox.Text = Utils.GetString("SearchHint");
				ChangeKeyWordsText.Text = Utils.GetString("ChangeKeywords");
			}
		}
		_selectedIndex = -1;
		if (IsCurrentSearchDisplayVersion(searchDisplayVersion))
		{
			_searchResultPagingStates.Clear();
			_searchResultPagingStates.AddRange(pagingStates);
			_searchItemModels = items;
			SearchItemList.ItemsSource = _searchItemModels;
		}
		void AddModels(List<SearchTagAndProjectModel> models, string titleName, bool expand, PtfType type)
		{
			if (models.Count > 0)
			{
				if (needSplit)
				{
					items.Add(new SearchSplitModel());
				}
				items.Add(new SearchTitleModel
				{
					Title = Utils.GetString(titleName)
				});
				SearchLoadMoreModel searchLoadMoreModel = null;
				List<SearchTagAndProjectModel> list3 = models;
				if (models.Count > 5 && !expand)
				{
					list3 = models.Take(5).ToList();
					searchLoadMoreModel = new SearchLoadMoreModel
					{
						Type = type,
						Category = GetCategoryByType(type),
						Children = models
					};
				}
				foreach (SearchTagAndProjectModel item3 in list3)
				{
					item3.Selected = false;
					items.Add(item3);
				}
				if (searchLoadMoreModel != null)
				{
					items.Add(searchLoadMoreModel);
				}
				needSplit = true;
			}
		}
		void AddTaskModels(List<TaskBaseViewModel> searchItems, SearchCategory category, string titleKey)
		{
			searchItems = searchItems.Where((TaskBaseViewModel item) => searchKey.exceptTaskIds == null || !searchKey.exceptTaskIds.Contains(item.Id)).ToList();
			if (searchItems.Count != 0)
			{
				if (needSplit)
				{
					items.Add(new SearchSplitModel());
				}
				needSplit = true;
				if (searchKey.kind != SearchKind.Task && !isCategorySearch)
				{
					items.Add(new SearchTitleModel
					{
						Title = Utils.GetString(titleKey, (category == SearchCategory.CalendarEvent) ? Utils.GetString("Calendar") : titleKey)
					});
				}
				SearchLoadMoreModel searchLoadMoreModel = null;
				if (searchItems.Count > 5 && searchKey.kind == SearchKind.Normal && !isCategorySearch)
				{
					searchItems = searchItems.Take(5).ToList();
					searchLoadMoreModel = new SearchLoadMoreModel
					{
						IsTask = true,
						Category = category
					};
				}
				bool flag6 = ShouldPageSearchTaskResults(searchItems, searchKey.kind, isCategorySearch);
				List<TaskBaseViewModel> list3 = (flag6 ? searchItems.Take(20).ToList() : searchItems);
				foreach (TaskBaseViewModel item4 in list3)
				{
					items.Add(CreateSearchTaskItem(item4, searchKey.kind));
				}
				if (flag6)
				{
					pagingStates.Add(new SearchTaskResultPagingState(searchItems, searchKey.kind, list3.Count));
				}
				if (searchLoadMoreModel != null)
				{
					items.Add(searchLoadMoreModel);
				}
			}
		}
		static SearchCategory GetCategoryByType(PtfType type)
		{
			return type switch
			{
				PtfType.Tag => SearchCategory.Tag, 
				PtfType.Project => SearchCategory.List, 
				PtfType.Filter => SearchCategory.Filter, 
				_ => SearchCategory.None, 
			};
		}
		List<SearchTagAndProjectModel> GetCategoryProjectItems(string text2)
		{
			List<SearchTagAndProjectModel> modelsMatched = SearchProjectHelper.GetModelsMatched(text2, onlyProject: false, withInbox: true);
			return _selectedCategory switch
			{
				SearchCategory.Tag => modelsMatched.Where((SearchTagAndProjectModel p) => p.IsTag).ToList(), 
				SearchCategory.List => modelsMatched.Where((SearchTagAndProjectModel p) => p.IsProject).ToList(), 
				SearchCategory.Filter => modelsMatched.Where((SearchTagAndProjectModel p) => p.IsFilter).ToList(), 
				_ => new List<SearchTagAndProjectModel>(), 
			};
		}
		async Task<List<TaskBaseViewModel>> GetSearchItems(SearchCategory category)
		{
			SearchHelper.ClearTaskSearchModels();
			return await TaskDisplayService.GetDisplayTaskInSearch(searchKey.kind, searchKey.text, filter, _tags, local: true, searchKey.exceptTaskIds, category);
		}
		static bool IsNotConcreteProjectFilterId(string id)
		{
			return CalendarUtils.IsNotConcreteProjectFilterId(id);
		}
		static void NarrowCandidatesByFilter(SearchFilterModel model, ref List<SearchTagAndProjectModel> reference, ref List<SearchTagAndProjectModel> reference2, ref List<SearchTagAndProjectModel> reference4, ref List<SearchTagAndProjectModel> reference3, ref List<SearchTagAndProjectModel> reference5)
		{
			if (model.HasFilterConditions())
			{
				List<string> tags = model.Tags;
				if (tags != null && tags.Count > 0)
				{
					HashSet<string> selectedTags = new HashSet<string>(model.Tags, StringComparer.OrdinalIgnoreCase);
					reference = reference.Where((SearchTagAndProjectModel p) => selectedTags.Contains(p.Id)).ToList();
				}
				else
				{
					reference.Clear();
				}
				List<string> groupIds = model.GroupIds;
				if (groupIds != null && groupIds.Count > 0)
				{
					HashSet<string> selectedGroups = new HashSet<string>(model.GroupIds);
					reference2 = reference2.Where((SearchTagAndProjectModel p) => selectedGroups.Contains(p.Id)).ToList();
				}
				else
				{
					reference2.Clear();
				}
				List<string> projectIds = model.ProjectIds;
				if (projectIds != null && projectIds.Count > 0)
				{
					HashSet<string> selectedProjects = new HashSet<string>(model.ProjectIds.Where((string id) => !IsNotConcreteProjectFilterId(id)));
					reference3 = reference3.Where((SearchTagAndProjectModel p) => selectedProjects.Contains(p.Id)).ToList();
				}
				else
				{
					reference3.Clear();
				}
				HashSet<string> selectedTaskGroupProjects = new HashSet<string>();
				List<string> projectIds2 = model.ProjectIds;
				if (projectIds2 != null && projectIds2.Count > 0)
				{
					selectedTaskGroupProjects.UnionWith(model.ProjectIds.Where((string id) => !IsNotConcreteProjectFilterId(id)));
				}
				List<string> groupIds2 = model.GroupIds;
				if (groupIds2 != null && groupIds2.Count > 0)
				{
					HashSet<string> selectedProjectFolders = new HashSet<string>(model.GroupIds);
					selectedTaskGroupProjects.UnionWith(from project in CacheManager.GetProjects()
						where selectedProjectFolders.Contains(project.groupId)
						select project.id);
				}
				reference4 = ((selectedTaskGroupProjects.Count > 0) ? reference4.Where((SearchTagAndProjectModel searchTagAndProjectModel) => selectedTaskGroupProjects.Contains(searchTagAndProjectModel.ProjectId)).ToList() : new List<SearchTagAndProjectModel>());
				reference5.Clear();
			}
		}
		static bool ShouldShowNarrowedCandidates(SearchFilterModel model)
		{
			if (!model.HasFilterConditions())
			{
				return true;
			}
			List<string> tags = model.Tags;
			if (tags == null || tags.Count <= 0)
			{
				List<string> groupIds = model.GroupIds;
				if (groupIds == null || groupIds.Count <= 0)
				{
					return model.ProjectIds?.Any((string id) => !IsNotConcreteProjectFilterId(id)) ?? false;
				}
			}
			return true;
		}
	}

	private static bool ShouldPageSearchTaskResults(IReadOnlyCollection<TaskBaseViewModel> searchItems, SearchKind kind, bool isCategorySearch)
	{
		if (kind == SearchKind.Task || (isCategorySearch && kind == SearchKind.Normal))
		{
			return searchItems.Count > 20;
		}
		return false;
	}

	private static SearchTaskItemViewModel CreateSearchTaskItem(TaskBaseViewModel item, SearchKind kind)
	{
		SearchTaskItemViewModel searchTaskItemViewModel = new SearchTaskItemViewModel(item);
		searchTaskItemViewModel.InitData(kind);
		return searchTaskItemViewModel;
	}

	private void OnSearchItemListScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (!(e.VerticalChange <= 0.0) && !(e.ViewportHeight <= 0.0) && !(e.ExtentHeight <= 0.0))
		{
			TryLoadMoreSearchResults(e.VerticalOffset, e.ViewportHeight, e.ExtentHeight, _searchDisplayVersion);
		}
	}

	private void TryLoadMoreSearchResults(double verticalOffset, double viewportHeight, double extentHeight, int searchDisplayVersion)
	{
		if (!IsCurrentSearchDisplayVersion(searchDisplayVersion) || IsLoadingSearchResultPage(searchDisplayVersion) || !HasMorePagedSearchResults() || extentHeight - verticalOffset - viewportHeight > 120.0)
		{
			return;
		}
		_loadingNextSearchResultPage = true;
		_loadingNextSearchResultPageVersion = searchDisplayVersion;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			try
			{
				if (IsCurrentSearchDisplayVersion(searchDisplayVersion))
				{
					AppendNextSearchResultPage();
				}
			}
			finally
			{
				if (_loadingNextSearchResultPageVersion == searchDisplayVersion)
				{
					_loadingNextSearchResultPage = false;
					_loadingNextSearchResultPageVersion = 0;
				}
			}
		}, DispatcherPriority.Input);
	}

	private void AppendNextSearchResultPage()
	{
		SearchTaskResultPagingState searchTaskResultPagingState = _searchResultPagingStates.FirstOrDefault((SearchTaskResultPagingState state) => state.HasMore);
		if (searchTaskResultPagingState == null)
		{
			return;
		}
		foreach (TaskBaseViewModel item in searchTaskResultPagingState.TakeNext(20))
		{
			_searchItemModels.Add(CreateSearchTaskItem(item, searchTaskResultPagingState.Kind));
		}
	}

	private bool HasMorePagedSearchResults()
	{
		return _searchResultPagingStates.Any((SearchTaskResultPagingState state) => state.HasMore);
	}

	private void ResetSearchResultPaging()
	{
		_searchResultPagingStates.Clear();
		_loadingNextSearchResultPage = false;
		_loadingNextSearchResultPageVersion = 0;
	}

	private int BeginSearchDisplayUpdate()
	{
		ResetSearchResultPaging();
		return ++_searchDisplayVersion;
	}

	private bool IsCurrentSearchDisplayVersion(int searchDisplayVersion)
	{
		return searchDisplayVersion == _searchDisplayVersion;
	}

	private bool IsLoadingSearchResultPage(int searchDisplayVersion)
	{
		if (_loadingNextSearchResultPage)
		{
			return _loadingNextSearchResultPageVersion == searchDisplayVersion;
		}
		return false;
	}

	private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
	{
		if (parent == null)
		{
			return null;
		}
		int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childrenCount; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is T result)
			{
				return result;
			}
			T val = FindVisualChild<T>(child);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	private async void SetHintVisibility(string text)
	{
		if (string.IsNullOrEmpty(text) && GetInputTags().Count == 0)
		{
			HintText.Visibility = Visibility.Visible;
			return;
		}
		await Task.Delay(50);
		HintText.Visibility = Visibility.Collapsed;
	}

	private async Task<bool> TryShowTagPopup(string text)
	{
		if (_isTagOpen)
		{
			return true;
		}
		if (text.EndsWith("#"))
		{
			if (Utils.IsWindows7())
			{
				await Task.Delay(50);
			}
			TagSelectAddWindow tagSelectAddWindow = new TagSelectAddWindow(canCreateTag: false);
			tagSelectAddWindow.TagItems.TagSelected -= OnTagAdded;
			tagSelectAddWindow.TagItems.TagSelected += OnTagAdded;
			tagSelectAddWindow.TagExit -= OnTagExit;
			tagSelectAddWindow.TagExit += OnTagExit;
			tagSelectAddWindow.Closed -= OnTagPopClosed;
			tagSelectAddWindow.Closed += OnTagPopClosed;
			Rect characterRect = TitleTextBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
			Point popupOffset = Utils.GetPopupOffset(App.Window, TitleTextBox, characterRect.X, characterRect.Y);
			tagSelectAddWindow.Show(popupOffset.X, popupOffset.Y);
			_isTagOpen = true;
			return true;
		}
		return false;
	}

	private void OnTagExit(object sender, string text)
	{
		Run tagRun = GetTagRun();
		if (tagRun != null)
		{
			string text2 = tagRun.Text;
			if (string.IsNullOrEmpty(text) && text2.EndsWith("#"))
			{
				text2 = text2.Substring(0, text2.Length - 1);
			}
			tagRun.Text = text2 + ((text != null && text.Length >= 1) ? text.Substring(1) : text);
			FocusEnd();
		}
	}

	private void OnTagPopClosed(object sender, EventArgs e)
	{
		SetSearchItem(GetContentText());
		_isTagOpen = false;
	}

	private void OnTagAdded(object sender, string tag)
	{
		TitleTextBox.TextChanged -= TaskTextChanged;
		TryAddTag(tag);
		TryAdjustTextAlignment();
		TitleTextBox.TextChanged += TaskTextChanged;
		SetSearchItem(GetContentText());
		SetHintVisibility(GetContentText());
	}

	private void TryAdjustTextAlignment()
	{
		InlineCollection textInlines = GetTextInlines();
		if (textInlines == null || textInlines.Count <= 0)
		{
			return;
		}
		foreach (Inline item in textInlines)
		{
			item.BaselineAlignment = BaselineAlignment.Top;
		}
	}

	private void TryAddTag(string tag)
	{
		_inputText = null;
		string text = "#" + tag;
		if (GetInputTags().Contains(tag))
		{
			FocusEnd();
			return;
		}
		Run tagRun = GetTagRun();
		if (tagRun == null)
		{
			return;
		}
		string text2 = tagRun.Text;
		if (text2.EndsWith("#"))
		{
			InlineCollection textInlines = GetTextInlines();
			string text3 = text2.Substring(0, text2.Length - 1).Trim();
			if (!string.IsNullOrEmpty(text3) || GetInputTags().Count > 0)
			{
				text3 = text3.TrimEnd() + " ";
			}
			Run newItem = new Run(text3);
			Run newItem2 = new Run(" ");
			InlineUIContainer inlineUIContainer = new InlineUIContainer(new QuickAddDisplayControl
			{
				Text = 
				{
					Text = text
				}
			});
			textInlines.InsertAfter(tagRun, inlineUIContainer);
			textInlines.InsertBefore(inlineUIContainer, newItem);
			textInlines.InsertAfter(inlineUIContainer, newItem2);
			textInlines.Remove(tagRun);
			RemoveEmptyRuns();
			FocusEnd();
		}
	}

	public async void FocusEnd(int deley = 100, bool isStart = false)
	{
		await Task.Delay(deley);
		TitleTextBox.Focus();
		TitleTextBox.CaretPosition = TitleTextBox.CaretPosition.DocumentEnd;
		if (isStart)
		{
			OnTextChanged();
			TitleTextBox.SelectAll();
		}
	}

	public async void InitSearch(bool restore, SearchKind kind, List<string> linkTaskIds)
	{
		_searchKind = kind;
		_restore = restore;
		_linkTaskIds = linkTaskIds;
		_selectedCategory = (restore ? SearchHelper.SearchCategory : SearchCategory.None);
		_searchFilter = new SearchFilterViewModel();
		if (restore)
		{
			_searchFilter.ApplySearchFilter(SearchHelper.SearchFilter.ToSearchFilter());
		}
		else
		{
			_searchFilter.ResetAllFilterConditions();
		}
		_searchFilter.Kind = _searchKind;
		_searchFilter.Category = _selectedCategory;
		_searchFilter.SearchKey = (restore ? SearchHelper.SearchKey : string.Empty);
		_searchFilter.SearchTags = (restore ? (SearchHelper.Tags ?? new List<string>()) : new List<string>());
		_searchFilter.SyncSearchTypeFromCategory();
		_searchFilter.RefreshCategoryState();
		PopupFilterControl.UseInSearchPopup(_searchFilter);
		SearchHelper.ClearTaskSearchModels();
		InitCategoryModels();
		if (restore)
		{
			_searchExtra = new SearchExtra
			{
				SearchKey = SearchHelper.SearchKey,
				Tags = SearchHelper.Tags,
				LinkTaskIds = linkTaskIds,
				Category = _selectedCategory
			};
			_tags = SearchHelper.Tags ?? new List<string>();
			SetTitleText(_searchExtra);
			OnTextChanged(forceSearch: true);
		}
		else
		{
			_inputText = null;
			_tags = new List<string>();
			SetSearchItem(string.Empty);
		}
		TitleTextBox.TextChanged += TaskTextChanged;
		if (_searchKind == SearchKind.Task)
		{
			NoTaskGridTitleTextBox.Text = Utils.GetString("Search");
			ChangeKeyWordsText.Text = Utils.GetString("ChangeKeywords");
			TryCloudSearchText.Text = "";
			SearchItemList.SetValue(Grid.RowSpanProperty, 2);
		}
		else
		{
			ChangeKeyWordsText.Text = "";
			SearchItemList.SetValue(Grid.RowSpanProperty, 1);
		}
	}

	private void InitCategoryModels()
	{
		UpdateCategoryOptionVisibility();
		if (_searchKind != SearchKind.Normal)
		{
			PopupFilterControl.Visibility = Visibility.Collapsed;
			return;
		}
		SearchFilter.Category = _selectedCategory;
		SearchFilter.SyncSearchTypeFromCategory();
		SearchFilter.RefreshCategoryState();
		PopupFilterControl.Reset();
		UpdateCategoryOptionVisibility();
	}

	private void UpdateCategoryOptionVisibility()
	{
		if (CategoryOptionPanel == null || PopupFilterControl == null)
		{
			return;
		}
		if (_searchKind != SearchKind.Normal)
		{
			CategoryOptionPanel.Visibility = Visibility.Collapsed;
			PopupFilterControl.Visibility = Visibility.Collapsed;
			return;
		}
		bool flag = _selectedCategory == SearchCategory.None;
		CategoryOptionPanel.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		PopupFilterControl.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		List<SearchTagAndProjectModel> modelsMatched = SearchProjectHelper.GetModelsMatched(string.Empty, onlyProject: false, withInbox: true);
		TagCategoryOption.Visibility = ((!modelsMatched.Any((SearchTagAndProjectModel model) => model.IsTag)) ? Visibility.Collapsed : Visibility.Visible);
		ListCategoryOption.Visibility = ((!modelsMatched.Any((SearchTagAndProjectModel model) => model.IsProject)) ? Visibility.Collapsed : Visibility.Visible);
		FilterCategoryOption.Visibility = ((!modelsMatched.Any((SearchTagAndProjectModel model) => model.IsFilter)) ? Visibility.Collapsed : Visibility.Visible);
		CalendarEventCategoryOption.Visibility = ((!HasVisibleCalendar()) ? Visibility.Collapsed : Visibility.Visible);
		CalendarEventCategoryText.Text = Utils.GetString("Event", Utils.GetString("Calendar"));
	}

	private static bool HasVisibleCalendar()
	{
		if (!CacheManager.GetSubscribeCalendars().Any((CalendarSubscribeProfileModel calendar) => calendar.Show != "hidden"))
		{
			return CacheManager.GetBindCalendars().Any((BindCalendarModel calendar) => calendar.Show != "hidden");
		}
		return true;
	}

	private List<string> GetInputTags()
	{
		List<string> list = new List<string>();
		InlineCollection textInlines = GetTextInlines();
		if (textInlines != null)
		{
			foreach (Inline item in textInlines)
			{
				if (item is InlineUIContainer { Child: not null, Child: QuickAddDisplayControl child })
				{
					list.Add(child.Text.Text.Replace("#", string.Empty).Trim());
				}
			}
		}
		return list;
	}

	private void RemoveEmptyRuns()
	{
		List<Inline> list = GetTextInlines().ToList();
		for (int i = 0; i < list.Count - 1; i++)
		{
			Inline inline = list[i];
			Inline inline2 = list[i + 1];
			if (inline is Run run && inline2 is Run run2 && run.Text == " " && run2.Text == " ")
			{
				run2.Text = string.Empty;
			}
		}
	}

	private Run GetTagRun()
	{
		InlineCollection textInlines = GetTextInlines();
		if (textInlines != null)
		{
			foreach (Inline item in textInlines)
			{
				if (item is Run run && run.Text.EndsWith("#"))
				{
					return run;
				}
			}
		}
		return null;
	}

	private void OnTagOrProjectSelected(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: SearchTagAndProjectModel dataContext }))
		{
			return;
		}
		try
		{
			int num = _searchItemModels.IndexOf(dataContext);
			if (num > 0)
			{
				for (int num2 = num; num2 >= 0; num2--)
				{
					if (_searchItemModels[num2] is SearchTitleModel)
					{
						UserActCollectUtils.AddClickEvent("search", "search_result_order", (dataContext.IsTag ? "tag" : (dataContext.IsProject ? "list" : (dataContext.IsTaskGroup ? "group" : (dataContext.IsGroup ? "folder" : "filter")))) + "_order_" + (num - num2));
						break;
					}
				}
			}
		}
		catch (Exception)
		{
		}
		if (dataContext.IsTag)
		{
			UserActCollectUtils.AddClickEvent("search", "search_type", "tag");
			AddClickEvent("click_tag");
			App.Window.NavigateProject("tag", dataContext.Id.ToLower());
		}
		else if (dataContext.IsProject)
		{
			UserActCollectUtils.AddClickEvent("search", "search_type", "list");
			AddClickEvent("click_list");
			App.Window.NavigateProject("project", dataContext.Id);
		}
		else if (dataContext.IsFilter)
		{
			UserActCollectUtils.AddClickEvent("search", "search_type", "filter");
			AddClickEvent("click_filter");
			App.Window.NavigateProject("filter", dataContext.Id);
		}
		else if (dataContext.IsTaskGroup)
		{
			UserActCollectUtils.AddClickEvent("search", "search_type", "group");
			AddClickEvent("click_group");
			App.Window.NavigateColumn(dataContext.ProjectId, dataContext.Id);
		}
		else if (dataContext.IsGroup)
		{
			UserActCollectUtils.AddClickEvent("search", "search_type", "group");
			AddClickEvent("click_group");
			App.Window.NavigateProject("group", dataContext.Id);
		}
		this.Close?.Invoke(this, null);
		_keepOldText = false;
		SearchHistoryDao.AddHistoryModel(GetContentText(), GetInputTags());
	}

	private void SearchHistoryClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: SearchHistoryViewModel dataContext })
		{
			SearchHistory(dataContext);
		}
	}

	private void SearchHistory(SearchHistoryViewModel model)
	{
		_searchExtra = new SearchExtra
		{
			SearchKey = model.SearchKey,
			Tags = model.Tags
		};
		_inputText = null;
		_selectedCategory = SearchCategory.None;
		_searchFilter = new SearchFilterViewModel();
		_searchFilter.Category = SearchCategory.None;
		_searchFilter.RefreshCategoryState();
		PopupFilterControl.UseInSearchPopup(_searchFilter);
		UpdateCategoryOptionVisibility();
		SetTitleText(_searchExtra);
		OnTextChanged(forceSearch: true);
	}

	private void SetTitleText(SearchExtra searchExtra)
	{
		Paragraph paragraph = new Paragraph();
		InlineCollection inlines = paragraph.Inlines;
		if (searchExtra.Tags != null)
		{
			foreach (string tag in searchExtra.Tags)
			{
				InlineUIContainer item = new InlineUIContainer(new QuickAddDisplayControl
				{
					Text = 
					{
						Text = "#" + tag
					}
				});
				inlines.Add(item);
				inlines.Add(new Run(" "));
			}
		}
		if (!string.IsNullOrEmpty(searchExtra.SearchKey))
		{
			inlines.Add(new Run(searchExtra.SearchKey)
			{
				Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80")
			});
		}
		TitleTextBox.Document.Blocks.Clear();
		TitleTextBox.Document.Blocks.Add(paragraph);
		TitleTextBox.CaretPosition = TitleTextBox.CaretPosition.DocumentEnd;
		TryAdjustTextAlignment();
	}

	private void OnTextKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Up:
			MoveUp();
			break;
		case Key.Down:
			MoveDown();
			break;
		case Key.Tab:
			if (Utils.IfShiftPressed())
			{
				MoveUp();
			}
			else
			{
				MoveDown();
			}
			break;
		case Key.Return:
			if (SelectItem())
			{
				e.Handled = true;
			}
			break;
		}
	}

	private bool SelectItem()
	{
		if (SearchItemList.IsHitTestVisible)
		{
			if (_selectedIndex == -1 || _selectedIndex == -2)
			{
				AddClickEvent("click_task");
				TryDelaySearch();
				return true;
			}
			if (_selectedIndex >= 0 && _selectedIndex < _searchItemModels.Count)
			{
				SearchItemBaseModel searchItemBaseModel = _searchItemModels[_selectedIndex];
				if (searchItemBaseModel is SearchTagAndProjectModel searchTagAndProjectModel)
				{
					this.Close?.Invoke(this, null);
					if (searchTagAndProjectModel.IsTag)
					{
						App.SelectTagProject(searchTagAndProjectModel.Id.ToLower());
					}
					else if (searchTagAndProjectModel.IsFilter)
					{
						string id = searchItemBaseModel.Id;
						App.Window.NavigateFilter(id);
					}
					else if (searchTagAndProjectModel.IsGroup)
					{
						string id2 = searchItemBaseModel.Id;
						App.Window.NavigateGroup(id2);
					}
					else if (searchTagAndProjectModel.IsTaskGroup)
					{
						App.Window.NavigateColumn(searchTagAndProjectModel.ProjectId, searchTagAndProjectModel.Id);
					}
					else
					{
						string id3 = searchItemBaseModel.Id;
						App.Window.NavigateNormalProject(id3);
					}
					return true;
				}
				AddClickEvent("click_task");
				TryDelaySearch((searchItemBaseModel as SearchTaskItemViewModel)?.SourceModel.Id);
			}
			else
			{
				TryDelaySearch();
			}
		}
		else if (SearchHistoryList.IsVisible && _selectedIndex >= 0 && _searchHistoryModels[_selectedIndex] != null)
		{
			SearchHistory(_searchHistoryModels[_selectedIndex]);
			return true;
		}
		return false;
	}

	private void MoveDown()
	{
		if (SearchItemList.IsVisible)
		{
			foreach (SearchItemBaseModel searchItemModel in _searchItemModels)
			{
				searchItemModel.Selected = false;
			}
			if (_searchItemModels.Count <= 0)
			{
				return;
			}
			while (++_selectedIndex < _searchItemModels.Count)
			{
				if (_searchItemModels[_selectedIndex].CanSelect)
				{
					_searchItemModels[_selectedIndex].Selected = true;
					SearchItemList.ScrollIntoView(SearchItemList.Items[_selectedIndex]);
					break;
				}
			}
			if (_selectedIndex >= _searchItemModels.Count)
			{
				_selectedIndex = -1;
			}
		}
		else
		{
			if (_searchHistoryModels.Count <= 0)
			{
				return;
			}
			foreach (SearchHistoryViewModel searchHistoryModel in _searchHistoryModels)
			{
				searchHistoryModel.Selected = false;
			}
			if (++_selectedIndex < _searchHistoryModels.Count)
			{
				_searchHistoryModels[_selectedIndex].Selected = true;
			}
			else
			{
				_selectedIndex = -1;
			}
		}
	}

	private void MoveUp()
	{
		if (SearchItemList.IsVisible)
		{
			foreach (SearchItemBaseModel searchItemModel in _searchItemModels)
			{
				searchItemModel.Selected = false;
			}
			if (_searchItemModels.Count <= 0)
			{
				return;
			}
			if (_selectedIndex <= -1)
			{
				_selectedIndex = _searchItemModels.Count;
			}
			while (--_selectedIndex >= 0)
			{
				if (_searchItemModels[_selectedIndex].CanSelect)
				{
					_searchItemModels[_selectedIndex].Selected = true;
					SearchItemList.ScrollIntoView(SearchItemList.Items[_selectedIndex]);
					break;
				}
			}
			return;
		}
		ObservableCollection<SearchHistoryViewModel> searchHistoryModels = _searchHistoryModels;
		if (searchHistoryModels == null || searchHistoryModels.Count <= 0)
		{
			return;
		}
		foreach (SearchHistoryViewModel searchHistoryModel in _searchHistoryModels)
		{
			searchHistoryModel.Selected = false;
		}
		_selectedIndex--;
		if (_selectedIndex != -1)
		{
			if (_selectedIndex < -1 || _selectedIndex >= _searchHistoryModels.Count)
			{
				_selectedIndex = _searchHistoryModels.Count - 1;
			}
			_searchHistoryModels[_selectedIndex].Selected = true;
		}
	}

	private void OnItemHover(object sender, MouseEventArgs e)
	{
		foreach (SearchItemBaseModel searchItemModel in _searchItemModels)
		{
			searchItemModel.Selected = false;
		}
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		if (frameworkElement.DataContext is SearchItemBaseModel searchItemBaseModel)
		{
			if (_selectedIndex >= 0)
			{
				_searchItemModels[_selectedIndex].Selected = false;
			}
			if (searchItemBaseModel.CanSelect)
			{
				searchItemBaseModel.Selected = true;
				_selectedIndex = _searchItemModels.IndexOf(searchItemBaseModel);
			}
		}
		else if (frameworkElement.DataContext is SearchHistoryViewModel searchHistoryViewModel)
		{
			if (_selectedIndex >= 0)
			{
				_searchHistoryModels[_selectedIndex].Selected = false;
			}
			searchHistoryViewModel.Selected = true;
			_selectedIndex = _searchHistoryModels.IndexOf(searchHistoryViewModel);
		}
	}

	private void OnItemMouseLeave(object sender, MouseEventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			return;
		}
		if (frameworkElement.DataContext is SearchTagAndProjectModel searchTagAndProjectModel)
		{
			if (searchTagAndProjectModel.Selected)
			{
				_selectedIndex = -1;
				searchTagAndProjectModel.Selected = false;
			}
		}
		else if (frameworkElement.DataContext is SearchTaskItemViewModel searchTaskItemViewModel)
		{
			if (searchTaskItemViewModel.Selected)
			{
				_selectedIndex = -1;
				searchTaskItemViewModel.Selected = false;
			}
		}
		else if (frameworkElement.DataContext is SearchHistoryViewModel { Selected: not false } searchHistoryViewModel)
		{
			_selectedIndex = -1;
			searchHistoryViewModel.Selected = false;
		}
	}

	private async void OnClearHistoryClick(object sender, MouseButtonEventArgs e)
	{
		_selectedIndex = -1;
		await SearchHistoryDao.ClearHistory();
		TitleTextBox.Focus();
		SetSearchItem(string.Empty);
	}

	private void CloseClick(object sender, MouseButtonEventArgs e)
	{
		this.Close?.Invoke(this, null);
	}

	public bool CanDragMove()
	{
		if (!CloseGrid.IsMouseOver && !ClearGrid.IsMouseOver && !CategoryOptionPanel.IsMouseOver && !PopupFilterControl.IsMouseOver && !SearchHistoryPage.IsMouseOver && !_isSearchMoreMouseOver)
		{
			return !NoTaskGridTitleTextBoxOnSearch.IsMouseOver;
		}
		return false;
	}

	private void OnSearchMoreMouseEnter(object sender, MouseEventArgs e)
	{
		_isSearchMoreMouseOver = true;
	}

	private void OnSearchMoreMouseLeave(object sender, MouseEventArgs e)
	{
		_isSearchMoreMouseOver = false;
	}

	private void OnPopupFilterChanged(object sender, EventArgs e)
	{
		if (_searchKind == SearchKind.Normal)
		{
			SearchCategory selectedCategory = _selectedCategory;
			_selectedCategory = SearchFilter.Category;
			if (selectedCategory != SearchCategory.None && _selectedCategory == SearchCategory.None)
			{
				IgnoreCategoryOptionClickUntilMouseUp();
			}
			SearchHelper.ClearTaskSearchModels();
			_inputText = null;
			UpdateCategoryOptionVisibility();
			SetSearchItem(GetContentText());
		}
	}

	private void IgnoreCategoryOptionClickUntilMouseUp()
	{
		_ignoreCategoryOptionClickUntil = DateTime.UtcNow.AddSeconds(2.0);
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			_ignoreCategoryOptionClickUntilMouseUp = true;
			Mouse.Capture(this);
		}
	}

	private void OnSearchInputPreviewMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_ignoreCategoryOptionClickUntilMouseUp && e.ChangedButton == MouseButton.Left)
		{
			ClearCategoryOptionClickIgnore();
			e.Handled = true;
		}
	}

	private void ClearCategoryOptionClickIgnore()
	{
		_ignoreCategoryOptionClickUntilMouseUp = false;
		_ignoreCategoryOptionClickUntil = DateTime.MinValue;
		if (Mouse.Captured == this)
		{
			Mouse.Capture(null);
		}
	}

	private void CategoryOptionClick(object sender, MouseButtonEventArgs e)
	{
		if (_ignoreCategoryOptionClickUntilMouseUp || DateTime.UtcNow < _ignoreCategoryOptionClickUntil)
		{
			ClearCategoryOptionClickIgnore();
			e.Handled = true;
		}
		else if (sender is FrameworkElement { Tag: string tag })
		{
			switch (tag)
			{
			case "Task":
				SetSelectedCategory(SearchCategory.Task);
				break;
			case "Tag":
				SetSelectedCategory(SearchCategory.Tag);
				break;
			case "List":
				SetSelectedCategory(SearchCategory.List);
				break;
			case "Filter":
				SetSelectedCategory(SearchCategory.Filter);
				break;
			case "CalendarEvent":
				SetSelectedCategory(SearchCategory.CalendarEvent);
				break;
			}
		}
	}

	private void ClearClick(object sender, MouseButtonEventArgs e)
	{
		TitleTextBox.Document.Blocks.Clear();
		OnTextChanged();
	}

	private void SetSelectedCategory(SearchCategory category)
	{
		_selectedCategory = category;
		SearchFilter.Category = category;
		SearchFilter.SyncSearchTypeFromCategory();
		SearchFilter.RefreshCategoryState();
		SearchHelper.ClearTaskSearchModels();
		PopupFilterControl.Reset();
		UpdateCategoryOptionVisibility();
		_inputText = null;
		SetSearchItem(GetContentText());
	}

	private void OnLoadMoreClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: SearchLoadMoreModel dataContext }))
		{
			return;
		}
		if (dataContext.IsTask)
		{
			if (dataContext.Category != SearchCategory.None)
			{
				_selectedCategory = dataContext.Category;
				SearchFilter.Category = dataContext.Category;
				SearchFilter.RefreshCategoryState();
				UpdateCategoryOptionVisibility();
			}
			TryDelaySearch();
			AddClickEvent("view_more_task");
			return;
		}
		if (dataContext.Category != SearchCategory.None && _selectedCategory == SearchCategory.None)
		{
			switch (dataContext.Category)
			{
			case SearchCategory.Tag:
				AddClickEvent("expand_tag");
				break;
			case SearchCategory.List:
				AddClickEvent("expand_list");
				break;
			case SearchCategory.Filter:
				AddClickEvent("expand_filter");
				break;
			}
			SetSelectedCategory(dataContext.Category);
			return;
		}
		List<SearchTagAndProjectModel> children = dataContext.Children;
		if (children == null || children.Count <= 5)
		{
			return;
		}
		int num = _searchItemModels.IndexOf(dataContext);
		if (num > 0)
		{
			_searchItemModels.RemoveAt(num);
			switch (dataContext.Type)
			{
			case PtfType.Project:
				AddClickEvent("expand_list");
				_listExpand = true;
				break;
			case PtfType.Tag:
				AddClickEvent("expand_tag");
				_tagExpand = true;
				break;
			case PtfType.Filter:
				AddClickEvent("expand_filter");
				_filterExpand = true;
				break;
			case PtfType.Group:
				AddClickEvent("expand_folder");
				_groupExpand = true;
				break;
			case PtfType.TaskGroup:
				AddClickEvent("expand_group");
				_taskGroupExpand = true;
				break;
			}
			for (int i = 5; i < dataContext.Children.Count; i++)
			{
				SearchTagAndProjectModel item = dataContext.Children[i];
				_searchItemModels.Insert(num, item);
				num++;
			}
		}
	}

	public void Dispose()
	{
		this.Search = null;
		this.Close = null;
		SearchItemList.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnSearchItemListScrollChanged));
		ResetSearchResultPaging();
		_searchActionHandler.Dispose();
	}

	private void OnSearchItemSelected(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: SearchTaskItemViewModel { IsParents: false } dataContext }))
		{
			return;
		}
		try
		{
			int num = _searchItemModels.IndexOf(dataContext);
			if (num > 0)
			{
				for (int num2 = num; num2 >= 0; num2--)
				{
					if (_searchItemModels[num2] is SearchTitleModel)
					{
						UserActCollectUtils.AddClickEvent("search", "search_result_order", "task_order_" + (num - num2));
					}
				}
			}
		}
		catch (Exception)
		{
		}
		TryDelaySearch(dataContext.SourceModel.Id);
		AddClickEvent("click_task");
	}

	public void OnItemCheckClick()
	{
		SearchHelper.ClearTaskSearchModels();
		SearchHelper.GetTaskSearchModels(SearchFilter.ToSearchFilter(), GetInputTags());
		string contentText = GetContentText();
		GetSearchDisplayModels((text: contentText, kind: _searchKind, exceptTaskIds: new List<string>()), BeginSearchDisplayUpdate());
		SearchHistoryDao.AddHistoryModel(contentText, GetInputTags());
	}

	public void OnItemProjectClick(string projectId)
	{
		App.Window.StopSearch(forceSwitch: false);
		App.Window.NavigateNormalProject(projectId);
		this.Close?.Invoke(this, null);
		_keepOldText = false;
	}

	public async Task OnUnlinkParent()
	{
		App.Window.StopSearch(forceSwitch: false);
		this.Close?.Invoke(this, null);
		if (_linkTaskIds != null || _linkTaskIds.Any())
		{
			await TaskDao.BatchUpdateParent(_linkTaskIds, string.Empty, null, canUndo: false);
			TaskChangeNotifier.NotifyTaskParentChanged(_linkTaskIds);
			App.Window?.TryToastString(null, Utils.GetString("LinkRemoved"));
		}
	}

	private void OnTryCloudSearchClick(object sender, MouseButtonEventArgs e)
	{
		AddClickEvent("view_more_task");
		TryDelaySearch();
	}

	private void AddClickEvent(string label)
	{
		if (!(label == "click_task") || _linkTaskIds == null)
		{
			UserActCollectUtils.AddClickEvent("search", "search_page", label);
		}
	}

	private void OnProjectTemplateSelected(object sender, MouseButtonEventArgs e)
	{
		this.Close?.Invoke(this, null);
		if (sender is FrameworkElement { DataContext: SearchProjectTemplateModel dataContext })
		{
			NavigateWebBrowserWindow navigateWebBrowserWindow = new NavigateWebBrowserWindow("/webview/templates/project/" + dataContext.Id);
			navigateWebBrowserWindow.Title = dataContext.Name;
			navigateWebBrowserWindow.Width = 455.0;
			navigateWebBrowserWindow.Height = 560.0;
			navigateWebBrowserWindow.Owner = App.Window;
			navigateWebBrowserWindow.ShowDialog();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchinputcontrol.xaml", UriKind.Relative);
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
			Root = (SearchInputControl)target;
			break;
		case 6:
			FocusHighlightLine = (Line)target;
			break;
		case 7:
			HintText = (TextBlock)target;
			break;
		case 8:
			Visual = (ScrollViewer)target;
			break;
		case 9:
			TitleTextBox = (RichTextBox)target;
			TitleTextBox.PreviewKeyDown += OnTextKeyDown;
			TitleTextBox.PreviewMouseLeftButtonUp += OnTextBoxClick;
			break;
		case 10:
			ClearGrid = (Border)target;
			ClearGrid.MouseLeftButtonUp += ClearClick;
			break;
		case 11:
			CloseGrid = (Border)target;
			CloseGrid.MouseLeftButtonUp += CloseClick;
			break;
		case 12:
			CategoryOptionPanel = (StackPanel)target;
			break;
		case 13:
			((Border)target).MouseLeftButtonUp += CategoryOptionClick;
			break;
		case 14:
			TagCategoryOption = (Border)target;
			TagCategoryOption.MouseLeftButtonUp += CategoryOptionClick;
			break;
		case 15:
			ListCategoryOption = (Border)target;
			ListCategoryOption.MouseLeftButtonUp += CategoryOptionClick;
			break;
		case 16:
			FilterCategoryOption = (Border)target;
			FilterCategoryOption.MouseLeftButtonUp += CategoryOptionClick;
			break;
		case 17:
			CalendarEventCategoryOption = (Border)target;
			CalendarEventCategoryOption.MouseLeftButtonUp += CategoryOptionClick;
			break;
		case 18:
			CalendarEventCategoryText = (TextBlock)target;
			break;
		case 19:
			PopupFilterControl = (SearchFilterControl)target;
			break;
		case 20:
			SearchHistoryPage = (StackPanel)target;
			break;
		case 21:
			SearchHistoryList = (ItemsControl)target;
			break;
		case 22:
			((Border)target).MouseLeftButtonUp += OnClearHistoryClick;
			break;
		case 23:
			SearchItemList = (ListView)target;
			break;
		case 24:
			EmptyGrid = (StackPanel)target;
			break;
		case 25:
			EmptyView = (EmptyView)target;
			break;
		case 26:
			NoTaskGridTitleTextBox = (TextBlock)target;
			break;
		case 27:
			NoTaskGridTitleTextBoxOnSearch = (TextBlock)target;
			break;
		case 28:
			NoResultText = (Run)target;
			break;
		case 29:
			TryCloudSearchText = (Run)target;
			TryCloudSearchText.PreviewMouseLeftButtonUp += OnTryCloudSearchClick;
			break;
		case 30:
			ChangeKeyWordsText = (TextBlock)target;
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
		case 2:
			((TextBlock)target).MouseLeftButtonUp += OnLoadMoreClick;
			break;
		case 3:
			((Border)target).MouseEnter += OnSearchMoreMouseEnter;
			((Border)target).MouseLeave += OnSearchMoreMouseLeave;
			break;
		case 4:
			((Run)target).PreviewMouseLeftButtonUp += OnTryCloudSearchClick;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += SearchHistoryClick;
			((Border)target).MouseEnter += OnItemHover;
			((Border)target).MouseLeave += OnItemMouseLeave;
			break;
		}
	}
}
