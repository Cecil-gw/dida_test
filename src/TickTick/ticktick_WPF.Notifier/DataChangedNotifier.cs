using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.MarkDown.Colorizer;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Notifier;

public static class DataChangedNotifier
{
	public delegate void StrStrDelegate(string str1, string str2);

	public static event EventHandler ProjectChanged;

	public static event EventHandler<ProjectIdentity> ViewModeChanged;

	public static event EventHandler<string> ProjectColumnChanged;

	public static event EventHandler<ProjectGroupModel> ProjectGroupChanged;

	public static event EventHandler<FilterChangeArgs> FilterChanged;

	public static event EventHandler TeamChanged;

	public static event EventHandler WeekStartFromChanged;

	public static event EventHandler YearStartFromChanged;

	public static event EventHandler<SyncResult> SyncDone;

	public static event EventHandler PeriodicCheck;

	public static event EventHandler<bool> CalendarConfigChanged;

	public static event EventHandler<CountdownNotifier> CountdownsChanged;

	public static event EventHandler CountdownsSyncDown;

	public static event EventHandler CountdownShowRemarkChanged;

	public static event EventHandler CalendarChanged;

	public static event EventHandler CalendarConnectChanged;

	public static event EventHandler TaskGoogleEventInfoChanged;

	public static event EventHandler EventArchivedChanged;

	public static event EventHandler CalendarShowIconChanged;

	public static event EventHandler<string> CalendarTitleChanged;

	public static event EventHandler<bool> CalendarShowYearHeatMapChanged;

	public static event EventHandler CalendarTaskBarStyleChanged;

	public static event EventHandler CalendarProjectFilterChanged;

	public static event EventHandler TimeFormatChanged;

	public static event EventHandler<string> PomoChanged;

	public static event EventHandler TaskDefaultChanged;

	public static event EventHandler TagDeleted;

	public static event EventHandler<TagModel> TagChanged;

	public static event EventHandler TagTypeChanged;

	public static event EventHandler ThemeModeChanged;

	public static event EventHandler IsDarkChanged;

	public static event EventHandler ChooseDarkThemeChanged;

	public static event EventHandler MainWindowHidden;

	public static event EventHandler HideCompleteChanged;

	public static event EventHandler<HabitCheckInModel> HabitCheckInChanged;

	public static event EventHandler<string> HabitCyclesChanged;

	public static event EventHandler<string> HabitArchived;

	public static event EventHandler HabitLogChanged;

	public static event EventHandler HabitsChanged;

	public static event EventHandler<string> HabitSkip;

	public static event EventHandler HabitSectionChanged;

	public static event EventHandler HabitsSyncDone;

	public static event EventHandler TimelineSettingsChanged;

	public static event EventHandler ScheduleChanged;

	public static event EventHandler<int> MatrixQuadrantChanged;

	public static event EventHandler ShowCompleteLineChanged;

	public static event EventHandler ProjectPinRemoteChanged;

	public static event EventHandler<(string, SortOption)> ListSectionOpenChanged;

	public static event EventHandler<string> SortOptionChanged;

	public static event EventHandler<bool> CalOrListBatchSelected;

	public static event StrStrDelegate NewTaskWhenModifyRepeat;

	public static event EventHandler FontFamilyChanged;

	public static event EventHandler InterfaceStyleChanged;

	public static event EventHandler ProjectListColorVisChanged;

	public static event EventHandler TemplateChanged;

	public static event EventHandler PaymentUpdated;

	public static event EventHandler<TaskModel> AddTaskToSelectView;

	public static void NotifyPaymentUpdated()
	{
		DataChangedNotifier.PaymentUpdated?.Invoke(null, null);
	}

	public static void NotifyTemplateChanged()
	{
		DataChangedNotifier.TemplateChanged?.Invoke(null, null);
	}

	public static void NotifyHabitsSyncDone()
	{
		DataChangedNotifier.HabitsSyncDone?.Invoke(null, null);
	}

	internal static void OnScheduleChanged()
	{
		DataChangedNotifier.ScheduleChanged?.Invoke(null, null);
	}

	public static void NotifyHabitsChanged()
	{
		DataChangedNotifier.HabitsChanged?.Invoke(null, null);
	}

	public static void NotifyCountdownsSyncDone()
	{
		DataChangedNotifier.CountdownsSyncDown?.Invoke(null, null);
	}

