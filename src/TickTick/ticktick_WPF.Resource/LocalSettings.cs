using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Summary;

namespace ticktick_WPF.Resource;

[Serializable]
public class LocalSettings : INotifyPropertyChanged
{
	public static readonly LocalSettings Settings = new LocalSettings();

	private bool _isChanged = true;

	public SettingsModel SettingsModel = new SettingsModel();

	public CommonSettings Common = new CommonSettings();

	public CustomThemeSetting CustomTheme = new CustomThemeSetting();

	private CalendarDisplaySettings _calendarDisplaySettings;

	private ProjectListOpenStatus _projectListOpenStatus;

	private ShortcutModel _shortcutModel;

	private PomoLocalSetting _pomoLocalSetting;

	private StatisticsModel _statisticsModel;

	private bool _inSearch;

	private SummaryFilterModel _summaryFilter;

	private List<SmartProjectConf> _smartProjects;

	private bool _projectPinFolded = true;

	private FontFamily _fontFamily;

	private ExtraSettings _extraSettings;

	public UserPreferenceModel UserPreference { get; set; }

	public DesktopTabBar DesktopConfig => UserPreference?.desktopTabBars;

	public bool EnableCalendarTimeZone
	{
		get
		{
			if (UserPreference?.timelineTimeZones != null)
			{
				return UserPreference.timelineTimeZones.enabled;
			}
			return false;
		}
	}

	public Action OnPreferenceChanged { get; set; }

	public bool ShowMatrix => TabBarModel.IsActive("Matrix", DesktopConfig?.bars);

	public MatrixModel MatrixModel => UserPreference?.matrix;

	public string MatrixActiveViewId
	{
		get
		{
			Dictionary<string, string> dictionary = Common?.GetOtherSettings().MatrixActiveViewIds;
			if (dictionary == null || !dictionary.TryGetValue(LoginUserId, out var value))
			{
				return MatrixModel?.LegacyActiveViewId;
			}
			return value;
		}
		set
		{
			if (Common != null && !string.IsNullOrEmpty(LoginUserId))
			{
				CommonOtherSettings otherSettings = Common.GetOtherSettings();
				if (otherSettings.MatrixActiveViewIds == null)
				{
					otherSettings.MatrixActiveViewIds = new Dictionary<string, string>();
				}
				otherSettings.MatrixActiveViewIds[LoginUserId] = value;
				Common.SetOtherSettings(otherSettings);
				SetChanged();
			}
		}
	}

	public TimelineSettingsModel TimelineSettingsModel => UserPreference?.Timeline;

	public bool? PendingMatrixShowCompleted
	{
		get
		{
			Dictionary<string, bool> dictionary = Common?.GetOtherSettings().PendingMatrixShowCompleted;
			if (dictionary == null || !dictionary.TryGetValue(LoginUserId, out var value))
			{
				return null;
			}
			return value;
		}
		private set
		{
			if (Common != null && !string.IsNullOrEmpty(LoginUserId))
			{
				CommonOtherSettings otherSettings = Common.GetOtherSettings();
				if (otherSettings.PendingMatrixShowCompleted == null)
				{
					otherSettings.PendingMatrixShowCompleted = new Dictionary<string, bool>();
				}
				if (value.HasValue)
				{
					otherSettings.PendingMatrixShowCompleted[LoginUserId] = value.Value;
				}
				else
				{
					otherSettings.PendingMatrixShowCompleted.Remove(LoginUserId);
				}
				Common.SetOtherSettings(otherSettings);
				SetChanged();
			}
		}
	}

	public bool MatrixShowCompleted => PendingMatrixShowCompleted ?? MatrixModel?.show_completed ?? true;

	public bool ShowTimeTableInCal
	{
		get
		{
			if (UserDao.IsPro())
			{
				return UserPreference.TimeTable?.ShowInCal ?? false;
			}
			return false;
		}
	}

	public bool AutoAcceptShare => UserPreference.TeamConfig?.autoAcceptInvite ?? true;

	public object this[string propertyName]
	{
		get
		{
			PropertyInfo propertyInfo = GetType().GetProperties().FirstOrDefault((PropertyInfo f) => string.Equals(f.Name.ToLower(), propertyName.ToLower(), StringComparison.CurrentCultureIgnoreCase));
			if (!(propertyInfo != null))
			{
				return null;
			}
			return propertyInfo.GetValue(this);
		}
		set
		{
			GetType().GetProperties().FirstOrDefault((PropertyInfo f) => string.Equals(f.Name, propertyName, StringComparison.CurrentCultureIgnoreCase))?.SetValue(this, value);
		}
	}

	public CalendarDisplaySettings CalendarDisplaySettings
	{
		get
		{
			if (_calendarDisplaySettings == null && !string.IsNullOrEmpty(Settings.SettingsModel.CalendarDisplaySettings))
			{
				_calendarDisplaySettings = JsonConvert.DeserializeObject<CalendarDisplaySettings>(Settings.SettingsModel.CalendarDisplaySettings);
			}
			_calendarDisplaySettings = _calendarDisplaySettings ?? new CalendarDisplaySettings();
			_calendarDisplaySettings.CheckNull();
			return _calendarDisplaySettings;
		}
		set
		{
			_calendarDisplaySettings = value;
		}
	}

	public ProjectListOpenStatus ProjectListOpenStatus
	{
		get
		{
			if (_projectListOpenStatus == null && !string.IsNullOrEmpty(Settings.SettingsModel.ProjectListOpenStatus))
			{
				_projectListOpenStatus = JsonConvert.DeserializeObject<ProjectListOpenStatus>(Settings.SettingsModel.ProjectListOpenStatus);
			}
			_projectListOpenStatus = _projectListOpenStatus ?? new ProjectListOpenStatus();
			return _projectListOpenStatus;
		}
		set
		{
			_projectListOpenStatus = value;
		}
	}

	public ShortcutModel ShortCutModel
	{
		get
		{
			if (_shortcutModel != null)
			{
				return _shortcutModel;
			}
			_shortcutModel = new ShortcutModel
			{
				CanNotify = true
			};
			try
			{
				ShortcutModel shortCut = ((!string.IsNullOrEmpty(Settings.SettingsModel.ShortcutModel)) ? JsonConvert.DeserializeObject<ShortcutModel>(Settings.SettingsModel.ShortcutModel) : null);
				_shortcutModel.CopyProperties(shortCut);
			}
			catch (Exception ex)
			{
				UtilLog.Warn(ex.Message);
			}
			return _shortcutModel;
		}
		set
		{
			_shortcutModel = value;
		}
	}

	public PomoLocalSetting PomoLocalSetting
	{
		get
		{
			if (_pomoLocalSetting == null && !string.IsNullOrEmpty(Settings.SettingsModel.PomoLocalSetting))
			{
				_pomoLocalSetting = JsonConvert.DeserializeObject<PomoLocalSetting>(Settings.SettingsModel.PomoLocalSetting) ?? new PomoLocalSetting();
			}
			_pomoLocalSetting = _pomoLocalSetting ?? new PomoLocalSetting();
			return _pomoLocalSetting;
		}
	}

	public StatisticsModel StatisticsModel
	{
		get
		{
			if (_statisticsModel == null && !string.IsNullOrEmpty(Settings.SettingsModel.Statistics))
			{
				_statisticsModel = JsonConvert.DeserializeObject<StatisticsModel>(Settings.SettingsModel.Statistics);
			}
			return _statisticsModel;
		}
		set
		{
			_statisticsModel = value;
		}
	}

	public int InBoxId
	{
		get
		{
			return Common.InBoxId;
		}
		set
		{
			Common.InBoxId = value;
			_isChanged = true;
		}
	}

	public string LoginUserId
	{
		get
		{
			return Common.LoginUserId;
		}
		set
		{
			Common.LoginUserId = value;
			GlobalSettings.UserId = value;
			_isChanged = true;
		}
	}

	public string LoginUserAuth
	{
		get
		{
			return Common.LoginUserAuth;
		}
		set
		{
			Common.LoginUserAuth = value;
			_isChanged = true;
		}
	}

	public string LoginUserName
	{
		get
		{
			return Common.LoginUserName;
		}
		set
		{
			Common.LoginUserName = value;
			_isChanged = true;
		}
	}

	public string LastLoginAccount
	{
		get
		{
			return Common.LastLoginAccount;
		}
		set
		{
			Common.LastLoginAccount = value;
			_isChanged = true;
		}
	}

	public string LoginAvatarUrl
	{
		get
		{
			return Common.LoginAvatarUrl;
		}
		set
		{
			Common.LoginAvatarUrl = value;
			_isChanged = true;
		}
	}

	public string InServerBoxId
	{
		get
		{
			return Common.InServerBoxId;
		}
		set
		{
			Common.InServerBoxId = value;
			_isChanged = true;
		}
	}

	public string LocalTimeZone
	{
		get
		{
			return Common.LocalTimeZone;
		}
		set
		{
			Common.LocalTimeZone = value;
			_isChanged = true;
		}
	}

	public int GrayVersionDate
	{
		get
		{
			return Common.GrayVersionDate;
		}
		set
		{
			Common.GrayVersionDate = value;
			_isChanged = true;
		}
	}

	public string UserDeviceString
	{
		get
		{
			return Common.UserDeviceString;
		}
		set
		{
			Common.UserDeviceString = value;
			_isChanged = true;
		}
	}

	public string CustomThemeLocation
	{
		get
		{
			return CustomTheme.CustomThemeLocation;
		}
		set
		{
			CustomTheme.CustomThemeLocation = value;
		}
	}

	public string CustomThemeColor
	{
		get
		{
			return CustomTheme.CustomThemeColor;
		}
		set
		{
			CustomTheme.CustomThemeColor = value;
		}
	}

	public double ThemeImageOpacity
	{
		get
		{
			return Math.Max(CustomTheme.ThemeImageOpacity, 0.6);
		}
		set
		{
			CustomTheme.ThemeImageOpacity = Math.Max(value, 0.6);
			OnPropertyChanged("ThemeImageOpacity");
		}
	}

	public int ThemeImageBlurRadius
	{
		get
		{
			return Math.Min(CustomTheme.ThemeImageBlurRadius, 40);
		}
		set
		{
			CustomTheme.ThemeImageBlurRadius = Math.Min(value, 40);
			OnPropertyChanged("ThemeImageBlurRadius");
		}
	}

	public double ShowAreaOpacity
	{
		get
		{
			return Math.Max(CustomTheme.ShowAreaOpacity, 0.8);
		}
		set
		{
			CustomTheme.ShowAreaOpacity = Math.Max(value, 0.8);
		}
	}

	public double MainWindowHeight
	{
		get
		{
			return SettingsModel.MainWindowHeight;
		}
		set
		{
			SettingsModel.MainWindowHeight = value;
			OnPropertyChanged("MainWindowHeight");
		}
	}

	public double MainWindowWidth
	{
		get
		{
			return SettingsModel.MainWindowWidth;
		}
		set
		{
			SettingsModel.MainWindowWidth = value;
			OnPropertyChanged("MainWindowWidth");
		}
	}

	public long CheckPoint
	{
		get
		{
			return SettingsModel.CheckPoint;
		}
		set
		{
			SettingsModel.CheckPoint = value;
			OnPropertyChanged("CheckPoint");
		}
	}

