using System;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class AppLockDao
{
	private const string PassPrefix = "&";

	private const string PassPosfix = "#";

	public static async Task<AppLockModel> GetLockConfig()
	{
		try
		{
			string userId = LocalSettings.Settings.LoginUserId;
			if (string.IsNullOrEmpty(userId))
			{
				return null;
			}
			return await (from m in App.Connection.Table<AppLockModel>()
				where m.UserId == userId
				select m).FirstOrDefaultAsync();
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task SaveLockConfig(AppLockModel model)
	{
		AppLockModel appLockModel = await GetLockConfig();
		model.UserId = LocalSettings.Settings.LoginUserId;
		if (appLockModel == null)
		{
			await App.Connection.InsertAsync(model);
		}
		else
		{
			if (string.IsNullOrEmpty(model.Password))
			{
				model.Password = appLockModel.Password;
				model._Id = appLockModel._Id;
			}
			model.Locked = appLockModel.Locked;
			await App.Connection.UpdateAsync(model);
		}
		AppLockCache.SetModel(model);
	}

	public static async Task SetAppLocked(bool locked)
	{
		AppLockModel model = await GetLockConfig();
		if (model != null)
		{
			model.Locked = locked;
			await App.Connection.UpdateAsync(model);
			AppLockCache.SetModel(model);
		}
	}

	public static async Task ClearPassword()
	{
		await SaveLockPassword(string.Empty, clear: true);
	}

	public static async Task SaveLockPassword(string password, bool clear = false)
	{
		if (!string.IsNullOrEmpty(password))
		{
			string plainText = "&" + password + "#";
			password = Utils.Base64Encode(plainText);
		}
		AppLockModel config = await GetLockConfig();
		if (config != null)
		{
			config.Password = password;
			if (clear)
			{
				config.Locked = false;
			}
			await App.Connection.UpdateAsync(config);
			AppLockCache.SetModel(config);
		}
		else
		{
			AppLockModel model = new AppLockModel
			{
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				Password = password,
				MinLock = false,
				LockAfter = false,
				LockInterval = 5,
				LockWidget = false,
				Locked = false
			};
			await App.Connection.InsertAsync(model);
			AppLockCache.SetModel(model);
		}
	}
}
