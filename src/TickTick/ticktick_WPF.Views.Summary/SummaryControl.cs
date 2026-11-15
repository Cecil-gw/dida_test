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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Misc.ColorSelector;
using ticktick_WPF.Views.Print;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Summary;

public class SummaryControl : System.Windows.Controls.UserControl, IComponentConnector, IStyleConnector
{
	private SummaryFilterViewModel _viewModel;

	private bool _isEditingTime;

	private string _projectIds;

	private DateTime _oldStart;

	private DateTime _oldEnd;

	private string _oldProjectIds;

	private AssigneeEditDialog _assignEditDialog;

	private SummaryTemplateViewModel _editingTemplate;

	private bool _titleBorderMouseDown;

	private bool _inOneColumn;

	internal Grid Container;

	internal Border TitleBorder;

	internal StackPanel TitlePanel;

	internal Border MenuPathGrid;

	internal Path FoldImage;

	internal Border TemplateBorder;

	internal TextBlock TemplateName;

	internal Border ArrowGrid;

	internal HoverIconButton CloseButton;

	internal EscPopup ChooseTemplatePopup;

	internal ItemsControl TemplatesItems;

	internal EscPopup TemplateOptionPopup;

	internal PopupPlacementBorder DateFilterText;

	internal EscPopup DateSelectPopup;

	internal PopupPlacementBorder ProjectOrGroupFilterText;

	internal EscPopup ProjectOrGroupFilterPopup;

	internal PopupPlacementBorder TagFilterText;

	internal EscPopup TagFilterPopup;

	internal PopupPlacementBorder StatusFilter;

	internal EscPopup StatusFilterPopup;

	internal PopupPlacementBorder PriorityFilter;

	internal EscPopup PriorityFilterPopup;

	internal PopupPlacementBorder AssignFilter;

	internal EscPopup AssignFilterPopup;

	internal PopupPlacementBorder MoreFilter;

	internal EscPopup MoreFilterPopup;

	internal ItemsControl MoreConditionPopup;

	internal Grid ClearFilterGrid;

	internal SummaryDisplayFilterControl DisplayFilter;

	internal EditorMenu TextEditorMenu;

	internal Grid SpliteLine;

	internal Grid ContentGrid;

	internal ScrollViewer ContentScrollViewer;

	internal Grid MdGrid;

	internal MarkDownEditor SummaryContent;

	internal System.Windows.Controls.Button CopyButton;

	internal System.Windows.Controls.Button ExportButton;

	internal System.Windows.Controls.Button InsertButton;

	internal Popup CopyPopup;

	internal Popup ExportPopup;

	private bool _contentLoaded;

	public event EventHandler<string> InsertSummary;

	public SummaryControl()
	{
		InitializeComponent();
		SummaryContent.DisableLinkEditorMenu = true;
		SummaryContent.EscapePlainInlineSyntaxOnPersistence = true;
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		LoadSummaryData();
		LocalSettings settings = LocalSettings.Settings;
		settings.OnPreferenceChanged = (Action)Delegate.Combine(settings.OnPreferenceChanged, new Action(NotifySummaryChanged));
	}

	private void NotifySummaryChanged()
	{
		ThreadUtil.DetachedRunOnUiThread(async delegate
		{
			LoadSummaryData();
		});
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		LocalSettings settings = LocalSettings.Settings;
		settings.OnPreferenceChanged = (Action)Delegate.Remove(settings.OnPreferenceChanged, new Action(NotifySummaryChanged));
		SummaryContent.CaretVerticalOffsetChanged -= OnContentVerticalOffsetChanged;
		SummaryContent.EditBox.TextArea.SelectionChanged -= OnSummaryContentSelectionChanged;
		SummaryContent.EditBox.TextArea.Caret.PositionChanged -= OnSummaryContentCaretPositionChanged;
	}

	private static SummaryTemplate GetInitTemplate()
	{
		List<SummaryDisplayItem> displayItems = (from it in SummaryFilterViewModel.InitDefaultDisplayItems()
			select new SummaryDisplayItem
			{
				key = it.Key,
				enabled = it.Enabled,
				sortOrder = it.SortOrder,
				style = it.Style
			}).ToList();
		NormalFilterViewModel normalFilterViewModel = new NormalFilterViewModel
		{
			Projects = new List<string> { "ProjectAll2e4c103c57ef480997943206" },
			Groups = new List<string>(),
			DueDates = new List<string> { "thisweek" },
			CompletedTimes = new List<string> { "thisweek" },
			Version = 6
		};
		SummaryTemplate summaryTemplate = new SummaryTemplate
		{
			id = "defaultId",
			name = Utils.GetString("Summary"),
			sortType = SummarySortType.progress.ToString(),
			displayItems = displayItems,
			rule = normalFilterViewModel.ToRule(isSave: false)
		};
		LocalSettings.Settings.ExtraSettings.DefaultSummaryTemplate = JsonConvert.SerializeObject(summaryTemplate);
		LocalSettings.Settings.Save();
		return summaryTemplate;
	}

