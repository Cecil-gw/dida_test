using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Util.Sync;

public class SyncWebSocket : TickWebSocket
{
	private PushModel _pushModel;

	private string lastDataMsgId { get; set; }

	public SyncWebSocket(string pushUrl)
		: base(pushUrl)
	{
	}

	protected override bool IsConnectId(string msg)
	{
		if (!msg.StartsWith("{"))
		{
			return msg.Split('-').Length >= 4;
		}
		return false;
	}

	protected override async Task Register(string msg)
	{
		_pushModel = await Communicator.RegisterPushService(_pushModel?.id ?? string.Empty, msg);
		UtilLog.Info("SyncWebSocket.OnMessageReceived() RegisterPushService channelId=" + msg + ", " + _pushModel);
	}

	protected override async Task HandleMessage(string message)
	{
		WSReminderModel reminderModel = null;
		JObject jObject = null;
		try
		{
			reminderModel = JsonConvert.DeserializeObject<WSReminderModel>(message);
		}
		catch (Exception)
		{
		}
		try
		{
			jObject = JObject.Parse(message);
		}
		catch (Exception)
		{
		}
		if (jObject != null && jObject.TryGetValue("type", out JToken value))
		{
			UtilLog.Info("SyncWebSocket.HandleMessage()" + value);
			switch (value.ToString())
			{
			case "paymentUpdate":
				App.Window.HandlePaymentUpdated().ContinueWith(UtilRun.LogTask);
				CheckNotification(syncBeforeIndication: false);
				break;
			case "team":
			case "share":
			case "sn":
			case "notification":
				if (value.ToString() == "sn")
				{
					string text = jObject["data"]["id"].ToString();
					if (text == lastDataMsgId)
					{
						return;
					}
					lastDataMsgId = text;
					string text2 = jObject["data"]["type"].ToString();
					if (text2 == "subscribeCharge" || text2 == "PayReminder")
					{
						NotificationModel notificationModel = JsonConvert.DeserializeObject<NotificationModel>(jObject["data"].ToString());
						await ReminderBase.ShowReminderWindow(new ReminderModel
						{
							Type = ReminderModel.RemindType.PayRemind,
							Title = Utils.GetAppName(),
							Content = notificationModel.title
						});
					}
				}
				CheckNotification();
				break;
			case "needSync":
				HandleNeedSync();
				break;
			case "habit":
				HabitSyncService.DelaySync();
				break;
			case "test":
			{
				Communicator.NotifyPushArrive(new PushArriveModel
				{
					id = (jObject.TryGetValue("data", out JToken value5) ? value5.ToString() : string.Empty),
					model = Utils.GetWindowsVersion(),
					osVersion = Utils.VersionNumToDouble() + string.Empty,
					time = DateTime.Now
				});
				break;
			}
			case "preference":
				await SyncService.PullPreferenceChangesAsync();
				break;
			case "timetable":
				ScheduleService.GetRemoteSchedules();
				break;
			case "countdown":
				CountdownSyncService.SyncCountdowns();
				break;
			case "calendar":
				await CalendarService.PullSubscribeCalendars();
				await CalendarService.PullAccountCalendarsAndEvents();
				ListViewContainer.ReloadProjectData();
				break;
			case "calendar.events.refresh":
			{
				if (jObject.TryGetValue("data", out JToken value6) && value6 is JObject jObject2 && jObject2.TryGetValue("accountId", out JToken value7))
				{
					await CalendarService.TryPullAccountCacheEvents(value7.ToString());
				}
				break;
			}
			case "ab":
			{
				if (jObject.TryGetValue("type", out JToken value2) && jObject.TryGetValue("testCode", out JToken value3) && jObject.TryGetValue("planCode", out JToken value4) && value2.ToString() == "change")
				{
					Dictionary<string, string> aBTestData = LocalSettings.Settings.ExtraSettings.ABTestData;
					if (aBTestData.ContainsKey(value3.ToString()))
					{
						aBTestData[value3.ToString()] = value4.ToString().Trim();
					}
					LocalSettings.Settings.ExtraSettings.ABTestData = aBTestData;
					LocalSettings.Settings.Save();
				}
				break;
			}
			}
		}
		if (reminderModel?.data == null)
		{
			return;
		}
		try
		{
			UtilLog.Info("SyncWebSocket.HandleMessage()" + reminderModel.data.type);
			GlobalEventManager.NotifyRemindHandled(reminderModel.data);
			AnnoyingReminderManager.OnRemoteHandled(reminderModel.data);
			switch (reminderModel.data.type)
			{
			case "calendar":
			{
				CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventIdOrId(reminderModel.data.id);
				if (calendarEventModel != null)
				{
					SystemToastUtils.RemoveToast(reminderModel.data.id, calendarEventModel.CalendarId);
				}
				break;
			}
			case "task":
			{
				TaskModel taskModel2 = await TaskDao.GetThinTaskById(reminderModel.data.id);
				if (taskModel2 != null)
				{
					SystemToastUtils.RemoveToast(taskModel2.id, taskModel2.projectId);
				}
				break;
			}
			case "checklist":
			{
				TaskDetailItemModel checkitem = await TaskDetailItemDao.GetCheckItemById(reminderModel.data.id);
				if (checkitem != null)
				{
					TaskModel taskModel = await TaskDao.GetThinTaskById(checkitem.TaskServerId);
					if (taskModel != null)
					{
						SystemToastUtils.RemoveToast(checkitem.id, taskModel.projectId);
					}
				}
				break;
			}
			case "habit":
				SystemToastUtils.RemoveHabitToast(reminderModel.data.id);
				break;
			case "pomo":
				SystemToastUtils.RemoveToast("focus", "focus");
				TickFocusManager.HidePomoReminder();
				break;
			case "countdown":
				SystemToastUtils.RemoveCountdown(reminderModel.data.id);
				break;
			}
		}
		catch (Exception ex3)
		{
			UtilLog.Error(ex3);
		}
	}

	private static async void CheckNotification(bool syncBeforeIndication = true)
	{
		Utils.RunOnUiThread(Application.Current?.Dispatcher, SetNotificationIndication);
		async void SetNotificationIndication()
		{
			NotificationUnreadCount notificationCount = await Communicator.GetNotificationCount();
			if (notificationCount != null && (notificationCount.Activity > 0 || notificationCount.Notification > 0))
			{
				if (syncBeforeIndication)
				{
					await SyncManager.Sync();
				}
				App.Window.LeftTabBar.ShowNotificationIndicator(notificationCount);
			}
		}
	}

	private static void HandleNeedSync()
	{
		Utils.RunOnUiThread(App.Window.Dispatcher, delegate
		{
			SyncManager.TryDelaySync(1000);
			ReminderCalculator.AssembleReminders();
		});
	}

	public override void SendHello()
	{
		SendTextAsync("hello").ContinueWith(UtilRun.LogTask);
	}
}