	public static void NotifyCountdownsChanged(CountdownNotifier notifier = null)
	{
		DataChangedNotifier.CountdownsChanged?.Invoke(null, notifier);
	}

	public static void NotifyTaskGoogleEventInfoChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.TaskGoogleEventInfoChanged?.Invoke(null, EventArgs.Empty);
		});
	}

	public static void NotifyCountdownShowRemarkChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.CountdownShowRemarkChanged?.Invoke(null, null);
		});
	}

	public static void NotifyHabitSkip(string habitId)
	{
		DataChangedNotifier.HabitSkip?.Invoke(null, habitId);
	}

	public static void NotifyHabitSectionChanged()
	{
		DataChangedNotifier.HabitSectionChanged?.Invoke(null, null);
	}

	public static void NotifyHabitCyclesChanged(string str)
	{
		DataChangedNotifier.HabitCyclesChanged?.Invoke(null, str);
	}

	public static async void NotifyHabitCheckInChanged(HabitCheckInModel checkIn, object sender = null)
	{
		DataChangedNotifier.HabitCheckInChanged?.Invoke(sender, checkIn);
		bool num = checkIn.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		if (num && TickFocusManager.IsFocusing(checkIn.HabitId) && checkIn.CheckStatus != 0)
		{
			FocusTimer.TryStartWithId("");
		}
		HabitCheckinCache.OnHabitCheckInChanged(sender, checkIn.HabitId);
		if (!num)
		{
			return;
		}
		if (ABTestManager.IsNewRemindCalculate())
		{
			try
			{
				await HabitReminderCalculator.RecalHabitReminder(checkIn.HabitId);
				return;
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
				return;
			}
		}
		ReminderCalculator.AssembleReminders();
	}

	public static void NotifyTagDeleted()
	{
		DataChangedNotifier.TagDeleted?.Invoke(null, null);
	}

	public static void NotifyTagChanged(TagModel model, string originName = null)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.TagChanged?.Invoke(originName, model);
		});
	}

	public static void NotifyTagTypeChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.TagTypeChanged?.Invoke(null, null);
		});
	}

	public static void NotifyProjectChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.ProjectChanged?.Invoke(null, null);
			DataChangedNotifier.TaskDefaultChanged?.Invoke(null, null);
		});
	}

	public static void NotifyGroupChanged(ProjectGroupModel group)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.ProjectGroupChanged?.Invoke(null, group);
		});
	}

	public static void NotifyFilterChanged(FilterChangeArgs args)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.FilterChanged?.Invoke(null, args);
		});
	}

	public static void NotifyTeamChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.TeamChanged?.Invoke(null, null);
		});
	}

	public static void NotifyWeekStartFromChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			ScheduleService.CourseDisplayModelDict.Clear();
			DataChangedNotifier.WeekStartFromChanged?.Invoke(null, null);
		});
	}

	public static void NotifyYearStartFromChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.YearStartFromChanged?.Invoke(null, null);
		});
	}

	public static void NotifyAutoSyncDone(SyncResult result)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.SyncDone?.Invoke(null, result);
		});
	}

	public static void NotifyPeriodicCheck()
	{
		DataChangedNotifier.PeriodicCheck?.Invoke(null, null);
	}

	public static void NotifyCalendarConfigChanged(bool colorChanged)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.CalendarConfigChanged?.Invoke(null, colorChanged);
		});
	}

	public static void NotifyCalendarProjectFilterChanged(object sender)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.CalendarProjectFilterChanged?.Invoke(sender, null);
		});
	}

	public static void NotifyCalendarChanged(int interval = 1000)
	{
		DelayActionHandlerCenter.TryDoAction("NotifySubscribeCalendarChanged", delegate
		{
			Application.Current?.Dispatcher?.InvokeAsync(delegate
			{
				DataChangedNotifier.CalendarChanged?.Invoke(null, null);
			});
		}, interval);
	}

	public static void NotifyCalendarConnectChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.CalendarConnectChanged?.Invoke(null, null);
		});
	}

	public static void NotifyCalendarShowIconChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.CalendarShowIconChanged?.Invoke(null, null);
		});
	}

	public static void NotifyCalendarTitleChanged(string id)
	{
		DataChangedNotifier.CalendarTitleChanged?.Invoke(null, id);
	}

	public static void NotifyCalendarYearHeatMapChanged(bool show)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.CalendarShowYearHeatMapChanged?.Invoke(null, show);
		});
	}

	public static void NotifyCalendarTaskbarStyleChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.CalendarTaskBarStyleChanged?.Invoke(null, null);
		});
	}

	public static void NotifyEventArchivedChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.EventArchivedChanged?.Invoke(null, null);
		});
	}

	public static void NotifyPomoChanged(string taskId)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.PomoChanged?.Invoke(null, taskId);
		});
	}

	public static void NotifyTaskDefaultChanged()
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			DataChangedNotifier.TaskDefaultChanged?.Invoke(null, null);
		});
	}

	public static void NotifyTimeFormatChanged()
	{
		DataChangedNotifier.TimeFormatChanged?.Invoke(null, null);
	}

	public static void NotifyThemeModeChanged()
	{
		DataChangedNotifier.ThemeModeChanged?.Invoke(null, null);
		KanbanColumnView.SetEmptyBrushColor();
		App.Window.ChangeHeaderButtonColor();
	}

	public static void NotifyIsDarkChanged()
	{
		DeleteLineColorizer.OnThemeChanged();
		DataChangedNotifier.IsDarkChanged?.Invoke(null, null);
	}

	public static void NotifyChooseDarkThemeChanged()
	{
		DataChangedNotifier.ChooseDarkThemeChanged?.Invoke(null, null);
	}

	public static void NotifyMainWindowHidden()
	{
		DataChangedNotifier.MainWindowHidden?.Invoke(null, null);
	}

	public static void NotifyHideCompleteChanged()
	{
		DataChangedNotifier.HideCompleteChanged?.Invoke(null, null);
	}

	public static void NotifyColumnChanged(string projectId)
	{
		ThreadUtil.DetachedRunOnUiBackThread(delegate
		{
			DataChangedNotifier.ProjectColumnChanged?.Invoke(null, projectId);
		});
	}

	public static void NotifyMatrixQuadrantChanged(int level)
	{
		DataChangedNotifier.MatrixQuadrantChanged?.Invoke(null, level);
	}

	public static void NotifyTimelineSettingsChanged()
	{
		ThreadUtil.DetachedRunOnUiBackThread(delegate
		{
			DataChangedNotifier.TimelineSettingsChanged?.Invoke(null, null);
		});
	}

	public static void NotifyShowCompleteLineChanged()
	{
		DataChangedNotifier.ShowCompleteLineChanged?.Invoke(null, null);
	}

	public static void NotifyProjectViewModeChanged(ProjectIdentity identity)
	{
		DataChangedNotifier.ViewModeChanged?.Invoke(null, identity);
	}

	public static void NotifyProjectPinRemoteChanged()
	{
		DataChangedNotifier.ProjectPinRemoteChanged?.Invoke(null, null);
	}

	public static void NotifyHabitArchived(string habitId)
	{
		DataChangedNotifier.HabitArchived?.Invoke(null, habitId);
	}

	public static void NotifyHabitLogChanged()
	{
		DataChangedNotifier.HabitLogChanged?.Invoke(null, null);
	}

	public static async Task NotifyListSectionOpenChanged(object sender, (string, SortOption) idToSort)
	{
		await Task.Delay(100);
		DataChangedNotifier.ListSectionOpenChanged?.Invoke(sender, idToSort);
	}

	public static void NotifyFontFamilyChanged()
	{
		DataChangedNotifier.FontFamilyChanged?.Invoke(null, null);
	}

	public static void NotifySortOptionChanged(string catId)
	{
		DataChangedNotifier.SortOptionChanged?.Invoke(null, catId);
	}

	public static void NotifyNewTaskWhenModifyRepeat(string taskId, string originTaskId)
	{
		DataChangedNotifier.NewTaskWhenModifyRepeat?.Invoke(taskId, originTaskId);
	}

	public static void NotifyCalOrListBatchSelected(bool calBatchSelected)
	{
		DataChangedNotifier.CalOrListBatchSelected?.Invoke(null, calBatchSelected);
	}

	public static void NotifyAddTaskToSelectView(TaskModel task)
	{
		DataChangedNotifier.AddTaskToSelectView?.Invoke(null, task);
	}

	public static void NotifyInterfaceStyleChanged()
	{
		DataChangedNotifier.InterfaceStyleChanged?.Invoke(null, null);
	}

	public static void NotifyProjectListColorVisChanged()
	{
		DataChangedNotifier.ProjectListColorVisChanged?.Invoke(null, null);
	}
}