	private static SummaryTemplate GetDefaultTemplate()
	{
		if (!string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings.DefaultSummaryTemplate))
		{
			try
			{
				return JsonConvert.DeserializeObject<SummaryTemplate>(LocalSettings.Settings.ExtraSettings.DefaultSummaryTemplate);
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
				return GetInitTemplate();
			}
		}
		return GetInitTemplate();
	}

	private void LoadSummaryData()
	{
		_viewModel = new SummaryFilterViewModel
		{
			DateFilter = DateFilter.ThisWeek
		};
		SummaryTemplates summaryTemplates = LocalSettings.Settings.UserPreference.summaryTemplates;
		List<SummaryTemplate> list = new List<SummaryTemplate>();
		if (summaryTemplates?.templates != null && summaryTemplates.templates.Count > 0)
		{
			list = new List<SummaryTemplate>(LocalSettings.Settings.UserPreference.summaryTemplates.templates.Select((SummaryTemplate it) => new SummaryTemplate
			{
				id = it.id,
				name = it.name,
				sortOrder = it.sortOrder,
				sortType = it.sortType,
				displayItems = it.displayItems,
				rule = it.rule
			}));
		}
		List<SummaryTemplate> list2 = (from item in list?.Where((SummaryTemplate it) => it.id != null)
			group item by item.id into @group
			select @group.First()).ToList() ?? new List<SummaryTemplate>();
		if (list2.Count == 0 || list2.All((SummaryTemplate it) => it.id != "defaultId"))
		{
			list2.Insert(0, GetDefaultTemplate());
		}
		_viewModel.ShowTemplate = list2.Count > 1;
		string selectedId = LocalSettings.Settings.ExtraSettings.SelectedSummaryTemplateId;
		if (string.IsNullOrEmpty(selectedId) || list.Find((SummaryTemplate it) => it.id == selectedId) == null)
		{
			selectedId = "defaultId";
		}
		_viewModel.SelectedTemplateId = selectedId;
		_viewModel.SummaryTemplates = list2;
		InitEvent();
		SummaryContent.EditBox.Margin = new Thickness(15.0);
		SummaryContent.SetMarkRegexText(unable: true, inDetail: false);
		LoadData();
		base.Loaded -= OnLoaded;
		base.DataContext = _viewModel;
		SetColumnMode(_inOneColumn);
	}

	private void InitEvent()
	{
		TextEditorMenu.EditorAction -= OnEditorAction;
		TextEditorMenu.EditorAction += OnEditorAction;
		TextEditorMenu.HighlightColorRequested -= OnHighlightColorRequested;
		TextEditorMenu.HighlightColorRequested += OnHighlightColorRequested;
		TextEditorMenu.GetCurrentStyle = SummaryContent.GetCurrentParagraphStyle;
		SummaryContent.RegisterCaretChanged();
		SummaryContent.CaretVerticalOffsetChanged -= OnContentVerticalOffsetChanged;
		SummaryContent.CaretVerticalOffsetChanged += OnContentVerticalOffsetChanged;
		SummaryContent.EditBox.TextArea.SelectionChanged -= OnSummaryContentSelectionChanged;
		SummaryContent.EditBox.TextArea.SelectionChanged += OnSummaryContentSelectionChanged;
		SummaryContent.EditBox.TextArea.Caret.PositionChanged -= OnSummaryContentCaretPositionChanged;
		SummaryContent.EditBox.TextArea.Caret.PositionChanged += OnSummaryContentCaretPositionChanged;
		SummaryContent.StyleMenu.EditorStyleAction -= OnEditorAction;
		SummaryContent.StyleMenu.EditorStyleAction += OnEditorAction;
		SummaryContent.EditorMenu.EditorAction -= OnEditorAction;
		SummaryContent.EditorMenu.EditorAction += OnEditorAction;
		SummaryContent.EditorMenu.HighlightColorRequested -= OnHighlightColorRequested;
		SummaryContent.EditorMenu.HighlightColorRequested += OnHighlightColorRequested;
		RefreshBottomEditorMenuContext();
	}

	private void OnSummaryContentSelectionChanged(object sender, EventArgs e)
	{
		RefreshBottomEditorMenuContext();
	}

	private void OnSummaryContentCaretPositionChanged(object sender, EventArgs e)
	{
		RefreshBottomEditorMenuContext();
	}

	private void RefreshBottomEditorMenuContext()
	{
		string currentParagraphStyle = SummaryContent.GetCurrentParagraphStyle();
		TextEditorMenu.ApplyEditorState(currentParagraphStyle, SummaryContent.GetCurrentEditorActions(), SummaryContent.GetCurrentHighlightContextColor());
	}

	private void OnContentVerticalOffsetChanged(object sender, double offset)
	{
		double num = ContentScrollViewer.VerticalOffset + ContentScrollViewer.ActualHeight;
		double num2 = offset + 15.0;
		if (num2 > num)
		{
			ContentScrollViewer.ScrollToVerticalOffset(num2 - ContentScrollViewer.ActualHeight);
		}
		double verticalOffset = ContentScrollViewer.VerticalOffset;
		if (offset - 21.0 < verticalOffset)
		{
			ContentScrollViewer.ScrollToVerticalOffset(offset - 10.0);
		}
		if (offset <= 24.0)
		{
			ContentScrollViewer.ScrollToVerticalOffset(0.0);
		}
	}

	private async void OnEditorAction(object sender, string tag)
	{
		SetContentStyle(tag);
		await FocusSummaryEditor();
	}

	private async void OnHighlightColorRequested(object sender, ColorExtra color)
	{
		if (color != null)
		{
			SummaryContent.HighlightEx(color.Background, color.Color);
			await FocusSummaryEditor();
		}
	}

	private async Task FocusSummaryEditor()
	{
		await Task.Delay(100);
		SummaryContent.FocusEditBox();
		RefreshBottomEditorMenuContext();
	}

	private void SetContentStyle(string tag)
	{
		string[] array = tag.Split('_');
		string text = string.Empty;
		if (array.Length > 1)
		{
			tag = array[0];
			text = array[1];
		}
		if (tag == null)
		{
			return;
		}
		switch (tag.Length)
		{
		case 4:
			switch (tag[0])
			{
			case 'T':
				if (tag == "Text")
				{
					SummaryContent.RemoveStartText();
				}
				break;
			case 'B':
				if (tag == "Bold")
				{
					SummaryContent.Bold();
				}
				break;
			case 'L':
				if (tag == "Link")
				{
					SummaryContent.ShowInsertLinkForCurrentContext();
				}
				break;
			case 'C':
				if (tag == "Code")
				{
					SummaryContent.Code();
				}
				break;
			}
			break;
		case 8:
			switch (tag[7])
			{
			case '1':
				if (tag == "Heading1")
				{
					SummaryContent.InsertHeader(1);
				}
				break;
			case '2':
				if (tag == "Heading2")
				{
					SummaryContent.InsertHeader(2);
				}
				break;
			case '3':
				if (tag == "Heading3")
				{
					SummaryContent.InsertHeader(3);
				}
				break;
			case 'e':
				if (tag == "DateTime")
				{
					SummaryContent.InsertTextAtCurrentCaretAndFocus(text);
				}
				break;
			}
			break;
		case 9:
			switch (tag[0])
			{
			case 'U':
				if (tag == "UnderLine")
				{
					SummaryContent.UnderLine();
				}
				break;
			case 'H':
				if (tag == "HighLight")
				{
					SummaryContent.Highlight();
				}
				break;
			case 'C':
				if (tag == "CheckItem")
				{
					SummaryContent.InsertCheckItem();
				}
				break;
			case 'S':
				if (tag == "SplitLine")
				{
					SummaryContent.InsertLine();
				}
				break;
			}
			break;
		case 6:
			if (tag == "Italic")
			{
				SummaryContent.Italic();
			}
			break;
		case 13:
			if (tag == "StrikeThrough")
			{
				SummaryContent.StrokeLine();
			}
			break;
		case 5:
			if (tag == "Quote")
			{
				SummaryContent.Quote();
			}
			break;
		case 10:
			if (tag == "BulletList")
			{
				SummaryContent.InsertUnOrderList();
			}
			break;
		case 12:
			if (tag == "NumberedList")
			{
				SummaryContent.InsertNumberedList();
			}
			break;
		case 7:
		case 11:
			break;
		}
	}

	private async void SetSummary(object sender = null, EventArgs eventArgs = null)
	{
		_viewModel.NotifyProItems();
		_viewModel.SetAssignDisplayText(_viewModel.Assignees);
		ClearFilterGrid.Visibility = ((!_viewModel.ShowClearSummaryOption()) ? Visibility.Collapsed : Visibility.Visible);
		if (_viewModel.StartDate.HasValue && _viewModel.EndDate.HasValue && (_oldStart != _viewModel.StartDate.Value || _oldEnd != _viewModel.EndDate.Value || _projectIds != _oldProjectIds))
		{
			List<TaskModel> list = ((!string.IsNullOrEmpty(_projectIds)) ? (await Communicator.GetClosedTasks(_projectIds, _viewModel.StartDate.Value, _viewModel.EndDate.Value, 1000)) : (await Communicator.GetClosedTasks("all", _viewModel.StartDate.Value, _viewModel.EndDate.Value, 1000)));
			List<TaskModel> list2 = list;
			if (list2 != null && list2.Count > 0)
			{
				await TaskService.MergeTasks(list2);
			}
			_oldStart = _viewModel.StartDate.Value;
			_oldEnd = _viewModel.EndDate.Value;
			_oldProjectIds = _projectIds;
		}
		SetSummaryContent(await TaskDao.GetSummaryTask(_viewModel.ToFilterModel()), await LoadHabitData(), await LoadCalendarData());
	}

	private async Task<List<SummaryTaskViewModel>> LoadCalendarData()
	{
		if (_viewModel.WithCalendar && _viewModel.StartDate.HasValue && _viewModel.EndDate.HasValue)
		{
			ProjectExtra projectFilter = BuildCalendarProjectFilter();
			bool num = projectFilter.IsAll || (projectFilter.SubscribeCalendars?.Contains("#allSubscribe") ?? false);
			List<string> subscribes = (num ? new List<string>() : projectFilter.SubscribeCalendars.ToList());
			List<string> accounts = (num ? new List<string>() : projectFilter.BindAccounts.ToList());
			List<CalendarDisplayModel> calendarEvents = await CalendarDisplayService.GetCalendarDisplayModelsBetweenSpan(_viewModel.StartDate.Value, _viewModel.EndDate.Value.AddDays(1.0), subscribes, accounts);
			calendarEvents.AddRange(await CalendarDisplayService.GetRepeatDisplayModels(_viewModel.StartDate.GetValueOrDefault(_viewModel.StartDate.Value), _viewModel.EndDate.GetValueOrDefault(_viewModel.EndDate.Value).AddDays(1.0), allSelected: false, projectFilter));
			Dictionary<string, string> calendarId2Names = (await BindCalendarAccountDao.GetBindCalendars()).ToDictionaryEx((BindCalendarModel it) => it.Id, (BindCalendarModel it) => it.Name);
			List<SummaryTaskViewModel> list = calendarEvents.Select((CalendarDisplayModel h) => SummaryTaskViewModel.BuildCalendar(h, calendarId2Names, _viewModel)).ToList();
			if (_viewModel.SelectedStatus != null && _viewModel.SelectedStatus.Count > 0 && !_viewModel.SelectedStatus.Contains("all"))
			{
				List<Func<SummaryTaskViewModel, bool>> conditionList = new List<Func<SummaryTaskViewModel, bool>>();
				foreach (string item in _viewModel.SelectedStatus)
				{
					if (!(item == "completed"))
					{
						if (item == "uncompleted")
						{
							conditionList.Add((SummaryTaskViewModel it) => it.Status == 0);
						}
					}
					else
					{
						conditionList.Add((SummaryTaskViewModel it) => it.Status == 2);
					}
				}
				list = list.Where((SummaryTaskViewModel it) => conditionList.Any((Func<SummaryTaskViewModel, bool> condition) => condition(it))).ToList();
			}
			return list;
		}
		return new List<SummaryTaskViewModel>();
	}

	private ProjectExtra BuildCalendarProjectFilter()
	{
		ProjectExtra projectExtra = new ProjectExtra
		{
			BindAccounts = _viewModel.SelectedCalendarAccountIds.ToList(),
			SubscribeCalendars = _viewModel.SelectedCalendarIds.ToList()
		};
		if (_viewModel.SelectedProjectIds.Contains("Calendar5959a2259161d16d23a4f272") && !projectExtra.BindAccounts.Any() && !projectExtra.SubscribeCalendars.Any())
		{
			projectExtra.SubscribeCalendars = new List<string> { "#allSubscribe" };
		}
		return projectExtra;
	}

	private void AssembleGroupDate(SummaryTaskViewModel taskModel)
	{
		if (_viewModel.SortBy == SummarySortType.completedTime)
		{
			taskModel.GroupDate = taskModel.CompletedDate ?? taskModel.StartDate;
		}
		else if (_viewModel.SortBy == SummarySortType.dueDate)
		{
			taskModel.GroupDate = taskModel.StartDate ?? taskModel.CompletedDate;
		}
	}

	private async Task<List<SummaryTaskViewModel>> LoadHabitData()
	{
		List<SummaryTaskViewModel> habitModels = new List<SummaryTaskViewModel>();
		if (_viewModel.WithHabit && _viewModel.StartDate.HasValue && _viewModel.EndDate.HasValue)
		{
			List<HabitModel> habits = (await HabitDao.GetAllHabits()).Where((HabitModel it) => it.Status == 0).ToList();
			Dictionary<string, HabitModel> id2Habit = habits.ToDictionaryEx((HabitModel it) => it.Id, (HabitModel it) => it);
			Dictionary<string, string> sectionId2Name = (await HabitSectionDao.GetAllHabitSections()).ToDictionaryEx((HabitSectionModel it) => it.Id, (HabitSectionModel it) => it.Name);
			List<HabitCheckInModel> checkIns = await HabitCheckInDao.GetCheckInsByIdsInSpan(habits.Select((HabitModel it) => it.Id).ToList(), _viewModel.StartDate.Value, _viewModel.EndDate.Value);
			int checkStamp = int.Parse(_viewModel.StartDate.Value.ToString("yyyyMMdd"));
			Dictionary<string, HabitRecordModel> habit2Note = new Dictionary<string, HabitRecordModel>();
			if (_viewModel.ShowDetail)
			{
				habit2Note = (await HabitRecordDao.GetHabitRecords(checkStamp)).ToDictionaryEx((HabitRecordModel it) => it.HabitId + "_" + it.Stamp, (HabitRecordModel it) => it);
			}
			if (checkIns != null && checkIns.Count > 0)
			{
				foreach (HabitCheckInModel item in checkIns)
				{
					if (id2Habit.TryGetValue(item.HabitId, out var value) && (item.Value > 0.0 || item.CheckStatus == 1))
					{
						SummaryTaskViewModel summaryTaskViewModel = SummaryTaskViewModel.BuildHabit(value, item, habit2Note, sectionId2Name, _viewModel);
						AssembleGroupDate(summaryTaskViewModel);
						habitModels.Add(summaryTaskViewModel);
					}
				}
			}
			if (_viewModel.WithHabit && _viewModel.StartDate.HasValue && _viewModel.EndDate.HasValue)
			{
				DateTime today = DateTime.Today;
				DateTime? startDate = _viewModel.StartDate;
				if (today >= startDate)
				{
					today = DateTime.Today;
					startDate = _viewModel.EndDate;
					if (today <= startDate)
					{
						List<HabitModel> todayHabits = await HabitDao.GetNeedCheckHabits();
						List<HabitCheckInModel> monthCheckIns = await HabitCheckInDao.GetCheckInsInSpan(DateTime.Today.AddDays(-30.0), DateTime.Today.AddDays(1.0));
						foreach (HabitModel habit in todayHabits)
						{
							string todayStamp = DateTime.Today.ToString("yyyyMMdd");
							List<HabitCheckInModel> habitCheckIns = monthCheckIns.Where((HabitCheckInModel c) => c.HabitId == habit.Id).ToList();
							if (await HabitUtils.IsHabitValidInToday(habit, habitCheckIns) && !habitCheckIns.Any((HabitCheckInModel c) => c.HabitId == habit.Id && c.CheckinStamp == todayStamp))
							{
								SummaryTaskViewModel summaryTaskViewModel2 = SummaryTaskViewModel.BuildHabit(habit, sectionId2Name, _viewModel);
								AssembleGroupDate(summaryTaskViewModel2);
								habitModels.Add(summaryTaskViewModel2);
							}
						}
					}
				}
			}
		}
		if (_viewModel.SelectedStatus != null && _viewModel.SelectedStatus.Count > 0 && !_viewModel.SelectedStatus.Contains("all"))
		{
			List<Func<SummaryTaskViewModel, bool>> conditionList = new List<Func<SummaryTaskViewModel, bool>>();
			using (List<string>.Enumerator enumerator3 = _viewModel.SelectedStatus.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					switch (enumerator3.Current)
					{
					case "inProgress":
						conditionList.Add((SummaryTaskViewModel it) => it.Status == 0 && !string.IsNullOrEmpty(it.ProgressText));
						break;
					case "completed":
						conditionList.Add((SummaryTaskViewModel it) => it.Status == 2);
						break;
					case "uncompleted":
						conditionList.Add((SummaryTaskViewModel it) => it.Status == 0 && string.IsNullOrEmpty(it.ProgressText));
						break;
					case "wontDo":
						conditionList.Add((SummaryTaskViewModel it) => it.Status == -1);
						break;
					}
				}
			}
			habitModels = habitModels.Where((SummaryTaskViewModel it) => conditionList.Any((Func<SummaryTaskViewModel, bool> condition) => condition(it))).ToList();
		}
		return habitModels;
	}

	private async void SetSummaryContent(List<TaskBaseViewModel> tasks, IReadOnlyCollection<SummaryTaskViewModel> habits, IReadOnlyCollection<SummaryTaskViewModel> calendarEvents)
	{
		Dictionary<string, Node<TaskBaseViewModel>> uncompletedNodeDict = tasks.Where((TaskBaseViewModel t) => t.Status == 0).ToDictionaryEx((Func<TaskBaseViewModel, string>)((TaskBaseViewModel task) => task.Id), (Func<TaskBaseViewModel, Node<TaskBaseViewModel>>)((TaskBaseViewModel task) => new TaskNode(task)));
		Dictionary<string, Node<TaskBaseViewModel>> closedNodeDict = tasks.Where((TaskBaseViewModel t) => t.Status != 0).ToDictionaryEx((Func<TaskBaseViewModel, string>)((TaskBaseViewModel task) => task.Id), (Func<TaskBaseViewModel, Node<TaskBaseViewModel>>)((TaskBaseViewModel task) => new TaskNode(task)));
		if (_viewModel.ShowColumn)
		{
			Dictionary<string, ColumnModel> dictionary = (from cm in await ColumnDao.GetAllColumnsAsync()
				group cm by cm.projectId into @group
				where @group.Count() > 1
				select @group).SelectMany((IGrouping<string, ColumnModel> group) => group.ToList()).ToList().ToDictionaryEx((ColumnModel it) => it.id, (ColumnModel c) => c);
			foreach (TaskBaseViewModel task in tasks)
			{
				dictionary.TryGetValue(task.ColumnId ?? string.Empty, out var value);
				task.ColumnName = ((value != null) ? value.name : string.Empty);
			}
		}
		TaskNodeUtils.BuildNodeTree(uncompletedNodeDict);
		TaskNodeUtils.BuildNodeTree(closedNodeDict);
		IEnumerable<Node<TaskBaseViewModel>> enumerable = uncompletedNodeDict.Values.Where((Node<TaskBaseViewModel> n) => !n.HasParent);
		IEnumerable<Node<TaskBaseViewModel>> closedParentNode = closedNodeDict.Values.Where((Node<TaskBaseViewModel> n) => !n.HasParent);
		if (!_viewModel.StartDate.HasValue || !_viewModel.EndDate.HasValue)
		{
			return;
		}
		string content = "# " + GetTitle();
		List<SummaryTaskViewModel> models = new List<SummaryTaskViewModel>();
		foreach (Node<TaskBaseViewModel> item in enumerable)
		{
			SummaryTaskViewModel summaryTaskViewModel = await SummaryTaskViewModel.Build(item, _viewModel);
			if (_viewModel.SortBy == SummarySortType.completedTime)
			{
				summaryTaskViewModel.GroupDate = ((summaryTaskViewModel.Status != 0) ? summaryTaskViewModel.CompletedDate : summaryTaskViewModel.StartDate);
				if (Utils.IsEmptyDate(summaryTaskViewModel.GroupDate))
				{
					continue;
				}
			}
			else if (_viewModel.SortBy == SummarySortType.dueDate)
			{
				summaryTaskViewModel.GroupDate = summaryTaskViewModel.StartDate ?? summaryTaskViewModel.CompletedDate;
				if (Utils.IsEmptyDate(summaryTaskViewModel.GroupDate))
				{
					continue;
				}
			}
			models.Add(summaryTaskViewModel);
		}
		foreach (Node<TaskBaseViewModel> item2 in closedParentNode)
		{
			SummaryTaskViewModel summaryTaskViewModel2 = await SummaryTaskViewModel.Build(item2, _viewModel);
			if (_viewModel.SortBy == SummarySortType.completedTime)
			{
				summaryTaskViewModel2.GroupDate = summaryTaskViewModel2.CompletedDate;
				if (Utils.IsEmptyDate(summaryTaskViewModel2.GroupDate))
				{
					continue;
				}
			}
			else if (_viewModel.SortBy == SummarySortType.dueDate)
			{
				summaryTaskViewModel2.GroupDate = summaryTaskViewModel2.StartDate ?? summaryTaskViewModel2.CompletedDate;
				if (Utils.IsEmptyDate(summaryTaskViewModel2.GroupDate))
				{
					continue;
				}
			}
			models.Add(summaryTaskViewModel2);
		}
		if (habits != null && habits.Count > 0)
		{
			models.AddRange(habits);
		}
		if (calendarEvents != null && calendarEvents.Count > 0)
		{
			models.AddRange(calendarEvents);
		}
		if (models.Count == 0)
		{
			content = content + "\r\n" + Utils.GetString("NoMatchTask");
		}
		else
		{
			string text = content;
			content = text + await GetTaskSummary(models);
		}
		SummaryContent.Text = MarkdownUtils.RemoveFoldedMetadataComments(content);
	}

	private string GetTitle(bool withSpace = true)
	{
		if (_viewModel.StartDate.HasValue && _viewModel.EndDate.HasValue)
		{
			string text = DateUtils.FormatMonthDay(_viewModel.StartDate.Value, DateFormatLen.Long);
			if (_viewModel.StartDate.Value != _viewModel.EndDate.Value)
			{
				text = text + (withSpace ? " - " : "-") + DateUtils.FormatMonthDay(_viewModel.EndDate.Value, DateFormatLen.Long);
			}
			return text;
		}
		return "";
	}

	private async Task<string> GetTaskSummary(List<SummaryTaskViewModel> models)
	{
		string text = "";
		string groupName = Utils.GetString("statistics_habit");
		switch (_viewModel.SortBy)
		{
		case SummarySortType.progress:
			text = GetTaskContent(models, text);
			break;
		case SummarySortType.project:
		{
			models.Sort(delegate(SummaryTaskViewModel a, SummaryTaskViewModel b)
			{
				if (a.Project.Isinbox && !b.Project.Isinbox)
				{
					return -1;
				}
				return (!a.Project.Isinbox && b.Project.Isinbox) ? 1 : a.Project.sortOrder.CompareTo(b.Project.sortOrder);
			});
			IEnumerable<IGrouping<string, SummaryTaskViewModel>> groups5 = from m in models
				group m by m.Project.id;
			text = await GetGroupTaskContent(groups5, text, isProject: true);
			break;
		}
		case SummarySortType.completedTime:
		case SummarySortType.dueDate:
		{
			foreach (SummaryTaskViewModel model in models)
			{
				AssembleGroupDate(model);
				model.GroupName = model.GroupDate?.ToString((model.GroupDate.Value.Year == DateTime.Today.Year) ? "m" : "d", App.Ci);
			}
			models.Sort((SummaryTaskViewModel a, SummaryTaskViewModel b) => (a.GroupDate.HasValue && b.GroupDate.HasValue) ? a.GroupDate.Value.CompareTo(b.GroupDate.Value) : 0);
			IEnumerable<IGrouping<string, SummaryTaskViewModel>> groups4 = from m in models
				group m by m.GroupName;
			text = await GetGroupTaskContent(groups4, text);
			break;
		}
		case SummarySortType.priority:
		{
			foreach (SummaryTaskViewModel model2 in models)
			{
				if (model2.IsHabitModel())
				{
					model2.GroupName = groupName;
					continue;
				}
				switch (model2.Priority)
				{
				case 0:
					model2.GroupName = Utils.GetString("PriorityNull");
					break;
				case 1:
					model2.GroupName = Utils.GetString("PriorityLow");
					break;
				case 3:
					model2.GroupName = Utils.GetString("PriorityMedium");
					break;
				case 5:
					model2.GroupName = Utils.GetString("PriorityHigh");
					break;
				}
			}
			models.Sort((SummaryTaskViewModel a, SummaryTaskViewModel b) => b.Priority.CompareTo(a.Priority));
			IEnumerable<IGrouping<string, SummaryTaskViewModel>> groups2 = from m in models
				group m by m.GroupName;
			text = await GetGroupTaskContent(groups2, text);
			break;
		}
		case SummarySortType.tag:
		{
			string text2 = Utils.GetString("NoTags");
			Dictionary<string, long> tagSortDict = TagDataHelper.GetTagSortDict();
			foreach (SummaryTaskViewModel model3 in models)
			{
				if (model3.Tags != null)
				{
					model3.PrimaryTag = CacheManager.GetTagByName(TagDataHelper.GetPrimaryTag(tagSortDict, model3.Tags.ToList(), null));
				}
				if (model3.IsHabitModel())
				{
					model3.GroupName = groupName;
				}
				else
				{
					model3.GroupName = ((model3.PrimaryTag != null) ? (model3.PrimaryTag.label ?? model3.PrimaryTag.name) : text2);
				}
			}
			models.Sort(delegate(SummaryTaskViewModel a, SummaryTaskViewModel b)
			{
				if (a.IsHabitModel() && !b.IsHabitModel())
				{
					return 1;
				}
				if (!a.IsHabitModel() && b.IsHabitModel())
				{
					return -1;
				}
				if (a.PrimaryTag == null && b.PrimaryTag != null)
				{
					return 1;
				}
				if (a.PrimaryTag != null && b.PrimaryTag == null)
				{
					return -1;
				}
				return (a.PrimaryTag != null && b.PrimaryTag != null) ? a.PrimaryTag.sortOrder.CompareTo(b.PrimaryTag.sortOrder) : 0;
			});
			IEnumerable<IGrouping<string, SummaryTaskViewModel>> groups3 = from m in models
				group m by m.GroupName;
			text = await GetGroupTaskContent(groups3, text);
			break;
		}
		case SummarySortType.assignee:
		{
			string me = LocalSettings.Settings.LoginUserId;
			models.Sort(delegate(SummaryTaskViewModel a, SummaryTaskViewModel b)
			{
				if (a.IsHabitModel() && !b.IsHabitModel())
				{
					return 1;
				}
				if (!a.IsHabitModel() && b.IsHabitModel())
				{
					return -1;
				}
				if (a.Assignee != me && b.Assignee == me)
				{
					return 1;
				}
				if (a.Assignee == me && b.Assignee != me)
				{
					return -1;
				}
				if (a.Assignee != b.Assignee)
				{
					if (string.IsNullOrEmpty(a.Assignee) || a.Assignee == "-1")
					{
						return 1;
					}
					if (string.IsNullOrEmpty(b.Assignee) || b.Assignee == "-1")
					{
						return -1;
					}
				}
				return string.Compare(a.Assignee, b.Assignee, StringComparison.Ordinal);
			});
			IEnumerable<IGrouping<string, SummaryTaskViewModel>> groups = models.GroupBy(delegate(SummaryTaskViewModel m)
			{
				if (m.IsHabitModel())
				{
					return "habit";
				}
				return (string.IsNullOrEmpty(m.Assignee) || m.Assignee == "-1") ? "noAssignee" : m.Assignee;
			});
			text = await GetGroupTaskContent(groups, text, isProject: false, isAssign: true);
			break;
		}
		}
		return text;
	}

	private async Task<string> GetGroupTaskContent(IEnumerable<IGrouping<string, SummaryTaskViewModel>> groups, string content, bool isProject = false, bool isAssign = false)
	{
		foreach (IGrouping<string, SummaryTaskViewModel> group in groups)
		{
			if (isProject)
			{
				ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == group.Key);
				content = ((projectModel == null) ? ((!(group.Key == "habit")) ? ((!(group.Key == "calendar")) ? (content + "\r\n## " + group.Key) : (content + "\r\n## " + Utils.GetString("Calendar"))) : (content + "\r\n## " + Utils.GetString("statistics_habit"))) : (content + "\r\n## " + projectModel.name));
			}
			else if (isAssign)
			{
				long result;
				if (group.Key == LocalSettings.Settings.LoginUserId)
				{
					content = content + "\r\n## " + Utils.GetString("Me");
				}
				else if (group.Key == "habit")
				{
					content = content + "\r\n## " + Utils.GetString("statistics_habit");
				}
				else if (group.Key != "noAssignee" && long.TryParse(group.Key, out result))
				{
					AvatarViewModel avatarViewModel = (await AvatarHelper.GetProjectAvatars(group.ToList().FirstOrDefault()?.Project.id, showMyName: false, fetchRemote: true))?.FirstOrDefault((AvatarViewModel u) => u.UserId == group.Key);
					content = ((avatarViewModel == null) ? (content + "\r\n## " + group.Key) : (content + "\r\n## " + avatarViewModel.Name));
				}
				else
				{
					content = content + "\r\n## " + Utils.GetString("NotAssigned");
				}
			}
			else
			{
				content = content + "\r\n## " + group.Key;
			}
			content = GetTaskContent(group.ToList(), content, withSpace: true);
		}
		return content;
	}

	private string GetClosedTaskContent(IEnumerable<SummaryTaskViewModel> unorderTasks, string title, bool withSpace)
	{
		string text = string.Empty;
		IOrderedEnumerable<SummaryTaskViewModel> orderedEnumerable = from t in unorderTasks
			orderby t.CompletedDate descending, t.Priority, t.SortOrder, t.Project.sortOrder
			select t;
		if (orderedEnumerable.Any())
		{
			text = text + "\r\n" + (withSpace ? "###    " : "## ") + title;
		}
		foreach (SummaryTaskViewModel item in orderedEnumerable)
		{
			text += item.GetContent(withSpace);
			List<SummaryTaskViewModel> children = item.Children;
			if (children != null && children.Count > 0)
			{
				text = GetChildrenContent(item.Children, text, withSpace);
			}
		}
		if (orderedEnumerable.Any())
		{
			text += "\r\n";
		}
		return text;
	}

	private string GetTaskContent(List<SummaryTaskViewModel> models, string content, bool withSpace = false)
	{
		List<SummaryTaskViewModel> source = models.Where((SummaryTaskViewModel m) => m.Status != 0).ToList();
		IEnumerable<SummaryTaskViewModel> unorderTasks = source.Where((SummaryTaskViewModel m) => m.Status != -1);
		IEnumerable<SummaryTaskViewModel> unorderTasks2 = source.Where((SummaryTaskViewModel m) => m.Status == -1);
		content += GetClosedTaskContent(unorderTasks, Utils.GetString("Completed"), withSpace);
		content += GetClosedTaskContent(unorderTasks2, Utils.GetString("Abandoned"), withSpace);
		List<SummaryTaskViewModel> list = models.Where((SummaryTaskViewModel m) => m.Status == 0 && m.Progress.GetValueOrDefault() > 0).ToList();
		if (list.Count > 0)
		{
			SortUnCompleteTask(list);
			list = list.OrderByDescending((SummaryTaskViewModel it) => it.Progress).ToList();
			content = content + "\r\n" + (withSpace ? "###    " : "## ") + Utils.GetString("SummaryInProgress");
			content = list.Aggregate(content, delegate(string current, SummaryTaskViewModel u)
			{
				current += u.GetContent(withSpace);
				List<SummaryTaskViewModel> children = u.Children;
				if (children != null && children.Count > 0)
				{
					current = GetChildrenContent(u.Children, current, withSpace);
				}
				return current;
			});
			content += "\r\n";
		}
		List<SummaryTaskViewModel> list2 = models.Where((SummaryTaskViewModel m) => m.Status == 0 && m.Progress.GetValueOrDefault() == 0).ToList();
		if (list2.Count > 0)
		{
			SortUnCompleteTask(list2);
			content = content + "\r\n" + (withSpace ? "###    " : "## ") + Utils.GetString("Uncompleted");
			content = list2.Aggregate(content, delegate(string current, SummaryTaskViewModel u)
			{
				current += u.GetContent(withSpace);
				List<SummaryTaskViewModel> children = u.Children;
				if (children != null && children.Count > 0)
				{
					current = GetChildrenContent(u.Children, current, withSpace);
				}
				return current;
			});
			content += "\r\n";
		}
		return content;
	}

	private string GetChildrenContent(List<SummaryTaskViewModel> children, string content, bool withSpace)
	{
		SortUnCompleteTask(children);
		foreach (SummaryTaskViewModel child in children)
		{
			content += child.GetContent(withSpace);
			List<SummaryTaskViewModel> children2 = child.Children;
			if (children2 != null && children2.Count > 0)
			{
				content = GetChildrenContent(child.Children, content, withSpace);
			}
		}
		return content;
	}

	private void SortUnCompleteTask(List<SummaryTaskViewModel> models)
	{
		models.Sort(delegate(SummaryTaskViewModel a, SummaryTaskViewModel b)
		{
			int num = CompareModelByTime(a, b);
			if (num != 0 && (_viewModel.SortBy == SummarySortType.dueDate || _viewModel.SortBy == SummarySortType.completedTime || _viewModel.SortBy == SummarySortType.progress))
			{
				return num;
			}
			if (_viewModel.SortBy == SummarySortType.priority && a.Priority != b.Priority)
			{
				return b.Priority.CompareTo(a.Priority);
			}
			int valueOrDefault = a.Progress.GetValueOrDefault();
			int valueOrDefault2 = b.Progress.GetValueOrDefault();
			if (valueOrDefault == valueOrDefault2)
			{
				if (num == 0)
				{
					if (a.Priority == b.Priority)
					{
						int num2 = CompareModelType(a, b);
						if (num2 != 0)
						{
							return num2;
						}
						if (a.Project.sortOrder != b.Project.sortOrder)
						{
							return a.Project.sortOrder.CompareTo(b.Project.sortOrder);
						}
						return a.SortOrder.CompareTo(b.SortOrder);
					}
					return b.Priority.CompareTo(a.Priority);
				}
				return num;
			}
			return valueOrDefault2.CompareTo(valueOrDefault);
		});
	}

	private int GetTypeOrdinal(SummaryTaskViewModel model)
	{
		if (model.IsTaskModel())
		{
			return 3;
		}
		if (model.IsCalendarModel())
		{
			return 2;
		}
		if (model.IsHabitModel())
		{
			return 1;
		}
		return 3;
	}

	private int CompareModelType(SummaryTaskViewModel left, SummaryTaskViewModel right)
	{
		int typeOrdinal = GetTypeOrdinal(left);
		return GetTypeOrdinal(right).CompareTo(typeOrdinal);
	}

	private int CompareModelByTime(SummaryTaskViewModel left, SummaryTaskViewModel right)
	{
		if (!left.StartDate.HasValue && right.StartDate.HasValue)
		{
			return 1;
		}
		if (left.StartDate.HasValue && !right.StartDate.HasValue)
		{
			return -1;
		}
		if (left.SourceViewModel != null && right.SourceViewModel != null)
		{
			return DateSortHelper.CompareDate(left.SourceViewModel, right.SourceViewModel, ignoreDuration: true);
		}
		return 0;
	}

	private static string GetDateSpanDesc((DateTime, DateTime) span)
	{
		return " (" + DateUtils.FormatMonthDay(span.Item1, DateFormatLen.Middle) + " - " + DateUtils.FormatMonthDay(span.Item2, DateFormatLen.Middle) + ")";
	}

	private void DateFilterClick(object sender, MouseButtonEventArgs e)
	{
		TimeFilterDialog timeFilterDialog = new TimeFilterDialog
		{
			ItemsSource = new List<ListItemData>
			{
				new ListItemData(DateFilter.Today, Utils.GetString("Today")),
				new ListItemData(DateFilter.Tomorrow, Utils.GetString("Tomorrow")),
				new ListItemData(DateFilter.Yesterday, Utils.GetString("PublicYesterday")),
				new ListItemData(DateFilter.ThisWeek, Utils.GetString("ThisWeek") + GetDateSpanDesc(SearchDateTextConverter.ConvertDateFilter2Span(DateFilter.ThisWeek))),
				new ListItemData(DateFilter.NextWeek, Utils.GetString("NextWeek") + GetDateSpanDesc(SearchDateTextConverter.ConvertDateFilter2Span(DateFilter.NextWeek))),
				new ListItemData(DateFilter.LastWeek, Utils.GetString("LastWeek") + GetDateSpanDesc(SearchDateTextConverter.ConvertDateFilter2Span(DateFilter.LastWeek))),
				new ListItemData(DateFilter.ThisMonth, Utils.GetString("ThisMonth")),
				new ListItemData(DateFilter.LastMonth, Utils.GetString("LastMonth")),
				new ListItemData(DateFilter.Custom, Utils.GetString("Custom"))
			}
		};
		if (_viewModel.DateFilter == DateFilter.Custom)
		{
			timeFilterDialog.SetStartDate(_viewModel.StartDate);
			timeFilterDialog.SetEndDate(_viewModel.EndDate);
		}
		timeFilterDialog.ItemsSource.ForEach(delegate(ListItemData item)
		{
			if ((DateFilter)item.Key == _viewModel.DateFilter)
			{
				item.Selected = true;
			}
		});
		timeFilterDialog.OnFilterSelect += delegate(object obj, DateFilterData item)
		{
			_viewModel.DateFilter = item.Type;
			if (item.StartDate.HasValue)
			{
				_viewModel.StartDate = item.StartDate.Value;
			}
			if (item.EndDate.HasValue)
			{
				_viewModel.EndDate = item.EndDate.Value;
			}
			RefreshDate();
			_viewModel.SaveTemplate();
			DateSelectPopup.IsOpen = false;
			UserActCollectUtils.AddClickEvent("summary", "filter", "date");
			UserActCollectUtils.AddClickEvent("summary", "time_range", GetDateAnalyticsLabel());
			SetSummary();
		};
		timeFilterDialog.OnEndEditDate += async delegate
		{
			_isEditingTime = true;
		};
		timeFilterDialog.OnCancel += delegate
		{
			DateSelectPopup.IsOpen = false;
		};
		DateSelectPopup.Child = timeFilterDialog;
		DateSelectPopup.IsOpen = true;
		DateSelectPopup.Closed += async delegate
		{
			if (_isEditingTime)
			{
				await Task.Delay(1);
				_isEditingTime = false;
			}
		};
	}

	private string GetDateAnalyticsLabel()
	{
		switch (_viewModel.DateFilter)
		{
		case DateFilter.Today:
			return "today";
		case DateFilter.Yesterday:
			return "yesterday";
		case DateFilter.ThisWeek:
			return "this_week";
		case DateFilter.NextWeek:
			return "next_week";
		case DateFilter.LastWeek:
			return "last_week";
		case DateFilter.Tomorrow:
			return "tomorrow";
		case DateFilter.ThisMonth:
			return "this_month";
		case DateFilter.LastMonth:
			return "last_month";
		case DateFilter.Custom:
			if (_viewModel.StartDate.HasValue && _viewModel.EndDate.HasValue)
			{
				DayOfWeek dayOfWeek = DateTime.Today.DayOfWeek;
				int num = (int)((dayOfWeek != DayOfWeek.Sunday) ? (dayOfWeek - 1) : DayOfWeek.Saturday);
				DateTime dateTime = DateTime.Today.AddDays(-num);
				DateTime value = dateTime.AddDays(4.0);
				DateTime? startDate = _viewModel.StartDate;
				DateTime dateTime2 = dateTime;
				if (startDate.HasValue && startDate.GetValueOrDefault() == dateTime2 && _viewModel.EndDate == value)
				{
					return "this_mon_to_fri_custom";
				}
				DateTime dateTime3 = DateTime.Today.AddDays(-num - 7);
				DateTime value2 = dateTime3.AddDays(4.0);
				startDate = _viewModel.StartDate;
				dateTime2 = dateTime3;
				if (startDate.HasValue && startDate.GetValueOrDefault() == dateTime2 && _viewModel.EndDate == value2)
				{
					return "last_mon_to_fri_custom";
				}
				return "other_custom";
			}
			return "other_custom";
		default:
			return "other_custom";
		}
	}

	private void RefreshDate()
	{
		_viewModel.RefreshDate();
	}

	private void ProjectOrGroupFilterClick(object sender, MouseButtonEventArgs e)
	{
		ProjectExtra data = BuildProjectExtraForPopup();
		bool flag = CacheManager.GetSubscribeCalendars().Any((CalendarSubscribeProfileModel cal) => cal.Show != "hidden") || CacheManager.GetBindCalendars().Any((BindCalendarModel cal) => cal.Show != "hidden");
		bool showAll = flag || LocalSettings.Settings.ShowHabit;
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(ProjectOrGroupFilterPopup, data, new ProjectSelectorExtra
		{
			showAll = showAll,
			showCalendars = flag,
			showListGroup = true,
			showAllProjectCategory = false,
			showHabitCategory = LocalSettings.Settings.ShowHabit,
			useCircleCheckForAll = true,
			popupWidth = 240.0
		});
		projectOrGroupPopup.Save += OnProjectSelect;
		projectOrGroupPopup.Show();
	}

	private async void OnProjectSelect(object sender, ProjectExtra data)
	{
		if (!data.IsAll)
		{
			List<string> list = data.ProjectIds.ToList();
			if (data.BindAccounts.Any() || data.SubscribeCalendars.Any())
			{
				list.Add("Calendar5959a2259161d16d23a4f272");
			}
			if (list.Count > 0)
			{
				_projectIds = list[0];
				for (int i = 0; i < list.Count; i++)
				{
					if (i != 0)
					{
						_projectIds = _projectIds + "," + list[i];
					}
				}
			}
			_viewModel.SelectedProjectIds = list;
			_viewModel.SelectedProjectGroupIds = data.GroupIds.ToList();
			_viewModel.SelectedCalendarAccountIds = data.BindAccounts.ToList();
			_viewModel.SelectedCalendarIds = data.SubscribeCalendars.ToList();
			_viewModel.SelectedProjectDisplayText = ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(data));
			UserActCollectUtils.AddClickEvent("summary", "filter", "list");
		}
		else
		{
			_projectIds = null;
			_viewModel.SelectedProjectIds = new List<string>();
			_viewModel.SelectedProjectGroupIds = new List<string>();
			_viewModel.SelectedCalendarAccountIds = new List<string>();
			_viewModel.SelectedCalendarIds = new List<string>();
			_viewModel.SelectedProjectDisplayText = Utils.GetString("All");
		}
		AnalyticsContentRange();
		_viewModel.SaveTemplate();
		SetSummary();
	}

	private ProjectExtra BuildProjectExtraForPopup()
	{
		List<string> list = _viewModel.SelectedProjectIds ?? new List<string>();
		ProjectExtra projectExtra = new ProjectExtra
		{
			ProjectIds = list.Where((string id) => id != "Calendar5959a2259161d16d23a4f272").ToList(),
			GroupIds = _viewModel.SelectedProjectGroupIds.ToList()
		};
		if (!list.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			return projectExtra;
		}
		if (_viewModel.SelectedCalendarAccountIds.Any() || _viewModel.SelectedCalendarIds.Any())
		{
			projectExtra.BindAccounts = _viewModel.SelectedCalendarAccountIds.ToList();
			projectExtra.SubscribeCalendars = _viewModel.SelectedCalendarIds.ToList();
		}
		else
		{
			projectExtra.SubscribeCalendars = new List<string> { "#allSubscribe" };
		}
		return projectExtra;
	}

	private void AnalyticsContentRange()
	{
		List<string> selectedProjectIds = _viewModel.SelectedProjectIds;
		if (selectedProjectIds != null && selectedProjectIds.Count == 0 && _viewModel.SelectedProjectGroupIds.Count == 0)
		{
			UserActCollectUtils.AddClickEvent("summary", "content_range", "all");
		}
		if (_viewModel.SelectedProjectIds != null && _viewModel.SelectedProjectIds.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			UserActCollectUtils.AddClickEvent("summary", "content_range", "calendar");
		}
		if (_viewModel.SelectedProjectIds != null && _viewModel.SelectedProjectIds.Contains("Habit2e4c103c57ef480997943206"))
		{
			UserActCollectUtils.AddClickEvent("summary", "content_range", "habit");
		}
		List<string> selectedProjectIds2 = _viewModel.SelectedProjectIds;
		if ((selectedProjectIds2 != null && selectedProjectIds2.Count((string it) => it != "Calendar5959a2259161d16d23a4f272" && it != "Habit2e4c103c57ef480997943206") > 0) || _viewModel.SelectedProjectGroupIds.Count > 0)
		{
			UserActCollectUtils.AddClickEvent("summary", "content_range", "list");
		}
	}

	private void TagFilterClick(object sender, MouseButtonEventArgs e)
	{
		TagSearchFilterControl tagSearchFilterControl = new TagSearchFilterControl(_viewModel.SelectedTags);
		tagSearchFilterControl.Cancel -= OnTagFilterCancel;
		tagSearchFilterControl.Cancel += OnTagFilterCancel;
		tagSearchFilterControl.Save -= OnTagFilterSelected;
		tagSearchFilterControl.Save += OnTagFilterSelected;
		TagFilterPopup.Child = tagSearchFilterControl;
		TagFilterPopup.IsOpen = true;
	}

	private void OnTagFilterSelected(object sender, List<string> tags)
	{
		TagFilterPopup.IsOpen = false;
		_viewModel.SelectedTags = tags;
		_viewModel.SelectedTagDisplayText = TagCardViewModel.ToNormalDisplayText(tags);
		_viewModel.SaveTemplate();
		UserActCollectUtils.AddClickEvent("summary", "filter", "tag");
		SetSummary();
	}

	private void OnTagFilterCancel(object sender, EventArgs e)
	{
		TagFilterPopup.IsOpen = false;
	}

	private void StatusFilterClick(object sender, MouseButtonEventArgs e)
	{
		TaskStatusTypeEditDialog taskStatusTypeEditDialog = new TaskStatusTypeEditDialog(_viewModel.SelectedStatus);
		taskStatusTypeEditDialog.OnCancel += delegate
		{
			StatusFilterPopup.IsOpen = false;
		};
		taskStatusTypeEditDialog.OnSave += delegate(object s, ticktick_WPF.ViewModels.FilterConditionViewModel model)
		{
			List<string> list = (from p in model.ItemsSource
				where p.Value != "all" && p.Selected
				select p.Value as string).ToList();
			_viewModel.SelectedStatus = ((list.Count == 4) ? new List<string>() : list);
			_viewModel.SelectedStatusDisplayText = TaskStatusTypeEditDialog.FormatDisplayText(_viewModel.SelectedStatus);
			_viewModel.SaveTemplate();
			UserActCollectUtils.AddClickEvent("summary", "filter", "status");
			StatusFilterPopup.IsOpen = false;
			SetSummary();
		};
		StatusFilterPopup.Child = taskStatusTypeEditDialog;
		StatusFilterPopup.IsOpen = true;
	}

	private void PriorityFilterClick(object sender, MouseButtonEventArgs e)
	{
		TaskPriorityTypeEditDialog taskPriorityTypeEditDialog = new TaskPriorityTypeEditDialog(_viewModel.SelectedPriorities);
		taskPriorityTypeEditDialog.OnCancel += delegate
		{
			PriorityFilterPopup.IsOpen = false;
		};
		taskPriorityTypeEditDialog.OnSave += delegate(object s, ticktick_WPF.ViewModels.FilterConditionViewModel model)
		{
			List<string> list = (from p in model.ItemsSource
				where p.Value != "all" && p.Selected
				select p.Value as string).ToList();
			_viewModel.SelectedPriorities = ((list.Count == 4) ? new List<string>() : list);
			_viewModel.SelectedPriorityDisplayText = TaskPriorityTypeEditDialog.FormatDisplayText(_viewModel.SelectedPriorities);
			_viewModel.SaveTemplate();
			UserActCollectUtils.AddClickEvent("summary", "filter", "priority");
			PriorityFilterPopup.IsOpen = false;
			SetSummary();
		};
		PriorityFilterPopup.Child = taskPriorityTypeEditDialog;
		PriorityFilterPopup.IsOpen = true;
	}

	private void AssignFilterClick(object sender, MouseButtonEventArgs e)
	{
		_assignEditDialog = new AssigneeEditDialog(_viewModel.Assignees ?? new List<string>(), showAll: true);
		_assignEditDialog.OnSelectedAssigneeChanged += delegate(object s, List<string> assignees)
		{
			_viewModel.Assignees = assignees;
		};
		AssignFilterPopup.Closed -= AssigneePopup_Closed;
		AssignFilterPopup.Closed += AssigneePopup_Closed;
		AssignFilterPopup.Child = _assignEditDialog;
		_assignEditDialog.OnCancel += delegate
		{
			AssignFilterPopup.IsOpen = false;
		};
		_assignEditDialog.OnSave += delegate
		{
			_viewModel.SaveTemplate();
			AssignFilterPopup.IsOpen = false;
			UserActCollectUtils.AddClickEvent("summary", "filter", "assignee");
			SetSummary();
		};
		AssignFilterPopup.IsOpen = true;
	}

	private void AssigneePopup_Closed(object sender, EventArgs e)
	{
		if (!_assignEditDialog.IsSave)
		{
			_assignEditDialog.Restore();
		}
		_assignEditDialog.IsSave = false;
	}

	private async void MenuGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		await Task.Delay(1);
		Utils.FindParent<ListViewContainer>(this)?.ShowProjectMenu();
	}

	public async void LoadData()
	{
		if (_viewModel != null)
		{
			_viewModel.LoadTemplate();
			RefreshDate();
			DisplayFilter.FilterChanged -= SetSummary;
			DisplayFilter.InitModel(_viewModel);
			DisplayFilter.FilterChanged += SetSummary;
			SetSummary();
		}
	}

	public bool RemoveInputFocus()
	{
		return !SummaryContent.IsMouseOver;
	}

	private void OnCopyClick(object sender, RoutedEventArgs e)
	{
		CopyPopup.IsOpen = true;
	}

	private void SaveImage()
	{
		string outputPath = "";
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			FileName = Utils.GetString("Summary") + "(" + GetTitle(withSpace: false) + ")",
			Filter = "Png Files|*.png"
		};
		if (saveFileDialog.ShowDialog() == DialogResult.OK)
		{
			outputPath = saveFileDialog.FileName;
		}
		if (ThemeUtil.IsDark())
		{
			SummaryContent.SetTheme();
			SummaryContent.Background = Brushes.White;
			SummaryContent.UpdateLayout();
			Utils.GetPicFromControl(SummaryContent, "png", outputPath);
			SummaryContent.ClearTheme();
			SummaryContent.Background = Brushes.Transparent;
		}
		else
		{
			SummaryContent.Background = Brushes.White;
			SummaryContent.UpdateLayout();
			Utils.GetPicFromControl(SummaryContent, "png", outputPath);
			SummaryContent.Background = Brushes.Transparent;
		}
	}

	private void OnContentLostFocus(object sender, RoutedEventArgs e)
	{
		SummaryContent.UnRegisterInputHandler();
	}

	private void OnContentGotFocus(object sender, RoutedEventArgs e)
	{
		SummaryContent.RegisterInputHandler();
	}

	private void EditorOnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	private void OnMouseScroll(object sender, MouseWheelEventArgs e)
	{
		ContentScrollViewer.ScrollToVerticalOffset(ContentScrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
	}

	private void OnContentClick(object sender, MouseButtonEventArgs e)
	{
		SummaryContent.FocusEditBox();
	}

	private void OnClearClick(object sender, MouseButtonEventArgs e)
	{
		_projectIds = null;
		_viewModel.DateFilter = DateFilter.ThisWeek;
		_viewModel.SelectedProjectIds = new List<string> { "ProjectAll2e4c103c57ef480997943206" };
		_viewModel.SelectedProjectGroupIds = new List<string>();
		_viewModel.SelectedCalendarAccountIds = new List<string>();
		_viewModel.SelectedCalendarIds = new List<string>();
		_viewModel.SelectedProjectDisplayText = Utils.GetString("AllList");
		_viewModel.SelectedTags = new List<string>();
		_viewModel.SelectedTagDisplayText = Utils.GetString("AllTags");
		_viewModel.Assignees = new List<string>();
		_viewModel.SelectedAssignTypeText = Utils.GetString("AssignTo");
		_viewModel.SelectedStatus = new List<string>();
		_viewModel.SelectedPriorities = new List<string>();
		_viewModel.SelectedPriorityDisplayText = Utils.GetString("all_priorities");
		_viewModel.SelectedStatusDisplayText = Utils.GetString("AllStatus");
		_viewModel.SaveTemplate();
		ClearFilterGrid.Visibility = Visibility.Collapsed;
		RefreshDate();
		SetSummary();
	}

	private void OnTemplateClick(object sender, MouseButtonEventArgs e)
	{
		CopyPopup.IsOpen = false;
		ExportPopup.IsOpen = false;
		if (!UserDao.IsPro())
		{
			ProChecker.ShowUpgradeDialog(ProType.SummaryTemplate);
			return;
		}
		if (_viewModel.SelectedTemplateId == "defaultId" && _viewModel.SummaryTemplates.Count > 9)
		{
			Utils.Toast(Utils.GetString("summary_template_limit_hint"));
			return;
		}
		UserActCollectUtils.AddClickEvent("summary", "action", "save_template");
		ShowSaveTemplateDialog();
	}

	private void ShowSaveTemplateDialog()
	{
		bool replace = _viewModel.SelectedTemplateId != "defaultId";
		List<string> exsitTitles = _viewModel.SummaryTemplates.Select((SummaryTemplate it) => it.name).ToList();
		EditTemplateWindow editTemplateWindow = new EditTemplateWindow((!replace) ? string.Empty : _viewModel.Name, exsitTitles, isAdd: true, Utils.GetString(replace ? "would_you_replace_template" : "save_template_desc"), appendNameIndex: true)
		{
			Title = Utils.GetString(replace ? "replace_template" : "SaveAsTemplate"),
			Owner = Window.GetWindow(this)
		};
		if (replace)
		{
			editTemplateWindow.OkButton.Content = Utils.GetString("replace_action");
		}
		editTemplateWindow.Save += delegate(object sender, string title)
		{
			if (replace)
			{
				_viewModel.SaveTemplate(title, sync: true);
			}
			else
			{
				_viewModel.AddTemplate(title, _viewModel);
			}
			ShowTemplateTooltip();
			LoadSummaryData();
		};
		editTemplateWindow.ShowDialog();
	}

	private async void ShowTemplateTooltip()
	{
		ExtraSettings settings = LocalSettings.Settings.ExtraSettings;
		if (!settings.SummaryTemplateTooltipShown)
		{
			System.Windows.Controls.ToolTip templateTip = new System.Windows.Controls.ToolTip
			{
				Content = Utils.GetString("summary_template_hint"),
				PlacementTarget = TemplateName,
				Placement = PlacementMode.Bottom,
				IsOpen = true
			};
			templateTip.IsOpen = true;
			await Task.Delay(3000);
			templateTip.IsOpen = false;
			settings.SummaryTemplateTooltipShown = true;
			LocalSettings.Settings.Save();
		}
	}

	private void OnCopyTypeClick(object sender, MouseButtonEventArgs e)
	{
		CopyPopup.IsOpen = false;
		ExportPopup.IsOpen = false;
		if (!(sender is StackPanel { Tag: string tag }))
		{
			return;
		}
		string summaryMarkdownText = GetSummaryMarkdownText();
		switch (tag)
		{
		case "email":
		case "richtext":
		{
			string text = MarkdownClipboardHtmlRenderer.CreateHtml(summaryMarkdownText);
			if (!string.IsNullOrEmpty(text))
			{
				try
				{
					CopySummaryRichTextToClipboard(summaryMarkdownText, text);
					Utils.Toast(Utils.GetString("Copied"));
				}
				catch (Exception)
				{
					Utils.Toast("error");
				}
			}
			else
			{
				Utils.Toast("error");
			}
			if (tag == "email")
			{
				try
				{
					Process.Start("mailto:");
				}
				catch (Exception)
				{
				}
				UserActCollectUtils.AddClickEvent("summary", "action", "save_email");
			}
			else
			{
				UserActCollectUtils.AddClickEvent("summary", "action", "copy_rich_text");
			}
			return;
		}
		case "pdf":
		{
			PrintPreviewWindow printPreviewWindow = new PrintPreviewWindow(summaryMarkdownText);
			UserActCollectUtils.AddClickEvent("summary", "action", "save_pdf");
			printPreviewWindow.Show();
			return;
		}
		case "png":
			UserActCollectUtils.AddClickEvent("summary", "action", "save_image");
			SaveImage();
			return;
		}
		string text2 = summaryMarkdownText;
		if (tag == "plaintext")
		{
			UserActCollectUtils.AddClickEvent("summary", "action", "copy_plain_text");
			text2 = MarkdownClipboardHtmlRenderer.CreatePlainText(summaryMarkdownText).Trim('\r', '\n');
		}
		else
		{
			UserActCollectUtils.AddClickEvent("summary", "action", "copy_md");
		}
		if (!string.IsNullOrEmpty(text2))
		{
			System.Windows.Clipboard.Clear();
			try
			{
				System.Windows.Forms.Clipboard.Clear();
				System.Windows.Forms.Clipboard.SetDataObject(text2, copy: false, 8, 250);
				Utils.Toast(Utils.GetString("Copied"));
			}
			catch (Exception)
			{
			}
		}
	}

	private void OnExportClick(object sender, RoutedEventArgs e)
	{
		ExportPopup.IsOpen = true;
	}

	private void OnInsertClick(object sender, RoutedEventArgs e)
	{
		this.InsertSummary?.Invoke(this, GetSummaryMarkdownText());
	}

	private string GetSummaryMarkdownText()
	{
		return SummaryContent?.GetPersistedText() ?? string.Empty;
	}

	private static void CopySummaryRichTextToClipboard(string markdown, string html)
	{
		string textData = MarkdownClipboardHtmlRenderer.CreatePlainText(markdown);
		System.Windows.Forms.DataObject dataObject = new System.Windows.Forms.DataObject();
		dataObject.SetText(html, System.Windows.Forms.TextDataFormat.Html);
		dataObject.SetText(textData, System.Windows.Forms.TextDataFormat.UnicodeText);
		dataObject.SetText(textData, System.Windows.Forms.TextDataFormat.Text);
		dataObject.SetData("Markdown", autoConvert: false, markdown ?? string.Empty);
		System.Windows.Forms.Clipboard.SetDataObject(dataObject, copy: false, 8, 250);
	}

	public void SetInsertMode()
	{
		InsertButton.Visibility = Visibility.Visible;
		TextEditorMenu.Visibility = Visibility.Collapsed;
		SpliteLine.Visibility = Visibility.Collapsed;
		CopyButton.Visibility = Visibility.Collapsed;
		ExportButton.Visibility = Visibility.Collapsed;
		HideFoldMenuIcon();
		ContentGrid.Margin = new Thickness(10.0);
	}

	public void HideFoldMenuIcon()
	{
		MenuPathGrid.Visibility = Visibility.Collapsed;
	}

	private void OnTemplateToggleClick(object sender, MouseButtonEventArgs e)
	{
		if (_viewModel.ShowTemplate)
		{
			ObservableCollection<SummaryTemplateViewModel> itemsSource = new ObservableCollection<SummaryTemplateViewModel>(_viewModel.SummaryTemplates.Select((SummaryTemplate it) => new SummaryTemplateViewModel
			{
				Id = it.id,
				Name = it.name,
				IsSelected = (_viewModel.SelectedTemplateId == it.id)
			}));
			TemplatesItems.ItemsSource = itemsSource;
			ChooseTemplatePopup.IsOpen = true;
		}
	}

	private void OnSelectTemplateClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement frameworkElement)
		{
			SummaryTemplateViewModel summaryTemplateViewModel = (SummaryTemplateViewModel)frameworkElement.DataContext;
			summaryTemplateViewModel.IsSelected = true;
			LocalSettings.Settings.ExtraSettings.SelectedSummaryTemplateId = summaryTemplateViewModel.Id;
			LocalSettings.Settings.Save();
			ChooseTemplatePopup.IsOpen = false;
			LoadSummaryData();
		}
	}

	private void MoreFilterClick(object sender, MouseButtonEventArgs e)
	{
		MoreConditionPopup.ItemsSource = new ObservableCollection<FilterConditionViewModel>(_viewModel.MoreConditions.Select((string it) => new FilterConditionViewModel
		{
			Id = it,
			Name = Utils.GetString(it)
		}));
		MoreFilterPopup.IsOpen = true;
	}

	private void OnFilterConditionClick(object sender, MouseButtonEventArgs e)
	{
		MoreFilterPopup.IsOpen = false;
		if (sender is FrameworkElement frameworkElement)
		{
			string item = (frameworkElement.DataContext as FilterConditionViewModel)?.Id;
			List<string> moreConditions = _viewModel.MoreConditions;
			moreConditions.Remove(item);
			_viewModel.MoreConditions = moreConditions;
		}
	}

	private void OnTemplateOptionClick(object sender, MouseButtonEventArgs e)
	{
		TemplateOptionPopup.PlacementTarget = sender as UIElement;
		TemplateOptionPopup.Placement = PlacementMode.Right;
		TemplateOptionPopup.IsOpen = true;
		if (sender is FrameworkElement frameworkElement)
		{
			_editingTemplate = frameworkElement.DataContext as SummaryTemplateViewModel;
		}
	}

	private void OnTemplateEditClick(object s, MouseButtonEventArgs e)
	{
		TemplateOptionPopup.IsOpen = false;
		if (_editingTemplate == null)
		{
			return;
		}
		List<string> exsitTitles = _viewModel.SummaryTemplates.Select((SummaryTemplate it) => it.name).ToList();
		EditTemplateWindow editTemplateWindow = new EditTemplateWindow(_editingTemplate.Name, exsitTitles, isAdd: true, string.Empty, appendNameIndex: true);
		editTemplateWindow.Title = Utils.GetString("edit_template");
		editTemplateWindow.Owner = Window.GetWindow(this);
		editTemplateWindow.Save += delegate(object sender, string title)
		{
			_viewModel.SaveTemplate(_editingTemplate, delegate(SummaryTemplate template)
			{
				template.name = title;
			});
			LoadSummaryData();
		};
		editTemplateWindow.ShowDialog();
	}

	private void OnTemplateDeleteClick(object sender, MouseButtonEventArgs e)
	{
		DeleteTemplate();
	}

	private void DeleteTemplate()
	{
		if (_editingTemplate != null)
		{
			_viewModel.DeleteTemplate(_editingTemplate.Id);
		}
		ObservableCollection<SummaryTemplateViewModel> itemsSource = new ObservableCollection<SummaryTemplateViewModel>(_viewModel.SummaryTemplates.Select((SummaryTemplate it) => new SummaryTemplateViewModel
		{
			Id = it.id,
			Name = it.name,
			IsSelected = (_viewModel.SelectedTemplateId == it.id)
		}));
		TemplatesItems.ItemsSource = itemsSource;
		TemplateOptionPopup.IsOpen = false;
		ChooseTemplatePopup.IsOpen = false;
		LoadSummaryData();
	}

	public void SetFoldMenuIcon(bool hideMenu, bool oneColumn, bool showClose)
	{
		FoldImage.SetResourceReference(Path.DataProperty, oneColumn ? "Ic.ProjectList.List" : (hideMenu ? "Ic.ProjectList.ExpandSide" : "Ic.ProjectList.CollapseSide"));
		SetColumnMode(oneColumn);
		CloseButton.Visibility = ((!showClose) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void SetColumnMode(bool isOneColumn)
	{
		_inOneColumn = isOneColumn;
		if (_viewModel != null && _viewModel.ShowTemplate)
		{
			isOneColumn = false;
		}
		if (isOneColumn)
		{
			TemplateBorder.IsHitTestVisible = false;
			TitlePanel.IsHitTestVisible = false;
			TitleBorder.SetResourceReference(FrameworkElement.StyleProperty, "HoverBorderStyle");
			TitleBorder.PreviewMouseLeftButtonDown -= OnTitleBorderMouseDown;
			TitleBorder.PreviewMouseLeftButtonDown += OnTitleBorderMouseDown;
			TitleBorder.MouseLeftButtonUp -= OnTitleClick;
			TitleBorder.MouseLeftButtonUp += OnTitleClick;
		}
		else
		{
			TitlePanel.IsHitTestVisible = true;
			TemplateBorder.IsHitTestVisible = true;
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

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("split_view", "calendar+task", "close_from_om");
		MainWindow.ShowTaskInCalendarChanged();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/summary/summarycontrol.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			Container = (Grid)target;
			break;
		case 2:
			TitleBorder = (Border)target;
			break;
		case 3:
			TitlePanel = (StackPanel)target;
			break;
		case 4:
			MenuPathGrid = (Border)target;
			MenuPathGrid.MouseLeftButtonUp += MenuGrid_MouseLeftButtonUp;
			break;
		case 5:
			FoldImage = (Path)target;
			break;
		case 6:
			TemplateBorder = (Border)target;
			TemplateBorder.MouseLeftButtonUp += OnTemplateToggleClick;
			break;
		case 7:
			TemplateName = (TextBlock)target;
			break;
		case 8:
			ArrowGrid = (Border)target;
			break;
		case 9:
			CloseButton = (HoverIconButton)target;
			break;
		case 10:
			ChooseTemplatePopup = (EscPopup)target;
			break;
		case 11:
			TemplatesItems = (ItemsControl)target;
			break;
		case 14:
			TemplateOptionPopup = (EscPopup)target;
			break;
		case 15:
			DateFilterText = (PopupPlacementBorder)target;
			break;
		case 16:
			((TextBlock)target).MouseLeftButtonUp += DateFilterClick;
			break;
		case 17:
			DateSelectPopup = (EscPopup)target;
			break;
		case 18:
			ProjectOrGroupFilterText = (PopupPlacementBorder)target;
			break;
		case 19:
			ProjectOrGroupFilterPopup = (EscPopup)target;
			break;
		case 20:
			TagFilterText = (PopupPlacementBorder)target;
			break;
		case 21:
			TagFilterPopup = (EscPopup)target;
			break;
		case 22:
			StatusFilter = (PopupPlacementBorder)target;
			break;
		case 23:
			StatusFilterPopup = (EscPopup)target;
			break;
		case 24:
			PriorityFilter = (PopupPlacementBorder)target;
			break;
		case 25:
			PriorityFilterPopup = (EscPopup)target;
			break;
		case 26:
			AssignFilter = (PopupPlacementBorder)target;
			break;
		case 27:
			AssignFilterPopup = (EscPopup)target;
			break;
		case 28:
			MoreFilter = (PopupPlacementBorder)target;
			break;
		case 29:
			MoreFilterPopup = (EscPopup)target;
			break;
		case 30:
			MoreConditionPopup = (ItemsControl)target;
			break;
		case 31:
			ClearFilterGrid = (Grid)target;
			ClearFilterGrid.MouseLeftButtonUp += OnClearClick;
			break;
		case 32:
			DisplayFilter = (SummaryDisplayFilterControl)target;
			break;
		case 33:
			TextEditorMenu = (EditorMenu)target;
			break;
		case 34:
			SpliteLine = (Grid)target;
			break;
		case 35:
			ContentGrid = (Grid)target;
			break;
		case 36:
			ContentScrollViewer = (ScrollViewer)target;
			ContentScrollViewer.PreviewMouseWheel += OnMouseScroll;
			break;
		case 37:
			MdGrid = (Grid)target;
			MdGrid.MouseLeftButtonUp += OnContentClick;
			break;
		case 38:
			SummaryContent = (MarkDownEditor)target;
			break;
		case 39:
			CopyButton = (System.Windows.Controls.Button)target;
			CopyButton.Click += OnCopyClick;
			break;
		case 40:
			ExportButton = (System.Windows.Controls.Button)target;
			ExportButton.Click += OnExportClick;
			break;
		case 41:
			InsertButton = (System.Windows.Controls.Button)target;
			InsertButton.Click += OnInsertClick;
			break;
		case 42:
			CopyPopup = (Popup)target;
			break;
		case 43:
			((StackPanel)target).MouseLeftButtonUp += OnCopyTypeClick;
			break;
		case 44:
			((StackPanel)target).MouseLeftButtonUp += OnCopyTypeClick;
			break;
		case 45:
			((StackPanel)target).MouseLeftButtonUp += OnCopyTypeClick;
			break;
		case 46:
			ExportPopup = (Popup)target;
			break;
		case 47:
			((StackPanel)target).MouseLeftButtonUp += OnTemplateClick;
			break;
		case 48:
			((StackPanel)target).MouseLeftButtonUp += OnCopyTypeClick;
			break;
		case 49:
			((StackPanel)target).MouseLeftButtonUp += OnCopyTypeClick;
			break;
		case 50:
			((StackPanel)target).MouseLeftButtonUp += OnCopyTypeClick;
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
		case 12:
			((Grid)target).MouseLeftButtonUp += OnTemplateOptionClick;
			break;
		case 13:
			((Border)target).MouseLeftButtonUp += OnSelectTemplateClick;
			break;
		}
	}
}
