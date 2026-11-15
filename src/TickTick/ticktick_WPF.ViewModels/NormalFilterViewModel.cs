using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Dal;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.ViewModels;

public class NormalFilterViewModel : BaseViewModel
{
	private List<string> _assignees = new List<string>();

	private string _displayProjectText = string.Empty;

	private List<string> _dueDates = new List<string>();

	private List<string> _groups = new List<string>();

	private List<int> _priorities = new List<int>();

	private List<string> _projects = new List<string>();

	private List<string> _calendarAccounts = new List<string>();

	private List<string> _calendars = new List<string>();

	private List<string> _tags = new List<string>();

	private List<string> _status = new List<string>();

	private List<string> _taskTypes = new List<string>();

	private List<string> _completedTimes = new List<string>();

	public int Version = 1;

	public List<int> Priorities
	{
		get
		{
			return _priorities ?? new List<int>();
		}
		set
		{
			_priorities = value;
			OnPropertyChanged("Priorities");
		}
	}

	public List<string> TaskTypes
	{
		get
		{
			return _taskTypes ?? new List<string>();
		}
		set
		{
			_taskTypes = value;
			OnPropertyChanged("TaskTypes");
		}
	}

	public List<string> Status
	{
		get
		{
			return _status ?? new List<string>();
		}
		set
		{
			_status = value;
			OnPropertyChanged("Status");
		}
	}

	public List<string> Tags
	{
		get
		{
			return _tags ?? new List<string>();
		}
		set
		{
			_tags = TagDao.GetValidAndChildren(TagSerializer.NormalizeNames(value));
			OnPropertyChanged("Tags");
		}
	}

	public List<string> DueDates
	{
		get
		{
			return _dueDates ?? new List<string>();
		}
		set
		{
			_dueDates = value;
			OnPropertyChanged("DueDates");
		}
	}

	public List<string> CompletedTimes
	{
		get
		{
			return _completedTimes ?? new List<string>();
		}
		set
		{
			_completedTimes = value;
			OnPropertyChanged("CompletedTimes");
		}
	}

	public List<string> Projects
	{
		get
		{
			return _projects ?? new List<string>();
		}
		set
		{
			_projects = value;
			OnPropertyChanged("Projects");
		}
	}

	public List<string> Groups
	{
		get
		{
			return _groups ?? new List<string>();
		}
		set
		{
			_groups = value;
			OnPropertyChanged("Groups");
		}
	}

	public List<string> CalendarAccounts
	{
		get
		{
			return _calendarAccounts ?? new List<string>();
		}
		set
		{
			_calendarAccounts = value;
			OnPropertyChanged("CalendarAccounts");
		}
	}

	public List<string> Calendars
	{
		get
		{
			return _calendars ?? new List<string>();
		}
		set
		{
			_calendars = value;
			OnPropertyChanged("Calendars");
		}
	}

	public List<string> Assignees
	{
		get
		{
			return _assignees ?? new List<string>();
		}
		set
		{
			_assignees = value;
			OnPropertyChanged("Assignees");
		}
	}

	public string DisplayProjectText
	{
		get
		{
			return _displayProjectText;
		}
		set
		{
			_displayProjectText = value;
			OnPropertyChanged("DisplayProjectText");
		}
	}

	public List<string> Keywords { get; set; } = new List<string>();

	public NormalFilterViewModel()
	{
	}

