using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util;

public static class SettingsHelper
{
	private static bool _localChecked;

	private static long _lastPushTime;

	public static bool ShortCutChanged { get; set; }

	public static bool GetShowDetail()
	{
		return LocalSettings.Settings.ShowDetails;
	}

	public static async Task PullRemoteSettings()
	{
		await PullRemotePreference();
		UserSettingsModel model = await Communicator.GetUserSettings();
		if (model == null || !string.IsNullOrEmpty(model.errorId))
		{
			return;
		}
		ThreadUtil.DetachedRunOnUiBackThread(async delegate
		{
			bool needPush = false;
			LocalSettings settings = LocalSettings.Settings;
			settings.DateParsing = model.nlpEnabled;
			settings.RemoveTimeText = model.removeDate;
			settings.KeepTagsInText = !model.removeTag;
			TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
			if (defaultSafely != null)
			{
				bool flag = false;
				if (defaultSafely.Priority != model.defaultPriority)
				{
					defaultSafely.Priority = model.defaultPriority;
					flag = true;
				}
				if (defaultSafely.AddTo != model.defaultToAdd)
				{
					defaultSafely.AddTo = model.defaultToAdd;
					flag = true;
				}
				if (int.TryParse(model.defaultTimeMode, out var result) && defaultSafely.DateMode != result)
				{
					defaultSafely.DateMode = result;
					flag = true;
				}
				if (defaultSafely.Duration != model.defaultTimeDuration)
				{
					defaultSafely.Duration = model.defaultTimeDuration;
					flag = true;
				}
				if (model.defaultReminds != null && model.defaultReminds.Any())
				{
					string text = string.Join(",", model.defaultReminds.ToArray());
					if (defaultSafely.TimeReminders != text)
					{
						defaultSafely.TimeReminders = text;
						flag = true;
					}
				}
				else if (!string.IsNullOrEmpty(defaultSafely.TimeReminders))
				{
					defaultSafely.TimeReminders = string.Empty;
					flag = true;
				}
				if (model.defaultADReminders != null && model.defaultADReminders.Any())
				{
					string text2 = string.Join(",", model.defaultADReminders.ToArray());
					if (defaultSafely.AllDayReminders != text2)
					{
						defaultSafely.AllDayReminders = text2;
						flag = true;
					}
				}
				else if (!string.IsNullOrEmpty(defaultSafely.AllDayReminders))
				{
					defaultSafely.AllDayReminders = string.Empty;
					flag = true;
				}
				string defaultDate = GetDefaultDate(model.defaultDueDate);
				if (defaultSafely.Date != defaultDate)
				{
					defaultSafely.Date = defaultDate;
					flag = true;
				}
				if (model.defaultTags != null && TagSerializer.ToJsonContent(model.defaultTags) != defaultSafely.TagString)
				{
					defaultSafely.Tags = model.defaultTags;
					flag = true;
				}
				if (model.defaultProjectId != null && defaultSafely.ProjectId != model.defaultProjectId)
				{
					defaultSafely.ProjectId = model.defaultProjectId;
					flag = true;
				}
				if (flag)
				{
					await TaskDefaultDao.SaveTaskDefault(defaultSafely);
					DataChangedNotifier.NotifyTaskDefaultChanged();
				}
				await ProjectDao.SaveInboxColor(model.inboxColor);
			}
			settings.SetRemoteTaskListDisplay(!model.showCompleted, model.showChecklist);
			settings.WeekStartFromType = GetWeekStartFrom(model.startDayOfWeek);
			settings.PosOfOverdue = model.posOfOverdue;
			List<string> list = model.notificationOptions?.ToList();
			list?.Remove("");
			settings.NotificationOptions = ((list == null) ? "" : list.Join(";"));
			settings.EnableLunar = model.lunarEnabled;
			settings.EnableHoliday = model.holidayEnabled;
			settings.ShowWeek = model.weekNumbersEnabled;
			settings.EnableCountDown = model.enableCountdown;
			if (settings.StartWeekOfYear != model.startWeekOfYear)
			{
				settings.StartWeekOfYear = model.startWeekOfYear;
				DataChangedNotifier.NotifyYearStartFromChanged();
			}
			if (model.calendarViewConf != null)
			{
				settings.ShowCheckListInCal = model.calendarViewConf.showChecklist;
				settings.ShowCompletedInCal = model.calendarViewConf.showCompleted;
				settings.ShowRepeatCircles = model.calendarViewConf.showFutureTask;
				settings.ShowFocusRecord = model.calendarViewConf.showFocusRecord;
				settings.CellColorType = model.calendarViewConf.cellColorType;
				settings.HabitInCal = model.calendarViewConf.showHabit ?? settings.HabitInCal;
				settings.ExtraSettings.ShowCountdownInCal = model.calendarViewConf.showCountdown;
			}
			List<SmartProjectConf> list2 = model.smartProjects?.ToList();
			SmartProjectConf smartProjectConf = list2?.FirstOrDefault((SmartProjectConf p) => p.name == "subscribe");
			if (smartProjectConf != null)
			{
				SmartProjectConf smartProjectConf2 = list2.FirstOrDefault((SmartProjectConf p) => p.name == "subscribedCalendar");
				if (smartProjectConf2 == null)
				{
					needPush = true;
					smartProjectConf.name = "subscribedCalendar";
				}
				else
				{
					needPush = true;
					list2.Remove(smartProjectConf);
					smartProjectConf2.order = smartProjectConf.order;
				}
			}
			settings.SmartProjects = list2 ?? new List<SmartProjectConf>();
			settings.EnableTimeZone = model.isTimeZoneOptionEnabled;
			if (model.shortcutItemConfPc != null && !string.IsNullOrEmpty(model.shortcutItemConfPc) && !"null".Equals(model.shortcutItemConfPc))
			{
				settings.ShortCutModel.CopyProperties(JsonConvert.DeserializeObject<ShortcutModel>(model.shortcutItemConfPc));
				JObject jObject = JObject.Parse(model.shortcutItemConfPc);
				if (jObject["ShortcutAddTask"] != null)
				{
					LocalSettings.Settings.ShortcutAddTask = jObject["ShortcutAddTask"].ToString();
				}
				if (jObject["ShortcutOpenOrClose"] != null)
				{
					LocalSettings.Settings.ShortcutOpenOrClose = jObject["ShortcutOpenOrClose"].ToString();
				}
				if (jObject["ShortcutPin"] != null)
				{
					LocalSettings.Settings.ShortcutPin = jObject["ShortcutPin"].ToString();
				}
				if (jObject["LockShortcut"] != null)
				{
					LocalSettings.Settings.LockShortcut = jObject["LockShortcut"].ToString();
				}
				if (jObject["PomoShortcut"] != null)
				{
					LocalSettings.Settings.PomoShortcut = jObject["PomoShortcut"].ToString();
				}
				if (jObject["CreateStickyShortcut"] != null)
				{
					LocalSettings.Settings.CreateStickyShortCut = jObject["CreateStickyShortcut"].ToString();
				}
				if (jObject["ShowHideStickyShortCut"] != null)
				{
					LocalSettings.Settings.ShowHideStickyShortCut = jObject["ShowHideStickyShortCut"].ToString();
				}
			}
			settings.Save();
			bool pullTimeZone = false;
			if (!_localChecked)
			{
				_localChecked = true;
				if (model.timeZone == null || model.locale == null)
				{
					needPush = true;
					pullTimeZone = true;
				}
			}
			if (needPush)
			{
				await PushLocalSettings(pullTimeZone);
			}
		});
	}