	public int SmartListAll
	{
		get
		{
			if (SettingsModel.SmartListAll < 0)
			{
				SettingsModel.SmartListAll = 0;
			}
			return SettingsModel.SmartListAll;
		}
		set
		{
			SettingsModel.SmartListAll = value;
			OnPropertyChanged("SmartListAll");
		}
	}

	public int SmartListInbox
	{
		get
		{
			if (SettingsModel.SmartListInbox < 0)
			{
				SettingsModel.SmartListInbox = 0;
			}
			return SettingsModel.SmartListInbox;
		}
		set
		{
			SettingsModel.SmartListInbox = value;
			OnPropertyChanged("SmartListInbox");
		}
	}

	public int SmartListToday
	{
		get
		{
			if (SettingsModel.SmartListToday < 0)
			{
				SettingsModel.SmartListToday = 0;
			}
			return SettingsModel.SmartListToday;
		}
		set
		{
			SettingsModel.SmartListToday = value;
			OnPropertyChanged("SmartListToday");
		}
	}

	public int SmartListTomorrow
	{
		get
		{
			if (SettingsModel.SmartListTomorrow < 0)
			{
				SettingsModel.SmartListTomorrow = 0;
			}
			return SettingsModel.SmartListTomorrow;
		}
		set
		{
			SettingsModel.SmartListTomorrow = value;
			OnPropertyChanged("SmartListTomorrow");
		}
	}

	public int SmartList7Day
	{
		get
		{
			if (SettingsModel.SmartList7Day < 0)
			{
				SettingsModel.SmartList7Day = 0;
			}
			return SettingsModel.SmartList7Day;
		}
		set
		{
			SettingsModel.SmartList7Day = value;
			OnPropertyChanged("SmartList7Day");
		}
	}

	public int SmartListForMe
	{
		get
		{
			if (SettingsModel.SmartListForMe < 0)
			{
				SettingsModel.SmartListForMe = 0;
			}
			return SettingsModel.SmartListForMe;
		}
		set
		{
			SettingsModel.SmartListForMe = value;
			OnPropertyChanged("SmartListForMe");
		}
	}

	public int SmartListComplete
	{
		get
		{
			if (SettingsModel.SmartListComplete < 0)
			{
				SettingsModel.SmartListComplete = 0;
			}
			return SettingsModel.SmartListComplete;
		}
		set
		{
			SettingsModel.SmartListComplete = value;
			OnPropertyChanged("SmartListComplete");
		}
	}

	public int SmartListAbandoned
	{
		get
		{
			if (SettingsModel.SmartListAbandoned < 0)
			{
				SettingsModel.SmartListAbandoned = 0;
			}
			return SettingsModel.SmartListAbandoned;
		}
		set
		{
			SettingsModel.SmartListAbandoned = value;
			OnPropertyChanged("SmartListAbandoned");
		}
	}

	public int SmartListTrash
	{
		get
		{
			if (SettingsModel.SmartListTrash < 0)
			{
				SettingsModel.SmartListTrash = 0;
			}
			return SettingsModel.SmartListTrash;
		}
		set
		{
			SettingsModel.SmartListTrash = value;
			OnPropertyChanged("SmartListTrash");
		}
	}

	public string ShortcutOpenOrClose
	{
		get
		{
			return SettingsModel.ShortcutOpenOrClose;
		}
		set
		{
			SettingsModel.ShortcutOpenOrClose = value;
			OnPropertyChanged("ShortcutOpenOrClose");
		}
	}

	public string ShortcutAddTask
	{
		get
		{
			return SettingsModel.ShortcutAddTask;
		}
		set
		{
			SettingsModel.ShortcutAddTask = value;
			OnPropertyChanged("ShortcutAddTask");
		}
	}

	public int ShowCompleteLine
	{
		get
		{
			return ExtraSettings.ShowCompleteLine;
		}
		set
		{
			ExtraSettings.ShowCompleteLine = value;
			OnPropertyChanged("ShowCompleteLine");
		}
	}

	public bool IsPro
	{
		get
		{
			return SettingsModel.IsPro;
		}
		set
		{
			if (SettingsModel.IsPro != value)
			{
				SettingsModel.IsPro = value;
				MainWindowManager.SetCalendarProStatus();
				OnPropertyChanged("IsPro");
			}
		}
	}

	public string SortTypeOfAllProject
	{
		get
		{
			return SettingsModel.SortTypeOfAllProject;
		}
		set
		{
			SettingsModel.SortTypeOfAllProject = value;
			_isChanged = true;
		}
	}

	public string SortTypeOfInbox
	{
		get
		{
			return SettingsModel.SortTypeOfInbox;
		}
		set
		{
			SettingsModel.SortTypeOfInbox = value;
			OnPropertyChanged("SortTypeOfInbox");
		}
	}

	public string SortTypeOfAssignMe
	{
		get
		{
			return SettingsModel.SortTypeOfAssignMe;
		}
		set
		{
			SettingsModel.SortTypeOfAssignMe = value;
			OnPropertyChanged("SortTypeOfAssignMe");
		}
	}

	public string SortTypeOfToday
	{
		get
		{
			return SettingsModel.SortTypeOfToday;
		}
		set
		{
			SettingsModel.SortTypeOfToday = value;
			OnPropertyChanged("SortTypeOfToday");
		}
	}

	public string SortTypeOfTomorrow
	{
		get
		{
			return SettingsModel.SortTypeOfTomorrow;
		}
		set
		{
			SettingsModel.SortTypeOfTomorrow = value;
			OnPropertyChanged("SortTypeOfTomorrow");
		}
	}

	public string SortTypeOfWeek
	{
		get
		{
			return SettingsModel.SortTypeOfWeek;
		}
		set
		{
			SettingsModel.SortTypeOfWeek = value;
			OnPropertyChanged("SortTypeOfWeek");
		}
	}

	public string ClosedSectionStatus
	{
		get
		{
			return SettingsModel.ClosedSectionStatus;
		}
		set
		{
			SettingsModel.ClosedSectionStatus = value;
			OnPropertyChanged("ClosedSectionStatus");
		}
	}

	public bool MainWindowTopmost
	{
		get
		{
			return SettingsModel.MainWindowTopmost;
		}
		set
		{
			SettingsModel.MainWindowTopmost = value;
			OnPropertyChanged("MainWindowTopmost");
		}
	}

	public int SmartListTag
	{
		get
		{
			if (SettingsModel.SmartListTag < 0)
			{
				SettingsModel.SmartListTag = 0;
			}
			return SettingsModel.SmartListTag;
		}
		set
		{
			SettingsModel.SmartListTag = value;
			OnPropertyChanged("SmartListTag");
		}
	}

	public string UserChooseLanguage
	{
		get
		{
			if (string.IsNullOrEmpty(Common.UserChooseLanguage))
			{
				return Utils.GetSystemLanguage();
			}
			return Common.UserChooseLanguage;
		}
		set
		{
			Common.UserChooseLanguage = value;
			OnPropertyChanged("UserChooseLanguage");
		}
	}

	public int ShowCustomSmartList
	{
		get
		{
			if (SettingsModel.ShowCustomSmartList < 0)
			{
				SettingsModel.ShowCustomSmartList = 0;
			}
			return SettingsModel.ShowCustomSmartList;
		}
		set
		{
			SettingsModel.ShowCustomSmartList = value;
			OnPropertyChanged("ShowCustomSmartList");
		}
	}

	public long SyncPoint
	{
		get
		{
			return SettingsModel.SyncPoint;
		}
		set
		{
			SettingsModel.SyncPoint = value;
			OnPropertyChanged("SyncPoint");
		}
	}

	public string CalendarFilterData
	{
		get
		{
			return SettingsModel.CalendarFilterData;
		}
		set
		{
			SettingsModel.CalendarFilterData = value;
			OnPropertyChanged("CalendarFilterData");
		}
	}

	public string ArrangeTaskFilterData
	{
		get
		{
			return SettingsModel.ArrangeTaskFilterData;
		}
		set
		{
			SettingsModel.ArrangeTaskFilterData = value;
			OnPropertyChanged("ArrangeTaskFilterData");
		}
	}

	public bool FocusKeepInSync
	{
		get
		{
			if (UserDao.IsPro())
			{
				return UserPreference?.FocusConfig?.keepInSync == true;
			}
			return false;
		}
	}

	public int PomoDuration
	{
		get
		{
			return SettingsModel.PomoDuration;
		}
		set
		{
			if (SettingsModel.PomoDuration != value)
			{
				SettingsModel.PomoDuration = value;
				TickFocusManager.OnPomoSettingsChanged();
				_isChanged = true;
			}
		}
	}

	public string PomoSound
	{
		get
		{
			return PomoLocalSetting.PomoSound;
		}
		set
		{
			PomoLocalSetting.PomoSound = value;
			OnPropertyChanged("PomoSound");
		}
	}

	public int ShortBreakDuration
	{
		get
		{
			if (SettingsModel.ShortBreakDuration > 0)
			{
				return SettingsModel.ShortBreakDuration;
			}
			return 5;
		}
		set
		{
			if (SettingsModel.ShortBreakDuration != value)
			{
				SettingsModel.ShortBreakDuration = value;
				TickFocusManager.OnPomoSettingsChanged();
				_isChanged = true;
			}
		}
	}

	public int LongBreakDuration
	{
		get
		{
			if (SettingsModel.LongBreakDuration > 0)
			{
				return SettingsModel.LongBreakDuration;
			}
			return 10;
		}
		set
		{
			if (SettingsModel.LongBreakDuration != value)
			{
				SettingsModel.LongBreakDuration = value;
				TickFocusManager.OnPomoSettingsChanged();
				_isChanged = true;
			}
		}
	}

	public int LongBreakEvery
	{
		get
		{
			return SettingsModel.LongBreakEvery;
		}
		set
		{
			SettingsModel.LongBreakEvery = value;
			OnPropertyChanged("LongBreakEvery");
		}
	}

	public bool AutoNextPomo
	{
		get
		{
			return PomoLocalSetting.AutoNextPomo;
		}
		set
		{
			PomoLocalSetting.AutoNextPomo = value;
			OnPropertyChanged("AutoNextPomo");
		}
	}

	public bool AutoBreak
	{
		get
		{
			return PomoLocalSetting.AutoBreak;
		}
		set
		{
			PomoLocalSetting.AutoBreak = value;
			OnPropertyChanged("AutoBreak");
		}
	}

	public bool ShowDailyPomo
	{
		get
		{
			return SettingsModel.ShowDailyPomo;
		}
		set
		{
			SettingsModel.ShowDailyPomo = value;
			OnPropertyChanged("ShowDailyPomo");
			GlobalEventManager.NotifyPomoDailyGoalChanged();
		}
	}

	public bool ShowDailyDuration
	{
		get
		{
			return SettingsModel.ShowDailyDuration;
		}
		set
		{
			SettingsModel.ShowDailyDuration = value;
			OnPropertyChanged("ShowDailyDuration");
			GlobalEventManager.NotifyPomoDailyGoalChanged();
		}
	}

	public string PomoShortcut
	{
		get
		{
			return SettingsModel.PomoShortcut;
		}
		set
		{
			SettingsModel.PomoShortcut = value;
			OnPropertyChanged("PomoShortcut");
		}
	}

	public string CreateStickyShortCut
	{
		get
		{
			return SettingsModel.CreateStickyShortCut;
		}
		set
		{
			SettingsModel.CreateStickyShortCut = value;
			OnPropertyChanged("CreateStickyShortCut");
		}
	}

