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
using System.Windows.Media.Animation;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Models.Interface;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.Misc;

public class ItemSelection : UserControl, ITabControl, IComponentConnector
{
	private class UnavailableLocalCalendarViewModel : SubscribeCalendarViewModel
	{
		public List<string> AccountIds { get; }

		public List<string> CalendarIds { get; }

		public UnavailableLocalCalendarViewModel(IEnumerable<string> accountIds, IEnumerable<string> calendarIds)
			: base(BuildItemId(accountIds, calendarIds), Utils.GetString("LocalCalendars"))
		{
			AccountIds = accountIds?.Where((string id) => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
			CalendarIds = calendarIds?.Where((string id) => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
		}

		private static string BuildItemId(IEnumerable<string> accountIds, IEnumerable<string> calendarIds)
		{
			IEnumerable<string> values = from id in (calendarIds ?? Enumerable.Empty<string>()).Concat(accountIds ?? Enumerable.Empty<string>())
				where !string.IsNullOrEmpty(id)
				select id;
			return "#unavailableLocalCalendar:" + string.Join(",", values);
		}
	}

	public string AccountId;

	public bool CalendarCreateMode;

	public ProjectExtra OriginalData = new ProjectExtra();

	public bool ShowAll;

	public bool ShowSmartAll;

	public bool ShowCalendars;

	public bool ShowProjects = true;

	public bool ShowFilters;

	public bool ShowSmartProjects;

	public bool ShowTags;

	public bool ShowColumns;

	public string SelectedColumn;

	private bool _tagShowed;

	public bool ShowNoteProject = true;

	public bool ShowSharedProject = true;

	private bool _showIndent;

	private List<SelectableItemViewModel> _data;

	private string _searchText;

	private HashSet<string> _foldIds = new HashSet<string>();

	private SelectableItemViewModel _hoverParent;

	private SelectableItemViewModel _hoverSubParent;

	private Popup _popup;

	private Dictionary<string, List<ColumnModel>> _projectColumnDict = new Dictionary<string, List<ColumnModel>>();

	private readonly PopupLocationInfo _popupTracker = new PopupLocationInfo();

	private readonly PopupLocationInfo _subPopupTracker = new PopupLocationInfo();

	internal Grid Container;

	internal UpDownSelectListView SelectableItems;

	internal EscPopup SubPopup;

	internal UpDownSelectListView SubItems;

	internal EscPopup SubSubPopup;

	internal UpDownSelectListView SubSubItems;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public bool BatchMode
	{
		get
		{
			return SelectableItems.CanBatchSelected;
		}
		set
		{
			SelectableItems.CanBatchSelected = value;
		}
	}

	public bool CanSelectGroup { get; set; }

	public bool OnlyShowPermission { get; set; }

	public bool ShowFilterGroup { get; set; }

	public bool IsCalFilter { get; set; }

	public bool ShowFilterCalendar { get; set; }

	public bool ShowHabitCategory { get; set; }

	public bool ShowAllProjectsCategory { get; set; }

	public bool ShowListGroup { get; set; }

	public bool CanExpandAllList { get; set; } = true;

	public bool CanExpandAllCalendars { get; set; } = true;

	public bool HighlightAllListChildren { get; set; } = true;

	public bool UseCircleCheckForAll { get; set; }

	public bool UseCircleCheckForSectionGroups { get; set; }

	public bool UseCalendarSidebarFilterStyle { get; set; }

	public bool ShowInbox { get; set; } = true;

	public List<string> LinkedProjectIds { get; set; }

	public event EventHandler<SelectableItemViewModel> ItemSelect;

	public ItemSelection(Popup popup, bool defaultHoverMode = true)
	{
		_popup = popup;
		if (_popup != null)
		{
			_popup.Closed += delegate
			{
				SubPopup.IsOpen = false;
			};
		}
		InitializeComponent();
		SubPopup.NeedFocus = false;
		SubSubPopup.NeedFocus = false;
		AttachMouseWheelHandler(SelectableItems);
		AttachMouseWheelHandler(SubItems);
		AttachMouseWheelHandler(SubSubItems);
		SelectableItems.NeedHandleItemEnter = defaultHoverMode;
		DataChangedNotifier.CalendarProjectFilterChanged += DataChangedNotifier_CalendarProjectFilterChanged;
	}

	private void DataChangedNotifier_CalendarProjectFilterChanged(object sender, EventArgs e)
	{
		if (sender == null)
		{
			LoadData();
		}
	}

	public ProjectExtra GetSelectedData()
	{
		if (_data == null)
		{
			return new ProjectExtra();
		}
		return OriginalData = BuildSelectedData(allowAllShortcut: true);
	}

	private ProjectExtra BuildSelectedData(bool allowAllShortcut)
	{
		ProjectExtra projectExtra = new ProjectExtra
		{
			FilterIds = GetSelectedFilterIds()
		};
		if (allowAllShortcut && ShowAll)
		{
			AllProjectViewModel allProjectViewModel = _data.OfType<AllProjectViewModel>().FirstOrDefault();
			if (allProjectViewModel != null && allProjectViewModel.Selected && !HasSelectedFilterCondition())
			{
				return projectExtra;
			}
		}
		CalendarGroupViewModel calendarGroupViewModel = _data.OfType<CalendarGroupViewModel>()?.FirstOrDefault();
		if (calendarGroupViewModel != null && calendarGroupViewModel.Selected)
		{
			projectExtra.SubscribeCalendars.Add("#allSubscribe");
		}
		else
		{
			CollectSelectedCalendars(calendarGroupViewModel, projectExtra);
		}
		ListGroupViewModel listGroupViewModel = _data.OfType<ListGroupViewModel>()?.FirstOrDefault();
		if (listGroupViewModel != null && listGroupViewModel.Selected)
		{
			projectExtra.ProjectIds.Add("#alllists");
			FilterHabitViewModel filterHabitViewModel = _data.OfType<FilterHabitViewModel>().FirstOrDefault();
			if (filterHabitViewModel != null && filterHabitViewModel.Selected)
			{
				projectExtra.ProjectIds.Add("Habit2e4c103c57ef480997943206");
			}
		}
		else
		{
			HashSet<SelectableItemViewModel> allListOrGroup = GetAllListOrGroup(listGroupViewModel, withTeam: false);
			projectExtra.ProjectIds = (from p in allListOrGroup
				where (p is ProjectViewModel || p is FilterCalendarViewModel || p is FilterHabitViewModel || p is AllProjectCategoryViewModel) && p.Selected
				select p.Id).ToList();
			projectExtra.GroupIds = (from p in allListOrGroup
				where p is ProjectGroupViewModel && p.Selected
				select p.Id).ToList();
		}
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel m) => m is TagGroupViewModel);
		if (selectableItemViewModel != null)
		{
			if (selectableItemViewModel.Selected)
			{
				projectExtra.Tags.Add("*withtags");
			}
			else
			{
				List<SelectableItemViewModel> list = new List<SelectableItemViewModel>();
				foreach (SelectableItemViewModel child in selectableItemViewModel.Children)
				{
					list.Add(child);
					child.Children.ForEach(list.Add);
				}
				projectExtra.Tags = (from p in list
					where p.Selected
					select p.Id).ToList();
			}
		}
		else
		{
			projectExtra.Tags = (from p in _data
				where p is TagViewModel && p.Selected
				select p.Id).ToList();
		}
		if (projectExtra.IsAll)
		{
			SelectableItemViewModel selectableItemViewModel2 = _data.FirstOrDefault((SelectableItemViewModel item) => item is AllProjectViewModel);
			if (selectableItemViewModel2 != null)
			{
				selectableItemViewModel2.Selected = true;
			}
		}
		return projectExtra;
	}

	private List<string> GetSelectedFilterIds()
	{
		IEnumerable<FilterItemViewModel> first = _data?.OfType<FilterItemViewModel>() ?? Enumerable.Empty<FilterItemViewModel>();
		IEnumerable<FilterItemViewModel> second = _data?.OfType<FilterGroupViewModel>().SelectMany((FilterGroupViewModel group) => group.Children?.OfType<FilterItemViewModel>() ?? Enumerable.Empty<FilterItemViewModel>()) ?? Enumerable.Empty<FilterItemViewModel>();
		return (from filter in first.Concat(second)
			where filter.Selected
			select filter.Id).Distinct().ToList();
	}

	private HashSet<SelectableItemViewModel> GetAllListOrGroup(ListGroupViewModel listGroupItem, bool withTeam)
	{
		HashSet<SelectableItemViewModel> listAndGroupData = new HashSet<SelectableItemViewModel>();
		List<SelectableItemViewModel> second = _data.Where((SelectableItemViewModel item) => item is FilterCalendarViewModel || item is AllProjectCategoryViewModel || item is FilterHabitViewModel).ToList();
		foreach (SelectableItemViewModel item in (listGroupItem != null) ? listGroupItem.Children.Concat(second).ToList() : _data.Where((SelectableItemViewModel item) => item.GroupParent is ListGroupViewModel || item is FilterCalendarViewModel || item is AllProjectCategoryViewModel || item is FilterHabitViewModel).ToList())
		{
			if (listAndGroupData.Contains(item))
			{
				continue;
			}
			if (item is TeamSectionViewModel teamSectionViewModel)
			{
				foreach (SelectableItemViewModel child in teamSectionViewModel.Children)
				{
					listAndGroupData.Add(child);
					child.Children.ForEach(delegate(SelectableItemViewModel model)
					{
						listAndGroupData.Add(model);
					});
				}
			}
			else
			{
				listAndGroupData.Add(item);
				item.Children.ForEach(delegate(SelectableItemViewModel model)
				{
					listAndGroupData.Add(model);
				});
			}
		}
		return listAndGroupData;
	}

