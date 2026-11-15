using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Kanban;

public class KanbanViewModel : BaseViewModel
{
	private static readonly SemaphoreLocker LoadLock = new SemaphoreLocker();

	private bool _showAdd;

	private bool _enable = true;

	private string _name;

	public List<TaskBaseViewModel> SourceModels;

	public ProjectIdentity Identity;

	public readonly ClosedLoader CompletedTaskLoader = new ClosedLoader();

	private KanbanContainer _kanban;

	private bool _loading;

	public bool ShowAdd
	{
		get
		{
			string text = SortOption.groupBy;
			if (text == "none" && Identity is NormalProjectIdentity)
			{
				text = "sortOrder";
			}
			if (_showAdd && _enable)
			{
				return text == "sortOrder";
			}
			return false;
		}
		set
		{
			_showAdd = value;
			OnPropertyChanged("ShowAdd");
		}
	}

	public bool Enable
	{
		get
		{
			return _enable;
		}
		set
		{
			_enable = value;
			OnPropertyChanged("ShowAdd");
			OnPropertyChanged("Enable");
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			OnPropertyChanged("Name");
		}
	}

	public SortOption SortOption => Identity.SortOption;

	public KanbanViewModel(ProjectIdentity identity, KanbanContainer kanban)
	{
		_kanban = kanban;
		SetIdentity(identity);
	}

	public async Task LoadTasks(bool force = false, bool pullRemote = false)
	{
		if (_loading && !force)
		{
			return;
		}
		_loading = true;
		await LoadLock.LockAsync(async delegate
		{
			List<TaskBaseViewModel> sourceModels = await ProjectTaskDataProvider.GetDisplayModels(Identity);
			SourceModels = sourceModels;
			List<ColumnViewModel> list = (await GetColumns(force, pullRemote)) ?? new List<ColumnViewModel>();
			if (SortOption.groupByType != Constants.SortType.sortOrder)
			{
				list = GetDisplayColumns(list);
			}
			list.Sort((ColumnViewModel a, ColumnViewModel b) => (a.SortOrder != b.SortOrder) ? a.SortOrder.CompareTo(b.SortOrder) : a.SortOrder2.CompareTo(b.SortOrder2));
			KanbanContainer kanban = _kanban;
			if (kanban != null)
			{
				await kanban.SetupColumn(list);
			}
			_loading = false;
			if (pullRemote)
			{
				TryLoadCompletedTasks();
			}
		});
	}

	private List<ColumnViewModel> GetDisplayColumns(List<ColumnViewModel> columns)
	{
		List<ColumnViewModel> list = columns.Where((ColumnViewModel c) => c.SourceItems.Any()).ToList();
		ColumnViewModel columnViewModel = null;
		if (Identity.CanAddTask() && list.Count > 0 && list.All((ColumnViewModel c) => !c.CanAdd))
		{
			switch (SortOption.groupBy)
			{
			case "dueDate":
			{
				ProjectIdentity identity = Identity;
				if (!(identity is WeekProjectIdentity) && !(identity is TodayProjectIdentity))
				{
					if (identity is TomorrowProjectIdentity)
					{
						columnViewModel = columns.FirstOrDefault((ColumnViewModel c) => c.ColumnId == "date:1");
					}
				}
				else
				{
					columnViewModel = columns.FirstOrDefault((ColumnViewModel c) => c.ColumnId == "date:0");
				}
				break;
			}
			case "project":
			{
				string projectId = Identity.GetProjectId();
				if (projectId != null)
				{
					ProjectModel projectById = CacheManager.GetProjectById(projectId);
					columnViewModel = new ColumnViewModel
					{
						Name = projectById?.name,
						SortOrder = (projectById?.sortOrder ?? long.MinValue),
						ColumnId = "project:" + projectId,
						Color = projectById?.color,
						CanDropDown = true,
						CanAdd = true
					};
				}
				break;
			}
			case "tag":
			{
				List<string> tags = TaskDefaultDao.GetDefaultSafely().Tags;
				if (tags != null && tags.Count > 0)
				{
					TagModel tagByName = CacheManager.GetTagByName(TagDataHelper.GetPrimaryTag(null, tags, null)?.ToLower());
					if (tagByName != null)
					{
						columnViewModel = new ColumnViewModel
						{
							Name = tagByName.GetDisplayName(),
							SortOrder = tagByName.parentSortOrder,
							SortOrder2 = tagByName.sortOrder,
							ColumnId = "tag:" + tagByName.name,
							Color = tagByName.color,
							CanDropDown = false
						};
						break;
					}
				}
				columnViewModel = columns.FirstOrDefault((ColumnViewModel c) => c.ColumnId == "tag:#notag");
				break;
			}
			case "priority":
				columnViewModel = columns.FirstOrDefault((ColumnViewModel c) => c.ColumnId == "priority:" + TaskDefaultDao.GetDefaultSafely().Priority);
				break;
			}
		}
		if (columnViewModel != null)
		{
			columnViewModel.SetColumnIdentity(Identity);
			list.Add(columnViewModel);
		}
		return list;
	}

