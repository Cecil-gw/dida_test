using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Eisenhower;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.TabBar;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Util;

public static class MainWindowManager
{
	private static bool _tokenOutDateHandled;

	private static bool _resignIn;

	private static MainWindow _window;

	private static object _o = new object();

	private static bool _windowInited;

	public static MainWindow Window => _window;

	public static MatrixContainer MainMatrix => Window.MatrixContainer;

	public static ListViewContainer MainListView => Window.ListView;

	public static LeftMenuBar LeftBar => Window.LeftTabBar;

	public static CalendarControl MainCal => Window.MainCalendar;

	public static HabitContainer MainHabit => Window.HabitView;

	public static ImageSource BackImageSource { get; set; }

	public static void InitWindow()
	{
		if (_window == null)
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				_window = new MainWindow();
			});
		}
	}

	internal static void TrySelectFilter(FilterModel filter)
	{
		if (!LocalSettings.Settings.InSearch && filter != null)
		{
			FilterProjectIdentity identity = new FilterProjectIdentity(filter);
			MainListView.SelectProject(identity);
		}
	}

	public static void CheckSelectedModule()
	{
		Utils.RunOnUiThread(Application.Current.Dispatcher, delegate
		{
			Window.LeftTabBar.SetItems();
			bool flag = false;
			switch (LocalSettings.Settings.MainWindowDisplayModule)
			{
			case 3:
				if (!LocalSettings.Settings.ShowHabit)
				{
					flag = true;
				}
				break;
			case 2:
				if (!LocalSettings.Settings.ShowMatrix)
				{
					flag = true;
				}
				break;
			case 4:
				if (!LocalSettings.Settings.EnableFocus)
				{
					flag = true;
				}
				break;
			case 1:
				if (!LocalSettings.Settings.EnableCalendar)
				{
					flag = true;
				}
				break;
			case 5:
				if (!LocalSettings.Settings.EnableCountDownDays)
				{
					flag = true;
				}
				break;
			case 6:
				if (!LocalSettings.Settings.EnableAI)
				{
					flag = true;
				}
				break;
			}
			if (flag)
			{
				Window.SwitchModule(DisplayModule.Task);
			}
			Window.RefreshAIFeatureAvailability();
		});
	}

	public static ProjectIdentity GetSelectedProject()
	{
		if (LocalSettings.Settings.MainWindowDisplayModule == 2)
		{
			return null;
		}
		return MainListView?.GetSelectedProject();
	}

	public static void HandleLogout()
	{
		AppLockWindowManager.ResetOnLogout();
		_tokenOutDateHandled = true;
		_resignIn = true;
		Window.InvalidateWindowRestore();
		Window.Hide();
		Window.ClearChildren();
		SettingDialog.CloseInstance();
		Window.LeftTabBar.Reset();
		TickFocusManager.MainFocus = null;
	}

	public static bool IsReSignIn()
	{
		if (_resignIn)
		{
			_resignIn = false;
			return true;
		}
		return false;
	}

	public static void BeginSyncStory()
	{
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			LeftBar?.StartSyncStory();
		});
	}

	public static void TokenOutDate()
	{
		ThreadUtil.DetachedRunOnUiThread(delegate
		{
			if (!_tokenOutDateHandled)
			{
				_tokenOutDateHandled = true;
				MessageBox.Show(Utils.GetString("LoginOutDate"), Utils.GetString("LoginOutDate"), MessageBoxButton.OK);
				Window?.ForceLogout();
			}
		});
	}

	public static async Task HandleSyncFinished(SyncResult syncResult)
	{
		if (syncResult.SyncType == 1)
		{
			NotificationUnreadCount notificationCount = syncResult.NotificationCount;
			if (notificationCount == null || notificationCount.Activity <= 0)
			{
				NotificationUnreadCount notificationCount2 = syncResult.NotificationCount;
				if (notificationCount2 == null || notificationCount2.Notification <= 0)
				{
					LeftBar.HideNotificationIndicator();
					goto IL_0071;
				}
			}
			LeftBar.ShowNotificationIndicator(syncResult.NotificationCount);
		}
		goto IL_0071;
		IL_0071:
		if (syncResult.TeamChanged)
		{
			try
			{
				await LeftBar.LoadUserInfo(force: true);
			}
			catch (Exception ex)
			{
				UtilLog.Warn("MainWindowManager.HandleSyncFinished LoadUserInfo ex: " + ex);
			}
		}
		MainListView?.OnSyncFinished(syncResult);
		DataChangedNotifier.NotifyAutoSyncDone(syncResult);
		LeftBar.RefreshHead();
		if (syncResult.RemoteTasksChanged || syncResult.RemoteProjectsChanged)
		{
			ReminderCalculator.AssembleReminders();
		}
		App.Instance.TryDismissRemindersOnSync();
	}

	private static void ReloadAll()
	{
		Application.Current?.Dispatcher.Invoke(delegate
		{
			if (Window.ShowList)
			{
				TaskCountCache.SetNeedLoad();
				MainListView?.ReloadView();
				MainListView?.LoadSavedProject();
			}
			if (Window.ShowCalendar)
			{
				MainCal?.Reload(force: true);
			}
			if (Window.ShowMatrix)
			{
				MainMatrix?.LoadTask();
			}
		});
	}

	public static void InitWindowSettingOnLogin()
	{
		Window.InitRestoreLocation();
		Window.CheckLoginStatus();
		LeftBar.RefreshHead();
		TickFocusManager.SetFocusType((!(LocalSettings.Settings.PomoType == FocusConstance.Focus)) ? 1 : 0);
		TickFocusManager.LoadSavedFocus();
		CheckSelectedModule();
		_windowInited = true;
		_tokenOutDateHandled = false;
		WindowManager.AppLockOrExit = false;
	}

	public static void SetSyncErrorStatus(NetworkError errorType)
	{
		LeftBar?.SetNetworkStatus(errorType);
	}

	public static void ShowShortCutPanel()
	{
		Window.ShowShortCutPanel();
	}

	public static void DoOperation(SearchOperationViewModel model)
	{
		if (model.Type == SearchOperationType.Calendar || model.Type == SearchOperationType.Matrix || model.Type == SearchOperationType.Habit || model.Type == SearchOperationType.Focus || model.Type == SearchOperationType.Anniversary)
		{
			UserActCollectUtils.AddShortCutEvent("navigate", "switch_the_tab_bar");
		}
		switch (model.Type)
		{
		case SearchOperationType.AddTask:
			Window.InputCommand();
			break;
		case SearchOperationType.GlobalAddTask:
			Window.OpenAddTaskWindow();
			break;
		case SearchOperationType.ShowOrHideApp:
			Window.HideWindow();
			break;
		case SearchOperationType.OpenOrCloseFocusWindow:
			TickFocusManager.HideOrShowFocusWidget(force: true);
			break;
		case SearchOperationType.NewSticky:
			TaskStickyWindow.CreateNewStickyStatic();
			break;
		case SearchOperationType.Task:
			Window.SwitchModule(DisplayModule.Task);
			SearchHelper.InSearch = false;
			break;
		case SearchOperationType.Calendar:
			Window.SwitchModule(DisplayModule.Calendar);
			SearchHelper.InSearch = false;
			break;
		case SearchOperationType.Matrix:
			Window.SwitchModule(DisplayModule.Matrix);
			SearchHelper.InSearch = false;
			break;
		case SearchOperationType.Habit:
			Window.SwitchModule(DisplayModule.Habit);
			SearchHelper.InSearch = false;
			break;
		case SearchOperationType.AI:
			if (LocalSettings.Settings.EnableAI)
			{
				Window.SwitchModule(DisplayModule.AI);
				SearchHelper.InSearch = false;
			}
			break;
		case SearchOperationType.Focus:
			Window.SwitchModule(DisplayModule.Pomo);
			LocalSettings.Settings.InSearch = false;
			break;
		case SearchOperationType.Anniversary:
			Window.SwitchModule(DisplayModule.CountDown);
			LocalSettings.Settings.InSearch = false;
			break;
		case SearchOperationType.SearchTask:
			Window.SearchCommand();
			break;
		case SearchOperationType.GoSettings:
			LeftBar.ShowSettingDialog();
			break;
		case SearchOperationType.GoAll:
			Window.JumpSmartProject(SmartListType.All);
			break;
		case SearchOperationType.GoToday:
			Window.JumpSmartProject(SmartListType.Today);
			break;
		case SearchOperationType.GoTomorrow:
			Window.JumpSmartProject(SmartListType.Tomorrow);
			break;
		case SearchOperationType.GoNext7Day:
			Window.JumpSmartProject(SmartListType.Week);
			break;
		case SearchOperationType.GoAssignToMe:
			Window.JumpSmartProject(SmartListType.Assign);
			break;
		case SearchOperationType.GoInbox:
			Window.JumpSmartProject(SmartListType.Inbox);
			break;
		case SearchOperationType.GoCompleted:
			Window.JumpSmartProject(SmartListType.Completed);
			break;
		case SearchOperationType.GoAbandoned:
			Window.JumpSmartProject(SmartListType.Abandoned);
			break;
		case SearchOperationType.GoTrash:
			Window.JumpSmartProject(SmartListType.Trash);
			break;
		case SearchOperationType.GoSummary:
			Window.JumpSmartProject(SmartListType.Summary);
			break;
		case SearchOperationType.HelpCenter:
			GotoHelpCenter();
			break;
		case SearchOperationType.ShowShortCut:
			Window.ShowShortCutPanel();
			break;
		case SearchOperationType.GoProject:
			Window.NavigateProject("project", model.PtfId);
			break;
		case SearchOperationType.GoGroup:
			Window.NavigateGroup(model.PtfId);
			break;
		case SearchOperationType.GoTag:
			Window.NavigateProject("tag", model.PtfId);
			break;
		case SearchOperationType.GoFilter:
			Window.NavigateProject("filter", model.PtfId);
			break;
		case SearchOperationType.GoSearch:
			SearchHelper.ClearTaskSearchModels();
			Window.OnSearch(new SearchExtra
			{
				SearchKey = model.SearchText
			}, restore: false);
			break;
		case SearchOperationType.GoTaskGroup:
			Window.NavigateColumn(model.PtfId, model.ColumnId);
			break;
		case SearchOperationType.ShowHideSticky:
			break;
		}
	}

	private static void GotoHelpCenter()
	{
		Utils.TryProcessStartUrl(Utils.IsDida() ? "https://help.dida365.com/" : "https://help.ticktick.com/");
	}

	public static void SetCalendarProStatus()
	{
		Window.Dispatcher.Invoke(delegate
		{
			if (MainCal != null && MainCal.Visibility == Visibility.Visible)
			{
				if (!UserDao.IsPro() && ABTestManager.UsePremiumV2CalendarProStyle())
				{
					Window.ShowCalendarProView();
				}
				else
				{
					Window.BottomContent.Content = null;
				}
				MainCal.ShowProToast();
			}
			CalendarWidgetHelper.CheckProEnable();
			CalendarWindow.CheckProEnable();
		});
	}

	public static void OnDataInited()
	{
		ReloadAll();
	}
}