	public static async Task PushLocalSettings(bool pullTimeZone = false)
	{
		if (Utils.IsNetworkAvailable())
		{
			UserSettingsModel model = new UserSettingsModel();
			LocalSettings settings = LocalSettings.Settings;
			settings.Save();
			model.nlpEnabled = settings.DateParsing;
			model.nlpenabled = settings.DateParsing;
			model.removeDate = settings.RemoveTimeText;
			model.removeTag = !settings.KeepTagsInText;
			TaskDefaultModel taskDefaultModel = await TaskDefaultDao.GetTaskDefault();
			if (taskDefaultModel != null)
			{
				model.defaultPriority = taskDefaultModel.Priority;
				model.defaultToAdd = taskDefaultModel.AddTo;
				model.defaultTimeMode = taskDefaultModel.DateMode.ToString();
				model.defaultTimeDuration = taskDefaultModel.Duration;
				model.defaultReminds = ((!string.IsNullOrEmpty(taskDefaultModel.TimeReminders)) ? taskDefaultModel.TimeReminders.Split(',').ToList() : new List<string>());
				model.defaultADReminders = ((!string.IsNullOrEmpty(taskDefaultModel.AllDayReminders)) ? taskDefaultModel.AllDayReminders.Split(',').ToList() : new List<string>());
				model.defaultDueDate = Constants.DateValue.ToIndex(taskDefaultModel.Date);
				model.defaultTags = taskDefaultModel.Tags;
				model.defaultProjectId = ((!string.IsNullOrEmpty(taskDefaultModel.ProjectId)) ? taskDefaultModel.ProjectId : LocalSettings.Settings.InServerBoxId);
			}
			model.showCompleted = !settings.HideComplete;
			model.showFutureTask = settings.ShowRepeatCircles;
			model.showChecklist = settings.ShowSubtasks;
			model.startDayOfWeek = GetWeekStart(settings.WeekStartFrom);
			model.posOfOverdue = settings.PosOfOverdue;
			model.notificationOptions = settings.NotificationOptions?.Split(';');
			UserSettingsModel userSettingsModel = model;
			userSettingsModel.inboxColor = (await ProjectDao.GetProjectById(Utils.GetInboxId()))?.color;
			model.holidayEnabled = settings.EnableHoliday;
			model.lunarEnabled = settings.EnableLunar;
			model.weekNumbersEnabled = settings.ShowWeek;
			model.enableCountdown = settings.EnableCountDown;
			model.calendarViewConf = new CalendarViewConf
			{
				showChecklist = settings.ShowCheckListInCal,
				showCompleted = settings.ShowCompletedInCal,
				showFutureTask = settings.ShowRepeatCircles,
				showFocusRecord = settings.ShowFocusRecord,
				cellColorType = settings.CellColorType,
				showHabit = settings.HabitInCal,
				showCountdown = settings.ExtraSettings.ShowCountdownInCal
			};
			model.isTimeZoneOptionEnabled = settings.EnableTimeZone;
			if (pullTimeZone)
			{
				model.timeZone = TimeZoneData.LocalTimeZoneModel?.TimeZoneName;
				model.locale = CultureInfo.InstalledUICulture.Name.Replace("-", "_");
			}
			List<SmartProjectConf> smartProjects = settings.SmartProjects;
			if (smartProjects != null && smartProjects.Count > 0)
			{
				SmartProjectConf[] smartProjects2 = settings.SmartProjects.ToArray();
				model.smartProjects = smartProjects2;
			}
			JObject jObject = JObject.FromObject(settings.ShortCutModel);
			jObject.Add("ShortcutAddTask", LocalSettings.Settings.ShortcutAddTask);
			jObject.Add("ShortcutOpenOrClose", LocalSettings.Settings.ShortcutOpenOrClose);
			jObject.Add("ShortcutPin", LocalSettings.Settings.ShortcutPin);
			jObject.Add("LockShortcut", LocalSettings.Settings.LockShortcut);
			jObject.Add("PomoShortcut", LocalSettings.Settings.PomoShortcut);
			jObject.Add("CreateStickyShortcut", LocalSettings.Settings.CreateStickyShortCut);
			model.shortcutItemConfPc = jObject.ToString();
			await Communicator.SaveUserSettings(model);
		}
	}