	private async Task<List<ColumnViewModel>> GetColumns(bool force = false, bool pullRemote = false)
	{
		string groupBy = SortOption.groupBy;
		bool flag = Identity is NormalProjectIdentity;
		if (flag && (groupBy == "none" || groupBy == "sortOrder"))
		{
			return await GetCustomColumns(force, pullRemote);
		}
		if (flag || !(groupBy == "none"))
		{
			switch (groupBy)
			{
			case "dueDate":
				break;
			case "priority":
				return GetPriorityColumns();
			case "tag":
				return GetTagColumns();
			case "project":
				return GetProjectColumns();
			case "assignee":
				return GetAssignColumns();
			case "createdTime":
				return GetCreateTimeColumns();
			default:
				if (flag)
				{
					return await GetCustomColumns(force, pullRemote);
				}
				return GetDateColumns();
			}
		}
		return GetDateColumns();
	}

	private List<ColumnViewModel> GetAssignColumns()
	{
		Dictionary<string, ColumnViewModel> dictionary = new Dictionary<string, ColumnViewModel>();
		dictionary.Add("assign:-1", new ColumnViewModel
		{
			Name = Utils.GetString("NotAssigned"),
			SortOrder = long.MaxValue,
			ColumnId = "assign:-1",
			CanDropDown = true
		});
		List<Node<TaskBaseViewModel>> list = TaskCache.GetTaskNodeDict(SourceModels).Values.Where((Node<TaskBaseViewModel> n) => !n.HasParent).ToList();
		GroupProjectIdentity groupProjectIdentity = Identity as GroupProjectIdentity;
		List<ProjectModel> source = ((groupProjectIdentity != null) ? CacheManager.GetProjectsInGroup(groupProjectIdentity.GroupId) : new List<ProjectModel>());
		List<AvatarViewModel> list2 = ((groupProjectIdentity != null) ? AvatarHelper.GetProjectAvatarsFromCacheInGroup(groupProjectIdentity.Id) : AvatarHelper.GetProjectAvatarsFromCache(Identity?.CatId, sort: true));
		foreach (Node<TaskBaseViewModel> item in list)
		{
			TaskBaseViewModel value = item.Value;
			if (value.Status != 0 && LocalSettings.Settings.HideComplete)
			{
				continue;
			}
			string text = "assign:-1";
			string userId = value.Assignee;
			if (string.IsNullOrEmpty(value.Assignee) || value.Assignee == "-1")
			{
				userId = "-1";
			}
			if (!dictionary.ContainsKey("assign:" + userId))
			{
				AvatarViewModel avatarViewModel = ((userId == "-1") ? null : list2.FirstOrDefault((AvatarViewModel u) => (u.UserId ?? "") == userId));
				if (avatarViewModel != null)
				{
					bool flag = true;
					if (groupProjectIdentity != null && source.Any((ProjectModel p) => !p.IsEnable()))
					{
						flag = ProjectDao.GetAssignProjectInGroup(groupProjectIdentity.Id, userId) != null;
					}
					text = "assign:" + avatarViewModel.UserId;
					dictionary.Add(text, new ColumnViewModel
					{
						Name = avatarViewModel.Name,
						SortOrder = ((avatarViewModel.UserId == LocalSettings.Settings.LoginUserId) ? long.MinValue : list2.IndexOf(avatarViewModel)),
						ColumnId = text,
						CanDropDown = flag,
						CanAdd = flag
					});
				}
			}
			else
			{
				text = "assign:" + userId;
			}
			ColumnViewModel columnViewModel = dictionary[text];
			columnViewModel.SourceItems.Add(value);
			List<Node<TaskBaseViewModel>> children = item.Children;
			if (children != null && children.Count > 0)
			{
				columnViewModel.SourceItems.AddRange(item.GetAllChildrenValue());
			}
		}
		foreach (ColumnViewModel value2 in dictionary.Values)
		{
			value2.SetColumnIdentity(Identity);
		}
		return dictionary.Values.ToList();
	}