	public string ShowHideStickyShortCut
	{
		get
		{
			return ExtraSettings.ShowHideStickyShortCut;
		}
		set
		{
			ExtraSettings.ShowHideStickyShortCut = value;
			OnPropertyChanged("ShowHideStickyShortCut");
		}
	}

	public int DailyPomoGoals
	{
		get
		{
			return SettingsModel.DailyPomoGoals;
		}
		set
		{
			SettingsModel.DailyPomoGoals = value;
			OnPropertyChanged("DailyPomoGoals");
			GlobalEventManager.NotifyPomoDailyGoalChanged();
		}
	}

	public int PomoWindowLeft
	{
		get
		{
			return PomoLocalSetting.PomoWindowLeft;
		}
		set
		{
			PomoLocalSetting.PomoWindowLeft = value;
			OnPropertyChanged("PomoWindowLeft");
		}
	}

	public int PomoWindowTop
	{
		get
		{
			return PomoLocalSetting.PomoWindowTop;
		}
		set
		{
			PomoLocalSetting.PomoWindowTop = value;
			OnPropertyChanged("PomoWindowTop");
		}
	}

	public double PomoWindowDpiScale
	{
		get
		{
			return PomoLocalSetting.PomoWindowDpiScale;
		}
		set
		{
			PomoLocalSetting.PomoWindowDpiScale = value;
			OnPropertyChanged("PomoWindowDpiScale");
		}
	}

	public bool PomoTopMost
	{
		get
		{
			return PomoLocalSetting.PomoTopMost;
		}
		set
		{
			PomoLocalSetting.PomoTopMost = value;
			OnPropertyChanged("PomoTopMost");
		}
	}

	public bool PomoExpand
	{
		get
		{
			return PomoLocalSetting.PomoExpand;
		}
		set
		{
			PomoLocalSetting.PomoExpand = value;
			OnPropertyChanged("PomoExpand");
		}
	}

	public double ProjectPanelWidth
	{
		get
		{
			return SettingsModel.ProjectPanelWidth;
		}
		set
		{
			SettingsModel.ProjectPanelWidth = value;
			OnPropertyChanged("ProjectPanelWidth");
		}
	}

	public double DetailListDivide
	{
		get
		{
			return SettingsModel.DetailListDivide;
		}
		set
		{
			SettingsModel.DetailListDivide = value;
			OnPropertyChanged("DetailListDivide");
		}
	}

	public int SortOrderOfAll
	{
		get
		{
			return SettingsModel.SortOrderOfAll;
		}
		set
		{
			SettingsModel.SortOrderOfAll = value;
		}
	}

	public int SortOrderOfToday
	{
		get
		{
			return SettingsModel.SortOrderOfToday;
		}
		set
		{
			SettingsModel.SortOrderOfToday = value;
		}
	}

	public int SortOrderOfTomorrow
	{
		get
		{
			return SettingsModel.SortOrderOfTomorrow;
		}
		set
		{
			SettingsModel.SortOrderOfTomorrow = value;
		}
	}

	public int SortOrderOfWeek
	{
		get
		{
			return SettingsModel.SortOrderOfWeek;
		}
		set
		{
			SettingsModel.SortOrderOfWeek = value;
		}
	}

	public int SortOrderOfCalendar
	{
		get
		{
			return SettingsModel.SortOrderOfCalendar;
		}
		set
		{
			SettingsModel.SortOrderOfCalendar = value;
		}
	}

	public int SortOrderOfTag
	{
		get
		{
			return SettingsModel.SortOrderOfTag;
		}
		set
		{
			SettingsModel.SortOrderOfTag = value;
		}
	}

	public int SortOrderOfEvent
	{
		get
		{
			return SettingsModel.SortOrderOfEvent;
		}
		set
		{
			SettingsModel.SortOrderOfEvent = value;
		}
	}

	public int SortOrderOfInbox
	{
		get
		{
			return SettingsModel.SortOrderOfInbox;
		}
		set
		{
			SettingsModel.SortOrderOfInbox = value;
		}
	}

	public int SortOrderOfSummary
	{
		get
		{
			return ExtraSettings.SortOrderOfSummary;
		}
		set
		{
			ExtraSettings.SortOrderOfSummary = value;
		}
	}

	public int SortOrderOfAssign
	{
		get
		{
			return SettingsModel.SortOrderOfAssign;
		}
		set
		{
			SettingsModel.SortOrderOfAssign = value;
			OnPropertyChanged("SortOrderOfAssign");
		}
	}

	public bool ShowReminder
	{
		get
		{
			return SettingsModel.ShowReminder;
		}
		set
		{
			SettingsModel.ShowReminder = value;
			OnPropertyChanged("ShowReminder");
		}
	}

	public bool ShowReminderInClient
	{
		get
		{
			return SettingsModel.ShowReminderInClient;
		}
		set
		{
			SettingsModel.ShowReminderInClient = value;
			OnPropertyChanged("ShowReminderInClient");
		}
	}

	public string AutoTagUserIds
	{
		get
		{
			return SettingsModel.AutoTagUserIds;
		}
		set
		{
			SettingsModel.AutoTagUserIds = value;
			OnPropertyChanged("AutoTagUserIds");
		}
	}

	public string SelectProjectId
	{
		get
		{
			return SettingsModel.SelectProjectId;
		}
		set
		{
			SettingsModel.SelectProjectId = value;
			OnPropertyChanged("SelectProjectId");
		}
	}

	public int ProxyType
	{
		get
		{
			return Common.ProxyType;
		}
		set
		{
			Common.ProxyType = value;
			_isChanged = true;
		}
	}

	public string ProxyAddress
	{
		get
		{
			return Common.ProxyAddress;
		}
		set
		{
			Common.ProxyAddress = value;
			_isChanged = true;
		}
	}

	public string ProxyPort
	{
		get
		{
			return Common.ProxyPort;
		}
		set
		{
			Common.ProxyPort = value;
			_isChanged = true;
		}
	}

	public string ProxyUsername
	{
		get
		{
			return Common.ProxyUsername;
		}
		set
		{
			Common.ProxyUsername = value;
			_isChanged = true;
		}
	}

	public string ProxyPassword
	{
		get
		{
			return Common.ProxyPassword;
		}
		set
		{
			Common.ProxyPassword = value;
			_isChanged = true;
		}
	}

	public string ProxyDomain
	{
		get
		{
			return Common.ProxyDomain;
		}
		set
		{
			Common.ProxyDomain = value;
			_isChanged = true;
		}
	}

	public bool NeedResetPassword
	{
		get
		{
			return SettingsModel.NeedResetPassword;
		}
		set
		{
			SettingsModel.NeedResetPassword = value;
			OnPropertyChanged("NeedResetPassword");
		}
	}

	public string LockShortcut
	{
		get
		{
			return SettingsModel.LockShortcut;
		}
		set
		{
			SettingsModel.LockShortcut = value;
			OnPropertyChanged("LockShortcut");
		}
	}

	public string AccountType
	{
		get
		{
			return Common.AccountType;
		}
		set
		{
			Common.AccountType = value;
		}
	}

	public string NeedResetUserId
	{
		get
		{
			return SettingsModel.NeedResetUserId;
		}
		set
		{
			SettingsModel.NeedResetUserId = value;
			OnPropertyChanged("NeedResetUserId");
		}
	}

	public bool NeedShowTutorial
	{
		get
		{
			return SettingsModel.NeedShowTutorial;
		}
		set
		{
			SettingsModel.NeedShowTutorial = value;
			OnPropertyChanged("NeedShowTutorial");
		}
	}

	public bool Maxmized
	{
		get
		{
			return SettingsModel.Maxmized;
		}
		set
		{
			SettingsModel.Maxmized = value;
			OnPropertyChanged("Maxmized");
		}
	}

	public string SkipVersion
	{
		get
		{
			return Common.SkipVersion;
		}
		set
		{
			Common.SkipVersion = value;
			OnPropertyChanged("SkipVersion");
		}
	}

	public double CheckRemindDate
	{
		get
		{
			return SettingsModel.CheckRemindDate;
		}
		set
		{
			SettingsModel.CheckRemindDate = value;
			OnPropertyChanged("CheckRemindDate");
		}
	}

	public string ShortcutPin
	{
		get
		{
			return SettingsModel.ShortcutPin;
		}
		set
		{
			SettingsModel.ShortcutPin = value;
			OnPropertyChanged("ShortcutPin");
		}
	}

	public bool ShowTimelineTooltip
	{
		get
		{
			return SettingsModel.ShowTimelineTooltip;
		}
		set
		{
			SettingsModel.ShowTimelineTooltip = value;
			OnPropertyChanged("ShowTimelineTooltip");
		}
	}

	public string LastCheckSyncDate
	{
		get
		{
			return SettingsModel.LastCheckSyncDate;
		}
		set
		{
			SettingsModel.LastCheckSyncDate = value;
			OnPropertyChanged("LastCheckSyncDate");
		}
	}

	public string RecentProjects
	{
		get
		{
			return SettingsModel.RecentProjects;
		}
		set
		{
			SettingsModel.RecentProjects = value;
			OnPropertyChanged("RecentProjects");
		}
	}

	public int CollapsedStart
	{
		get
		{
			if (!UserDao.IsPro())
			{
				return 7;
			}
			return Math.Min(SettingsModel.CollapsedStart, 24);
		}
		set
		{
			SettingsModel.CollapsedStart = value;
			OnPropertyChanged("CollapsedStart");
		}
	}

	public int CollapsedEnd
	{
		get
		{
			if (!UserDao.IsPro())
			{
				return 21;
			}
			return Math.Min(SettingsModel.CollapsedEnd, 24);
		}
		set
		{
			SettingsModel.CollapsedEnd = value;
			OnPropertyChanged("CollapsedEnd");
		}
	}

	public bool ShowDetails
	{
		get
		{
			if (!SettingsModel.ShowDetails)
			{
				return _inSearch;
			}
			return true;
		}
		set
		{
			SettingsModel.ShowDetails = value;
			OnPropertyChanged("ShowDetails");
		}
	}

	public string AllDayCustomRemind
	{
		get
		{
			return SettingsModel.AllDayCustomRemind;
		}
		set
		{
			SettingsModel.AllDayCustomRemind = value;
			OnPropertyChanged("AllDayCustomRemind");
		}
	}

	public string TimeCustomRemind
	{
		get
		{
			return SettingsModel.TimeCustomRemind;
		}
		set
		{
			SettingsModel.TimeCustomRemind = value;
			OnPropertyChanged("TimeCustomRemind");
		}
	}

	public bool ExpandPersonalSection
	{
		get
		{
			return SettingsModel.ExpandPersonalSection;
		}
		set
		{
			SettingsModel.ExpandPersonalSection = value;
			OnPropertyChanged("ExpandPersonalSection");
		}
	}

	public int SmartListSummary
	{
		get
		{
			if (SettingsModel.SmartListSummary < 0)
			{
				SettingsModel.SmartListSummary = 0;
			}
			return SettingsModel.SmartListSummary;
		}
		set
		{
			SettingsModel.SmartListSummary = value;
			OnPropertyChanged("SmartListSummary");
		}
	}

	public string CompletionSound
	{
		get
		{
			return SettingsModel.CompletionSound;
		}
		set
		{
			SettingsModel.CompletionSound = value;
			OnPropertyChanged("CompletionSound");
		}
	}

