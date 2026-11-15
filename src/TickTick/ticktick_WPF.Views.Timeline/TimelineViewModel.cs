using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Timeline;

public class TimelineViewModel : BaseViewModel
{
	public static readonly TimelineViewModel Instance = new TimelineViewModel();

	private ClosedTaskWithFilterLoader _closedLoader;

	private readonly SemaphoreSlim _updateLineSemaphoreSlim = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim _updateArrangeSemaphoreSlim = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim _updateGroupSemaphoreSlim = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim _updateLeftSemaphoreSlim = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim _cellLock = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim _resetAllSemaphoreSlim = new SemaphoreSlim(1, 1);

	private bool _hasGroup;

	private bool _isArranging;

	private bool _isSetting;

	private int _lineCount;

	private double _xOffset;

	private double _yearNameWidth = double.PositiveInfinity;

	private string _yearName;

	private Visibility _arrangeEmptyTaskVisibility = Visibility.Collapsed;

	private bool _isDark = ThemeKey.IsDarkTheme(LocalSettings.Settings.ThemeId);

	private Dictionary<int, bool> _groupDictModels = new Dictionary<int, bool>();

	private double _groupWidth = 140.0;

	private bool _gotoBtnEnabled = true;

	private double _viewWidth;

	private Tuple<DateTime, DateTime> _hoverStartEndTuple;

	private bool _highLightHover;

	private bool _availableReset;

	private bool _itemEditing;

	private string _uid;

	private ObservableCollection<TimelineDisplayBase> _arrangeModels = new ObservableCollection<TimelineDisplayBase>();

	private readonly HashSet<string> _foldIds = new HashSet<string>();

	private readonly BlockingSet<TimelineCellViewModel> _batchSelectedModels = new BlockingSet<TimelineCellViewModel>();

	private readonly BlockingSet<TimelineCellViewModel> _tempBatchSelectedModels = new BlockingSet<TimelineCellViewModel>();

	private double _checkedWidth;

	private double _checkedHeight;

	public ObservableCollection<TimelineYearViewModel> YearModels { get; } = new ObservableCollection<TimelineYearViewModel>();

	public BlockingList<TimelineCellViewModel> CellModels { get; } = new BlockingList<TimelineCellViewModel>();

	public TimeLineItemList AvailableModels { get; } = new TimeLineItemList();

	public ObservableCollection<TimelineGroupViewModel> GroupModels { get; } = new ObservableCollection<TimelineGroupViewModel>();

	public Dictionary<int, bool> GroupDictModels
	{
		get
		{
			return _groupDictModels;
		}
		set
		{
			_groupDictModels = value;
			OnPropertyChanged("GroupDictModels");
		}
	}

	public double GroupWidthMin => 100.0;

	public double GroupWidthMax { get; set; }

	public double ViewWidth
	{
		get
		{
			return _viewWidth;
		}
		set
		{
			ChangeAndNotify(ref _viewWidth, value, "ViewWidth");
			if (_viewWidth > 100.0)
			{
				GroupWidthMax = _viewWidth / 3.0;
				OnPropertyChanged("GroupWidthMax");
				if (GroupWidth > GroupWidthMax)
				{
					GroupWidth = GroupWidthMax;
				}
			}
		}
	}

	public DateTime StartDate { get; private set; }

	public DateTime EndDate { get; private set; }

	public Tuple<DateTime, DateTime> StartEndTuple { get; private set; } = new Tuple<DateTime, DateTime>(default(DateTime), default(DateTime));

	public bool HighLightHover
	{
		get
		{
			return _highLightHover;
		}
		set
		{
			_highLightHover = value;
			OnPropertyChanged("HighLightHover");
		}
	}

	public Tuple<DateTime, DateTime> HoverStartEndTuple
	{
		get
		{
			return _hoverStartEndTuple;
		}
		set
		{
			_hoverStartEndTuple = value;
			OnPropertyChanged("HoverStartEndTuples");
		}
	}

	public List<Tuple<DateTime, DateTime>> HoverStartEndTuples
	{
		get
		{
			if (_batchSelectedModels.Count == 0)
			{
				if (_hoverStartEndTuple != null)
				{
					return new List<Tuple<DateTime, DateTime>> { _hoverStartEndTuple };
				}
				return null;
			}
			HashSet<DateTime> hashSet = new HashSet<DateTime>();
			foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
			{
				if (AvailableModels.Contains(item) || _highLightHover)
				{
					hashSet.Add(item.StartDate.Date);
					if (item.EndDate.HasValue)
					{
						hashSet.Add(item.IsAllDay ? item.EndDate.Value.AddSeconds(-1.0).Date : item.EndDate.Value.Date);
					}
				}
			}
			return new List<Tuple<DateTime, DateTime>>
			{
				new Tuple<DateTime, DateTime>(hashSet.Min(), hashSet.Max())
			};
		}
	}

	public ProjectIdentity ProjectIdentity { get; private set; }

	public bool ProjectEnable
	{
		get
		{
			if (ProjectIdentity is NormalProjectIdentity normalProjectIdentity)
			{
				return normalProjectIdentity.Project.IsEnable();
			}
			return true;
		}
	}

	public string ProjectTitle { get; private set; }

	public TimelineColorType ColorType
	{
		get
		{
			if (Enum.TryParse<TimelineColorType>(LocalSettings.Settings.TimelineSettingsModel?.Color ?? string.Empty, ignoreCase: true, out var result))
			{
				return result;
			}
			return TimelineColorType.List;
		}
		set
		{
			string color = LocalSettings.Settings.TimelineSettingsModel.Color;
			string name = value.GetName();
			if (color != name)
			{
				LocalSettings.Settings.TimelineSettingsModel.Color = name;
				LocalSettings.Settings.TimelineSettingsModel.mtime = Utils.GetNowTimeStampInMills();
				LocalSettings.Settings.Save();
				SettingsHelper.PushLocalPreference();
				GlobalEventManager.NotifyTimelineSetChanged("Color");
			}
		}
	}

	public double HeadHeight
	{
		get
		{
			if (!ShowWeek)
			{
				return 28.0;
			}
			return 42.0;
		}
	}

	public bool ShowWeek
	{
		get
		{
			return LocalSettings.Settings.TimelineSettingsModel?.ShowWeek ?? false;
		}
		set
		{
			if (LocalSettings.Settings.UserPreference != null && LocalSettings.Settings.TimelineSettingsModel?.ShowWeek != value)
			{
				if (LocalSettings.Settings.UserPreference.Timeline == null)
				{
					LocalSettings.Settings.UserPreference.Timeline = new TimelineSettingsModel();
				}
				LocalSettings.Settings.UserPreference.Timeline.ShowWeek = value;
				LocalSettings.Settings.UserPreference.Timeline.mtime = Utils.GetNowTimeStampInMills();
				LocalSettings.Settings.Save();
				SettingsHelper.PushLocalPreference();
				GlobalEventManager.NotifyTimelineSetChanged("ShowWeek");
			}
		}
	}

	public double GroupWidth
	{
		get
		{
			return _groupWidth;
		}
		set
		{
			if (GroupWidthMin <= value && value <= GroupWidthMax)
			{
				_groupWidth = value;
				OnPropertyChanged("GroupWidth");
			}
		}
	}

	public bool ShowGroup
	{
		get
		{
			return _hasGroup;
		}
		set
		{
			_hasGroup = value;
			OnPropertyChanged("ShowGroup");
		}
	}

	public bool IsArranging
	{
		get
		{
			return _isArranging;
		}
		set
		{
			_isArranging = value;
			if (_isArranging)
			{
				SetArrangeModels();
			}
			OnPropertyChanged("IsArranging");
		}
	}

	public bool IsSetting
	{
		get
		{
			return _isSetting;
		}
		set
		{
			_isSetting = value;
			OnPropertyChanged("IsSetting");
		}
	}

	public int TimelineRangeIndex
	{
		get
		{
			TimelineModel timelineModel = GetTimelineModel();
			if (timelineModel != null && timelineModel.DayWidthIndex > 0)
			{
				if (TimelineConstants.GetDayWidthRange(timelineModel.DayWidthIndex) != timelineModel.Range)
				{
					return TimelineConstants.GetRangeDefaultWidthIndex(timelineModel.Range);
				}
				return timelineModel.DayWidthIndex;
			}
			return TimelineConstants.GetRangeDefaultWidthIndex(timelineModel?.Range ?? TimelineConstants.RangeDefault);
		}
	}

	public string TimelineRange
	{
		get
		{
			return GetTimelineModel()?.Range ?? TimelineConstants.RangeDefault;
		}
		set
		{
			int rangeDefaultWidthIndex = TimelineConstants.GetRangeDefaultWidthIndex(value);
			TimelineModel timelineModel = GetTimelineModel();
			if (timelineModel != null && (timelineModel.Range != value || timelineModel.DayWidthIndex != rangeDefaultWidthIndex))
			{
				timelineModel.Range = value;
				timelineModel.DayWidthIndex = rangeDefaultWidthIndex;
				ProjectIdentity?.CommitTimeline(timelineModel);
				foreach (TimelineCellViewModel item in AvailableModels.ToList())
				{
					item.SetWidth();
				}
			}
			OnTimelineRangeChanged();
			UpdateYearModel(updateModels: true, updateLeft: true);
			UpdateCellLeftAsync();
		}
	}

	public Constants.SortType OrderByEnum
	{
		get
		{
			if (!Enum.TryParse<Constants.SortType>(TimelineSortOption.orderBy, out var result))
			{
				return Constants.SortType.sortOrder;
			}
			return result;
		}
	}

	public Constants.SortType GroupByEnum
	{
		get
		{
			if (!Enum.TryParse<Constants.SortType>(TimelineSortOption.groupBy, out var result))
			{
				return Constants.SortType.none;
			}
			return result;
		}
	}

	public bool ShowShareGrid
	{
		get
		{
			if (ProjectIdentity is NormalProjectIdentity normalProjectIdentity)
			{
				return normalProjectIdentity.Project?.IsShareList() ?? false;
			}
			return false;
		}
	}

	public bool ShowShareIcon
	{
		get
		{
			if (ProjectIdentity is NormalProjectIdentity normalProjectIdentity)
			{
				return !(normalProjectIdentity.Project?.IsShareList() ?? false);
			}
			return false;
		}
	}

	public bool CanShowActivity
	{
		get
		{
			if (ProjectIdentity is NormalProjectIdentity normalProjectIdentity)
			{
				return normalProjectIdentity.Project?.IsEnable() ?? false;
			}
			return false;
		}
	}

	public bool CanShowCheckItem
	{
		get
		{
			if (ProjectIdentity is FilterProjectIdentity filterProjectIdentity)
			{
				return filterProjectIdentity.Filter.ContainsDate();
			}
			return false;
		}
	}

	public string TimelinePermission
	{
		get
		{
			if (ProjectIdentity is NormalProjectIdentity { Project: not null } normalProjectIdentity)
			{
				return normalProjectIdentity.Project.permission;
			}
			return string.Empty;
		}
	}

	public SortOption TimelineSortOption
	{
		get
		{
			return GetTimelineModel()?.GetSortOption() ?? new SortOption
			{
				groupBy = ((ProjectIdentity is NormalProjectIdentity) ? "sortOrder" : "project"),
				orderBy = "sortOrder"
			};
		}
		set
		{
			TimelineModel timelineModel = GetTimelineModel();
			if (timelineModel != null && !value.Equal(timelineModel.sortOption))
			{
				timelineModel.sortOption = value;
				if (timelineModel.sortOption != null)
				{
					timelineModel.SortType = ((timelineModel.sortOption.groupByType == Constants.SortType.none) ? timelineModel.sortOption.orderBy : timelineModel.sortOption.groupBy);
				}
				ProjectIdentity?.CommitTimeline(timelineModel);
				DataChangedNotifier.NotifySortOptionChanged(ProjectIdentity?.CatId);
			}
		}
	}

	public double OneDayWidth => TimelineConstants.GetOneDayWidth(TimelineRangeIndex);