	private List<ColumnViewModel> GetTagColumns()
	{
		Dictionary<string, ColumnViewModel> dictionary = new Dictionary<string, ColumnViewModel>();
		dictionary.Add("tag:#notag", new ColumnViewModel
		{
			Name = Utils.GetString("NoTags"),
			SortOrder = 9223372036854775806L,
			ColumnId = "tag:#notag",
			CanDropDown = false
		});
		ColumnViewModel habitColumn = ColumnViewModel.GetHabitColumn(Identity is WeekProjectIdentity);
		dictionary.Add(habitColumn.ColumnId, habitColumn);
		ColumnViewModel countdownColumn = ColumnViewModel.GetCountdownColumn();
		dictionary.Add(countdownColumn.ColumnId, countdownColumn);
		Dictionary<string, Node<TaskBaseViewModel>> taskNodeDict = TaskCache.GetTaskNodeDict(SourceModels);
		Dictionary<string, long> tagSortDict = TagDataHelper.GetTagSortDict();
		List<string> limits = null;
		ProjectIdentity identity = Identity;
		TagProjectIdentity tagId = identity as TagProjectIdentity;
		if (tagId != null)
		{
			limits = (from t in TagDataHelper.GetTags()
				where t.parent == tagId.Tag || t.name == tagId.Tag
				select t.name).ToList();
		}
		foreach (Node<TaskBaseViewModel> value2 in taskNodeDict.Values)
		{
			TaskBaseViewModel value = value2.Value;
			if (value.IsHabit)
			{
				if (value.Status == 0 || !LocalSettings.Settings.HideComplete)
				{
					habitColumn.SourceItems.Add(value);
				}
			}
			else if (value.IsCountdown)
			{
				countdownColumn.SourceItems.Add(value);
			}
			else
			{
				if (value2.HasParent || (value.Status != 0 && LocalSettings.Settings.HideComplete))
				{
					continue;
				}
				string text = "tag:#notag";
				string text2 = TagDataHelper.GetPrimaryTag(tagSortDict, value.Tags?.ToList(), limits)?.ToLower();
				if (!string.IsNullOrEmpty(text2))
				{
					TagModel tagByName = CacheManager.GetTagByName(text2);
					if (tagByName != null)
					{
						text = text2;
						tagByName.SetParentSortOrder();
						if (!dictionary.ContainsKey(text))
						{
							dictionary.Add(text, new ColumnViewModel
							{
								Name = tagByName.GetDisplayName(),
								SortOrder = tagByName.parentSortOrder,
								SortOrder2 = tagByName.sortOrder,
								ColumnId = "tag:" + text,
								Color = tagByName.color,
								CanDropDown = false
							});
						}
					}
				}
				ColumnViewModel columnViewModel = dictionary[text];
				columnViewModel.SourceItems.Add(value);
				List<Node<TaskBaseViewModel>> children = value2.Children;
				if (children != null && children.Count > 0)
				{
					columnViewModel.SourceItems.AddRange(value2.GetAllChildrenValue());
				}
			}
		}
		foreach (ColumnViewModel value3 in dictionary.Values)
		{
			value3.SetColumnIdentity(Identity);
		}
		return dictionary.Values.ToList();
	}