	private void CollectSelectedCalendars(CalendarGroupViewModel calGroup, ProjectExtra result)
	{
		if (calGroup == null)
		{
			HashSet<string> flatBindAccounts = new HashSet<string>();
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<string> preservedCalendarAccounts = new HashSet<string>();
			List<SubscribeCalendarViewModel> list = (from calendar in _data.OfType<SubscribeCalendarViewModel>()
				where calendar.AccountId == AccountId
				select calendar).ToList();
			if (!string.IsNullOrEmpty(AccountId) && list.Count > 0 && list.All((SubscribeCalendarViewModel calendar) => calendar.Selected) && ShouldPreserveAccountAsCalendar(AccountId))
			{
				preservedCalendarAccounts.Add(AccountId);
				AddOriginalAccountCalendarRuleIds(AccountId, list, hashSet);
			}
			foreach (BindAccountViewModel item in from account in _data.OfType<BindAccountViewModel>()
				where account.Selected && AreAllAccountCalendarsSelected(account)
				select account)
			{
				if (ShouldPreserveAccountAsCalendar(item))
				{
					preservedCalendarAccounts.Add(item.Id);
					AddOriginalAccountCalendarRuleIds(item, hashSet);
					continue;
				}
				foreach (string bindAccountRuleId in GetBindAccountRuleIds(item.Id))
				{
					flatBindAccounts.Add(bindAccountRuleId);
				}
			}
			foreach (UnavailableLocalCalendarViewModel item2 in from calendar in _data.OfType<UnavailableLocalCalendarViewModel>()
				where calendar.Selected
				select calendar)
			{
				AddUnavailableLocalCalendarRuleIds(item2, flatBindAccounts, hashSet);
			}
			foreach (SubscribeCalendarViewModel item3 in from calendar in _data.OfType<SubscribeCalendarViewModel>()
				where !(calendar is UnavailableLocalCalendarViewModel)
				where calendar.Selected && !CalendarUtils.ContainsAccountId(flatBindAccounts, calendar.AccountId) && !CalendarUtils.ContainsAccountId(preservedCalendarAccounts, calendar.AccountId)
				select calendar)
			{
				foreach (string calendarRuleId in GetCalendarRuleIds(item3))
				{
					hashSet.Add(calendarRuleId);
				}
			}
			result.BindAccounts = flatBindAccounts.ToList();
			result.SubscribeCalendars = hashSet.ToList();
			PreserveUnavailableBindAccounts(result, _data);
			return;
		}
		HashSet<string> hashSet2 = new HashSet<string>();
		HashSet<string> hashSet3 = new HashSet<string>();
		foreach (SelectableItemViewModel child in calGroup.Children)
		{
			SubscribeCalendarViewModel subscribeCalendarViewModel;
			if (!(child is UnavailableLocalCalendarViewModel unavailableLocalCalendarViewModel))
			{
				if (child is BindAccountViewModel bindAccountViewModel)
				{
					if (bindAccountViewModel.Selected && AreAllAccountCalendarsSelected(bindAccountViewModel))
					{
						if (ShouldPreserveAccountAsCalendar(bindAccountViewModel))
						{
							AddOriginalAccountCalendarRuleIds(bindAccountViewModel, hashSet3);
							continue;
						}
						foreach (string bindAccountRuleId2 in GetBindAccountRuleIds(bindAccountViewModel.Id))
						{
							hashSet2.Add(bindAccountRuleId2);
						}
						continue;
					}
					foreach (SubscribeCalendarViewModel item4 in from calendar in (bindAccountViewModel.Children ?? new List<SelectableItemViewModel>()).OfType<SubscribeCalendarViewModel>()
						where calendar.Selected
						select calendar)
					{
						foreach (string calendarRuleId2 in GetCalendarRuleIds(item4))
						{
							hashSet3.Add(calendarRuleId2);
						}
					}
					continue;
				}
				subscribeCalendarViewModel = child as SubscribeCalendarViewModel;
				if (subscribeCalendarViewModel == null)
				{
					continue;
				}
			}
			else
			{
				if (unavailableLocalCalendarViewModel.Selected)
				{
					AddUnavailableLocalCalendarRuleIds(unavailableLocalCalendarViewModel, hashSet2, hashSet3);
					continue;
				}
				subscribeCalendarViewModel = (SubscribeCalendarViewModel)child;
			}
			if (!subscribeCalendarViewModel.Selected)
			{
				continue;
			}
			foreach (string calendarRuleId3 in GetCalendarRuleIds(subscribeCalendarViewModel))
			{
				hashSet3.Add(calendarRuleId3);
			}
		}
		result.BindAccounts = hashSet2.ToList();
		result.SubscribeCalendars = hashSet3.ToList();
		PreserveUnavailableBindAccounts(result, calGroup.Children);
	}

	private void PreserveUnavailableBindAccounts(ProjectExtra result, IEnumerable<SelectableItemViewModel> visibleItems)
	{
		if (result == null || IsAllCalendarSelectedInOriginalData())
		{
			return;
		}
		List<string> visibleAccountIds = (from id in (visibleItems ?? Enumerable.Empty<SelectableItemViewModel>()).SelectMany(GetVisibleAccountRuleIds)
			where !string.IsNullOrEmpty(id)
			select id).ToList() ?? new List<string>();
		foreach (string originalId in OriginalData?.BindAccounts?.Where((string id) => !string.IsNullOrEmpty(id) && !CalendarUtils.ContainsAccountId(visibleAccountIds, id)) ?? Enumerable.Empty<string>())
		{
			if (!result.BindAccounts.Any((string id) => CalendarUtils.IsSameAccountId(id, originalId)))
			{
				result.BindAccounts.Add(originalId);
			}
		}
	}

	private IEnumerable<string> GetVisibleAccountRuleIds(SelectableItemViewModel item)
	{
		if (!(item is UnavailableLocalCalendarViewModel unavailableLocalCalendarViewModel))
		{
			if (item is BindAccountViewModel bindAccountViewModel)
			{
				return SplitRuleIds(bindAccountViewModel.Id);
			}
			return Enumerable.Empty<string>();
		}
		return unavailableLocalCalendarViewModel.AccountIds;
	}

	private void AddUnavailableLocalCalendarRuleIds(UnavailableLocalCalendarViewModel localCalendar, ISet<string> bindAccounts, ISet<string> calendars)
	{
		if (localCalendar == null)
		{
			return;
		}
		foreach (string accountId in localCalendar.AccountIds)
		{
			foreach (string bindAccountRuleId in GetBindAccountRuleIds(accountId))
			{
				bindAccounts.Add(bindAccountRuleId);
			}
		}
		foreach (string calendarId in localCalendar.CalendarIds)
		{
			calendars.Add(calendarId);
		}
	}

	private IEnumerable<string> GetCalendarRuleIds(SubscribeCalendarViewModel calendar)
	{
		List<string> list = calendar.Id.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
		foreach (string id in list)
		{
			List<string> list2 = OriginalData?.SubscribeCalendars?.Where((string originalId) => originalId != "#allSubscribe" && CalendarUtils.IsSameCalendarId(originalId, id)).ToList();
			if (list2 != null && list2.Any())
			{
				foreach (string item in list2)
				{
					yield return item;
				}
			}
			else
			{
				yield return id;
			}
		}
	}

	private IEnumerable<string> GetOriginalCalendarRuleIds(SubscribeCalendarViewModel calendar)
	{
		List<string> list = calendar.Id.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
		foreach (string id in list)
		{
			foreach (string item in OriginalData?.SubscribeCalendars?.Where((string originalId) => originalId != "#allSubscribe" && CalendarUtils.IsSameCalendarId(originalId, id)) ?? Enumerable.Empty<string>())
			{
				yield return item;
			}
		}
	}

	private bool AreAllListsSelected(ListGroupViewModel listGroup)
	{
		List<SelectableItemViewModel> list = (from item in GetAllListOrGroup(listGroup, withTeam: false)
			where item is ProjectGroupViewModel || item is ProjectViewModel
			select item).ToList();
		if (list.Count > 0)
		{
			return list.All((SelectableItemViewModel item) => item.Selected);
		}
		return false;
	}

	private bool IsListScopeAllSelectedOrEmpty(ListGroupViewModel listGroup)
	{
		List<SelectableItemViewModel> list = (from item in GetAllListOrGroup(listGroup, withTeam: false)
			where item is ProjectGroupViewModel || item is ProjectViewModel
			select item).ToList();
		if (list.Count != 0 && list.Any((SelectableItemViewModel item) => item.Selected || item.PartSelected))
		{
			return list.All((SelectableItemViewModel item) => item.Selected);
		}
		return true;
	}

	private bool AreAllCalendarsSelected(CalendarGroupViewModel calGroup)
	{
		List<SelectableItemViewModel> source = (from item in calGroup.Children.SelectMany((SelectableItemViewModel item) => (!(item is BindAccountViewModel bindAccountViewModel)) ? new List<SelectableItemViewModel> { item } : bindAccountViewModel.Children)
			where item is SubscribeCalendarViewModel
			select item).ToList();
		if (source.Any(IsUnavailableLocalCalendarItem))
		{
			return false;
		}
		if (calGroup.Children.OfType<BindAccountViewModel>().Any((BindAccountViewModel account) => account.Selected && ShouldPreserveAccountAsCalendar(account)))
		{
			return false;
		}
		source = source.Where((SelectableItemViewModel item) => !IsUnavailableLocalCalendarItem(item)).ToList();
		if (source.Count > 0)
		{
			return source.All((SelectableItemViewModel item) => item.Selected);
		}
		return false;
	}

	private bool ShouldPreserveAccountAsCalendar(SelectableItemViewModel account)
	{
		return ShouldPreserveAccountAsCalendar(account?.Id);
	}

	private bool ShouldPreserveAccountAsCalendar(string accountId)
	{
		ProjectExtra originalData = OriginalData;
		if (originalData == null)
		{
			return false;
		}
		return originalData.SubscribeCalendars?.Any((string id) => id != "#allSubscribe" && CalendarUtils.IsSameAccountId(id, accountId)) == true;
	}

	private void AddOriginalAccountCalendarRuleIds(BindAccountViewModel account, ISet<string> calendars)
	{
		if (account != null && calendars != null)
		{
			AddOriginalAccountCalendarRuleIds(account.Id, (account.Children ?? new List<SelectableItemViewModel>()).OfType<SubscribeCalendarViewModel>(), calendars);
		}
	}

	private void AddOriginalAccountCalendarRuleIds(string accountId, IEnumerable<SubscribeCalendarViewModel> accountCalendars, ISet<string> calendars)
	{
		if (string.IsNullOrEmpty(accountId) || calendars == null)
		{
			return;
		}
		foreach (string item in OriginalData?.SubscribeCalendars?.Where((string id) => id != "#allSubscribe" && CalendarUtils.IsSameAccountId(id, accountId)) ?? Enumerable.Empty<string>())
		{
			calendars.Add(item);
		}
		foreach (SubscribeCalendarViewModel item2 in accountCalendars ?? Enumerable.Empty<SubscribeCalendarViewModel>())
		{
			foreach (string originalCalendarRuleId in GetOriginalCalendarRuleIds(item2))
			{
				calendars.Add(originalCalendarRuleId);
			}
		}
	}

	private IEnumerable<string> GetBindAccountRuleIds(string accountId)
	{
		foreach (string id in SplitRuleIds(accountId))
		{
			List<string> list = OriginalData?.BindAccounts?.Where((string originalId) => CalendarUtils.IsSameAccountId(originalId, id)).ToList();
			if (list != null && list.Any())
			{
				foreach (string item in list)
				{
					yield return item;
				}
			}
			else
			{
				yield return id;
			}
		}
	}

	private static IEnumerable<string> SplitRuleIds(string ids)
	{
		if (!string.IsNullOrEmpty(ids))
		{
			return ids.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}
		return Enumerable.Empty<string>();
	}

	private static bool IsUnavailableLocalCalendarItem(SelectableItemViewModel item)
	{
		if (item is SubscribeCalendarViewModel)
		{
			return item.ToolTip == Utils.GetString("CalendarFromAnotherDevice");
		}
		return false;
	}

	private bool AreAllAccountCalendarsSelected(BindAccountViewModel account)
	{
		List<SelectableItemViewModel> children = account.Children;
		if (children != null && children.Count > 0)
		{
			return account.Children.All((SelectableItemViewModel item) => item.Selected);
		}
		return false;
	}

