using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using SQLite;
using TickTickDao;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Properties;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.UriScheme;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Lock;
using ticktick_WPF.Views.MarkDown.SpellCheck;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Theme;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF;

[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, InstanceContextMode = InstanceContextMode.Single)]
public class App : System.Windows.Application, IS2CMessages
{
	private const int CheckReminderInterval = 5000;

	private const int CheckExtraInterval = 1000;

	private static Mutex _mutex;

	private static readonly object ReminderListLocker;

	public static List<ReminderModel> ReminderList;

	public static NotifyIcon NotifyIcon;

	public static bool IsHideStart;

	private static bool _isInOperation;

	public static bool IsRegisterSuccess;

	private static bool _isUnhandledException;

	private static ExceptionDialogSession _exceptionDialogSession;

	public static bool ProExpiredSyncError;

	private static bool _topmostCanceled;

	private static bool _reminderTimeZoneChanged;

	private System.Timers.Timer _checkReminderTimer;

	private static bool _windowShow;

	private readonly List<ReminderModel> _firedReminderModels = new List<ReminderModel>();

	private ServiceHost _clientHost;

	private Guid _clientId;

	private static DateTime _time;

	private NamedPipeServerStream _pipeServer;

	private string _pipeName = "TICKTICKAPP";

	private bool _contentLoaded;

	public static CultureInfo SystemCulture { get; }

	public bool TimeZoneChangeHandling { get; set; }

	public static CultureInfo Ci => GlobalSettings.Ci;

	public static MainWindow Window => MainWindowManager.Window;

	private UriRouter uriRouter { get; set; }

	public static bool? HasUsedSubTask { get; set; }

	private static double lastDbVersion { get; set; }

	private static bool hasShow8FuncGuide { get; set; }

	public static SQLiteAsyncConnection Connection => TickTickDao.Connection.DbConnection;

	public static App Instance { get; private set; }

	public static bool IsAdmin => GlobalSettings.IsAdmin;

	public static bool FirstUse { get; set; }

	public static string UId { get; set; }

	static App()
	{
		ReminderListLocker = new object();
		ReminderList = new List<ReminderModel>();
		NotifyIcon = new NotifyIcon();
		IsRegisterSuccess = false;
		ProExpiredSyncError = false;
		SystemCulture = CultureInfo.ReadOnly((CultureInfo)CultureInfo.CurrentCulture.Clone());
	}

	public static void AddLegacyReminder(ReminderModel reminder)
	{
		if (reminder == null || ABTestManager.IsNewRemindCalculate())
		{
			return;
		}
		lock (ReminderListLocker)
		{
			ReminderList.Add(reminder);
		}
	}

	public static void SetLegacyReminders(List<ReminderModel> reminders)
	{
		lock (ReminderListLocker)
		{
			ReminderList = reminders ?? new List<ReminderModel>();
		}
	}

	public static void ClearLegacyReminders()
	{
		lock (ReminderListLocker)
		{
			ReminderList.Clear();
		}
	}

	private static List<ReminderModel> GetLegacyReminderSnapshot()
	{
		lock (ReminderListLocker)
		{
			return new List<ReminderModel>(ReminderList);
		}
	}

	private static void RemoveLegacyReminders(List<ReminderModel> reminders)
	{
		if (reminders == null || reminders.Count == 0)
		{
			return;
		}
		lock (ReminderListLocker)
		{
			ReminderList = ReminderList.Where((ReminderModel reminder) => !reminders.Any((ReminderModel remove) => reminder == remove)).ToList();
		}
	}

	public App()
	{
		TaskScheduler.UnobservedTaskException += TaskUnhandledException;
		System.Windows.Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		SystemEvents.SessionSwitch += OnSessionSwitch;
	}

	private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
	{
		TickFocusManager.OnSessionChanged();
	}

