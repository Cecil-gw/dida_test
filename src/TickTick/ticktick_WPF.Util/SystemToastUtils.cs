using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using TickTickUtils;
using TickTickUtils.Lang;
using Windows.UI.Notifications;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Toast;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Util;

public static class SystemToastUtils
{
	private static string _logoUrl;

	private static string _appid;

	private static readonly object RemindingLocker;

	private static Dictionary<string, RemindMessage> RemindingDict;

	static SystemToastUtils()
	{
		_appid = "滴答清单";
		RemindingLocker = new object();
		RemindingDict = new Dictionary<string, RemindMessage>();
		if (Utils.IsWindows7())
		{
			return;
		}
		try
		{
			DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>(_appid);
			DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();
			string text = ((string.IsNullOrEmpty(LocalSettings.Settings.Common.AppIconKey) || LocalSettings.Settings.Common.AppIconKey == AppIconKey.Default.ToString()) ? AppIconKey.SClassic.ToString() : LocalSettings.Settings.Common.AppIconKey);
			_logoUrl = AppPaths.AppIconDir + text + ".png";
			ToastNotificationManagerCompat.OnActivated += delegate(ToastNotificationActivatedEventArgsCompat toastArgs)
			{
				ToastArguments toastArguments = ToastArguments.Parse(toastArgs.Argument);
				OnToastCallBack(kvs: toastArgs.UserInput?.ToList(), arg: toastArguments["action"]);
			};
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		AppIconUtils.TrySetToastIcon();
	}

	public static void SetLogoUrl()
	{
		string text = ((string.IsNullOrEmpty(LocalSettings.Settings.Common.AppIconKey) || LocalSettings.Settings.Common.AppIconKey == AppIconKey.Default.ToString()) ? AppIconKey.SClassic.ToString() : LocalSettings.Settings.Common.AppIconKey);
		_logoUrl = AppPaths.AppIconDir + text + ".png";
	}

	public static async void OnToastCallBack(string arg, List<KeyValuePair<string, object>> kvs)
	{
		UtilLog.Info(kvs?.Aggregate("SystemToastCallBack " + arg, (string current, KeyValuePair<string, object> kv) => $",{current},{kv.Key},{kv.Value}") ?? ("SystemToastCallBack " + arg));
		ToastOptionModel optionModel = ToastOptionModel.FromJson(arg);
		RemindMessage remindMessage = await HandleReminderToastClosed(optionModel?.Id ?? arg, (optionModel == null) ? "content" : "button");
		if (optionModel == null)
		{
			if (remindMessage == null)
			{
				return;
			}
			Dispatcher dispatcher = Application.Current?.Dispatcher;
			if (dispatcher != null)
			{
				await dispatcher.InvokeAsync((Func<Task>)async delegate
				{
					await TryShowDetail(remindMessage.id, remindMessage.type);
				}).Task.Unwrap();
			}
			return;
		}
		Dispatcher dispatcher2 = Application.Current?.Dispatcher;
		if (dispatcher2 == null)
		{
			return;
		}
		await dispatcher2.InvokeAsync((Func<Task>)async delegate
		{
			IToastHandler toastHandler = ToastHandlerBuilder.Build(optionModel.ToastType);
			if (toastHandler != null)
			{
				await toastHandler.Exec(optionModel, kvs);
			}
		}).Task.Unwrap();
	}

	internal static async Task<RemindMessage> HandleReminderToastClosed(string id, string reason)
	{
		RemindMessage remindMessage = null;
		if (!string.IsNullOrEmpty(id))
		{
			lock (RemindingLocker)
			{
				if (RemindingDict.ContainsKey(id))
				{
					remindMessage = RemindingDict[id];
					RemindingDict.Remove(id);
				}
			}
		}
		if (remindMessage != null)
		{
			await Communicator.TryNotifyCloseReminder(remindMessage);
			AnnoyingReminderManager.OnRemoteHandled(remindMessage);
		}
		else
		{
			AnnoyingReminderManager.OnUserHandled(id, reason);
		}
		return remindMessage;
	}

	public static async Task TryShowDetail(string id, string type)
	{
		string ctype = "task_reminder_system";
		switch (type)
		{
		case "task":
			if (await TaskDao.GetThinTaskById(id) != null)
			{
				App.ShowMainWindow(id);
			}
			break;
		case "checklist":
		{
			TaskDetailItemModel taskDetailItemModel = await TaskDetailItemDao.GetChecklistItemById(id);
			if (taskDetailItemModel != null)
			{
				App.ShowMainWindow(taskDetailItemModel.TaskServerId);
			}
			break;
		}
		case "calendar":
		{
			CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventId(id);
			if (calendarEventModel != null)
			{
				App.NavigateEvent(calendarEventModel.Id);
			}
			ctype = "calendar_reminder_system";
			break;
		}
		case "habit":
		{
			HabitModel habitModel = await HabitDao.GetHabitById(id);
			if (habitModel != null)
			{
				App.NavigateHabit(habitModel.Id);
			}
			ctype = "habit_reminder_system";
			break;
		}
		case "countdown":
			App.NavigateCountdown();
			break;
		case "course":
			ctype = "timetable_reminder_system";
			break;
		case "payremind":
			SettingDialog.ShowSettingDialog(SettingsType.Premium, App.Window);
			break;
		}
		UserActCollectUtils.AddClickEvent("reminder", ctype, "click_content");
	}

	public static async Task SystemToast(ReminderModel reminder, bool playSound)
	{
		if (reminder != null && reminder.Type == ReminderModel.RemindType.PayRemind)
		{
			ToastNormalMsg(reminder);
		}
		else
		{
			if (reminder == null || !reminder.ReminderTime.HasValue)
			{
				return;
			}
			if (reminder.Type == ReminderModel.RemindType.Calendar)
			{
				await ToastEvent(reminder);
				UserActCollectUtils.AddClickEvent("reminder_data", "type", "calendar_reminder");
				return;
			}
			UserActCollectUtils.AddClickEvent("reminder_data", "type", "task_reminder");
			TaskModel task = await TaskDao.GetThinTaskById(reminder.TaskId);
			bool showButton = true;
			if (task == null)
			{
				return;
			}
			string title = (reminder.IsEnd ? string.Format(Utils.GetString("CompletedA?"), task.title) : task.title);
			string content = null;
			if (reminder.ReminderTime != reminder.StartDate)
			{
				content = DateUtils.GetTaskActivityTimeStringForToast(reminder.StartDate ?? reminder.ReminderTime ?? DateTime.Now, reminder.DueDate, reminder.IsAllDay, task.timeZone, task.isFloating == true);
				content += Environment.NewLine;
			}
			if (reminder.Type == ReminderModel.RemindType.Task)
			{
				content += TaskUtils.ReplaceAttachmentTextInString((task.kind != "CHECKLIST") ? task.content : task.desc);
			}
			else
			{
				TaskDetailItemModel taskDetailItemModel = await TaskDetailItemDao.GetChecklistItemById(reminder.CheckItemId);
				if (taskDetailItemModel == null)
				{
					return;
				}
				content = content + "- " + taskDetailItemModel.title;
			}
			if (!string.IsNullOrEmpty(task.attendId) && !AgendaHelper.CanAccessAgenda(task))
			{
				showButton = false;
			}
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.projectId);
			if (projectModel != null && !projectModel.IsEnable())
			{
				showButton = false;
			}
			bool flag = LocalSettings.Settings.ExtraSettings.RemindDetail == 2;
			if (!flag)
			{
				bool flag2 = LocalSettings.Settings.ExtraSettings.RemindDetail == 1;
				if (flag2)
				{
					flag2 = await AppLockCache.GetAppLocked();
				}
				flag = flag2;
			}
			if (flag)
			{
				title = Utils.GetString("ReminderWhenLock");
				content = string.Empty;
				showButton = false;
			}
			TryToast(reminder, _logoUrl, title, content, showButton, task.projectId, task.kind != "NOTE", playSound);
		}
	}

