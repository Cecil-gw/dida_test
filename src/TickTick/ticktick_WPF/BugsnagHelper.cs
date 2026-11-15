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
	}

	public static void SendException(Exception e)
	{
	}

	public static void SetUser(UserModel user)
	{
	}
}