	public bool UpdateHabit
	{
		get
		{
			return SettingsModel.UpdateHabit;
		}
		set
		{
			SettingsModel.UpdateHabit = value;
			OnPropertyChanged("UpdateCalendar");
		}
	}

	public bool EnableRingtone => SettingsModel.EnableRingtone;

	public double WindowTop
	{
		get
		{
			return SettingsModel.WindowTop;
		}
		set
		{
			SettingsModel.WindowTop = value;
			OnPropertyChanged("WindowTop");
		}
	}

	public double WindowLeft
	{
		get
		{
			return SettingsModel.WindowLeft;
		}
		set
		{
			SettingsModel.WindowLeft = value;
			OnPropertyChanged("WindowLeft");
		}
	}

	public bool IsDarkTheme => ThemeKey.IsDarkTheme(ThemeId);

	public string ThemeId
	{
		get
		{
			return ThemeUtil.GetThemeKey(SettingsModel.ThemeId);
		}
		set
		{
			SettingsModel.ThemeId = value;
			OnPropertyChanged("ThemeId");
		}
	}

	public string LastProRemindeTime
	{
		get
		{
			return SettingsModel.LastProRemindeTime;
		}
		set
		{
			SettingsModel.LastProRemindeTime = value;
			OnPropertyChanged("LastProRemindeTime");
		}
	}

	public double CalendarMinMinute
	{
		get
		{
			return SettingsModel.CalendarMinMinute;
		}
		set
		{
			SettingsModel.CalendarMinMinute = Math.Round(value);
			OnPropertyChanged("CalendarMinMinute");
		}
	}

	public double CalendarHourHeight
	{
		get
		{
			return GetValidHeight(SettingsModel.CalendarHourHeight);
		}
		set
		{
			SettingsModel.CalendarHourHeight = Math.Round(value);
			OnPropertyChanged("CalendarHourHeight");
		}
	}

	public int ArrangeDisplayType
	{
		get
		{
			return SettingsModel.ArrangeDisplayType;
		}
		set
		{
			SettingsModel.ArrangeDisplayType = value;
			GlobalEventManager.NotifyArrangeDisplayTypeChanged();
		}
	}

	public int ArrangeTaskDateType
	{
		get
		{
			return SettingsModel.ArrangeTaskDateType;
		}
		set
		{
			SettingsModel.ArrangeTaskDateType = value;
			OnPropertyChanged("ArrangeTaskDateType");
		}
	}

	public double WeekAllDayHeight
	{
		get
		{
			return SettingsModel.WeekAllDayHeight;
		}
		set
		{
			SettingsModel.WeekAllDayHeight = value;
			OnPropertyChanged("WeekAllDayHeight");
		}
	}

	public bool ShowArrangeReminder
	{
		get
		{
			return SettingsModel.ShowArrangeReminder;
		}
		set
		{
			SettingsModel.ShowArrangeReminder = value;
			OnPropertyChanged("ShowArrangeReminder");
		}
	}

	public string PomoType
	{
		get
		{
			if (!string.IsNullOrEmpty(PomoLocalSetting.PomoType))
			{
				return PomoLocalSetting.PomoType;
			}
			return FocusConstance.Focus;
		}
		set
		{
			PomoLocalSetting.PomoType = value ?? FocusConstance.Focus;
			OnPropertyChanged("PomoType");
		}
	}

	public long UpgradeCheckPoint
	{
		get
		{
			return SettingsModel.UpgradeCheckPoint;
		}
		set
		{
			SettingsModel.UpgradeCheckPoint = value;
			_isChanged = true;
		}
	}

	public int DailyFocusDuration
	{
		get
		{
			return SettingsModel.DailyFocusDuration;
		}
		set
		{
			SettingsModel.DailyFocusDuration = value;
			GlobalEventManager.NotifyPomoDailyGoalChanged();
		}
	}

	public bool HabitInToday
	{
		get
		{
			return SettingsModel.HabitInToday;
		}
		set
		{
			SettingsModel.HabitInToday = value;
			DataChangedNotifier.NotifyHabitsChanged();
			OnPropertyChanged("HabitInToday");
		}
	}

	public bool HabitInCal
	{
		get
		{
			return SettingsModel.HabitInCal;
		}
		set
		{
			SettingsModel.HabitInCal = value;
			OnPropertyChanged("HabitInCal");
		}
	}

	public long HabitDefaultOrder
	{
		get
		{
			return SettingsModel.HabitDefaultOrder;
		}
		set
		{
			SettingsModel.HabitDefaultOrder = value;
			OnPropertyChanged("HabitDefaultOrder");
		}
	}

	public bool ShowHabit => TabBarModel.IsActive("HABIT", DesktopConfig?.bars);

	public bool EnableFocus => TabBarModel.IsActive("POMO", DesktopConfig?.bars);

	public bool EnableCalendar => TabBarModel.IsActive("CALENDAR", DesktopConfig?.bars);

	public bool EnableCountDownDays => TabBarModel.IsActive("COUNTDOWN", DesktopConfig?.bars);

	public bool EnableAI => TabBarModel.IsActive("AI_ASSISTANT", DesktopConfig?.bars);

	public bool ShowCountDown
	{
		get
		{
			return SettingsModel.ShowCountDown;
		}
		set
		{
			SettingsModel.ShowCountDown = value;
			OnPropertyChanged("ShowCountDown");
		}
	}

	public bool DateParsing
	{
		get
		{
			return SettingsModel.DateParsing;
		}
		set
		{
			SettingsModel.DateParsing = value;
			OnPropertyChanged("DateParsing");
		}
	}

	public bool RemoveTimeText
	{
		get
		{
			return SettingsModel.RemoveTimeText;
		}
		set
		{
			SettingsModel.RemoveTimeText = value;
			OnPropertyChanged("RemoveTimeText");
		}
	}

	public bool KeepTagsInText
	{
		get
		{
			return SettingsModel.KeepTagsInText;
		}
		set
		{
			SettingsModel.KeepTagsInText = value;
			OnPropertyChanged("KeepTagsInText");
		}
	}

	public bool HideComplete
	{
		get
		{
			return SettingsModel.HideComplete;
		}
		set
		{
			bool hideComplete = Settings.HideComplete;
			SettingsModel.HideComplete = value;
			OnPropertyChanged("HideComplete");
			if (hideComplete != value)
			{
				DataChangedNotifier.NotifyHideCompleteChanged();
			}
		}
	}

	public bool ShowRepeatCircles
	{
		get
		{
			return SettingsModel.ShowRepeatCircles;
		}
		set
		{
			SettingsModel.ShowRepeatCircles = value;
			OnPropertyChanged("ShowRepeatCircles");
		}
	}

	public bool ShowFocusRecord
	{
		get
		{
			return SettingsModel.ShowFocusRecord;
		}
		set
		{
			SettingsModel.ShowFocusRecord = value;
			OnPropertyChanged("ShowFocusRecord");
		}
	}

	public bool ShowSubtasks
	{
		get
		{
			return SettingsModel.ShowSubtasks;
		}
		set
		{
			SettingsModel.ShowSubtasks = value;
			OnPropertyChanged("ShowSubtasks");
		}
	}

	public string WeekStartFrom
	{
		get
		{
			return SettingsModel.WeekStartFrom;
		}
		set
		{
			if (SettingsModel.WeekStartFrom != value)
			{
				SettingsModel.WeekStartFrom = value;
				OnPropertyChanged("WeekStartFrom");
				DataChangedNotifier.NotifyWeekStartFromChanged();
			}
		}
	}

	public DayOfWeek WeekStartFromType
	{
		get
		{
			if (Enum.TryParse<DayOfWeek>(Settings.WeekStartFrom, ignoreCase: true, out var result))
			{
				return result;
			}
			return DayOfWeek.Sunday;
		}
		set
		{
			WeekStartFrom = value.ToString();
		}
	}

	public string TimeFormat
	{
		get
		{
			if (!(SettingsModel.TimeFormat == "System"))
			{
				return SettingsModel.TimeFormat;
			}
			return ticktick_WPF.Util.DateUtils.GetSystemTimeFormat();
		}
		set
		{
			SettingsModel.TimeFormat = value;
			GlobalEventManager.NotifyTimeFormatChanged();
			OnPropertyChanged("TimeFormat");
		}
	}

	public int PosOfOverdue
	{
		get
		{
			return SettingsModel.PosOfOverdue;
		}
		set
		{
			SettingsModel.PosOfOverdue = value;
			OnPropertyChanged("PosOfOverdue");
		}
	}

	public string NotificationOptions
	{
		get
		{
			return SettingsModel.NotificationOptions;
		}
		set
		{
			SettingsModel.NotificationOptions = value;
			OnPropertyChanged("NotificationOptions");
		}
	}

	public bool EnableHoliday
	{
		get
		{
			return SettingsModel.EnableHoliday;
		}
		set
		{
			SettingsModel.EnableHoliday = value;
			OnPropertyChanged("EnableHoliday");
		}
	}

	public bool EnableLunar
	{
		get
		{
			return SettingsModel.EnableLunar;
		}
		set
		{
			SettingsModel.EnableLunar = value;
			OnPropertyChanged("EnableLunar");
		}
	}

	public bool MiniCalendarEnabled
	{
		get
		{
			return ExtraSettings.MiniCalendarEnabled;
		}
		set
		{
			ExtraSettings.MiniCalendarEnabled = value;
			OnPropertyChanged("MiniCalendarEnabled");
		}
	}

	public bool ShowWeek
	{
		get
		{
			return SettingsModel.ShowWeek;
		}
		set
		{
			SettingsModel.ShowWeek = value;
			OnPropertyChanged("ShowWeek");
		}
	}

	public bool ShowCheckListInCal
	{
		get
		{
			return SettingsModel.ShowCheckListInCal;
		}
		set
		{
			SettingsModel.ShowCheckListInCal = value;
			OnPropertyChanged("ShowCheckListInCal");
		}
	}

	public bool ShowCompletedInCal
	{
		get
		{
			return SettingsModel.ShowCompletedInCal;
		}
		set
		{
			SettingsModel.ShowCompletedInCal = value;
			OnPropertyChanged("ShowCompletedInCal");
		}
	}

	public string CellColorType
	{
		get
		{
			return SettingsModel.CellColorType ?? "list";
		}
		set
		{
			SettingsModel.CellColorType = value ?? "list";
			OnPropertyChanged("CellColorType");
		}
	}

	public bool EnableTimeZone
	{
		get
		{
			return SettingsModel.EnableTimeZone;
		}
		set
		{
			SettingsModel.EnableTimeZone = value;
			OnPropertyChanged("EnableTimeZone");
		}
	}

	public bool InSearch
	{
		get
		{
			return _inSearch;
		}
		set
		{
			_inSearch = value;
		}
	}

	public int SmartListHabit => (!Settings.ShowHabit) ? 1 : 0;

	public bool IsNoteEnabled
	{
		get
		{
			return SettingsModel.IsNoteEnabled;
		}
		set
		{
			SettingsModel.IsNoteEnabled = value;
			_isChanged = true;
		}
	}

	public string StartWeekOfYear
	{
		get
		{
			return SettingsModel.StartWeekOfYear;
		}
		set
		{
			SettingsModel.StartWeekOfYear = value;
			OnPropertyChanged("StartWeekOfYear");
		}
	}