	private List<ColumnViewModel> GetDateColumns()
	{
		bool flag = Identity is WeekProjectIdentity;
		bool flag2 = Identity is TomorrowProjectIdentity;
		bool flag3 = Identity is TodayProjectIdentity;
		List<ColumnViewModel> list = (flag ? ColumnViewModel.GetWeekDateColumnViewModels() : ColumnViewModel.GetDateColumnViewModels());
		ColumnViewModel countdownColumn = ColumnViewModel.GetCountdownColumn();
		list.Add(countdownColumn);
		ColumnViewModel noteColumn = ColumnViewModel.GetNoteColumn();
		list.Add(noteColumn);
		ColumnViewModel habitColumn = ColumnViewModel.GetHabitColumn(flag);
		list.Add(habitColumn);
		foreach (ColumnViewModel item in list)
		{
			item.SetColumnIdentity(Identity);
		}
		Dictionary<string, ColumnViewModel> dictionary = list.ToDictionary((ColumnViewModel vm) => vm.ColumnId, (ColumnViewModel vm) => vm);
		if (flag)
		{
			dictionary["habit"].SortOrder = 2L;
		}
		foreach (Node<TaskBaseViewModel> value2 in TaskCache.GetTaskNodeDict(SourceModels).Values)
		{
			TaskBaseViewModel value = value2.Value;
			if (value.IsNote)
			{
				noteColumn.SourceItems.Add(value);
			}
			else if (value.IsHabit)
			{
				if (value.Status == 0 || !LocalSettings.Settings.HideComplete)
				{
					habitColumn.SourceItems.Add(value);
				}
			}
			else if (value.IsCountdown)
			{
				countdownColumn.SourceItems.Add(value);
			}
			else if (value.IsNote)
			{
				noteColumn.SourceItems.Add(value);
			}
			else
			{
				if ((value.IsTask && value2.HasParent) || (value.Status != 0 && LocalSettings.Settings.HideComplete))
				{
					continue;
				}
				ColumnViewModel columnViewModel = ((flag3 || flag) ? dictionary["date:0"] : dictionary["date:no"]);
				if (flag2)
				{
					columnViewModel = dictionary["date:1"];
				}
				else
				{
					switch (DateUtils.GetSectionCategory(value.StartDate, value.DueDate, value.IsAllDay))
					{
					case DateUtils.DateSectionCategory.OutDated:
						columnViewModel = dictionary["date:-1"];
						break;
					case DateUtils.DateSectionCategory.Today:
						columnViewModel = dictionary["date:0"];
						break;
					case DateUtils.DateSectionCategory.Tomorrow:
						columnViewModel = (flag3 ? dictionary["date:0"] : dictionary["date:1"]);
						break;
					case DateUtils.DateSectionCategory.ThisWeek:
						if (flag)
						{
							int num = (value.StartDate?.Date - DateTime.Today)?.Days ?? 0;
							columnViewModel = ((num < 0 || num > 6) ? dictionary["date:-1"] : dictionary["date:" + num]);
						}
						else
						{
							columnViewModel = (flag3 ? dictionary["date:0"] : dictionary["date:week"]);
						}
						break;
					case DateUtils.DateSectionCategory.Future:
						columnViewModel = ((flag3 || flag) ? dictionary["date:0"] : dictionary["date:later"]);
						break;
					}
				}
				columnViewModel.SourceItems.Add(value);
				columnViewModel.SourceItems.AddRange(value2.GetAllChildrenValue());
			}
		}
		return list;
	}

