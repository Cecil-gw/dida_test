using System;
using Bugsnag.Clients;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF;

public static class BugsnagHelper
{
	private static bool _shutDown;

	private static readonly DelayActionHandler _delaySender;

	private static Exception _exception;

	private static bool _userSetted;

	static BugsnagHelper()
	{
		_delaySender = new DelayActionHandler();
		if (LocalSettings.Settings?.Common != null && !_userSetted)
		{
			_userSetted = true;
			WPFClient.Config.SetUser(LocalSettings.Settings.LoginUserId, LocalSettings.Settings.LoginUserName, LocalSettings.Settings.LoginUserName);
		}
		WPFClient.Config.AutoNotify = false;
		_delaySender.SetAction(delegate
		{
			if (_exception != null)
			{
				try
				{
					WPFClient.Notify(_exception);
				}
				catch (Exception)
				{
				}
			}
		});
	}

	public static void SendException(Exception e)
	{
		if (!_shutDown)
		{
			_shutDown = true;
			_exception = e;
			_delaySender.TryDoAction();
		}
	}

	public static void SetUser(UserModel user)
	{
		if (user != null && !_userSetted)
		{
			WPFClient.Config.SetUser(user.userId, user.username, user.username);
		}
	}
}