	public bool ProjectTypeChangeNotified
	{
		get
		{
			return SettingsModel.ProjectTypeChangeNotified;
		}
		set
		{
			SettingsModel.ProjectTypeChangeNotified = value;
			OnPropertyChanged("ProjectTypeChangeNotified");
		}
	}

	public bool DontShowProWindow
	{
		get
		{
			return SettingsModel.DontShowProWindow;
		}
		set
		{
			SettingsModel.DontShowProWindow = value;
			_isChanged = true;
		}
	}

	public bool SpellCheckEnable
	{
		get
		{
			return SettingsModel.SpellCheckEnable;
		}
		set
		{
			SettingsModel.SpellCheckEnable = value;
			LocalSettings.SpellCheckChanged?.Invoke(this, null);
			_isChanged = true;
		}
	}

	public string SpellCheckLanguage
	{
		get
		{
			return SettingsModel.SpellCheckLanguage;
		}
		set
		{
			SettingsModel.SpellCheckLanguage = value;
			LocalSettings.SpellCheckChanged?.Invoke(this, null);
			_isChanged = true;
		}
	}

	public int AutoPomoTimes
	{
		get
		{
			return PomoLocalSetting.AutoPomoTimes;
		}
		set
		{
			PomoLocalSetting.AutoPomoTimes = value;
			OnPropertyChanged("PomoWindowTheme");
		}
	}

	public double PomoWindowOpacity
	{
		get
		{
			return PomoLocalSetting.PomoWindowOpacity;
		}
		set
		{
			PomoLocalSetting.PomoWindowOpacity = value;
			OnPropertyChanged("PomoWindowOpacity");
		}
	}

	public string PomoWindowTheme
	{
		get
		{
			return PomoLocalSetting.PomoWindowTheme;
		}
		set
		{
			PomoLocalSetting.PomoWindowTheme = value;
			GlobalEventManager.NotifyPomoThemeChanged(value);
			_isChanged = true;
		}
	}

	public bool EnableCountDown
	{
		get
		{
			return SettingsModel.EnableCountDown;
		}
		set
		{
			SettingsModel.EnableCountDown = value;
		}
	}

	public int MainWindowDisplayModule
	{
		get
		{
			return SettingsModel.MainWindowDisplayModule;
		}
		set
		{
			SettingsModel.MainWindowDisplayModule = value;
			OnPropertyChanged("MainWindowDisplayModule");
		}
	}

	public bool AllProjectOpened
	{
		get
		{
			return ProjectListOpenStatus.AllProjectOpened;
		}
		set
		{
			ProjectListOpenStatus.AllProjectOpened = value;
			_isChanged = true;
		}
	}

	public bool AllTagOpened
	{
		get
		{
			return ProjectListOpenStatus.AllTagOpened;
		}
		set
		{
			ProjectListOpenStatus.AllTagOpened = value;
			_isChanged = true;
		}
	}

	public bool AllShareTagOpened
	{
		get
		{
			return ProjectListOpenStatus.AllShareTagOpened;
		}
		set
		{
			ProjectListOpenStatus.AllShareTagOpened = value;
			_isChanged = true;
		}
	}

	public bool AllFilterOpened
	{
		get
		{
			return ProjectListOpenStatus.AllFilterOpened;
		}
		set
		{
			ProjectListOpenStatus.AllFilterOpened = value;
			_isChanged = true;
		}
	}

	public bool AllSubscribeOpened
	{
		get
		{
			return ProjectListOpenStatus.AllSubscribeOpened;
		}
		set
		{
			ProjectListOpenStatus.AllSubscribeOpened = value;
			_isChanged = true;
		}
	}

	public SummaryFilterModel SummaryFilter
	{
		get
		{
			if (_summaryFilter == null && !string.IsNullOrEmpty(Settings.SettingsModel.SummaryFilter))
			{
				try
				{
					_summaryFilter = JsonConvert.DeserializeObject<SummaryFilterModel>(Settings.SettingsModel.SummaryFilter) ?? new SummaryFilterModel();
				}
				catch (Exception)
				{
					_summaryFilter = new SummaryFilterModel();
				}
			}
			_summaryFilter = _summaryFilter ?? new SummaryFilterModel();
			return _summaryFilter;
		}
	}

	public List<SmartProjectConf> SmartProjects
	{
		get
		{
			if (_smartProjects == null)
			{
				return null;
			}
			return (from proj in _smartProjects
				group proj by proj.name into g
				select g.First()).ToList();
		}
		set
		{
			_smartProjects = value;
			ProjectDataProvider.ReSortPtfTypes();
			OnPropertyChanged("SmartProjects");
		}
	}

	public bool LightsOn
	{
		get
		{
			return PomoLocalSetting.LightsOn;
		}
		set
		{
			PomoLocalSetting.LightsOn = value;
		}
	}

	public bool TaskSetFolded
	{
		get
		{
			return ShortCutModel.TaskSetFolded != "0";
		}
		set
		{
			ShortCutModel.TaskSetFolded = (value ? "1" : "0");
			OnPropertyChanged("TaskSetFolded");
		}
	}

	public bool TaskOperateFolded
	{
		get
		{
			return ShortCutModel.TaskOperateFolded != "0";
		}
		set
		{
			ShortCutModel.TaskOperateFolded = (value ? "1" : "0");
			OnPropertyChanged("TaskOperateFolded");
		}
	}

	public bool GlobalOperateFolded
	{
		get
		{
			return ShortCutModel.GlobalOperateFolded != "0";
		}
		set
		{
			ShortCutModel.GlobalOperateFolded = (value ? "1" : "0");
			OnPropertyChanged("GlobalOperateFolded");
		}
	}

	public bool AppGlobalOperateFolded
	{
		get
		{
			return ShortCutModel.AppGlobalOperateFolded != "0";
		}
		set
		{
			ShortCutModel.AppGlobalOperateFolded = (value ? "1" : "0");
			OnPropertyChanged("AppGlobalOperateFolded");
		}
	}

	public bool SetTimeFolded
	{
		get
		{
			return ShortCutModel.SetTimeFolded != "0";
		}
		set
		{
			ShortCutModel.SetTimeFolded = (value ? "1" : "0");
			OnPropertyChanged("SetTimeFolded");
		}
	}

	public bool SetPriorityFolded
	{
		get
		{
			return ShortCutModel.SetPriorityFolded != "0";
		}
		set
		{
			ShortCutModel.SetPriorityFolded = (value ? "1" : "0");
			OnPropertyChanged("SetPriorityFolded");
		}
	}

	public bool QuickJumpFolded
	{
		get
		{
			return ShortCutModel.QuickJumpFolded != "0";
		}
		set
		{
			ShortCutModel.QuickJumpFolded = (value ? "1" : "0");
			OnPropertyChanged("QuickJumpFolded");
		}
	}

	public string RecentUsedEmojis
	{
		get
		{
			return SettingsModel.RecentUsedEmojis;
		}
		set
		{
			SettingsModel.RecentUsedEmojis = value;
		}
	}

	public long DomainInvalidTime
	{
		get
		{
			return Common.DomainInvalidTime;
		}
		set
		{
			Common.DomainInvalidTime = value;
		}
	}

	public bool ProjectPinFolded
	{
		get
		{
			return _projectPinFolded;
		}
		set
		{
			_projectPinFolded = value;
			OnPropertyChanged("ProjectPinFolded");
		}
	}

	public long FocusPushPoint
	{
		get
		{
			return ExtraSettings.FocusPushPoint;
		}
		set
		{
			ExtraSettings.FocusPushPoint = value;
			OnPropertyChanged("FocusPushPoint");
		}
	}

	public int HabitItemDisplayMode
	{
		get
		{
			return (UserPreference?.HabitConfig?.style == "card") ? 1 : 0;
		}
		set
		{
			SetHabitConfigStyle((value == 0) ? "list" : "card");
			OnPropertyChanged("HabitItemDisplayMode");
		}
	}

	public int BaseFontSize
	{
		get
		{
			return Common.BaseFontSize;
		}
		set
		{
			Common.BaseFontSize = value;
		}
	}

	public string CheckedNewFeature
	{
		get
		{
			return SettingsModel.CheckedNewFeature ?? string.Empty;
		}
		set
		{
			SettingsModel.CheckedNewFeature = value;
		}
	}

	public bool ShowCalWeekend
	{
		get
		{
			if (UserDao.IsPro())
			{
				return ExtraSettings.ShowCalWeekend;
			}
			return true;
		}
		set
		{
			ExtraSettings.ShowCalWeekend = value;
			OnPropertyChanged("ShowCalWeekend");
		}
	}

	public bool ShowHeatMap
	{
		get
		{
			return UserPreference?.CalendarConfig?.Heatmap ?? true;
		}
		set
		{
			if (UserPreference.CalendarConfig == null)
			{
				UserPreference.CalendarConfig = new CalendarConfig();
			}
			UserPreference.CalendarConfig.Heatmap = value;
		}
	}

	public CalTimelineTimeZones CalendarDisplayTimeZones => UserPreference.timelineTimeZones;

	public ExtraSettings ExtraSettings
	{
		get
		{
			if (_extraSettings == null && !string.IsNullOrEmpty(Settings.SettingsModel.ExtraSettings))
			{
				try
				{
					_extraSettings = JsonConvert.DeserializeObject<ExtraSettings>(Settings.SettingsModel.ExtraSettings);
				}
				catch (Exception)
				{
					_extraSettings = new ExtraSettings();
				}
			}
			_extraSettings = _extraSettings ?? new ExtraSettings();
			return _extraSettings;
		}
		set
		{
			_extraSettings = value;
		}
	}

	public long LastPullYearPromoTime
	{
		get
		{
			return ExtraSettings?.LastPullYearPromoTime ?? 0;
		}
		set
		{
			if (ExtraSettings != null)
			{
				ExtraSettings.LastPullYearPromoTime = value;
			}
		}
	}

	public FontFamily FontFamily
	{
		get
		{
			return _fontFamily;
		}
		set
		{
			_fontFamily = value;
			OnPropertyChanged("FontFamily");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public static event EventHandler SpellCheckChanged;

	public void SetRemotePreference(UserPreferenceModel remote)
	{
		if (remote == null || remote.mtime == 0L)
		{
			if (remote != null && remote.mtime == 0L && DesktopConfig == null)
			{
				InitDesktopTabBars(TabBarModel.InitTabBars());
				SettingsHelper.PushLocalPreference();
			}
			return;
		}
		bool flag = false;
		if (remote.mtime > UserPreference.mtime)
		{
			UserPreference.mtime = remote.mtime;
		}
		SettingsModel.PreferrenceMTime = remote.mtime;
		if (remote.desktopTabBars == null)
		{
			if (DesktopConfig == null)
			{
				InitDesktopTabBars(TabBarModel.InitTabBars());
			}
			UserPreference.mtime = Utils.GetNowTimeStampInMills();
			flag = true;
		}
		long? num = DesktopConfig?.mtime;
		PropertyInfo[] properties = typeof(UserPreferenceModel).GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetValue(UserPreference) is PreferenceBaseModel preferenceBaseModel)
			{
				bool flag2 = preferenceBaseModel.SetRemoteValue(propertyInfo.GetValue(remote) as PreferenceBaseModel);
				flag = flag || flag2;
			}
			else
			{
				propertyInfo.SetValue(UserPreference, propertyInfo.GetValue(remote));
			}
		}
		_isChanged = true;
		Save();
		if (flag)
		{
			SettingsHelper.PushLocalPreference();
		}
		if (num != UserPreference.desktopTabBars?.mtime)
		{
			MainWindowManager.CheckSelectedModule();
		}
		OnPreferenceChanged?.Invoke();
		WebSocketService.CheckFocusSocket();
		OnPropertyChanged("HabitItemDisplayMode");
	}