	private List<ColumnViewModel> GetPriorityColumns()
	{
		List<ColumnViewModel> priorityColumnViewModels = ColumnViewModel.GetPriorityColumnViewModels();
		ColumnViewModel countdownColumn = ColumnViewModel.GetCountdownColumn();
		priorityColumnViewModels.Add(countdownColumn);
		ColumnViewModel noteColumn = ColumnViewModel.GetNoteColumn();
		priorityColumnViewModels.Add(noteColumn);
		ColumnViewModel habitColumn = ColumnViewModel.GetHabitColumn(Identity is WeekProjectIdentity);
		priorityColumnViewModels.Add(habitColumn);
		foreach (ColumnViewModel item in priorityColumnViewModels)
		{
			item.SetColumnIdentity(Identity);
		}
		Dictionary<string, ColumnViewModel> dictionary = priorityColumnViewModels.ToDictionary((ColumnViewModel vm) => vm.ColumnId, (ColumnViewModel vm) => vm);
		foreach (Node<TaskBaseViewModel> value2 in TaskCache.GetTaskNodeDict(SourceModels).Values)
		{
			TaskBaseViewModel value = value2.Value;
			if (value.IsNote)
			{
				noteColumn.SourceItems.Add(value);
			}
			else if (value.IsHabit)
			{
				if (value.Status == 0 || !LocalSettings.Settings.HideComplete)
				{
					habitColumn.SourceItems.Add(value);
				}
			}
			else if (value.IsCountdown)
			{
				countdownColumn.SourceItems.Add(value);
			}
			else if (!value2.HasParent && (value.Status == 0 || !LocalSettings.Settings.HideComplete))
			{
				string key = "priority:" + value.Priority;
				ColumnViewModel obj = (dictionary.ContainsKey(key) ? dictionary[key] : dictionary["priority:" + 0]);
				obj.SourceItems.Add(value);
				obj.SourceItems.AddRange(value2.GetAllChildrenValue());
			}
		}
		return priorityColumnViewModels;
	}