	private static async Task ToastEvent(ReminderModel reminder)
	{
		if (reminder == null)
		{
			return;
		}
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(reminder.EventId);
		if (calendarEventModel == null)
		{
			calendarEventModel = await CalendarEventDao.GetEventByEventId(reminder.EventId);
		}
		CalendarEventModel model = calendarEventModel;
		bool flag = LocalSettings.Settings.ExtraSettings.RemindDetail == 2;
		if (!flag)
		{
			bool flag2 = LocalSettings.Settings.ExtraSettings.RemindDetail == 1;
			if (flag2)
			{
				flag2 = await AppLockCache.GetAppLocked();
			}
			flag = flag2;
		}
		bool flag3 = flag;
		if (model != null && reminder.ReminderTime.HasValue)
		{
			string title = (flag3 ? Utils.GetString("ReminderWhenLock") : model.Title);
			string content = (flag3 ? string.Empty : model.Content);
			bool showButton = !flag3;
			TryToast(reminder, _logoUrl, title, content, showButton, model.CalendarId);
		}
	}

	public static void ToastPomo(bool isCompleted, bool automatic)
	{
		ToastOptionModel toastOptionModel = new ToastOptionModel
		{
			ToastType = ((!isCompleted) ? ToastType.PomoRelaxCompleted : ToastType.PomoCompleted)
		};
		string content;
		string content2;
		if (isCompleted)
		{
			content = Utils.GetString("TimeToRelax");
			content2 = Utils.GetString("Relax");
		}
		else
		{
			content = Utils.GetString("TimeToFocus");
			content2 = Utils.GetString("StartFocus");
		}
		RemovePomoToast();
		if (automatic)
		{
			SystemToastManager.Notify("focus", "focus", _logoUrl, string.Empty, content, null, null, silent: true);
			Task.Delay(3000).ContinueWith(delegate
			{
				RemovePomoToast();
			});
			return;
		}
		SystemToastManager.Notify("focus", "focus", _logoUrl, string.Empty, content, null, new List<ToastCommands>
		{
			new ToastCommands
			{
				Content = content2,
				Argument = toastOptionModel.ToJson("Complete")
			},
			new ToastCommands
			{
				Content = Utils.GetString("Exit"),
				Argument = toastOptionModel.ToJson("Exit")
			}
		}, silent: true);
	}