	public static Task<UserPreferenceModel> PullRemotePreference(long? checkPoint = null)
	{
		return PullRemotePreferenceCore(checkPoint, Communicator.GetUserPreference);
	}

	private static async Task<UserPreferenceModel> PullRemotePreferenceCore(long? checkPoint, Func<long, Task<UserPreferenceModel>> fetch)
	{
		LocalSettings settings = LocalSettings.Settings;
		string userId = settings.LoginUserId;
		string auth = settings.LoginUserAuth;
		UserPreferenceModel preference = settings.UserPreference;
		UserPreferenceModel userPreferenceModel = await fetch(checkPoint ?? settings.SettingsModel.PreferrenceMTime);
		if (!IsCurrentPreferenceAccount(userId, auth, preference))
		{
			return null;
		}
		settings.SetRemotePreference(userPreferenceModel);
		_lastPushTime = settings.SettingsModel.PreferrenceMTime;
		return userPreferenceModel;
	}

	private static bool IsCurrentPreferenceAccount(string userId, string auth, UserPreferenceModel preference)
	{
		LocalSettings settings = LocalSettings.Settings;
		if (settings.LoginUserId == userId && settings.LoginUserAuth == auth)
		{
			return settings.UserPreference == preference;
		}
		return false;
	}

	public static async Task PushLocalPreference()
	{
		LocalSettings settings = LocalSettings.Settings;
		if (settings.UserPreference.matrix != null)
		{
			settings.UserPreference.matrix.show_completed = settings.UserPreference.matrix.show_completed ?? true;
		}
		settings.UserPreference.mtime = settings.UserPreference.GetMaxMTime();
		if (_lastPushTime < settings.UserPreference.mtime && await Communicator.SaveUserPreference(settings.UserPreference))
		{
			_lastPushTime = settings.UserPreference.mtime;
		}
	}

