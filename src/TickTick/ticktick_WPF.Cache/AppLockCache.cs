using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Cache;

public class AppLockCache
{
	private const string PassPrefix = "&";

	private const string PassPosfix = "#";

	public static AppLockModel LockModel;

	public static async Task<AppLockModel> GetModel()
	{
		if (LockModel == null || LockModel.UserId != LocalSettings.Settings.LoginUserId)
		{
			LockModel = await AppLockDao.GetLockConfig();
		}
		return LockModel;
	}

	public static void SetModel(AppLockModel model)
	{
		LockModel = model;
	}

	public static async Task<bool> IsLockValid()
	{
		return !string.IsNullOrEmpty(await GetLockPassword());
	}

	public static async Task<string> GetLockPassword()
	{
		return DecodePassword((await GetModel())?.Password);
	}

	private static string DecodePassword(string password)
	{
		if (string.IsNullOrEmpty(password))
		{
			return string.Empty;
		}
		string text = Utils.Base64Decode(password);
		if (text != null && text.StartsWith("&") && text.EndsWith("#"))
		{
			return text.Substring(1, text.Length - 2);
		}
		return string.Empty;
	}

	public static async Task<bool> GetAppLocked(bool isStart = false)
	{
		AppLockModel model = await GetModel();
		if (isStart)
		{
			bool flag = model != null;
			if (flag)
			{
				flag = await IsLockValid();
			}
			if (flag && model.MinLock && !model.Locked)
			{
				model.Locked = true;
				await App.Connection.UpdateAsync(model);
				UtilLog.Info("AppLockStart " + model.Locked);
			}
		}
		return model?.Locked ?? false;
	}

	public static async Task CheckLockStart()
	{
		await GetModel();
	}

	public static async Task<bool> IsMinLock()
	{
		AppLockModel appLockModel = await GetModel();
		if (appLockModel != null && appLockModel.MinLock)
		{
			return !string.IsNullOrEmpty(DecodePassword(appLockModel.Password));
		}
		return false;
	}
}
