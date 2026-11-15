using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using ticktick_WPF.Views;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CountDown.Views;
using ticktick_WPF.Views.Eisenhower;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.MainListView.TaskList;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Pomo.FocusStatistics;
using ticktick_WPF.Views.Print;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Subscribe;
using ticktick_WPF.Views.Summary;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Template;
using ticktick_WPF.Views.Theme;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Util;

internal static class AppLockWindowManager
{
	private static readonly HashSet<Popup> OpenPopups = new HashSet<Popup>();

	private static readonly HashSet<Window> HiddenWindows = new HashSet<Window>();

	public static bool IsLocked { get; private set; }

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr handle, int command);

	public static void TrackPopup(Popup popup)
	{
		popup.Opened += delegate
		{
			OpenPopups.Add(popup);
			if (IsLocked)
			{
				popup.SetCurrentValue(Popup.IsOpenProperty, false);
			}
		};
		popup.Closed += delegate
		{
			OpenPopups.Remove(popup);
		};
	}

	public static void Lock()
	{
		IsLocked = true;
		Popup[] array = OpenPopups.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetCurrentValue(Popup.IsOpenProperty, false);
		}
		foreach (Window item in from window in Application.Current.Windows.Cast<Window>().ToArray()
			where window.IsVisible && IsProtectedWindow(window)
			select window)
		{
			if (HiddenWindows.Add(item))
			{
				item.Closed += OnWindowClosed;
			}
			ShowWindow(new WindowInteropHelper(item).Handle, 0);
		}
	}

	private static bool IsProtectedWindow(Window window)
	{
		if (window is NavigateWebBrowserWindow)
		{
			return true;
		}
		if (window is SettingDialog || window is CalendarDisplaySettingWindow || window is HabitSettingWindow || window is PomoSettings || window is KanbanDisplaySettingWindow || window is TimelineSettingsWindow)
		{
			return true;
		}
		if (window is AddProjectGroupDialog || window is AddOrEditTagWindow || window is FilterEditDialog || window is FilterPreviewWindow || window is PrintPreviewWindow)
		{
			return true;
		}
		if (window is SelectSubscribeTypeWindow || window is IntegratedCalendarsWindow || window is LinkGoogleCalendarWindow || window is BindOutlookWindow || window is BindAccountWindow || window is BindICloudWindow || window is AddUrlCalendarWindow || window is EditBindAccountWindow || window is CalendarConnectInfoWindow || window is GetTickUrlLinkDialog)
		{
			return true;
		}
		if (window is CalendarColorConfigWindow || window is CalendarItemIconVisWindow || window is CalendarStyleSelectWindow || window is CalendarTimeZoneConfigWindow || window is AddCalendarWindow || window is EditHolidaySubsWindow || window is EditUrlWindow || window is IntegrationsAndImportTypeSelectWindow)
		{
			return true;
		}
		if (window is AddTemplateDialog || window is EditTemplateWindow || window is PreviewTemplateWindow || window is InsertSummaryWindow || window is AICompleteWindow || window is AIRecommendWindow || window is AIAudioTransResultWindow)
		{
			return true;
		}
		if (window is ShareProjectDialog || window is TransferProjectWindow || window is DuplicateProjectDialog || window is ModifyDefaultProjectDialog || window is ProjectActivityWindow || window is TaskListViewOptionsDialog || window is SearchFilterDialog || window is SearchOperationDialog || window is EditInboxDialog)
		{
			return true;
		}
		if (window is AddFocusRecordWindow || window is BatchFocusRecWindow || window is EditHabitLogWindow || window is CountdownRemarkWindow || window is CountdownStyleDialog || window is HolidaySelectionWindow)
		{
			return true;
		}
		if (window is CustomThemeDialog || window is InterfaceStyleWindow || window is AutoDarkSettingWindow)
		{
			return true;
		}
		if (!(window is LoadMoreWindow) && !(window is EditQuadrantWindow) && !(window is AddTimerWindow) && !(window is AddOrEditHabitDialog) && !(window is EditCountDownDialog) && !(window is EditProjectDialog) && !(window is SearchDialog))
		{
			if (window.Owner != null)
			{
				return IsProtectedWindow(window.Owner);
			}
			return false;
		}
		return true;
	}

	private static void OnWindowClosed(object sender, EventArgs args)
	{
		Window window = (Window)sender;
		window.Closed -= OnWindowClosed;
		HiddenWindows.Remove(window);
	}

	public static void Unlock()
	{
		IsLocked = false;
		Window[] array = HiddenWindows.ToArray();
		HiddenWindows.Clear();
		Window[] array2 = array;
		foreach (Window obj in array2)
		{
			obj.Closed -= OnWindowClosed;
			ShowWindow(new WindowInteropHelper(obj).Handle, 8);
		}
	}

	public static void ResetOnLogout()
	{
		Window[] array = HiddenWindows.ToArray();
		foreach (Window window in array)
		{
			if (HiddenWindows.Contains(window))
			{
				window.Close();
			}
		}
		IsLocked = false;
	}
}
