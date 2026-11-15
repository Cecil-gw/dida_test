using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KotlinModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar.Agenda;
using ticktick_WPF.Views.Calendar.Month;
using ticktick_WPF.Views.Calendar.Week;
using ticktick_WPF.Views.Calendar.Year;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Calendar;

public class CalendarControl : UserControl, IBatchTaskSelector
{
	private RenderTargetBitmap _bmp;

	private bool _editting;

	private int _lastMouseMoveOffset;

	private DateTime? _lastMouseMoveTime;

	public MultiWeekControl MultiWeek;

	public MultiDayView MultiDay;

	public CalendarAgendaView AgendaView;

	public YearCalendarControl YearCalendar;

	private readonly string _calUid = Utils.GetGuid0();

	private SettingDialog _settingDialog;

	public Dictionary<string, int> BatchSelectedRow = new Dictionary<string, int>();

	private bool _isVirtual;

	private CalMoreItems _calMorePopup;

	private DateTime? _lastScrollTime;

	private bool _columnDragging;

	private ScaleTransform _miniCalScale;

	private ScaleTransform _arrangeScale;

	public bool ParallelShow;

	private bool _loaded;

	private bool _eventLoaded;

	private BatchTaskControl _batchControl;

	private bool _batchHandling;

	private DateTime? _calendarDate;

	private DateTime? _calendarInListDate;

	private string _copiedTaskSelectId;

	private int _pomoReloadScheduled;

	public Grid Container;

	public Grid CalendarBody;

	public Border CalendarSideBorder;

	public CalendarHead HeadView;

	public CalendarSideBar CalendarSide;

	public ContentControl MonthGrid;

	public ContentControl WeekGrid;

	public ArrangeTaskPanel ArrangePanel;

	public ContentControl AgendaContent;

	private ColumnDefinition Column1;

	private ColumnDefinition Column2;

	private ColumnDefinition Column3;

	private BlurEffect CalendarBlurEffect;

	private CalProToastBorder ProToastBorder;

	private GridSplitter Splitter1;

	private GridSplitter Splitter2;

	public bool IsLocked { get; set; }

	public double TimeScrollVerticalOffset { get; set; }

	public CalendarViewSourceType ViewSourceType { get; set; }

	public BlockingSet<string> BatchSelected { get; set; } = new BlockingSet<string>();

	public bool IsVirtual
	{
		get
		{
			if (_isVirtual)
			{
				if (ViewSourceType == CalendarViewSourceType.Widget)
				{
					return !ABTestManager.UsePremiumV2CalendarProStyle();
				}
				return true;
			}
			return false;
		}
		set
		{
			_isVirtual = value;
		}
	}

	public bool LastShowIconState { get; set; }

	public CalendarControl()
	{
		InitUI();
		ViewSourceType = CalendarViewSourceType.Widget;
		InitEvents();
		LoadDefault();
		base.Loaded += OnControlLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		CalendarSide.SetCalendar(this);
		UtilLog.Info("CalendarFilter : " + LocalSettings.Settings.CalendarFilterData);
	}

	public CalendarControl(CalendarViewSourceType type)
	{
		InitUI();
		ViewSourceType = type;
		InitEvents();
		LoadDefault();
		base.Loaded += OnControlLoaded;
		base.Unloaded += delegate
		{
			UnbindEvents();
		};
		CalendarSide.SetCalendar(this);
		UtilLog.Info("CalendarFilter : " + LocalSettings.Settings.CalendarFilterData);
	}

	private void InitEvents()
	{
		InitHeadEvents();
		InitArrangeEvents();
	}

	public void UnbindEvents()
	{
		_eventLoaded = false;
		TasksChangeEventManager.RemoveHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
		CalendarEventChangeNotifier.Deleted -= Reload;
		CalendarEventChangeNotifier.Changed -= Reload;
		CalendarEventChangeNotifier.RemoteChanged -= DelayReloadCalendar;
		CalendarEventChangeNotifier.FontSizeChanged -= OnFontSizeChanged;
		DataChangedNotifier.HabitCheckInChanged -= OnCheckInChanged;
		DataChangedNotifier.HabitsChanged -= OnHabitsChanged;
		DataChangedNotifier.HabitSkip -= OnHabitSkip;
		DataChangedNotifier.ThemeModeChanged -= OnIsDarkChanged;
		DataChangedNotifier.CalendarConfigChanged -= ReloadOnConfigChanged;
		DataChangedNotifier.CalendarShowIconChanged -= OnShowIconChanged;
		DataChangedNotifier.CalendarProjectFilterChanged -= ReloadOnFilterChanged;
		DataChangedNotifier.CalendarChanged -= OnCalendarChanged;
		DataChangedNotifier.CalendarShowYearHeatMapChanged -= OnShowYearHeatMapChanged;
		DataChangedNotifier.CalendarTaskBarStyleChanged -= OnCalendarTaskBarStyleChanged;
		DataChangedNotifier.EventArchivedChanged -= Reload;
		DataChangedNotifier.WeekStartFromChanged -= OnWeekStartChanged;
		DataChangedNotifier.YearStartFromChanged -= Reload;
		DataChangedNotifier.ProjectChanged -= Reload;
		DataChangedNotifier.TagDeleted -= Reload;
		DataChangedNotifier.TagChanged -= Reload;
		DataChangedNotifier.ScheduleChanged -= Reload;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
		TimeChangeNotifier.DayChanged -= OnDayChanged;
		DataChangedNotifier.SyncDone -= OnSyncChanged;
		DataChangedNotifier.NewTaskWhenModifyRepeat -= OnNewTaskWhenModifyRepeat;
		GlobalEventManager.ReloadCalendar -= ForceReload;
		GlobalEventManager.ModifyRecurrenceCompleted -= OnRecurrenceModified;
		DataChangedNotifier.CountdownsChanged -= OnCountdownChanged;
		PomoNotifier.Changed -= OnPomoChanged;
		PomoNotifier.ServiceChanged -= OnPomoChanged;
		PomoNotifier.LinkChanged -= OnPomoChanged;
		DelayActionHandlerCenter.RemoveAction(_calUid);
		DelayActionHandlerCenter.RemoveAction(_calUid + "PomoReload");
		Interlocked.Exchange(ref _pomoReloadScheduled, 0);
	}

	private void OnCountdownChanged(object sender, EventArgs e)
	{
		if (base.IsVisible && LocalSettings.Settings.EnableCountDownDays)
		{
			Reload(force: false, reloadArrange: false, reloadFilter: false);
		}
	}

	private void OnNewTaskWhenModifyRepeat(string id, string originId)
	{
		if (BatchSelected.Contains(originId))
		{
			BatchSelected.Remove(originId);
			BatchSelectedRow.Remove(originId);
			BatchSelected.Add(id);
			DelayActionHandlerCenter.TryDoAction(_calUid + "ResetBatchSelected", delegate
			{
				_batchControl.SetTaskIds(BatchSelected.ToList(), isNote: false, useInList: false);
			}, 1000);
		}
	}

	private void OnWeekStartChanged(object sender, EventArgs e)
	{
		MultiWeekControl multiWeek = MultiWeek;
		if (multiWeek != null && multiWeek.IsVisible)
		{
			MultiWeek?.OnShowWeekChanged(checkWeekStart: true);
		}
		MultiDayView multiDay = MultiDay;
		if (multiDay != null && multiDay.IsVisible)
		{
			SetCalendarSideSelectRange();
		}
		if (base.Visibility == Visibility.Visible)
		{
			Navigate().Reload();
		}
	}

	private void OnShowWeekChanged(object sender, PropertyChangedEventArgs e)
	{
		if (HeadView.Mode == "1")
		{
			MultiDay?.OnShowWeekChanged(LocalSettings.Settings.ShowCalWeekend);
		}
		MultiWeek?.OnShowWeekChanged();
	}