	public NormalFilterViewModel(SearchFilterModel searchFilter)
	{
		if (!string.IsNullOrWhiteSpace(searchFilter.Key))
		{
			Keywords.Add(searchFilter.Key);
		}
		List<string> assignees = searchFilter.Assignees;
		if (assignees != null && assignees.Count > 0)
		{
			_assignees.AddRange(searchFilter.Assignees);
		}
		List<int> priorities = searchFilter.Priorities;
		if (priorities != null && priorities.Count > 0)
		{
			_priorities.AddRange(searchFilter.Priorities);
		}
		List<string> tags = searchFilter.Tags;
		if (tags != null && tags.Count > 0)
		{
			_tags.AddRange(searchFilter.Tags);
		}
		List<string> projectIds = searchFilter.ProjectIds;
		if (projectIds != null && projectIds.Count > 0)
		{
			_projects.AddRange(searchFilter.ProjectIds);
		}
		List<string> groupIds = searchFilter.GroupIds;
		if (groupIds != null && groupIds.Count > 0)
		{
			_groups.AddRange(searchFilter.GroupIds);
		}
		List<string> calendarAccountIds = searchFilter.CalendarAccountIds;
		if (calendarAccountIds != null && calendarAccountIds.Count > 0)
		{
			_calendarAccounts.AddRange(searchFilter.CalendarAccountIds);
		}
		List<string> calendarIds = searchFilter.CalendarIds;
		if (calendarIds != null && calendarIds.Count > 0)
		{
			_calendars.AddRange(searchFilter.CalendarIds);
		}
		if ((_calendarAccounts.Count > 0 || _calendars.Count > 0) && !_projects.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			_projects.Add("Calendar5959a2259161d16d23a4f272");
		}
		switch (searchFilter.TaskType)
		{
		case TaskType.Task:
			_taskTypes.Add("task");
			break;
		case TaskType.Note:
			_taskTypes.Add("note");
			break;
		}
		switch (searchFilter.DateFilter)
		{
		case DateFilter.ThisWeek:
			_dueDates.Add("thisweek");
			break;
		case DateFilter.NextWeek:
			_dueDates.Add("nextweek");
			break;
		case DateFilter.ThisMonth:
			_dueDates.Add("thismonth");
			break;
		case DateFilter.LastWeek:
			break;
		}
	}

	public bool Empty()
	{
		if (!Priorities.Any() && !Tags.Any() && !DueDates.Any() && !Projects.Any() && !Groups.Any() && !CalendarAccounts.Any() && !Calendars.Any() && !Assignees.Any() && !Keywords.Any())
		{
			return !TaskTypes.Any();
		}
		return false;
	}

	public void NotifyPriorityChanged()
	{
		OnPropertyChanged("Priorities");
	}

	public void NotifyTaskTypeChanged()
	{
		OnPropertyChanged("TaskTypes");
	}

	public void NotifyAssigneeChanged()
	{
		OnPropertyChanged("Assignees");
	}

	public bool OnlyNote()
	{
		List<string> taskTypes = _taskTypes;
		if (taskTypes != null && taskTypes.Count == 1)
		{
			return _taskTypes.Contains("note");
		}
		return false;
	}