	public double MinChangeDays => TimelineConstants.GetMinChangeDays(TimelineRange);

	public bool NeedsSplitLine => TimelineConstants.GetNeedsSplitLine(TimelineRange);

	public int NewTaskDefaultDays => TimelineConstants.GetNewTaskDefaultDays(TimelineRange);

	public double OneLineHeight => 40.0;

	public double Height => OneLineHeight * (double)LineCount;

	public double Width
	{
		get
		{
			var (dateTime3, dateTime4) = StartEndTuple;
			return (double)(dateTime4 - dateTime3).Days * OneDayWidth;
		}
	}

	public int LineCount
	{
		get
		{
			return _lineCount;
		}
		set
		{
			_lineCount = value;
			OnPropertyChanged("LineCount");
			OnPropertyChanged("Height");
		}
	}

	public double XOffset
	{
		get
		{
			return _xOffset;
		}
		set
		{
			_xOffset = value;
			UpdateYearModel(updateModels: false, updateLeft: true);
			DelayActionHandlerCenter.TryDoAction(_uid + "TimelineXOffset", delegate
			{
				ThreadUtil.DetachedRunOnUiThread(delegate
				{
					OnPropertyChanged("XOffset");
				});
			}, 200);
		}
	}

	public double YearNameWidth
	{
		get
		{
			return _yearNameWidth;
		}
		set
		{
			ChangeAndNotify(ref _yearNameWidth, value, "YearNameWidth");
		}
	}

	public string YearName
	{
		get
		{
			return _yearName;
		}
		set
		{
			if (_yearName != value)
			{
				_yearName = value;
				OnPropertyChanged("YearName");
			}
		}
	}

	public bool HideCompleted
	{
		get
		{
			return LocalSettings.Settings.HideComplete;
		}
		set
		{
			LocalSettings.Settings.HideComplete = value;
			LocalSettings.Settings.Save();
			SettingsHelper.PushLocalSettings();
		}
	}

	public bool IsOverDue
	{
		get
		{
			return LocalSettings.Settings.ExtraSettings.TimelineArrangeOverDue;
		}
		set
		{
			LocalSettings.Settings.ExtraSettings.TimelineArrangeOverDue = value;
			OnPropertyChanged("IsOverDue");
			SetArrangeModels();
			LocalSettings.Settings.Save(force: true);
		}
	}

	public Visibility ArrangeEmptyTaskVisibility
	{
		get
		{
			return _arrangeEmptyTaskVisibility;
		}
		set
		{
			ChangeAndNotify(ref _arrangeEmptyTaskVisibility, value, "ArrangeEmptyTaskVisibility");
		}
	}

	public bool IsDark
	{
		get
		{
			return _isDark;
		}
		set
		{
			_isDark = value;
			OnPropertyChanged("IsDark");
			foreach (TimelineCellViewModel item in CellModels.ToList())
			{
				item.NotifyColorChanged();
			}
		}
	}

	public bool GotoBtnEnabled
	{
		get
		{
			return _gotoBtnEnabled;
		}
		set
		{
			_gotoBtnEnabled = value;
			OnPropertyChanged("GotoBtnEnabled");
		}
	}

	public bool GroupEditing { get; private set; }

	public bool AvailableReset
	{
		get
		{
			return _availableReset;
		}
		set
		{
			ChangeAndNotify(ref _availableReset, value, "AvailableReset");
		}
	}

	public bool Editing { get; set; }

	public bool Hovering { get; set; }

	public bool ShowNoteInArrange
	{
		get
		{
			return LocalSettings.Settings.ExtraSettings.TimelineShowNote;
		}
		set
		{
			LocalSettings.Settings.ExtraSettings.TimelineShowNote = value;
			SetArrangeModels();
			LocalSettings.Settings.Save(force: true);
		}
	}

	public bool ShowParentInArrange
	{
		get
		{
			return LocalSettings.Settings.ExtraSettings.TimelineShowParent;
		}
		set
		{
			LocalSettings.Settings.ExtraSettings.TimelineShowParent = value;
			UpdateSortArrangeAsync();
			LocalSettings.Settings.Save(force: true);
		}
	}

	public ObservableCollection<TimelineDisplayBase> ArrangeModels
	{
		get
		{
			return _arrangeModels;
		}
		set
		{
			_arrangeModels = value;
			OnPropertyChanged("ArrangeModels");
		}
	}

	private List<TimelineDisplayBase> OverDueModels { get; } = new List<TimelineDisplayBase>();

	private List<TimelineDisplayBase> NoDateModels { get; } = new List<TimelineDisplayBase>();

	public bool BatchSelect => _batchSelectedModels.Count > 0;

	public event EventHandler BatchSelectedChanged;

	public TimelineViewModel()
	{
		_uid = Utils.GetGuid();
		DataChangedNotifier.TimelineSettingsChanged += delegate
		{
			OnPropertyChanged("ColorType");
			OnPropertyChanged("ShowWeek");
			OnPropertyChanged("HeadHeight");
			foreach (TimelineCellViewModel item in CellModels.ToList())
			{
				item.NotifyColorChanged();
			}
		};
		DataChangedNotifier.SyncDone += OnSyncChanged;
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		DataChangedNotifier.TagChanged += OnTagChanged;
		CalendarEventChangeNotifier.Changed += Reload;
	}

	private void Reload(object sender, CalendarEventModel e)
	{
		DelayActionHandlerCenter.TryDoAction(_uid + "ReloadTimeline", delegate
		{
			ThreadUtil.DetachedRunOnUiBackThread(Reload);
		});
	}

	private async void OnTagChanged(object sender, TagModel e)
	{
		if (TimelineSortOption.groupByType == Constants.SortType.tag)
		{
			await UpdateGroupAsync();
			await UpdateCellLineAsync();
		}
		if (ColorType != TimelineColorType.Tag)
		{
			return;
		}
		foreach (TimelineCellViewModel item in CellModels.ToList())
		{
			item.NotifyColorChanged();
		}
	}

	private async void OnProjectChanged(object sender, EventArgs e)
	{
		if (ProjectIdentity != null)
		{
			await Task.Delay(300);
			Reload();
		}
	}

	private async void OnSyncChanged(object sender, SyncResult e)
	{
		if (e.ColumnChanged && TimelineSortOption.groupByType == Constants.SortType.sortOrder)
		{
			await UpdateGroupAsync();
			await UpdateCellLineAsync();
		}
		if (ProjectIdentity is NormalProjectIdentity)
		{
			await UpdateSortArrangeAsync();
		}
	}