	public void ShowOrHideTabBar(string name, bool show)
	{
		if (UserPreference.desktopTabBars == null)
		{
			UserPreference.desktopTabBars = new DesktopTabBar
			{
				bars = TabBarModel.InitTabBars()
			};
		}
		if (UserPreference.desktopTabBars.bars == null)
		{
			UserPreference.desktopTabBars.bars = TabBarModel.InitTabBars();
		}
		List<TabBarModel> list = DesktopConfig.bars ?? TabBarModel.InitTabBars();
		TabBarModel bar = UserPreference.desktopTabBars.bars?.FirstOrDefault((TabBarModel b) => b.name.ToLower() == name.ToLower());
		if (bar != null)
		{
			bar.status = (show ? "active" : "inactive");
			if (show && list.Count((TabBarModel b) => b.sortOrder == bar.sortOrder) > 1)
			{
				List<TabBarModel> list2 = list.Where((TabBarModel b) => b.status == "active" && b.name != bar.name).ToList();
				list2.Sort((TabBarModel a, TabBarModel b) => a.sortOrder.CompareTo(b.sortOrder));
				for (int num = 0; num < list2.Count; num++)
				{
					list2[num].sortOrder = num;
				}
				bar.sortOrder = list2.Count;
			}
		}
		else
		{
			long sortOrder = list.Max((TabBarModel b) => b.sortOrder) + 1;
			if (string.Equals(name, "AI_ASSISTANT", StringComparison.OrdinalIgnoreCase))
			{
				TabBarModel tabBarModel = list.FirstOrDefault((TabBarModel b) => string.Equals(b.name, "SEARCH", StringComparison.OrdinalIgnoreCase));
				if (tabBarModel != null)
				{
					sortOrder = tabBarModel.sortOrder;
					foreach (TabBarModel item in list.Where((TabBarModel b) => b.sortOrder >= sortOrder))
					{
						item.sortOrder++;
					}
				}
			}
			UserPreference.desktopTabBars.bars.Add(new TabBarModel
			{
				name = name.ToUpper(),
				sortOrder = sortOrder,
				status = (show ? "active" : "inactive")
			});
		}
		long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
		UserPreference.mtime = nowTimeStampInMills;
		DesktopConfig.mtime = nowTimeStampInMills;
		DesktopConfig.bars = list;
		_isChanged = true;
		MainWindowManager.CheckSelectedModule();
		SettingsHelper.PushLocalPreference();
	}

	public async Task SaveMatrixShowCompleted(bool show)
	{
		if (UserPreference != null && !string.IsNullOrEmpty(LoginUserId))
		{
			PendingMatrixShowCompleted = show;
			await Save();
			SyncManager.TryDelaySync();
		}
	}

	public async Task AcknowledgeMatrixShowCompleted(string userId, bool submitted)
	{
		if (!(LoginUserId != userId) && PendingMatrixShowCompleted == submitted)
		{
			PendingMatrixShowCompleted = null;
			await Save();
		}
	}

	public void SaveMatrixQuadrant(int level, string rule, SortOption sort, string name)
	{
		if (MatrixModel == null)
		{
			UserPreference.matrix = MatrixModel.GetDefault();
		}
		UtilLog.Info($"SaveMatrixQuadrant {level} {rule}");
		MatrixProfileDao.UpdateCurrentQuadrantSync(level, rule, sort, name);
		OnMatrixQuadrantChanged();
	}

	public void OnMatrixQuadrantChanged()
	{
		SyncManager.TryDelaySync();
	}

	public void SetPreference()
	{
		string userPreference = Settings.SettingsModel.UserPreference;
		if (!string.IsNullOrEmpty(userPreference))
		{
			try
			{
				UserPreference = JsonConvert.DeserializeObject<UserPreferenceModel>(userPreference);
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
			}
		}
		UserPreference = UserPreference ?? new UserPreferenceModel();
		if (UserPreference.matrix == null || UserPreference.matrix.IsEmpty())
		{
			UserPreference.matrix = MatrixModel.GetDefault();
		}
		if (UserPreference.Timeline == null || string.IsNullOrEmpty(UserPreference.Timeline.Color))
		{
			UserPreference.Timeline = new TimelineSettingsModel();
		}
		if (UserPreference.desktop_conf == null)
		{
			UserPreference.desktop_conf = new DesktopConfigModel();
		}
	}

	public void SavePreference()
	{
		SettingsModel.UserPreference = JsonConvert.SerializeObject(UserPreference);
	}

	public void SetAutoAcceptShare(bool auto)
	{
		if (UserPreference.TeamConfig == null)
		{
			UserPreference.TeamConfig = new TeamConfig();
		}
		long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
		UserPreference.TeamConfig.autoAcceptInvite = auto;
		UserPreference.TeamConfig.mtime = nowTimeStampInMills;
		UserPreference.mtime = nowTimeStampInMills;
		_isChanged = true;
	}

	public void SetTimelineShowGuide()
	{
		if (UserPreference.Timeline == null)
		{
			UserPreference.Timeline = new TimelineSettingsModel();
		}
		long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
		UserPreference.Timeline.ShowGuide = false;
		UserPreference.Timeline.mtime = nowTimeStampInMills;
		UserPreference.mtime = nowTimeStampInMills;
		_isChanged = true;
	}

	public void SetUrlParseEnable(bool enable)
	{
		if (UserPreference.GeneralConfig == null)
		{
			UserPreference.GeneralConfig = new GeneralConfig();
		}
		long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
		UserPreference.GeneralConfig.urlParseEnabled = enable;
		UserPreference.GeneralConfig.mtime = nowTimeStampInMills;
		UserPreference.mtime = nowTimeStampInMills;
		_isChanged = true;
	}

	public void SetEmailRemindItems(List<string> items)
	{
		if (UserPreference.GeneralConfig == null)
		{
			UserPreference.GeneralConfig = new GeneralConfig();
		}
		long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
		UserPreference.GeneralConfig.mtime = nowTimeStampInMills;
		UserPreference.GeneralConfig.emailRemindItems = items;
		UserPreference.mtime = nowTimeStampInMills;
		_isChanged = true;
	}

	public void SetEmailReminder(bool enable)
	{
		if (UserPreference != null)
		{
			if (UserPreference.GeneralConfig == null)
			{
				UserPreference.GeneralConfig = new GeneralConfig();
			}
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.GeneralConfig.emailRemindEnabled = enable;
			UserPreference.GeneralConfig.mtime = nowTimeStampInMills;
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
		}
	}

	public void InitDesktopTabBars(List<TabBarModel> bars)
	{
		if (UserPreference != null)
		{
			if (UserPreference.desktopTabBars == null)
			{
				UserPreference.desktopTabBars = new DesktopTabBar();
			}
			UserPreference.desktopTabBars.bars = bars;
			UserPreference.desktopTabBars.mtime = 1L;
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
		}
	}

	public void SaveTabBarOrder(List<(string, long)> idToOrders)
	{
		if (UserPreference.desktopTabBars == null)
		{
			UserPreference.desktopTabBars = new DesktopTabBar();
		}
		if (UserPreference.desktopTabBars.bars == null || UserPreference.desktopTabBars.bars.Count == 0)
		{
			UserPreference.desktopTabBars.bars = TabBarModel.InitTabBars();
		}
		foreach (var idToOrder in idToOrders)
		{
			string id = idToOrder.Item1;
			long item = idToOrder.Item2;
			TabBarModel tabBarModel = UserPreference.desktopTabBars.bars.FirstOrDefault((TabBarModel b) => b.name.ToLower() == id.ToLower());
			if (tabBarModel != null)
			{
				tabBarModel.sortOrder = item;
				tabBarModel.status = "active";
				continue;
			}
			UserPreference.desktopTabBars.bars.Add(new TabBarModel
			{
				name = id.ToUpper(),
				sortOrder = item,
				status = "active"
			});
		}
		long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
		UserPreference.desktopTabBars.mtime = nowTimeStampInMills;
		UserPreference.mtime = nowTimeStampInMills;
		_isChanged = true;
	}

	public void InitSmartProjectsOption(bool push = true)
	{
		if (UserPreference.SmartProjectsOption == null)
		{
			UserPreference.SmartProjectsOption = new SmartProjectsOption
			{
				SmartProjects = SmartProjectService.InitSmartProjectOptions(),
				mtime = 1L
			};
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
		}
		if (push)
		{
			UserPreference.SmartProjectsOption.mtime = Utils.GetNowTimeStampInMills();
			SettingsHelper.PushLocalPreference();
		}
	}

	public void SaveSmartProjectOptions(string name, SortOption option)
	{
		if (UserPreference?.SmartProjectsOption?.SmartProjects == null)
		{
			InitSmartProjectsOption(push: false);
		}
		if (UserPreference?.SmartProjectsOption?.SmartProjects != null)
		{
			SmartProjectOption smartProjectOption = UserPreference.SmartProjectsOption?.GetSmartProjectByName(name);
			if (smartProjectOption != null)
			{
				smartProjectOption.SortOption = option;
			}
			else
			{
				UserPreference.SmartProjectsOption.SmartProjects.Add(new SmartProjectOption
				{
					Name = name,
					ViewMode = "list",
					SortOption = option
				});
			}
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.SmartProjectsOption.mtime = nowTimeStampInMills;
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
			SettingsHelper.PushLocalPreference();
		}
	}

	public void InitMatrixSortOption()
	{
		UserPreference.matrix?.InitSortOption();
		_isChanged = true;
	}

	public void SaveSmartViewMode(string name, string viewMode)
	{
		if (UserPreference?.SmartProjectsOption?.SmartProjects == null)
		{
			InitSmartProjectsOption(push: false);
		}
		if (UserPreference?.SmartProjectsOption?.SmartProjects == null)
		{
			return;
		}
		SmartProjectOption smartProjectOption = UserPreference.SmartProjectsOption?.GetSmartProjectByName(name);
		if (smartProjectOption != null)
		{
			smartProjectOption.ViewMode = viewMode;
			if (viewMode == "kanban")
			{
				SortOption sortOption = smartProjectOption.SortOption;
				if (sortOption != null && sortOption.groupByType == Constants.SortType.none)
				{
					smartProjectOption.SortOption.groupBy = ((name == "inbox") ? "sortOrder" : "project");
				}
			}
		}
		else
		{
			UserPreference.SmartProjectsOption.SmartProjects.Add(new SmartProjectOption
			{
				Name = name,
				ViewMode = viewMode,
				SortOption = ((name == "inbox") ? new SortOption
				{
					orderBy = "sortOrder",
					groupBy = "sortOrder"
				} : new SortOption
				{
					orderBy = "dueDate",
					groupBy = "dueDate"
				})
			});
		}
		long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
		UserPreference.SmartProjectsOption.mtime = nowTimeStampInMills;
		UserPreference.mtime = nowTimeStampInMills;
		_isChanged = true;
		SettingsHelper.PushLocalPreference();
	}