	public string ToRule(bool isSave)
	{
		NormalRuleModel normalRuleModel = new NormalRuleModel
		{
			and = new List<object>()
		};
		List<string> list = Projects.Where((string p) => !string.IsNullOrEmpty(p)).ToList();
		List<string> list2 = Groups.Where((string g) => !string.IsNullOrEmpty(g)).ToList();
		List<string> list3 = CalendarAccounts.Where((string id) => !string.IsNullOrEmpty(id)).ToList();
		List<string> list4 = Calendars.Where((string id) => !string.IsNullOrEmpty(id)).ToList();
		bool flag = list.RemoveAll(CalendarUtils.IsAllListId) > 0;
		bool num = list4.RemoveAll((string id) => id == "#allSubscribe") > 0;
		CalendarFilterRuleNormalizer.MoveAccountIdsFromCalendars(list3, list4);
		bool flag2 = num || (list.Contains("Calendar5959a2259161d16d23a4f272") && list3.Count == 0 && list4.Count == 0);
		if (flag2)
		{
			list.RemoveAll((string id) => id == "Calendar5959a2259161d16d23a4f272");
		}
		bool flag3 = flag && flag2 && list2.Count == 0 && list.Count == 0;
		if (!flag3 && flag && !list.Contains("ProjectAll2e4c103c57ef480997943206"))
		{
			list.Insert(0, "ProjectAll2e4c103c57ef480997943206");
		}
		if (flag2)
		{
			list3.Clear();
			list4.Clear();
			if (!flag3)
			{
				list.Add("Calendar5959a2259161d16d23a4f272");
			}
		}
		else if (list3.Count > 0 || list4.Count > 0)
		{
			list.RemoveAll((string id) => id == "Calendar5959a2259161d16d23a4f272");
		}
		if (list.Count > 0 || list2.Count > 0 || list3.Count > 0 || list4.Count > 0)
		{
			ListOrGroupRule listOrGroupRule = new ListOrGroupRule
			{
				or = new List<object>()
			};
			if (list.Count > 0)
			{
				ListRule item = new ListRule
				{
					or = list
				};
				listOrGroupRule.or.Add(item);
			}
			if (list2.Count > 0)
			{
				GroupRule item2 = new GroupRule
				{
					or = list2
				};
				listOrGroupRule.or.Add(item2);
			}
			if (list3.Count > 0)
			{
				BindAccountRule item3 = new BindAccountRule
				{
					or = list3
				};
				listOrGroupRule.or.Add(item3);
			}
			if (list4.Count > 0)
			{
				SubscribeCalendarRule item4 = new SubscribeCalendarRule
				{
					or = list4
				};
				listOrGroupRule.or.Add(item4);
			}
			if (list3.Count > 0 || list4.Count > 0)
			{
				normalRuleModel.version = Math.Max(normalRuleModel.version, 7);
			}
			else if (flag)
			{
				normalRuleModel.version = Math.Max(normalRuleModel.version, 7);
			}
			normalRuleModel.and.Add(listOrGroupRule);
		}
		if (Tags.Count > 0)
		{
			TagRule item5 = new TagRule
			{
				or = Tags
			};
			normalRuleModel.and.Add(item5);
		}
		if (Assignees.Count > 0)
		{
			AssigneeRule item6 = new AssigneeRule
			{
				or = Assignees
			};
			normalRuleModel.and.Add(item6);
			if (Assignees.Any((string assignee) => assignee != "me" && assignee != "other" && assignee != "noassignee"))
			{
				normalRuleModel.version = Math.Max(normalRuleModel.version, 3);
			}
		}
		if (Priorities.Count > 0)
		{
			PriorityRule item7 = new PriorityRule
			{
				or = Priorities
			};
			normalRuleModel.and.Add(item7);
		}
		if (Status.Count > 0)
		{
			StatusRule item8 = new StatusRule
			{
				or = Status
			};
			normalRuleModel.and.Add(item8);
		}
		if (TaskTypes.Count > 0)
		{
			TaskTypeRule item9 = new TaskTypeRule
			{
				or = TaskTypes
			};
			normalRuleModel.and.Add(item9);
			normalRuleModel.version = Math.Max(normalRuleModel.version, 3);
		}
		if (Keywords.Count > 0)
		{
			KeywordsRule item10 = new KeywordsRule
			{
				or = Keywords
			};
			normalRuleModel.and.Add(item10);
			normalRuleModel.version = Math.Max(normalRuleModel.version, 3);
		}
		if (DueDates.Count > 0)
		{
			if (CompletedTimes.Count > 0)
			{
				DateRule dateRule = new DateRule
				{
					or = new List<object>()
				};
				DueDateRule item11 = new DueDateRule
				{
					or = _dueDates
				};
				dateRule.or.Add(item11);
				CompletedTimeRule item12 = new CompletedTimeRule
				{
					or = _completedTimes
				};
				dateRule.or.Add(item12);
				normalRuleModel.and.Add(dateRule);
			}
			else
			{
				DueDateRule item13 = new DueDateRule
				{
					or = DueDates
				};
				normalRuleModel.and.Add(item13);
				if (DueDates.Any((string d) => d.StartsWith("offset")))
				{
					normalRuleModel.version = Math.Max(normalRuleModel.version, 6);
				}
				else if (DueDates.Any((string d) => d.StartsWith("span")))
				{
					normalRuleModel.version = Math.Max(normalRuleModel.version, 4);
				}
				else if (DueDates.Any((string d) => d.EndsWith("daysfromtoday") || d == "recurring"))
				{
					normalRuleModel.version = Math.Max(normalRuleModel.version, 3);
				}
			}
		}
		if (normalRuleModel.and.Count > 0)
		{
			if (isSave)
			{
				foreach (object item14 in normalRuleModel.and)
				{
					string text = null;
					if (!(item14 is ListOrGroupRule))
					{
						if (!(item14 is TagRule))
						{
							if (!(item14 is DueDateRule))
							{
								if (!(item14 is AssigneeRule))
								{
									if (!(item14 is PriorityRule))
									{
										if (!(item14 is TaskTypeRule))
										{
											if (item14 is KeywordsRule)
											{
												text = "content";
											}
										}
										else
										{
											text = "type";
										}
									}
									else
									{
										text = "priority";
									}
								}
								else
								{
									text = "assignee";
								}
							}
							else
							{
								text = "date";
							}
						}
						else
						{
							text = "tag";
						}
					}
					else
					{
						text = "project";
					}
					if (!string.IsNullOrEmpty(text))
					{
						UserActCollectUtils.AddClickEvent("edit_filter", "conditions", text);
					}
				}
			}
			return JsonConvert.SerializeObject(normalRuleModel);
		}
		return string.Empty;
	}
}
