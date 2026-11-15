using System;
using System.Collections.Generic;
using System.Windows;
using TickTickModels;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Config;

public class SubscribeExpiredWindow : CustomerDialog
{
	private readonly bool _multiMode;

	private bool _unsubscribeMode;

	private bool _monoSubscribeMode;

	private readonly BindCalendarAccountModel _account;

	private readonly CalendarSubscribeProfileModel _subscribe;

	private readonly CalendarConnectInfo _connect;

	public SubscribeExpiredWindow(List<BindCalendarAccountModel> accounts, List<CalendarSubscribeProfileModel> subscribes, List<CalendarConnectInfo> connects)
		: base(Utils.GetString("ThirdCalendarExpired"), "", Utils.GetString("Reauthorize"), Utils.GetString("Deauthorize"))
	{
		if ((accounts?.Count ?? 0) + (subscribes?.Count ?? 0) + (connects?.Count ?? 0) > 1)
		{
			_multiMode = true;
			string text = "";
			if (accounts != null && accounts.Count > 0)
			{
				for (int i = 0; i < accounts.Count; i++)
				{
					BindCalendarAccountModel bindCalendarAccountModel = accounts[i];
					text = text + (Utils.IsCn() ? "“" : "'") + bindCalendarAccountModel.DisplayName + (Utils.IsCn() ? "”" : "'");
					if (i < accounts.Count - 1 || (subscribes != null && subscribes.Count > 0))
					{
						text += "、 ";
					}
				}
			}
			if (connects != null && connects.Count > 0)
			{
				for (int j = 0; j < connects.Count; j++)
				{
					CalendarConnectInfo calendarConnectInfo = connects[j];
					text = text + (Utils.IsCn() ? "“" : "'") + calendarConnectInfo.account + (Utils.IsCn() ? "”" : "'");
					if (j < connects.Count - 1)
					{
						text += "、 ";
					}
				}
			}
			if (subscribes != null && subscribes.Count > 0)
			{
				for (int k = 0; k < subscribes.Count; k++)
				{
					CalendarSubscribeProfileModel calendarSubscribeProfileModel = subscribes[k];
					text = text + (Utils.IsCn() ? "“" : "'") + calendarSubscribeProfileModel.Name + (Utils.IsCn() ? "”" : "'");
					if (k < subscribes.Count - 1)
					{
						text += "、 ";
					}
				}
			}
			ContentTextBlock.Text = string.Format(Utils.GetString("SubscriptionCalendarsExpiredDesc"), text);
			CancelButton.Content = Utils.GetString("GotIt");
		}
		else if (accounts != null && accounts.Count == 1)
		{
			ContentTextBlock.Text = string.Format(Utils.GetString("SubscriptionCalendarExpiredDesc"), accounts[0].DisplayName);
			_account = accounts[0];
		}
		else if (subscribes != null && subscribes.Count == 1)
		{
			_monoSubscribeMode = true;
			ContentTextBlock.Text = string.Format(Utils.GetString("CalendarExpiredTips"), subscribes[0].Name);
			_subscribe = subscribes[0];
			OkButton.Content = Utils.GetString("Resubscribe");
			CancelButton.Content = Utils.GetString("Unsubscribe");
			_unsubscribeMode = true;
		}
		else if (connects != null && connects.Count == 1)
		{
			CalendarConnectInfo calendarConnectInfo2 = connects[0];
			ContentTextBlock.Text = string.Format(Utils.GetString("SubscriptionCalendarExpiredDesc"), calendarConnectInfo2.account + ((calendarConnectInfo2.site == "notion") ? (" (" + calendarConnectInfo2.workspaceName + ")") : ""));
			_connect = calendarConnectInfo2;
		}
	}

	protected override void Cancel()
	{
		if (!_multiMode)
		{
			if (_monoSubscribeMode)
			{
				UnsubscribeCalendar(_subscribe);
			}
			else if (!_unsubscribeMode)
			{
				_unsubscribeMode = true;
				TitleTextBlock.Text = Utils.GetString("Deauthorize");
				ContentTextBlock.Text = Utils.GetString("UnsubscribedConfirmContent");
				CancelButton.Content = Utils.GetString("GotIt");
				OkButton.Content = Utils.GetString("Deauthorize");
			}
			else
			{
				Close();
			}
		}
		else
		{
			Close();
		}
	}