	public void SaveSmartProjectTimeline(string name, TimelineModel timeline)
	{
		if (UserPreference?.SmartProjectsOption?.SmartProjects == null)
		{
			InitSmartProjectsOption(push: false);
		}
		if (UserPreference?.SmartProjectsOption?.SmartProjects != null)
		{
			SmartProjectOption smartProjectOption = UserPreference.SmartProjectsOption?.GetSmartProjectByName(name);
			if (smartProjectOption != null)
			{
				smartProjectOption.Timeline = timeline;
			}
			else
			{
				UserPreference.SmartProjectsOption.SmartProjects.Add(new SmartProjectOption
				{
					Name = name,
					Timeline = timeline,
					ViewMode = "list",
					SortOption = ((name == "inbox") ? new SortOption
					{
						orderBy = "sortOrder",
						groupBy = "sortOrder"
					} : new SortOption
					{
						orderBy = "dueDate",
						groupBy = "dueDate"
					})
				});
			}
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.SmartProjectsOption.mtime = nowTimeStampInMills;
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
			SettingsHelper.PushLocalPreference();
		}
	}

	public void SaveFocusKeepInSync(bool keepInSync)
	{
		if (UserPreference != null && UserPreference?.FocusConfig == null)
		{
			UserPreference.FocusConfig = new ticktick_WPF.Models.FocusConfig();
		}
		if (UserPreference?.FocusConfig != null)
		{
			UserPreference.FocusConfig.keepInSync = keepInSync;
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.FocusConfig.mtime = nowTimeStampInMills;
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
			SettingsHelper.PushLocalPreference();
		}
	}

	public void RemoveCustomColor(string color)
	{
		if (!string.IsNullOrEmpty(color) && (UserPreference == null || UserPreference?.RecentlyColors != null) && UserPreference?.RecentlyColors != null)
		{
			List<string> list = UserPreference?.RecentlyColors.colors ?? new List<string>();
			for (int i = 0; i < list.Count; i++)
			{
				string colorString = list[i];
				list[i] = ThemeUtil.GetNoAlphaColorString(colorString);
			}
			color = ThemeUtil.GetNoAlphaColorString(color);
			if (list.Contains(color))
			{
				list.Remove(color);
			}
			UserPreference.RecentlyColors.colors = list;
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.RecentlyColors.mtime = nowTimeStampInMills;
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
		}
	}

	public void SaveCustomColor(string color)
	{
		if (string.IsNullOrEmpty(color))
		{
			return;
		}
		if (UserPreference != null && UserPreference?.RecentlyColors == null)
		{
			UserPreference.RecentlyColors = new RecentlyColors();
		}
		if (UserPreference?.RecentlyColors != null)
		{
			List<string> list = UserPreference?.RecentlyColors.colors ?? new List<string>();
			for (int i = 0; i < list.Count; i++)
			{
				string colorString = list[i];
				list[i] = ThemeUtil.GetNoAlphaColorString(colorString);
			}
			color = ThemeUtil.GetNoAlphaColorString(color);
			if (list.Contains(color))
			{
				list.Remove(color);
			}
			list.Insert(0, color);
			if (list.Count > 7)
			{
				list = list.Take(7).ToList();
			}
			UserPreference.RecentlyColors.colors = list;
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			UserPreference.RecentlyColors.mtime = nowTimeStampInMills;
			UserPreference.mtime = nowTimeStampInMills;
			_isChanged = true;
		}
	}

	private async void SetHabitConfigStyle(string style)
	{
		UserPreference.HabitConfig = new HabitConfig
		{
			style = style,
			mtime = Utils.GetNowTimeStampInMills()
		};
		UserPreference.mtime = Utils.GetNowTimeStampInMills();
		Save();
		await SettingsHelper.PushLocalPreference();
	}

	public static async Task SetParallelGuideShow()
	{
		Settings.UserPreference.desktopSplitView = new GuideShowView
		{
			showGuide = false,
			mtime = Utils.GetNowTimeStampInMills()
		};
		Settings.UserPreference.mtime = Utils.GetNowTimeStampInMills();
		Settings.Save();
		await SettingsHelper.PushLocalPreference();
	}

	public void SetEnableCalendarTimeZone(bool enable)
	{
		if (Settings.UserPreference != null)
		{
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			if (Settings.UserPreference.timelineTimeZones == null)
			{
				Settings.UserPreference.timelineTimeZones = new CalTimelineTimeZones
				{
					enabled = enable
				};
			}
			else
			{
				Settings.UserPreference.timelineTimeZones.enabled = enable;
			}
			Settings.UserPreference.timelineTimeZones.mtime = nowTimeStampInMills;
			Settings.UserPreference.mtime = nowTimeStampInMills;
			Settings.Save();
			SettingsHelper.PushLocalPreference();
		}
	}

	public void SaveCalendarTimeZones(CalTimelineTimeZones calTimeZones)
	{
		if (Settings.UserPreference != null)
		{
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			if (Settings.UserPreference.timelineTimeZones == null)
			{
				Settings.UserPreference.timelineTimeZones = calTimeZones;
			}
			else
			{
				Settings.UserPreference.timelineTimeZones.Save(calTimeZones);
			}
			Settings.UserPreference.timelineTimeZones.mtime = nowTimeStampInMills;
			Settings.UserPreference.mtime = nowTimeStampInMills;
			Settings.Save();
			SettingsHelper.PushLocalPreference();
		}
	}

	public void SaveShowCountdownSection(bool showSection)
	{
		if (Settings.UserPreference != null)
		{
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			if (Settings.UserPreference.CountdownConfig == null)
			{
				Settings.UserPreference.CountdownConfig = new CountdownConfig
				{
					showSection = showSection
				};
			}
			else
			{
				Settings.UserPreference.CountdownConfig.showSection = showSection;
			}
			Settings.UserPreference.CountdownConfig.mtime = nowTimeStampInMills;
			Settings.UserPreference.mtime = nowTimeStampInMills;
			Settings.Save();
			SettingsHelper.PushLocalPreference();
		}
	}

	public void SaveCalendarColorConfig(CalendarColorConfigViewModel.CalColorGroupType group, CalendarColorConfigItem item, bool saveAndPush)
	{
		if (Settings.UserPreference != null)
		{
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			if (Settings.UserPreference.CalendarColorConfig == null)
			{
				Settings.UserPreference.CalendarColorConfig = new CalendarColorConfig();
			}
			switch (group)
			{
			case CalendarColorConfigViewModel.CalColorGroupType.Task:
				Settings.UserPreference.CalendarColorConfig.Task = item;
				break;
			case CalendarColorConfigViewModel.CalColorGroupType.Habit:
				Settings.UserPreference.CalendarColorConfig.Habit = item;
				break;
			case CalendarColorConfigViewModel.CalColorGroupType.Countdown:
				Settings.UserPreference.CalendarColorConfig.Countdown = item;
				break;
			}
			if (saveAndPush)
			{
				Settings.UserPreference.CalendarColorConfig.mtime = nowTimeStampInMills;
				Settings.UserPreference.mtime = nowTimeStampInMills;
				Settings.Save();
				SettingsHelper.PushLocalPreference();
			}
		}
	}

	public bool IsShowCalendarIcon(DisplayType type)
	{
		CalendarConfig calendarConfig = Settings?.UserPreference?.CalendarConfig;
		switch (type)
		{
		case DisplayType.Task:
			if (calendarConfig != null && calendarConfig.CheckBox.HasValue)
			{
				return calendarConfig.CheckBox == true;
			}
			return Settings.ExtraSettings.ShowCalCheckBox;
		case DisplayType.Event:
			return calendarConfig?.Event ?? true;
		case DisplayType.Note:
			return calendarConfig?.Note ?? true;
		case DisplayType.Habit:
			return calendarConfig?.Habit ?? true;
		case DisplayType.Pomo:
			return calendarConfig?.Focus ?? true;
		case DisplayType.Countdown:
			return calendarConfig?.Countdown ?? true;
		case DisplayType.Course:
			return calendarConfig?.Course ?? true;
		default:
			return true;
		}
	}

	public void UpdateCalendarIcon(DisplayType type, bool isShow)
	{
		CalendarConfig calendarConfig = Settings?.UserPreference?.CalendarConfig ?? new CalendarConfig();
		switch (type)
		{
		case DisplayType.Task:
			calendarConfig.CheckBox = isShow;
			Settings.ExtraSettings.ShowCalCheckBox = isShow;
			break;
		case DisplayType.Event:
			calendarConfig.Event = isShow;
			break;
		case DisplayType.Note:
			calendarConfig.Note = isShow;
			break;
		case DisplayType.Habit:
			calendarConfig.Habit = isShow;
			break;
		case DisplayType.Pomo:
			calendarConfig.Focus = isShow;
			break;
		case DisplayType.Countdown:
			calendarConfig.Countdown = isShow;
			break;
		case DisplayType.Course:
			calendarConfig.Course = isShow;
			break;
		default:
			return;
		}
		SaveCalendarStyleConfig(calendarConfig, saveAndPush: false);
	}

	public void SaveCalendarStyleConfig(CalendarConfig config, bool saveAndPush)
	{
		if (Settings.UserPreference != null)
		{
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			Settings.UserPreference.CalendarConfig = config;
			Settings.UserPreference.CalendarConfig.mtime = nowTimeStampInMills;
			Settings.UserPreference.mtime = nowTimeStampInMills;
			if (saveAndPush)
			{
				Settings.Save();
				SettingsHelper.PushLocalPreference();
			}
		}
	}

	public void SaveAnnoyingReminder(bool enable, List<DisplayModule> types)
	{
		if (Settings.UserPreference != null)
		{
			long nowTimeStampInMills = Utils.GetNowTimeStampInMills();
			if (Settings.UserPreference.Reminder == null)
			{
				Settings.UserPreference.Reminder = new ReminderConfig
				{
					annoyingAlertEnabled = enable,
					RemindTypes = types
				};
			}
			else
			{
				Settings.UserPreference.Reminder.annoyingAlertEnabled = enable;
				Settings.UserPreference.Reminder.RemindTypes = types;
			}
			Settings.UserPreference.Reminder.mtime = nowTimeStampInMills;
			Settings.UserPreference.mtime = nowTimeStampInMills;
			Settings.Save();
			SettingsHelper.PushLocalPreference();
		}
	}

	private LocalSettings()
	{
	}

	public static void Init()
	{
	}

	internal Func<bool> CaptureFocusSyncGuard()
	{
		SettingsModel settings = SettingsModel;
		string userId = LoginUserId;
		string auth = LoginUserAuth;
		return () => settings == SettingsModel && userId == LoginUserId && auth == LoginUserAuth;
	}

	internal bool ApplyFocusSyncResult(Func<bool> isCurrentUser, Action apply)
	{
		bool applied = false;
		Dispatcher dispatcher = Application.Current?.Dispatcher;
		if (dispatcher != null && !dispatcher.CheckAccess())
		{
			dispatcher.Invoke(Apply);
		}
		else
		{
			Apply();
		}
		return applied;
		void Apply()
		{
			if (isCurrentUser())
			{
				apply();
				applied = true;
			}
		}
	}

	internal bool ObserveSyncedFocusResult(FocusSyncResultBean result, Func<bool> isCurrentUser)
	{
		if (result == null || result.point <= 0)
		{
			return false;
		}
		return ApplyFocusSyncResult(isCurrentUser, delegate
		{
			ObserveSyncedFocusStatus(result.current);
			if (result.updates != null)
			{
				FocusSyncStatusModel[] updates = result.updates;
				foreach (FocusSyncStatusModel model in updates)
				{
					ObserveSyncedFocusStatus(model);
				}
			}
		});
	}