	public async Task OnTasksChanged(TasksChangeEventArgs e)
	{
		bool changed = false;
		bool changeLine = false;
		if (e.TagChangedIds.Any() && GroupByEnum == Constants.SortType.tag)
		{
			changed = true;
		}
		if (e.AssignChangedIds.Any() && GroupByEnum == Constants.SortType.assignee)
		{
			changed = true;
		}
		if (e.ProjectChangedIds.Any() && GroupByEnum == Constants.SortType.project)
		{
			changed = true;
		}
		if ((e.PriorityChangedIds.Any() || e.KindChangedIds.Any()) && GroupByEnum == Constants.SortType.priority)
		{
			changed = true;
		}
		if (e.SortOrderChangedIds.Any() && GroupByEnum == Constants.SortType.sortOrder)
		{
			changed = true;
		}
		if (e.BatchChangedIds.Any())
		{
			CellModels.Foreach(delegate(TimelineCellViewModel c)
			{
				if (e.DateChangedIds.Contains(c.Id))
				{
					UpdateCellTime(c, ignoreEdit: true, forceCheck: true);
				}
			});
			changed = true;
		}
		if (e.DateChangedIds.Any())
		{
			CellModels.Foreach(delegate(TimelineCellViewModel c)
			{
				if (e.DateChangedIds.Contains(c.Id))
				{
					UpdateCellTime(c, ignoreEdit: true, forceCheck: true);
				}
			});
			changed = true;
		}
		BlockingSet<string> blockingSet = e.BatchChangedIds.Copy();
		blockingSet.AddRange(e.ProjectChangedIds);
		blockingSet.AddRange(e.AddIds);
		blockingSet.AddRange(e.AssignChangedIds);
		blockingSet.AddRange(e.KindChangedIds);
		blockingSet.AddRange(e.TagChangedIds);
		blockingSet.AddRange(e.PriorityChangedIds);
		blockingSet.AddRange(e.DeletedChangedIds);
		blockingSet.AddRange(e.UndoDeletedIds);
		blockingSet.AddRange(e.StatusChangedIds);
		List<string> list = blockingSet.ToList();
		List<string> ids = list;
		if (ProjectIdentity is TagProjectIdentity && e.TagChangedIds.Any())
		{
			HashSet<string> hashSet = new HashSet<string>(list.Where((string value2) => !string.IsNullOrEmpty(value2)));
			foreach (string item in e.TagChangedIds.ToList())
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
				List<TaskBaseViewModel> list2 = ((taskById == null) ? null : TaskCache.GetAllSubTasksById(taskById.Id, taskById.ProjectId));
				if (list2 == null)
				{
					continue;
				}
				foreach (TaskBaseViewModel item2 in list2)
				{
					if (!string.IsNullOrEmpty(item2?.Id))
					{
						hashSet.Add(item2.Id);
					}
				}
			}
			list = hashSet.ToList();
			HashSet<string> hashSet2 = new HashSet<string>(list);
			foreach (string item3 in list)
			{
				List<TaskBaseViewModel> allParentTasksById = TaskCache.GetAllParentTasksById(item3);
				if (allParentTasksById == null)
				{
					continue;
				}
				foreach (TaskBaseViewModel item4 in allParentTasksById)
				{
					if (!string.IsNullOrEmpty(item4?.Id))
					{
						hashSet2.Add(item4.Id);
					}
				}
			}
			ids = hashSet2.ToList();
		}
		DateTime startDate = StartDate;
		double oneDayWidth = OneDayWidth;
		List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, ids);
		if (ProjectIdentity is TagProjectIdentity && e.TagChangedIds.Any() && matchedTasks != null)
		{
			HashSet<string> hashSet3 = new HashSet<string>(list);
			foreach (TaskBaseViewModel child in TaskCache.GetPinCheckChildren(matchedTasks.Select((TaskBaseViewModel t) => t.Id).ToList()) ?? new List<TaskBaseViewModel>())
			{
				if (!string.IsNullOrEmpty(child?.Id) && hashSet3.Contains(child.Id) && matchedTasks.All((TaskBaseViewModel t) => t.Id != child.Id))
				{
					matchedTasks.Add(child);
				}
			}
		}
		matchedTasks?.RemoveAll((TaskBaseViewModel t) => HideCompleted && t.Status != 0);
		foreach (string id in list)
		{
			TimelineCellViewModel timelineCellViewModel = FirstOrDefaultCell((TimelineCellViewModel c) => c?.Id == id);
			if (timelineCellViewModel == null)
			{
				TaskBaseViewModel taskBaseViewModel = matchedTasks?.FirstOrDefault((TaskBaseViewModel t) => t.Id == id);
				if (taskBaseViewModel != null)
				{
					TimelineCellViewModel timelineCellViewModel2 = new TimelineCellViewModel(this, taskBaseViewModel);
					AddCell(timelineCellViewModel2, checkAva: true);
					double left = (double)(timelineCellViewModel2.StartDate.Date - startDate).Days * oneDayWidth;
					timelineCellViewModel2.SetLeft(left, notify: true);
					changed = true;
				}
				continue;
			}
			TaskBaseViewModel taskBaseViewModel2 = matchedTasks?.FirstOrDefault((TaskBaseViewModel t) => t.Id == id);
			if (taskBaseViewModel2 == null)
			{
				RemoveCell(timelineCellViewModel);
				changed = true;
				continue;
			}
			double num = ((!taskBaseViewModel2.StartDate.HasValue) ? double.NegativeInfinity : ((double)(taskBaseViewModel2.StartDate.Value.Date - startDate).Days * oneDayWidth));
			if ((num < 0.0 || timelineCellViewModel.Left < 0.0) && Math.Abs(num - timelineCellViewModel.Left) > 1.0)
			{
				changed = true;
			}
			if (timelineCellViewModel.SetLeft(num, notify: true) || e.KindChangedIds.Contains(id))
			{
				changeLine = true;
			}
			if (e.DateChangedIds.Contains(id) || e.BatchChangedIds.Contains(id))
			{
				UpdateCellAvailable(timelineCellViewModel);
			}
			if (e.StatusChangedIds.Contains(id))
			{
				UpdateCellToArrange(timelineCellViewModel);
			}
		}
		if (CanShowCheckItem && LocalSettings.Settings.ShowSubtasks)
		{
			HashSet<string> value = e.CheckItemChangedIds.Value;
			e.AddIds.AddRange(e.StatusChangedIds);
			e.AddIds.AddRange(e.ProjectChangedIds);
			e.AddIds.AddRange(e.TagChangedIds);
			e.AddIds.AddRange(e.AssignChangedIds);
			e.AddIds.AddRange(e.PriorityChangedIds);
			e.AddIds.AddRange(e.PriorityChangedIds);
			e.AddIds.AddRange(e.DeletedChangedIds);
			e.AddIds.AddRange(e.UndoDeletedIds);
			e.AddIds.AddRange(e.KindChangedIds);
			e.AddIds.AddRange(e.BatchChangedIds);
			List<string> list3 = e.AddIds.ToList();
			if (list3.Any())
			{
				foreach (string item5 in list3)
				{
					TaskBaseViewModel taskById2 = TaskCache.GetTaskById(item5);
					if (taskById2?.CheckItems == null || taskById2.CheckItems.Count <= 0)
					{
						continue;
					}
					foreach (TaskBaseViewModel item6 in taskById2.CheckItems.Value)
					{
						value.Add(item6.Id);
					}
				}
			}
			List<TaskBaseViewModel> matchedItems = TaskViewModelHelper.GetMatchedItems(ProjectIdentity, value.ToList());
			if (HideCompleted)
			{
				matchedItems?.RemoveAll((TaskBaseViewModel t) => t.Status != 0);
			}
			foreach (string id2 in value)
			{
				TimelineCellViewModel timelineCellViewModel3 = FirstOrDefaultCell((TimelineCellViewModel c) => c?.Id == id2);
				if (timelineCellViewModel3 == null)
				{
					TaskBaseViewModel taskBaseViewModel3 = matchedItems?.FirstOrDefault((TaskBaseViewModel t) => t.Id == id2);
					if (taskBaseViewModel3 != null)
					{
						TimelineCellViewModel timelineCellViewModel4 = new TimelineCellViewModel(this, taskBaseViewModel3);
						AddCell(timelineCellViewModel4, checkAva: true);
						double left2 = (double)(timelineCellViewModel4.StartDate.Date - startDate).Days * oneDayWidth;
						timelineCellViewModel4.SetLeft(left2, notify: true);
						changed = true;
					}
				}
				else if (matchedItems?.All((TaskBaseViewModel t) => t.Id != id2) ?? true)
				{
					RemoveCell(timelineCellViewModel3);
					changed = true;
				}
				else
				{
					double left3 = (double)(timelineCellViewModel3.StartDate.Date - startDate).Days * oneDayWidth;
					if (timelineCellViewModel3.SetLeft(left3, notify: true))
					{
						changeLine = true;
					}
				}
			}
		}
		if (changed)
		{
			await UpdateGroupAsync();
		}
		if (changeLine || changed)
		{
			await UpdateCellLineAsync();
			if (BatchSelect)
			{
				OnPropertyChanged("HoverStartEndTuples");
			}
		}
	}

	public void ResetGroupHideModels(bool drag = false)
	{
		Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
		foreach (TimelineGroupViewModel groupModel in GroupModels)
		{
			groupModel.IsOpen = !drag && groupModel.IsOpen;
			dictionary[groupModel.Line] = groupModel.IsOpen;
		}
		GroupDictModels = dictionary;
	}

	public async Task ResetAllAsync(string taskId = null)
	{
		await Application.Current.Dispatcher.InvokeAsync((Func<Task>)async delegate
		{
			await _resetAllSemaphoreSlim.WaitAsync();
			try
			{
				while (Editing || Hovering)
				{
					await Task.Delay(100);
				}
				AvailableReset = true;
				await ResetAllResetModelsInternal();
				await SetAllModelsOptAndUpdateAvailableTask(StartDate, EndDate);
				AvailableReset = false;
				await UpdateGroupAsync();
				await UpdateCellLeftAsync(notify: false);
				await UpdateCellLineAsync(notify: false);
				await UpdateSortArrangeAsync();
				UpdateYearModel(updateModels: true, updateLeft: true);
				AvailableModels.NotifyItemsChanged(AvailableModels.Value);
			}
			finally
			{
				_resetAllSemaphoreSlim.Release();
			}
		});
	}

	private void UpdateModelOpt(TimelineCellViewModel cell, DateTime startDate, DateTime endDate, bool ignoreEdit = false)
	{
		if (cell.DisplayModel == null)
		{
			cell.Operation = TimelineCellOperation.Hide;
			return;
		}
		TimelineCellOperation timelineCellOperation = TimelineCellOperation.None;
		if ((!cell.DisplayModel.StartDate.HasValue && !cell.DisplayModel.DueDate.HasValue) || cell.DisplayModel.Deleted != 0)
		{
			timelineCellOperation = TimelineCellOperation.Hide;
		}
		else if (DateUtils.CheckDateInSpan(cell.StartDate, cell.EndDate?.Date ?? cell.StartDate.AddDays(1.0), startDate, endDate))
		{
			if (cell.Operation.Contain(TimelineCellOperation.Hide))
			{
				timelineCellOperation = TimelineCellOperation.None;
			}
		}
		else
		{
			timelineCellOperation = TimelineCellOperation.Hide;
		}
		if ((cell.Operation != TimelineCellOperation.Fold || timelineCellOperation != TimelineCellOperation.None) && (ignoreEdit || cell.Operation.IsNormalStatus()) && cell.Operation != timelineCellOperation)
		{
			cell.Operation = timelineCellOperation;
		}
	}

	private async Task SetAllModelsOptAndUpdateAvailableTask(DateTime startDate, DateTime endDate)
	{
		await _cellLock.WaitAsync();
		try
		{
			foreach (TimelineCellViewModel item in CellModels.Value)
			{
				UpdateModelOpt(item, startDate, endDate);
			}
			List<TimelineCellViewModel> list = CellModels.Where((TimelineCellViewModel c) => c.DisplayModel != null && c.DisplayModel.StartDate.HasValue && c.Operation != TimelineCellOperation.Hide).ToList();
			List<TimelineCellViewModel> list2 = new List<TimelineCellViewModel>();
			foreach (TimelineCellViewModel item2 in AvailableModels.Value)
			{
				if (!list.Contains(item2))
				{
					list2.Add(item2);
				}
			}
			if (list2.Count == AvailableModels.Count)
			{
				AvailableModels.Clear();
			}
			else
			{
				list2.ForEach(delegate(TimelineCellViewModel r)
				{
					AvailableModels.Remove(r);
				});
			}
			IEnumerable<TimelineCellViewModel> models = list.Where((TimelineCellViewModel t) => !AvailableModels.Contains(t));
			AvailableModels.AddRange(models);
		}
		finally
		{
			_cellLock.Release();
		}
	}

	private async Task ResetAllResetModelsInternal()
	{
		await _cellLock.WaitAsync();
		try
		{
			List<TaskBaseViewModel> list = await TaskListRefreshHelper.InitData(ProjectIdentity, withChild: false);
			if (HideCompleted)
			{
				list = list.Where((TaskBaseViewModel t) => t.Status == 0).ToList();
			}
			if (CheckTasksChanged(list))
			{
				ClearCell();
				{
					foreach (TaskBaseViewModel item in list)
					{
						AddCell(new TimelineCellViewModel(this, item), checkAva: false);
					}
					return;
				}
			}
			ClearBatchSelect();
		}
		finally
		{
			_cellLock.Release();
		}
	}

	private bool CheckTasksChanged(List<TaskBaseViewModel> tasks)
	{
		if (tasks == null)
		{
			return true;
		}
		if (tasks.Count != CellModels.Count)
		{
			return true;
		}
		HashSet<string> currentIds = new HashSet<string>((from t in CellModels.ToList()
			select t.Id).ToList());
		return tasks.Any((TaskBaseViewModel t) => !currentIds.Contains(t.Id));
	}

	public async Task OnDragColumn()
	{
		foreach (TimelineGroupViewModel groupModel in GroupModels)
		{
			groupModel.IsOpen = false;
		}
		UpdateCellLineAsync();
	}

	public async Task ReloadColumns()
	{
		await UpdateGroupAsync();
		await UpdateCellLineAsync();
	}

	public async void AddNewColumn()
	{
		if (!(ProjectIdentity is NormalProjectIdentity { Project: not null } normalProjectIdentity))
		{
			return;
		}
		string projectId = normalProjectIdentity.Project.id;
		if (!GroupModels.Any())
		{
			List<ColumnModel> list = await ColumnDao.GetColumnsByProjectId(projectId);
			if ((list == null || !list.Any()) && await ProjectDao.GetProjectById(projectId) != null)
			{
				await ColumnDao.TryInitColumns(projectId);
			}
			await UpdateGroupAsync();
		}
		List<TimelineGroupViewModel> list2 = GroupModels.ToList();
		if (list2.Count >= LimitCache.GetLimitByKey(Constants.LimitKind.KanbanNumber))
		{
			new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("ExceedMaxSectionCountMessage"), MessageBoxButton.OK).ShowDialog();
			return;
		}
		long sortOrder = ((list2.Count > 0) ? (list2[0].SortOrder - 268435456) : 0);
		list2.Insert(0, new TimelineGroupViewModel(this)
		{
			Parent = this,
			ProjectId = projectId,
			CatId = projectId,
			Editable = true,
			IsOpen = true,
			Editing = true,
			SortOrder = sortOrder
		});
		GroupModels.Clear();
		foreach (TimelineGroupViewModel item in list2)
		{
			GroupModels.Add(item);
		}
		ShowGroup = true;
		await UpdateCellLineAsync();
	}

	public async void AddNewColumn(TimelineGroupViewModel columnModel, bool below)
	{
		List<TimelineGroupViewModel> list = GroupModels.ToList();
		if (list.Count >= LimitCache.GetLimitByKey(Constants.LimitKind.KanbanNumber))
		{
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("ExceedMaxSectionCountMessage"), MessageBoxButton.OK);
			customerDialog.Owner = App.Window;
			customerDialog.ShowDialog();
			return;
		}
		int num = list.IndexOf(columnModel);
		TimelineGroupViewModel timelineGroupViewModel = (below ? columnModel : ((num < list.Count - 1) ? list[num + 1] : null));
		TimelineGroupViewModel timelineGroupViewModel2 = ((!below) ? columnModel : ((num > 0) ? list[num - 1] : null));
		if (timelineGroupViewModel == null && timelineGroupViewModel2 == null)
		{
			return;
		}
		long sortOrder = ((timelineGroupViewModel == null) ? (timelineGroupViewModel2.SortOrder - 268435456) : ((timelineGroupViewModel2 == null) ? (timelineGroupViewModel.SortOrder + 268435456) : MathUtil.LongAvg(timelineGroupViewModel.SortOrder, timelineGroupViewModel2.SortOrder)));
		num = ((num < 0) ? (below ? list.Count : 0) : (num + (below ? 1 : 0)));
		list.Insert(num, new TimelineGroupViewModel(this)
		{
			Parent = this,
			ProjectId = columnModel.ProjectId,
			CatId = columnModel.ProjectId,
			Editable = true,
			IsOpen = true,
			Editing = true,
			SortOrder = sortOrder
		});
		GroupModels.Clear();
		foreach (TimelineGroupViewModel item in list)
		{
			GroupModels.Add(item);
		}
		UpdateCellLineAsync();
	}

	public void RemoveGroup(TimelineGroupViewModel columnModel)
	{
		List<TimelineGroupViewModel> list = GroupModels.ToList();
		list.Remove(columnModel);
		GroupModels.Clear();
		foreach (TimelineGroupViewModel item in list)
		{
			item.IsOpen = list.Count < 2 || item.IsOpen;
			GroupModels.Add(item);
		}
		ShowGroup = list.Count > 1;
		UpdateCellLineAsync();
	}

	public async Task UpdateCellLineAsync(bool notify = true)
	{
		await _updateLineSemaphoreSlim.WaitAsync();
		await _updateGroupSemaphoreSlim.WaitAsync();
		try
		{
			if (ProjectIdentity != null)
			{
				UpdateCellLineInternal(ProjectIdentity, GroupByEnum, OrderByEnum, notify);
			}
		}
		finally
		{
			_updateLineSemaphoreSlim.Release();
			_updateGroupSemaphoreSlim.Release();
		}
	}

	public async Task UpdateGroupAsync()
	{
		await _updateGroupSemaphoreSlim.WaitAsync();
		await _cellLock.WaitAsync();
		try
		{
			if (ProjectIdentity != null)
			{
				await UpdateGroupInternal(ProjectIdentity, GroupByEnum);
			}
		}
		finally
		{
			_updateGroupSemaphoreSlim.Release();
			_cellLock.Release();
		}
	}

	private async Task UpdateGroupInternal(ProjectIdentity project, Constants.SortType sortType)
	{
		List<TimelineGroupViewModel> list = await GetGroupModels(project, sortType, CellModels.Where((TimelineCellViewModel t) => (t.DisplayModel?.StartDate).HasValue), containsNoChild: true);
		switch (sortType)
		{
		case Constants.SortType.sortOrder:
			ShowGroup = list.Count > 1;
			break;
		case Constants.SortType.none:
			ShowGroup = false;
			break;
		default:
			ShowGroup = list.Count > 0;
			ShowGroup = list.Count > 0;
			break;
		}
		ItemsSourceHelper.CopyTo(list, GroupModels);
	}

	private void UpdateCellLineInternal(ProjectIdentity project, Constants.SortType groupBy, Constants.SortType sortType, bool notify = true)
	{
		int num;
		switch (groupBy)
		{
		case Constants.SortType.tag:
			num = SortTag(AvailableModels.ToList(), sortType, notify);
			break;
		case Constants.SortType.priority:
			num = SortPriority(AvailableModels.ToList(), sortType, notify);
			break;
		case Constants.SortType.none:
		case Constants.SortType.dueDate:
			num = ((sortType == Constants.SortType.sortOrder) ? SortInternal(AvailableModels.ToList(), 0, Constants.SortType.sortOrder, notify) : SortDueDate(AvailableModels.ToList(), 0, Constants.SortType.sortOrder, notify));
			break;
		case Constants.SortType.assignee:
			num = SortAssignee(AvailableModels.ToList(), sortType, notify);
			break;
		default:
			num = ((!(project is NormalProjectIdentity)) ? SortProjects(AvailableModels.ToList(), sortType, notify) : (ShowGroup ? SortColumns(project, AvailableModels.ToList(), sortType, notify) : ((sortType == Constants.SortType.sortOrder) ? SortInternal(AvailableModels.ToList(), 0, Constants.SortType.sortOrder, notify) : SortDueDate(AvailableModels.ToList(), 0, Constants.SortType.sortOrder, notify))));
			break;
		}
		LineCount = num + 3;
		ResetGroupHideModels();
	}

	private async Task<List<TimelineGroupViewModel>> GetGroupModels(ProjectIdentity project, Constants.SortType sortType, IList<TimelineCellViewModel> children, bool containsNoChild)
	{
		List<TimelineGroupViewModel> models = new List<TimelineGroupViewModel>();
		string identityId = project?.CatId;
		List<SectionStatusModel> openStatus = (from item in CacheManager.GetSectionStatus()
			where identityId == item.Identity
			select item).ToList();
		string arrangeIdentity = "Arrange_" + identityId;
		List<SectionStatusModel> arrangeOpenStatus = (from item in CacheManager.GetSectionStatus()
			where arrangeIdentity == item.Identity
			select item).ToList();
		switch (sortType)
		{
		case Constants.SortType.sortOrder:
			await GetColumns();
			break;
		case Constants.SortType.project:
			GetProjectSections();
			break;
		case Constants.SortType.tag:
			GetTagSections();
			break;
		case Constants.SortType.assignee:
			GetAssigneeSection();
			break;
		case Constants.SortType.priority:
			GetPrioritySection();
			break;
		default:
			if (ProjectIdentity is NormalProjectIdentity)
			{
				await GetColumns();
			}
			else
			{
				GetProjectSections();
			}
			break;
		case Constants.SortType.none:
		case Constants.SortType.dueDate:
			break;
		}
		return models;
		void AddGroup(string id, string title)
		{
			models.Add(new TimelineGroupViewModel(this)
			{
				Parent = this,
				Id = id,
				Title = title,
				ProjectId = ProjectIdentity?.GetProjectId(),
				CatId = ProjectIdentity?.CatId,
				IsOpen = openStatus.All((SectionStatusModel s) => s.Name != id),
				IsArrangeOpen = arrangeOpenStatus.All((SectionStatusModel s) => s.Name != title)
			});
		}
		void GetAssigneeSection()
		{
			List<AvatarViewModel> list = ((!(ProjectIdentity is GroupProjectIdentity groupProjectIdentity)) ? AvatarHelper.GetProjectAvatarsFromCache(ProjectIdentity?.CatId, sort: true) : AvatarHelper.GetProjectAvatarsFromCacheInGroup(groupProjectIdentity.Id));
			Dictionary<string, TimelineGroupViewModel> dictionary = new Dictionary<string, TimelineGroupViewModel>();
			foreach (TimelineCellViewModel child in children)
			{
				string userId = child.DisplayModel.GetAssignee();
				if (userId != "-1" && !dictionary.ContainsKey(userId))
				{
					AvatarViewModel user = list.FirstOrDefault((AvatarViewModel u) => (u.UserId ?? "") == userId);
					if (user != null)
					{
						dictionary.Add(userId, new TimelineGroupViewModel(this)
						{
							Title = user.Name,
							Id = user.UserId,
							ProjectId = ProjectIdentity?.GetProjectId(),
							CatId = ProjectIdentity?.CatId,
							Parent = this,
							IsOpen = true,
							IsArrangeOpen = arrangeOpenStatus.All((SectionStatusModel s) => s.Name != user.UserId),
							AvatarUrl = user.AvatarUrl,
							IsAvatar = true,
							ShowArrow = Visibility.Collapsed,
							SortOrder = ((user.UserId == LocalSettings.Settings.LoginUserId) ? long.MinValue : list.IndexOf(user))
						});
					}
				}
			}
			models.AddRange(from m in dictionary.Values.ToList()
				orderby m.SortOrder
				select m);
			string title = Utils.GetString("NotAssigned");
			models.Add(new TimelineGroupViewModel(this)
			{
				Title = title,
				Id = "-1",
				ProjectId = ProjectIdentity?.GetProjectId(),
				CatId = ProjectIdentity?.CatId,
				Parent = this,
				IsAvatar = true,
				IsOpen = true,
				IsArrangeOpen = arrangeOpenStatus.All((SectionStatusModel s) => s.Name != "-1"),
				ShowArrow = Visibility.Collapsed
			});
		}
		async Task GetColumns()
		{
			string projectId = ProjectIdentity?.GetProjectId();
			if (children.Any((TimelineCellViewModel c) => string.IsNullOrEmpty(c.DisplayModel.ColumnId)))
			{
				await TaskService.CheckColumnEmptyTask(projectId);
			}
			List<ColumnModel> columns = await ColumnDao.GetColumnsByProjectId(projectId);
			columns?.ForEach(delegate(ColumnModel c)
			{
				models.Add(new TimelineGroupViewModel(this)
				{
					Title = c.name,
					Id = c.id,
					Parent = this,
					IsOpen = (columns.Count <= 1 || openStatus.All((SectionStatusModel s) => s.Name != c.id)),
					IsArrangeOpen = arrangeOpenStatus.All((SectionStatusModel s) => s.Name != c.id),
					ProjectId = projectId,
					CatId = projectId,
					SortOrder = c.sortOrder.GetValueOrDefault(),
					Editable = (CacheManager.GetProjectById(projectId)?.IsEnable() ?? false)
				});
			});
			if (!containsNoChild)
			{
				List<TimelineGroupViewModel> alone = new List<TimelineGroupViewModel>();
				models.Where((TimelineGroupViewModel m) => children.All((TimelineCellViewModel c) => c.DisplayModel.ColumnId != m.Id)).ToList().ForEach(delegate(TimelineGroupViewModel m)
				{
					alone.Add(m);
				});
				alone.ForEach(delegate(TimelineGroupViewModel a)
				{
					models.Remove(a);
				});
			}
		}
		void GetPrioritySection()
		{
			if (children.Any((TimelineCellViewModel c) => !c.DisplayModel.IsNote && c.DisplayModel.Priority == 5))
			{
				AddGroup("5", Utils.GetString("PriorityHigh"));
			}
			if (children.Any((TimelineCellViewModel c) => !c.DisplayModel.IsNote && c.DisplayModel.Priority == 3))
			{
				AddGroup("3", Utils.GetString("PriorityMedium"));
			}
			if (children.Any((TimelineCellViewModel c) => !c.DisplayModel.IsNote && c.DisplayModel.Priority == 1))
			{
				AddGroup("1", Utils.GetString("PriorityLow"));
			}
			if (children.Any((TimelineCellViewModel c) => !c.DisplayModel.IsNote && c.DisplayModel.Priority == 0))
			{
				AddGroup("0", Utils.GetString("PriorityNull"));
			}
			if (children.Any((TimelineCellViewModel c) => c.DisplayModel.IsNote))
			{
				AddGroup("note", Utils.GetString("Notes"));
			}
		}
		void GetProjectSections()
		{
			List<ProjectModel> list;
			if (ProjectIdentity is GroupProjectIdentity && containsNoChild)
			{
				list = (from projectModel in CacheManager.GetProjects()
					where projectModel.groupId == ProjectIdentity.CatId
					orderby projectModel.sortOrder
					select projectModel).ToList();
			}
			else
			{
				HashSet<string> projectIds = new HashSet<string>(children.Select((TimelineCellViewModel c) => c.DisplayModel.IsEvent ? "Calendar5959a2259161d16d23a4f272" : c.DisplayModel.ProjectId));
				list = (from projectModel in CacheManager.GetProjects()
					where projectIds.Contains(projectModel.id)
					select projectModel).ToList();
				list.Sort((ProjectModel a, ProjectModel b) => a.CompareTo(b));
				if (projectIds.Contains("Calendar5959a2259161d16d23a4f272"))
				{
					list.Add(new ProjectModel
					{
						id = "Calendar5959a2259161d16d23a4f272",
						name = Utils.GetString("Calendar")
					});
				}
			}
			foreach (ProjectModel p in list)
			{
				models.Add(new TimelineGroupViewModel(this)
				{
					Parent = this,
					Id = p.id,
					Title = p.name,
					ProjectId = p.id,
					CatId = ProjectIdentity.CatId,
					IsOpen = openStatus.All((SectionStatusModel s) => s.Name != p.id),
					IsArrangeOpen = arrangeOpenStatus.All((SectionStatusModel s) => s.Name != p.id)
				});
			}
		}
		void GetTagSections()
		{
			List<TagModel> tags = TagDataHelper.GetTags();
			tags.Sort((TagModel a, TagModel b) => a.sortOrder.CompareTo(b.sortOrder));
			List<TagModel> list = new List<TagModel>();
			HashSet<string> hashSet = new HashSet<string>();
			foreach (TimelineCellViewModel child2 in children)
			{
				string label = Utils.GetString("NoTags");
				if (string.IsNullOrEmpty(child2.DisplayModel.Tag) || child2.DisplayModel.Tag == "[]")
				{
					if (!hashSet.Contains("!tag"))
					{
						TagModel tagModel = new TagModel
						{
							name = "!tag",
							label = label,
							sortOrder = 9223372036854775806L
						};
						list.Add(tagModel);
						hashSet.Add(tagModel.name);
					}
				}
				else
				{
					List<string> taskTags = TagSerializer.ToTags(child2.DisplayModel.Tag.ToLower());
					TagModel tagModel2 = tags.FirstOrDefault((TagModel t) => taskTags.Contains(t.name));
					if (tagModel2 != null && tagModel2.name != null && !hashSet.Contains(tagModel2.name))
					{
						list.Add(tagModel2);
						hashSet.Add(tagModel2.name);
					}
				}
			}
			list.Sort((TagModel a, TagModel b) => a.sortOrder.CompareTo(b.sortOrder));
			foreach (TagModel tag in list)
			{
				models.Add(new TimelineGroupViewModel(this)
				{
					Parent = this,
					Id = tag.name,
					Title = tag.GetDisplayName(),
					ProjectId = ProjectIdentity?.GetProjectId(),
					CatId = ProjectIdentity?.CatId,
					IsOpen = openStatus.All((SectionStatusModel s) => s.Name != tag.name),
					IsArrangeOpen = arrangeOpenStatus.All((SectionStatusModel s) => s.Name != tag.name)
				});
			}
		}
	}

	public async Task MoveSpan(DateTime startDate, DateTime endDate, bool updateModels = true)
	{
		if (Utils.IsEmptyDate(startDate) || Utils.IsEmptyDate(startDate))
		{
			return;
		}
		startDate = startDate.Date;
		endDate = endDate.Date;
		bool changed = false;
		startDate = startDate.AddDays((int)(startDate.DayOfWeek - 1) * -1);
		endDate = endDate.AddDays((int)endDate.DayOfWeek * -1).AddDays(7.0);
		if (StartDate != startDate || EndDate != endDate)
		{
			StartEndTuple = new Tuple<DateTime, DateTime>(startDate, endDate);
			OnPropertyChanged("StartEndTuple");
			OnPropertyChanged("Width");
		}
		if (StartDate != startDate)
		{
			StartDate = startDate;
			changed = true;
			OnPropertyChanged("StartDate");
			if (updateModels)
			{
				await UpdateCellLeftAsync();
			}
		}
		if (EndDate != endDate)
		{
			EndDate = endDate;
			changed = true;
			OnPropertyChanged("EndDate");
		}
		if (!changed || ProjectIdentity == null)
		{
			return;
		}
		await SetAllModelsOptAndUpdateAvailableTask(startDate, endDate);
		foreach (TimelineCellViewModel item in AvailableModels.ToList())
		{
			item.SetWidth();
		}
		if (updateModels)
		{
			await UpdateGroupAsync();
			await UpdateCellLineAsync();
		}
		UpdateYearModel(updateModels: true, updateLeft: true);
		Fetch();
	}

	private void UpdateYearModel(bool updateModels, bool updateLeft)
	{
		DateTime startDate = StartDate;
		DateTime endDate = EndDate;
		double oneDayWidth = OneDayWidth;
		string timelineRange = TimelineRange;
		double xOffset = XOffset;
		int year = DateTime.Now.Year;
		lock (YearModels)
		{
			List<TimelineYearViewModel> list = YearModels.ToList();
			if (updateModels)
			{
				list.Clear();
				if (timelineRange == "year")
				{
					DateTime dateTime = new DateTime(startDate.Year, 1, 1);
					while (dateTime.Year < endDate.Year)
					{
						list.Add(new TimelineYearViewModel
						{
							Left = (double)(dateTime - startDate).Days * oneDayWidth,
							YearName = dateTime.Year.ToString()
						});
						dateTime = dateTime.AddYears(1);
					}
				}
				else
				{
					(DateTime? min, DateTime? max) currentCultureLimitDate = CalendarConverter.GetCurrentCultureLimitDate();
					DateTime dateTime2 = currentCultureLimitDate.min ?? DateTime.MinValue;
					DateTime dateTime3 = currentCultureLimitDate.max ?? DateTime.MaxValue;
					DateTime dateTime4 = new DateTime(startDate.Year, startDate.Month, 1);
					while (dateTime4 < endDate)
					{
						TimelineYearViewModel timelineYearViewModel = new TimelineYearViewModel
						{
							Left = (double)(dateTime4 - startDate).Days * oneDayWidth
						};
						if (dateTime4 >= dateTime2 && dateTime4 <= dateTime3)
						{
							timelineYearViewModel.YearName = ((dateTime4.Year == year) ? DateUtils.FormatMonth(dateTime4) : dateTime4.ToString("Y"));
						}
						list.Add(timelineYearViewModel);
						dateTime4 = dateTime4.AddMonths(1);
					}
				}
				ItemsSourceHelper.CopyTo(list, YearModels);
			}
			if (!updateLeft)
			{
				return;
			}
			double yearNameXOffset = xOffset + 10.0;
			TimelineYearViewModel timelineYearViewModel2 = list.LastOrDefault((TimelineYearViewModel i) => i.Left < yearNameXOffset);
			if (timelineYearViewModel2 == null)
			{
				return;
			}
			foreach (TimelineYearViewModel item in list)
			{
				item.Visibility = Visibility.Visible;
			}
			int num = list.IndexOf(timelineYearViewModel2);
			if (num != -1 && num < list.Count - 1)
			{
				double num2 = list[num + 1].Left - yearNameXOffset;
				YearNameWidth = ((num2 > 0.0) ? num2 : double.NaN);
			}
			else
			{
				YearNameWidth = double.NaN;
			}
			timelineYearViewModel2.Visibility = Visibility.Collapsed;
			YearName = timelineYearViewModel2.YearName;
		}
	}

	private void Fetch()
	{
		if (ProjectIdentity != null)
		{
			DateTime end = ((EndDate < DateTime.Now) ? EndDate : DateTime.Now);
			_closedLoader?.LoadTasksInSpan(StartDate, end, (ProjectIdentity is NormalProjectIdentity normalProjectIdentity) ? normalProjectIdentity.Id : "all", onlyCheckStart: true);
		}
	}

	public async Task UpdateCellLeftAsync(bool notify = true)
	{
		await _updateLeftSemaphoreSlim.WaitAsync();
		await _cellLock.WaitAsync();
		try
		{
			UpdateCellModelsLeftInternal(CellModels.Value, notify);
		}
		finally
		{
			_updateLeftSemaphoreSlim.Release();
			_cellLock.Release();
		}
	}

	private void UpdateCellModelsLeftInternal(IEnumerable<TimelineCellViewModel> cellModels, bool notify)
	{
		DateTime startDate = StartDate;
		double oneDayWidth = OneDayWidth;
		foreach (TimelineCellViewModel cellModel in cellModels)
		{
			double left = (double)(cellModel.StartDate.Date - startDate).Days * oneDayWidth;
			cellModel.SetLeft(left, notify);
		}
	}

	private TimelineModel GetTimelineModel()
	{
		return ProjectIdentity?.GetTimelineModel();
	}

	public void ClearCell()
	{
		_batchSelectedModels.Clear();
		OnPropertyChanged("HoverStartEndTuples");
		CellModels.Clear();
		AvailableModels.Clear();
		ClearArrangeModels();
	}

	public void RemoveDisplayCell(TimelineCellViewModel model)
	{
		AvailableModels.Remove(model);
	}

	public async Task RemoveCell(TimelineCellViewModel model, bool setPos = false)
	{
		CellModels.Remove(model);
		AvailableModels.Remove(model);
		if (model.BatchSelected)
		{
			ChangeBatchSelectStatus(model);
		}
		await UpdateCellToArrange(model, remove: true);
		if (setPos)
		{
			await UpdateGroupAsync();
			await UpdateCellLineAsync();
		}
	}

	public void AddCell(TimelineCellViewModel model, bool checkAva, bool addToAva = false)
	{
		CellModels.Add(model);
		if (checkAva)
		{
			UpdateModelOpt(model, StartDate, EndDate);
			if (model.Operation != TimelineCellOperation.Hide && AvailableModels.All((TimelineCellViewModel m) => m.Id != model.Id))
			{
				AvailableModels.Add(model);
			}
			UpdateCellToArrange(model);
		}
		else if (addToAva)
		{
			AvailableModels.Add(model);
		}
	}

	public void UpdateCellTime(TimelineCellViewModel model, bool ignoreEdit = false, bool forceCheck = false)
	{
		if (ProjectIdentity is FilterProjectIdentity || forceCheck)
		{
			switch (model.DisplayModel?.Type)
			{
			case DisplayType.Task:
			case DisplayType.Note:
			{
				List<string> list = new List<string> { model.Id };
				List<TaskBaseViewModel> allParentTasksById = TaskCache.GetAllParentTasksById(model.Id);
				if (allParentTasksById != null)
				{
					list.AddRange(from t in allParentTasksById
						where !string.IsNullOrEmpty(t?.Id)
						select t.Id);
				}
				List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, list);
				matchedTasks?.AddRange(TaskCache.GetPinCheckChildren(matchedTasks.Select((TaskBaseViewModel t) => t.Id).ToList()));
				matchedTasks?.RemoveAll((TaskBaseViewModel t) => HideCompleted && t.Status != 0);
				if (matchedTasks == null || matchedTasks.All((TaskBaseViewModel t) => t.Id != model.Id))
				{
					RemoveCell(model, setPos: true);
					return;
				}
				break;
			}
			case DisplayType.CheckItem:
			{
				List<TaskBaseViewModel> matchedItems = TaskViewModelHelper.GetMatchedItems(ProjectIdentity, new List<string> { model.Id });
				matchedItems?.RemoveAll((TaskBaseViewModel t) => HideCompleted && t.Status != 0);
				if (matchedItems == null || matchedItems.Count == 0)
				{
					RemoveCell(model, setPos: true);
					return;
				}
				break;
			}
			}
		}
		UpdateCellAvailable(model, ignoreEdit);
	}

	public void UpdateCellAvailable(TimelineCellViewModel model, bool ignoreEdit = false)
	{
		UpdateModelOpt(model, StartDate, EndDate, ignoreEdit);
		UpdateCellToArrange(model);
		if (model.Operation == TimelineCellOperation.Hide || Utils.IsEmptyDate(model.StartDate))
		{
			AvailableModels.Remove(model);
			return;
		}
		AddToAvailable(model);
		model.SetLeft();
	}

	public void AddToAvailable(TimelineCellViewModel model)
	{
		if (model.BatchSelected)
		{
			foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
			{
				if (AvailableModels.All((TimelineCellViewModel m) => m.Id != model.Id))
				{
					AvailableModels.Add(item);
				}
			}
			return;
		}
		if (AvailableModels.All((TimelineCellViewModel m) => m.Id != model.Id))
		{
			AvailableModels.Add(model);
		}
	}

	public void RemoveAvailable(TimelineCellViewModel model)
	{
		if (model.BatchSelected)
		{
			foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
			{
				AvailableModels.Remove(item);
			}
			return;
		}
		AvailableModels.Remove(model);
	}

	public bool ContainsCell(TimelineCellViewModel model)
	{
		return CellModels.Contains(model);
	}

	public bool AnyCell(Func<TimelineCellViewModel, bool> predicate)
	{
		return CellModels.Value.Any(predicate);
	}

	public TimelineCellViewModel FirstOrDefaultCell(Func<TimelineCellViewModel, bool> predicate)
	{
		return CellModels.FirstOrDefault(predicate);
	}

	public IEnumerable<TimelineCellViewModel> WhereCell(Func<TimelineCellViewModel, bool> predicate)
	{
		return CellModels.Value.Where(predicate);
	}

	public DateTime? GetRentCellDate()
	{
		DateTime? result = null;
		foreach (TimelineCellViewModel item in CellModels.Value)
		{
			if (!item.DisplayModel.StartDate.HasValue)
			{
				continue;
			}
			DateTime value = item.DisplayModel.StartDate.Value;
			if (item.DisplayModel.DueDate.HasValue)
			{
				DateTime? dueDate = item.DisplayModel.DueDate;
				DateTime today = DateTime.Today;
				if (dueDate.HasValue && dueDate.GetValueOrDefault() > today && item.DisplayModel.StartDate <= DateTime.Today)
				{
					return DateTime.Today;
				}
				if (item.DisplayModel.DueDate < DateTime.Today)
				{
					value = item.DisplayModel.DueDate.Value;
				}
			}
			if (!result.HasValue)
			{
				result = value.Date;
				continue;
			}
			double num = Math.Abs((DateTime.Today - result.Value).TotalDays);
			double num2 = Math.Abs((DateTime.Today - value.Date).TotalDays);
			if (num > num2 || (Math.Abs(num - num2) < 0.1 && value.Date > result.Value.Date))
			{
				result = value.Date;
			}
		}
		return result;
	}

	public async Task SetProjectIdentity(ProjectIdentity value)
	{
		bool num = TimelineRange != value?.GetTimelineModel()?.Range || TimelineRangeIndex != value?.GetTimelineModel()?.DayWidthIndex;
		ProjectIdentity = value;
		OnPropertyChanged("ShowShareGrid");
		OnPropertyChanged("ShowShareIcon");
		_closedLoader = new ClosedTaskWithFilterLoader();
		if (num)
		{
			OnTimelineRangeChanged();
			await Task.Delay(50);
		}
		await ResetAllAsync();
		SetProjectTitle(ProjectIdentity?.GetDisplayTitle());
		OnPropertyChanged("CanShowActivity");
		OnPropertyChanged("CanShowCheckItem");
		OnPropertyChanged("TimelinePermission");
		OnPropertyChanged("TimelineSortOption");
		OnPropertyChanged("ProjectEnable");
	}

	public void SetProjectTitle(string title)
	{
		ProjectTitle = title;
		OnPropertyChanged("ProjectTitle");
	}

	public async Task SetRangIndex(int index)
	{
		TimelineModel timelineModel = GetTimelineModel();
		if (timelineModel == null || timelineModel.DayWidthIndex == index)
		{
			return;
		}
		timelineModel.DayWidthIndex = index;
		if (TimelineConstants.GetDayWidthRange(index) != timelineModel.Range)
		{
			timelineModel.Range = TimelineConstants.GetDayWidthRange(index);
		}
		ProjectIdentity?.CommitTimeline(timelineModel);
		OnTimelineRangeChanged();
		UpdateYearModel(updateModels: true, updateLeft: true);
		await UpdateCellLeftAsync();
		foreach (TimelineCellViewModel item in AvailableModels.ToList())
		{
			item.SetWidth();
		}
	}

	private void OnTimelineRangeChanged()
	{
		OnPropertyChanged("TimelineRange");
		OnPropertyChanged("TimelineRangeIndex");
		OnPropertyChanged("OneDayWidth");
		OnPropertyChanged("Width");
		OnPropertyChanged("NewTaskDefaultDays");
		OnPropertyChanged("MinChangeDays");
		OnPropertyChanged("NeedsSplitLine");
	}

	public async void UpdateTimelineSortType()
	{
		await UpdateGroupAsync();
		await UpdateCellLeftAsync();
		await UpdateCellLineAsync();
		await UpdateSortArrangeAsync();
	}

	public bool CheckTaskMatched(string taskId)
	{
		if (!(ProjectIdentity is FilterProjectIdentity))
		{
			return true;
		}
		List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, new List<string> { taskId });
		matchedTasks?.RemoveAll((TaskBaseViewModel t) => HideCompleted && t.Status != 0);
		if (matchedTasks == null || matchedTasks.Count == 0)
		{
			TimelineCellViewModel timelineCellViewModel = CellModels.FirstOrDefault((TimelineCellViewModel m) => m.Id == taskId);
			if (timelineCellViewModel != null)
			{
				RemoveCell(timelineCellViewModel);
			}
			return false;
		}
		return true;
	}

	public void NotifyItemPosChanged(TimelineCellViewModel cellModel)
	{
		AvailableModels.OnItemPosChanged(cellModel);
	}

	public void Reload()
	{
		if (!_batchSelectedModels.Any() && ProjectIdentity != null)
		{
			SetProjectIdentity(ProjectIdentity.Copy(ProjectIdentity));
		}
	}

	public async void SetGroupEditing(bool value)
	{
		if (!value)
		{
			await Task.Delay(200);
		}
		GroupEditing = value;
	}

	public void SetItemEditing(bool value)
	{
		_itemEditing = value;
	}

	public TimelineCellViewModel GetCellModel(string taskId)
	{
		return CellModels.Where((TimelineCellViewModel c) => c.Id == taskId).FirstOrDefault();
	}

	public void OnSettingChanged(string name)
	{
		if (!(name == "Color"))
		{
			if (name == "ShowWeek")
			{
				OnPropertyChanged("ShowWeek");
				OnPropertyChanged("HeadHeight");
			}
			return;
		}
		OnPropertyChanged("ColorType");
		foreach (TimelineCellViewModel item in CellModels.ToList())
		{
			item.NotifyColorChanged();
		}
	}

	public void OnLocalSettingsChanged(string name)
	{
		if (!(name == "HideComplete"))
		{
			if (name == "ShowSubtasks")
			{
				ResetAllAsync();
			}
		}
		else
		{
			ResetAllAsync();
			OnPropertyChanged("HideCompleted");
		}
	}

	public void OnSortOptionChanged()
	{
		OnPropertyChanged("TimelineSortOption");
		UpdateTimelineSortType();
	}

	public async void ToggleGroupsOpen()
	{
		List<TimelineGroupViewModel> list = GroupModels.Where((TimelineGroupViewModel g) => g.ShowArrow == Visibility.Visible).ToList();
		if (ShowGroup && list.Count > 0)
		{
			bool allOpen = list.All((TimelineGroupViewModel g) => g.IsOpen);
			list.ForEach(delegate(TimelineGroupViewModel g)
			{
				g.ToggleOpen(!allOpen);
			});
			await UpdateCellLineAsync();
		}
	}

	private bool IsCellOverDue(TimelineCellViewModel model)
	{
		if (!(ProjectIdentity is NormalProjectIdentity) && !model.Editable)
		{
			return false;
		}
		if (model.DisplayModel == null)
		{
			return false;
		}
		DateTime? startDate = model.DisplayModel.StartDate;
		DateTime? dateTime = model.DisplayModel.DueDate;
		if (model.IsAllDay)
		{
			dateTime = dateTime?.AddDays(-1.0);
		}
		return (dateTime ?? startDate) < DateTime.Today;
	}

	private bool IsCellNoDate(TimelineCellViewModel model)
	{
		if (!(ProjectIdentity is NormalProjectIdentity) && !model.Editable)
		{
			return false;
		}
		if (!(model.DisplayModel?.DueDate).HasValue)
		{
			return !(model.DisplayModel?.StartDate).HasValue;
		}
		return false;
	}

	private void ClearArrangeModels()
	{
		NoDateModels.Clear();
		OverDueModels.Clear();
		ArrangeModels.Clear();
		UpdateArrangeEmptyTaskVisibility();
	}

	private void RemoveCellInArrangeCollection(List<TimelineDisplayBase> collection, TimelineCellViewModel cellModel)
	{
		int num = collection.IndexOf(cellModel) - 1;
		collection.Remove(cellModel);
		if (num > -1 && num < collection.Count && collection[num] is TimelineGroupViewModel && (collection.Count == num + 1 || (num + 1 < collection.Count && collection[num] is TimelineGroupViewModel && collection[num + 1] is TimelineGroupViewModel)))
		{
			collection.RemoveAt(num);
		}
	}

	private async Task UpdateCellToArrange(TimelineCellViewModel cellModel, bool remove = false)
	{
		await _updateArrangeSemaphoreSlim.WaitAsync();
		bool flag = false;
		try
		{
			flag = await UpdateCellToArrangeInternal(cellModel, remove);
		}
		finally
		{
			_updateArrangeSemaphoreSlim.Release();
		}
		if (!flag)
		{
			await UpdateSortArrangeAsync();
		}
		else if (_isArranging)
		{
			SetArrangeModels();
		}
	}

	private async Task<bool> UpdateCellToArrangeInternal(TimelineCellViewModel cellModel, bool remove)
	{
		if (cellModel?.DisplayModel == null)
		{
			return true;
		}
		remove = remove || cellModel.DisplayModel.Deleted != 0;
		if (cellModel.Status != 0)
		{
			RemoveCellInArrangeCollection(OverDueModels, cellModel);
			RemoveCellInArrangeCollection(NoDateModels, cellModel);
			return true;
		}
		bool flag = IsCellOverDue(cellModel);
		bool flag2 = IsCellNoDate(cellModel);
		if (!flag || remove)
		{
			RemoveCellInArrangeCollection(OverDueModels, cellModel);
		}
		if (!flag2 || remove)
		{
			RemoveCellInArrangeCollection(NoDateModels, cellModel);
		}
		if ((!flag && !flag2) || remove)
		{
			return true;
		}
		List<TimelineDisplayBase> list = (flag ? OverDueModels : NoDateModels);
		int num = list.IndexOf(cellModel);
		if (!(ProjectIdentity is NormalProjectIdentity))
		{
			if (num > 0)
			{
				for (int num2 = num; num2 > 0; num2--)
				{
					if (list[num2] is TimelineGroupViewModel timelineGroupViewModel && timelineGroupViewModel.Id == cellModel.DisplayModel.ProjectId)
					{
						return true;
					}
				}
			}
			return false;
		}
		if (num > 0)
		{
			int num3;
			for (num3 = num; num3 > 0; num3--)
			{
				if (list[num3] is TimelineGroupViewModel timelineGroupViewModel2 && timelineGroupViewModel2.Id == cellModel.DisplayModel.ColumnId)
				{
					return true;
				}
			}
			if (string.IsNullOrEmpty(cellModel.DisplayModel.ColumnId) && num3 == 0)
			{
				return true;
			}
		}
		return false;
	}

	public async Task UpdateSortArrangeAsync(bool updateGroup = true)
	{
		await _cellLock.WaitAsync();
		await _updateArrangeSemaphoreSlim.WaitAsync();
		try
		{
			List<TimelineCellViewModel> avaModels = CellModels.Where((TimelineCellViewModel c) => c.DisplayModel != null && c.DisplayModel.IsTaskOrNote && c.Status == 0).ToList();
			Constants.SortType sortType = ((!(ProjectIdentity is NormalProjectIdentity)) ? Constants.SortType.project : Constants.SortType.sortOrder);
			List<TimelineCellViewModel> overDueModels = avaModels.Where(IsCellOverDue).ToList();
			List<TimelineGroupViewModel> list = ((!updateGroup) ? OverDueModels.OfType<TimelineGroupViewModel>().ToList() : (await GetGroupModels(ProjectIdentity, sortType, overDueModels, containsNoChild: false)));
			List<TimelineGroupViewModel> groups = list;
			UpdateSortArrangeInternal(sortType, overDueModels, groups, OverDueModels);
			List<TimelineCellViewModel> noDateModels = avaModels.Where(IsCellNoDate).ToList();
			list = ((!updateGroup) ? NoDateModels.OfType<TimelineGroupViewModel>().ToList() : (await GetGroupModels(ProjectIdentity, sortType, noDateModels, containsNoChild: false)));
			List<TimelineGroupViewModel> groups2 = list;
			UpdateSortArrangeInternal(sortType, noDateModels, groups2, NoDateModels);
			if (_isArranging)
			{
				SetArrangeModels();
			}
		}
		finally
		{
			_updateArrangeSemaphoreSlim.Release();
			_cellLock.Release();
		}
	}

	private void UpdateArrangeEmptyTaskVisibility()
	{
		ArrangeEmptyTaskVisibility = ((ArrangeModels.Count > 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateSortArrangeInternal(Constants.SortType sortType, List<TimelineCellViewModel> cells, List<TimelineGroupViewModel> groups, List<TimelineDisplayBase> collection)
	{
		collection.Clear();
		if (sortType == Constants.SortType.tag)
		{
			cells.Sort(new SortHelper.TimelineTagComparer());
		}
		else
		{
			cells.Sort(TimelineCellViewModelCompare);
		}
		switch (sortType)
		{
		case Constants.SortType.project:
		{
			foreach (TimelineGroupViewModel group2 in groups)
			{
				collection.Add(group2);
				if (group2.IsArrangeOpen)
				{
					List<TimelineCellViewModel> children2 = cells.Where((TimelineCellViewModel item) => item.DisplayModel.ProjectId == group2.Id).ToList();
					IEnumerable<TimelineDisplayBase> collection4 = GetSortedResult(children2);
					collection.AddRange(collection4);
				}
			}
			break;
		}
		case Constants.SortType.sortOrder:
		{
			if (groups.Any())
			{
				foreach (TimelineGroupViewModel group in groups)
				{
					collection.Add(group);
					if (group.IsArrangeOpen)
					{
						List<TimelineCellViewModel> children = cells.Where((TimelineCellViewModel item) => item.DisplayModel.ColumnId == group.Id).ToList();
						IEnumerable<TimelineDisplayBase> collection2 = GetSortedResult(children);
						collection.AddRange(collection2);
					}
				}
				break;
			}
			IEnumerable<TimelineDisplayBase> collection3 = GetSortedResult(cells);
			collection.AddRange(collection3);
			break;
		}
		}
		IEnumerable<TimelineDisplayBase> GetSortedResult(List<TimelineCellViewModel> list)
		{
			List<TimelineCellViewModel> result = new List<TimelineCellViewModel>();
			List<TaskBaseViewModel> tasks = list.Select((TimelineCellViewModel m) => m.DisplayModel).ToList();
			Dictionary<string, TimelineCellViewModel> cDict = new Dictionary<string, TimelineCellViewModel>();
			foreach (TimelineCellViewModel item in list)
			{
				cDict[item.Id] = item;
				item.IsOpen = !_foldIds.Contains(item.Id);
				item.ParentItem = null;
				item.ChildItems.Clear();
			}
			foreach (Node<TaskBaseViewModel> item2 in TaskNodeUtils.GetTaskNodeTree(tasks))
			{
				if (!item2.HasParent)
				{
					AddChild(item2);
					foreach (Node<TaskBaseViewModel> item3 in item2.GetAllChildrenNode())
					{
						AddChild(item3);
					}
				}
			}
			return result;
			void AddChild(Node<TaskBaseViewModel> node)
			{
				TimelineCellViewModel timelineCellViewModel = cDict[node.NodeId];
				if (!string.IsNullOrEmpty(node.ParentId) && cDict.TryGetValue(node.ParentId, out var value))
				{
					timelineCellViewModel.ParentItem = value;
					value.ChildItems.Add(timelineCellViewModel);
					value.IsParent = LocalSettings.Settings.ExtraSettings.TimelineShowParent;
				}
				if (timelineCellViewModel.AllParentItemOpen())
				{
					result.Add(timelineCellViewModel);
				}
				timelineCellViewModel.Level = (ShowParentInArrange ? node.GetLevel(0) : 0);
			}
		}
	}

	private void SetArrangeModels()
	{
		List<TimelineDisplayBase> source = (IsOverDue ? OverDueModels : NoDateModels);
		if (ShowNoteInArrange)
		{
			ItemsSourceHelper.CopyTo(source, ArrangeModels);
		}
		else
		{
			ItemsSourceHelper.CopyTo(source.Where((TimelineDisplayBase m) => m.Type != DisplayType.Note).ToList(), ArrangeModels);
		}
		UpdateArrangeEmptyTaskVisibility();
	}

	public void ToggleArrangeItemOpen(TimelineCellViewModel model)
	{
		model.IsOpen = !model.IsOpen;
		if (model.IsOpen)
		{
			_foldIds.Remove(model.Id);
		}
		else
		{
			_foldIds.Add(model.Id);
		}
		UpdateSortArrangeAsync();
	}

	public void UpdateBatchSelectCellTime()
	{
		foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
		{
			UpdateCellTime(item, ignoreEdit: true, forceCheck: true);
			item.DragStatus = 0;
			item.Visibility = Visibility.Visible;
		}
	}

	public void BatchSelectInRect(Rect rect)
	{
		foreach (TimelineCellViewModel item in AvailableModels.Value)
		{
			if ((item.DisplayModel.IsTaskOrNote || item.DisplayModel.Type == DisplayType.Agenda) && item.Editable && !item.Operation.Contain(TimelineCellOperation.Fold) && new Rect(item.Left, item.Top, item.Width, 40.0).IntersectsWith(rect) && !item.Operation.Contain(TimelineCellOperation.BatchSelect))
			{
				item.Operation = item.Operation.Add(TimelineCellOperation.BatchSelect);
			}
		}
	}

	public void TryBatchSelectOnMove(Rect rect)
	{
		if (Math.Abs(rect.Width - _checkedWidth) < 5.0 && Math.Abs(rect.Height - _checkedHeight) < 5.0)
		{
			return;
		}
		_checkedWidth = rect.Width;
		_checkedHeight = rect.Height;
		foreach (TimelineCellViewModel item in AvailableModels.Value)
		{
			if ((!item.DisplayModel.IsTaskOrNote && item.DisplayModel.Type != DisplayType.Agenda) || !item.Editable || item.Operation.Contain(TimelineCellOperation.Fold))
			{
				continue;
			}
			if (new Rect(item.Left, item.Top, item.Width, 40.0).IntersectsWith(rect))
			{
				if (!item.Operation.Contain(TimelineCellOperation.BatchSelect))
				{
					item.Operation = item.Operation.Add(TimelineCellOperation.BatchSelect);
					_tempBatchSelectedModels.Add(item);
				}
			}
			else if (_tempBatchSelectedModels.Contains(item))
			{
				item.Operation = item.Operation.Remove(TimelineCellOperation.BatchSelect);
				_tempBatchSelectedModels.Remove(item);
			}
		}
	}

	public bool ClearBatchSelect()
	{
		if (_batchSelectedModels.Any())
		{
			foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
			{
				item.Operation = item.Operation.Remove(TimelineCellOperation.BatchSelect);
				item.DragStatus = 0;
			}
			_batchSelectedModels.Clear();
			return true;
		}
		return false;
	}

	public void ChangeBatchSelectStatus(TimelineCellViewModel model)
	{
		if (model.BatchSelected || ((model.DisplayModel.IsTaskOrNote || model.DisplayModel.Type == DisplayType.Agenda) && model.Editable && !model.Operation.Contain(TimelineCellOperation.Fold)))
		{
			if (model.Operation.Contain(TimelineCellOperation.BatchSelect))
			{
				model.Operation = model.Operation.Remove(TimelineCellOperation.BatchSelect);
				model.DragStatus = 0;
			}
			else
			{
				model.Operation = model.Operation.Add(TimelineCellOperation.BatchSelect);
			}
		}
	}

	public void OnItemBatchSelectChanged(TimelineCellViewModel model)
	{
		bool num = model.Operation.Contain(TimelineCellOperation.BatchSelect);
		bool flag = _batchSelectedModels.Contains(model);
		if (num && !flag)
		{
			_batchSelectedModels.Add(model);
		}
		if (!num && flag)
		{
			_batchSelectedModels.Remove(model);
			model.DragStatus = 0;
		}
		DelayActionHandlerCenter.TryDoAction("NotifyBatchSelectDateChanged", delegate
		{
			ThreadUtil.DetachedRunOnUiThread(delegate
			{
				OnPropertyChanged("HoverStartEndTuples");
				this.BatchSelectedChanged?.Invoke(this, null);
			});
		}, 100);
	}

	public async void ShowBatchOperationDialog(Action hideAction)
	{
	}

	public List<string> GetSelectedTaskIds()
	{
		return _batchSelectedModels.Select((TimelineCellViewModel m) => m.Id);
	}

	public void OnBatchDrag(int spanDays)
	{
		foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
		{
			item.TrySetStartDate(item.StartDate.AddDays(spanDays));
			item.EndDate = item.EndDate?.AddDays(spanDays);
			item.SetLeft();
		}
		OnPropertyChanged("HoverStartEndTuples");
	}

	public async void SetBatchDragging(int status, bool delay = false)
	{
		if (delay)
		{
			await Task.Delay(1);
		}
		foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
		{
			item.DragStatus = status;
		}
	}

	public int GetSelectedTaskCounts()
	{
		return _batchSelectedModels.Count;
	}

	public void ClearTempBatchSelect()
	{
		_tempBatchSelectedModels.Clear();
		_checkedWidth = 0.0;
		_checkedHeight = 0.0;
	}

	public bool IsBatchSelectOverGroup()
	{
		if (_batchSelectedModels.Count <= 1)
		{
			return false;
		}
		string text = null;
		foreach (TimelineCellViewModel model in _batchSelectedModels.ToList())
		{
			string text2 = GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= model.Line)?.Id ?? string.Empty;
			if (text == null)
			{
				text = text2;
			}
			if (text2 != text)
			{
				return true;
			}
		}
		return false;
	}

	public async Task OnBatchDragDrop(int? line)
	{
		bool num = line.HasValue && (TimelineSortOption.groupByType == Constants.SortType.sortOrder || TimelineSortOption.groupByType == Constants.SortType.project);
		List<string> selectedIds = GetSelectedTaskIds();
		List<string> ids = (num ? TaskDao.GetTreeTopIds(selectedIds, string.Empty) : selectedIds);
		if (line.HasValue && !_highLightHover)
		{
			await CommitBatchGroup(ids, line.Value);
		}
		HashSet<string> matchedIds = null;
		if (ProjectIdentity is FilterProjectIdentity)
		{
			List<TaskBaseViewModel> matchedTasks = TaskViewModelHelper.GetMatchedTasks(ProjectIdentity, selectedIds);
			matchedTasks?.RemoveAll((TaskBaseViewModel t) => HideCompleted && t.Status != 0);
			matchedIds = new HashSet<string>(matchedTasks?.Select((TaskBaseViewModel m) => m.Id) ?? new List<string>());
		}
		foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
		{
			if (matchedIds == null || matchedIds.Contains(item.Id))
			{
				await item.CommitDate(null);
			}
			else
			{
				RemoveCell(item);
			}
		}
	}

	public async Task CommitBatchGroup(List<string> ids, int line)
	{
		string groupBy = TimelineSortOption.groupBy;
		TimelineGroupViewModel group = GroupModels.LastOrDefault((TimelineGroupViewModel c) => c.Line <= line);
		if (group == null)
		{
			return;
		}
		switch (groupBy)
		{
		case "priority":
		{
			if (int.TryParse(group.Id ?? "", out var result))
			{
				await TaskService.BatchSetPriority(ids, result);
			}
			break;
		}
		case "project":
		{
			List<TaskBaseViewModel> taskAndChildrenInBatch = TaskCache.GetTaskAndChildrenInBatch(ids);
			ProjectModel projectById = CacheManager.GetProjectById(group.Id);
			if (projectById != null && projectById.IsEnable() && taskAndChildrenInBatch != null && taskAndChildrenInBatch.Count > 0 && taskAndChildrenInBatch[0].ProjectId != group.Id)
			{
				await TaskService.BatchMoveProject(taskAndChildrenInBatch.Select((TaskBaseViewModel s) => s.Id).ToList(), new MoveProjectArgs(projectById));
			}
			break;
		}
		case "assignee":
		{
			TaskBaseViewModel taskById2 = TaskCache.GetTaskById(ids[0]);
			if (taskById2 == null || !(taskById2.Assignee != group.Id))
			{
				break;
			}
			if (!string.IsNullOrEmpty(group.Id) && group.Id != "-1")
			{
				List<string> list = TaskCache.GetTasksByIds(ids)?.Select((TaskBaseViewModel m) => m.ProjectId).Distinct().ToList();
				if (list != null && list.Count > 0)
				{
					foreach (string item in list)
					{
						List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(item);
						if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != group.Id))
						{
							Utils.Toast(Utils.GetString("ChangeAssigneeError"));
							return;
						}
					}
				}
			}
			await TaskService.BatchSetAssignee(ids, group.Id);
			break;
		}
		default:
			foreach (string id in ids)
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(id);
				if (taskById != null && taskById.ColumnId != group.Id)
				{
					if (!string.IsNullOrEmpty(taskById.ParentId))
					{
						await TaskDao.UpdateParent(id, string.Empty);
					}
					await TaskService.BatchSaveTasksColumnId(ids, group.Id, group.ProjectId);
				}
			}
			break;
		case "tag":
			break;
		}
		if (!group.IsOpen)
		{
			UpdateGroupAsync();
		}
	}

	public void ResetBatchDates()
	{
		foreach (TimelineCellViewModel item in _batchSelectedModels.ToList())
		{
			item.EndDate = item.DisplayModel.DueDate;
			item.TrySetStartDate(item.DisplayModel.StartDate);
			item.SetLeft();
		}
	}

	private static int SortInternal(List<TimelineCellViewModel> cells, int startLine, Constants.SortType sortType = Constants.SortType.sortOrder, bool notify = true)
	{
		if (cells == null)
		{
			return 0;
		}
		if (sortType == Constants.SortType.tag)
		{
			cells.Sort(new SortHelper.TimelineTagComparer());
		}
		else
		{
			cells.Sort(TimelineCellViewModelCompare);
		}
		int num = startLine;
		while (cells.Any())
		{
			TimelineCellViewModel timelineCellViewModel = cells.First();
			timelineCellViewModel.SetLine(num, notify);
			timelineCellViewModel.MaxWidth = double.PositiveInfinity;
			while (timelineCellViewModel != null)
			{
				TimelineCellViewModel cellViewModel = timelineCellViewModel;
				cells.Remove(timelineCellViewModel);
				TimelineCellViewModel timelineCellViewModel2 = cells.FirstOrDefault(delegate(TimelineCellViewModel c)
				{
					DateTime? endDate = cellViewModel.EndDate;
					if (endDate.HasValue)
					{
						DateTime valueOrDefault = endDate.GetValueOrDefault();
						if (cellViewModel.IsAllDay || valueOrDefault == valueOrDefault.Date)
						{
							return valueOrDefault.AddSeconds(-1.0).Date < c.StartDate.Date;
						}
						return valueOrDefault.Date < c.StartDate.Date;
					}
					return cellViewModel.StartDate.Date < c.StartDate.Date;
				});
				if (timelineCellViewModel2 != null)
				{
					timelineCellViewModel.MaxWidth = Math.Max(timelineCellViewModel2.Left - timelineCellViewModel.Left, 6.0);
					timelineCellViewModel2.SetLine(num, notify);
					cells.Remove(timelineCellViewModel2);
				}
				else
				{
					timelineCellViewModel.MaxWidth = double.PositiveInfinity;
				}
				timelineCellViewModel = timelineCellViewModel2;
			}
			num++;
		}
		return num - startLine;
	}

	private static int TimelineCellViewModelCompare(TimelineCellViewModel a, TimelineCellViewModel b)
	{
		return CalendarSortHelper.CompareForTimelineView(a, b, DateTime.Today, isAllDaySection: false);
	}

	private int SortColumns(ProjectIdentity project, List<TimelineCellViewModel> availableTask, Constants.SortType orderBy, bool notify)
	{
		int num = 0;
		List<string> list = GroupModels.Select((TimelineGroupViewModel g) => g.Id).ToList();
		foreach (TimelineGroupViewModel column in GroupModels)
		{
			if (column.ProjectId != project.GetProjectId())
			{
				continue;
			}
			int num2 = 1;
			List<TimelineCellViewModel> tasks = availableTask.Where((TimelineCellViewModel item) => item.DisplayModel.ColumnId == column.Id).ToList();
			if (num == 0)
			{
				string lastDefaultColumnId = TaskCache.GetLastDefaultColumnId(project.GetProjectId());
				if (!string.IsNullOrEmpty(lastDefaultColumnId) && lastDefaultColumnId != column.Id)
				{
					foreach (TimelineCellViewModel item in availableTask)
					{
						if (string.IsNullOrEmpty(item.DisplayModel.ColumnId) || !list.Contains(item.DisplayModel.ColumnId))
						{
							TaskService.SaveTaskColumnId(item.Id, lastDefaultColumnId, checkEmpty: true);
							tasks.Add(item);
						}
					}
					TaskCache.SetLastDefaultColumnId(project.GetProjectId(), column.Id);
				}
				else if (string.IsNullOrEmpty(lastDefaultColumnId))
				{
					TaskCache.SetLastDefaultColumnId(project.GetProjectId(), column.Id);
					foreach (TimelineCellViewModel item2 in availableTask)
					{
						if (string.IsNullOrEmpty(item2.DisplayModel.ColumnId) || !list.Contains(item2.DisplayModel.ColumnId))
						{
							tasks.Add(item2);
						}
					}
				}
			}
			availableTask.RemoveAll((TimelineCellViewModel c) => tasks.Contains(c));
			if (!column.IsOpen)
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					t.Operation = TimelineCellOperation.Fold;
				});
			}
			else if (tasks.Any())
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					if (t.Operation.IsNormalStatus())
					{
						t.Operation = TimelineCellOperation.None;
					}
				});
				num2 += ((orderBy == Constants.SortType.sortOrder) ? SortInternal(tasks, num, Constants.SortType.sortOrder, notify) : SortDueDate(tasks, num, Constants.SortType.sortOrder, notify));
			}
			column.Height = (double)num2 * OneLineHeight;
			column.Line = num;
			num += num2;
		}
		return num;
	}

	private int SortTag(List<TimelineCellViewModel> availableTask, Constants.SortType sortType, bool notify = true)
	{
		int num = 0;
		Dictionary<string, long> sortedTagDict = TagDataHelper.GetTagSortDict();
		foreach (TimelineGroupViewModel gp in GroupModels)
		{
			List<TimelineCellViewModel> tasks = ((gp.Id == "!tag") ? availableTask.Where((TimelineCellViewModel item) => item.DisplayModel.Tags == null || item.DisplayModel.Tags.Length == 0).ToList() : availableTask.Where((TimelineCellViewModel item) => item.DisplayModel.Tags != null && item.DisplayModel.Tags.Any() && TagDataHelper.GetPrimaryTag(sortedTagDict, item.DisplayModel.Tags, null) == gp.Id).ToList());
			int num2 = 1;
			availableTask.RemoveAll((TimelineCellViewModel t) => tasks.Contains(t));
			if (!gp.IsOpen)
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					t.Operation = TimelineCellOperation.Fold;
				});
			}
			else if (tasks.Any())
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					if (t.Operation == TimelineCellOperation.Fold)
					{
						t.Operation = TimelineCellOperation.None;
					}
				});
				num2 += ((sortType == Constants.SortType.sortOrder) ? SortInternal(tasks, num, Constants.SortType.tag, notify) : SortDueDate(tasks, num, Constants.SortType.tag, notify));
			}
			gp.Height = (double)num2 * OneLineHeight;
			gp.Line = num;
			num += num2;
		}
		return num;
	}

	private int SortPriority(List<TimelineCellViewModel> availableTask, Constants.SortType sortType, bool notify = true)
	{
		int num = 0;
		foreach (TimelineGroupViewModel gp in GroupModels)
		{
			List<TimelineCellViewModel> tasks = ((gp.Id == "note") ? availableTask.Where((TimelineCellViewModel item) => item.DisplayModel.IsNote).ToList() : availableTask.Where((TimelineCellViewModel item) => !item.DisplayModel.IsNote && item.DisplayModel.Priority.ToString() == gp.Id).ToList());
			int num2 = 1;
			availableTask.RemoveAll((TimelineCellViewModel t) => tasks.Contains(t));
			if (!gp.IsOpen)
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					t.Operation = TimelineCellOperation.Fold;
				});
			}
			else if (tasks.Any())
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					if (t.Operation == TimelineCellOperation.Fold)
					{
						t.Operation = TimelineCellOperation.None;
					}
				});
				num2 += ((sortType == Constants.SortType.sortOrder) ? SortInternal(tasks, num, Constants.SortType.sortOrder, notify) : SortDueDate(tasks, num, Constants.SortType.sortOrder, notify));
			}
			gp.Height = (double)num2 * OneLineHeight;
			gp.Line = num;
			num += num2;
		}
		return num;
	}

	private int SortProjects(List<TimelineCellViewModel> availableTask, Constants.SortType orderBy, bool notify = true)
	{
		int num = 0;
		foreach (TimelineGroupViewModel gp in GroupModels)
		{
			List<TimelineCellViewModel> tasks = availableTask.Where((TimelineCellViewModel item) => item.DisplayModel.IsEvent ? (gp.Id == "Calendar5959a2259161d16d23a4f272") : (item.DisplayModel.ProjectId == gp.Id)).ToList();
			int num2 = 1;
			availableTask.RemoveAll((TimelineCellViewModel t) => tasks.Contains(t));
			if (!gp.IsOpen)
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					t.Operation = TimelineCellOperation.Fold;
				});
			}
			else if (tasks.Any())
			{
				tasks.ForEach(delegate(TimelineCellViewModel t)
				{
					if (t.Operation == TimelineCellOperation.Fold)
					{
						t.Operation = TimelineCellOperation.None;
					}
				});
				num2 += ((orderBy == Constants.SortType.sortOrder) ? SortInternal(tasks, num, Constants.SortType.sortOrder, notify) : SortDueDate(tasks, num, Constants.SortType.sortOrder, notify));
			}
			gp.Height = (double)num2 * OneLineHeight;
			gp.Line = num;
			num += num2;
		}
		return num;
	}

	private int SortDueDate(List<TimelineCellViewModel> availableTask, int start, Constants.SortType groupBy = Constants.SortType.sortOrder, bool notify = true)
	{
		int num = 0;
		if (groupBy == Constants.SortType.tag)
		{
			availableTask.Sort(new SortHelper.TimelineTagComparer());
		}
		else
		{
			availableTask.Sort(TimelineCellViewModelCompare);
		}
		foreach (TimelineCellViewModel item in availableTask)
		{
			item.SetLine(start + num, notify);
			if (item.Operation.IsNormalStatus())
			{
				item.Operation = TimelineCellOperation.None;
			}
			item.MaxWidth = double.PositiveInfinity;
			num++;
		}
		return num;
	}

	private int SortAssignee(List<TimelineCellViewModel> availableTask, Constants.SortType orderBy, bool notify = true)
	{
		int num = 0;
		foreach (TimelineGroupViewModel group in GroupModels)
		{
			int num2 = 1;
			List<TimelineCellViewModel> list;
			if (group.Id == "-1")
			{
				list = availableTask.Where((TimelineCellViewModel c) => c.DisplayModel.GetAssignee() == group.Id).ToList();
				list.AddRange(availableTask.Where((TimelineCellViewModel c) => GroupModels.All((TimelineGroupViewModel g) => g.Id != c.DisplayModel.GetAssignee())));
			}
			else
			{
				list = availableTask.Where((TimelineCellViewModel c) => c.DisplayModel.GetAssignee() == group.Id).ToList();
			}
			if (list.Any())
			{
				list.ForEach(delegate(TimelineCellViewModel t)
				{
					if (t.Operation.IsNormalStatus())
					{
						t.Operation = TimelineCellOperation.None;
					}
				});
				num2 += ((orderBy == Constants.SortType.sortOrder) ? SortInternal(list, num, Constants.SortType.sortOrder, notify) : SortDueDate(list, num, Constants.SortType.sortOrder, notify));
			}
			group.Height = (double)num2 * OneLineHeight;
			group.Line = num;
			num += num2;
		}
		return num;
	}
}