	protected override async void OnOk()
	{
		Close();
		if (!_multiMode && (_account != null || _subscribe != null || _connect != null))
		{
			if (_account != null)
			{
				if (!_unsubscribeMode)
				{
					ReauthorizeAccount(_account);
				}
				else
				{
					UnsubscribeAccount(_account);
				}
			}
			else if (_subscribe != null)
			{
				if (_subscribe.Kind != 2)
				{
					EditUrlWindow editUrlWindow = new EditUrlWindow(new SubscribeCalendarViewModel(_subscribe));
					editUrlWindow.Owner = base.Owner;
					editUrlWindow.ShowDialog();
				}
				else
				{
					EditHolidaySubsWindow editHolidaySubsWindow = new EditHolidaySubsWindow(new SubscribeCalendarViewModel(_subscribe));
					editHolidaySubsWindow.Owner = base.Owner;
					editHolidaySubsWindow.ShowDialog();
				}
			}
			else
			{
				if (_connect == null)
				{
					return;
				}
				if (!_unsubscribeMode)
				{
					if (_connect.site == "notion")
					{
						Utils.TryProcessToNotionConnect(null);
						return;
					}
					BindGoogleAccount instance = BindGoogleAccount.GetInstance(isSubscribe: false);
					instance.Start(_connect.account);
					instance.CalendarConnected += delegate
					{
						Utils.Toast(Utils.GetString("Integrated"));
					};
					instance.ToastText += delegate(object sender, string s)
					{
						Utils.Toast(s);
					};
				}
				else
				{
					await CalendarConnectService.Disintegration(_connect.id);
				}
			}
		}
		else
		{
			SettingDialog.ShowSettingDialog(SettingsType.Calendar);
		}
	}

	private async void UnsubscribeAccount(BindCalendarAccountModel account)
	{
		await SubscribeCalendarHelper.UnbindCalendar(account.Id);
		ListViewContainer.ReloadProjectData();
	}

	private async void UnsubscribeCalendar(CalendarSubscribeProfileModel calendar)
	{
		await SubscribeCalendarHelper.UnsubscribeCalendar(calendar.Id);
		App.Window.TryReloadCalendar();
		Close();
	}

	private void ReauthorizeAccount(BindCalendarAccountModel account)
	{
		if (account.IsBindAccountPassword())
		{
			BindAccountWindow bindAccountWindow = new BindAccountWindow(account);
			bindAccountWindow.Owner = base.Owner;
			bindAccountWindow.ShowDialog();
		}
		else if (account.Site == "outlook")
		{
			BindOutlookWindow bindOutlookWindow = new BindOutlookWindow();
			bindOutlookWindow.Owner = base.Owner;
			bindOutlookWindow.SetOriginAccount(account.Id);
			bindOutlookWindow.ShowDialog();
		}
		else if (account.Site == "google")
		{
			BindGoogleAccount instance = BindGoogleAccount.GetInstance();
			instance.Start(account.Id);
			instance.ToastText += delegate(object sender, string s)
			{
				Utils.Toast(s);
			};
		}
		else if (account.Kind == "icloud")
		{
			BindICloudWindow bindICloudWindow = new BindICloudWindow(null, account);
			bindICloudWindow.Owner = base.Owner;
			bindICloudWindow.Topmost = false;
			bindICloudWindow.ShowDialog();
		}
	}

	public static void TryShowWindow(List<BindCalendarAccountModel> expired, List<CalendarSubscribeProfileModel> subscribes = null, List<CalendarConnectInfo> connects = null, bool setCheckTime = true)
	{
		if (setCheckTime)
		{
			LocalSettings.Settings.ExtraSettings.BindCalendarExpireCheckTime = DateUtils.GetDateNum(DateTime.Today);
			LocalSettings.Settings.Save(force: true);
		}
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			SubscribeExpiredWindow subscribeExpiredWindow = new SubscribeExpiredWindow(expired, subscribes, connects)
			{
				Topmost = false
			};
			try
			{
				subscribeExpiredWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				subscribeExpiredWindow.Owner = Utils.GetToastWindow() as Window;
				subscribeExpiredWindow.Show();
			}
			catch
			{
				subscribeExpiredWindow.Owner = null;
				subscribeExpiredWindow.Show();
			}
		});
	}
}