	private List<ColumnViewModel> GetCreateTimeColumns()
	{
		Dictionary<string, ColumnViewModel> dictionary = new Dictionary<string, ColumnViewModel>();
		ColumnViewModel countdownColumn = ColumnViewModel.GetCountdownColumn();
		dictionary.Add(countdownColumn.ColumnId, countdownColumn);
		ColumnViewModel habitColumn = ColumnViewModel.GetHabitColumn(Identity is WeekProjectIdentity);
		dictionary.Add(habitColumn.ColumnId, habitColumn);
		ColumnViewModel columnViewModel = new ColumnViewModel
		{
			Name = Utils.GetString("SubscribeCalendar"),
			SortOrder = 9223372036854775802L,
			ColumnId = "calendar",
			CanDropDown = false,
			CanAdd = false
		};
		dictionary.Add(columnViewModel.ColumnId, columnViewModel);
		Dictionary<string, Node<TaskBaseViewModel>> taskNodeDict = TaskCache.GetTaskNodeDict(SourceModels);
		List<ProjectModel> projects = CacheManager.GetProjects();
		projects.Sort((ProjectModel l, ProjectModel r) => l.CompareTo(r));
		foreach (Node<TaskBaseViewModel> value in taskNodeDict.Values)
		{
			try
			{
				TaskBaseViewModel model = value.Value;
				if (model.IsHabit)
				{
					if (model.Status == 0 || !LocalSettings.Settings.HideComplete)
					{
						habitColumn.SourceItems.Add(model);
					}
				}
				else if (model.IsCountdown)
				{
					countdownColumn.SourceItems.Add(model);
				}
				else if (model.ProjectId == "8ac3038d93c54b80a67321b6a03df066")
				{
					columnViewModel.SourceItems.Add(model);
				}
				else
				{
					if (value.HasParent || (model.Status != 0 && LocalSettings.Settings.HideComplete) || !model.CreatedTime.HasValue)
					{
						continue;
					}
					DateUtils.CreateTimeCategory createTimeSectionCategory = DateUtils.GetCreateTimeSectionCategory(model.CreatedTime.Value);
					string text = (model.IsEvent ? "calendar" : (model.IsCourse ? "course" : (model.IsHabit ? Utils.GetString("Habit") : DateUtils.GetCreateTimeSectionEntityId(createTimeSectionCategory))));
					if (!dictionary.ContainsKey(text))
					{
						bool flag = projects.FirstOrDefault((ProjectModel p) => p.id == model.ProjectId)?.IsEnable() ?? false;
						string text2 = string.Empty;
						switch (createTimeSectionCategory)
						{
						case DateUtils.CreateTimeCategory.Today:
							text2 = Utils.GetString("Today") + ", " + Utils.GetString(DateTime.Today.DayOfWeek.ToString().Substring(0, 3));
							break;
						case DateUtils.CreateTimeCategory.Yesterday:
							text2 = Utils.GetString("PublicYesterday") + ", " + Utils.GetString(DateTime.Today.AddDays(-1.0).DayOfWeek.ToString().Substring(0, 3));
							break;
						case DateUtils.CreateTimeCategory.LastWeek:
							text2 = Utils.GetString("Previous7Days");
							break;
						case DateUtils.CreateTimeCategory.LastMonth:
							text2 = Utils.GetString("Previous30Days");
							break;
						case DateUtils.CreateTimeCategory.Earlier:
							text2 = Utils.GetString("Earlier");
							break;
						}
						dictionary.Add(text, new ColumnViewModel
						{
							Name = (model.IsEvent ? Utils.GetString("SubscribeCalendar") : (model.IsCourse ? Utils.GetString("Timetable") : (model.IsHabit ? Utils.GetString("Habit") : text2))),
							SortOrder = (model.IsEvent ? 9223372036854775805L : (model.IsCourse ? 9223372036854775806L : (model.IsHabit ? long.MaxValue : ((long)(createTimeSectionCategory + 1))))),
							ColumnId = text,
							CanDropDown = flag,
							CanAdd = flag
						});
					}
					ColumnViewModel columnViewModel2 = dictionary[text];
					columnViewModel2.SourceItems.Add(model);
					List<Node<TaskBaseViewModel>> children = value.Children;
					if (children != null && children.Count > 0)
					{
						columnViewModel2.SourceItems.AddRange(value.GetAllChildrenValue());
					}
					continue;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}
		List<ColumnViewModel> list = dictionary.Values.ToList();
		foreach (ColumnViewModel item in list)
		{
			item.SetColumnIdentity(Identity);
		}
		return list;
	}

	private async Task<List<ColumnViewModel>> GetCustomColumns(bool force = false, bool pullRemote = false)
	{
		Dictionary<string, ColumnViewModel> vms = new Dictionary<string, ColumnViewModel>();
		if (Identity is NormalProjectIdentity { Project: not null } normalProjectIdentity)
		{
			ProjectModel project = normalProjectIdentity.Project;
			List<ColumnModel> columns = await ColumnDao.GetColumnsByProjectId(project.id);
			if (columns == null || !columns.Any())
			{
				if (project.IsShareList() || (project.closed.HasValue && project.closed.Value) || force)
				{
					if (await ColumnBatchHandler.MergeWithServer(project.id))
					{
						return null;
					}
					pullRemote = false;
				}
				if ((columns == null || !columns.Any()) && SourceModels.Count > 0)
				{
					await ColumnDao.InitColumns(project.id);
					columns = await ColumnDao.GetColumnsByProjectId(project.id);
				}
			}
			if (columns == null || !columns.Any())
			{
				return null;
			}
			columns.Sort((ColumnModel a, ColumnModel b) => (a.sortOrder ?? long.MinValue).CompareTo(b.sortOrder ?? long.MinValue));
			if (LocalSettings.Settings.ExtraSettings.KbShowColumnColor && project.IsEnable())
			{
				if (await InitializeCustomColumnColors(columns))
				{
					SyncManager.TryDelaySync();
				}
			}
			else
			{
				InitializeTransientCustomColumnColors(columns);
			}
			string id = columns[0].id;
			foreach (ColumnModel item in columns)
			{
				ColumnProjectIdentity identity = new ColumnProjectIdentity(Identity, item.id);
				ColumnViewModel value = new ColumnViewModel(item)
				{
					Identity = identity,
					Enable = project.IsEnable(),
					CanAdd = project.IsEnable()
				};
				vms.Add(item.id, value);
			}
			Dictionary<string, Node<TaskBaseViewModel>> taskNodeDict = TaskCache.GetTaskNodeDict(SourceModels);
			bool flag = false;
			foreach (Node<TaskBaseViewModel> value3 in taskNodeDict.Values)
			{
				if (value3.HasParent && value3.Value.ColumnId != value3.Parent.Value.ColumnId && !string.IsNullOrEmpty(value3.Parent.Value.ColumnId))
				{
					string cId = value3.Parent.Value.ColumnId;
					value3.GetAllChildrenValue().ForEach(delegate(TaskBaseViewModel m)
					{
						m.ColumnId = cId;
					});
					value3.Value.ColumnId = cId;
				}
				TaskBaseViewModel value2 = value3.Value;
				if (string.IsNullOrEmpty(value2.ColumnId) || !vms.ContainsKey(value2.ColumnId))
				{
					ColumnViewModel columnViewModel = vms[id];
					if (!string.IsNullOrEmpty(value2.ColumnId) && !vms.ContainsKey(value2.ColumnId))
					{
						UtilLog.Info("KanbanTaskNoColumn : t:" + value2.Id + " c:" + value2.ColumnId + " tp:" + value2.ProjectId + " cp:" + columnViewModel.ProjectId);
						flag = true;
					}
					if (columnViewModel.ProjectId == value2.ProjectId)
					{
						value2.ColumnId = id;
						if (string.IsNullOrEmpty(value2.ColumnId))
						{
							TaskService.BatchSetColumn(new List<string> { value2.Id }, id);
						}
					}
					columnViewModel.SourceItems.Add(value2);
				}
				else
				{
					vms[value2.ColumnId].SourceItems.Add(value2);
				}
			}
			if (flag)
			{
				UtilLog.Info("KanbanTaskNoColumn : currentColumns " + vms.Keys.ToList().Join(","));
			}
			if (force && pullRemote)
			{
				Task.Run(delegate
				{
					ColumnBatchHandler.MergeWithServer(project.id);
				});
			}
		}
		return vms.Values.ToList();
	}

	private List<ColumnViewModel> GetProjectColumns()
	{
		Dictionary<string, ColumnViewModel> dictionary = new Dictionary<string, ColumnViewModel>();
		ColumnViewModel countdownColumn = ColumnViewModel.GetCountdownColumn();
		dictionary.Add(countdownColumn.ColumnId, countdownColumn);
		ColumnViewModel habitColumn = ColumnViewModel.GetHabitColumn(Identity is WeekProjectIdentity);
		dictionary.Add(habitColumn.ColumnId, habitColumn);
		Dictionary<string, Node<TaskBaseViewModel>> taskNodeDict = TaskCache.GetTaskNodeDict(SourceModels);
		List<ProjectModel> projects = CacheManager.GetProjects();
		projects.Sort((ProjectModel l, ProjectModel r) => l.CompareTo(r));
		foreach (Node<TaskBaseViewModel> value in taskNodeDict.Values)
		{
			TaskBaseViewModel model = value.Value;
			if (model.IsHabit)
			{
				if (model.Status == 0 || !LocalSettings.Settings.HideComplete)
				{
					habitColumn.SourceItems.Add(model);
				}
			}
			else if (model.IsCountdown)
			{
				countdownColumn.SourceItems.Add(model);
			}
			else
			{
				if (value.HasParent || (model.Status != 0 && LocalSettings.Settings.HideComplete))
				{
					continue;
				}
				string text = "project:" + model.ProjectId;
				if (model.IsEvent)
				{
					text = "calendar";
				}
				if (model.IsCourse)
				{
					text = "course";
				}
				if (model.IsHabit)
				{
					text = "habit";
				}
				if (!dictionary.ContainsKey(text))
				{
					ProjectModel projectModel = projects.FirstOrDefault((ProjectModel p) => p.id == model.ProjectId);
					bool flag = projectModel?.IsEnable() ?? false;
					dictionary.Add(text, new ColumnViewModel
					{
						Name = (model.IsEvent ? Utils.GetString("SubscribeCalendar") : (model.IsCourse ? Utils.GetString("Timetable") : (model.IsHabit ? Utils.GetString("Habit") : projectModel?.name))),
						SortOrder = (model.IsEvent ? 9223372036854775805L : (model.IsCourse ? 9223372036854775806L : (model.IsHabit ? long.MaxValue : projects.IndexOf(projectModel)))),
						ColumnId = text,
						Color = projectModel?.color,
						CanDropDown = flag,
						CanAdd = flag
					});
				}
				ColumnViewModel columnViewModel = dictionary[text];
				columnViewModel.SourceItems.Add(model);
				List<Node<TaskBaseViewModel>> children = value.Children;
				if (children != null && children.Count > 0)
				{
					columnViewModel.SourceItems.AddRange(value.GetAllChildrenValue());
				}
			}
		}
		List<ColumnViewModel> list = dictionary.Values.ToList();
		foreach (ColumnViewModel item in list)
		{
			item.SetColumnIdentity(Identity);
		}
		return list;
	}

	private static async Task<bool> InitializeCustomColumnColors(IList<ColumnModel> columns)
	{
		List<ColumnModel> updatedColumns = AssignMissingCustomColumnColors(columns);
		foreach (ColumnModel item in updatedColumns)
		{
			item.modifiedTime = DateTime.Now;
			await ColumnDao.UpdateColumn(item);
		}
		return updatedColumns.Count > 0;
	}

	private static void InitializeTransientCustomColumnColors(IList<ColumnModel> columns)
	{
		AssignMissingCustomColumnColors(columns);
	}

	private static List<ColumnModel> AssignMissingCustomColumnColors(IList<ColumnModel> columns)
	{
		List<ColumnModel> list = new List<ColumnModel>();
		if (columns == null || columns.Count == 0)
		{
			return list;
		}
		bool flag = columns.All((ColumnModel column) => KanbanColumnColorHelper.NeedsDefaultCustomColor(column.color));
		List<string> list2 = columns.Select((ColumnModel column) => column.color).ToList();
		for (int num = 0; num < columns.Count; num++)
		{
			ColumnModel columnModel = columns[num];
			if (KanbanColumnColorHelper.NeedsDefaultCustomColor(columnModel.color))
			{
				columnModel.color = (flag ? KanbanColumnColorHelper.GetInitialCustomColor(num) : KanbanColumnColorHelper.SelectColorForInsertion(list2, num));
				list2[num] = columnModel.color;
				list.Add(columnModel);
			}
		}
		return list;
	}

	public void SetIdentity(ProjectIdentity identity)
	{
		Name = identity.GetDisplayTitle();
		if (identity is NormalProjectIdentity normalProjectIdentity)
		{
			ShowAdd = normalProjectIdentity.Project?.ShowAddColumn ?? false;
			Enable = normalProjectIdentity.Project?.IsEnable() ?? false;
		}
		else
		{
			ShowAdd = false;
		}
		Identity = identity;
	}

	private async void TryLoadCompletedTasks()
	{
		if (await CompletedTaskLoader.NeedPullCompletedTasks(Identity) && await CompletedTaskLoader.TryLoadCompletedTasks(Identity))
		{
			await LoadTasks();
		}
	}

	public void Dispose()
	{
		_kanban = null;
	}
}
