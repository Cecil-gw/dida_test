using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Models.Interface;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Filter;

public class ProjectOrGroupEditDialog : ConditionEditDialogBase<string>
{
	private class UnavailableLocalCalendarFilterItemViewModel : FilterListItemViewModel
	{
		public List<string> AccountIds { get; }

		public List<string> CalendarIds { get; }

		public UnavailableLocalCalendarFilterItemViewModel(IEnumerable<string> accountIds, IEnumerable<string> calendarIds)
		{
			AccountIds = accountIds?.Where((string id) => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
			CalendarIds = calendarIds?.Where((string id) => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
		}
	}

	private const string ItemKindAllLists = "all_lists";

	private const string ItemKindListGroup = "list_group";

	private const string ItemKindProject = "project";

	private const string ItemKindCalendarRoot = "calendar_root";

	private const string ItemKindCalendarAccount = "calendar_account";

	private const string ItemKindCalendarItem = "calendar_item";

	private const string ItemKindUnavailableCalendarItem = "calendar_unavailable";

	private readonly LogicType _logic;

	private readonly List<ProjectModel> _projects;

	private readonly bool _showAll;

	private List<string> _originalGroupIds;

	private List<string> _originalProjectIds;

	private List<string> _originalCalendarAccountIds;

	private List<string> _originalCalendarIds;

	public event EventHandler<List<string>> OnSelectedProjectChanged;

	public event EventHandler<List<string>> OnSelectedGroupChanged;

	public event EventHandler<List<string>> OnSelectedCalendarAccountChanged;

	public event EventHandler<List<string>> OnSelectedCalendarChanged;

	public ProjectOrGroupEditDialog(bool showAll, List<string> projectIds, List<string> groupIds, LogicType logicType = LogicType.Or, List<string> calendarAccountIds = null, List<string> calendarIds = null)
	{
		InitializeComponent();
		_showAll = showAll;
		_projects = CacheManager.GetProjects();
		List<string> list = projectIds.ToList();
		List<string> originalCalendarIds = calendarIds?.Where((string id) => id != "#allSubscribe").ToList() ?? new List<string>();
		if (calendarIds != null && calendarIds.Contains("#allSubscribe") && !list.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			list.Add("Calendar5959a2259161d16d23a4f272");
		}
		_originalProjectIds = list;
		_originalGroupIds = groupIds.ToList();
		_originalCalendarAccountIds = calendarAccountIds?.ToList() ?? new List<string>();
		_originalCalendarIds = originalCalendarIds;
		_logic = logicType;
		InitData(_originalProjectIds, groupIds);
	}

	private ProjectOrGroupEditDialog(bool showAll)
		: this(showAll, new List<string>(), new List<string>())
	{
	}

	public ProjectOrGroupEditDialog()
		: this(showAll: false)
	{
	}

	private void InitData(List<string> projectIds, List<string> groupIds)
	{
		bool allListSelected = projectIds.Any(CalendarUtils.IsAllListId);
		bool allCalendarSelected = projectIds.Contains("Calendar5959a2259161d16d23a4f272") && !_originalCalendarAccountIds.Any() && !_originalCalendarIds.Any();
		string inboxId = Utils.GetInboxId();
		FilterListItemViewModel filterListItemViewModel = new FilterListItemViewModel
		{
			Title = Utils.GetString("AllList"),
			Icon = "Ic.ProjectList.All",
			ShowIcon = true,
			Value = "ProjectAll2e4c103c57ef480997943206",
			Kind = "all_lists",
			Selected = allListSelected,
			IsProjectGroup = true,
			UseCircleCheck = true,
			Unfold = true,
			Children = new List<FilterListItemViewModel>()
		};
		List<FilterListItemViewModel> list = new List<FilterListItemViewModel>
		{
			new FilterListItemViewModel
			{
				Title = Utils.GetString("Inbox"),
				Icon = "Ic.ProjectList.Inbox",
				IsSecondLevel = false,
				IndentLevel = 1,
				SortOrder = long.MinValue,
				Value = inboxId,
				GroupId = "NONE",
				ShowIcon = true,
				Kind = "project",
				Selected = (allListSelected || projectIds.Contains(inboxId))
			}
		};
		List<SelectableItemViewModel> source = ProjectDataAssembler.AssembleProjects();
		if (source.Any())
		{
			list.AddRange(from project in source
				where !(project is ticktick_WPF.Views.Misc.SubProjectViewModel)
				select BuildProjectItem(project, projectIds, groupIds, allListSelected));
		}
		foreach (FilterListItemViewModel item in list.Where((FilterListItemViewModel item) => item != null))
		{
			AssignIndentLevel(item, 0);
			filterListItemViewModel.Children.Add(item);
		}
		ObservableCollection<FilterListItemViewModel> observableCollection = new ObservableCollection<FilterListItemViewModel>();
		observableCollection.Add(filterListItemViewModel);
		if (filterListItemViewModel.Unfold)
		{
			int index = observableCollection.IndexOf(filterListItemViewModel);
			ConditionEditDialog.InsertVisibleChildren(observableCollection, filterListItemViewModel, ref index);
		}
		bool flag = projectIds.Contains("Calendar5959a2259161d16d23a4f272");
		if (!flag)
		{
			flag = CacheManager.GetSubscribeCalendars().Any((CalendarSubscribeProfileModel cal) => cal.Show != "hidden");
		}
		if (!flag)
		{
			flag = CacheManager.GetBindCalendars().Any((BindCalendarModel cal) => cal.Show != "hidden");
		}
		if (flag)
		{
			observableCollection.Add(new FilterListItemViewModel
			{
				IsSplit = true
			});
			AddCalendarItems(observableCollection, allCalendarSelected);
		}
		base.ViewModel = new FilterConditionViewModel
		{
			Type = CondType.Lists,
			ItemsSource = observableCollection,
			ShowAll = _showAll,
			IsAllSelected = (projectIds.Count == 0 && groupIds.Count == 0 && _originalCalendarAccountIds.Count == 0 && _originalCalendarIds.Count == 0),
			Logic = _logic,
			SupportedLogic = new List<LogicType>
			{
				LogicType.Or,
				LogicType.Not
			}
		};
		base.OnSelectedValueChanged += delegate
		{
			NormalizeProjectSelections();
			NormalizeCalendarSelections();
			List<string> selectedGroups = GetSelectedGroups();
			List<string> selectedProjectIds = GetSelectedProjectIds(selectedGroups);
			List<string> selectedCalendarAccountIds = GetSelectedCalendarAccountIds();
			List<string> selectedCalendarIds = GetSelectedCalendarIds();
			if (IsAllCalendarsSelected() || selectedCalendarAccountIds.Any() || selectedCalendarIds.Any())
			{
				if (!selectedProjectIds.Contains("Calendar5959a2259161d16d23a4f272"))
				{
					selectedProjectIds.Add("Calendar5959a2259161d16d23a4f272");
				}
			}
			else
			{
				selectedProjectIds.RemoveAll((string id) => id == "Calendar5959a2259161d16d23a4f272");
			}
			this.OnSelectedProjectChanged?.Invoke(this, selectedProjectIds);
			this.OnSelectedGroupChanged?.Invoke(this, selectedGroups);
			this.OnSelectedCalendarAccountChanged?.Invoke(this, selectedCalendarAccountIds);
			this.OnSelectedCalendarChanged?.Invoke(this, selectedCalendarIds);
		};
	}

	protected override bool CanSave()
	{
		List<string> selectedGroups = GetSelectedGroups();
		if (GetSelectedProjectIds(selectedGroups).Count <= 0 && selectedGroups.Count <= 0 && !IsAllCalendarsSelected() && GetSelectedCalendarAccountIds().Count <= 0 && GetSelectedCalendarIds().Count <= 0)
		{
			return base.CanSave();
		}
		return true;
	}

	protected override void SyncOriginal()
	{
		_originalGroupIds = GetSelectedGroups().ToList();
		_originalProjectIds = GetSelectedProjectIds(_originalGroupIds).ToList();
		if (IsAllCalendarsSelected() && !_originalProjectIds.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			_originalProjectIds.Add("Calendar5959a2259161d16d23a4f272");
		}
		_originalCalendarAccountIds = GetSelectedCalendarAccountIds().ToList();
		_originalCalendarIds = GetSelectedCalendarIds().ToList();
	}

	public override void Restore()
	{
		RestoreSetSelected();
		this.OnSelectedProjectChanged?.Invoke(this, _originalProjectIds);
		this.OnSelectedGroupChanged?.Invoke(this, _originalGroupIds);
		this.OnSelectedCalendarAccountChanged?.Invoke(this, _originalCalendarAccountIds);
		this.OnSelectedCalendarChanged?.Invoke(this, _originalCalendarIds);
	}

	private void RestoreSetSelected()
	{
		foreach (FilterListItemViewModel allItem in GetAllItems())
		{
			switch (allItem.Kind)
			{
			case "all_lists":
				allItem.Selected = _originalProjectIds.Any(CalendarUtils.IsAllListId);
				break;
			case "list_group":
				allItem.Selected = _originalProjectIds.Any(CalendarUtils.IsAllListId) || _originalGroupIds.Contains(allItem.Value?.ToString());
				break;
			case "project":
				allItem.Selected = _originalProjectIds.Any(CalendarUtils.IsAllListId) || _originalProjectIds.Contains(allItem.Value?.ToString()) || _originalGroupIds.Contains(allItem.GroupId);
				break;
			case "calendar_root":
				allItem.Selected = _originalProjectIds.Contains("Calendar5959a2259161d16d23a4f272") && !_originalCalendarAccountIds.Any() && !_originalCalendarIds.Any();
				break;
			case "calendar_account":
				allItem.Selected = CalendarUtils.ContainsAccountId(_originalCalendarAccountIds, allItem.Value?.ToString());
				break;
			case "calendar_item":
				allItem.Selected = (_originalProjectIds.Contains("Calendar5959a2259161d16d23a4f272") && !_originalCalendarAccountIds.Any() && !_originalCalendarIds.Any()) || CalendarUtils.ContainsAccountId(_originalCalendarAccountIds, allItem.GroupId) || CalendarUtils.ContainsAccountId(_originalCalendarIds, allItem.GroupId) || CalendarUtils.ContainsCalendarId(_originalCalendarIds, allItem.Value?.ToString());
				break;
			case "calendar_unavailable":
				allItem.Selected = IsUnavailableLocalCalendarSelected(allItem);
				break;
			default:
				allItem.Selected = _originalProjectIds.Contains(allItem.Value?.ToString()) || _originalGroupIds.Contains(allItem.GroupId) || _originalGroupIds.Contains(allItem.Value?.ToString());
				break;
			}
		}
		NormalizeProjectSelections();
		NormalizeCalendarSelections();
	}

	private List<string> GetSelectedGroups()
	{
		List<FilterListItemViewModel> allItems = GetAllItems();
		if (allItems.Any((FilterListItemViewModel item) => item.Kind == "all_lists" && item.Selected))
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		foreach (FilterListItemViewModel item in allItems)
		{
			if (item.Selected && item.Kind == "list_group")
			{
				list.Add(item.Value.ToString());
			}
		}
		return list;
	}

	private List<string> GetSelectedProjectIds(IReadOnlyCollection<string> selectedGroupIds)
	{
		List<FilterListItemViewModel> allItems = GetAllItems();
		List<string> list = new List<string>();
		if (allItems.Any((FilterListItemViewModel item) => item.Kind == "all_lists" && item.Selected))
		{
			list.Add("ProjectAll2e4c103c57ef480997943206");
			return list;
		}
		foreach (FilterListItemViewModel item in allItems)
		{
			if (!(item.Kind != "project") && item.Selected)
			{
				string text = item.Value?.ToString();
				if (!string.IsNullOrEmpty(text) && !selectedGroupIds.Contains(item.GroupId))
				{
					list.Add(text);
				}
			}
		}
		return list;
	}

	private List<string> GetSelectedCalendarAccountIds()
	{
		List<FilterListItemViewModel> allItems = GetAllItems();
		if (IsAllCalendarsSelected())
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		foreach (FilterListItemViewModel item in allItems.Where((FilterListItemViewModel item) => item.Kind == "calendar_account" && item.Selected && !ShouldPreserveAccountAsCalendar(item)))
		{
			foreach (string calendarAccountRuleId in GetCalendarAccountRuleIds(item))
			{
				if (!list.Contains(calendarAccountRuleId))
				{
					list.Add(calendarAccountRuleId);
				}
			}
		}
		foreach (UnavailableLocalCalendarFilterItemViewModel item2 in from item in allItems.OfType<UnavailableLocalCalendarFilterItemViewModel>()
			where item.Selected
			select item)
		{
			foreach (string accountId in item2.AccountIds)
			{
				foreach (string calendarAccountRuleId2 in GetCalendarAccountRuleIds(accountId))
				{
					if (!list.Contains(calendarAccountRuleId2))
					{
						list.Add(calendarAccountRuleId2);
					}
				}
			}
		}
		PreserveUnavailableCalendarAccountIds(list, allItems);
		return list;
	}

	private List<string> GetSelectedCalendarIds()
	{
		List<FilterListItemViewModel> allItems = GetAllItems();
		if (IsAllCalendarsSelected())
		{
			return new List<string>();
		}
		HashSet<string> selectedAccounts = new HashSet<string>(GetSelectedCalendarAccountIds());
		List<FilterListItemViewModel> list = allItems.Where((FilterListItemViewModel item) => item.Kind == "calendar_account" && item.Selected && ShouldPreserveAccountAsCalendar(item)).ToList();
		HashSet<string> selectedPreservedAccountIds = new HashSet<string>(from item in list
			select item.Value?.ToString() into id
			where !string.IsNullOrEmpty(id)
			select id);
		List<string> list2 = new List<string>();
		foreach (FilterListItemViewModel item in allItems.Where((FilterListItemViewModel item) => IsCalendarRuleItem(item) && item.Selected && !CalendarUtils.ContainsAccountId(selectedAccounts, item.GroupId) && !CalendarUtils.ContainsAccountId(selectedPreservedAccountIds, item.GroupId)))
		{
			foreach (string calendarRuleId in GetCalendarRuleIds(item))
			{
				if (!list2.Contains(calendarRuleId))
				{
					list2.Add(calendarRuleId);
				}
			}
		}
		foreach (FilterListItemViewModel item2 in list)
		{
			foreach (string originalAccountCalendarId in GetOriginalAccountCalendarIds(item2))
			{
				if (!list2.Contains(originalAccountCalendarId))
				{
					list2.Add(originalAccountCalendarId);
				}
			}
		}
		return list2;
	}

	private void NormalizeProjectSelections()
	{
		FilterListItemViewModel filterListItemViewModel = ListView.SelectedItem as FilterListItemViewModel;
		FilterListItemViewModel filterListItemViewModel2 = GetAllItems().FirstOrDefault((FilterListItemViewModel item) => item.Kind == "all_lists");
		if (filterListItemViewModel2 != null)
		{
			if (filterListItemViewModel?.Kind == "all_lists" && filterListItemViewModel.Selected)
			{
				ConditionEditDialog.SetChildrenSelectedRecursive(filterListItemViewModel2, selected: true);
				filterListItemViewModel2.PartSelected = false;
			}
			else if (filterListItemViewModel == null || filterListItemViewModel.Kind == "project" || filterListItemViewModel.Kind == "list_group")
			{
				RefreshAllListSelection(filterListItemViewModel2);
			}
		}
	}

	private static void RefreshAllListSelection(FilterListItemViewModel allListItem)
	{
		List<FilterListItemViewModel> source = (from item in ConditionEditDialog.EnumerateDescendants(allListItem)
			where item.Kind == "project" || item.Kind == "list_group"
			select item).ToList();
		if (source.Any())
		{
			allListItem.Selected = source.All((FilterListItemViewModel item) => item.Selected);
			allListItem.PartSelected = false;
		}
	}

	private void NormalizeCalendarSelections()
	{
		FilterListItemViewModel filterListItemViewModel = GetAllItems().FirstOrDefault((FilterListItemViewModel item) => item.Kind == "calendar_root");
		if (filterListItemViewModel == null)
		{
			return;
		}
		FilterListItemViewModel filterListItemViewModel2 = ListView.SelectedItem as FilterListItemViewModel;
		if (filterListItemViewModel2?.Kind == "calendar_root" && filterListItemViewModel2.Selected)
		{
			ConditionEditDialog.SetChildrenSelectedRecursive(filterListItemViewModel, selected: true);
			filterListItemViewModel.PartSelected = false;
			return;
		}
		foreach (FilterListItemViewModel item in from item in ConditionEditDialog.EnumerateDescendants(filterListItemViewModel)
			where item.Kind == "calendar_account"
			select item)
		{
			List<FilterListItemViewModel> source = item.Children ?? new List<FilterListItemViewModel>();
			if (source.Any())
			{
				item.Selected = source.All((FilterListItemViewModel child) => child.Selected);
				item.PartSelected = !item.Selected && source.Any((FilterListItemViewModel child) => child.Selected);
			}
		}
		List<FilterListItemViewModel> source2 = ConditionEditDialog.EnumerateDescendants(filterListItemViewModel).Where(IsCalendarRuleItem).ToList();
		if (source2.Any())
		{
			bool flag = ConditionEditDialog.EnumerateDescendants(filterListItemViewModel).Any((FilterListItemViewModel item) => item.Kind == "calendar_account" && item.Selected && ShouldPreserveAccountAsCalendar(item));
			bool flag2 = source2.Any(IsUnavailableLocalCalendarItem);
			filterListItemViewModel.Selected = !flag && !flag2 && source2.All((FilterListItemViewModel item) => item.Selected);
			filterListItemViewModel.PartSelected = false;
		}
	}

	private bool IsAllCalendarsSelected()
	{
		return GetAllItems().Any((FilterListItemViewModel item) => item.Kind == "calendar_root" && item.Selected);
	}

	private bool ShouldPreserveAccountAsCalendar(FilterListItemViewModel account)
	{
		string accountId = account?.Value?.ToString();
		return _originalCalendarIds?.Any((string id) => id != "#allSubscribe" && CalendarUtils.IsSameAccountId(id, accountId)) ?? false;
	}

	private IEnumerable<string> GetCalendarAccountRuleIds(FilterListItemViewModel account)
	{
		return GetCalendarAccountRuleIds(account?.Value?.ToString());
	}

	private IEnumerable<string> GetCalendarAccountRuleIds(string accountId)
	{
		if (string.IsNullOrEmpty(accountId))
		{
			return Enumerable.Empty<string>();
		}
		List<string> list = new List<string>();
		foreach (string id in SplitCalendarRuleIds(accountId))
		{
			List<string> list2 = _originalCalendarAccountIds?.Where((string originalId) => CalendarUtils.IsSameAccountId(originalId, id)).ToList();
			list.AddRange((list2 != null && list2.Any()) ? list2 : new List<string> { id });
		}
		return list;
	}

	private void PreserveUnavailableCalendarAccountIds(List<string> selected, IEnumerable<FilterListItemViewModel> allItems)
	{
		if (selected == null || IsAllCalendarsSelected())
		{
			return;
		}
		List<string> visibleAccountIds = (from id in allItems?.SelectMany(GetVisibleCalendarAccountRuleIds)
			where !string.IsNullOrEmpty(id)
			select id).ToList() ?? new List<string>();
		foreach (string originalId in _originalCalendarAccountIds?.Where((string id) => !string.IsNullOrEmpty(id) && !CalendarUtils.ContainsAccountId(visibleAccountIds, id)) ?? Enumerable.Empty<string>())
		{
			if (!selected.Any((string id) => CalendarUtils.IsSameAccountId(id, originalId)))
			{
				selected.Add(originalId);
			}
		}
	}

	private IEnumerable<string> GetVisibleCalendarAccountRuleIds(FilterListItemViewModel item)
	{
		if (item is UnavailableLocalCalendarFilterItemViewModel unavailableLocalCalendarFilterItemViewModel)
		{
			return unavailableLocalCalendarFilterItemViewModel.AccountIds;
		}
		if (!(item?.Kind == "calendar_account"))
		{
			return Enumerable.Empty<string>();
		}
		return SplitCalendarRuleIds(item.Value?.ToString());
	}

	private IEnumerable<string> GetOriginalAccountCalendarIds(FilterListItemViewModel account)
	{
		string accountId = account?.Value?.ToString();
		List<string> list = new List<string>();
		foreach (string item in _originalCalendarIds?.Where((string id) => id != "#allSubscribe" && CalendarUtils.IsSameAccountId(id, accountId)) ?? Enumerable.Empty<string>())
		{
			list.Add(item);
		}
		foreach (FilterListItemViewModel item2 in (account?.Children ?? new List<FilterListItemViewModel>()).Where((FilterListItemViewModel child) => child.Kind == "calendar_item"))
		{
			foreach (string originalCalendarRuleId in GetOriginalCalendarRuleIds(item2))
			{
				if (!list.Contains(originalCalendarRuleId))
				{
					list.Add(originalCalendarRuleId);
				}
			}
		}
		return list;
	}

	private IEnumerable<string> GetCalendarRuleIds(FilterListItemViewModel calendar)
	{
		List<string> list = new List<string>();
		foreach (string calendarId in SplitCalendarRuleIds(calendar?.Value?.ToString()))
		{
			List<string> list2 = _originalCalendarIds?.Where((string originalId) => originalId != "#allSubscribe" && CalendarUtils.IsSameCalendarId(originalId, calendarId)).ToList();
			if (list2 != null && list2.Any())
			{
				list.AddRange(list2);
			}
			else
			{
				list.Add(calendarId);
			}
		}
		return list;
	}

	private IEnumerable<string> GetOriginalCalendarRuleIds(FilterListItemViewModel calendar)
	{
		foreach (string calendarId in SplitCalendarRuleIds(calendar?.Value?.ToString()))
		{
			foreach (string item in _originalCalendarIds?.Where((string originalId) => originalId != "#allSubscribe" && CalendarUtils.IsSameCalendarId(originalId, calendarId)) ?? Enumerable.Empty<string>())
			{
				yield return item;
			}
		}
	}

	private static IEnumerable<string> SplitCalendarRuleIds(string calendarIds)
	{
		if (!string.IsNullOrEmpty(calendarIds))
		{
			return from id in calendarIds.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				select id.Trim();
		}
		return Enumerable.Empty<string>();
	}

	private static bool IsCalendarRuleItem(FilterListItemViewModel item)
	{
		if (!(item.Kind == "calendar_item"))
		{
			return item.Kind == "calendar_unavailable";
		}
		return true;
	}

	private static bool IsCalendarAccountRuleItem(FilterListItemViewModel item)
	{
		return item.Kind == "calendar_account";
	}

	private static bool IsUnavailableLocalCalendarItem(FilterListItemViewModel item)
	{
		return item.Kind == "calendar_unavailable";
	}

	private bool IsUnavailableLocalCalendarSelected(FilterListItemViewModel item)
	{
		if (item is UnavailableLocalCalendarFilterItemViewModel unavailableLocalCalendarFilterItemViewModel)
		{
			if (!unavailableLocalCalendarFilterItemViewModel.AccountIds.Any((string id) => CalendarUtils.ContainsAccountId(_originalCalendarAccountIds, id)))
			{
				return unavailableLocalCalendarFilterItemViewModel.CalendarIds.Any((string id) => _originalCalendarIds.Contains(id));
			}
			return true;
		}
		return SplitCalendarRuleIds(item?.Value?.ToString()).Any((string id) => _originalCalendarIds.Contains(id));
	}

	private List<FilterListItemViewModel> GetAllItems()
	{
		List<FilterListItemViewModel> list = new List<FilterListItemViewModel>();
		foreach (FilterListItemViewModel item in base.ViewModel.ItemsSource)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
			foreach (FilterListItemViewModel item2 in ConditionEditDialog.EnumerateDescendants(item))
			{
				if (!list.Contains(item2))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	private FilterListItemViewModel BuildProjectItem(SelectableItemViewModel project, List<string> projectIds, List<string> groupIds, bool allListSelected)
	{
		FilterListItemViewModel filterListItemViewModel = FilterListItemViewModel.BuildItem(project, projectIds, groupIds);
		if (filterListItemViewModel == null)
		{
			return null;
		}
		if (filterListItemViewModel.IsTeam)
		{
			filterListItemViewModel.Kind = string.Empty;
			return filterListItemViewModel;
		}
		filterListItemViewModel.Kind = (filterListItemViewModel.IsProjectGroup ? "list_group" : "project");
		if (allListSelected && (filterListItemViewModel.Kind == "project" || filterListItemViewModel.Kind == "list_group"))
		{
			filterListItemViewModel.Selected = true;
		}
		List<FilterListItemViewModel> children = filterListItemViewModel.Children;
		if (children != null && children.Any())
		{
			foreach (FilterListItemViewModel child in filterListItemViewModel.Children)
			{
				child.Kind = "project";
				if (allListSelected)
				{
					child.Selected = true;
				}
			}
		}
		return filterListItemViewModel;
	}

	private static void AssignIndentLevel(FilterListItemViewModel item, int level)
	{
		item.IndentLevel = level;
		if (item.Children == null)
		{
			return;
		}
		foreach (FilterListItemViewModel child in item.Children)
		{
			AssignIndentLevel(child, level + 1);
		}
	}

	private void AddCalendarItems(ObservableCollection<FilterListItemViewModel> itemSource, bool allCalendarSelected)
	{
		FilterListItemViewModel filterListItemViewModel = new FilterListItemViewModel
		{
			Title = Utils.GetString("AllCalendars", "All Calendars"),
			Icon = "Ic.ProjectList.CalendarSubscribe",
			ShowIcon = true,
			Value = "Calendar5959a2259161d16d23a4f272",
			Kind = "calendar_root",
			IsProjectGroup = true,
			UseCircleCheck = true,
			Unfold = true,
			Selected = allCalendarSelected,
			Children = new List<FilterListItemViewModel>()
		};
		itemSource.Add(filterListItemViewModel);
		List<OrderInterface> list = new List<OrderInterface>();
		List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
		if (bindCalendarAccounts.Any())
		{
			bindCalendarAccounts.Sort(delegate(BindCalendarAccountModel a, BindCalendarAccountModel b)
			{
				int accountType = SubscribeCalendarHelper.GetAccountType(a);
				int accountType2 = SubscribeCalendarHelper.GetAccountType(b);
				return (accountType == accountType2 && a.CreatedTime.HasValue) ? a.CreatedTime.Value.CompareTo(b.CreatedTime) : accountType.CompareTo(accountType2);
			});
			list.AddRange(bindCalendarAccounts);
		}
		List<CalendarSubscribeProfileModel> list2 = (from cal in CacheManager.GetSubscribeCalendars()
			where cal.Show != "hidden"
			select cal).ToList();
		if (list2.Any())
		{
			list2.Sort((CalendarSubscribeProfileModel a, CalendarSubscribeProfileModel b) => a.CreatedTime?.CompareTo(b.CreatedTime) ?? (-1));
			list.AddRange(list2);
		}
		foreach (OrderInterface item3 in list.OrderBy((OrderInterface orderInterface) => orderInterface.Order))
		{
			if (!(item3 is BindCalendarAccountModel bindCalendarAccountModel))
			{
				if (item3 is CalendarSubscribeProfileModel calendarSubscribeProfileModel)
				{
					FilterListItemViewModel item = new FilterListItemViewModel
					{
						Title = calendarSubscribeProfileModel.Name,
						Icon = "Ic.ProjectList.Url",
						ShowIcon = true,
						Value = calendarSubscribeProfileModel.Id,
						GroupId = "Calendar5959a2259161d16d23a4f272",
						Kind = "calendar_item",
						IsSecondLevel = true,
						IndentLevel = 0,
						Selected = (allCalendarSelected || CalendarUtils.ContainsCalendarId(_originalCalendarIds, calendarSubscribeProfileModel.Id))
					};
					filterListItemViewModel.Children.Add(item);
				}
				continue;
			}
			List<BindCalendarModel> list3 = BindCalendarAccountDao.GetBindCalendarAccountInstant(bindCalendarAccountModel.Id)?.Calendars;
			if (list3 == null || !list3.Any())
			{
				continue;
			}
			string title = ((bindCalendarAccountModel.Site == "feishu") ? Utils.GetString("FeishuCalendar") : bindCalendarAccountModel.DisplayName);
			FilterListItemViewModel filterListItemViewModel2 = new FilterListItemViewModel
			{
				Title = title,
				Icon = GetCalendarIconKey(bindCalendarAccountModel.Kind, bindCalendarAccountModel.Site),
				ShowIcon = true,
				Value = bindCalendarAccountModel.Id,
				GroupId = "Calendar5959a2259161d16d23a4f272",
				Kind = "calendar_account",
				IsProjectGroup = true,
				Unfold = true,
				IsSecondLevel = true,
				IndentLevel = 0,
				Children = new List<FilterListItemViewModel>()
			};
			foreach (BindCalendarModel item4 in list3)
			{
				FilterListItemViewModel item2 = new FilterListItemViewModel
				{
					Title = item4.Name,
					Icon = GetCalendarIconKey(bindCalendarAccountModel.Kind, bindCalendarAccountModel.Site),
					ShowIcon = true,
					Value = item4.Id,
					GroupId = bindCalendarAccountModel.Id,
					Kind = "calendar_item",
					IsSecondLevel = true,
					IndentLevel = 1,
					Selected = (allCalendarSelected || CalendarUtils.ContainsAccountId(_originalCalendarAccountIds, bindCalendarAccountModel.Id) || CalendarUtils.ContainsAccountId(_originalCalendarIds, bindCalendarAccountModel.Id) || CalendarUtils.ContainsCalendarId(_originalCalendarIds, item4.Id))
				};
				filterListItemViewModel2.Children.Add(item2);
			}
			filterListItemViewModel2.Selected = allCalendarSelected || filterListItemViewModel2.Children.All((FilterListItemViewModel child) => child.Selected);
			filterListItemViewModel2.PartSelected = !filterListItemViewModel2.Selected && filterListItemViewModel2.Children.Any((FilterListItemViewModel child) => child.Selected);
			filterListItemViewModel.Children.Add(filterListItemViewModel2);
		}
		if (!allCalendarSelected)
		{
			AddUnavailableLocalCalendarItem(filterListItemViewModel);
		}
		bool flag = filterListItemViewModel.Children.Any((FilterListItemViewModel filterListItemViewModel3) => filterListItemViewModel3.Kind == "calendar_account" && filterListItemViewModel3.Selected && ShouldPreserveAccountAsCalendar(filterListItemViewModel3));
		List<FilterListItemViewModel> source = filterListItemViewModel.Children.SelectMany((FilterListItemViewModel filterListItemViewModel3) => (!IsCalendarAccountRuleItem(filterListItemViewModel3)) ? new List<FilterListItemViewModel> { filterListItemViewModel3 } : (filterListItemViewModel3.Children ?? new List<FilterListItemViewModel>())).Where(IsCalendarRuleItem).ToList();
		bool flag2 = source.Any(IsUnavailableLocalCalendarItem);
		filterListItemViewModel.Selected = !flag && !flag2 && source.Any() && source.All((FilterListItemViewModel filterListItemViewModel3) => filterListItemViewModel3.Selected);
		filterListItemViewModel.PartSelected = false;
		if (filterListItemViewModel.Unfold)
		{
			int index = itemSource.IndexOf(filterListItemViewModel);
			ConditionEditDialog.InsertVisibleChildren(itemSource, filterListItemViewModel, ref index);
		}
	}

	private void AddUnavailableLocalCalendarItem(FilterListItemViewModel root)
	{
		List<string> visibleCalendarIds = (from item in root.Children.SelectMany((FilterListItemViewModel item) => (!IsCalendarAccountRuleItem(item)) ? new List<FilterListItemViewModel> { item } : (item.Children ?? new List<FilterListItemViewModel>()))
			where item.Kind == "calendar_item"
			select item.Value?.ToString() into id
			where !string.IsNullOrEmpty(id)
			select id).ToList();
		List<string> visibleAccountIds = (from item in root.Children.Where(IsCalendarAccountRuleItem)
			select item.Value?.ToString() into id
			where !string.IsNullOrEmpty(id)
			select id).ToList();
		List<string> list = _originalCalendarAccountIds.Where((string id) => !string.IsNullOrEmpty(id) && !CalendarUtils.ContainsAccountId(visibleAccountIds, id)).Distinct().ToList();
		List<string> list2 = _originalCalendarIds.Where((string id) => id != "#allSubscribe" && !CalendarUtils.ContainsAccountId(visibleAccountIds, id) && !CalendarUtils.ContainsCalendarId(visibleCalendarIds, id)).Distinct().ToList();
		if (list.Any() || list2.Any())
		{
			root.Children.Add(new UnavailableLocalCalendarFilterItemViewModel(list, list2)
			{
				Title = Utils.GetString("LocalCalendars"),
				Icon = "Ic.ProjectList.CalendarSubscribe",
				ShowIcon = true,
				Value = string.Join(",", list2),
				GroupId = "Calendar5959a2259161d16d23a4f272",
				Kind = "calendar_unavailable",
				IsSecondLevel = true,
				IndentLevel = 0,
				Selected = true
			});
		}
	}

	private static string GetCalendarIconKey(string kind, string site)
	{
		switch (kind)
		{
		case "caldav":
			return "Ic.ProjectList.Caldav";
		case "exchange":
			return "Ic.ProjectList.Exchange";
		case "icloud":
			return "Ic.ProjectList.ICloud";
		default:
			if (!(site == "outlook"))
			{
				if (site == "google")
				{
					return "Ic.ProjectList.Google";
				}
				return "Ic.ProjectList.Url";
			}
			return "Ic.ProjectList.Outlook";
		}
	}

	public static string GetDisplayProjectText(List<string> selectedProjectIds, List<string> selectedGroupIds)
	{
		if (selectedProjectIds.Any(CalendarUtils.IsAllListId) && !selectedProjectIds.Any((string id) => !CalendarUtils.IsAllListId(id) && id != "Calendar5959a2259161d16d23a4f272") && selectedGroupIds.Count == 0)
		{
			return Utils.GetString("AllList");
		}
		if (selectedProjectIds.Count == 0 && selectedGroupIds.Count == 0)
		{
			return Utils.GetString("All");
		}
		List<ProjectModel> projects = CacheManager.GetProjects();
		if (projects != null && projects.Count > 0)
		{
			List<ProjectModel> list = (from p in projects
				where selectedProjectIds.Contains(p.id) || selectedGroupIds.Contains(p.groupId)
				where p.IsValid()
				orderby p.sortOrder
				select p).ToList();
			if (selectedProjectIds.Contains("Calendar5959a2259161d16d23a4f272"))
			{
				list.Add(new ProjectModel
				{
					id = "Calendar5959a2259161d16d23a4f272",
					name = Utils.GetString("Calendar")
				});
			}
			if (list.Count > 3)
			{
				string arg = (list.Count - 3).ToString();
				return string.Format(Utils.GetString("ProjectDisplayMessage"), GetDisplayProjectText(list.Take(3).ToList()), arg);
			}
			if (list.Count <= 0)
			{
				return Utils.GetString("Expired");
			}
			return GetDisplayProjectText(list);
		}
		return Utils.GetString("All");
	}

	private static string GetDisplayProjectText(List<ProjectModel> projects)
	{
		if (projects.Count == 0)
		{
			return Utils.GetString("All");
		}
		string text = string.Empty;
		for (int i = 0; i < projects.Count; i++)
		{
			text = ((i >= projects.Count - 1) ? (text + GetProjectName(projects[i])) : (text + GetProjectName(projects[i]) + ", "));
		}
		return text;
	}

	private static string GetProjectName(ProjectModel project)
	{
		if (project != null)
		{
			if (!project.Isinbox)
			{
				return project.name;
			}
			return Utils.GetString("Inbox");
		}
		return string.Empty;
	}
}