	public void LoadData(string searchText = null)
	{
		_searchText = searchText;
		List<SelectableItemViewModel> list = new List<SelectableItemViewModel>();
		if (CalendarCreateMode && string.IsNullOrEmpty(AccountId))
		{
			LoadCreateCalendarAccounts(list);
		}
		else if (!string.IsNullOrEmpty(AccountId))
		{
			LoadAccountCalendars(list, AccountId);
		}
		else
		{
			if (ShowAll)
			{
				AllProjectViewModel allProjectViewModel = new AllProjectViewModel
				{
					Selected = (OriginalData != null && IsAllContentSelected()),
					InCalFilter = IsCalFilter,
					UseCircleCheck = UseCircleCheckForAll
				};
				ApplyCalendarSidebarFilterStyle(allProjectViewModel);
				list.Add(allProjectViewModel);
				if (!IsCalFilter)
				{
					list.Add(new SelectableItemViewModel
					{
						IsSplit = true,
						IsEnable = false
					});
				}
			}
			if (ShowAllProjectsCategory)
			{
				list.Add(new AllProjectCategoryViewModel
				{
					Selected = (OriginalData != null && CalendarUtils.ContainsAllListId(OriginalData.ProjectIds))
				});
				list.Add(new SelectableItemViewModel
				{
					IsSplit = true,
					IsEnable = false
				});
			}
			if (ShowSmartAll && string.IsNullOrEmpty(searchText))
			{
				bool linked = LinkedProjectIds?.Contains("all") ?? false;
				List<string> linkedProjectIds = LinkedProjectIds;
				bool flag = linkedProjectIds == null || linkedProjectIds.Count <= 0;
				list.Add(new AllSmartProjectViewModel
				{
					Linked = linked,
					Selectable = flag,
					IsGray = !flag,
					Selected = (OriginalData != null && OriginalData.SmartIds.Contains("_special_id_all"))
				});
			}
			if (ShowSmartProjects)
			{
				list.Add(new TodaySmartProjectViewModel
				{
					Selected = (OriginalData != null && OriginalData.SmartIds.Contains("_special_id_today"))
				});
				list.Add(new TomorrowSmartProjectViewModel
				{
					Selected = (OriginalData != null && OriginalData.SmartIds.Contains("_special_id_tomorrow"))
				});
				list.Add(new WeekSmartProjectViewModel
				{
					Selected = (OriginalData != null && OriginalData.SmartIds.Contains("_special_id_week"))
				});
				list.Add(new AssignToMeSmartProjectViewModel
				{
					Selected = (OriginalData != null && OriginalData.SmartIds.Contains("_special_id_assigned"))
				});
			}
			using (List<PtfType>.Enumerator enumerator = ProjectDataProvider.SortedPtfTypes.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					switch (enumerator.Current)
					{
					case PtfType.Project:
					{
						if (ShowProjects)
						{
							LoadProjects(list, searchText);
						}
						if (!ShowProjects || !ShowFilterCalendar)
						{
							break;
						}
						bool flag2 = OriginalData.ProjectIds.Contains("Calendar5959a2259161d16d23a4f272");
						if (!flag2)
						{
							flag2 = CacheManager.GetSubscribeCalendars().Any((CalendarSubscribeProfileModel cal) => cal.Show != "hidden");
						}
						if (!flag2)
						{
							flag2 = CacheManager.GetBindCalendars().Any((BindCalendarModel cal) => cal.Show != "hidden");
						}
						if (flag2)
						{
							list.Add(new SelectableItemViewModel
							{
								IsSplit = true,
								IsEnable = false
							});
							list.Add(new FilterCalendarViewModel
							{
								Selected = OriginalData.ProjectIds.Contains("Calendar5959a2259161d16d23a4f272")
							});
						}
						break;
					}
					case PtfType.Tag:
						if (ShowTags)
						{
							LoadTags(list);
						}
						break;
					case PtfType.Filter:
						if (ShowFilters)
						{
							LoadFilters(list, BatchMode);
						}
						break;
					case PtfType.Subscribe:
						if (ShowCalendars)
						{
							LoadCalendars(list);
						}
						break;
					}
				}
			}
			if (ShowProjects && ShowHabitCategory)
			{
				list.Add(new SelectableItemViewModel
				{
					IsSplit = true,
					IsEnable = false
				});
				list.Add(new FilterHabitViewModel
				{
					Selected = (OriginalData?.ProjectIds.Contains("Habit2e4c103c57ef480997943206") ?? false)
				});
			}
		}
		list.ToList().ForEach(delegate(SelectableItemViewModel model)
		{
			model.BatchMode = BatchMode;
		});
		_data = list;
		TagGroupViewModel tagGroupViewModel = list.OfType<TagGroupViewModel>().FirstOrDefault();
		if (tagGroupViewModel != null && _foldIds.Contains(tagGroupViewModel.Id))
		{
			tagGroupViewModel.Open = false;
			OnTagGroupChanged(tagGroupViewModel, setData: false);
		}
		ListGroupViewModel listGroupViewModel = list.OfType<ListGroupViewModel>().FirstOrDefault();
		if (listGroupViewModel != null && CanExpandAllList && _foldIds.Contains(listGroupViewModel.Id))
		{
			listGroupViewModel.Open = false;
			OnListGroupChanged(listGroupViewModel, setData: false);
		}
		FilterGroupViewModel filterGroupViewModel = list.OfType<FilterGroupViewModel>().FirstOrDefault();
		if (filterGroupViewModel != null && _foldIds.Contains(filterGroupViewModel.Id))
		{
			filterGroupViewModel.Open = false;
			OnFilterGroupChanged(filterGroupViewModel, setData: false);
		}
		CalendarGroupViewModel calendarGroupViewModel = list.OfType<CalendarGroupViewModel>().FirstOrDefault();
		if (calendarGroupViewModel != null && CanExpandAllCalendars && (_foldIds.Contains(calendarGroupViewModel.Id) || !calendarGroupViewModel.Open))
		{
			calendarGroupViewModel.Open = false;
			OnCalendarGroupChanged(calendarGroupViewModel, setData: false);
		}
		SetModels(_data);
	}

	public bool HasItems()
	{
		List<SelectableItemViewModel> data = _data;
		if (data == null)
		{
			return false;
		}
		return data.Count > 0;
	}

	private void ApplyCalendarSidebarFilterStyle(SelectableItemViewModel model)
	{
		if (!UseCalendarSidebarFilterStyle || model == null)
		{
			return;
		}
		if (!(model is AllProjectViewModel))
		{
			if (!(model is ListGroupViewModel))
			{
				if (!(model is TagGroupViewModel))
				{
					if (model is CalendarGroupViewModel)
					{
						model.Title = Utils.GetString("SubscribeCalendar");
						model.Selectable = false;
						model.ShowIcon = false;
						model.ShowSelected = false;
						model.IsBold = true;
						model.UseCircleCheck = false;
					}
				}
				else
				{
					model.ShowIcon = false;
					model.IsBold = true;
					model.UseCircleCheck = false;
				}
			}
			else
			{
				model.Title = Utils.GetString("lists");
				model.ShowIcon = false;
				model.IsBold = true;
				model.UseCircleCheck = false;
			}
		}
		else
		{
			model.ShowIcon = false;
			model.IsBold = true;
		}
	}

	private bool IsAllContentSelected()
	{
		ProjectExtra originalData = OriginalData;
		if (originalData == null || originalData.Tags?.Any() != true)
		{
			ProjectExtra originalData2 = OriginalData;
			if (originalData2 == null || originalData2.FilterIds?.Any() != true)
			{
				if (OriginalData == null || OriginalData.IsAll)
				{
					return true;
				}
				bool num = !ShowProjects || IsAllListSelectedInOriginalData();
				bool flag = !ShowCalendars || IsAllCalendarSelectedInOriginalData();
				bool flag2 = !ShowHabitCategory || (OriginalData.ProjectIds?.Contains("Habit2e4c103c57ef480997943206") ?? false);
				return num && flag && flag2;
			}
		}
		return false;
	}

	private bool IsAllListSelectedInOriginalData()
	{
		List<string> list = OriginalData?.ProjectIds;
		if (list != null && list.Any(CalendarUtils.IsAllListId) && (list == null || !list.Any((string id) => !CalendarUtils.IsAllListId(id) && id != "Habit2e4c103c57ef480997943206" && id != "Calendar5959a2259161d16d23a4f272")))
		{
			return OriginalData?.GroupIds?.Any() != true;
		}
		return false;
	}

	private bool IsAllCalendarSelectedInOriginalData()
	{
		if (OriginalData == null)
		{
			return false;
		}
		bool num = OriginalData.SubscribeCalendars?.Contains("#allSubscribe") ?? false;
		bool flag = OriginalData.ProjectIds?.Contains("Calendar5959a2259161d16d23a4f272") ?? false;
		List<string> bindAccounts = OriginalData.BindAccounts;
		bool flag2 = (bindAccounts != null && bindAccounts.Any()) || (OriginalData.SubscribeCalendars?.Any((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe") ?? false);
		if (!num)
		{
			if (flag)
			{
				return !flag2;
			}
			return false;
		}
		return true;
	}

	private bool HasSelectedFilterCondition()
	{
		List<SelectableItemViewModel> data = _data;
		if (data == null || !data.Where((SelectableItemViewModel item) => item is TagGroupViewModel || item is FilterGroupViewModel).Any(HasSelectedItemOrChild))
		{
			List<SelectableItemViewModel> data2 = _data;
			if (data2 == null || !data2.OfType<TagViewModel>().Any((TagViewModel tag) => tag.Selected))
			{
				return _data?.OfType<FilterItemViewModel>().Any((FilterItemViewModel filter) => filter.Selected) ?? false;
			}
		}
		return true;
	}

	private static bool HasSelectedItemOrChild(SelectableItemViewModel item)
	{
		if ((item == null || !item.Selected) && (item == null || !item.PartSelected))
		{
			if (item == null)
			{
				return false;
			}
			return item.Children?.Any(HasSelectedItemOrChild) == true;
		}
		return true;
	}

	private void RefreshAllProjectSelected()
	{
		SelectableItemViewModel selectableItemViewModel = _data?.FirstOrDefault((SelectableItemViewModel item) => item is AllProjectViewModel);
		if (selectableItemViewModel != null)
		{
			ListGroupViewModel listGroup = _data.OfType<ListGroupViewModel>().FirstOrDefault();
			CalendarGroupViewModel calendarGroupViewModel = _data.OfType<CalendarGroupViewModel>().FirstOrDefault();
			SelectableItemViewModel selectableItemViewModel2 = _data.FirstOrDefault((SelectableItemViewModel item) => item is FilterHabitViewModel);
			bool flag = !ShowProjects || IsListScopeAllSelectedOrEmpty(listGroup);
			bool flag2 = !ShowCalendars || (calendarGroupViewModel?.Selected ?? false);
			bool flag3 = selectableItemViewModel2?.Selected ?? true;
			selectableItemViewModel.Selected = flag && flag2 && flag3 && !HasSelectedFilterCondition();
		}
	}

	internal void OnSectionGroupSelected(SelectableItemViewModel model)
	{
		OriginalData = BuildSelectedData(allowAllShortcut: false);
		if (!(model is ListGroupViewModel listGroupViewModel))
		{
			if (!(model is TagGroupViewModel tagGroupViewModel))
			{
				if (model is CalendarGroupViewModel calendarGroupViewModel)
				{
					OriginalData.SubscribeCalendars.Clear();
					OriginalData.BindAccounts.Clear();
					OriginalData.FilterIds.Clear();
					if (calendarGroupViewModel.Selected)
					{
						OriginalData.SubscribeCalendars.Add(calendarGroupViewModel.Id);
					}
				}
			}
			else
			{
				OriginalData.Tags.Clear();
				OriginalData.FilterIds.Clear();
				if (tagGroupViewModel.Selected)
				{
					OriginalData.Tags.Add("*withtags");
				}
			}
		}
		else
		{
			bool flag = OriginalData.ProjectIds.Contains("Habit2e4c103c57ef480997943206");
			OriginalData.ProjectIds.Clear();
			OriginalData.GroupIds.Clear();
			OriginalData.FilterIds.Clear();
			if (listGroupViewModel.Selected)
			{
				OriginalData.ProjectIds.Add(listGroupViewModel.Id);
				if (flag)
				{
					OriginalData.ProjectIds.Add("Habit2e4c103c57ef480997943206");
				}
			}
		}
		LoadData();
		RefreshAllProjectSelected();
		this.ItemSelect?.Invoke(this, model);
	}

	private void SetModels(List<SelectableItemViewModel> models)
	{
		ItemsSourceHelper.SetItemsSource(SelectableItems, models);
	}

	private void LoadAccountCalendars(ICollection<SelectableItemViewModel> models, string accountId)
	{
		BindCalendarAccountModel account = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel acc) => acc.Id == accountId);
		if (account == null)
		{
			return;
		}
		List<BindCalendarModel> list = (from cal in CacheManager.GetBindCalendars()
			where cal.AccountId == account.Id && cal.Accessible
			select cal).ToList();
		Geometry icon = SubscribeCalendarHelper.GetCalendarProjectIconById(accountId);
		if (!list.Any())
		{
			return;
		}
		list.Sort((BindCalendarModel a, BindCalendarModel b) => a.sortOrder.CompareTo(b.sortOrder));
		foreach (SubscribeCalendarViewModel item in from cal in list
			where cal.Accessible
			select new SubscribeCalendarViewModel(cal.Id, cal.Name, account.Id)
			{
				Selected = (CalendarUtils.ContainsCalendarId(OriginalData.SubscribeCalendars, cal.Id) || CalendarUtils.ContainsAccountId(OriginalData.SubscribeCalendars, account.Id) || CalendarUtils.ContainsAccountId(OriginalData.BindAccounts, account.Id)),
				ShowIndent = _showIndent,
				Icon = icon,
				InCalFilter = IsCalFilter
			})
		{
			models.Add(item);
		}
	}

	private void LoadCreateCalendarAccounts(ICollection<SelectableItemViewModel> models)
	{
		List<BindCalendarAccountModel> source = (from bindCalendarAccountModel in CacheManager.GetBindCalendarAccounts()
			where bindCalendarAccountModel != null && !bindCalendarAccountModel.Expired
			select bindCalendarAccountModel).ToList();
		List<BindCalendarModel> bindCalendars = (from calendar in CacheManager.GetBindCalendars()
			where calendar?.Accessible ?? false
			select calendar).ToList();
		List<BindCalendarAccountModel> list = source.Where((BindCalendarAccountModel bindCalendarAccountModel) => bindCalendars.Any((BindCalendarModel calendar) => calendar.AccountId == bindCalendarAccountModel.Id)).ToList();
		list.Sort(delegate(BindCalendarAccountModel a, BindCalendarAccountModel b)
		{
			int accountType = SubscribeCalendarHelper.GetAccountType(a);
			int accountType2 = SubscribeCalendarHelper.GetAccountType(b);
			return (accountType == accountType2 && a.CreatedTime.HasValue) ? a.CreatedTime.Value.CompareTo(b.CreatedTime) : accountType.CompareTo(accountType2);
		});
		foreach (BindCalendarAccountModel account in list)
		{
			string title = account.DisplayName;
			if (account.Site == "feishu")
			{
				title = Utils.GetString("FeishuCalendar");
			}
			BindAccountViewModel bindAccountViewModel = new BindAccountViewModel(account.Id, title, account.Kind)
			{
				Selectable = false,
				ShowSubOnSide = true
			};
			List<SelectableItemViewModel> source2 = (bindAccountViewModel.Children = ((IEnumerable<SelectableItemViewModel>)(from calendar in bindCalendars
				where calendar.AccountId == account.Id
				orderby calendar.sortOrder
				select new SubscribeCalendarViewModel(calendar.Id, calendar.Name, account.Id)
				{
					Selected = OriginalData.SubscribeCalendars.Contains(calendar.Id)
				})).ToList());
			bindAccountViewModel.PartSelected = source2.Any((SelectableItemViewModel calendar) => calendar.Selected);
			models.Add(bindAccountViewModel);
		}
	}

	private void LoadCalendars(ICollection<SelectableItemViewModel> models)
	{
		bool allSelect = IsAllCalendarSelectedInOriginalData();
		CalendarGroupViewModel calendarGroupViewModel = new CalendarGroupViewModel
		{
			InCalFilter = IsCalFilter,
			Open = (!CanExpandAllCalendars || !_foldIds.Contains("#allSubscribe")),
			Selected = allSelect,
			PartSelected = false,
			UseCircleCheck = UseCircleCheckForSectionGroups,
			CanExpand = CanExpandAllCalendars
		};
		ApplyCalendarSidebarFilterStyle(calendarGroupViewModel);
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
		if (list.Any())
		{
			models.Add(calendarGroupViewModel);
		}
		list = list.OrderBy((OrderInterface orderInterface) => orderInterface.Order).ToList();
		foreach (OrderInterface item3 in list)
		{
			if (!(item3 is CalendarSubscribeProfileModel calendarSubscribeProfileModel))
			{
				BindCalendarAccountModel bindCalendarAccountModel = item3 as BindCalendarAccountModel;
				if (bindCalendarAccountModel == null)
				{
					continue;
				}
				List<BindCalendarModel> list3 = BindCalendarAccountDao.GetBindCalendarAccountInstant(bindCalendarAccountModel.Id)?.Calendars;
				if (list3 == null || !list3.Any())
				{
					continue;
				}
				string title = bindCalendarAccountModel.DisplayName;
				if (bindCalendarAccountModel.Site == "feishu")
				{
					title = Utils.GetString("FeishuCalendar");
				}
				List<SubscribeCalendarViewModel> list4 = list3.Select((BindCalendarModel cal) => new SubscribeCalendarViewModel(cal.Id, cal.Name, bindCalendarAccountModel.Id)
				{
					Selected = (allSelect || CalendarUtils.ContainsCalendarId(OriginalData.SubscribeCalendars, cal.Id) || CalendarUtils.ContainsAccountId(OriginalData.SubscribeCalendars, bindCalendarAccountModel.Id) || CalendarUtils.ContainsAccountId(OriginalData.BindAccounts, bindCalendarAccountModel.Id)),
					ShowIndent = true,
					IsSubItem = true,
					InCalFilter = IsCalFilter
				}).ToList();
				BindAccountViewModel bindAccountViewModel = new BindAccountViewModel(bindCalendarAccountModel.Id, title, bindCalendarAccountModel.Kind)
				{
					Selected = (list4.Count > 0 && list4.All((SubscribeCalendarViewModel cal) => cal.Selected)),
					PartSelected = (list4.Any((SubscribeCalendarViewModel cal) => cal.Selected) && list4.Any((SubscribeCalendarViewModel cal) => !cal.Selected)),
					ShowIndent = true,
					InCalFilter = IsCalFilter,
					IsParent = true,
					Open = true
				};
				calendarGroupViewModel.Children.Add(bindAccountViewModel);
				models.Add(bindAccountViewModel);
				bindAccountViewModel.Children = new List<SelectableItemViewModel>();
				foreach (SubscribeCalendarViewModel item4 in list4)
				{
					bindAccountViewModel.Children.Add(item4);
					models.Add(item4);
				}
			}
			else
			{
				SubscribeCalendarViewModel item = new SubscribeCalendarViewModel(calendarSubscribeProfileModel.Id, calendarSubscribeProfileModel.Name)
				{
					Selected = (allSelect || CalendarUtils.ContainsCalendarId(OriginalData.SubscribeCalendars, calendarSubscribeProfileModel.Id)),
					ShowIndent = _showIndent,
					InCalFilter = IsCalFilter
				};
				calendarGroupViewModel.Children.Add(item);
				models.Add(item);
			}
		}
		if (!allSelect)
		{
			HashSet<string> visibleCalendarIds = new HashSet<string>(from cal in calendarGroupViewModel.Children.SelectMany((SelectableItemViewModel selectableItemViewModel) => (!(selectableItemViewModel is BindAccountViewModel bindAccountViewModel2)) ? new List<SelectableItemViewModel> { selectableItemViewModel } : bindAccountViewModel2.Children).OfType<SubscribeCalendarViewModel>()
				select cal.Id);
			HashSet<string> visibleAccountIds = new HashSet<string>(from account in calendarGroupViewModel.Children.OfType<BindAccountViewModel>()
				select account.Id);
			List<string> list5 = OriginalData.BindAccounts.Where((string id) => !string.IsNullOrEmpty(id) && !CalendarUtils.ContainsAccountId(visibleAccountIds, id)).Distinct().ToList();
			List<string> list6 = OriginalData.SubscribeCalendars.Where((string id) => id != "#allSubscribe" && !CalendarUtils.ContainsAccountId(visibleAccountIds, id) && !CalendarUtils.ContainsCalendarId(visibleCalendarIds, id)).Distinct().ToList();
			if ((list5.Any() || list6.Any()) && !models.Contains(calendarGroupViewModel))
			{
				models.Add(calendarGroupViewModel);
			}
			if (list5.Any() || list6.Any())
			{
				UnavailableLocalCalendarViewModel item2 = new UnavailableLocalCalendarViewModel(list5, list6)
				{
					Selected = true,
					ShowIndent = true,
					InCalFilter = IsCalFilter,
					ToolTip = Utils.GetString("CalendarFromAnotherDevice")
				};
				calendarGroupViewModel.Children.Add(item2);
				models.Add(item2);
			}
		}
	}

	private void LoadFilters(List<SelectableItemViewModel> models, bool batchMode)
	{
		List<FilterModel> list = (from filter in CacheManager.GetFilters()
			where filter.deleted != 1
			orderby filter.sortOrder
			select filter).ToList();
		if (list.Count <= 0)
		{
			return;
		}
		FilterGroupViewModel filterGroupViewModel = new FilterGroupViewModel
		{
			InCalFilter = IsCalFilter
		};
		models.Add(filterGroupViewModel);
		foreach (FilterItemViewModel item in list.Select((FilterModel filter) => new FilterItemViewModel(filter.id, filter.name)
		{
			Selected = OriginalData.FilterIds.Contains(filter.id),
			ShowIndent = true,
			InCalFilter = IsCalFilter
		}))
		{
			models.Add(item);
			filterGroupViewModel.Children.Add(item);
		}
	}

	private void LoadTags(List<SelectableItemViewModel> models)
	{
		List<TagModel> list = (from tag in CacheManager.GetTags()
			orderby tag.sortOrder
			select tag).ToList();
		if (list.Count > 0)
		{
			bool allTagSelect = OriginalData.Tags.Contains("*withtags");
			TagGroupViewModel tagGroup = new TagGroupViewModel
			{
				InCalFilter = IsCalFilter,
				Selected = allTagSelect,
				PartSelected = (BatchMode && OriginalData.Tags.Any())
			};
			ApplyCalendarSidebarFilterStyle(tagGroup);
			models.Add(tagGroup);
			List<TagModel> tagChildrens = list.Where((TagModel t) => !string.IsNullOrEmpty(t.parent)).ToList();
			list = list.Where((TagModel t) => string.IsNullOrEmpty(t.parent)).ToList();
			foreach (TagViewModel item in list.Select(delegate(TagModel tag)
			{
				TagViewModel tagViewModel = new TagViewModel(tag, showIndent: true, IsCalFilter)
				{
					Selected = (allTagSelect || OriginalData.Tags.Contains(tag.name))
				};
				tagViewModel.GroupParent = tagGroup;
				tagGroup.Children.Add(tagViewModel);
				tagViewModel.Children = (tagViewModel.IsParent ? tagChildrens.Where((TagModel t) => t.parent == tag.name).Select((Func<TagModel, SelectableItemViewModel>)((TagModel t) => new TagViewModel(t, showIndent: true, IsCalFilter)
				{
					Selected = (allTagSelect || OriginalData.Tags.Contains(t.name))
				})).ToList() : new List<SelectableItemViewModel>());
				tagViewModel.PartSelected = tagViewModel.IsParent && !tagViewModel.Selected && tagViewModel.Children != null && tagViewModel.Children.Any((SelectableItemViewModel child) => child.Selected);
				return tagViewModel;
			}).ToList())
			{
				models.Add(item);
				if (item.IsParent && item.Open)
				{
					item.Children?.ForEach(models.Add);
				}
			}
			_tagShowed = true;
		}
		else
		{
			_tagShowed = false;
		}
	}

	private void LoadProjects(List<SelectableItemViewModel> models, string searchText)
	{
		List<SelectableItemViewModel> list = ProjectDataAssembler.AssembleProjects(OnlyShowPermission, IsCalFilter);
		bool flag = IsAllListSelectedInOriginalData();
		bool childSelectedByAllList = flag && HighlightAllListChildren;
		bool showIndent = ShowListGroup;
		ListGroupViewModel listGroup = new ListGroupViewModel
		{
			Open = (!CanExpandAllList || !_foldIds.Contains("#alllists")),
			InCalFilter = IsCalFilter,
			Selected = flag,
			PartSelected = false,
			UseCircleCheck = UseCircleCheckForSectionGroups,
			CanExpand = CanExpandAllList
		};
		ApplyCalendarSidebarFilterStyle(listGroup);
		if (showIndent)
		{
			models.Add(listGroup);
		}
		else if (ShowSmartProjects)
		{
			models.Add(new SelectableItemViewModel
			{
				IsSplit = true,
				IsEnable = false
			});
		}
		InboxProjectViewModel inbox = new InboxProjectViewModel
		{
			Selected = (childSelectedByAllList || (OriginalData.ProjectIds?.Any((string id) => id?.Contains("inbox") ?? false) ?? false)),
			ShowIndent = showIndent,
			InCalFilter = IsCalFilter,
			Linked = (LinkedProjectIds != null && LinkedProjectIds.Contains(Utils.GetInboxId()))
		};
		inbox.Selectable = !inbox.Linked;
		inbox.IsGray = inbox.Linked;
		inbox.GroupParent = listGroup;
		listGroup.Children.Add(inbox);
		if (!string.IsNullOrWhiteSpace(searchText))
		{
			searchText = searchText.ToLower();
			list.Insert(0, inbox);
			List<string> list2 = searchText.Split(' ').ToList();
			list2.RemoveAll((string key) => key == "");
			foreach (SelectableItemViewModel item in list)
			{
				if ((item.IsNote && !ShowNoteProject) || (item.IsShare && !ShowSharedProject) || !(item is ProjectViewModel projectViewModel))
				{
					continue;
				}
				string text = projectViewModel.Title?.ToLower();
				if (PinyinUtils.ToPinyin(text).Contains(searchText) || SearchHelper.KeyWordMatch(projectViewModel.Emoji + text, searchText.Trim(), list2) != 0)
				{
					item.Linked = LinkedProjectIds != null && LinkedProjectIds.Contains(item.Id);
					item.Selectable = item.Selectable && !item.Linked;
					item.IsGray = item.Linked;
					models.Add(item);
					item.IsSubItem = false;
				}
				if (!_projectColumnDict.ContainsKey(projectViewModel.Id))
				{
					continue;
				}
				foreach (ColumnModel item2 in _projectColumnDict[projectViewModel.Id])
				{
					string text2 = item2.name?.ToLower();
					if (PinyinUtils.ToPinyin(text2).Contains(searchText) || SearchHelper.KeyWordMatch(text2, searchText.Trim(), list2) != 0)
					{
						models.Add(new ProjectColumnViewModel(item2)
						{
							Desc = projectViewModel.Emoji + projectViewModel.Title,
							IsSubItem = false
						});
					}
				}
			}
			models.Sort(delegate(SelectableItemViewModel l, SelectableItemViewModel r)
			{
				if (l is ProjectViewModel && r is ProjectColumnViewModel)
				{
					return -1;
				}
				if (l is ProjectColumnViewModel && r is ProjectViewModel)
				{
					return 1;
				}
				if (l is ProjectViewModel projectViewModel3 && r is ProjectViewModel projectViewModel4)
				{
					if (!l.IsNote && r.IsNote)
					{
						return -1;
					}
					if (l.IsNote && !r.IsNote)
					{
						return 1;
					}
					return projectViewModel3.SortOrder.CompareTo(projectViewModel4.SortOrder);
				}
				return l.SortOrder.CompareTo(r.SortOrder);
			});
			ClosePopup();
			return;
		}
		if (ShowInbox)
		{
			models.Add(inbox);
		}
		if ((inbox.Children == null || inbox.Children.Count == 0) && inbox.Id != null && _projectColumnDict.ContainsKey(inbox.Id))
		{
			List<ColumnModel> source = _projectColumnDict[inbox.Id];
			string text3 = source.FirstOrDefault()?.id;
			string selectedColumnId = (string.IsNullOrEmpty(SelectedColumn) ? text3 : SelectedColumn);
			inbox.Children = ((IEnumerable<ColumnModel>)source).Select((Func<ColumnModel, SelectableItemViewModel>)((ColumnModel c) => new ProjectColumnViewModel(c)
			{
				Selected = (inbox.Selected && c.id == selectedColumnId)
			})).ToList();
			List<SelectableItemViewModel> children = inbox.Children;
			if (children != null && children.Count > 1)
			{
				inbox.ShowSubOnSide = true;
				inbox.Selectable = false;
			}
			else
			{
				inbox.Children?.Clear();
			}
		}
		foreach (SelectableItemViewModel project in list)
		{
			project.ShowIndent = showIndent;
			project.GroupParent = listGroup;
			if (!(project is TeamSectionViewModel))
			{
				ProjectGroupViewModel projectGroupViewModel = project as ProjectGroupViewModel;
				if (projectGroupViewModel == null)
				{
					ProjectViewModel projectViewModel2 = project as ProjectViewModel;
					if (projectViewModel2 == null || (!ShowNoteProject && project.IsNote) || (!ShowSharedProject && project.IsShare))
					{
						continue;
					}
					project.Selected = childSelectedByAllList || (OriginalData.ProjectIds?.Contains(projectViewModel2.Id) ?? false);
					if ((projectViewModel2.Children == null || projectViewModel2.Children.Count == 0) && projectViewModel2.Id != null && _projectColumnDict.ContainsKey(projectViewModel2.Id))
					{
						List<ColumnModel> source2 = _projectColumnDict[projectViewModel2.Id];
						string text4 = source2.FirstOrDefault()?.id;
						string selectedColumnId2 = (string.IsNullOrEmpty(SelectedColumn) ? text4 : SelectedColumn);
						projectViewModel2.Children = ((IEnumerable<ColumnModel>)source2).Select((Func<ColumnModel, SelectableItemViewModel>)((ColumnModel c) => new ProjectColumnViewModel(c)
						{
							Selected = (projectViewModel2.Selected && c.id == selectedColumnId2)
						})).ToList();
						List<SelectableItemViewModel> children2 = projectViewModel2.Children;
						if (children2 != null && children2.Count > 1)
						{
							projectViewModel2.ShowSubOnSide = true;
						}
						else
						{
							projectViewModel2.Children?.Clear();
						}
					}
					projectViewModel2.Linked = LinkedProjectIds != null && LinkedProjectIds.Contains(projectViewModel2.Id);
					projectViewModel2.Selectable = projectViewModel2.Selectable && !projectViewModel2.Linked;
					projectViewModel2.IsGray = project.Linked;
					if (!(projectViewModel2 is SubProjectViewModel))
					{
						models.Add(project);
						listGroup.Children.Add(project);
					}
					continue;
				}
				projectGroupViewModel.Selected = childSelectedByAllList || (OriginalData.GroupIds?.Contains(projectGroupViewModel.Id) ?? false);
				projectGroupViewModel.Children.ForEach(delegate(SelectableItemViewModel p)
				{
					p.Selected = projectGroupViewModel.Selected || childSelectedByAllList || (OriginalData.ProjectIds?.Contains(p.Id) ?? false);
					p.BatchMode = BatchMode;
					p.IsSubItem = true;
					p.ShowIndent = showIndent;
				});
				projectGroupViewModel.Children.RemoveAll((SelectableItemViewModel p) => p.IsNote && !ShowNoteProject);
				projectGroupViewModel.Children.RemoveAll((SelectableItemViewModel p) => p.IsShare && !ShowSharedProject);
				projectGroupViewModel.PartSelected = !projectGroupViewModel.Selected && (projectGroupViewModel.Children?.Any((SelectableItemViewModel child) => child.Selected)).Value;
				projectGroupViewModel.ShowSubOnSide = !CanSelectGroup;
				projectGroupViewModel.Selectable = CanSelectGroup;
				projectGroupViewModel.IsParent = CanSelectGroup;
				models.Add(projectGroupViewModel);
				listGroup.Children.Add(project);
				if (!CanSelectGroup)
				{
					continue;
				}
				projectGroupViewModel.Children?.ForEach(delegate(SelectableItemViewModel child)
				{
					if (projectGroupViewModel.Open)
					{
						models.Add(child);
						listGroup.Children.Add(project);
					}
				});
			}
			else
			{
				listGroup.Children.Add(project);
				models.Add(project);
			}
		}
	}

	public void OnTagGroupChanged(TagGroupViewModel tagGroup, bool setData = true)
	{
		if (_data != null && tagGroup != null)
		{
			if (tagGroup.Open)
			{
				_foldIds.Remove(tagGroup.Id);
				int index = _data.IndexOf(tagGroup);
				foreach (SelectableItemViewModel child in tagGroup.Children)
				{
					_data.Insert(++index, child);
					if (child.IsParent && child.Open)
					{
						child.Children?.ForEach(delegate(SelectableItemViewModel child)
						{
							_data.Insert(++index, child);
						});
					}
				}
			}
			else
			{
				_foldIds.Add(tagGroup.Id);
				_data.RemoveAll((SelectableItemViewModel m) => m is TagViewModel);
			}
		}
		if (setData)
		{
			ItemsSourceHelper.SetItemsSource(SelectableItems, _data);
		}
	}

	public void OnListGroupChanged(ListGroupViewModel listGroup, bool setData = true)
	{
		if (_data != null && listGroup != null)
		{
			List<SelectableItemViewModel> listAndGroupData = new List<SelectableItemViewModel>();
			HashSet<string> dataIds = new HashSet<string>();
			foreach (SelectableItemViewModel child in listGroup.Children)
			{
				if (dataIds.Contains(child.Id))
				{
					continue;
				}
				listAndGroupData.Add(child);
				dataIds.Add(child.Id);
				if (child is TeamSectionViewModel teamSectionViewModel)
				{
					if (!child.Open)
					{
						continue;
					}
					foreach (SelectableItemViewModel child2 in teamSectionViewModel.Children)
					{
						listAndGroupData.Add(child2);
						dataIds.Add(child2.Id);
						if (child2.Open)
						{
							child2.Children.ForEach(delegate(SelectableItemViewModel model)
							{
								listAndGroupData.Add(model);
								dataIds.Add(model.Id);
							});
						}
					}
				}
				else if (child.Open)
				{
					child.Children.ForEach(delegate(SelectableItemViewModel model)
					{
						listAndGroupData.Add(model);
						dataIds.Add(model.Id);
					});
				}
			}
			if (listGroup.Open)
			{
				_foldIds.Remove(listGroup.Id);
				int num = _data.IndexOf(listGroup);
				foreach (SelectableItemViewModel item in listAndGroupData)
				{
					_data.Insert(++num, item);
				}
			}
			else
			{
				_foldIds.Add(listGroup.Id);
				_data.RemoveAll((SelectableItemViewModel item) => dataIds.Contains(item.Id));
			}
		}
		if (setData)
		{
			ItemsSourceHelper.SetItemsSource(SelectableItems, _data);
		}
	}

	public void OnItemSelectedChanged(SelectableItemViewModel model)
	{
		if (!(model is ProjectGroupViewModel))
		{
			if (!(model is ProjectViewModel))
			{
				if (!(model is AllProjectViewModel model2))
				{
					if (!(model is AllProjectCategoryViewModel model3))
					{
						if (!(model is FilterItemViewModel model4))
						{
							if (!(model is BindAccountViewModel account))
							{
								if (!(model is SubscribeCalendarViewModel sub))
								{
									if (model is TagViewModel model5)
									{
										if (BatchMode)
										{
											HandleOnTagSelected(model5);
										}
										PartSelectAllTag();
									}
								}
								else
								{
									HandleOnSubscribeSelected(sub);
									PartSelectAllCal();
								}
							}
							else
							{
								HandleOnAccountSelected(account);
								PartSelectAllCal();
							}
						}
						else if (BatchMode)
						{
							HandleOnAllOrFilterSelected(model4);
						}
					}
					else
					{
						HandleOnAllProjectOrFilterSelected(model3);
					}
				}
				else
				{
					HandleOnAllOrFilterSelected(model2);
				}
			}
			else
			{
				PartSelectAllList();
				if (model is SubProjectViewModel)
				{
					HandleOnSubItemCheckChanged(model);
				}
			}
		}
		else
		{
			if (!CanSelectGroup)
			{
				model.Selected = false;
				return;
			}
			model.Children?.ForEach(delegate(SelectableItemViewModel child)
			{
				child.Selected = model.Selected;
			});
			PartSelectAllList();
		}
		SelectAllOnItemUnSelected(model);
		UnSelectAllOnItemSelected(model);
		if (!(model is AllProjectViewModel))
		{
			RefreshAllProjectSelected();
		}
		this.ItemSelect?.Invoke(this, model);
	}

	private void PartSelectAllList()
	{
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel i) => i is ListGroupViewModel);
		if (selectableItemViewModel == null)
		{
			return;
		}
		OriginalData.ProjectIds.RemoveAll(CalendarUtils.IsAllListId);
		selectableItemViewModel.Selected = AreAllListsSelected((ListGroupViewModel)selectableItemViewModel);
		selectableItemViewModel.PartSelected = false;
		if (selectableItemViewModel.Selected)
		{
			bool num = OriginalData.ProjectIds.Contains("Habit2e4c103c57ef480997943206");
			OriginalData.ProjectIds.Clear();
			OriginalData.GroupIds.Clear();
			OriginalData.ProjectIds.Add(selectableItemViewModel.Id);
			if (num)
			{
				OriginalData.ProjectIds.Add("Habit2e4c103c57ef480997943206");
			}
		}
	}

	private void PartSelectAllTag()
	{
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel i) => i is TagGroupViewModel);
		if (selectableItemViewModel != null)
		{
			if (selectableItemViewModel.Selected)
			{
				OriginalData.Tags.Clear();
				selectableItemViewModel.Selected = false;
			}
			selectableItemViewModel.PartSelected = _data.Any((SelectableItemViewModel m) => m is TagViewModel tagViewModel && tagViewModel.Selected);
		}
	}

	private void PartSelectAllCal()
	{
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel i) => i is CalendarGroupViewModel);
		if (selectableItemViewModel != null)
		{
			OriginalData.SubscribeCalendars.Remove("#allSubscribe");
			selectableItemViewModel.Selected = AreAllCalendarsSelected((CalendarGroupViewModel)selectableItemViewModel);
			selectableItemViewModel.PartSelected = false;
			if (selectableItemViewModel.Selected)
			{
				OriginalData.SubscribeCalendars.Clear();
				OriginalData.BindAccounts.Clear();
				OriginalData.SubscribeCalendars.Add(selectableItemViewModel.Id);
			}
		}
	}

	private void HandleOnTagSelected(SelectableItemViewModel model)
	{
		if (!string.IsNullOrEmpty(model.ParentId))
		{
			HandleOnSubItemCheckChanged(model);
		}
		else if (model.IsParent)
		{
			model.Children?.ForEach(delegate(SelectableItemViewModel child)
			{
				child.Selected = model.Selected;
			});
		}
	}

	public void OnGroupItemMouseMove(object sender, MouseEventArgs args)
	{
		if (!(sender is SelectableItem { DataContext: SelectableItemViewModel dataContext } selectableItem))
		{
			return;
		}
		UpDownSelectListView upDownSelectListView = Utils.FindParent<UpDownSelectListView>(selectableItem);
		if (upDownSelectListView == SubSubItems)
		{
			HoverItem(upDownSelectListView, dataContext);
			return;
		}
		if (upDownSelectListView == SubItems)
		{
			HandleSubItemMouseMove(selectableItem, dataContext);
			return;
		}
		if (dataContext is ProjectColumnViewModel)
		{
			if (!dataContext.HoverSelected)
			{
				upDownSelectListView?.ClearHover();
				dataContext.HoverSelected = true;
			}
			return;
		}
		if (SubPopup.IsOpen)
		{
			if (_hoverParent == dataContext)
			{
				_popupTracker.Mark();
				return;
			}
			if (_popupTracker.IsInSafeArea())
			{
				return;
			}
		}
		CloseSubSubPopup();
		if (SubPopup.IsOpen && _hoverParent != null)
		{
			SubPopup.IsOpen = false;
			_hoverParent.HoverSelected = false;
			_hoverParent.SubOpened = false;
			_hoverParent = null;
		}
		SelectableItemViewModel selectableItemViewModel = dataContext;
		if (!(selectableItemViewModel is ProjectViewModel projectViewModel))
		{
			if (selectableItemViewModel == null)
			{
				return;
			}
			if (selectableItemViewModel.ShowSubOnSide)
			{
				List<SelectableItemViewModel> children = selectableItemViewModel.Children;
				if (children != null && children.Count > 0)
				{
					ShowSideSubPopup(selectableItem, selectableItemViewModel, selectableItemViewModel.Children);
					return;
				}
			}
			if (selectableItemViewModel is SmartProjectViewModel)
			{
				HoverItem(upDownSelectListView, dataContext);
			}
		}
		else
		{
			List<SelectableItemViewModel> children2 = projectViewModel.Children;
			if (children2 != null && children2.Count > 1)
			{
				ShowSideSubPopup(selectableItem, projectViewModel, projectViewModel.Children);
			}
			else
			{
				HoverItem(upDownSelectListView, dataContext);
			}
		}
	}

	private void HandleSubItemMouseMove(SelectableItem item, SelectableItemViewModel model)
	{
		if (SubSubPopup.IsOpen)
		{
			if (_hoverSubParent == model)
			{
				_subPopupTracker.Mark();
				return;
			}
			if (_subPopupTracker.IsInSafeArea())
			{
				return;
			}
		}
		CloseSubSubPopup();
		List<SelectableItemViewModel> children = model.Children;
		if (children != null && children.Count > 1)
		{
			_hoverSubParent = model;
			HoverItem(SubItems, model);
			SubSubPopup.PlacementTarget = item.GetSubPopupPlacement();
			SubSubPopup.IsOpen = true;
			model.SubOpened = true;
			SetSubSubItems(model.Children);
			_subPopupTracker.Bind(SubSubPopup, delay: true);
		}
		else
		{
			HoverItem(SubItems, model);
		}
	}

	private void ShowSideSubPopup(SelectableItem item, SelectableItemViewModel parent, List<SelectableItemViewModel> children)
	{
		_hoverParent = parent;
		HoverItem(SelectableItems, parent);
		SubPopup.PlacementTarget = item.GetSubPopupPlacement();
		ShowSubPopup();
		parent.SubOpened = true;
		SetSubItems(children);
	}

	private static void HoverItem(UpDownSelectListView list, SelectableItemViewModel model)
	{
		if (!model.HoverSelected)
		{
			list?.ClearHover();
			model.HoverSelected = true;
		}
	}

	private void CloseSubSubPopup()
	{
		if (SubSubPopup.IsOpen && _hoverSubParent != null)
		{
			SubSubPopup.IsOpen = false;
			_hoverSubParent.HoverSelected = false;
			_hoverSubParent.SubOpened = false;
			_hoverSubParent = null;
		}
	}

	private void ShowSubPopup()
	{
		SubPopup.IsOpen = true;
		_popupTracker.Bind(SubPopup, delay: true);
	}

	private void SetSubItems(List<SelectableItemViewModel> groupChildren)
	{
		groupChildren.ForEach(delegate(SelectableItemViewModel m)
		{
			m.ShowIndent = false;
			m.IsSubItem = false;
		});
		ObservableCollection<SelectableItemViewModel> observableCollection = new ObservableCollection<SelectableItemViewModel>(groupChildren);
		SubItems.ItemsSource = observableCollection;
		SelectableItemViewModel selectableItemViewModel = observableCollection.FirstOrDefault((SelectableItemViewModel m) => m.Selected);
		if (selectableItemViewModel != null)
		{
			SubItems.ScrollIntoView(selectableItemViewModel);
		}
	}

	private void SetSubSubItems(List<SelectableItemViewModel> groupChildren)
	{
		groupChildren.ForEach(delegate(SelectableItemViewModel m)
		{
			m.ShowIndent = false;
			m.IsSubItem = false;
		});
		ObservableCollection<SelectableItemViewModel> observableCollection = new ObservableCollection<SelectableItemViewModel>(groupChildren);
		SubSubItems.ItemsSource = observableCollection;
		SelectableItemViewModel selectableItemViewModel = observableCollection.FirstOrDefault((SelectableItemViewModel m) => m.Selected);
		if (selectableItemViewModel != null)
		{
			SubSubItems.ScrollIntoView(selectableItemViewModel);
		}
	}

	private async void OnSubItemClick(bool enter, UpDownSelectViewModel e)
	{
		if (e.Selectable)
		{
			SubPopup.IsOpen = false;
		}
		OnItemClick(enter, e);
	}

	private async void OnSubSubItemClick(bool enter, UpDownSelectViewModel e)
	{
		if (e.Selectable)
		{
			SubSubPopup.IsOpen = false;
			SubPopup.IsOpen = false;
		}
		OnItemClick(enter, e);
	}

	private void OnItemClick(bool enter, UpDownSelectViewModel e)
	{
		if (!(e is SelectableItemViewModel selectableItemViewModel))
		{
			return;
		}
		if (selectableItemViewModel.IsSectionGroup)
		{
			if (ShouldSelectSectionGroupOnRowClick(selectableItemViewModel))
			{
				selectableItemViewModel.PartSelected = false;
				OnSectionGroupSelected(selectableItemViewModel);
				return;
			}
			selectableItemViewModel.Open = !selectableItemViewModel.Open;
			if (selectableItemViewModel.Selectable)
			{
				selectableItemViewModel.Selected = !selectableItemViewModel.Selected;
			}
			if (!(selectableItemViewModel is TagGroupViewModel tagGroup))
			{
				if (!(selectableItemViewModel is ListGroupViewModel listGroup))
				{
					if (!(selectableItemViewModel is FilterGroupViewModel filterGroup))
					{
						if (!(selectableItemViewModel is CalendarGroupViewModel calGroup))
						{
							if (selectableItemViewModel is TeamSectionViewModel section)
							{
								OnSectionGroupChanged(section);
							}
						}
						else
						{
							OnCalendarGroupChanged(calGroup);
						}
					}
					else
					{
						OnFilterGroupChanged(filterGroup);
					}
				}
				else
				{
					OnListGroupChanged(listGroup);
				}
			}
			else
			{
				OnTagGroupChanged(tagGroup);
			}
		}
		else if (selectableItemViewModel.Selectable)
		{
			OnItemSelectedChanged(selectableItemViewModel);
		}
		else if (selectableItemViewModel.Linked)
		{
			Toast(Utils.GetString("ListIntegrated"));
		}
		else if (selectableItemViewModel is AllSmartProjectViewModel)
		{
			List<string> linkedProjectIds = LinkedProjectIds;
			if (linkedProjectIds != null && linkedProjectIds.Count > 0)
			{
				Toast(Utils.GetString("NotSupportIntegrate"));
			}
		}
		if (!selectableItemViewModel.InCalFilter || !selectableItemViewModel.Selected)
		{
			return;
		}
		if (!(selectableItemViewModel is AllProjectViewModel))
		{
			if (!(selectableItemViewModel is ProjectViewModel) && !(selectableItemViewModel is ProjectGroupViewModel))
			{
				if (!(selectableItemViewModel is FilterItemViewModel))
				{
					if (!(selectableItemViewModel is TagViewModel))
					{
						if (selectableItemViewModel is SubscribeCalendarViewModel)
						{
							UserActCollectUtils.AddClickEvent("calendar", "filter_select", "calendar");
						}
					}
					else
					{
						UserActCollectUtils.AddClickEvent("calendar", "filter_select", "tag");
					}
				}
				else
				{
					UserActCollectUtils.AddClickEvent("calendar", "filter_select", "filter");
				}
			}
			else
			{
				UserActCollectUtils.AddClickEvent("calendar", "filter_select", "list_or_folder");
			}
		}
		else
		{
			UserActCollectUtils.AddClickEvent("calendar", "filter_select", "all");
		}
	}

	private bool ShouldSelectSectionGroupOnRowClick(SelectableItemViewModel model)
	{
		if (!UseCalendarSidebarFilterStyle)
		{
			if (!(model is ListGroupViewModel) && !(model is CalendarGroupViewModel))
			{
				return model.UseCircleCheck;
			}
			return true;
		}
		return false;
	}

	private void HandleOnSubscribeSelected(SubscribeCalendarViewModel sub)
	{
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel model) => model.Id == sub.AccountId);
		if (selectableItemViewModel != null)
		{
			int num = selectableItemViewModel.Children.Count((SelectableItemViewModel item) => item.Selected);
			selectableItemViewModel.PartSelected = num > 0 && num != selectableItemViewModel.Children?.Count;
			selectableItemViewModel.Selected = num == selectableItemViewModel.Children?.Count;
		}
	}

	private void HandleOnAccountSelected(SelectableItemViewModel account)
	{
		BindAccountViewModel model = account as BindAccountViewModel;
		if (model != null)
		{
			model.PartSelected = false;
			model.Children.ForEach(delegate(SelectableItemViewModel child)
			{
				child.Selected = model.Selected;
			});
		}
	}

	private void SelectAllOnItemUnSelected(SelectableItemViewModel model)
	{
		if (model.Selected || _data.Any((SelectableItemViewModel m) => m is ProjectGroupViewModel { Highlighted: not false } || (m is ProjectViewModel projectViewModel && projectViewModel.Selected)))
		{
			return;
		}
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel item) => item is FilterHabitViewModel);
		SelectableItemViewModel selectableItemViewModel2 = _data.FirstOrDefault((SelectableItemViewModel item) => item.Id == "Calendar5959a2259161d16d23a4f272");
		SelectableItemViewModel selectableItemViewModel3 = _data.FirstOrDefault((SelectableItemViewModel item) => item.Id == "ProjectAll2e4c103c57ef480997943206");
		if ((selectableItemViewModel == null || !selectableItemViewModel.Selected) && (selectableItemViewModel2 == null || !selectableItemViewModel2.Selected) && (selectableItemViewModel3 == null || !selectableItemViewModel3.Selected))
		{
			SelectableItemViewModel selectableItemViewModel4 = _data.FirstOrDefault((SelectableItemViewModel item) => item is AllProjectViewModel);
			if (selectableItemViewModel4 != null)
			{
				selectableItemViewModel4.Selected = true;
			}
			else if (selectableItemViewModel3 != null)
			{
				selectableItemViewModel3.Selected = true;
			}
		}
	}

	private void UnSelectAllOnItemSelected(SelectableItemViewModel model)
	{
		if (!(model is AllProjectViewModel) && model.Selected)
		{
			SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel item) => item is AllProjectViewModel);
			if (selectableItemViewModel != null)
			{
				selectableItemViewModel.Selected = false;
			}
		}
		if (!(model is AllProjectCategoryViewModel) && model.Selected && model.Id != "Habit2e4c103c57ef480997943206" && model.Id != "Calendar5959a2259161d16d23a4f272")
		{
			SelectableItemViewModel selectableItemViewModel2 = _data.FirstOrDefault((SelectableItemViewModel item) => item is AllProjectCategoryViewModel);
			if (selectableItemViewModel2 != null)
			{
				selectableItemViewModel2.Selected = false;
			}
		}
		if (!model.Selected || model is FilterItemViewModel)
		{
			return;
		}
		_data.ToList().ForEach(delegate(SelectableItemViewModel item)
		{
			if (item is FilterItemViewModel)
			{
				item.Selected = false;
			}
		});
	}

	private void HandleOnAllProjectOrFilterSelected(SelectableItemViewModel model)
	{
		if (model.Selected)
		{
			_data.ToList().ForEach(CancelSelectChildren);
			return;
		}
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel item) => item is FilterHabitViewModel);
		SelectableItemViewModel selectableItemViewModel2 = _data.FirstOrDefault((SelectableItemViewModel item) => item.Id == "Calendar5959a2259161d16d23a4f272");
		if (selectableItemViewModel != null && !selectableItemViewModel.Selected && selectableItemViewModel2 != null && !selectableItemViewModel2.Selected)
		{
			SelectableItemViewModel selectableItemViewModel3 = _data.FirstOrDefault((SelectableItemViewModel item) => item is AllProjectViewModel);
			if (selectableItemViewModel3 != null)
			{
				selectableItemViewModel3.Selected = true;
			}
		}
		void CancelSelectChildren(SelectableItemViewModel item)
		{
			if (item.Id != model.Id && item.Id != "Habit2e4c103c57ef480997943206" && item.Id != "Calendar5959a2259161d16d23a4f272")
			{
				item.Selected = false;
				item.PartSelected = false;
				List<SelectableItemViewModel> children = item.Children;
				if (children != null && children.Count > 0)
				{
					foreach (SelectableItemViewModel child in item.Children)
					{
						CancelSelectChildren(child);
					}
				}
			}
		}
	}

	private void HandleOnAllOrFilterSelected(SelectableItemViewModel model)
	{
		if (model.Selected)
		{
			_data.ToList().ForEach(CancelSelectChildren);
		}
		else if (!HasAnyConcreteSelection(model))
		{
			SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel item) => item is AllProjectViewModel);
			if (selectableItemViewModel != null)
			{
				selectableItemViewModel.Selected = true;
			}
		}
		void CancelSelectChildren(SelectableItemViewModel item)
		{
			if (item.Id != model.Id)
			{
				item.Selected = false;
				item.PartSelected = false;
				List<SelectableItemViewModel> children = item.Children;
				if (children != null && children.Count > 0)
				{
					foreach (SelectableItemViewModel child in item.Children)
					{
						CancelSelectChildren(child);
					}
				}
			}
		}
	}

	private bool HasAnyConcreteSelection(SelectableItemViewModel except)
	{
		return _data?.Any((SelectableItemViewModel item) => item != except && !(item is AllProjectViewModel) && (item.Selected || item.PartSelected)) ?? false;
	}

	private void HandleOnSubItemCheckChanged(SelectableItemViewModel model)
	{
		SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel item) => item.Id == model.ParentId);
		if (selectableItemViewModel == null)
		{
			return;
		}
		if (!model.Selected)
		{
			selectableItemViewModel.Selected = false;
			selectableItemViewModel.PartSelected = selectableItemViewModel.Children?.Any((SelectableItemViewModel child) => child.Selected) ?? false;
		}
		else if (!selectableItemViewModel.Selected)
		{
			List<SelectableItemViewModel> children = selectableItemViewModel.Children;
			if (children != null && children.Any((SelectableItemViewModel child) => !child.Selected))
			{
				selectableItemViewModel.PartSelected = true;
			}
			else
			{
				selectableItemViewModel.Selected = true;
			}
		}
	}

	private async void OnItemsLoaded(object sender, RoutedEventArgs e)
	{
		if (ShowColumns)
		{
			await GetAllColumns();
		}
		LoadData(_searchText);
		if (!BatchMode)
		{
			SelectableItemViewModel selectableItemViewModel = _data?.FirstOrDefault((SelectableItemViewModel d) => d.Selected || d.Highlighted);
			if (selectableItemViewModel != null)
			{
				ScrollToItemWithoutAnimation(_data?.IndexOf(selectableItemViewModel) ?? 0);
			}
		}
	}

	private void ScrollToItemWithoutAnimation(int index)
	{
		if (index >= 8)
		{
			(SelectableItems.Template.FindName("ScrollViewer", SelectableItems) as ScrollViewer)?.ScrollToVerticalOffset(index * 36);
		}
	}

	private async Task GetAllColumns()
	{
		List<ColumnModel> obj = await ColumnDao.GetAllColumnsAsync();
		obj.Sort((ColumnModel a, ColumnModel b) => a.sortOrder?.CompareTo(b.sortOrder) ?? 1);
		_projectColumnDict.Clear();
		foreach (ColumnModel item in obj)
		{
			if (item.deleted != 1 && !string.IsNullOrEmpty(item.projectId))
			{
				if (_projectColumnDict.ContainsKey(item.projectId))
				{
					_projectColumnDict[item.projectId].Add(item);
					continue;
				}
				_projectColumnDict[item.projectId] = new List<ColumnModel> { item };
			}
		}
	}

	public void OnSectionGroupChanged(TeamSectionViewModel section)
	{
		if (section.Open)
		{
			SelectableItemViewModel selectableItemViewModel = _data.FirstOrDefault((SelectableItemViewModel model) => model is TeamSectionViewModel teamSectionViewModel && teamSectionViewModel.Id == section.TeamId);
			if (selectableItemViewModel != null)
			{
				int index = _data.IndexOf(selectableItemViewModel);
				List<SelectableItemViewModel> list = new List<SelectableItemViewModel>();
				AddItems(selectableItemViewModel, list);
				list.ForEach(delegate(SelectableItemViewModel child)
				{
					if ((!child.IsNote || ShowNoteProject) && (!child.IsShare || ShowSharedProject))
					{
						_data.Insert(++index, child);
					}
				});
			}
		}
		else
		{
			_data.RemoveAll((SelectableItemViewModel item) => !(item is TeamSectionViewModel) && item.TeamId == section.TeamId);
		}
		ItemsSourceHelper.SetItemsSource(SelectableItems, _data);
	}

	private static void AddItems(SelectableItemViewModel item, List<SelectableItemViewModel> models)
	{
		if (item.Children == null || !item.Children.Any())
		{
			return;
		}
		foreach (SelectableItemViewModel child in item.Children)
		{
			models.Add(child);
			if (!(child is ProjectViewModel))
			{
				AddItems(child, models);
			}
		}
	}

	public void OnFoldChildren(SelectableItem item, SelectableItemViewModel model)
	{
		if (!model.CanExpand || !model.IsParent || _data == null)
		{
			return;
		}
		model.Open = !model.Open;
		if (model.Open)
		{
			int index = _data.IndexOf(model);
			if (index >= 0)
			{
				model.Children?.ForEach(delegate(SelectableItemViewModel child)
				{
					_data.Insert(++index, child);
				});
			}
		}
		else
		{
			_data.RemoveAll((SelectableItemViewModel m) => m.ParentId == model.Id);
		}
		ItemsSourceHelper.SetItemsSource(SelectableItems, _data);
	}

	public bool ChildMouseOver()
	{
		if (!SubPopup.IsMouseOver)
		{
			return SubSubPopup.IsMouseOver;
		}
		return true;
	}

	private void OnItemsPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		HandleMouseWheel(sender as UpDownSelectListView, e);
	}

	private void AttachMouseWheelHandler(UpDownSelectListView list)
	{
		list.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnItemsPreviewMouseWheel), handledEventsToo: true);
	}

	private void HandleMouseWheel(UpDownSelectListView list, MouseWheelEventArgs e)
	{
		if (list != null)
		{
			list.ApplyTemplate();
			ScrollViewer scrollViewer = (list.Template.FindName("ScrollViewer", list) as ScrollViewer) ?? Utils.FindSingleVisualChildren<ScrollViewer>(list);
			if (scrollViewer is SmoothScrollViewer smoothScrollViewer)
			{
				smoothScrollViewer.HandleSmoothMouseWheel(e);
				return;
			}
			scrollViewer?.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (double)e.Delta / 2.0);
			e.Handled = true;
		}
	}

	public void OnFilterGroupChanged(FilterGroupViewModel filterGroup, bool setData = true)
	{
		if (_data != null && filterGroup != null)
		{
			if (filterGroup.Open)
			{
				_foldIds.Remove(filterGroup.Id);
				int index = _data.IndexOf(filterGroup);
				foreach (SelectableItemViewModel child in filterGroup.Children)
				{
					_data.Insert(++index, child);
					if (child.IsParent && child.Open)
					{
						child.Children?.ForEach(delegate(SelectableItemViewModel child)
						{
							_data.Insert(++index, child);
						});
					}
				}
			}
			else
			{
				_foldIds.Add(filterGroup.Id);
				_data.RemoveAll((SelectableItemViewModel m) => m is FilterItemViewModel);
			}
		}
		if (setData)
		{
			ItemsSourceHelper.SetItemsSource(SelectableItems, _data);
		}
	}

	public void OnCalendarGroupChanged(CalendarGroupViewModel calGroup, bool setData = true)
	{
		if (_data != null && calGroup != null)
		{
			if (calGroup.Open)
			{
				_foldIds.Remove(calGroup.Id);
				int index = _data.IndexOf(calGroup);
				foreach (SelectableItemViewModel child in calGroup.Children)
				{
					_data.Insert(++index, child);
					if (child.IsParent && child.Open)
					{
						child.Children?.ForEach(delegate(SelectableItemViewModel child)
						{
							_data.Insert(++index, child);
						});
					}
				}
			}
			else
			{
				_foldIds.Add(calGroup.Id);
				_data.RemoveAll((SelectableItemViewModel m) => m is SubscribeCalendarViewModel || m is BindAccountViewModel);
			}
		}
		if (setData)
		{
			ItemsSourceHelper.SetItemsSource(SelectableItems, _data);
		}
	}

	public bool UpDownSelect(bool isUp)
	{
		if (SubSubPopup.IsOpen)
		{
			SubSubItems.UpDownSelect(isUp);
			return true;
		}
		if (SubPopup.IsOpen)
		{
			SubItems.UpDownSelect(isUp);
			return true;
		}
		return SelectableItems.UpDownSelect(isUp);
	}

	public bool HandleTab(bool shift)
	{
		UpDownSelect(shift);
		return true;
	}

	public bool HandleEnter()
	{
		if (SubSubPopup.IsOpen)
		{
			SubSubPopup.HandleEnter();
			return true;
		}
		if (SubPopup.IsOpen)
		{
			SubPopup.HandleEnter();
			return true;
		}
		return SelectableItems.HandleEnter();
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		if (SubSubPopup.IsOpen && isLeft)
		{
			SubSubItems.ClearHover();
			SubSubPopup.IsOpen = false;
			if (_hoverSubParent != null)
			{
				_hoverSubParent.SubOpened = false;
				_hoverSubParent = null;
			}
			return true;
		}
		if (isLeft && SubPopup.IsOpen)
		{
			if (_hoverSubParent != null)
			{
				_hoverSubParent.SubOpened = false;
				_hoverSubParent = null;
			}
			SubItems.ClearHover();
			SubPopup.IsOpen = false;
			if (_hoverParent != null)
			{
				_hoverParent.SubOpened = false;
				_hoverParent = null;
			}
			return true;
		}
		if (!isLeft && SubPopup.IsOpen)
		{
			SelectableItem selectableItem = Utils.FindSingleVisualChildren<SelectableItem>(SubItems.GetHoverSelectedItem());
			if (selectableItem != null && selectableItem.DataContext is SelectableItemViewModel { ShowSubOnSide: not false } selectableItemViewModel)
			{
				List<SelectableItemViewModel> children = selectableItemViewModel.Children;
				if (children != null && children.Count > 0)
				{
					_hoverSubParent = selectableItemViewModel;
					_hoverSubParent.HoverSelected = true;
					SubSubPopup.PlacementTarget = selectableItem.GetSubPopupPlacement();
					SubSubPopup.IsOpen = true;
					selectableItemViewModel.SubOpened = true;
					selectableItemViewModel.Children[0].HoverSelected = true;
					SetSubSubItems(selectableItemViewModel.Children);
				}
			}
			return true;
		}
		if (!isLeft && !SubPopup.IsOpen)
		{
			SelectableItem selectableItem2 = Utils.FindSingleVisualChildren<SelectableItem>(SelectableItems.GetHoverSelectedItem());
			if (selectableItem2 != null && selectableItem2.DataContext is SelectableItemViewModel { ShowSubOnSide: not false } selectableItemViewModel2)
			{
				List<SelectableItemViewModel> children2 = selectableItemViewModel2.Children;
				if (children2 != null && children2.Count > 0)
				{
					_hoverParent = selectableItemViewModel2;
					_hoverParent.HoverSelected = true;
					SubPopup.PlacementTarget = selectableItem2.GetSubPopupPlacement();
					SubPopup.IsOpen = true;
					selectableItemViewModel2.SubOpened = true;
					selectableItemViewModel2.Children[0].HoverSelected = true;
					SetSubItems(selectableItemViewModel2.Children);
				}
			}
			return true;
		}
		return false;
	}

	public bool SpaceSelect()
	{
		if (!SubSubPopup.IsOpen && !SubPopup.IsOpen)
		{
			return SelectableItems.HandleSpace();
		}
		return false;
	}

	public void EnterSelect()
	{
		if (SubSubPopup.IsOpen)
		{
			SubSubPopup.HandleEnter();
		}
		else if (SubPopup.IsOpen)
		{
			SubItems.HandleEnter();
		}
		else
		{
			SelectableItems.HandleEnter();
		}
	}

	public void HoverSelectFirst()
	{
		List<SelectableItemViewModel> data = _data;
		if (data != null && data.Count > 0)
		{
			_data[0].HoverSelected = true;
		}
	}

	private void OnLeftRightKeyDown(object sender, bool isLeft)
	{
		LeftRightSelect(isLeft);
	}

	private void OnSubLeftRightKeyDown(object sender, bool isLeft)
	{
		LeftRightSelect(isLeft);
	}

	private void OnSubClosed(object sender, EventArgs e)
	{
		if (_hoverParent != null)
		{
			_hoverParent.SubOpened = false;
			_hoverParent = null;
		}
	}

	private void OnSubSubClosed(object sender, EventArgs e)
	{
		if (_hoverParent != null)
		{
			_hoverParent.SubOpened = false;
			_hoverParent = null;
		}
	}

	public void ClosePopup()
	{
		SubSubPopup.IsOpen = false;
		SubPopup.IsOpen = false;
	}

	public void Toast(string toastText)
	{
		ToastBorder.MaxWidth = Container.ActualWidth - 20.0;
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/itemselection.xaml", UriKind.Relative);
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
			((ItemSelection)target).Loaded += OnItemsLoaded;
			break;
		case 2:
			((Storyboard)target).Completed += OnToasted;
			break;
		case 3:
			Container = (Grid)target;
			break;
		case 4:
			SelectableItems = (UpDownSelectListView)target;
			break;
		case 5:
			SubPopup = (EscPopup)target;
			break;
		case 6:
			SubItems = (UpDownSelectListView)target;
			break;
		case 7:
			SubSubPopup = (EscPopup)target;
			break;
		case 8:
			SubSubItems = (UpDownSelectListView)target;
			break;
		case 9:
			ToastBorder = (Border)target;
			break;
		case 10:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