	public void CommandInClient(string text)
	{
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		bool createdNew = false;
		Utils.LogActionTimes(delegate
		{
			_mutex = new Mutex(initiallyOwned: true, "TickTick", out createdNew);
			AppPaths.Init(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Tick_Tick\\", AppDomain.CurrentDomain.BaseDirectory);
			CheckIsAdmin();
		}, 100, null, "StartMutex");
		_pipeName += Environment.UserName.GetHashCode();
		await StartUpSafely(e, createdNew);
	}

	public static void RegisterURIProtocol()
	{
		string text = "ticktick";
		string text2 = Process.GetCurrentProcess().MainModule?.FileName;
		if (string.IsNullOrEmpty(text2))
		{
			UtilLog.Info("RegisterURIProtocol application Path should not be null or empty!");
			return;
		}
		string text3 = "\"" + text2 + "\" %1";
		RegistryKey registryKey = null;
		RegistryKey registryKey2 = null;
		RegistryKey registryKey3 = null;
		try
		{
			registryKey = Registry.CurrentUser?.OpenSubKey("Software", writable: true)?.OpenSubKey("Classes", writable: true);
			registryKey2 = registryKey?.OpenSubKey(text);
			if (registryKey2 != null)
			{
				registryKey3 = registryKey2.OpenSubKey("shell\\open\\command");
				if (registryKey3 != null)
				{
					object value = registryKey3.GetValue("URL Protocol");
					object value2 = registryKey3.GetValue("");
					if (text3.Equals(value2) && text.Equals(value))
					{
						return;
					}
				}
			}
			registryKey2 = registryKey2 ?? registryKey?.CreateSubKey(text);
			if (registryKey2 != null)
			{
				object value3 = registryKey2.GetValue("URL Protocol");
				if (!text.Equals(value3))
				{
					registryKey2.SetValue("URL Protocol", text);
				}
				registryKey3 = registryKey3 ?? registryKey2.CreateSubKey("shell\\open\\command");
				registryKey3?.SetValue("", text3);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("RegisterURIProtocol Exception, " + ex.Message);
		}
		finally
		{
			registryKey?.Close();
			registryKey2?.Close();
			registryKey3?.Close();
		}
	}

	private void CheckIsAdmin()
	{
		try
		{
			if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
			{
				GlobalSettings.IsAdmin = true;
			}
		}
		catch (Exception)
		{
		}
	}

	private async Task StartUpSafely(StartupEventArgs e, bool createdNew)
	{
		string text = "Show";
		if (e.Args.Length != 0)
		{
			text = string.Empty;
			string[] args = e.Args;
			foreach (string text2 in args)
			{
				if (text2 == "-hide")
				{
					IsHideStart = true;
				}
				text += text2;
			}
		}
		lastDbVersion = FileUtils.GetDbTxtVersion();
		bool? firstInstall;
		if (createdNew)
		{
			Instance = this;
			GlobalSettings.AppVersion = Utils.GetVersion(".");
			GlobalSettings.Ci = new CultureInfo("zh-CN");
			firstInstall = null;
			try
			{
				Utils.LoadDefaultLanguage();
				await Utils.LogTaskTimes(InitApp(), 300, "InitApp", "InitApp");
				UtilLog.Info(Assembly.GetEntryAssembly()?.GetName()?.Version.ToString());
			}
			catch (Exception ex)
			{
				UtilLog.Error("Db Init Eror:");
				UtilLog.Error(ex);
				System.Windows.MessageBox.Show(Utils.GetString("CreatDbFailed"), Utils.GetString("PublicError"));
				System.Windows.MessageBox.Show(ExceptionUtils.BuildExceptionMessage(ex));
			}
			LocalSettings.Init();
			InitDll();
			await Utils.LogTaskTimes(TryGetLocalSettings());
			await Utils.LogTaskTimes(CheckTimeZoneOnStart());
			GlobalSettings.AppInstance = this;
			GlobalSettings.UseCustomWindowChrome = LocalSettings.Settings.Common.UseCustomWindowChrome;
			LogFirstInstall(firstInstall);
			GlobalSettings.Ci = new CultureInfo(LocalSettings.Settings.UserChooseLanguage);
			Thread.CurrentThread.CurrentCulture = Ci;
			Thread.CurrentThread.CurrentUICulture = Ci;
			UtilLog.Info($"SetCi {Ci}");
			Utils.LogActionTimes(delegate
			{
				Utils.SwitchLanguage();
				SetFontSize();
				TaskDllInitializer.Init();
				TTCalendarDllInitializer.Init();
			}, 1000, null, "SetLanguageAndIcon");
			if (LocalSettings.Settings.LoginUserId == "")
			{
				FirstUse = false;
				LocalSettings.Settings.LoginUserId = "localuser";
				LocalSettings.Settings.LoginUserName = "Local";
				LocalSettings.Settings.Save();
			}
			else
			{
				bool show = true;
				if (IsHideStart)
				{
					show = (!IsHideStart && LocalSettings.Settings.Common.ShowWhenStart == 0) || LocalSettings.Settings.Common.ShowWhenStart == 1;
				}
				ShowMainWindow(show, delay: false, isStart: true);
				await Task.Delay(4000);
				JumpHelper.InitJumpList();
				JumpHelper.InitChangeEvents();
			}
			SetStickyFontSize();
			MemoryHelper.Cracker();
			InitReminderTimer();
			InitDateChangedEvent();
			InitTimeZoneChangedListener();
			AppIconUtils.SetAppIcons(LocalSettings.Settings.Common.AppIconKey, checkDefault: true);
			try
			{
				TicketLogUtils.RemoveTenDaysAgoLogs();
			}
			catch (Exception)
			{
			}
			InitPipe();
			SetBrowserVersion();
			return;
		}
		try
		{
			NamePipeService.SendPipeText(text, _pipeName);
		}
		catch (Exception ex3)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex3));
			OriginalPipe(text);
		}
		finally
		{
			Shutdown();
		}
		async Task InitApp()
		{
			try
			{
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			}
			catch (Exception)
			{
			}
			SetAlignment();
			CheckNewUser();
			firstInstall = await TickDbHelper.CreateDbAsync();
			PipelineService.GetInstance().RegisterServiceHost();
		}
	}

	private void LogFirstInstall(bool? firstInstall)
	{
		UtilLog.Info("Log FirstInstall firstInstall:" + ((!firstInstall.HasValue) ? "null" : firstInstall.Value.ToString()));
		if (firstInstall.HasValue)
		{
			if (firstInstall.Value && !LocalSettings.Settings.Common.FirstInstall)
			{
				LoginDialog.FirstDownload = true;
			}
			LocalSettings.Settings.Common.FirstInstall = firstInstall.Value;
			LocalSettings.Settings.Save();
		}
	}

	private void OriginalPipe(string arg)
	{
		try
		{
			RegisterClientHost();
			base.Dispatcher?.BeginInvoke((Action)delegate
			{
				Register(_clientId, arg);
			});
		}
		catch (Exception)
		{
			UtilLog.Warn("PipeRegisterFailed");
		}
	}

	private void InitDll()
	{
	}

	private async Task CheckTimeZoneOnStart()
	{
		TimeZoneInfo tz = TimeZoneInfo.Local;
		string localTimeZone = LocalSettings.Settings.LocalTimeZone;
		if (!string.IsNullOrEmpty(localTimeZone) && localTimeZone != tz.Id)
		{
			try
			{
				TimeZoneInfo oldTz = TimeZoneInfo.FindSystemTimeZoneById(localTimeZone);
				await HandleTimeZoneChanged(tz, oldTz, isStart: true);
				return;
			}
			catch
			{
				LocalSettings.Settings.LocalTimeZone = tz.Id;
				return;
			}
		}
		if (string.IsNullOrEmpty(localTimeZone))
		{
			LocalSettings.Settings.LocalTimeZone = tz.Id;
		}
	}

	public async void Restart(bool delay = false)
	{
		await LocalSettings.Settings.Save();
		if (delay)
		{
			await Task.Delay(1000);
		}
		ReleaseMutex();
		Process.Start(Assembly.GetExecutingAssembly().Location);
		Shutdown();
	}

	private void SetBrowserVersion()
	{
		if (!InternetExplorerBrowserEmulation.IsBrowserEmulationSet())
		{
			InternetExplorerBrowserEmulation.SetBrowserEmulationVersion();
		}
	}

	public static void SetFontSize()
	{
		switch (LocalSettings.Settings.BaseFontSize)
		{
		case 16:
			Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri("Resource\\FontResource\\Font_Middle.xaml", UriKind.Relative)
			});
			break;
		case 18:
			Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri("Resource\\FontResource\\Font_Large.xaml", UriKind.Relative)
			});
			break;
		default:
			Instance.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri("Resource\\FontResource\\Font_Normal.xaml", UriKind.Relative)
			});
			break;
		}
	}

	public static void SetStickyFontSize()
	{
		int stickyFont = LocalSettings.Settings.ExtraSettings.StickyFont;
		ResourceDictionary resources = Instance.Resources;
		switch (stickyFont)
		{
		case 0:
			resources["StickyFont14"] = 13.0;
			resources["StickyFont13"] = 12.0;
			resources["StickyFont12"] = 11.0;
			resources["StickyFont11"] = 10.0;
			resources["StickyFont10"] = 10.0;
			resources["StickyHeight30"] = 28.0;
			resources["StickyHeight42"] = 38.0;
			resources["StickyTopTitleMargin"] = new Thickness(12.0, 3.0, 6.0, 0.0);
			resources["StickyCheckItemIconMargin"] = new Thickness(3.0, 7.0, 0.0, 0.0);
			break;
		case 1:
			resources["StickyFont14"] = 14.0;
			resources["StickyFont13"] = 13.0;
			resources["StickyFont12"] = 12.0;
			resources["StickyFont11"] = 11.0;
			resources["StickyFont10"] = 10.0;
			resources["StickyHeight30"] = 30.0;
			resources["StickyHeight42"] = 42.0;
			resources["StickyTopTitleMargin"] = new Thickness(12.0, 2.0, 6.0, 0.0);
			resources["StickyCheckItemIconMargin"] = new Thickness(3.0, 8.0, 0.0, 0.0);
			break;
		case 2:
			resources["StickyFont14"] = 16.0;
			resources["StickyFont13"] = 14.0;
			resources["StickyFont12"] = 13.0;
			resources["StickyFont11"] = 12.0;
			resources["StickyFont10"] = 12.0;
			resources["StickyHeight30"] = 34.0;
			resources["StickyHeight42"] = 42.0;
			resources["StickyTopTitleMargin"] = new Thickness(12.0, 2.0, 6.0, 0.0);
			resources["StickyCheckItemIconMargin"] = new Thickness(3.0, 9.0, 0.0, 0.0);
			break;
		case 3:
			resources["StickyFont14"] = 18.0;
			resources["StickyFont13"] = 16.0;
			resources["StickyFont12"] = 14.0;
			resources["StickyFont11"] = 13.0;
			resources["StickyFont10"] = 12.0;
			resources["StickyHeight30"] = 36.0;
			resources["StickyHeight42"] = 46.0;
			resources["StickyTopTitleMargin"] = new Thickness(12.0, 1.0, 6.0, 0.0);
			resources["StickyCheckItemIconMargin"] = new Thickness(3.0, 10.0, 0.0, 0.0);
			break;
		}
	}

	private static async Task TryGetLocalSettings()
	{
		int step = 0;
		try
		{
			if (Connection == null)
			{
				UtilLog.Error("Connection is null!");
			}
			CommonSettings common = await Connection.Table<CommonSettings>().FirstOrDefaultAsync();
			step = 1;
			if (common == null)
			{
				common = new CommonSettings();
				step = 2;
				await Connection.InsertAsync(common);
			}
			step = 3;
			LocalSettings.Settings.Common = common;
			step = 4;
			if (!string.IsNullOrEmpty(common.LoginUserId))
			{
				step = 5;
				List<SettingsModel> list = await Connection.Table<SettingsModel>().ToListAsync();
				step = 6;
				SettingsModel setting = list?.FirstOrDefault((SettingsModel m) => m?.UserId == common.LoginUserId);
				step = 7;
				if (setting == null)
				{
					setting = new SettingsModel
					{
						UserId = common.LoginUserId
					};
					step = 8;
					await Connection.InsertAsync(setting);
				}
				step = 9;
				LocalSettings.Settings.SettingsModel = setting;
				step = 10;
				Task.Run(delegate
				{
					LocalSettings.Settings.SetPreference();
					LocalSettings.Settings.SetSmartProjects(LocalSettings.Settings.SettingsModel.SmartProjects);
					SpellCheckUtils.TryInitSpellLanguage();
				});
			}
			step = 11;
			GlobalSettings.UserId = common.LoginUserId;
			step = 12;
		}
		catch (NullReferenceException)
		{
			UserActCollectUtils.SendException(new Exception("TryGetLocalSettings NullReferenceException " + step), ExceptionType.UiThread);
			throw;
		}
	}

	private void LoadProBaseTheme(string themeId = "", string oldTheme = "")
	{
		if (string.IsNullOrEmpty(oldTheme) || ThemeKey.IsDarkTheme(oldTheme))
		{
			base.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri("Resource/Themes/Light.xaml", UriKind.Relative)
			});
		}
		base.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri("Resource/Themes/ProBase.xaml", UriKind.Relative)
		});
		try
		{
			SetMainBackground(themeId);
		}
		catch (Exception)
		{
		}
	}

	private void LoadDerivedTheme()
	{
		AddDictionary("Resource/Themes/ColorDerived.xaml");
	}

	private static async Task SetMainBackground(string themeId = "")
	{
		if (string.IsNullOrEmpty(themeId))
		{
			themeId = LocalSettings.Settings.ThemeId;
		}
		string name = AppPaths.ThemeDir + themeId.ToLower() + ".png";
		Rect rect = new Rect(0.0, 0.0, 1.0, 1.0);
		if (themeId == "Custom")
		{
			if (!Directory.Exists(AppPaths.ThemeDir))
			{
				Directory.CreateDirectory(AppPaths.ThemeDir);
			}
			string[] files = Directory.GetFiles(AppPaths.ThemeDir, "custom*.*");
			if (files.Length != 0)
			{
				name = files[0];
			}
			rect = Utils.GetRectByString(LocalSettings.Settings.CustomThemeLocation);
			if (rect.Width <= 0.0 || rect.Height <= 0.0)
			{
				rect = new Rect(0.0, 0.0, 1.0, 1.0);
			}
		}
		UtilLog.Info("SetMainBackground " + name);
		if (File.Exists(name) && new FileInfo(name).Length > 0)
		{
			BitmapImage background = Utils.LoadBitmap(name);
			Window.SetBackgroundImage(background, rect);
			return;
		}
		UtilLog.Warn("SetMainBackground " + name + " not Exist");
		if (await FileUtils.TryDownloadBackground(themeId.ToLower() + ".png"))
		{
			UtilLog.Warn("SetMainBackground ReDownloaded");
			BitmapImage background2 = Utils.LoadBitmap(name);
			Window.SetBackgroundImage(background2, rect);
		}
	}

	private void LoadColorBaseTheme(string oldTheme = "")
	{
		if (string.IsNullOrEmpty(oldTheme) || ThemeKey.IsDarkTheme(oldTheme))
		{
			AddDictionary("Resource/Themes/Light.xaml");
		}
		AddDictionary("Resource/Themes/ColorBase.xaml");
		Window?.HideBackground();
	}

	private void TryLoadSavedTheme()
	{
		if (string.IsNullOrEmpty(LocalSettings.Settings.ExtraSettings.AppTheme))
		{
			LocalSettings.Settings.ExtraSettings.AppTheme = LocalSettings.Settings.ThemeId;
		}
		LocalSettings.Settings.ThemeId = string.Empty;
		if (LocalSettings.Settings.ExtraSettings.UseSystemTheme)
		{
			ThemeUtil.TrySetSystemTheme(onlyDark: false, force: true);
		}
		else
		{
			LoadTargetTheme(LocalSettings.Settings.ExtraSettings.AppTheme);
		}
	}

	public void LoadTargetTheme(string themeId, bool fromDark = false)
	{
		if (LocalSettings.Settings.ExtraSettings.AppTheme == "Custom")
		{
			LocalSettings.Settings.LoadCustomTheme();
			SetCustomTheme(string.IsNullOrEmpty(LocalSettings.Settings.CustomThemeColor) ? "#4772FA" : LocalSettings.Settings.CustomThemeColor, fromDark);
		}
		else
		{
			LoadTheme(LocalSettings.Settings.ThemeId, themeId);
		}
	}

	private void SetCustomTheme(string color, bool fromDark)
	{
		ResourceDictionary resourceDictionary = new ResourceDictionary();
		resourceDictionary["ProjectHoverColor"] = ColorConverter.ConvertFromString("#1AFFFFFF");
		resourceDictionary["TextBoxBackground"] = ThemeUtil.GetAlphaColor("#000000", 4);
		resourceDictionary["ProjectMenuBackGround"] = ThemeUtil.GetColorInString("#1A000000");
		resourceDictionary["ShowAreaBottomColor"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
		resourceDictionary["ParallelPartBackground"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
		resourceDictionary["CardBackBottomColor"] = ThemeUtil.GetAlphaColor("#FFFFFF", (int)(LocalSettings.Settings.ShowAreaOpacity * 100.0));
		resourceDictionary["LeftBarIconColor"] = ColorConverter.ConvertFromString("#FFFFFF");
		resourceDictionary["ProjectMenuBaseColor"] = ColorConverter.ConvertFromString("#FFFFFF");
		resourceDictionary["WindowBackColorTop"] = ColorConverter.ConvertFromString("#191919");
		resourceDictionary["WindowBackColorBottom"] = ColorConverter.ConvertFromString("#191919");
		resourceDictionary["KanbanContainerColor"] = Brushes.Transparent;
		resourceDictionary["CardBackColor"] = Brushes.Transparent;
		if (ColorConverter.ConvertFromString(color) is Color color2)
		{
			resourceDictionary["ColorPrimary"] = color2;
			resourceDictionary["MainWindow"] = color2;
			resourceDictionary["PrimaryColor"] = new SolidColorBrush(color2);
			resourceDictionary["DateColorPrimary"] = new SolidColorBrush(color2);
		}
		if (ColorConverter.ConvertFromString("#CC" + color.Substring(color.Length - 6, 6)) is Color color3)
		{
			resourceDictionary["LeftBarBackColorTop"] = color3;
			resourceDictionary["LeftBarBackColorBottom"] = color3;
		}
		if (ColorConverter.ConvertFromString("#1E" + color.Substring(color.Length - 6, 6)) is Color color4)
		{
			resourceDictionary["ItemSelected"] = color4;
		}
		if (ColorConverter.ConvertFromString("#14" + color.Substring(color.Length - 6, 6)) is Color color5)
		{
			resourceDictionary["ItemHover"] = color5;
		}
		string oldTheme = (fromDark ? "Dark" : "Blue");
		LoadTheme(oldTheme, "Custom", resourceDictionary);
	}

	private void AddDictionary(string url)
	{
		base.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri(url, UriKind.Relative)
		});
	}

	public void LoadTheme(string oldTheme = "", string newTheme = "", ResourceDictionary rd = null, bool isRetry = false)
	{
		try
		{
			string text = LocalSettings.Settings.ThemeId;
			if (newTheme != null && text != newTheme && !string.IsNullOrEmpty(newTheme))
			{
				text = newTheme;
			}
			text = ThemeUtil.GetThemeKey(text);
			LocalSettings.Settings.ThemeId = text;
			if (!ThemeUtil.ExistTheme(text))
			{
				LocalSettings.Settings.ThemeId = "White";
				text = LocalSettings.Settings.ThemeId;
			}
			if (!ThemeKey.IsDarkTheme(text))
			{
				if (string.IsNullOrEmpty(oldTheme) || ThemeKey.IsDarkTheme(oldTheme) || ThemeKey.IsPinkTheme(oldTheme) || ThemeKey.IsGreenTheme(oldTheme))
				{
					AddDictionary("Resource/icons_light.xaml");
				}
				if (ThemeKey.IsPinkTheme(text) || ThemeKey.IsGreenTheme(text))
				{
					AddDictionary("Resource/icons_pink.xaml");
				}
				AddDictionary("Resource/Themes/LightNecessaryColor.xaml");
			}
			else
			{
				if (!ThemeKey.IsDarkTheme(oldTheme))
				{
					AddDictionary("Resource/icons_dark.xaml");
				}
				AddDictionary("Resource/Themes/Dark.xaml");
			}
			if (ThemeKey.IsColorTheme(text))
			{
				LoadColorBaseTheme(oldTheme);
			}
			else if (!ThemeKey.IsDarkTheme(text))
			{
				LoadProBaseTheme(text, oldTheme);
			}
			else
			{
				Window?.HideBackground();
			}
			if (rd != null)
			{
				base.Resources.MergedDictionaries.Add(rd);
			}
			else
			{
				LocalSettings.Settings.ResetCustomTheme();
				string url = "Resource/Themes/" + text + ".xaml";
				AddDictionary(url);
				if (text == "DarkCustom")
				{
					ThemeUtil.SetDarkCustomColor();
				}
			}
			if (!ThemeKey.IsDarkTheme(text))
			{
				LoadDerivedTheme();
				SolidColorBrush blendColor = ThemeUtil.GetBlendColor(((Color)FindResource("ColorPrimary")/*cast due to .constrained prefix*/).ToString(), "#191919", 1f, 0.7f);
				ResourceDictionary resourceDictionary = new ResourceDictionary();
				resourceDictionary["EmptyIconColor1"] = blendColor;
				resourceDictionary["EmptyIconColor2"] = blendColor;
				base.Resources.MergedDictionaries.Add(resourceDictionary);
			}
			SetProjectMenuTextShadow(ThemeKey.ShowProjectTextShadow(text));
			DataChangedNotifier.NotifyThemeModeChanged();
			if (!string.IsNullOrEmpty(oldTheme) && ((!ThemeKey.IsDarkTheme(oldTheme) && ThemeKey.IsDarkTheme(text)) || (ThemeKey.IsDarkTheme(oldTheme) && !ThemeKey.IsDarkTheme(text))))
			{
				DataChangedNotifier.NotifyIsDarkChanged();
			}
			CardThemeDictService.ReloadInterfaceStyle();
			DataChangedNotifier.NotifyInterfaceStyleChanged();
		}
		catch (Exception ex)
		{
			if (!isRetry)
			{
				LoadTheme(oldTheme, newTheme, rd, isRetry: true);
			}
			else
			{
				UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
	}

	internal async void LoadFontResource()
	{
		string appFontFamily = LocalSettings.Settings.ExtraSettings.AppFontFamily;
		string fontFamily = FontFamilyUtils.GetFontFamily(appFontFamily);
		FontFamily fontFamilyByKey = FontFamilyUtils.GetFontFamilyByKey(appFontFamily);
		LocalSettings.Settings.FontFamily = fontFamilyByKey;
		Constants.DefaultMonoFont = "Consolas," + fontFamily;
		string uriString = "Resource/FontResource/FontFamilyNormal.xaml";
		switch (appFontFamily)
		{
		case "SourceHansansSC_CN":
			uriString = "Resource/FontResource/FontFamilySourceHanSans.xaml";
			break;
		case "975Maru_CN":
			uriString = "Resource/FontResource/FontFamily975Maru.xaml";
			break;
		case "Default_CN":
			uriString = "Resource/FontResource/FontFamilyYaHei.xaml";
			break;
		case "Yozai_CN":
			uriString = "Resource/FontResource/FontFamilyYozai.xaml";
			break;
		}
		base.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri(uriString, UriKind.Relative)
		});
	}

	public void SetProjectMenuTextShadow(bool showShadow)
	{
		if (showShadow)
		{
			base.Resources["ProjectTitleEffect"] = FindResource("ProjectTitleShadowEffect");
		}
		else
		{
			base.Resources.Remove("ProjectTitleEffect");
		}
	}

	private void InitDateChangedEvent()
	{
		TimeChangeNotifier.DayChanged -= OnDateChanged;
		TimeChangeNotifier.DayChanged += OnDateChanged;
	}

	private static void OnDateChanged(object sender, EventArgs e)
	{
		Window?.HandleDateChanged();
		Window?.SyncVersionStatus();
		Instance?.Dispatcher?.Invoke(CalendarWidgetHelper.OnCurrentDateChanged);
	}

	private void InitTimeZoneChangedListener()
	{
		SystemEvents.TimeChanged -= OnTimeZoneChanged;
		SystemEvents.TimeChanged += OnTimeZoneChanged;
	}

	private async void OnTimeZoneChanged(object sender, EventArgs e)
	{
		TimeZoneInfo.ClearCachedData();
		TimeZoneInfo local = TimeZoneInfo.Local;
		TimeZoneInfo oldTz = TimeZoneData.LocalTimeZoneModel?.TimeZone;
		TimeZoneData.LocalTimeZoneModel = new TimeZoneViewModel(local);
		await HandleTimeZoneChanged(local, oldTz);
	}

	private async Task HandleTimeZoneChanged(TimeZoneInfo newTz, TimeZoneInfo oldTz, bool isStart = false)
	{
		if (oldTz == null || oldTz.Id == newTz.Id)
		{
			return;
		}
		TimeZoneChangeHandling = true;
		try
		{
			if (!isStart)
			{
				Utils.Toast(Utils.GetString("TimeZoneChangeRestart"));
			}
			else
			{
				_reminderTimeZoneChanged = true;
			}
			await TaskDao.HandleTimeZoneChanged(newTz, oldTz);
			await CalendarEventDao.HandleTimeZoneChanged(newTz, oldTz);
			LocalSettings.Settings.LocalTimeZone = newTz.Id;
			await UserActCollectUtils.OnDeviceDataChanged();
			if (!isStart)
			{
				LocalSettings.Settings.ExtraSettings.ReminderCalculated = false;
				if (!LocalSettings.Settings.EnableTimeZone && new CustomerDialog(Utils.GetString("TimeZoneChange"), Utils.GetString("TimeZoneChangeText"), Utils.GetString("Enable"), Utils.GetString("Cancel")).ShowDialog() == true)
				{
					LocalSettings.Settings.EnableTimeZone = true;
					await SettingsHelper.PushLocalSettings();
				}
				await LocalSettings.Settings.Save();
				Restart(delay: true);
			}
		}
		finally
		{
			TimeZoneChangeHandling = false;
		}
	}

	public async Task ShowMainWindow(bool show = true, bool delay = false, bool isStart = false)
	{
		MainWindowManager.InitWindow();
		if (LocalSettings.Settings.NeedShowTutorial)
		{
			LocalSettings.Settings.NeedShowTutorial = false;
			Window.TryShowTutorial();
			await Task.Delay(500);
		}
		_windowShow = true;
		LoadUserSettings();
		MainWindowManager.InitWindowSettingOnLogin();
		InitData();
		if (show)
		{
			await Window.TryShowMainWindow(force: false, delay, isStart);
		}
		else
		{
			Window.HideInInit();
		}
	}

	private void LoadUserSettings()
	{
		TryLoadSavedTheme();
		LoadSavedFont();
		ResourceUtils.SetKanbanColumnWidth();
		CheckIfNewVersion();
		LocalSettings.Settings.OnPreferenceChanged?.Invoke();
	}

	public static async Task CheckIfNewVersion(bool byPull = false)
	{
		double.TryParse(LocalSettings.Settings.ExtraSettings.LastVersion, out var result);
		bool isNewLocalUser = result <= 0.0;
		double num = Utils.VersionNumToDouble();
		LocalSettings.Settings.ExtraSettings.IsNewVersion = result < num;
		LocalSettings.Settings.Save();
		if (!byPull && LocalSettings.Settings.ExtraSettings.IsNewVersion)
		{
			LocalSettings.Settings.SettingsModel.PreferrenceMTime = 0L;
			await LocalSettings.Settings.Save();
			await CountdownDao.ClearCountdownEtag();
			TryMigrateSetting(isNewLocalUser);
		}
	}

	private static async Task TryMigrateSetting(bool isNewLocalUser)
	{
		LocalSettings settings = LocalSettings.Settings;
		int num;
		if (settings == null)
		{
			num = 0;
		}
		else
		{
			UserPreferenceModel userPreference = settings.UserPreference;
			num = ((userPreference != null && (userPreference.CalendarConfig?.ItemStyleType.HasValue ?? false)) ? 1 : 0);
		}
		if (num != 0)
		{
			return;
		}
		CalendarConfig calendarConfig = LocalSettings.Settings?.UserPreference?.CalendarConfig ?? new CalendarConfig();
		if (isNewLocalUser)
		{
			calendarConfig.ItemStyleType = CalendarConfig.CalendarItemStyle.Classic;
			calendarConfig.CheckBox = true;
		}
		else
		{
			if (calendarConfig == null || !calendarConfig.CheckBox.HasValue)
			{
				calendarConfig.CheckBox = LocalSettings.Settings?.ExtraSettings?.ShowCalCheckBox;
			}
			calendarConfig.ItemStyleType = CalendarConfig.CalendarItemStyle.Classic;
		}
		LocalSettings.Settings.SaveCalendarStyleConfig(calendarConfig, saveAndPush: true);
	}

	private async void InitData()
	{
		await Task.Run(async delegate
		{
			await WarmCache();
			HolidayManager.TryPullRemoteHolidays();
			SyncManager.Init();
			TimeChangeNotifier.BeginTimer();
			ReminderCalculator.AssembleReminders();
			await ReminderBase.InitReminders(_reminderTimeZoneChanged);
			TryStartWidgets();
		});
		SearchProjectHelper.InitModels();
		TickFocusManager.SaveAfterClose = true;
		if (_reminderTimeZoneChanged && !LocalSettings.Settings.EnableTimeZone && new CustomerDialog(Utils.GetString("TimeZoneChange"), Utils.GetString("TimeZoneChangeText"), Utils.GetString("OK"), Utils.GetString("Cancel")).ShowDialog() == true)
		{
			LocalSettings.Settings.EnableTimeZone = true;
			LocalSettings.Settings.Save();
			SettingsHelper.PushLocalSettings();
		}
	}

	private void LoadSavedFont()
	{
		if ((Utils.IsZhCn() && Utils.IsDida()) || (Utils.IsEn() && !Utils.IsDida()))
		{
			LoadFontResource();
			return;
		}
		string text = Constants.DefaultFont;
		string text2 = Ci.ToString();
		if (!(text2 == "ja-JP"))
		{
			if (text2 == "ko-KR")
			{
				text = "Segoe UI, Malgun Gothic, Microsoft JhengHei UI, Microsoft YaHei UI";
			}
		}
		else
		{
			text = "Segoe UI, Yu Gothic UI, Microsoft JhengHei UI, Microsoft YaHei UI";
		}
		FontFamily fontFamily = new FontFamily(text);
		LocalSettings.Settings.FontFamily = fontFamily;
		Constants.DefaultMonoFont = "Consolas," + text;
	}

	private static async Task WarmCache()
	{
		await CacheManager.Init();
		MainWindowManager.OnDataInited();
		UserManager.Init(init: true);
		TaskDefaultDao.InitCache();
		UpgradeHelper.CheckUpgradePoint();
		NotificationHelper.InitProjectNtfOptions();
		HabitSectionCache.SetSections();
		SmartListTaskFoldHelper.Load();
	}

	private static async void TryStartWidgets()
	{
		await Task.Delay(10);
		if (await CanShowWidget())
		{
			TaskStickyWindow.TryLoadSavedStickies();
			await Task.Delay(1000);
			Instance.Dispatcher.Invoke(delegate
			{
				CalendarWidgetHelper.TryLoadWidget();
				MatrixWidgetHelper.TryLoadWidget();
				AIWindow.ShowDesktopWidget(force: false);
				ProjectWidgetsHelper.LoadWidgets();
				CountdownWidgetsHelper.LoadAllWidgets();
				HabitWidgetHelper.LoadAllWidgets();
			});
		}
		AppLockModel appLockModel = await AppLockCache.GetModel();
		if (appLockModel == null || !appLockModel.Locked)
		{
			IndependentWindow.Restore();
		}
		if (LocalSettings.Settings.PomoLocalSetting.OpenWidget)
		{
			System.Windows.Application.Current?.Dispatcher.Invoke(delegate
			{
				TickFocusManager.HideOrShowFocusWidget(force: true);
			});
		}
	}

	private static async Task<bool> CanShowWidget()
	{
		AppLockModel appLockModel = await AppLockCache.GetModel();
		if (appLockModel != null && appLockModel.Locked && appLockModel.LockWidget)
		{
			return false;
		}
		return true;
	}

	public static bool IsInOperation()
	{
		return _isInOperation;
	}

	public static async Task NavigateAI()
	{
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(Navigate);
		}
		else
		{
			Navigate();
		}
		static void Navigate()
		{
			Window.ShowWindow();
			if (!LocalSettings.Settings.EnableAI)
			{
				LocalSettings.Settings.ShowOrHideTabBar("AI_ASSISTANT", show: true);
			}
			Window.SwitchModule(DisplayModule.AI);
		}
	}

	public static async Task NavigateCountdown()
	{
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(Navigate);
		}
		else
		{
			Navigate();
		}
		static void Navigate()
		{
			Window.ShowWindow();
			Window.SwitchModule(DisplayModule.CountDown);
		}
	}

	public static async Task NavigateEvent(string eventId)
	{
		if (await AppLockCache.GetAppLocked())
		{
			AppUnlockWindow.TryUnlockApp(Navigate);
		}
		else
		{
			Navigate();
		}
		void Navigate()
		{
			Window.ShowWindow();
			Window.NavigateEvent(eventId);
		}
	}

	public static void NavigateCourse(string courseId)
	{
		try
		{
			Window.TryShowMainWindow();
			Window.NavigateCourse(courseId);
		}
		catch (Exception)
		{
		}
	}

	public static void NavigateHabit(string habitId, bool showRecord = false)
	{
		try
		{
			Window.TryShowMainWindow();
			Window.NavigateHabit(habitId, showRecord);
		}
		catch (Exception)
		{
		}
	}

	public static void ShowMainWindow(string taskId, string itemId = "", bool skiplock = false)
	{
		try
		{
			if (!string.IsNullOrEmpty(taskId))
			{
				Window.UnlockAndNavigateTask(taskId, itemId);
			}
			else
			{
				Window.TryShowMainWindow(skiplock);
			}
		}
		catch (Exception)
		{
		}
	}

	private void InitReminderTimer()
	{
		_checkReminderTimer = new System.Timers.Timer(5000.0);
		_checkReminderTimer.Elapsed += CheckOnTimeReminders;
		_checkReminderTimer.Start();
	}

	private void RegisterClientHost()
	{
		_clientHost = new ServiceHost(this);
		_clientId = Guid.NewGuid();
		Guid clientId = _clientId;
		UtilLog.Info("RegisterClientHost " + clientId.ToString());
		ServiceHost clientHost = _clientHost;
		Type typeFromHandle = typeof(IS2CMessages);
		NetNamedPipeBinding binding = new NetNamedPipeBinding();
		clientId = _clientId;
		clientHost.AddServiceEndpoint(typeFromHandle, binding, "net.pipe://localhost/Client_" + clientId.ToString());
		_clientHost.Open();
	}

	private static void SetAlignment()
	{
		if (SystemParameters.MenuDropAlignment)
		{
			FieldInfo field = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.Static | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(null, false);
			}
		}
	}

	private void TaskUnhandledException(object sender, UnobservedTaskExceptionEventArgs e)
	{
		UtilLog.Warn("TaskUnhandledException() " + e.Exception);
		System.IO.File.AppendAllText(@"C:\\114514\\dida\\ticktick-wpf-custom\\docs\\unhandled_exceptions.log", DateTime.Now + " " + e.Exception + "\r\n");
		UserActCollectUtils.SendException(e.Exception, ExceptionType.Task);
		e.SetObserved();
	}

	private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		if (_isUnhandledException)
		{
			_exceptionDialogSession?.TryRequestNative(e.Exception);
			e.Handled = true;
			return;
		}
		if (!_isUnhandledException)
		{
			if (TryHandleException(e.Exception))
			{
				e.Handled = true;
				return;
			}
			string stackTrace = e.Exception.StackTrace;
			if (stackTrace != null && stackTrace.Contains("LoadFontResource"))
			{
				LocalSettings.Settings.ExtraSettings.AppFontFamily = "Default_CN";
				LocalSettings.Settings.Save(force: true);
			}
			bool restart = false;
			string stackTrace2 = e.Exception.StackTrace;
			if (stackTrace2 != null && stackTrace2.Contains("WindowChrome") && !GlobalSettings.UseCustomWindowChrome)
			{
				LocalSettings.Settings.Common.UseCustomWindowChrome = true;
				LocalSettings.Settings.Save(force: true);
				restart = true;
			}
			_isUnhandledException = true;
			Exception exception = e.Exception;
			ExceptionDialogSession session = new ExceptionDialogSession(exception, () => new UnhandledExceptionWindow(exception, restart), delegate(Window window)
			{
				((UnhandledExceptionWindow)window).CloseForNativeFallback();
			}, delegate
			{
				ShowNativeExceptionWindow(exception, restart);
			});
			_exceptionDialogSession = session;
			if (RenderFailureRecovery.IsRenderFailure(e.Exception))
			{
				ExceptionWindowDiagnostics.LogRenderFailure(Window);
			}
			UtilLog.Error("DispatcherUnhandledException() " + e.Exception);
			System.IO.File.AppendAllText(@"C:\\114514\\dida\\ticktick-wpf-custom\\docs\\unhandled_exceptions.log", DateTime.Now + " " + e.Exception + "\r\n");
			UserActCollectUtils.SendException(e.Exception, ExceptionType.UiThread);
			Utils.RunOnUiThread(base.Dispatcher, delegate
			{
				ShowExceptionWindow(exception, session);
			});
		}
		e.Handled = true;
	}

	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception ex && ex.GetType().ToString() != "System.Threading.ThreadAbortException" && ex.GetType().ToString() != "System.Threading.Tasks.TaskCanceledException")
		{
			UtilLog.Error("CurrentDomain_UnhandledException() " + ex);
			System.IO.File.AppendAllText(@"C:\\114514\\dida\\ticktick-wpf-custom\\docs\\unhandled_exceptions.log", DateTime.Now + " " + ex + "\r\n");
			UserActCollectUtils.SendException(ex, ExceptionType.OtherThread);
		}
	}

	private static bool TryHandleException(Exception e)
	{
		if (e is COMException ex && (ex.ErrorCode == 2147746259u || ex.ErrorCode == 2147746256u))
		{
			Utils.Toast("ClipBoard Error");
			return true;
		}
		return false;
	}

	private static async void ShowExceptionWindow(Exception e, ExceptionDialogSession session)
	{
		if (!(e is NotSupportedException) && !(e is ConfigurationErrorsException))
		{
			LocalSettings.Settings.Save();
			await Task.Delay(100);
			session.Show();
		}
	}

	private static void ShowNativeExceptionWindow(Exception e, bool restart)
	{
		bool needDownload = UnhandledExceptionWindow.NeedsDownload(e);
		using Form form = RenderFailureRecovery.CreateNativeDialog(e, restart, delegate
		{
			UnhandledExceptionWindow.HandlePrimaryAction(needDownload, restart);
		}, () => UnhandledExceptionWindow.HandleSecondaryAction(needDownload), delegate
		{
			System.Windows.Application.Current?.Shutdown();
		});
		form.ShowDialog();
	}

	public void StopReminderTimer()
	{
		_checkReminderTimer?.Stop();
	}

	public void StartReminderTimer()
	{
		_checkReminderTimer?.Start();
	}

	private async void CheckOnTimeReminders(object sender, ElapsedEventArgs e)
	{
		_checkReminderTimer.Stop();
		List<ReminderModel> handledReminders = new List<ReminderModel>();
		DateTime checkReminderTime = DateTime.Now;
		List<ReminderModel> legacyReminderSnapshot = GetLegacyReminderSnapshot();
		foreach (ReminderModel item in legacyReminderSnapshot)
		{
			if (item == null)
			{
				handledReminders.Add(item);
				continue;
			}
			DateTime? reminderTime = item.ReminderTime;
			if (reminderTime.HasValue && Math.Abs((checkReminderTime - reminderTime.Value).TotalMilliseconds) < 6000.0)
			{
				handledReminders.Add(item);
				if (!IsReminderRecentFired(item))
				{
					_firedReminderModels.Add(item);
					await ReminderBase.ShowReminderWindow(item);
				}
			}
		}
		RemoveLegacyReminders(handledReminders);
		await AnnoyingReminderManager.Tick(checkReminderTime);
		_checkReminderTimer.Start();
	}

	private bool IsTaskProjectMuted(ReminderModel reminder)
	{
		if (reminder.Type == ReminderModel.RemindType.Task || reminder.Type == ReminderModel.RemindType.Checklist)
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(reminder.TaskId);
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == reminder.ProjectId);
			if (projectModel != null && !projectModel.IsValid())
			{
				return true;
			}
			bool flag = string.IsNullOrEmpty(taskById.Assignee) || taskById.Assignee == "-1";
			if (((flag && taskById.Creator != LocalSettings.Settings.LoginUserId) || (!flag && reminder.Assignee != Utils.GetCurrentUserIdInt().ToString())) && projectModel != null && projectModel.muted && projectModel.IsShareList())
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReminderRecentFired(ReminderModel model)
	{
		return _firedReminderModels.Any((ReminderModel reminder) => reminder.TaskId == model.TaskId && reminder.CheckItemId == model.CheckItemId && reminder.EventId == model.EventId && reminder.ReminderTime == model.ReminderTime && reminder.HabitId == model.HabitId);
	}

	public async void TryDismissRemindersOnSync()
	{
		List<ReminderModel> removes = new List<ReminderModel>();
		if (_firedReminderModels.Any())
		{
			for (int i = 0; i < _firedReminderModels.Count; i++)
			{
				ReminderModel reminder = _firedReminderModels[i];
				if (await ReminderCalculator.IsReminderExpired(reminder))
				{
					SystemToastUtils.RemoveToast((!string.IsNullOrEmpty(reminder.CheckItemId)) ? reminder.CheckItemId : reminder.TaskId, reminder.ProjectId);
					removes.Add(reminder);
				}
			}
		}
		if (!removes.Any())
		{
			return;
		}
		foreach (ReminderModel item in removes)
		{
			if (item.ReminderTime <= DateTime.Now.AddMinutes(-1.0))
			{
				_firedReminderModels.Remove(item);
			}
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		RemoveTrayIcon();
		try
		{
			HotKeyUtils.RemoveHotkeys();
		}
		catch (Exception)
		{
		}
		TaskStickyWindow.OnAppExit();
		UserActCollectUtils.OnAppExit();
		_pipeServer?.Close();
		base.OnExit(e);
	}

	public async void ExitApplication()
	{
		if (await TickFocusManager.ExitPomo())
		{
			RemoveTrayIcon();
			Shutdown();
		}
	}

	private static void RemoveTrayIcon()
	{
		if (NotifyIcon != null)
		{
			NotifyIcon.Visible = false;
			NotifyIcon.Dispose();
			NotifyIcon = null;
		}
	}

	private static void CloseChannel(ICommunicationObject channel)
	{
		try
		{
			channel.Close();
		}
		catch (Exception)
		{
		}
		finally
		{
			channel.Abort();
		}
	}

	private void InitPipe()
	{
		uriRouter = new UriRouter(Assembly.GetExecutingAssembly());
		NamePipeService.InitPipe(_pipeName, HandlePipeCommand);
	}

	private void HandlePipeCommand(string command)
	{
		base.Dispatcher.Invoke(delegate
		{
			if (command.StartsWith("ticktick://"))
			{
				TickTickUri(command);
			}
			else
			{
				Window?.HandlePipelineMsg(command);
			}
		});
	}

	private void TickTickUri(string uri)
	{
		uriRouter.RouteRequest(uri);
		uri = uri.Substring(uri.IndexOf("//", StringComparison.Ordinal) + 2);
		uri = HttpUtility.UrlDecode(uri);
		if (uri.Contains('?'))
		{
			string path = uri.Substring(0, uri.IndexOf('?'));
			string param = uri.Substring(uri.IndexOf('?') + 1);
			UriNotifier.NotifyUri(path, param);
			if (!uri.StartsWith("sign/apple"))
			{
				Window?.TryShowMainWindow();
			}
		}
	}

	private static void Register(Guid clientId, string command)
	{
		using ChannelFactory<IC2SMessages> channelFactory = new ChannelFactory<IC2SMessages>(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/TickTick"));
		IC2SMessages iC2SMessages = channelFactory.CreateChannel();
		try
		{
			iC2SMessages.Register(clientId);
			iC2SMessages.DisplayCommandOnServer(command);
		}
		catch (Exception ex)
		{
			UtilLog.Warn(ExceptionUtils.BuildExceptionMessage(ex));
		}
		finally
		{
			CloseChannel(iC2SMessages as ICommunicationObject);
		}
	}

	public static void SelectTagProject(string tag)
	{
		LocalSettings.Settings.SelectProjectId = "tag:" + tag;
		Window.SelectTagProject(tag);
	}

	public static void ClearReminders()
	{
		ClearLegacyReminders();
	}

	public static void ClearCountdown()
	{
		Window?.CountDownContainer?.Clear();
	}

	public static void CancelTopMost()
	{
		if (Window != null && LocalSettings.Settings.MainWindowTopmost)
		{
			_topmostCanceled = true;
			LocalSettings.Settings.MainWindowTopmost = false;
		}
	}

	public static void RestoreTopMost()
	{
		if (Window != null && _topmostCanceled)
		{
			_topmostCanceled = false;
			LocalSettings.Settings.MainWindowTopmost = true;
		}
	}

	public static void RefreshIconMenu()
	{
	}

	public async void AddCalendarWidget()
	{
		if (ProChecker.CheckPro(ProType.CalendarWidget))
		{
			CalendarWidgetHelper.AddWidget();
		}
	}

	public async void CloseCalendarWidget()
	{
		CalendarWidgetHelper.CloseWidget();
	}

	public async void AddMatrixWidget()
	{
		if (ProChecker.CheckPro(ProType.MatrixWidget))
		{
			MatrixWidgetHelper.AddWidget();
		}
	}

	public async void CloseMatrixWidget()
	{
		MatrixWidgetHelper.CloseWidget();
	}

	public static async void AddProjectWidget()
	{
		ProjectWidgetsHelper.AddDefaultWidget();
	}

	public async void AddSingleCountdownWidget()
	{
		if (ProChecker.CheckPro(ProType.CountdownWidget))
		{
			CountdownWidgetsHelper.AddWidget(WidgetType.CountdownItem);
		}
	}

	public async void AddListCountdownWidget()
	{
		if (ProChecker.CheckPro(ProType.CountdownWidget))
		{
			CountdownWidgetsHelper.AddWidget(WidgetType.CountdownList);
		}
	}

	public async void CloseListCountdownWidget()
	{
		CountdownWidgetsHelper.CloseListWidget();
	}

	public async void AddHabitItemWidget()
	{
		HabitWidgetHelper.AddWidget(WidgetType.HabitItem);
	}

	public async void AddHabitListWidget()
	{
		HabitWidgetHelper.AddWidget(WidgetType.HabitList);
	}

	public async void AddHabitWeekWidget()
	{
		HabitWidgetHelper.AddWidget(WidgetType.HabitWeekItem);
	}

	public void CloseListHabitWidget()
	{
		HabitWidgetHelper.CloseListWidget();
	}

	public static void ReloadProjects()
	{
		Window?.ReloadView();
	}

	public static async Task Unlock()
	{
		await AppLockDao.SetAppLocked(locked: false);
		AppLockWindowManager.Unlock();
		Window.UnlockApp();
	}

	public static void ReleaseMutex()
	{
		System.Windows.Application.Current.Dispatcher.Invoke(delegate
		{
			try
			{
				_mutex.ReleaseMutex();
				_mutex.Dispose();
			}
			catch (Exception)
			{
			}
		});
	}

	private async void OnAppExit(object sender, ExitEventArgs e)
	{
		PipelineService.GetInstance().CloseServiceHost();
		await LocalSettings.Settings.Save(force: true);
		await TaskCompletionRateDao.SaveToDb();
		if (!Utils.IsWindows7())
		{
			SystemToastUtils.ClearToastHistory();
		}
	}

	private async void CheckNewUser()
	{
		try
		{
			string appName = "Dida";
			await Task.Delay(1);
			if (Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey(appName) == null)
			{
				string guid = Utils.GetGuid();
				Registry.CurrentUser.CreateSubKey("Software\\" + appName)?.SetValue("UId", guid);
			}
			UId = Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey(appName)?.GetValue("UId") as string;
			if (string.IsNullOrEmpty(UId))
			{
				UId = Utils.GetGuid0();
			}
		}
		catch (Exception)
		{
			UId = Utils.GetGuid0();
		}
		ObjectIdGenerator.RefreshMachineHash();
	}

	public void SetNotifyIcon(bool isLock)
	{
		if (isLock)
		{
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconLocked;
			return;
		}
		Enum.TryParse<AppIconKey>(LocalSettings.Settings.Common.AppIconKey, out var result);
		switch (result)
		{
		case AppIconKey.CClassic:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconCClassic;
			break;
		case AppIconKey.SClassic:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSClassic;
			break;
		case AppIconKey.SClassicPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSClassicPro;
			break;
		case AppIconKey.SGreenPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSGreenPro;
			break;
		case AppIconKey.SOrangePro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSOrangePro;
			break;
		case AppIconKey.SPinkPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSPinkPro;
			break;
		case AppIconKey.SYellowPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSYellowPro;
			break;
		case AppIconKey.SBlackPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSBlackPro;
			break;
		case AppIconKey.WGreenPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconWGreenPro;
			break;
		case AppIconKey.PlanetPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconPlanetPro;
			break;
		case AppIconKey.Christmas:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconChristmas;
			break;
		case AppIconKey.SpringFestival:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSpringFestival;
			break;
		case AppIconKey.GYellowPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconGYellowPro;
			break;
		case AppIconKey.GRedPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconGRedPro;
			break;
		case AppIconKey.GSiriPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconGSiriPro;
			break;
		case AppIconKey.GBluePro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconGBluePro;
			break;
		case AppIconKey.LRedPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconLRedPro;
			break;
		case AppIconKey.LYellowPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconLYellowPro;
			break;
		case AppIconKey.LGreenPro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconLGreenPro;
			break;
		case AppIconKey.ScratchBluePro:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconScratchBluePro;
			break;
		case AppIconKey.Plump3d:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconPlump3d;
			break;
		case AppIconKey.Emotion:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconEmotion;
			break;
		case AppIconKey.Macintosh:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconMacintosh;
			break;
		case AppIconKey.Ai:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconAi;
			break;
		case AppIconKey.Spaceship:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconSpaceship;
			break;
		case AppIconKey.Mechanical:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconMechanical;
			break;
		case AppIconKey.Neon:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconNeon;
			break;
		case AppIconKey.Planet:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconPlanet;
			break;
		case AppIconKey.Dark_Linear:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconDark_Linear;
			break;
		default:
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconDefault;
			break;
		}
	}

	public void LoadTimelineStyle()
	{
		string source = "Styles\\TimelineStyle.xaml";
		if (base.Resources.MergedDictionaries.FirstOrDefault((ResourceDictionary d) => d.Source?.OriginalString != null && d.Source.OriginalString.Equals(source)) == null)
		{
			base.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri(source, UriKind.Relative)
			});
		}
	}

	public static bool GetWindowShow()
	{
		return _windowShow;
	}

	public async void TryLockApp()
	{
		if (string.IsNullOrEmpty(await AppLockCache.GetLockPassword()))
		{
			new SetPasswordWindow(SetPasswordMode.FirstTry).ShowDialog();
			if (!string.IsNullOrEmpty(await AppLockCache.GetLockPassword()))
			{
				LockAndHide();
			}
		}
		else
		{
			LockAndHide();
		}
	}

	public void LockAndHide()
	{
		Utils.RunOnUiThread(base.Dispatcher, delegate
		{
			LockApp();
			Window.HideWindow(checkLock: false);
		});
	}

	public async Task LockApp()
	{
		AppLockWindowManager.Lock();
		AppLockDao.SetAppLocked(locked: true);
		JumpHelper.InitJumpList();
		if (NotifyIcon != null)
		{
			NotifyIcon.Icon = ticktick_WPF.Properties.Resources.AppIconLocked;
		}
		AppLockModel appLockModel = await AppLockCache.GetModel();
		WindowManager.AppLockOrExit = true;
		UtilLog.Info("AppLocked");
		if (appLockModel != null)
		{
			if (appLockModel.LockWidget)
			{
				CalendarWidgetHelper.HideWidget();
				MatrixWidgetHelper.HideWidget();
				AIWindow.HideDesktopWidget();
				ProjectWidgetsHelper.CloseAll();
				CountdownWidgetsHelper.CloseAll();
				HabitWidgetHelper.CloseAll();
				TaskStickyWindow.CloseAllStickies();
			}
			IndependentWindow.CloseAll();
			if (appLockModel.LockAfter)
			{
				Window.StopLock();
			}
		}
		TaskDetailWindows.CloseAll();
	}

	public static async void CheckLastActiveTime()
	{
		int lastActiveTime = LocalSettings.Settings.ExtraSettings.LastActiveTime;
		int dateNum = ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today);
		if (lastActiveTime != dateNum)
		{
			LocalSettings.Settings.ExtraSettings.LastActiveTime = dateNum;
			LocalSettings.Settings.Save();
			UserActCollectUtils.OnDeviceDataChanged();
			UserActCollectUtils.AddClickEvent("global_data", "theme", ThemeKey.GetCollectLabel(LocalSettings.Settings.ExtraSettings.AppTheme));
			UserActCollectUtils.AddClickEvent("global_data", "app_icon", AppIconViewModel.GetEventLabel(LocalSettings.Settings.Common.AppIconKey));
			UserActCollectUtils.AddClickEvent("global_data", "interface_style", LocalSettings.Settings.ExtraSettings.InterfaceStyle.ToString().ToLower());
			await Task.Delay(5000);
			string text = ABTestManager.TryGetABTestGroup("aatest_newuser_202406");
			if (!string.IsNullOrEmpty(text))
			{
				await UserActCollectUtils.SendUserDataWithGroup("user_active", "open_app", text);
			}
			UserActCollectUtils.PostEventNow();
		}
	}

	public static async Task<bool> CheckTwitterLogin(bool logOut)
	{
		return true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.Exit += OnAppExit;
			Uri resourceLocator = new Uri("/TickTick;component/app.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
