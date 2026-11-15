using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public static class UserDao
{
	public static async Task AdjustCheckPointAfterLogout(string userId)
	{
		UserModel userModel = await QueryUserModelListDbByIdAsync(userId);
		if (userModel != null && userModel.checkPoint > 0)
		{
			userModel.checkPoint -= 86400000L;
			await App.Connection.UpdateAsync(userModel);
		}
	}

	public static async Task SaverUserSyncCheckPoint(string userId, long checkpoint)
	{
		UserModel userModel = await QueryUserModelListDbByIdAsync(userId);
		if (userModel != null)
		{
			userModel.checkPoint = checkpoint;
			await App.Connection.UpdateAsync(userModel);
		}
	}

	public static async Task SaveColumnCheckPoint(string userId, long checkPoint)
	{
		UserModel userModel = await QueryUserModelListDbByIdAsync(userId);
		if (userModel != null)
		{
			userModel.columnCheckPoint = checkPoint;
			await App.Connection.UpdateAsync(userModel);
		}
	}

	public static async Task<long> GetColumnCheckPoint(string userId)
	{
		return (await QueryUserModelListDbByIdAsync(userId))?.columnCheckPoint ?? 0;
	}

	public static async Task<long> GetUserSyncCheckPoint(string userId)
	{
		return (await QueryUserModelListDbByIdAsync(userId))?.checkPoint ?? 0;
	}

	public static async Task<bool> UpdateOrInsertUserModelListDbAsync(UserModel user)
	{
		try
		{
			List<UserModel> list = await (from v in App.Connection.Table<UserModel>()
				where v.userId.Equals(user.userId)
				select v).ToListAsync();
			if (list.Count != 0)
			{
				user.checkPoint = list[0].checkPoint;
				user.columnCheckPoint = list[0].columnCheckPoint;
				await App.Connection.UpdateAsync(user);
				return false;
			}
			await App.Connection.InsertAsync(user);
			return true;
		}
		catch (Exception ex)
		{
			UtilLog.Error("UpdateOrInsertUserModelListDbAsync:" + ex);
			return false;
		}
	}

	public static async Task UpdateOrInsertUserInfoModelListDbAsync(UserInfoModel user)
	{
		await Task.Run(async delegate
		{
			_ = 2;
			try
			{
				if ((await (from v in App.Connection.Table<UserInfoModel>()
					where v.userCode.Equals(user.userCode)
					select v).ToListAsync()).Count != 0)
				{
					return await App.Connection.UpdateAsync(user);
				}
				await App.Connection.InsertAsync(user);
				return 1;
			}
			catch (Exception)
			{
				return -1;
			}
		});
	}

	public static async Task<UserModel> QueryUserModelListDbByIdAsync(string userId)
	{
		if (string.IsNullOrEmpty(userId))
		{
			return null;
		}
		return await Task.Run(async delegate
		{
			List<UserModel> list = await (from v in App.Connection.Table<UserModel>()
				where v.userId.Equals(userId)
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task<UserModel> GetUserModelByName(string userName)
	{
		return await Task.Run(async delegate
		{
			List<UserModel> list = await (from v in App.Connection.Table<UserModel>()
				where v.username.Equals(userName)
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task<UserInfoModel> GetUserByName(string username)
	{
		if (string.IsNullOrEmpty(username))
		{
			return null;
		}
		return await Task.Run(async delegate
		{
			List<UserInfoModel> list = await (from v in App.Connection.Table<UserInfoModel>()
				where v.username.Equals(username)
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task<string> GetDisplayName(UserInfoModel userInfo)
	{
		string name = (string.IsNullOrEmpty(userInfo.name) ? userInfo.username : userInfo.name);
		if (string.IsNullOrEmpty(name))
		{
			UserModel userModel = await QueryUserModelListDbByIdAsync(userInfo.username);
			if (userModel != null)
			{
				name = userModel.phone;
			}
		}
		return name;
	}

	public static bool IsPro()
	{
		return IsPro2();
	}

	public static bool IsPro2()
	{
		return LocalSettings.Settings.IsPro;
	}

	public static bool IsUserValid()
	{
		return IsPro();
	}
}