	public void BindEvents()
	{
		if (!_eventLoaded)
		{
			_eventLoaded = true;
			TasksChangeEventManager.AddHandler(TaskChangeNotifier.Notifier, OnTasksChanged);
			CalendarEventChangeNotifier.Deleted += Reload;
			CalendarEventChangeNotifier.Changed += Reload;
			CalendarEventChangeNotifier.RemoteChanged += DelayReloadCalendar;
			CalendarEventChangeNotifier.FontSizeChanged += OnFontSizeChanged;
			DataChangedNotifier.HabitCheckInChanged += OnCheckInChanged;
			DataChangedNotifier.HabitsChanged += OnHabitsChanged;
			DataChangedNotifier.HabitSkip += OnHabitSkip;
			DataChangedNotifier.ThemeModeChanged += OnIsDarkChanged;
			DataChangedNotifier.CalendarConfigChanged += ReloadOnConfigChanged;
			DataChangedNotifier.CalendarShowIconChanged += OnShowIconChanged;
			DataChangedNotifier.CalendarProjectFilterChanged += ReloadOnFilterChanged;
			DataChangedNotifier.CalendarChanged += OnCalendarChanged;
			DataChangedNotifier.CalendarShowYearHeatMapChanged += OnShowYearHeatMapChanged;
			DataChangedNotifier.CalendarTaskBarStyleChanged += OnCalendarTaskBarStyleChanged;
			DataChangedNotifier.EventArchivedChanged += Reload;
			DataChangedNotifier.WeekStartFromChanged += OnWeekStartChanged;
			DataChangedNotifier.YearStartFromChanged += Reload;
			DataChangedNotifier.ProjectChanged += Reload;
			DataChangedNotifier.TagDeleted += Reload;
			DataChangedNotifier.TagChanged += Reload;
			DataChangedNotifier.FilterChanged += OnFilterChanged;
			DataChangedNotifier.ScheduleChanged += Reload;
			DataChangedNotifier.SyncDone += OnSyncChanged;
			DataChangedNotifier.NewTaskWhenModifyRepeat += OnNewTaskWhenModifyRepeat;
			DataChangedNotifier.CountdownsChanged += OnCountdownChanged;
			GlobalEventManager.ReloadCalendar += ForceReload;
			GlobalEventManager.ModifyRecurrenceCompleted += OnRecurrenceModified;
			TimeChangeNotifier.DayChanged += OnDayChanged;
			PomoNotifier.Changed += OnPomoChanged;
			PomoNotifier.ServiceChanged += OnPomoChanged;
			PomoNotifier.LinkChanged += OnPomoChanged;
			PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnShowWeekChanged, "ShowCalWeekend");
			PropertyChangedEventManager.AddHandler(LocalSettings.Settings, OnShowWeekChanged, "WeekStartFrom");
		}
	}

	private void OnCalendarTaskBarStyleChanged(object sender, EventArgs e)
	{
		if (MultiDay != null && MultiDay.IsVisible)
		{
			MultiDay.SetColor();
		}
		if (MultiWeek != null && MultiWeek.IsVisible)
		{
			MultiWeek.SetColor();
		}
	}

	private void OnShowYearHeatMapChanged(object sender, bool e)
	{
	}

	private void OnFontSizeChanged(object sender, EventArgs e)
	{
		MultiDay?.OnFontSizeChanged();
		MultiWeek?.OnFontSizeChanged();
	}

	private void OnCalendarChanged(object sender, EventArgs e)
	{
		CalendarSide.ReloadFilter();
		Navigate().Reload();
	}

	private void OnSyncChanged(object sender, SyncResult e)
	{
		if (base.IsVisible && e.RemoteTagChanged)
		{
			ProjectExtra projectExtra = ((ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
			if (CalendarUtils.GetTaskColorMode() == CalTaskColorMode.Tag || (projectExtra.Tags != null && projectExtra.Tags.Any()))
			{
				Navigate().Reload();
			}
			else
			{
				CalendarSide.ReloadFilter();
			}
			ArrangePanel.TryResetTagFilterText();
		}
	}

	private void OnDayChanged(object sender, EventArgs e)
	{
		Navigate().Reload();
		ArrangePanel.TryReloadTasks();
		CalendarSide.DayPicker.SetDayCells();
	}

	private async void OnTasksChanged(object sender, TasksChangeEventArgs e)
	{
		bool flag = base.Visibility == Visibility.Visible;
		if (flag)
		{
			bool flag2 = AgendaContent.Visibility == Visibility.Visible && (e.PriorityChangedIds.Any() || e.SortOrderChangedIds.Any());
			if (!flag2)
			{
				bool flag3 = e.BatchChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckChangedTaskMatch(e.BatchChangedIds);
				}
				flag2 = flag3;
			}
			bool flag4 = flag2;
			if (!flag4)
			{
				bool flag3 = e.DeletedChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckTaskDeletedIds(e.DeletedChangedIds.Value);
				}
				flag4 = flag3;
			}
			bool flag5 = flag4;
			if (!flag5)
			{
				bool flag3 = e.UndoDeletedIds.Any();
				if (flag3)
				{
					flag3 = await CheckTaskDeletedIds(e.UndoDeletedIds.Value);
				}
				flag5 = flag3;
			}
			bool flag6 = flag5;
			if (!flag6)
			{
				bool flag3 = e.AddIds.Any();
				if (flag3)
				{
					flag3 = await CheckAddTasksMatch(e.AddIds);
				}
				flag6 = flag3;
			}
			bool flag7 = flag6;
			if (!flag7)
			{
				bool flag3 = e.StatusChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckChangedTaskMatch(e.StatusChangedIds);
				}
				flag7 = flag3;
			}
			bool flag8 = flag7;
			if (!flag8)
			{
				bool flag3 = e.KindChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckChangedTaskMatch(e.KindChangedIds);
				}
				flag8 = flag3;
			}
			bool flag9 = flag8;
			if (!flag9)
			{
				bool flag3 = e.ProjectChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckChangedTaskMatch(e.ProjectChangedIds);
				}
				flag9 = flag3;
			}
			bool flag10 = flag9;
			if (!flag10)
			{
				bool flag3 = e.DateChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckChangedTaskMatch(e.DateChangedIds);
				}
				flag10 = flag3;
			}
			bool flag11 = flag10;
			if (!flag11)
			{
				bool flag3 = e.PriorityChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckPriorityChangedIds(e.PriorityChangedIds);
				}
				flag11 = flag3;
			}
			bool flag12 = flag11;
			if (!flag12)
			{
				bool flag3 = e.TagChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckTagChangedIds(e.TagChangedIds);
				}
				flag12 = flag3;
			}
			bool flag13 = flag12;
			if (!flag13)
			{
				bool flag3 = e.AssignChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckAssignChangedIds(e.AssignChangedIds);
				}
				flag13 = flag3;
			}
			bool flag14 = flag13;
			if (!flag14)
			{
				bool flag3 = e.CheckItemChangedIds.Any();
				if (flag3)
				{
					flag3 = await CheckItemsChangedIds(e.CheckItemChangedIds);
				}
				flag14 = flag3;
			}
			flag = flag14;
		}
		if (flag)
		{
			Reload(force: true, reloadArrange: false, reloadFilter: false);
		}
		if (e.PinChangedIds.Count > 0)
		{
			ArrangePanel?.TryReloadTasks();
		}
	}

	private async Task<bool> CheckItemsChangedIds(BlockingSet<string> ids)
	{
		if (LocalSettings.Settings.ShowCheckListInCal)
		{
			var (startDate, endDate) = Navigate().GetTaskTimeSpan();
			if (Navigate().ExistTasks(ids.Value) > 0)
			{
				return true;
			}
			List<TaskBaseViewModel> matchedItemsInCal = TaskViewModelHelper.GetMatchedItemsInCal(startDate, endDate, ids.ToList(), ViewSourceType == CalendarViewSourceType.WithList);
			return matchedItemsInCal != null && matchedItemsInCal.Count > 0;
		}
		return false;
	}

	private async Task<bool> CheckAddTasksMatch(BlockingSet<string> ids)
	{
		var (startDate, dateTime) = Navigate().GetTaskTimeSpan();
		return (await TaskViewModelHelper.GetMatchedTasksInCal(startDate, dateTime.AddDays(1.0), ids.ToList(), ViewSourceType == CalendarViewSourceType.WithList)).Count > 0;
	}

	private async Task<bool> CheckAssignChangedIds(BlockingSet<string> ids)
	{
		ProjectExtra projectFilter = ((ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		bool flag = projectFilter.IsAll;
		if (!flag && projectFilter.FilterIds.Any())
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0]);
			if (filterModel != null)
			{
				if (Parser.GetFilterRuleType(filterModel.rule) == 0)
				{
					if (Parser.ToNormalModel(filterModel.rule).Assignees.Any())
					{
						flag = true;
					}
				}
				else if (Parser.ToAdvanceModel(filterModel.rule).CardList.Any((CardViewModel c) => c.ConditionName == "assignee"))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			return await CheckChangedTaskMatch(ids);
		}
		return false;
	}

	private async Task<bool> CheckTagChangedIds(BlockingSet<string> ids)
	{
		ProjectExtra projectFilter = ((ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		bool flag = projectFilter.Tags.Any();
		if (!flag && projectFilter.FilterIds.Any())
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0]);
			if (filterModel != null && (FilterViewModel.CalculateTaskDefault(filterModel.rule).DefaultTags?.Any() ?? false))
			{
				flag = true;
			}
		}
		if (flag)
		{
			return await CheckChangedTaskMatch(ids, checkCount: false);
		}
		return false;
	}

	private async Task<bool> CheckPriorityChangedIds(BlockingSet<string> ids)
	{
		ProjectExtra projectFilter = ((ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (projectFilter.FilterIds.Any())
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0]);
			if (filterModel != null && FilterViewModel.CalculateTaskDefault(filterModel.rule).Priority.HasValue)
			{
				return await CheckChangedTaskMatch(ids, checkCount: false);
			}
		}
		return false;
	}

	private async Task<bool> CheckChangedTaskMatch(BlockingSet<string> ids, bool checkCount = true)
	{
		(DateTime, DateTime) taskTimeSpan = Navigate().GetTaskTimeSpan();
		DateTime item = taskTimeSpan.Item1;
		DateTime item2 = taskTimeSpan.Item2;
		int modelCount = Navigate().ExistTasks(ids.Value);
		if (checkCount && modelCount > 0)
		{
			return true;
		}
		return (await TaskViewModelHelper.GetMatchedTasksInCal(item, item2, ids.ToList(), ViewSourceType == CalendarViewSourceType.WithList)).Count != modelCount;
	}

	private async Task<bool> CheckTaskDeletedIds(HashSet<string> ids)
	{
		(DateTime, DateTime) taskTimeSpan = Navigate().GetTaskTimeSpan();
		DateTime item = taskTimeSpan.Item1;
		DateTime item2 = taskTimeSpan.Item2;
		int modelCount = Navigate().ExistTasks(ids);
		if (modelCount != (await TaskViewModelHelper.GetMatchedTasksInCal(item, item2, ids.ToList(), ViewSourceType == CalendarViewSourceType.WithList)).Count)
		{
			return true;
		}
		return false;
	}

	private void OnPomoChanged(object sender, object e)
	{
		if (Interlocked.Exchange(ref _pomoReloadScheduled, 1) != 0)
		{
			return;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (!base.IsVisible || !LocalSettings.Settings.ShowFocusRecord)
			{
				Interlocked.Exchange(ref _pomoReloadScheduled, 0);
			}
			else
			{
				DelayActionHandlerCenter.TryDoAction(_calUid + "PomoReload", delegate
				{
					base.Dispatcher.Invoke(delegate
					{
						try
						{
							if (base.IsVisible && LocalSettings.Settings.ShowFocusRecord)
							{
								Reload(force: false, reloadArrange: false, reloadFilter: false);
							}
						}
						finally
						{
							Interlocked.Exchange(ref _pomoReloadScheduled, 0);
						}
					});
				}, 120);
			}
		});
	}

	private async void OnRecurrenceModified(object sender, EventArgs e)
	{
		await Task.Delay(50);
		if (_editting && !PopupStateManager.IsViewPopOpened())
		{
			_editting = false;
		}
	}

	private void ForceReload(object sender, EventArgs e)
	{
		if (base.IsVisible)
		{
			Reload(force: true);
		}
	}

	private void OnIsDarkChanged(object sender, EventArgs e)
	{
		if (base.IsVisible)
		{
			Reload(force: true, reloadArrange: true, reloadFilter: false, delay: false, resetDatePickerColor: true, setWeekend: false, byThemeChange: true);
		}
	}

	private void OnHabitSkip(object sender, string e)
	{
		if (base.IsVisible && LocalSettings.Settings.HabitInCal)
		{
			Reload(force: false, reloadArrange: false, reloadFilter: false);
		}
	}

	private void OnHabitsChanged(object sender, EventArgs e)
	{
		if (base.IsVisible && LocalSettings.Settings.HabitInCal)
		{
			Reload(force: false, reloadArrange: false, reloadFilter: false);
		}
	}

	private void OnCheckInChanged(object sender, HabitCheckInModel checkIn)
	{
		if (base.IsVisible)
		{
			Reload(force: false, reloadArrange: false, reloadFilter: false);
		}
	}

	private void ReloadOnSubtaskChanged(object sender, string e)
	{
		if (LocalSettings.Settings.ShowCheckListInCal)
		{
			Navigate()?.Reload();
			CalendarSide.DayPicker.LoadTaskIndicator();
		}
	}

	private void ReloadOnFilterChanged(object sender, EventArgs e)
	{
		if (sender != null && !sender.Equals(CalendarSide))
		{
			CalendarSide.ReloadFilter();
		}
		if (base.IsVisible)
		{
			Reload(force: false, reloadArrange: true, reloadFilter: false);
		}
	}

	private void Reload(object sender, TagModel e)
	{
		ProjectExtra projectExtra = ((ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (CalendarUtils.GetTaskColorMode() == CalTaskColorMode.Tag || (projectExtra.Tags != null && projectExtra.Tags.Any()))
		{
			Navigate().Reload();
		}
		else
		{
			CalendarSide.ReloadFilter();
		}
		ArrangePanel.TryResetTagFilterText();
	}

	private void OnFilterChanged(object sender, FilterChangeArgs e)
	{
		if (base.IsVisible && e.RuleChanged && ((ViewSourceType == CalendarViewSourceType.WithList) ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData))?.FilterIds?.Contains(e.Filter?.id) == true)
		{
			Navigate()?.Reload();
		}
	}

	private void DelayReloadCalendar(object sender, EventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		DelayActionHandlerCenter.TryDoAction(_calUid + "Reload", delegate
		{
			base.Dispatcher.Invoke(delegate
			{
				Navigate()?.Reload();
			});
		}, 120);
	}

	private void Reload(object sender, CalendarEventModel e)
	{
		if (base.IsVisible)
		{
			Reload();
		}
	}

	private void Reload(object sender, string e)
	{
		if (base.IsVisible)
		{
			Reload(force: false, reloadArrange: true, reloadFilter: false);
		}
	}

	private void ReloadOnConfigChanged(object sender, bool e)
	{
		if (base.IsVisible)
		{
			Navigate()?.Reload();
			CalendarSide.DayPicker.LoadTaskIndicator();
			if (e)
			{
				ArrangePanel.TryReloadTasks();
			}
		}
	}

	private void InitArrangeEvents()
	{
		CalendarSide.DateSelected -= OnDateSelected;
		CalendarSide.DateSelected += OnDateSelected;
	}

	private void OnDateSelected(object sender, DateTime date)
	{
		HeadView.SelectedDate = date;
		GoTo(date);
		MultiDayView multiDay = MultiDay;
		if (multiDay != null && multiDay.IsVisible)
		{
			MultiDay.FlashNaviDate(date);
			return;
		}
		MultiWeekControl multiWeek = MultiWeek;
		if (multiWeek != null && multiWeek.IsVisible)
		{
			MultiWeek.FlashNaviDates(new List<DateTime> { date });
		}
	}

	private void InitHeadEvents()
	{
		HeadView.AddClick -= OnAddTaskClick;
		HeadView.AddClick += OnAddTaskClick;
		HeadView.SwitchView -= OnSwitchView;
		HeadView.SwitchView += OnSwitchView;
		HeadView.Next -= MoveNext;
		HeadView.Next += MoveNext;
		HeadView.Last -= MoveLast;
		HeadView.Last += MoveLast;
		HeadView.Today -= GotoToday;
		HeadView.Today += GotoToday;
		HeadView.DateSelect -= GotoDate;
		HeadView.DateSelect += GotoDate;
		HeadView.Action -= OnHeadAction;
		HeadView.Action += OnHeadAction;
	}

	private async void OnHeadAction(object sender, string e)
	{
		switch (e)
		{
		case "ArrangeTask":
			ShowOrHideArrange();
			break;
		case "DisplaySetting":
			ShowDisplaySetting();
			break;
		case "Subscribe":
			ShowSubscribe();
			break;
		case "Print":
			await Task.Delay(280);
			Print(isAll: false);
			break;
		case "PrintDetail":
			await Task.Delay(280);
			Print(isAll: true);
			break;
		case "ExitParallel":
			UserActCollectUtils.AddClickEvent("split_view", "task+calendar", "close_from_om");
			MainWindow.ShowCalendarInTaskChanged();
			break;
		}
	}

	public void OnKeyUp(object sender, KeyEventArgs e)
	{
		OnKeyUp(e.Key);
		e.Handled = true;
	}

	private void GotoDate(object sender, DateTime date)
	{
		INavigate navigate = Navigate();
		navigate.GoTo(date, navigate.Equals(MultiWeek));
	}

	private void GoTo(DateTime date)
	{
		Navigate().GoTo(date);
	}

	private void SetCalendarSideSelectRange()
	{
		if (!(HeadView.Mode == "10"))
		{
			CalendarSide.SetMonth(HeadView.GetCurrentMonthDate());
			if ((HeadView.EndDate - HeadView.StartDate).TotalDays < 28.0)
			{
				CalendarSide.SetSelectedRange(HeadView.StartDate, HeadView.EndDate);
			}
			else
			{
				CalendarSide.SetSelectedRange(null, null);
			}
		}
	}

	private void Reload(object sender, EventArgs e)
	{
		if (base.Visibility == Visibility.Visible)
		{
			Reload(force: false, reloadArrange: true, reloadFilter: true, delay: true);
		}
	}

	public void Reload(bool force = false, bool reloadArrange = true, bool reloadFilter = true, bool delay = false, bool resetDatePickerColor = false, bool setWeekend = false, bool byThemeChange = false)
	{
		if (_batchHandling)
		{
			return;
		}
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			Navigate().Reload(byThemeChange);
			if (reloadArrange)
			{
				ArrangePanel.TryReloadTasks();
			}
			if (reloadFilter)
			{
				CalendarSide.ReloadFilter();
			}
			if (resetDatePickerColor)
			{
				CalendarSide.DayPicker.ReloadColor();
			}
			CalendarSide.DayPicker.LoadTaskIndicator();
		});
	}

	public async void GotoCalDate()
	{
		GotoDate(_calendarDate, scrollToNow: true);
	}

	public async void GotoListCalDate()
	{
		GotoDate(_calendarInListDate, scrollToNow: true);
	}

	public async void GotoDate(DateTime? date, bool scrollToNow = false)
	{
		Navigate().GoTo(date ?? HeadView.SelectedDate ?? DateTime.Today, !date.HasValue, scrollToNow, delay: true);
		SetCalendarSideSelectRange();
		await Task.Delay(100);
		CalendarSide.DayPicker.LoadTaskIndicator();
		CalendarSide.DayPicker.ReloadColor();
		ArrangePanel.TryReloadTasks();
		MultiDay?.RefreshTimeLine(countChanged: false);
	}

	public async void GotoToday()
	{
		Navigate().Today();
		if (HeadView.Mode == "10")
		{
			CalendarSide.SetMonth(DateTime.Today);
			CalendarSide.SetSelectedRange(null, null);
			HeadView.SelectedDate = DateTime.Today;
		}
	}

	private void GotoToday(object sender, EventArgs eventArgs)
	{
		GotoToday();
	}

	private async void MoveLast(object sender, EventArgs eventArgs)
	{
		if (UserDao.IsPro())
		{
			Navigate().Last();
		}
	}

	private async void MoveNext(object sender, EventArgs eventArgs)
	{
		if (UserDao.IsPro())
		{
			Navigate().Next();
		}
	}

	private INavigate Navigate()
	{
		if (!UserDao.IsPro() && !ABTestManager.UsePremiumV2CalendarProStyle())
		{
			if (MultiWeek == null)
			{
				InitMonthView(5);
			}
			SwitchMultiWeeks(toMonth: true);
			HeadView.SetMode("0");
			HeadView.DateBorder.IsEnabled = true;
			return MultiWeek;
		}
		if (MonthGrid.Visibility == Visibility.Visible)
		{
			if (MultiWeek == null)
			{
				InitMonthView(5);
			}
			return MultiWeek;
		}
		if (YearCalendar.Visibility == Visibility.Visible)
		{
			return YearCalendar;
		}
		if (WeekGrid.Visibility == Visibility.Visible)
		{
			if (MultiDay == null)
			{
				InitWeekView(7);
			}
			return MultiDay;
		}
		if (AgendaContent.Visibility == Visibility.Visible)
		{
			if (AgendaView == null)
			{
				InitAgendaView();
			}
			return AgendaView;
		}
		if (MultiWeek == null)
		{
			InitMonthView(5);
		}
		SwitchMultiWeeks(toMonth: true);
		return MultiWeek;
	}

	private void InitWeekView(int days, bool showWeekend = true)
	{
		MultiDay = new MultiDayView(this)
		{
			Margin = new Thickness(0.0, 0.0, -1.0, 0.0)
		};
		MultiDay.SetDisplayDays(days, showWeekend);
		MultiDay.DateRangeChanged += OnDateRangeChanged;
		WeekGrid.Content = MultiDay;
	}

	private void OnDateRangeChanged(object sender, (DateTime, DateTime) dateRange)
	{
		var (dateTime, dateTime2) = dateRange;
		if (dateTime != HeadView.StartDate || dateTime2 != HeadView.EndDate)
		{
			HeadView.EndDate = dateRange.Item2;
			HeadView.StartDate = dateRange.Item1;
			if (ViewSourceType == CalendarViewSourceType.WithList)
			{
				_calendarInListDate = HeadView.StartDate;
			}
			else
			{
				_calendarDate = HeadView.StartDate;
			}
			SetCalendarSideSelectRange();
		}
	}

	private void InitMonthView(int weeks)
	{
		MultiWeek = new MultiWeekControl();
		MultiWeek.SetWeeks(weeks, isMultiWeek: false);
		MultiWeek.DateRangeChanged += OnDateRangeChanged;
		MonthGrid.Content = MultiWeek;
	}

	private void InitAgendaView()
	{
		AgendaView = new CalendarAgendaView(this);
		AgendaContent.Content = AgendaView;
		AgendaView.DateRangeChanged += OnDateRangeChanged;
	}

	private async void OnSwitchView(string from, string to, bool b)
	{
		if (from == to)
		{
			return;
		}
		HeadView.DateBorder.IsEnabled = to != "10";
		UpdateLayout();
		await Task.Delay(50);
		switch (to)
		{
		case "8":
			SwitchAgenda();
			UserActCollectUtils.AddClickEvent("calendar", "view_switch", "agenda");
			break;
		case "2":
		{
			CalendarControl calendarControl2 = this;
			bool fromMultiWeek = from == "0" || from.StartsWith("W");
			calendarControl2.SwitchMultiDay(isDay: true, isWeek: false, 1, null, fromMultiWeek);
			UserActCollectUtils.AddClickEvent("calendar", "view_switch", "day");
			break;
		}
		case "1":
			if (from == "2")
			{
				HeadView.SelectedDate = HeadView.StartDate;
			}
			SwitchMultiDay(isDay: false, isWeek: true, 7, (from == "2" || from == "8") ? new DateTime?(HeadView.StartDate) : (from.StartsWith("D") ? new DateTime?(HeadView.GetCurrentMonthDate(checkToday: true)) : ((DateTime?)null)), from == "0" || from.StartsWith("W"));
			UserActCollectUtils.AddClickEvent("calendar", "view_switch", "week");
			break;
		case "0":
		{
			switch (from)
			{
			default:
				if (!to.StartsWith("D"))
				{
					break;
				}
				goto case "2";
			case "2":
			case "1":
			case "10":
				HeadView.SelectedDate = HeadView.GetCurrentMonthDate();
				break;
			}
			CalendarControl calendarControl = this;
			object navDates;
			switch (from)
			{
			default:
				navDates = null;
				break;
			case "1":
			case "2":
			case "8":
				navDates = HeadView.GetRangeDates();
				break;
			}
			calendarControl.SwitchMultiWeeks(toMonth: true, 5, (List<DateTime>)navDates);
			UserActCollectUtils.AddClickEvent("calendar", "view_switch", "month");
			break;
		}
		case "10":
			if (from != "10")
			{
				HeadView.SelectedDate = HeadView.GetCurrentMonthDate();
			}
			SwitchYear();
			UserActCollectUtils.AddClickEvent("calendar", "view_switch", "year");
			break;
		}
		if (to.StartsWith("D"))
		{
			int result;
			int days = (int.TryParse(to.Substring(1), out result) ? result : 3);
			SwitchMultiDay(isDay: false, isWeek: false, days, (from == "2" || from == "8") ? new DateTime?((from == "1") ? HeadView.GetCurrentMonthDate(checkToday: true) : HeadView.StartDate) : ((DateTime?)null), from == "0" || from.StartsWith("W"));
			if (!from.StartsWith("D"))
			{
				UserActCollectUtils.AddClickEvent("calendar", "view_switch", "multi_day");
			}
		}
		if (to.StartsWith("W"))
		{
			int result2;
			int weeks = (int.TryParse(to.Substring(1), out result2) ? result2 : 3);
			CalendarControl calendarControl3 = this;
			object navDates2;
			switch (from)
			{
			default:
				navDates2 = null;
				break;
			case "1":
			case "2":
			case "8":
				navDates2 = HeadView.GetRangeDates();
				break;
			}
			calendarControl3.SwitchMultiWeeks(toMonth: false, weeks, (List<DateTime>)navDates2, from.StartsWith("W"));
			if (!from.StartsWith("W"))
			{
				UserActCollectUtils.AddClickEvent("calendar", "view_switch", "multi_week");
			}
		}
		PopupStateManager.Reset();
		UserActCollectUtils.AddClickEvent("calendar", "view_show", LocalSettings.Settings.CalendarDisplaySettings.MainCal.GetSwitchText());
	}

	public async void SwitchAgenda()
	{
		ExitBatchSelect(ignorePopup: true, ignoreCtrlPress: true);
		if (AgendaView == null)
		{
			InitAgendaView();
		}
		LocalSettings.Settings.CalendarDisplaySettings.SetHeadSwitch(ViewSourceType, CalendarViewType.Agenda);
		CalendarSide.DayPicker.ShowSelectedInCal = false;
		WeekGrid.Visibility = Visibility.Collapsed;
		MonthGrid.Visibility = Visibility.Collapsed;
		YearCalendar.Visibility = Visibility.Collapsed;
		MultiDay?.ClearItems(onlyData: true);
		MultiWeek?.ClearItems(onlyData: true);
		AgendaContent.Visibility = Visibility.Visible;
		UpdateLayout();
		await Task.Delay(10);
		AgendaView.LoadTask(HeadView.GetCurrentMonthDate());
		SetCalendarSideSelectRange();
	}

	public async void SwitchMultiWeeks(bool toMonth, int weeks = 5, List<DateTime> navDates = null, bool fromMultiWeeks = false, bool delay = true)
	{
		if (MultiWeek == null)
		{
			InitMonthView(weeks);
		}
		LocalSettings.Settings.CalendarDisplaySettings.SetHeadSwitch(ViewSourceType, (!toMonth) ? CalendarViewType.MultiWeek : CalendarViewType.Month, weeks);
		CalendarSide.DayPicker.ShowSelectedInCal = false;
		WeekGrid.Visibility = Visibility.Collapsed;
		AgendaContent.Visibility = Visibility.Collapsed;
		YearCalendar.Visibility = Visibility.Collapsed;
		MonthGrid.Visibility = Visibility.Visible;
		MultiWeek.SetWeeks(weeks, !toMonth);
		MultiWeek.SetMode(HeadView.Mode);
		UpdateLayout();
		await Task.Delay(10);
		MultiWeek.GoTo(fromMultiWeeks ? HeadView.StartDate : HeadView.GetCurrentMonthDate(checkToday: true), !fromMultiWeeks, isScrollToNow: false, delay);
		if (navDates != null)
		{
			MultiWeek.FlashNaviDates(navDates, delay: true);
		}
		SetCalendarSideSelectRange();
		MultiWeek.AddShowCheckIconUserEvent();
	}

	private async void SwitchMultiDay(bool isDay, bool isWeek, int days, DateTime? date = null, bool fromMultiWeek = false)
	{
		days = (isDay ? 1 : ((!isWeek) ? days : (LocalSettings.Settings.ShowCalWeekend ? 7 : 5)));
		if (MultiDay == null)
		{
			InitWeekView(days, !isWeek || LocalSettings.Settings.ShowCalWeekend);
		}
		CalendarSide.DayPicker.ShowSelectedInCal = days == 1;
		LocalSettings.Settings.CalendarDisplaySettings.SetHeadSwitch(ViewSourceType, isDay ? CalendarViewType.Day : (isWeek ? CalendarViewType.Week : CalendarViewType.MultiDay), days);
		MonthGrid.Visibility = Visibility.Collapsed;
		AgendaContent.Visibility = Visibility.Collapsed;
		YearCalendar.Visibility = Visibility.Collapsed;
		WeekGrid.Visibility = Visibility.Visible;
		MultiDay.SetMode(HeadView.Mode);
		MultiDay.GoTo(date ?? HeadView.GetCurrentMonthDate(), checkStart: true, isScrollToNow: false, fromMultiWeek);
		MultiDay.SetDisplayDays(days, !isWeek || LocalSettings.Settings.ShowCalWeekend);
		MultiDay.GoTo(date ?? HeadView.GetCurrentMonthDate(), checkStart: true, isScrollToNow: false, fromMultiWeek);
		UpdateLayout();
		await Task.Delay(10);
		if (date.HasValue)
		{
			MultiDay.FlashNaviDate(date.Value, delay: true);
		}
		MultiDay.TryShowTooltip();
		SetCalendarSideSelectRange();
		FocusManager.SetFocusedElement(this, MultiDay.TimelineView);
		Keyboard.Focus(MultiDay.TimelineView);
		MultiDay.AddShowCheckIconUserEvent();
	}

	public async Task SwitchYear()
	{
		ExitBatchSelect(ignorePopup: true, ignoreCtrlPress: true);
		LocalSettings.Settings.CalendarDisplaySettings.SetHeadSwitch(ViewSourceType, CalendarViewType.Year);
		CalendarSide.DayPicker.ShowSelectedInCal = false;
		YearCalendar.Visibility = Visibility.Visible;
		WeekGrid.Visibility = Visibility.Collapsed;
		MonthGrid.Visibility = Visibility.Collapsed;
		MultiDay?.ClearItems(onlyData: true);
		MultiWeek?.ClearItems(onlyData: true);
		AgendaContent.Visibility = Visibility.Collapsed;
		UpdateLayout();
		CalendarSide.SetSelectedRange(null, null);
		YearCalendar.GoTo(HeadView.GetCurrentMonthDate());
		YearCalendar.ResetNewSwitchViewFlag();
		if (LocalSettings.Settings.ShowHeatMap)
		{
			UserActCollectUtils.AddClickEvent("calendar", "element_show", "heatmap");
		}
	}

	private void LoadDefault()
	{
		CalendarViewType headSwitch = LocalSettings.Settings.CalendarDisplaySettings.GetHeadSwitch(ViewSourceType);
		HeadView.DateBorder.IsEnabled = headSwitch != CalendarViewType.Year;
		switch (headSwitch)
		{
		default:
			SwitchMultiWeeks(toMonth: true);
			HeadView.SetMode("0");
			break;
		case CalendarViewType.Week:
			SwitchMultiDay(isDay: false, isWeek: true, 0, null, fromMultiWeek: true);
			HeadView.SetMode("1");
			break;
		case CalendarViewType.Day:
			SwitchMultiDay(isDay: true, isWeek: false, 1, null, fromMultiWeek: true);
			HeadView.SetMode("2");
			break;
		case CalendarViewType.Agenda:
			SwitchAgenda();
			HeadView.SetMode("8");
			break;
		case CalendarViewType.MultiDay:
		{
			int multiNum2 = LocalSettings.Settings.CalendarDisplaySettings.GetMultiNum(ViewSourceType);
			SwitchMultiDay(isDay: false, isWeek: false, Math.Min(14, Math.Max(1, multiNum2)), null, fromMultiWeek: true);
			HeadView.SetMode("D" + multiNum2);
			break;
		}
		case CalendarViewType.MultiWeek:
		{
			int multiNum = LocalSettings.Settings.CalendarDisplaySettings.GetMultiNum(ViewSourceType);
			SwitchMultiWeeks(toMonth: false, Math.Min(6, Math.Max(2, multiNum)));
			HeadView.SetMode("W" + multiNum);
			break;
		}
		case CalendarViewType.Year:
			SwitchYear();
			HeadView.SetMode("10");
			break;
		}
	}

	private void OnControlLoaded(object sender, EventArgs e)
	{
		LoadDisplaySettings();
		NotifyAllDayHeightChanged();
		BindEvents();
		_miniCalScale = new ScaleTransform();
		CalendarSideBorder.RenderTransform = _miniCalScale;
		_arrangeScale = new ScaleTransform();
		ArrangePanel.RenderTransform = _arrangeScale;
		_loaded = true;
	}

	private void Resize(object sender, EventArgs e)
	{
		Utils.RunOnUiThread(base.Dispatcher, Resize);
	}

	private void Resize()
	{
		if (base.IsVisible && WeekGrid.IsVisible)
		{
			MultiDay?.Resize();
		}
	}

	private async void LoadDisplaySettings()
	{
		int times = 20;
		while (base.ActualWidth <= 0.0 && times-- > 0)
		{
			await Task.Delay(50);
			times--;
		}
		HeadView.SetMode(LocalSettings.Settings.CalendarDisplaySettings.GetHeadMode(ViewSourceType));
		bool flag = base.ActualWidth >= 544.0 && LocalSettings.Settings.CalendarDisplaySettings.ShowMiniCal(ViewSourceType);
		bool flag2 = base.ActualWidth >= 544.0 && LocalSettings.Settings.CalendarDisplaySettings.ShowArrange(ViewSourceType);
		HeadView.SetShowCalendarSide(flag);
		ShowOrHideMiniCalPanel(flag, save: false);
		ShowOrHideArrangePanel(flag2, save: false, checkWidth: true);
		double num = (flag ? LocalSettings.Settings.CalendarDisplaySettings.MiniCalWidth(ViewSourceType) : 0.0);
		double num2 = (flag2 ? LocalSettings.Settings.CalendarDisplaySettings.ArrangeWidth(ViewSourceType) : 0.0);
		if (base.ActualWidth - 300.0 - num - num2 < 0.0)
		{
			if (num2 > 0.0)
			{
				num2 = 244.0;
			}
			num = base.ActualWidth - 300.0 - num2;
			if (num2 > 0.0)
			{
				Column3.Width = new GridLength(num2);
			}
			if (num > 0.0)
			{
				Column1.Width = new GridLength(num);
			}
		}
	}

	public IDragBarEvent GetDragEvent()
	{
		if (MultiWeek != null && MonthGrid.Visibility == Visibility.Visible)
		{
			return MultiWeek;
		}
		if (MultiDay != null && WeekGrid.Visibility == Visibility.Visible)
		{
			return MultiDay;
		}
		return null;
	}

	public void NavigateDate(DateTime date, CalendarViewType mode)
	{
		if (HeadView.Mode == mode.ToString())
		{
			return;
		}
		HeadView.SelectedDate = date;
		WeekMonthSwitch weekMonthSwitch = HeadView.WeekMonthSwitch;
		int num = (int)mode;
		weekMonthSwitch.NavigateMode(num.ToString());
		switch (mode)
		{
		case CalendarViewType.Day:
			HeadView.SetMode("2");
			SwitchMultiDay(isDay: true, isWeek: false, 1, date);
			break;
		case CalendarViewType.Week:
			HeadView.SetMode("1");
			SwitchMultiDay(isDay: false, isWeek: true, 1, date);
			break;
		case CalendarViewType.Month:
		{
			List<DateTime> navDates = null;
			if (HeadView.Mode == "10")
			{
				navDates = new List<DateTime>
				{
					new DateTime(date.Year, date.Month, 1)
				};
			}
			HeadView.SetMode("0");
			SwitchMultiWeeks(toMonth: true, 5, navDates);
			break;
		}
		}
		HeadView.DateBorder.IsEnabled = mode != CalendarViewType.Year;
	}

	public void SetEditting(bool editting)
	{
		_editting = editting;
	}

	public bool IsEditting()
	{
		return _editting;
	}

	public void OnTouchScroll(int offset)
	{
		if (PopupStateManager.CanShowAddPopup() && Navigate() is MultiDayView multiDayView)
		{
			if (Math.Abs(offset) > 1)
			{
				offset /= 2;
			}
			multiDayView.OnTouchScroll(-1.0 * (double)offset);
		}
	}

	public void MoveLastOrNext(bool next)
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			HeadView.AddWeekMonthAction();
			if (next)
			{
				MoveNext(null, null);
			}
			else
			{
				MoveLast(null, null);
			}
		});
	}

	public void OnKeyUp(Key key)
	{
		if (!PopupStateManager.CanShowAddPopup() || !UserDao.IsPro() || !(((!_lastMouseMoveTime.HasValue) ? 1000.0 : (DateTime.Now - _lastMouseMoveTime.Value).TotalMilliseconds) > 100.0))
		{
			return;
		}
		_lastMouseMoveTime = DateTime.Now;
		switch (key)
		{
		case Key.Up:
			if ((MultiWeek != null && MonthGrid.Visibility == Visibility.Visible) || (AgendaView != null && AgendaContent.Visibility == Visibility.Visible))
			{
				MoveLastOrNext(next: false);
			}
			break;
		case Key.Down:
			if ((MultiWeek != null && MonthGrid.Visibility == Visibility.Visible) || (AgendaView != null && AgendaContent.Visibility == Visibility.Visible))
			{
				MoveLastOrNext(next: true);
			}
			break;
		case Key.Prior:
		case Key.Left:
			if (!Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
			{
				MoveLastOrNext(next: false);
			}
			break;
		case Key.Next:
		case Key.Right:
			if (!Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
			{
				MoveLastOrNext(next: true);
			}
			break;
		case Key.OemPlus:
		case Key.OemMinus:
			if (Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
			{
				double calendarHourHeight3 = LocalSettings.Settings.CalendarHourHeight;
				CalendarGeoHelper.ChangeFontSize(key == Key.OemPlus);
				MultiDay?.ScrollOnHourHeightChanged(calendarHourHeight3, LocalSettings.Settings.CalendarHourHeight);
			}
			break;
		case Key.Add:
		case Key.Subtract:
			if (Utils.IfCtrlPressed())
			{
				double calendarHourHeight = LocalSettings.Settings.CalendarHourHeight;
				CalendarGeoHelper.ChangeFontSize(key == Key.Add);
				MultiDay?.ScrollOnHourHeightChanged(calendarHourHeight, LocalSettings.Settings.CalendarHourHeight);
			}
			break;
		case Key.D0:
			if (Utils.IfCtrlPressed() && !Utils.IfShiftPressed())
			{
				double calendarHourHeight2 = LocalSettings.Settings.CalendarHourHeight;
				switch (LocalSettings.Settings.BaseFontSize)
				{
				default:
					CalendarGeoHelper.SetFontSize(2);
					break;
				case 16:
					CalendarGeoHelper.SetFontSize(3);
					break;
				case 18:
					CalendarGeoHelper.SetFontSize(5);
					break;
				}
				MultiDay?.ScrollOnHourHeightChanged(calendarHourHeight2, LocalSettings.Settings.CalendarHourHeight);
			}
			break;
		case Key.D1:
		case Key.D2:
		case Key.D3:
		case Key.D4:
		case Key.D5:
		case Key.A:
		case Key.D:
		case Key.M:
		case Key.W:
		case Key.Y:
			if (!Utils.IfModifierKeyDown() && !Utils.IfWinPressed())
			{
				HeadView.OnSwitchKeyUp(key);
			}
			break;
		case Key.T:
			if (!Utils.IfModifierKeyDown() && !Utils.IfWinPressed())
			{
				UserActCollectUtils.AddShortCutEvent("switch_views", "back_to_today");
				GotoToday();
			}
			break;
		}
	}

	public void NotifyAllDayHeightChanged()
	{
		if (MultiDay != null)
		{
			MultiDay.SetFirstRowHeight();
		}
	}

	public bool IsFilterAll()
	{
		return CalendarSide.IsAll;
	}

	public void Print(bool isAll)
	{
		if (MultiWeek != null && MonthGrid.Visibility == Visibility.Visible)
		{
			MultiWeek.PrintMonthCalendar(isAll);
		}
		else if (MultiDay != null && WeekGrid.Visibility == Visibility.Visible)
		{
			MultiDay.PrintWeekCalendar(isAll);
		}
	}

	public async void GetFocus(bool force = false)
	{
		await Task.Delay(200);
		if (!force && !PopupStateManager.CanShowAddPopup())
		{
			return;
		}
		Window window = Window.GetWindow(this);
		if (window != null && window.IsActive)
		{
			if (MultiDay?.IsVisible ?? false)
			{
				FocusManager.SetFocusedElement(window, MultiDay.TimelineView);
				Keyboard.Focus(MultiDay.TimelineView);
			}
			else
			{
				FocusManager.SetFocusedElement(window, window);
				Keyboard.Focus(window);
			}
		}
	}

	private void OnShortCutChanged(object sender, string key)
	{
		if (!(key == "Print"))
		{
			if (key == "PrintDetail")
			{
				KeyBindingManager.SetKeyGesture("PrintDetail", base.InputBindings[1] as KeyBinding);
			}
		}
		else
		{
			KeyBindingManager.SetKeyGesture("Print", base.InputBindings[0] as KeyBinding);
		}
	}

	public void SetColorOpacity(float opacity)
	{
	}

	private void OnAddTaskClick(object sender, EventArgs e)
	{
		TaskDetailViewModel taskDetailViewModel = TaskDetailViewModel.BuildInitModel(null);
		taskDetailViewModel.SourceViewModel.StartDate = HeadView.GetCurrentMonthDate();
		string calendarDefaultAddProjectId = CalendarUtils.GetCalendarDefaultAddProjectId(ViewSourceType == CalendarViewSourceType.WithList);
		taskDetailViewModel.SourceViewModel.ProjectId = calendarDefaultAddProjectId;
		TaskDetailPopup taskDetailPopup = new TaskDetailPopup
		{
			IsCalendarContext = true
		};
		IToastShowWindow toastShowWindow = Utils.FindParent<IToastShowWindow>(this);
		if (toastShowWindow != null)
		{
			if (PopupStateManager.LastTarget == this)
			{
				return;
			}
			taskDetailPopup.DependentWindow = toastShowWindow;
		}
		taskDetailPopup.Disappear -= OnDetailClosed;
		taskDetailPopup.Disappear += OnDetailClosed;
		taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
		taskDetailPopup.CalendarAddFlowClosed += OnCalendarAddFlowClosed;
		taskDetailPopup.TaskSaved -= OnTaskAdded;
		taskDetailPopup.TaskSaved += OnTaskAdded;
		UIElement target = sender as UIElement;
		taskDetailPopup.Show(taskDetailViewModel, string.Empty, new TaskWindowDisplayArgs(target, 40.0, focus: false, 0)
		{
			TitleParseMode = TitleDateParseMode.Full
		}, default(Point), show: true, canParse: false, null, calAdd: true);
		UserActCollectUtils.AddClickEvent("calendar", "action", "add_btn");
		SetEditting(editting: true);
	}

	private async void OnTaskAdded(object sender, string taskId)
	{
		if (sender is TaskDetailWindow taskDetailWindow)
		{
			taskDetailWindow.TaskSaved -= OnTaskAdded;
		}
		string text = CalendarUtils.CheckTaskCanShown(taskId, ViewSourceType == CalendarViewSourceType.WithList);
		if (!string.IsNullOrEmpty(text))
		{
			Utils.Toast(text);
		}
		else if (AgendaView != null && AgendaContent.Visibility == Visibility.Visible)
		{
			AgendaView.CheckTaskDisplay(taskId, isAdd: true);
		}
	}

	private string GetWidgetThemeId()
	{
		CalendarWidget calendarWidget = Utils.FindParent<CalendarWidget>(this);
		if (calendarWidget != null)
		{
			return calendarWidget.ThemeId;
		}
		return string.Empty;
	}

	private void OnDetailClosed(object sender, string e)
	{
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			if (taskDetailPopup.DelayCalendarAddCleanup)
			{
				return;
			}
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		RemoveUnsavedMonthTask(e);
		SetEditting(editting: false);
	}

	private void OnCalendarAddFlowClosed(object sender, CalendarEventModel e)
	{
		string taskId = (sender as TaskDetailPopup)?.CalendarAddFlowTaskId;
		if (sender is TaskDetailPopup taskDetailPopup)
		{
			taskDetailPopup.CalendarAddFlowClosed -= OnCalendarAddFlowClosed;
			taskDetailPopup.Disappear -= OnDetailClosed;
			taskDetailPopup.TaskSaved -= OnTaskAdded;
		}
		RemoveUnsavedMonthTask(taskId, forceRemove: true);
		SetEditting(editting: false);
		string text = CalendarUtils.CheckCalendarEventCanShown(e, ViewSourceType == CalendarViewSourceType.WithList);
		if (!string.IsNullOrEmpty(text))
		{
			Utils.Toast(text);
		}
	}

	private void RemoveUnsavedMonthTask(string taskId, bool forceRemove = false)
	{
		if (!string.IsNullOrEmpty(taskId) && (forceRemove || !TaskCache.ExistTask(taskId)))
		{
			MultiWeek?.RemoveTaskById(taskId);
		}
	}

	private void ShowSubscribe()
	{
		UserActCollectUtils.AddClickEvent("calendar", "toolbar", "subscribe_calendar");
		SettingsHelper.PullRemoteSettings();
		SettingDialog.ShowSettingDialog(SettingsType.Calendar, Window.GetWindow(this));
	}

	public void ShowDisplaySetting()
	{
		Window parentWindow = Utils.GetParentWindow(this);
		CalendarDisplaySettingWindow obj = new CalendarDisplaySettingWindow
		{
			Owner = parentWindow
		};
		UserActCollectUtils.AddClickEvent("calendar", "toolbar", "view_options");
		obj.ShowDialog();
	}

	public void ShowOrHideArrange()
	{
		if (ArrangePanel.Visibility != Visibility.Visible)
		{
			ShowOrHideArrangePanel(show: true, save: true, checkWidth: true, anim: true);
			UserActCollectUtils.AddClickEvent("calendar", "toolbar", "arrangement");
		}
		else
		{
			ShowOrHideArrangePanel(show: false, save: true, checkWidth: false, anim: true);
		}
	}

	public void ShowOrHideCalSide()
	{
		if (CalendarSideBorder.Visibility != Visibility.Visible)
		{
			UserActCollectUtils.AddClickEvent("calendar", "toolbar", "filter");
			double num = LocalSettings.Settings.CalendarDisplaySettings.MiniCalWidth(ViewSourceType);
			double num2 = base.ActualWidth - Column3.ActualWidth - 300.0;
			if (num2 < 244.0)
			{
				ShowMiniCalPanelInPopup();
				return;
			}
			if (num2 < num && num2 >= 244.0)
			{
				LocalSettings.Settings.CalendarDisplaySettings.SetMiniCalWidth(ViewSourceType, num2);
			}
			ShowOrHideMiniCalPanel(show: true, save: true, anim: true);
		}
		else
		{
			ShowOrHideMiniCalPanel(show: false, save: true, anim: true);
		}
	}

	private void ShowMiniCalPanelInPopup()
	{
		CalendarSideBorder.Child = null;
		EscPopup obj = new EscPopup
		{
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade,
			Placement = PlacementMode.Left,
			HorizontalOffset = 0.0,
			PlacementTarget = HeadView.ShowFilterButton,
			VerticalOffset = -5.0
		};
		ContentControl contentControl = new ContentControl
		{
			Width = 254.0,
			MaxHeight = 640.0
		};
		CalendarSide.DayPicker.Margin = new Thickness(10.0, 0.0, 10.0, 12.0);
		contentControl.SetResourceReference(FrameworkElement.StyleProperty, "PopupContentStyle");
		contentControl.Content = CalendarSide;
		obj.Child = contentControl;
		obj.IsOpen = true;
	}

	internal void CheckPro(bool setBlur = true)
	{
		bool flag = UserDao.IsPro();
		if (setBlur)
		{
			CalendarBlurEffect.Radius = ((!flag) ? ((!ABTestManager.UsePremiumV2CalendarProStyle()) ? 8 : 0) : 0);
		}
		else
		{
			CalendarBlurEffect.Radius = 0.0;
		}
		if (ProToastBorder != null)
		{
			ProToastBorder.Visibility = Visibility.Collapsed;
		}
		YearCalendar.IsEnabled = flag;
		MonthGrid.IsEnabled = flag;
		WeekGrid.IsEnabled = flag;
		AgendaContent.IsEnabled = flag;
		HeadView.SetEnable(flag, ViewSourceType == CalendarViewSourceType.Widget);
		if (!flag && setBlur)
		{
			ShowOrHideMiniCalPanel(show: false);
			CalendarBody.Opacity = 0.4;
		}
		else
		{
			CalendarBody.Opacity = 1.0;
		}
	}

	public void ClearData()
	{
		MultiWeek?.ClearItems();
		MultiDay?.ClearItems();
		ArrangePanel.ClearItems();
	}

	public void RemoveItem(CalendarDisplayViewModel model)
	{
		Navigate()?.RemoveItem(model);
		ArrangePanel.RemoveItem(model);
	}

	public void SetVirtual(bool isVirtual)
	{
		IsVirtual = isVirtual;
	}

	public void ShowProToast()
	{
		if (UserDao.IsPro())
		{
			if (!MonthGrid.IsEnabled)
			{
				CheckPro();
				SetVirtual(isVirtual: false);
				Navigate().Today();
			}
			return;
		}
		if (MonthGrid.Visibility != Visibility.Visible)
		{
			SwitchMultiWeeks(toMonth: true);
		}
		SetVirtual(isVirtual: true);
		ShowOrHideArrangePanel(show: false);
		HeadView.SetShowCalendarSide(isShow: false);
		if (ProToastBorder == null)
		{
			ProToastBorder = new CalProToastBorder();
			ProToastBorder.SetValue(Grid.RowProperty, 1);
			CalendarBody.Children.Add(ProToastBorder);
		}
		if (!ABTestManager.UsePremiumV2CalendarProStyle())
		{
			MultiWeek.Today();
			MultiWeek.LoadVirtualModels();
			if (ProToastBorder.Visibility != Visibility.Visible)
			{
				ProToastBorder.Opacity = 1.0;
				UserActCollectUtils.SendUpgradePromptData("calendar_view_preview");
			}
		}
		else
		{
			ProToastBorder.Opacity = 0.0;
			if (ViewSourceType != CalendarViewSourceType.Widget)
			{
				Navigate()?.LoadVirtualModels();
			}
		}
		CheckPro(setBlur: false);
		ProToastBorder.Visibility = Visibility.Visible;
	}

	public void ProClick()
	{
		Utils.StartUpgrade((ViewSourceType == CalendarViewSourceType.Widget) ? "calendar_widget" : "calendar_view_preview");
	}

	public void CollectUserEvent()
	{
		if (CalendarSideBorder.IsVisible)
		{
			UserActCollectUtils.AddClickEvent("calendar", "element_show", "filter");
		}
		if (ArrangePanel.IsVisible)
		{
			UserActCollectUtils.AddClickEvent("calendar", "element_show", "arrangement");
		}
		else if (!CalendarSideBorder.IsVisible)
		{
			UserActCollectUtils.AddClickEvent("calendar", "element_show", "view_show");
		}
		UserActCollectUtils.AddClickEvent("calendar", "font_size_show", ((int)CalendarGeoHelper.CalTitleFontSize).ToString());
		if (LocalSettings.Settings.EnableLunar)
		{
			UserActCollectUtils.AddClickEvent("calendar", "element_show", "lunar_calendar");
		}
	}

	public void OnDoubleFingerTouch()
	{
		if (MultiDay != null && MultiDay.IsMouseOver)
		{
			MultiDay.OnDoubleFingerTouch();
		}
		if (MultiWeek != null && MultiWeek.IsMouseOver)
		{
			MultiWeek.OnDoubleFingerTouch();
		}
		if (YearCalendar != null && YearCalendar.IsMouseOver)
		{
			YearCalendar.OnDoubleFingerTouch();
		}
	}

	public void SetDateText(DateTime? date)
	{
		HeadView.SetDateText(date);
	}

	private void OnScrollChanged(object sender, MouseWheelEventArgs e)
	{
		if (!PopupStateManager.CanShowAddPopup() || (!CalendarSide.DayPicker.IsMouseOver && !HeadView.IsMouseOver) || (_lastScrollTime.HasValue && (DateTime.Now - _lastScrollTime.Value).TotalMilliseconds < 500.0))
		{
			return;
		}
		_lastScrollTime = DateTime.Now;
		int delta = e.Delta;
		if (delta > 0)
		{
			if (HeadView.Mode == "10")
			{
				CalendarSide.DayPicker.NextMonthOrYear(-1);
				DateTime? piovtDate = CalendarSide.DayPicker.PiovtDate;
				if (piovtDate.HasValue && piovtDate.GetValueOrDefault().Month == 12)
				{
					Navigate().GoTo(new DateTime(CalendarSide.DayPicker.PiovtDate.Value.Year, 12, 1));
				}
			}
			else
			{
				MoveLast(null, null);
			}
		}
		else
		{
			if (delta >= 0)
			{
				return;
			}
			if (HeadView.Mode == "10")
			{
				CalendarSide.DayPicker.NextMonthOrYear(1);
				DateTime? piovtDate = CalendarSide.DayPicker.PiovtDate;
				if (piovtDate.HasValue && piovtDate.GetValueOrDefault().Month == 1)
				{
					Navigate().GoTo(new DateTime(CalendarSide.DayPicker.PiovtDate.Value.Year, 1, 1));
				}
			}
			else
			{
				MoveNext(null, null);
			}
		}
	}

	public async Task CheckItemFiltered(object data)
	{
		string taskId = null;
		string text = null;
		if (!(data is DisplayItemModel displayItemModel))
		{
			if (!(data is CheckItemViewModel checkItemViewModel))
			{
				if (data is CalendarDisplayViewModel { SourceViewModel: not null } calendarDisplayViewModel)
				{
					taskId = calendarDisplayViewModel.GetTaskId();
					text = calendarDisplayViewModel.ItemId;
				}
			}
			else if (UserDao.IsPro())
			{
				text = checkItemViewModel.Id;
				taskId = checkItemViewModel.TaskServerId;
			}
		}
		else
		{
			taskId = displayItemModel.GetTaskId();
			text = (displayItemModel.IsItem ? displayItemModel.Id : null);
		}
		string text2 = ((!string.IsNullOrEmpty(text)) ? (await CalendarUtils.CheckCheckItemCanShown(taskId, text, ViewSourceType == CalendarViewSourceType.WithList)) : CalendarUtils.CheckTaskCanShown(taskId, ViewSourceType == CalendarViewSourceType.WithList));
		string text3 = text2;
		if (!string.IsNullOrEmpty(text3))
		{
			Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, text3);
			DelayReloadCalendar(null, null);
		}
	}

	private void OnSplitter1DragCompleted(object sender, DragCompletedEventArgs e)
	{
		double num = Column1.ActualWidth;
		if (num < 180.0)
		{
			ShowOrHideMiniCalPanel(show: false);
		}
		else
		{
			if (num < 244.0)
			{
				num = 244.0;
				Column1.Width = new GridLength(num);
			}
			LocalSettings.Settings.CalendarDisplaySettings.SetMiniCalWidth(ViewSourceType, num);
		}
		CalendarSideBorder.Opacity = 1.0;
		_miniCalScale.ScaleX = 1.0;
		_miniCalScale.ScaleY = 1.0;
		_columnDragging = false;
		DelayActionHandlerCenter.TryDoAction(_calUid, Resize, 50);
	}

	private void OnSplitter2DragCompleted(object sender, DragCompletedEventArgs e)
	{
		double num = Column3.ActualWidth;
		if (num < 180.0)
		{
			ShowOrHideArrangePanel(show: false);
			LocalSettings.Settings.CalendarDisplaySettings.SetShowArrange(ViewSourceType, val: false);
		}
		else
		{
			if (num < 244.0)
			{
				num = 244.0;
				Column3.Width = new GridLength(num);
			}
			LocalSettings.Settings.CalendarDisplaySettings.SetArrangeWidth(ViewSourceType, num);
		}
		ArrangePanel.Opacity = 1.0;
		_arrangeScale.ScaleX = 1.0;
		_arrangeScale.ScaleY = 1.0;
		ArrangePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
		_columnDragging = false;
		DelayActionHandlerCenter.TryDoAction(_calUid, Resize, 50);
	}

	private void OnSplitter1DragStart(object sender, DragStartedEventArgs e)
	{
		Column1.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
		Column1.MaxWidth = Math.Max(244.0, Math.Min(base.ActualWidth - 300.0 - Column3.ActualWidth, 320.0));
		_columnDragging = true;
	}

	private void OnSplitter2DragStart(object sender, DragStartedEventArgs e)
	{
		Column3.MaxWidth = Math.Max(244.0, Math.Min(base.ActualWidth - 300.0 - Column1.ActualWidth, 320.0));
		_columnDragging = true;
	}

	private void OnSplitter1Dragging(object sender, DragDeltaEventArgs e)
	{
		double actualWidth = Column1.ActualWidth;
		CalendarSideBorder.Opacity = ((actualWidth < 180.0) ? (actualWidth / 300.0) : 1.0);
		_miniCalScale.ScaleX = ((actualWidth < 180.0) ? (1.0 - (1.0 - actualWidth / 180.0) / 18.0) : 1.0);
		_miniCalScale.ScaleY = ((actualWidth < 180.0) ? (1.0 - (1.0 - actualWidth / 180.0) / 18.0) : 1.0);
	}

	private void OnSplitter2Dragging(object sender, DragDeltaEventArgs e)
	{
		double actualWidth = Column3.ActualWidth;
		ArrangePanel.HorizontalAlignment = ((actualWidth > 244.0) ? HorizontalAlignment.Stretch : HorizontalAlignment.Right);
		ArrangePanel.Opacity = ((actualWidth < 180.0) ? (actualWidth / 300.0) : 1.0);
		_arrangeScale.ScaleX = ((actualWidth < 180.0) ? (1.0 - (1.0 - actualWidth / 180.0) / 18.0) : 1.0);
		_arrangeScale.ScaleY = ((actualWidth < 180.0) ? (1.0 - (1.0 - actualWidth / 180.0) / 18.0) : 1.0);
	}

	private void ShowOrHideArrangePanel(bool show, bool save = true, bool checkWidth = false, bool anim = false)
	{
		if (show && checkWidth)
		{
			double num = base.ActualWidth - Column1.ActualWidth - 300.0;
			double num2 = LocalSettings.Settings.CalendarDisplaySettings.ArrangeWidth(ViewSourceType);
			if (num < 244.0)
			{
				Column2.MinWidth = 56.0 + num;
				LocalSettings.Settings.CalendarDisplaySettings.SetArrangeWidth(ViewSourceType, 244.0);
			}
			if (num < num2 && num >= 244.0)
			{
				LocalSettings.Settings.CalendarDisplaySettings.SetArrangeWidth(ViewSourceType, num);
			}
		}
		if (!show)
		{
			Column2.MinWidth = 300.0;
		}
		WeekGrid.Margin = new Thickness(0.0, 0.0, (ParallelShow || ArrangePanel.IsVisible) ? 2 : 3, 0.0);
		if (anim)
		{
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation((!show) ? 320 : 0, show ? 320 : 0, 240);
			if (show)
			{
				Column3.MaxWidth = 0.0;
				ArrangePanel.Visibility = Visibility.Visible;
				Column3.Width = new GridLength(LocalSettings.Settings.CalendarDisplaySettings.ArrangeWidth(ViewSourceType));
			}
			else
			{
				doubleAnimation.Completed += delegate
				{
					Column3.Width = new GridLength(0.0);
					ArrangePanel.Visibility = Visibility.Collapsed;
				};
			}
			if (HeadView.Mode == "10")
			{
				YearCalendar?.OnSideVisibleAnimate(240);
			}
			Column3.BeginAnimation(ColumnDefinition.MaxWidthProperty, doubleAnimation);
		}
		else
		{
			Column3.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
			Column3.MaxWidth = 320.0;
			ArrangePanel.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
			Column3.Width = new GridLength(show ? LocalSettings.Settings.CalendarDisplaySettings.ArrangeWidth(ViewSourceType) : 0.0);
		}
		if (save)
		{
			LocalSettings.Settings.CalendarDisplaySettings.SetShowArrange(ViewSourceType, show);
		}
		ArrangePanel.SetInDisplay(show);
	}

	private void ShowOrHideMiniCalPanel(bool show, bool save = true, bool anim = false)
	{
		if (CalendarSideBorder.Child == null)
		{
			ContentControl contentControl = Utils.FindParent<ContentControl>(CalendarSide);
			if (contentControl != null)
			{
				contentControl.Content = null;
			}
			CalendarSide.DayPicker.Margin = new Thickness(10.0, 22.0, 10.0, 12.0);
			CalendarSideBorder.Child = CalendarSide;
		}
		if (anim)
		{
			DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation((!show) ? 320 : 0, show ? 320 : 0, 240);
			if (show)
			{
				Column1.MaxWidth = 0.0;
				CalendarSideBorder.Visibility = Visibility.Visible;
				Column1.Width = new GridLength(LocalSettings.Settings.CalendarDisplaySettings.MiniCalWidth(ViewSourceType));
			}
			else
			{
				doubleAnimation.Completed += delegate
				{
					Column1.Width = new GridLength(0.0);
					CalendarSideBorder.Visibility = Visibility.Collapsed;
				};
			}
			if (HeadView.Mode == "10")
			{
				YearCalendar?.OnSideVisibleAnimate(240);
			}
			Column1.BeginAnimation(ColumnDefinition.MaxWidthProperty, doubleAnimation);
		}
		else
		{
			Column1.BeginAnimation(ColumnDefinition.MaxWidthProperty, null);
			Column1.MaxWidth = 320.0;
			CalendarSideBorder.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible);
			Column1.Width = new GridLength(show ? LocalSettings.Settings.CalendarDisplaySettings.MiniCalWidth(ViewSourceType) : 0.0);
		}
		if (save)
		{
			LocalSettings.Settings.CalendarDisplaySettings.SetShowMiniCal(ViewSourceType, show);
		}
		HeadView.SetShowCalendarSide(CalendarSideBorder.IsVisible);
	}

	private void OnWeekWidthChanged(object sender, SizeChangedEventArgs e)
	{
		if (!_columnDragging && Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 1.0)
		{
			DelayActionHandlerCenter.TryDoAction(_calUid, Resize);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		double actualWidth = Column1.ActualWidth;
		double actualWidth2 = Column2.ActualWidth;
		double actualWidth3 = Column3.ActualWidth;
		if (e.NewSize.Width < e.PreviousSize.Width)
		{
			if (actualWidth2 <= Column2.MinWidth)
			{
				if (actualWidth3 > 244.0)
				{
					actualWidth3 = Math.Max(244.0, base.ActualWidth - actualWidth - actualWidth2);
					Column3.Width = new GridLength(actualWidth3);
				}
				else if (actualWidth > 244.0)
				{
					actualWidth = Math.Max(244.0, base.ActualWidth - actualWidth3 - actualWidth2);
					Column1.Width = new GridLength(actualWidth);
				}
				else if (actualWidth3 > 0.0)
				{
					ShowOrHideArrangePanel(show: false, save: false);
				}
				else if (actualWidth > 0.0)
				{
					ShowOrHideMiniCalPanel(show: false, save: false);
				}
			}
		}
		else
		{
			if ((actualWidth != 0.0 && actualWidth3 != 0.0) || !(actualWidth2 > 544.0))
			{
				return;
			}
			if (actualWidth == 0.0 && LocalSettings.Settings.CalendarDisplaySettings.ShowMiniCal(ViewSourceType))
			{
				if (_loaded)
				{
					LocalSettings.Settings.CalendarDisplaySettings.SetMiniCalWidth(ViewSourceType, 244.0);
				}
				ShowOrHideMiniCalPanel(show: true);
			}
			else if (actualWidth3 == 0.0 && LocalSettings.Settings.CalendarDisplaySettings.ShowArrange(ViewSourceType))
			{
				if (_loaded)
				{
					LocalSettings.Settings.CalendarDisplaySettings.SetArrangeWidth(ViewSourceType, 244.0);
				}
				ShowOrHideArrangePanel(show: true);
			}
		}
	}

	public async void SetParallelShow(bool parallel, bool inTask)
	{
		if ((!parallel && inTask) || (ParallelShow == parallel && ViewSourceType == CalendarViewSourceType.WithList == inTask))
		{
			if (!inTask)
			{
				await Task.Delay(200);
				MultiDayView multiDay = MultiDay;
				if (multiDay != null && multiDay.Visibility == Visibility.Visible)
				{
					MultiDay.AddShowCheckIconUserEvent();
				}
				MultiWeekControl multiWeek = MultiWeek;
				if (multiWeek != null && multiWeek.Visibility == Visibility.Visible)
				{
					MultiWeek.AddShowCheckIconUserEvent();
				}
			}
			return;
		}
		if (ParallelShow != parallel)
		{
			ParallelShow = parallel;
			CheckColumns();
		}
		if (inTask)
		{
			ViewSourceType = CalendarViewSourceType.WithList;
		}
		else
		{
			ViewSourceType = CalendarViewSourceType.Main;
		}
		HeadView.SetShowInParallel(parallel);
		if (parallel)
		{
			base.Background = Brushes.Transparent;
		}
		SetMiniCalFilter();
		WeekGrid.Margin = new Thickness(0.0, 0.0, (ParallelShow || ArrangePanel.IsVisible) ? 2 : 3, 0.0);
		MonthGrid.Margin = new Thickness(-1.0, 0.0, 0.0, ParallelShow ? (-2) : 0);
	}

	private async void CheckColumns()
	{
		await Task.Delay(500);
		double actualWidth = Column1.ActualWidth;
		double actualWidth2 = Column3.ActualWidth;
		if (actualWidth < 244.0)
		{
			if (actualWidth2 > 244.0)
			{
				ShowOrHideArrangePanel(show: false, save: false);
			}
			else
			{
				ShowOrHideMiniCalPanel(show: false, save: false);
			}
		}
	}

	public async void SetMiniCalFilter()
	{
		LoadDefault();
		base.SizeChanged -= OnParallelSizeChanged;
		base.SizeChanged += OnParallelSizeChanged;
		await Task.Delay(200);
		CalendarSide.SetItemData(ViewSourceType == CalendarViewSourceType.WithList);
	}

	private void OnParallelSizeChanged(object sender, SizeChangedEventArgs e)
	{
		base.SizeChanged -= OnParallelSizeChanged;
		LoadDisplaySettings();
	}

	public void AddItemClickEvent()
	{
		if (ParallelShow)
		{
			UserActCollectUtils.AddClickEvent("split_view", (ViewSourceType != CalendarViewSourceType.WithList) ? "calendar+task" : "task+calendar", "task_detail");
		}
	}

	public bool InOneDay()
	{
		if (MultiDay != null && MultiDay.IsVisible)
		{
			return MultiDay.DisplayDays == 1;
		}
		return false;
	}

	public void SwitchVirtualView(string view)
	{
		HeadView.SelectedDate = new DateTime(2023, 9, 6);
		if (view == null)
		{
			return;
		}
		switch (view.Length)
		{
		case 4:
			switch (view[0])
			{
			case 'Y':
				if (view == "Year")
				{
					HeadView.SetMode("10");
					SwitchYear();
				}
				break;
			case 'W':
				if (view == "Week")
				{
					HeadView.SetMode("1");
					SwitchMultiDay(isDay: false, isWeek: false, 7);
				}
				break;
			}
			break;
		case 5:
			if (view == "Month")
			{
				HeadView.SetMode("0");
				SwitchMultiWeeks(toMonth: true, 5, null, fromMultiWeeks: false, delay: false);
			}
			break;
		case 3:
			if (view == "Day")
			{
				HeadView.SetMode("2");
				SwitchMultiDay(isDay: true, isWeek: false, 1);
			}
			break;
		case 10:
			if (view == "MultiWeeks")
			{
				HeadView.SetMode("W2");
				SwitchMultiWeeks(toMonth: false, 2, null, fromMultiWeeks: false, delay: false);
			}
			break;
		case 9:
			if (view == "MultiDays")
			{
				HeadView.SetMode("D3");
				SwitchMultiDay(isDay: false, isWeek: false, 3);
			}
			break;
		case 6:
			if (view == "Agenda")
			{
				HeadView.SetMode("8");
				SwitchAgenda();
			}
			break;
		case 7:
		case 8:
			break;
		}
	}

	public void OnCurrentDateChanged()
	{
		if (_calendarDate.HasValue && _calendarDate.Value.Date == DateTime.Today.AddDays(-1.0))
		{
			_calendarDate = DateTime.Today;
		}
		if (_calendarInListDate.HasValue && _calendarInListDate.Value.Date == DateTime.Today.AddDays(-1.0))
		{
			_calendarInListDate = DateTime.Today;
		}
		UtilLog.Info("CalendarControl.OnCurrentDateChanged " + HeadView.SelectedDate.ToString() + " ," + TaskDetailPopup.Showing + " ," + CalendarDetailWindow.PopupShowing + " ," + PomoDetailWindow.PopupShowing + " ," + CourseDetailWindow.PopupShowing + " ," + HabitCheckInWindow.PopupShowing + " ," + TickDragHelper.Dragging);
		if ((HeadView.SelectedDate == DateTime.Today.AddDays(-1.0) || !HeadView.SelectedDate.HasValue) && !TaskDetailPopup.Showing && !CalendarDetailWindow.PopupShowing && !PomoDetailWindow.PopupShowing && !CourseDetailWindow.PopupShowing && !HabitCheckInWindow.PopupShowing && !TickDragHelper.Dragging)
		{
			GotoToday();
		}
	}

	private void OnShowIconChanged(object sender, EventArgs e)
	{
		SetShowIcon();
	}

	private void SetShowIcon()
	{
		bool flag = true;
		if (MultiDay != null && (MultiDay.IsVisible || (!flag && LastShowIconState)))
		{
			MultiDay.SetShowIcon();
		}
		if (MultiWeek != null && (MultiWeek.IsVisible || (!flag && LastShowIconState)))
		{
			MultiWeek.SetShowIcon();
		}
		LastShowIconState = flag;
	}

	public void SetBatchSelected(string taskId, bool batchSelected, int row = -1)
	{
		if (batchSelected)
		{
			BatchSelected.Add(taskId);
			BatchSelectedRow[taskId] = row;
		}
		else
		{
			BatchSelected.Remove(taskId);
			BatchSelectedRow.Remove(taskId);
		}
		OnBatchSelectedChanged();
	}

	public async void SelectCopiedTask(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return;
		}
		ClearCopiedTaskSelection();
		_copiedTaskSelectId = taskId;
		ExitBatchSelect(ignorePopup: true, ignoreCtrlPress: true);
		for (int i = 0; i < 8; i++)
		{
			await Task.Delay(150);
			if (_copiedTaskSelectId != taskId || TrySetCopiedTaskSelected(taskId))
			{
				return;
			}
		}
		if (_copiedTaskSelectId == taskId)
		{
			_copiedTaskSelectId = null;
		}
	}

	public bool ClearCopiedTaskSelection(string clickedTaskId = null)
	{
		if (string.IsNullOrEmpty(_copiedTaskSelectId))
		{
			return false;
		}
		if (_copiedTaskSelectId == clickedTaskId)
		{
			_copiedTaskSelectId = null;
			return true;
		}
		ClearCopiedTaskSelected(_copiedTaskSelectId);
		_copiedTaskSelectId = null;
		return true;
	}

	private bool TrySetCopiedTaskSelected(string taskId)
	{
		bool flag = false;
		foreach (TaskBar item in FindVisualChildren<TaskBar>(CalendarBody))
		{
			if (item.DataContext is CalendarDisplayViewModel model)
			{
				flag |= SetCalendarModelSelected(model, taskId);
			}
		}
		foreach (TaskCell item2 in FindVisualChildren<TaskCell>(CalendarBody))
		{
			if (item2.DataContext is CalendarDisplayViewModel model2)
			{
				flag |= SetCalendarModelSelected(model2, taskId);
			}
		}
		foreach (CalendarAgendaItem item3 in FindVisualChildren<CalendarAgendaItem>(CalendarBody))
		{
			if (item3.DataContext is CalendarAgendaItemViewModel model3)
			{
				flag |= SetAgendaModelSelected(model3, taskId);
			}
		}
		return flag;
	}

	private void ClearCopiedTaskSelected(string taskId)
	{
		foreach (TaskBar item in FindVisualChildren<TaskBar>(CalendarBody))
		{
			if (item.DataContext is CalendarDisplayViewModel calendarDisplayViewModel && calendarDisplayViewModel.GetTaskId() == taskId)
			{
				calendarDisplayViewModel.Selected = false;
			}
		}
		foreach (TaskCell item2 in FindVisualChildren<TaskCell>(CalendarBody))
		{
			if (item2.DataContext is CalendarDisplayViewModel calendarDisplayViewModel2 && calendarDisplayViewModel2.GetTaskId() == taskId)
			{
				calendarDisplayViewModel2.Selected = false;
			}
		}
		foreach (CalendarAgendaItem item3 in FindVisualChildren<CalendarAgendaItem>(CalendarBody))
		{
			if (item3.DataContext is CalendarAgendaItemViewModel calendarAgendaItemViewModel && calendarAgendaItemViewModel.Id == taskId)
			{
				calendarAgendaItemViewModel.Selected = false;
			}
		}
	}

	private static bool SetCalendarModelSelected(CalendarDisplayViewModel model, string taskId)
	{
		if (model == null)
		{
			return false;
		}
		bool flag = model.GetTaskId() == taskId;
		if (flag || model.Selected)
		{
			model.Selected = flag;
		}
		return flag;
	}

	private static bool SetAgendaModelSelected(CalendarAgendaItemViewModel model, string taskId)
	{
		if (model == null)
		{
			return false;
		}
		bool flag = model.Id == taskId;
		if (flag || model.Selected)
		{
			model.Selected = flag;
		}
		return flag;
	}

	private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : class
	{
		if (parent == null)
		{
			yield break;
		}
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is T val)
			{
				yield return val;
			}
			foreach (T item in FindVisualChildren<T>(child))
			{
				yield return item;
			}
		}
	}

	public void AddBatchSelected(Dictionary<string, int> selected)
	{
		foreach (string item in selected.Keys.ToList())
		{
			BatchSelected.Add(item);
			BatchSelectedRow[item] = selected[item];
		}
		OnBatchSelectedChanged();
	}

	public bool ExitBatchSelect(bool ignorePopup = false, bool ignoreCtrlPress = false)
	{
		if (_batchControl != null && _batchControl.IsVisible)
		{
			if (_batchControl.PopupShowing && !ignorePopup)
			{
				return true;
			}
			if (!ignoreCtrlPress && base.IsMouseOver && Utils.IfCtrlPressed())
			{
				return true;
			}
			MultiDay?.ClearBatchSelected();
			ClearBatchSelected();
			return true;
		}
		return false;
	}

	public void ClearBatchSelected()
	{
		BatchSelected.Clear();
		OnBatchSelectedChanged();
		LoadMoreWindow.ExitBatchSelected();
	}

	private void OnBatchSelectedChanged()
	{
		if (BatchSelected.Count > 0)
		{
			InitBatchControl();
			_batchControl.Visibility = Visibility.Visible;
			_batchControl.SetTaskIds(BatchSelected.ToList(), isNote: false, useInList: false);
			if (ParallelShow)
			{
				DataChangedNotifier.NotifyCalOrListBatchSelected(calBatchSelected: true);
			}
		}
		else if (_batchControl != null)
		{
			_batchControl.Visibility = Visibility.Collapsed;
		}
		MultiDayView multiDay = MultiDay;
		if (multiDay != null && multiDay.IsVisible)
		{
			MultiDay.SetItemBatchSelected(BatchSelected);
		}
		MultiWeekControl multiWeek = MultiWeek;
		if (multiWeek != null && multiWeek.IsVisible)
		{
			MultiWeek.SetItemBatchSelected(BatchSelected);
		}
	}

	private void InitBatchControl()
	{
		if (_batchControl == null)
		{
			_batchControl = new BatchTaskControl
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
			};
			_batchControl.SetValue(Grid.RowProperty, 1);
			_batchControl.BatchOperated += delegate
			{
				CheckBatchSelectAfterDataChanged();
			};
			CalendarBody.Children.Add(_batchControl);
		}
	}

	public void SetBatchDragging(bool dragging)
	{
		MultiDay?.SetBatchDragging(dragging);
		MultiWeek?.SetBatchDragging(dragging);
	}

	public async Task OnMultiWeekBatchDragDrop(int dragDays)
	{
		BlockingSet<string> batchSelected = BatchSelected;
		if (batchSelected == null || batchSelected.Count <= 0)
		{
			return;
		}
		_batchHandling = true;
		foreach (string item in BatchSelected.ToList())
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
			DateTime? dateTime = taskById.StartDate ?? taskById.CompletedTime;
			if (dateTime.HasValue && dragDays != 0)
			{
				await CalendarDisplayViewModel.Build(new CalendarDisplayModel(taskById)).NotifyTaskDrop(dateTime.Value.AddDays(dragDays), toAllDay: false, batchHandle: true);
			}
		}
		await Task.Delay(50);
		_batchHandling = false;
		Reload();
		_batchControl.SetTaskIds(BatchSelected.ToList(), isNote: false, useInList: false);
		CheckBatchSelectAfterDataChanged();
	}

	public async Task OnBatchDragCellDrop(TimeSpan batchDragDiff, int dragDays, bool toAllDay, bool toPoint)
	{
		BlockingSet<string> batchSelected = BatchSelected;
		if (batchSelected == null || batchSelected.Count <= 0)
		{
			return;
		}
		_batchHandling = true;
		foreach (string item in BatchSelected.ToList())
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
			if (taskById == null)
			{
				continue;
			}
			DateTime? start = taskById.StartDate ?? taskById.CompletedTime;
			if (!start.HasValue)
			{
				continue;
			}
			bool flag = !(taskById.IsAllDay ?? true) && (!taskById.DueDate.HasValue || (taskById.DueDate.Value - start.Value).TotalHours <= 24.0);
			if (flag && !toPoint && !toAllDay)
			{
				CalendarDisplayViewModel barItem = CalendarDisplayViewModel.Build(new CalendarDisplayModel(taskById));
				if (barItem?.SourceViewModel == null)
				{
					continue;
				}
				TimeDataModel timeDataModel = TaskService.GetDelayTimeData(taskById, (long)batchDragDiff.TotalMinutes, useCompleteTime: true);
				if (timeDataModel != null)
				{
					if (!string.IsNullOrEmpty(barItem.RepeatFlag))
					{
						List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(barItem.TaskId);
						TimeData reviseData = new TimeData
						{
							StartDate = timeDataModel.StartDate,
							DueDate = timeDataModel.DueDate,
							IsAllDay = false,
							Reminders = reminders,
							RepeatFrom = barItem.RepeatFrom,
							RepeatFlag = barItem.RepeatFlag,
							ExDates = ((barItem.SourceViewModel.ExDates == null) ? new List<string>() : ExDateSerilizer.ToArray(barItem.SourceViewModel.ExDates)?.ToList()),
							TimeZone = new TimeZoneViewModel(barItem.SourceViewModel.IsFloating, barItem.SourceViewModel.TimeZoneName)
						};
						await ModifyRepeatHandler.TryUpdateDueDate(barItem.TaskId, barItem.DisplayStartDate, barItem.DisplayDueDate, reviseData, 1, 0, batchHandle: true);
					}
					else
					{
						await barItem.SetDropTimeSpan(timeDataModel.StartDate, timeDataModel.DueDate, Window.GetWindow(this), timeDataModel.IsAllDay);
					}
				}
			}
			else if (((!toPoint && !flag) || (toPoint && flag)) && dragDays != 0)
			{
				CalendarDisplayViewModel calendarDisplayViewModel = CalendarDisplayViewModel.Build(new CalendarDisplayModel(taskById));
				if (calendarDisplayViewModel?.SourceViewModel != null)
				{
					await calendarDisplayViewModel.NotifyTaskDrop(start.Value.AddDays(dragDays), toAllDay: false, batchHandle: true);
				}
			}
			else if (!flag)
			{
				if (!toPoint)
				{
					continue;
				}
				CalendarDisplayViewModel barItem = CalendarDisplayViewModel.Build(new CalendarDisplayModel(taskById));
				if (barItem?.SourceViewModel != null)
				{
					if (!string.IsNullOrEmpty(barItem.RepeatFlag))
					{
						List<TaskReminderModel> reminders2 = await TaskReminderDao.GetRemindersByTaskId(barItem.TaskId);
						TimeData reviseData2 = new TimeData
						{
							StartDate = start.Value.Date.Add(batchDragDiff),
							DueDate = null,
							IsAllDay = false,
							Reminders = reminders2,
							RepeatFrom = barItem.RepeatFrom,
							RepeatFlag = barItem.RepeatFlag,
							ExDates = ((barItem.SourceViewModel.ExDates == null) ? new List<string>() : ExDateSerilizer.ToArray(barItem.SourceViewModel.ExDates)?.ToList()),
							TimeZone = new TimeZoneViewModel(barItem.SourceViewModel.IsFloating, barItem.SourceViewModel.TimeZoneName)
						};
						await ModifyRepeatHandler.TryUpdateDueDate(barItem.TaskId, barItem.DisplayStartDate, barItem.DisplayDueDate, reviseData2, 1, 0, batchHandle: true);
					}
					else
					{
						await barItem.SetDropTimeSpan(start.Value.Date.Add(batchDragDiff), null, Window.GetWindow(this), false);
					}
				}
			}
			else if (toAllDay)
			{
				await CalendarDisplayViewModel.Build(new CalendarDisplayModel(taskById)).NotifyTaskDrop(start.Value.Date.AddDays(dragDays), toAllDay: true, batchHandle: true);
			}
		}
		await Task.Delay(50);
		_batchHandling = false;
		Reload();
		_batchControl?.SetTaskIds(BatchSelected.ToList(), isNote: false, useInList: false);
		CheckBatchSelectAfterDataChanged();
		await Task.Delay(100);
		SetBatchDragging(dragging: false);
	}

	public void CancelBatchSelect()
	{
		ExitBatchSelect(ignorePopup: true);
	}

	public async void BatchSetPriority(int priority)
	{
		foreach (string item in BatchSelected.ToList())
		{
			await TaskService.SetPriority(item, priority);
		}
		CheckBatchSelectAfterDataChanged();
	}

	public async void OnBatchDelete()
	{
		List<string> taskIds = BatchSelected.ToList();
		List<string> repeatIds = new List<string>();
		foreach (string id in BatchSelected.ToList())
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(id);
			if (!string.IsNullOrEmpty(taskById.RepeatFlag) && taskById.StartDate.HasValue && taskById.Status == 0)
			{
				await ModifyRepeatHandler.TryDeleteRecurrence(id, taskById.StartDate, null, null, batchHandle: true);
				taskIds.Remove(id);
				repeatIds.Add(id);
			}
		}
		List<string> deleteTaskIds = ModifyRepeatHandler.GetBatchDeleteAllIds();
		repeatIds.RemoveAll((string i) => deleteTaskIds.Contains(i));
		taskIds.AddRange(deleteTaskIds);
		List<TaskDeleteRecurrenceUndoEntity> batchDeleteUndo = new List<TaskDeleteRecurrenceUndoEntity>();
		foreach (string item in repeatIds)
		{
			TaskBaseViewModel taskById2 = TaskCache.GetTaskById(item);
			await ModifyRepeatHandler.TryDeleteRecurrence(item, taskById2.StartDate, null, null, batchHandle: true, batchDeleteUndo);
		}
		ModifyRepeatHandler.ClearBatchDelete();
		if (taskIds.Count > 0)
		{
			await TaskService.BatchDeleteTaskByIds(taskIds, showUndo: true, 1, batchDeleteUndo);
		}
		else
		{
			Utils.GetToastWindow()?.ToastDeleteRecUndo(batchDeleteUndo);
		}
		ExitBatchSelect(ignorePopup: true, ignoreCtrlPress: true);
	}

	public async void OnBatchComplete()
	{
		await TaskService.BatchCompleteTasks(BatchSelected.ToList());
	}

	public async void OnBatchSetProject(SelectableItemViewModel e, string originProjectId, string originColumnId)
	{
		(string, string, string) projectAndColumnId = e.GetProjectAndColumnId();
		string item = projectAndColumnId.Item1;
		string columnId = projectAndColumnId.Item2;
		ProjectModel project = CacheManager.GetProjectById(item);
		if (project == null)
		{
			return;
		}
		List<TaskBaseViewModel> taskAndChildrenInBatch = TaskCache.GetTaskAndChildrenInBatch(BatchSelected.ToList());
		List<string> ids = taskAndChildrenInBatch.Select((TaskBaseViewModel t) => t.Id).ToList();
		if (originProjectId != item || (!string.IsNullOrEmpty(originColumnId) && !(originColumnId == columnId)))
		{
			await TaskDao.RemoveTaskParentIdInBatch((from node in TaskNodeUtils.GetTaskNodeTree(taskAndChildrenInBatch)
				where !node.HasParent
				select node.Value.Id).ToList());
			await TaskService.BatchMoveProject(ids, new MoveProjectArgs(project)
			{
				ColumnId = columnId
			});
		}
		if (!string.IsNullOrEmpty(columnId) && originColumnId != columnId)
		{
			await TaskService.BatchSetColumn(ids, columnId);
			DataChangedNotifier.NotifyColumnChanged(project.id);
		}
		CheckBatchSelectAfterDataChanged();
	}

	public async void OnBatchSetAssignee(AvatarInfo assignee)
	{
		if (!string.IsNullOrEmpty(assignee.UserId) && assignee.UserId != "-1")
		{
			List<string> list = TaskCache.GetTasksByIds(BatchSelected.ToList())?.Select((TaskBaseViewModel m) => m.ProjectId).Distinct().ToList();
			if (list != null && list.Count > 0)
			{
				foreach (string item in list)
				{
					List<ShareUserModel> projectUsers = AvatarHelper.GetProjectUsers(item);
					if (projectUsers == null || projectUsers.Count == 0 || projectUsers.All((ShareUserModel a) => (a.userId.ToString() ?? "") != assignee.UserId))
					{
						Utils.Toast(Utils.GetString("ChangeAssigneeError"));
						return;
					}
				}
			}
		}
		await TaskService.BatchSetAssignee(BatchSelected.ToList(), assignee.UserId);
		CheckBatchSelectAfterDataChanged();
	}

	public async void OnBatchSetTags(TagSelectData tagData)
	{
		await TaskService.BatchSetTags(BatchSelected.ToList(), tagData);
		CheckBatchSelectAfterDataChanged();
	}

	public async void OnBatchSkip()
	{
		await TaskService.BatchSkipRecurrence(BatchSelected.ToList());
		CheckBatchSelectAfterDataChanged();
	}

	public async void OnBatchSwitchTaskNote()
	{
		List<string> list = BatchSelected.ToList();
		UtilLog.Info("BatchSwitchTaskNote " + list.Join(";"));
		if (await TaskService.BatchSwitchTaskOrNote(list, toast: false))
		{
			Utils.FindParent<IToastShowWindow>(this)?.TryToastString(null, Utils.GetString("Converted"));
			SyncManager.TryDelaySync();
			CheckBatchSelectAfterDataChanged();
		}
	}

	public async void BatchClearDate()
	{
		await TaskService.BatchClearDate(BatchSelected.ToList());
		ExitBatchSelect(ignorePopup: true);
	}

	public async void BatchSetTimeData(TimeData data)
	{
		_batchHandling = true;
		try
		{
			foreach (string item in BatchSelected.ToList())
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(item);
				if (string.IsNullOrEmpty(taskById.RepeatFlag) || !taskById.StartDate.HasValue || taskById.Status != 0)
				{
					await TaskService.SetDate(item, data);
				}
				else
				{
					await ModifyRepeatHandler.TryUpdateDueDate(taskById.Id, taskById.StartDate, taskById.DueDate, data, 1, 1, batchHandle: true);
				}
			}
		}
		finally
		{
			await Task.Delay(50);
			_batchHandling = false;
			Reload();
			_batchControl.SetTaskIds(BatchSelected.ToList(), isNote: false, useInList: false);
			CheckBatchSelectAfterDataChanged();
		}
	}

	private void CheckBatchSelectAfterDataChanged()
	{
		string text = null;
		foreach (string item in BatchSelected.ToList())
		{
			text = CalendarUtils.CheckTaskCanShown(item, ViewSourceType == CalendarViewSourceType.WithList);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
		}
		ExitBatchSelect(ignorePopup: true);
		Utils.Toast(text);
	}

	public async void BatchSetQuickDate(DateTime date)
	{
		_batchHandling = true;
		try
		{
			foreach (string item in BatchSelected.ToList())
			{
				TaskBaseViewModel task = TaskCache.GetTaskById(item);
				if (string.IsNullOrEmpty(task?.RepeatFlag) || task.Status != 0)
				{
					await TaskService.SetStartDate(item, date);
				}
				else if (task.StartDate.HasValue)
				{
					List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.Id);
					TimeData timeData = new TimeData
					{
						StartDate = task.StartDate,
						DueDate = task.DueDate,
						IsAllDay = (!task.StartDate.HasValue || (task.IsAllDay ?? true)),
						Reminders = reminders,
						RepeatFrom = task.RepeatFrom,
						RepeatFlag = task.RepeatFlag,
						ExDates = ((task.ExDates == null) ? new List<string>() : ExDateSerilizer.ToArray(task.ExDates)?.ToList())
					};
					await ModifyRepeatHandler.TryUpdateDueDateOnlyDate(task.Id, task.StartDate, task.DueDate, timeData, date, 1, 1, batchHandle: true);
				}
			}
		}
		finally
		{
			await Task.Delay(50);
			_batchHandling = false;
			Reload();
			_batchControl.SetTaskIds(BatchSelected.ToList(), isNote: false, useInList: false);
			CheckBatchSelectAfterDataChanged();
		}
		SyncManager.TryDelaySync();
	}

	public async void OnBatchDelay(long l)
	{
		_batchHandling = true;
		try
		{
			foreach (string id in BatchSelected.ToList())
			{
				TaskBaseViewModel task = TaskCache.GetTaskById(id);
				if (task == null)
				{
					continue;
				}
				if (!string.IsNullOrEmpty(task.RepeatFlag) && task.StartDate.HasValue && task.Status == 0)
				{
					TimeDataModel dataModel = TaskService.GetDelayTimeData(task, l);
					if (dataModel != null)
					{
						TimeData reviseData = new TimeData(await TaskDao.GetTaskById(id), await TaskReminderDao.GetRemindersByTaskId(id))
						{
							StartDate = dataModel.StartDate,
							DueDate = dataModel.DueDate,
							IsAllDay = dataModel.IsAllDay
						};
						await ModifyRepeatHandler.TryUpdateDueDate(task.Id, task.StartDate, task.DueDate, reviseData, 1, 1, batchHandle: true);
					}
				}
				else
				{
					TaskService.DelayOrAdvancedTasks(new List<string> { id }, l);
				}
			}
		}
		finally
		{
			await Task.Delay(50);
			_batchHandling = false;
			Reload();
			_batchControl.SetTaskIds(BatchSelected.ToList(), isNote: false, useInList: false);
			CheckBatchSelectAfterDataChanged();
		}
	}

	public void BatchOpenSticky()
	{
		TaskStickyWindow.ShowTaskSticky(BatchSelected.ToList());
	}

	public async void CopyBatchTasksText(List<string> taskIds)
	{
		List<TaskModel> list = await TaskDao.GetThinTasksInBatch(taskIds);
		if (list == null || list.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (TaskModel item in list)
		{
			stringBuilder.Append(string.IsNullOrEmpty(item.title) ? Utils.GetString("NoTitle") : item.title);
			if (!Utils.IsEmptyDate(item.startDate))
			{
				bool flag = item.isAllDay ?? true;
				stringBuilder.Append("(" + ticktick_WPF.Util.DateUtils.FormatDateStringNoWord(item.startDate.Value, item.dueDate?.AddDays(flag ? (-1) : 0), flag) + ")");
			}
			stringBuilder.Append(Environment.NewLine);
		}
		stringBuilder.Remove(stringBuilder.Length - Environment.NewLine.Length, Environment.NewLine.Length);
		try
		{
			Clipboard.SetDataObject(stringBuilder.ToString(), copy: true);
			Utils.Toast(Utils.GetString("CopyTextTips"));
		}
		catch (Exception)
		{
		}
	}

	public void InitUI()
	{
		base.MouseWheel += OnScrollChanged;
		base.SizeChanged += OnSizeChanged;
		base.MinWidth = 300.0;
		base.Background = Brushes.Transparent;
		InitShortCut();
		AddContainer();
		AddCalendarBody();
		AddCalendarSideBorder();
		AddArrangePanel();
		InitCalendarSideBar();
		InitGridSplitter();
	}

	public void AddContainer()
	{
		Container = new Grid();
		Column1 = new ColumnDefinition
		{
			Width = new GridLength(0.0),
			MaxWidth = 320.0
		};
		Column2 = new ColumnDefinition
		{
			MinWidth = 300.0
		};
		Column3 = new ColumnDefinition
		{
			Width = new GridLength(0.0),
			MaxWidth = 320.0
		};
		Container.ColumnDefinitions.Add(Column1);
		Container.ColumnDefinitions.Add(Column2);
		Container.ColumnDefinitions.Add(Column3);
		base.Content = Container;
		CalendarBlurEffect = new BlurEffect
		{
			Radius = 0.0
		};
	}

	public void AddCalendarBody()
	{
		CalendarBody = new Grid();
		CalendarBody.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		CalendarBody.RowDefinitions.Add(new RowDefinition());
		Container.Children.Add(CalendarBody);
		CalendarBody.SetValue(Grid.ColumnProperty, 1);
		HeadView = new CalendarHead
		{
			VerticalAlignment = VerticalAlignment.Top
		};
		CalendarBody.Children.Add(HeadView);
		HeadView.PreviewMouseLeftButtonDown += OnHeadMouseDown;
		MonthGrid = new ContentControl
		{
			Visibility = Visibility.Hidden,
			Margin = new Thickness(-1.0, 0.0, 0.0, 0.0)
		};
		WeekGrid = new ContentControl
		{
			Visibility = Visibility.Hidden,
			Margin = new Thickness(0.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Stretch
		};
		AgendaContent = new ContentControl
		{
			Visibility = Visibility.Hidden,
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
		};
		YearCalendar = new YearCalendarControl();
		YearCalendar.ViewModel.DateRangeChanged += OnDateRangeChanged;
		WeekGrid.SizeChanged += OnWeekWidthChanged;
		Line line = new Line
		{
			X1 = 0.0,
			X2 = 1.0,
			Stretch = Stretch.Fill,
			IsHitTestVisible = false,
			VerticalAlignment = VerticalAlignment.Top,
			StrokeThickness = 1.0,
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
		};
		line.SetValue(Grid.RowProperty, 1);
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		line.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = AgendaContent
		});
		MonthGrid.SetValue(Grid.RowProperty, 1);
		WeekGrid.SetValue(Grid.RowProperty, 1);
		AgendaContent.SetValue(Grid.RowProperty, 1);
		YearCalendar.SetValue(Grid.RowProperty, 1);
		line.SetValue(Grid.RowProperty, 1);
		CalendarBody.Children.Add(MonthGrid);
		CalendarBody.Children.Add(WeekGrid);
		CalendarBody.Children.Add(AgendaContent);
		CalendarBody.Children.Add(YearCalendar);
		CalendarBody.Children.Add(line);
	}

	private void OnHeadMouseDown(object sender, MouseButtonEventArgs e)
	{
		ExitBatchSelect();
	}

	private void InitShortCut()
	{
		base.InputBindings.Add(new KeyBinding(CalendarCommands.PrintCommand, new KeyGesture(Key.P, ModifierKeys.Control))
		{
			CommandParameter = this
		});
		base.InputBindings.Add(new KeyBinding(CalendarCommands.PrintAllCommand, new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift))
		{
			CommandParameter = this
		});
	}

	private void AddCalendarSideBorder()
	{
		CalendarSideBorder = new Border
		{
			MinWidth = 244.0,
			Visibility = Visibility.Collapsed,
			RenderTransformOrigin = new Point(0.0, 0.5),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		CalendarSideBorder.SetBinding(UIElement.IsEnabledProperty, new Binding("IsEnabled")
		{
			Source = CalendarBody
		});
		Line line = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			Stretch = Stretch.Fill,
			IsHitTestVisible = false,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalAlignment = HorizontalAlignment.Left,
			StrokeThickness = 1.0
		};
		line.SetValue(Grid.ColumnProperty, 1);
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		line.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = CalendarSideBorder
		});
		Container.Children.Add(CalendarSideBorder);
		Container.Children.Add(line);
	}

	private void AddArrangePanel()
	{
		ArrangePanel = new ArrangeTaskPanel
		{
			MinWidth = 244.0,
			RenderTransformOrigin = new Point(1.0, 0.5),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		ArrangePanel.SetBinding(FrameworkElement.HeightProperty, new Binding("ActualHeight")
		{
			Source = this
		});
		ArrangePanel.SetBinding(UIElement.IsEnabledProperty, new Binding("IsEnabled")
		{
			Source = CalendarBody
		});
		ArrangePanel.SetValue(Grid.ColumnProperty, 2);
		Line line = new Line
		{
			Y1 = 0.0,
			Y2 = 1.0,
			Stretch = Stretch.Fill,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Right,
			StrokeThickness = 1.0
		};
		line.SetValue(Grid.ColumnProperty, 1);
		line.SetResourceReference(Shape.StrokeProperty, "BaseColorOpacity4");
		line.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = ArrangePanel
		});
		Container.Children.Add(ArrangePanel);
		Container.Children.Add(line);
	}

	public void InitCalendarSideBar()
	{
		CalendarSide = new CalendarSideBar();
		CalendarSideBorder.Child = CalendarSide;
	}

	private void InitGridSplitter()
	{
		Splitter1 = new GridSplitter
		{
			Width = 5.0,
			Background = Brushes.Transparent,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		Splitter1.SetValue(Grid.ColumnProperty, 0);
		Splitter1.DragStarted += OnSplitter1DragStart;
		Splitter1.DragCompleted += OnSplitter1DragCompleted;
		Splitter1.DragDelta += OnSplitter1Dragging;
		Splitter1.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = CalendarSideBorder
		});
		Splitter2 = new GridSplitter
		{
			Width = 5.0,
			Background = Brushes.Transparent,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		Splitter2.SetValue(Grid.ColumnProperty, 2);
		Splitter2.DragStarted += OnSplitter2DragStart;
		Splitter2.DragCompleted += OnSplitter2DragCompleted;
		Splitter2.DragDelta += OnSplitter2Dragging;
		Splitter2.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = ArrangePanel
		});
		Container.Children.Add(Splitter1);
		Container.Children.Add(Splitter2);
	}
}
