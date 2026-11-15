using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public class UserManager
{
	private static UserModel _user;

	private static UserInfoModel _userInfo;

	public static DateTime LastInitTime;

	public static bool IsTeamUser()
	{
		if (_user != null)
		{
			return _user.teamUser;
		}
		return false;
	}

	public static bool IsFreeUser()
	{
		UserModel user = _user;
		if (user != null && user.proEndDate.HasValue)
		{
			return _user?.proEndDate.Value.Date < DateTime.Now.AddYears(-20);
		}
		return true;
	}

	public static async Task<int> GetProExpireDays()
	{
		List<string> list = new List<string> { "paypal_subscribe", "apple", "google", "stripe_subscribe", "alipay_subscribe", "wxpay_subscribe" };
		if (_user?.proEndDate.HasValue ?? false)
		{
			if (list.Contains(_user.subscribeType))
			{
				return 30;
			}
			return (int)(_user.proEndDate.Value - DateTime.Today).TotalDays;
		}
		return -1;
	}

	public static bool IsTeamActive()
	{
		if (_user != null)
		{
			if (!_user.activeTeamUser)
			{
				return _user.teamPro;
			}
			return true;
		}
		return false;
	}

	public static async Task<UserModel> GetCurrentUser(bool force = false)
	{
		await Init(init: false, force);
		return _user;
	}

	public static async Task Init(bool init, bool force = false)
	{
		if (_user == null && !string.IsNullOrEmpty(LocalSettings.Settings.LoginUserName))
		{
			_user = await UserDao.QueryUserModelListDbByIdAsync(LocalSettings.Settings.LoginUserId);
		}
		if (force || (Utils.IsNetworkAvailable() && (DateTime.Now - LastInitTime).TotalMinutes > 1.0))
		{
			UserModel user = await Communicator.GetUserStatus();
			if (!string.IsNullOrEmpty(user.userId))
			{
				LastInitTime = DateTime.Now;
				bool add = await UserDao.UpdateOrInsertUserModelListDbAsync(user);
				if (init)
				{
					OnUserDbChange(user, add);
				}
			}
			_user = await UserDao.QueryUserModelListDbByIdAsync(LocalSettings.Settings.LoginUserId);
		}
		else
		{
			_user = await UserDao.QueryUserModelListDbByIdAsync(LocalSettings.Settings.LoginUserId);
		}
	}

	public static bool ProExpired()
	{
		UserModel user = _user;
		if (user != null && user.proEndDate.HasValue && _user?.proEndDate.Value < DateTime.Now)
		{
			return _user?.proEndDate.Value.Date > DateTime.Now.AddYears(-20);
		}
		return false;
	}

	public static string GetToken()
	{
		return _user?.token;
	}

	public static string GetUserEmail()
	{
		return _user?.username;
	}

	public static async Task PullUserInfo()
	{
		UserInfoModel userInfoModel = await Communicator.GetUserInfo();
		if (userInfoModel != null)
		{
			_userInfo = userInfoModel;
			if (LocalSettings.Settings.LoginUserName != _userInfo.username)
			{
				LocalSettings.Settings.LoginUserName = _userInfo.username;
			}
			await UserDao.UpdateOrInsertUserInfoModelListDbAsync(userInfoModel);
		}
	}

	public static async Task<string> GetUserCode()
	{
		if (string.IsNullOrEmpty(LocalSettings.Settings.LoginUserId))
		{
			return null;
		}
		if (_userInfo == null || _userInfo.username != LocalSettings.Settings.LoginUserName)
		{
			_userInfo = await UserDao.GetUserByName(LocalSettings.Settings.LoginUserName);
		}
		return _userInfo?.userCode;
	}

	public static void Clear()
	{
		_user = null;
		_userInfo = null;
	}

	public static async Task<UserInfoModel> GetUserInfo(bool force = false)
	{
		if (_userInfo == null || _userInfo.username != LocalSettings.Settings.LoginUserName)
		{
			_userInfo = await UserDao.GetUserByName(LocalSettings.Settings.LoginUserName);
			if (_userInfo == null && force)
			{
				await PullUserInfo();
			}
		}
		return _userInfo;
	}

	public static async Task CheckUserP(bool force)
	{
		UserModel userModel = await GetCurrentUser(force);
		if (userModel == null)
		{
			return;
		}
		if (userModel.pro && userModel.proEndDate > DateTime.Now)
		{
			LocalSettings.Settings.IsPro = true;
			LocalSettings.Settings.DontShowProWindow = false;
			if (!userModel.gracePeriod.HasValue || !userModel.gracePeriod.Value)
			{
				LocalSettings.Settings.ExtraSettings.GraceDateNotifyDate = 0;
			}
		}
		else
		{
			LocalSettings.Settings.IsPro = CacheManager.GetTeams().Any((TeamModel t) => t.IsPro());
		}
	}

	public static bool InGraceDate()
	{
		UserModel user = _user;
		if (user != null && user.gracePeriod.HasValue && _user.gracePeriod.Value)
		{
			UserModel user2 = _user;
			if (user2 != null && user2.proEndDate.HasValue && _user?.proEndDate.Value > DateTime.Now)
			{
				UserModel user3 = _user;
				if (user3 != null && user3.noGraceDate.HasValue)
				{
					return _user?.noGraceDate.Value <= DateTime.Now;
				}
			}
		}
		return false;
	}

	public static bool IsLastGraceDate()
	{
		UserModel user = _user;
		if (user != null && user.gracePeriod.HasValue && _user.gracePeriod.Value)
		{
			UserModel user2 = _user;
			if (user2 != null && user2.noGraceDate.HasValue && _user?.noGraceDate.Value <= DateTime.Now)
			{
				return _user?.noGraceDate.Value.Date == DateTime.Now.Date;
			}
		}
		return false;
	}

	public static DateTime? GetUserGraceDate()
	{
		return _user?.noGraceDate;
	}

	public static string GetSubscribeType()
	{
		return _user?.subscribeType;
	}

	public static int? GetSubscribeErrorCode()
	{
		return _user?.errorType;
	}

	public static async Task OnUserDbChange(UserModel user, bool add)
	{
	}
}