	public static UserPreferenceModel BuildMatrixShowCompletedPreference(MatrixModel remoteMatrix, bool show)
	{
		UserPreferenceModel userPreferenceModel = JsonConvert.DeserializeObject<UserPreferenceModel>(JsonConvert.SerializeObject(LocalSettings.Settings.UserPreference));
		userPreferenceModel.matrix = JsonConvert.DeserializeObject<MatrixModel>(JsonConvert.SerializeObject(remoteMatrix));
		userPreferenceModel.matrix.SetShowComplete(show);
		userPreferenceModel.matrix.mtime = Math.Max(userPreferenceModel.matrix.mtime, remoteMatrix.mtime + 1);
		userPreferenceModel.mtime = userPreferenceModel.GetMaxMTime();
		return userPreferenceModel;
	}

	public static async Task PushPendingMatrixShowCompleted()
	{
		LocalSettings settings = LocalSettings.Settings;
		string userId = settings.LoginUserId;
		string auth = settings.LoginUserAuth;
		UserPreferenceModel localPreference = settings.UserPreference;
		if (!settings.PendingMatrixShowCompleted.HasValue || !(await MatrixProfileDao.BuildSyncBeanAsync()).Empty() || !IsCurrentPreferenceAccount(userId, auth, localPreference))
		{
			return;
		}
		UserPreferenceModel remote = await PullRemotePreference(0L);
		if (!IsCurrentPreferenceAccount(userId, auth, localPreference) || remote?.matrix == null || remote.mtime == 0L || remote.matrix.defaultViewId != settings.MatrixModel?.defaultViewId)
		{
			return;
		}
		ModelSyncBean<MatrixProfileSyncModel> obj = await MatrixProfileDao.BuildSyncBeanAsync();
		bool? show = settings.PendingMatrixShowCompleted;
		if (!obj.Empty() || !show.HasValue || !IsCurrentPreferenceAccount(userId, auth, localPreference))
		{
			return;
		}
		UserPreferenceModel preference = BuildMatrixShowCompletedPreference(remote.matrix, show.Value);
		if (await Communicator.SaveUserPreference(preference) && IsCurrentPreferenceAccount(userId, auth, localPreference))
		{
			settings.MatrixModel.SetRemoteValue(preference.matrix);
			settings.UserPreference.mtime = Math.Max(settings.UserPreference.mtime, preference.mtime);
			_lastPushTime = preference.mtime;
			settings.SetChanged();
			await settings.Save();
			if (IsCurrentPreferenceAccount(userId, auth, localPreference))
			{
				await settings.AcknowledgeMatrixShowCompleted(userId, show.Value);
			}
		}
	}

	private static string GetDefaultDate(int model)
	{
		return Constants.DateValue.ToValue(model);
	}

	private static string GetWeekStart(string dayOfWeek)
	{
		return dayOfWeek.Substring(0, 3).ToUpper();
	}

	private static DayOfWeek GetWeekStartFrom(string dayOfWeek)
	{
		return dayOfWeek switch
		{
			"SUN" => DayOfWeek.Sunday, 
			"MON" => DayOfWeek.Monday, 
			"TUE" => DayOfWeek.Tuesday, 
			"WED" => DayOfWeek.Wednesday, 
			"THU" => DayOfWeek.Thursday, 
			"FRI" => DayOfWeek.Friday, 
			"SAT" => DayOfWeek.Saturday, 
			_ => DayOfWeek.Sunday, 
		};
	}

	public static bool GetShowHoliday()
	{
		return LocalSettings.Settings.EnableHoliday;
	}
}
