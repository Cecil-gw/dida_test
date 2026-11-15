using System;
using System.Threading.Tasks;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Calendar;

public class BindGoogleAccount
{
	private static readonly object _locker = new object();

	private static BindGoogleAccount _instance;

	public EventHandler<EventArgs> Finished;

	private string _accountId;

	private bool _isSubscribe = true;

	public event EventHandler<CalendarConnectInfo> CalendarConnected;

	public event EventHandler<string> ToastText;

	private BindGoogleAccount()
	{
		App.RegisterURIProtocol();
	}

	public static BindGoogleAccount GetInstance(bool isSubscribe = true)
	{
		if (_instance == null)
		{
			lock (_locker)
			{
				if (_instance == null)
				{
					_instance = new BindGoogleAccount();
				}
			}
		}
		_instance._isSubscribe = isSubscribe;
		_instance.CalendarConnected = null;
		_instance.ToastText = null;
		return _instance;
	}

	public void Start(string accountId = null)
	{
		_accountId = accountId;
		UriNotifier.Uri -= OnUri;
		UriNotifier.Uri += OnUri;
		bool num = Utils.IsDida();
		string domain = BaseUrl.Domain;
		string text = (num ? "713670663672-vbvi5l69rgqo1u1dhg0fcp08u7h60ldn" : "366263775281");
		Utils.TryProcessStartUrl("https://accounts.google.com/o/oauth2/v2/auth?scope=https://www.googleapis.com/auth/calendar&redirect_uri=https://" + domain + "/pub/calendar/bind&response_type=code&prompt=select_account%20consent&state=ios_google&access_type=offline&client_id=" + text + ".apps.googleusercontent.com");
	}

	private async void OnUri(object sender, UriModel e)
	{
		if (!e.Path.StartsWith("v1/calendar/bind"))
		{
			return;
		}
		UriNotifier.Uri -= OnUri;
		App.Window.TryShowMainWindow(force: false, delay: true);
		string code = e.Parmas["code"];
		if (_isSubscribe)
		{
			BindCalendarAccountModel calendar = await SubscribeCalendarHelper.BindCalendarAccount(code);
			if (calendar != null)
			{
				BindCalendarAccountModel account = CacheManager.GetAccountCalById(calendar.Id);
				UserActCollectUtils.AddClickEvent("subscribe_calendar", "google", "subscribe_success");
				if (!string.IsNullOrEmpty(_accountId))
				{
					BindCalendarAccountDao.DeleteBindAccountById(calendar.Id);
					BindCalendarAccountDao.DeleteBindAccountById(_accountId);
					_accountId = string.Empty;
				}
				await SubscribeCalendarHelper.SaveBindAccount(calendar);
				CalendarService.PullAccountEvents(calendar.Id, null, calendar.Site);
				this.ToastText?.Invoke(this, Utils.GetString((account == null || account.Expired) ? "SuccessfullySubscribed" : "AccountHasSubscribed"));
			}
			else
			{
				Toast(Utils.GetString("VerificationFailedHaveTry"));
			}
		}
		else
		{
			CalendarConnectInfo calendar2 = await Communicator.GetCalendarConnect(code, "google");
			if (calendar2 != null)
			{
				UserActCollectUtils.AddClickEvent("subscribe_calendar", "google", "integrate_success");
				string text = await CalendarConnectService.SaveConnect(calendar2);
				if (!string.IsNullOrEmpty(text))
				{
					this.ToastText?.Invoke(this, text);
				}
				this.CalendarConnected?.Invoke(this, calendar2);
			}
			else
			{
				Toast(Utils.GetString("VerificationFailedHaveTry"));
			}
		}
		Finished?.Invoke(this, null);
	}

	public void Toast(string msg)
	{
		Task.Delay(300);
		if (!SettingDialog.TryToast(SettingsType.Calendar, msg))
		{
			Utils.Toast(msg);
		}
	}

	public static void ClearEvent()
	{
		GetInstance();
	}
}