	public void ObserveSyncedFocusDuration(double seconds)
	{
		if (seconds >= 60.0 && seconds < 300.0 && !PomoLocalSetting.HasSyncedShortFocus)
		{
			PomoLocalSetting.HasSyncedShortFocus = true;
			SettingsModel.PomoLocalSetting = JsonConvert.SerializeObject(PomoLocalSetting);
			OnPropertyChanged("PomoLocalSetting");
		}
	}

	internal void ObserveSyncedFocusRecord(PomodoroModel record)
	{
		if (record != null && record.SyncStatus != -1)
		{
			double seconds = ((record.AdjustTime >= 60000) ? ((double)record.AdjustTime.Value / 1000.0) : ((record.EndTime - record.StartTime).TotalSeconds - (double)record.PauseDuration));
			ObserveSyncedFocusDuration(seconds);
		}
	}

	internal void ObserveSyncedFocusStatus(FocusSyncStatusModel model)
	{
		if (model != null && model.valid && !model.IsDropOrExit())
		{
			if (model.type == 0 && !model.endTime.HasValue)
			{
				ObserveSyncedFocusDuration((double)model.duration * 60.0);
			}
			else if ((model.type == 0 || model.type == 1) && model.endTime.HasValue)
			{
				ObserveSyncedFocusDuration((model.endTime.Value - model.startTime).TotalSeconds - model.GetPauseTime(getTotal: true));
			}
		}
	}

	internal void ObserveSyncedFocusTimer(TimerModel timer)
	{
		if (timer != null && !timer.Deleted && timer.Type == "pomodoro")
		{
			ObserveSyncedFocusDuration((double)timer.PomodoroTime * 60.0);
		}
	}

	internal void ObserveSyncedFocusSummary(PomodoroSummaryModel summary)
	{
		if (summary?.userId != LoginUserId || summary?.focuses == null)
		{
			return;
		}
		foreach (object[] item in summary.focuses.Where((object[] f) => f != null && f.Length == 3))
		{
			if (item[2] is long num)
			{
				ObserveSyncedFocusDuration(num);
			}
			else if (item[2] is int num2)
			{
				ObserveSyncedFocusDuration(num2);
			}
		}
	}

	public void SetSmartProjects(string smartProjects)
	{
		try
		{
			_smartProjects = JsonConvert.DeserializeObject<List<SmartProjectConf>>(smartProjects);
			ProjectDataProvider.ReSortPtfTypes();
		}
		catch
		{
		}
	}

	public void AddSmartProjectConf(SmartProjectConf smartProjectConf)
	{
		if (_smartProjects == null)
		{
			_smartProjects = new List<SmartProjectConf> { smartProjectConf };
			return;
		}
		SmartProjectConf smartProjectConf2 = _smartProjects.FirstOrDefault((SmartProjectConf p) => p.name == smartProjectConf.name);
		if (smartProjectConf2 != null)
		{
			smartProjectConf2.order = smartProjectConf.order;
		}
		else
		{
			_smartProjects.Add(smartProjectConf);
		}
	}

	public void SetSummaryFilter(SummaryFilterModel value)
	{
		_summaryFilter = value;
		_isChanged = true;
	}

	public static double GetValidHeight(double value)
	{
		int val = (int)value;
		val = Math.Max(44, val);
		val = Math.Min(212, val);
		return val;
	}

	protected void OnPropertyChanged(string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		_isChanged = true;
	}

	public void SetChanged()
	{
		_isChanged = true;
	}

	public async Task Save(bool force = false)
	{
		if ((!_isChanged && !force) || Common == null || SettingsModel == null || App.Connection == null)
		{
			return;
		}
		CommonSettings commonSettings = await App.Connection.Table<CommonSettings>().FirstOrDefaultAsync();
		if (commonSettings == null)
		{
			await App.Connection.InsertAsync(Common);
		}
		else
		{
			Common._Id = commonSettings._Id;
			await App.Connection.UpdateAsync(Common);
		}
		if (string.IsNullOrEmpty(SettingsModel.UserId) || (!string.IsNullOrEmpty(Common.LoginUserId) && !(SettingsModel.UserId == Common.LoginUserId)))
		{
			return;
		}
		SettingsModel settingsModel = await (from m in App.Connection.Table<SettingsModel>()
			where m.UserId == SettingsModel.UserId
			select m).FirstOrDefaultAsync();
		if (settingsModel != null)
		{
			SettingsModel._Id = settingsModel._Id;
			SettingsModel.PomoLocalSetting = JsonConvert.SerializeObject(PomoLocalSetting);
			SettingsModel.SummaryFilter = JsonConvert.SerializeObject(SummaryFilter);
			SettingsModel.ShortcutModel = JsonConvert.SerializeObject(ShortCutModel);
			SettingsModel.ProjectListOpenStatus = JsonConvert.SerializeObject(ProjectListOpenStatus);
			SettingsModel.CalendarDisplaySettings = JsonConvert.SerializeObject(CalendarDisplaySettings);
			SettingsModel.ExtraSettings = JsonConvert.SerializeObject(ExtraSettings);
			SettingsModel.Statistics = ((StatisticsModel == null) ? null : JsonConvert.SerializeObject(StatisticsModel));
			SavePreference();
			_smartProjects = ((_smartProjects == null) ? _smartProjects : (from proj in _smartProjects
				group proj by proj.name into g
				select g.First()).ToList());
			SettingsModel.SmartProjects = JsonConvert.SerializeObject(_smartProjects);
			SettingsModel.CheckNotNullProperty();
			await App.Connection.UpdateAsync(SettingsModel);
			_isChanged = false;
		}
		else
		{
			await App.Connection.InsertAsync(SettingsModel);
			_isChanged = false;
		}
	}

	public void OnTagNameChanged(string original, string revised)
	{
		string calendarFilterData = CalendarFilterData;
		if (calendarFilterData != null && calendarFilterData.Contains(original))
		{
			ProjectExtra projectExtra = ProjectExtra.Deserialize(CalendarFilterData, withChild: false);
			if (projectExtra.Tags.Contains(original))
			{
				projectExtra.Tags.Remove(original);
				projectExtra.Tags.Add(revised.ToLower());
			}
			CalendarFilterData = ProjectExtra.Serialize(projectExtra);
		}
		ProjectExtra calendarParallelSelectProject = ExtraSettings.CalendarParallelSelectProject;
		if (calendarParallelSelectProject != null && calendarParallelSelectProject.Tags.Contains(original))
		{
			ProjectExtra calendarParallelSelectProject2 = ExtraSettings.CalendarParallelSelectProject;
			if (calendarParallelSelectProject2.Tags.Contains(original))
			{
				calendarParallelSelectProject2.Tags.Remove(original);
				calendarParallelSelectProject2.Tags.Add(revised.ToLower());
			}
		}
		string arrangeTaskFilterData = ArrangeTaskFilterData;
		if (arrangeTaskFilterData != null && arrangeTaskFilterData.Contains(original))
		{
			ProjectExtra projectExtra2 = ProjectExtra.Deserialize(ArrangeTaskFilterData, withChild: false);
			if (projectExtra2.Tags.Contains(original))
			{
				projectExtra2.Tags.Remove(original);
				projectExtra2.Tags.Add(revised.ToLower());
			}
			ArrangeTaskFilterData = ProjectExtra.Serialize(projectExtra2);
			ArrangeTaskPanel.ResetArrangeFilter(projectExtra2);
		}
	}

	public void NotifyPropertyChanged(string propertyName)
	{
		OnPropertyChanged(propertyName);
	}

	public bool DomainInvalid()
	{
		return Utils.GetTimeStampInSecond(DateTime.Now) - DomainInvalidTime < 86400;
	}

	public void ResetCustomTheme()
	{
		ThemeImageOpacity = 1.0;
		ThemeImageBlurRadius = 0;
		ShowAreaOpacity = 0.9;
		CustomThemeColor = string.Empty;
		CustomThemeLocation = string.Empty;
	}

	public void SaveCustomTheme()
	{
		Common.ThemeImageOpacity = ThemeImageOpacity;
		Common.ThemeImageBlurRadius = ThemeImageBlurRadius;
		Common.ShowAreaOpacity = ShowAreaOpacity;
		Common.CustomThemeLocation = CustomThemeLocation;
		Common.CustomThemeColor = CustomThemeColor;
		_isChanged = true;
		Save();
	}

	public void LoadCustomTheme()
	{
		ThemeImageOpacity = Common.ThemeImageOpacity;
		ThemeImageBlurRadius = Common.ThemeImageBlurRadius;
		ShowAreaOpacity = Common.ShowAreaOpacity;
		CustomThemeLocation = Common.CustomThemeLocation;
		CustomThemeColor = Common.CustomThemeColor;
	}

	public void NotifyThemeChanged()
	{
		OnPropertyChanged("ThemeId");
	}

	public static async Task ResetUserSettings(UserModel user, string accountType)
	{
		ClearModel();
		SettingsModel settingsModel = (await (from m in App.Connection.Table<SettingsModel>()
			where m.UserId == user.userId
			select m).FirstOrDefaultAsync()) ?? new SettingsModel
		{
			UserId = user.userId,
			WindowLeft = (SystemParameters.PrimaryScreenWidth - 1140.0) / 2.0,
			WindowTop = (SystemParameters.PrimaryScreenHeight - 780.0) / 2.0
		};
		Settings.SettingsModel = settingsModel;
		Settings._extraSettings = null;
		Settings.SetSmartProjects(Settings.SettingsModel.SmartProjects);
		Settings.SetPreference();
		Settings.LoginUserAuth = user.token;
		Settings.LoginUserName = user.username;
		Settings.InServerBoxId = user.inboxId;
		Settings.LoginUserId = user.userId;
		int inBoxId = await Utils.CreatInbox();
		Settings.InBoxId = inBoxId;
		if (!string.IsNullOrEmpty(accountType))
		{
			Settings.AccountType = accountType;
		}
		Settings.Save();
	}

	public void SetRemoteTaskListDisplay(bool hideComplete, bool showChecklist)
	{
		bool flag = false;
		if (HideComplete != hideComplete)
		{
			HideComplete = hideComplete;
			flag = true;
		}
		if (ShowSubtasks != showChecklist)
		{
			ShowSubtasks = showChecklist;
			flag = true;
		}
		if (flag)
		{
			App.Window.Reload();
		}
	}

	public static void ClearModel()
	{
		Settings.SettingsModel = new SettingsModel();
		Settings.UserPreference = new UserPreferenceModel();
		Settings.CalendarDisplaySettings = null;
		Settings.ProjectListOpenStatus = null;
		Settings.ShortCutModel = null;
		Settings._pomoLocalSetting = null;
		Settings._summaryFilter = null;
		Settings._smartProjects = null;
		Settings._extraSettings = null;
		Settings._statisticsModel = null;
		Settings.MainWindowDisplayModule = -1;
	}

	public static async Task BeforeLogout()
	{
		Settings.LastProRemindeTime = DateTime.Today.AddDays(-1.0).ToString("yyyy MM dd", App.Ci);
		Settings.LastPullYearPromoTime = DateTime.Today.AddDays(-1.0).Ticks;
		Settings.SettingsModel.PreferrenceMTime = 0L;
		await Settings.Save();
	}
}