	public static async Task ToastHabit(ReminderModel reminder, bool playSound)
	{
		HabitModel habitModel = await HabitDao.GetHabitById(reminder.HabitId);
		if (habitModel == null)
		{
			return;
		}
		string fileName = AppPaths.DataDir + "Image\\" + habitModel.Id + ".png";
		Application.Current?.Dispatcher.Invoke(delegate
		{
			BitmapSource icon = HabitService.GetIcon(habitModel.IconRes, habitModel.Color);
			ThemeUtil.SaveBitmapToPng(fileName, icon);
		});
		ToastOptionModel optModel = new ToastOptionModel(reminder);
		string unit = HabitUtils.GetUnitText(habitModel.Unit);
		List<HabitCheckInModel> source = await HabitCheckInDao.GetHabitCheckInsByHabitId(habitModel.Id);
		double todayCheckCount = 0.0;
		if (source.Any())
		{
			HabitCheckInModel habitCheckInModel = source.FirstOrDefault((HabitCheckInModel checkIn) => checkIn.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			if (habitCheckInModel != null)
			{
				todayCheckCount = habitCheckInModel.GetDisplayValue();
			}
		}
		HabitCheckInType checkInType = HabitUtils.GetHabitCheckInType(0, habitModel.Type, habitModel.Step, habitModel.Goal);
		Action notify = null;
		bool flag = LocalSettings.Settings.ExtraSettings.RemindDetail == 2;
		if (!flag)
		{
			bool flag2 = LocalSettings.Settings.ExtraSettings.RemindDetail == 1;
			if (flag2)
			{
				flag2 = await AppLockCache.GetAppLocked();
			}
			flag = flag2;
		}
		if (flag)
		{
			notify = delegate
			{
				SystemToastManager.Notify(optModel.Id, "habit", _logoUrl, Utils.GetString("ReminderWhenLock"), string.Empty, null, null, !playSound);
			};
		}
		else
		{
			DateTime date = reminder.ReminderTime ?? DateTime.Now;
			Dictionary<string, List<ToastCommands>> reminders = GetRemindersByTime(date, withTomorrow: false);
			switch (checkInType)
			{
			case HabitCheckInType.BoolCompleted:
			case HabitCheckInType.BoolUnCompleted:
			case HabitCheckInType.CompletedAllCompleted:
			case HabitCheckInType.CompletedAllUnCompleted:
				optModel.ToastType = ToastType.BoolHabitCompleted;
				notify = delegate
				{
					SystemToastManager.Notify(optModel.Id, "habit", fileName, habitModel.Name, string.Empty, reminders, new List<ToastCommands>
					{
						new ToastCommands
						{
							Content = Utils.GetString("PublicComplete"),
							Argument = optModel.ToJson("Complete")
						},
						new ToastCommands
						{
							Content = Utils.GetString("RemindLater"),
							Argument = optModel.ToJson("Delay")
						}
					}, !playSound);
				};
				break;
			case HabitCheckInType.AutoCompleted:
			case HabitCheckInType.AutoUncompleted:
				optModel.ToastType = ToastType.MultiHabitCompleted;
				notify = delegate
				{
					SystemToastManager.Notify(optModel.Id, "habit", fileName, habitModel.Name, $"{todayCheckCount} / {habitModel.Goal} {unit}", reminders, new List<ToastCommands>
					{
						new ToastCommands
						{
							Content = $"+{habitModel.Step} {unit}",
							Argument = optModel.ToJson("AddStep")
						},
						new ToastCommands
						{
							Content = Utils.GetString("ManuallyRecord"),
							Argument = optModel.ToJson("Manual")
						},
						new ToastCommands
						{
							Content = Utils.GetString("RemindLater"),
							Argument = optModel.ToJson("Delay")
						}
					}, !playSound);
				};
				break;
			case HabitCheckInType.ManualCompleted:
			case HabitCheckInType.ManualUnCompleted:
				optModel.ToastType = ToastType.MultiHabitCompleted;
				notify = delegate
				{
					SystemToastManager.Notify(optModel.Id, "habit", fileName, habitModel.Name, $"{todayCheckCount}/{habitModel.Goal} {unit}", reminders, new List<ToastCommands>
					{
						new ToastCommands
						{
							Content = Utils.GetString("ManuallyRecord"),
							Argument = optModel.ToJson("Manual")
						},
						new ToastCommands
						{
							Content = Utils.GetString("RemindLater"),
							Argument = optModel.ToJson("Delay")
						}
					}, !playSound);
				};
				break;
			}
		}
		if (notify != null)
		{
			RemoveHabitToast(reminder.HabitId);
			notify();
			RemindMessage value = new RemindMessage(reminder);
			if (!string.IsNullOrEmpty(optModel.Id))
			{
				try
				{
					lock (RemindingLocker)
					{
						RemindingDict[optModel.Id] = value;
					}
				}
				catch (Exception)
				{
					UtilLog.Info($"SystemToast.TryToast() : RemindingDict {RemindingDict == null} , optModel.Id {optModel.Id == null}");
				}
			}
		}
		Task.Delay(3000).ContinueWith(delegate
		{
			File.Delete(fileName);
		});
	}

	private static Dictionary<string, List<ToastCommands>> GetRemindersByTime(DateTime date, bool withTomorrow = true)
	{
		switch (date.Hour * 60 + date.Minute)
		{
		default:
		{
			List<ToastCommands> list = new List<ToastCommands>
			{
				new ToastCommands
				{
					Content = Utils.GetString("Snooze15m"),
					Argument = "15mins"
				},
				new ToastCommands
				{
					Content = Utils.GetString("Snooze30m"),
					Argument = "30mins"
				},
				new ToastCommands
				{
					Content = Utils.GetString("Snooze1h"),
					Argument = "1hr"
				},
				new ToastCommands
				{
					Content = Utils.GetString("Snooze3h"),
					Argument = "3hrs"
				}
			};
			Dictionary<string, List<ToastCommands>> result = new Dictionary<string, List<ToastCommands>> { { "Snooze", list } };
			if (withTomorrow)
			{
				list.Add(new ToastCommands
				{
					Content = string.Format(Utils.GetString("SnoozeUntil"), Utils.GetString("Tomorrow")),
					Argument = "1day"
				});
			}
			return result;
		}
		}
	}

	private static void ToastNormalMsg(ReminderModel reminder)
	{
		TryToast(reminder, _logoUrl, _appid, reminder.Content, showButton: false, "normal");
	}

	private static void TryToast(ReminderModel reminderModel, string img, string title, string content, bool showButton, string projectId, bool isTask = false, bool playSound = true, bool showTomorrow = true)
	{
		content = MarkdownUtils.RemoveFoldedMetadataComments(content);
		string text = title;
		if (text != null && text.Length > 128)
		{
			title = title.Substring(0, 127);
		}
		string text2 = content;
		if (text2 != null && text2.Length > 256)
		{
			content = content.Substring(0, 255);
		}
		ToastOptionModel optModel = new ToastOptionModel(reminderModel);
		Action action;
		if (showButton)
		{
			DateTime date = reminderModel.ReminderTime ?? DateTime.Now;
			Dictionary<string, List<ToastCommands>> reminders = GetRemindersByTime(date, showTomorrow);
			action = delegate
			{
				SystemToastManager.Notify(optModel.Id, projectId, img, title ?? "", content ?? "", reminders, isTask ? new List<ToastCommands>
				{
					new ToastCommands
					{
						Content = Utils.GetString((reminderModel.Type == ReminderModel.RemindType.Checklist) ? "CompleteCheckItem" : "TaskComplete"),
						Argument = optModel.ToJson("Complete")
					},
					new ToastCommands
					{
						Content = Utils.GetString("RemindLater"),
						Argument = optModel.ToJson("Delay")
					}
				} : new List<ToastCommands>
				{
					new ToastCommands
					{
						Content = Utils.GetString("GotIt"),
						Argument = optModel.ToJson("GotIt")
					},
					new ToastCommands
					{
						Content = Utils.GetString("RemindLater"),
						Argument = optModel.ToJson("Delay")
					}
				}, !playSound);
			};
		}
		else
		{
			action = delegate
			{
				SystemToastManager.Notify(optModel.Id, projectId, img, title ?? "", content ?? "", null, null, !playSound);
			};
		}
		RemindMessage remindMessage = new RemindMessage(reminderModel);
		RemoveToast(remindMessage.id, projectId);
		action();
		if (string.IsNullOrEmpty(optModel.Id))
		{
			return;
		}
		try
		{
			lock (RemindingLocker)
			{
				RemindingDict[optModel.Id] = remindMessage;
			}
		}
		catch (Exception)
		{
			UtilLog.Info($"SystemToast.TryToast() : RemindingDict {RemindingDict == null} , optModel.Id {optModel.Id == null}");
		}
	}

	public static void ClearToastHistory()
	{
		if (!Utils.IsWindows7())
		{
			try
			{
				ToastNotificationManagerCompat.History.Clear();
			}
			catch (Exception)
			{
			}
		}
	}

	public static void RemoveHabitToast(string habitId)
	{
		if (Utils.IsWindows7())
		{
			return;
		}
		try
		{
			List<KeyValuePair<string, RemindMessage>> list;
			lock (RemindingLocker)
			{
				list = RemindingDict.ToList();
			}
			foreach (KeyValuePair<string, RemindMessage> item in list)
			{
				if (item.Value.id == habitId)
				{
					ToastNotificationManagerCompat.History.Remove(item.Key, "habit");
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void RemoveCountdown(string countdownId)
	{
		if (Utils.IsWindows7())
		{
			return;
		}
		try
		{
			List<KeyValuePair<string, RemindMessage>> list;
			lock (RemindingLocker)
			{
				list = RemindingDict.ToList();
			}
			foreach (KeyValuePair<string, RemindMessage> item in list)
			{
				if (item.Value.id == countdownId)
				{
					ToastNotificationManagerCompat.History.RemoveGroup(countdownId);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void RemovePomoToast()
	{
		if (!Utils.IsWindows7())
		{
			try
			{
				ToastNotificationManagerCompat.History.RemoveGroup("focus");
			}
			catch (Exception)
			{
			}
		}
	}

	public static void RemoveToast(string id, string projectId)
	{
		if (Utils.IsWindows7())
		{
			return;
		}
		try
		{
			List<KeyValuePair<string, RemindMessage>> list;
			lock (RemindingLocker)
			{
				list = RemindingDict.ToList();
			}
			foreach (KeyValuePair<string, RemindMessage> item in list)
			{
				if (item.Value.id == id)
				{
					ToastNotificationManagerCompat.History.Remove(item.Key, projectId);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void RemoveProjectToast(string projectId)
	{
		if (!Utils.IsWindows7())
		{
			try
			{
				ToastNotificationManagerCompat.History.RemoveGroup(projectId);
			}
			catch (Exception)
			{
			}
		}
	}

	public static bool CheckSystemToastEnable()
	{
		if (Utils.IsWindows7())
		{
			return false;
		}
		return CheckToastEnable();
	}

	private static bool CheckToastEnable()
	{
		try
		{
			return ToastNotificationManagerCompat.CreateToastNotifier().Setting == NotificationSetting.Enabled;
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return false;
	}

	public static async Task ToastCountdown(ReminderModel reminder, bool playSound, bool showButton = true)
	{
		string content = reminder.Content;
		CountdownModel countdownModel = await CountdownDao.GetCountdownById(reminder.Id);
		if (countdownModel != null && !string.IsNullOrEmpty(countdownModel.Remark))
		{
			content = content + Environment.NewLine + countdownModel.Remark;
		}
		TryToast(reminder, _logoUrl, reminder.Title, content, showButton, reminder.Id, isTask: false, playSound, showTomorrow: false);
	}

	public static async Task ToastCourse(ReminderModel reminder, bool playSound, bool showButton = true)
	{
		TryToast(reminder, _logoUrl, reminder.Title, reminder.Content, showButton, reminder.GroupId, isTask: false, playSound, showTomorrow: false);
	}
}
